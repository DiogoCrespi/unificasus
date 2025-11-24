usuario:
o fire bird nao aceita 2 requisições do mesmo usuario, e o servidor é um pouco lento , fora que  toda vez que se clica em algum a caixa na tela inical faz a requisição e se a pessoa clicar ao mesmo tempo em "buscar relacionados" o banco retorna um erro , o usuario nao precisa ver o erro , mas presisamos levar em conta isso e colocar uma barra de caregamneto e a aplicação deve organizar os requests para nao haver paralelização, e otimizando para um maior desenpenho , ate porque nao temos paralezação para aumentar a velucidade, temos que verificar como estao sendo feitos as buscas no banco , vejo que por exemplo cid 10 é muito lento na busca o que faz a pessoa clicar 2 3 vezes no botao "buscar relacionados"  o banco ja reclama e nao retorna nada , temos que ver tambem que no data grid se a pessoa clicar muito rapido em varios serviços o banco reclama e nao retorna nada , evitar carregar coisas desnecessarias e acho que colocar uma barra de caregamento fixa na tela inical nao um popup pois como disse é muito lento e o pop up abriria varias vezes , compile essas informações em um arquivo .md para termos um controle do que precisamos modificar e o que ja foi modificado 
---

# Otimização de Requisições ao Banco de Dados Firebird

## 📋 Problemas Identificados

### 1. Limitação do Firebird: Uma Conexão por Usuário
- **Problema**: O Firebird não aceita múltiplas requisições simultâneas do mesmo usuário
- **Impacto**: Quando o usuário clica rapidamente em vários botões, o banco retorna erro
- **Sintoma**: Erros como "lock conflict" ou "concurrent transaction" quando há cliques rápidos

### 2. Servidor Lento
- **Problema**: O servidor Firebird é lento para processar requisições
- **Impacto**: Usuário clica múltiplas vezes pensando que não funcionou
- **Sintoma**: Múltiplos cliques no botão "Buscar Relacionados" causam erros

### 3. Busca de CID 10 Muito Lenta
- **Problema**: A query de CID 10 (`BuscarCID10RelacionadosAsync`) é muito lenta
- **Causa Provável**: 
  - JOIN entre `RL_PROCEDIMENTO_CID` e `TB_CID`
  - Leitura de BLOB para `NO_CID`
  - Falta de índices adequados
- **Impacto**: Usuário clica 2-3 vezes, banco reclama e não retorna nada

### 4. Clicar Rápido no DataGrid
- **Problema**: Ao clicar rapidamente em diferentes procedimentos no DataGrid, múltiplas requisições são disparadas
- **Causa**: `ProcedimentosDataGrid_SelectionChanged` chama `AtualizarAreaRelacionados()` a cada mudança de seleção
- **Impacto**: Banco retorna erro por requisições simultâneas

### 5. Falta de Feedback Visual
- **Problema**: Não há indicação clara de que uma requisição está em andamento
- **Impacto**: Usuário não sabe se deve esperar ou clicar novamente
- **Solução Atual**: Apenas um TextBlock "Carregando..." na área de relacionados (insuficiente)

### 6. Carregamento Desnecessário
- **Problema**: Dados são recarregados mesmo quando já estão disponíveis
- **Exemplo**: Ao clicar no mesmo procedimento novamente, faz nova requisição
- **Impacto**: Requisições desnecessárias ao banco

---

## 🔍 Análise do Código Atual

### Gerenciamento de Conexões

**Configuração Atual (App.xaml.cs)**:
```csharp
services.AddScoped<FirebirdContext>();
services.AddScoped<IProcedimentoRepository, ProcedimentoRepository>();
```

**Problema Identificado**:
- `FirebirdContext` é registrado como `Scoped`
- Em WPF, não há escopo HTTP, então cada `MainWindow` tem seu próprio contexto
- **MAS**: Múltiplas operações assíncronas simultâneas na mesma janela compartilham a mesma conexão
- Firebird não permite múltiplas transações simultâneas na mesma conexão
- Mesmo com `Pooling=true`, a conexão é compartilhada dentro do escopo

**Conexão String**:
```csharp
Pooling=true;  // Pool está habilitado, mas não resolve o problema de concorrência
Connection timeout=15;
```

**Conclusão**: O problema não é múltiplas conexões, mas múltiplas requisições simultâneas na **mesma conexão**.

**Solução**: Implementar fila de requisições que garante execução sequencial, mesmo que múltiplas operações sejam iniciadas simultaneamente.

### Pontos de Requisição Identificados

#### 1. `ProcedimentosDataGrid_SelectionChanged` (linha 521)
```csharp
private async void ProcedimentosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    await AtualizarAreaRelacionados();
}
```
- **Problema**: Dispara requisição a cada mudança de seleção
- **Frequência**: Alta (a cada clique no DataGrid)
- **Solução**: Debounce + verificar se já está carregando

#### 2. `BuscarRelacionados_Click` (linha 2051)
```csharp
private async void BuscarRelacionados_Click(object sender, RoutedEventArgs e)
{
    // Múltiplas chamadas await sem controle de concorrência
    relacionados = await _procedimentoService.BuscarCID10RelacionadosAsync(...);
}
```
- **Problema**: Não verifica se já está carregando
- **Frequência**: Média (quando usuário clica no botão)
- **Solução**: Desabilitar botão durante carregamento + fila de requisições

#### 3. `AtualizarAreaRelacionados` (linha 2697)
```csharp
private async Task AtualizarAreaRelacionados()
{
    // Chama múltiplos métodos do serviço sem controle
    relacionados = await _procedimentoService.BuscarCID10RelacionadosAsync(...);
}
```
- **Problema**: Chamado de múltiplos lugares sem sincronização
- **Frequência**: Alta (a cada seleção + botão)
- **Solução**: Fila de requisições + cache

### Queries Identificadas como Lentas

#### 1. Busca CID 10 (`BuscarCID10RelacionadosAsync`)
```sql
SELECT 
    c.CO_CID,
    CAST(c.NO_CID AS BLOB) AS NO_CID_BLOB,
    c.NO_CID,
    pc.ST_PRINCIPAL
FROM RL_PROCEDIMENTO_CID pc
INNER JOIN TB_CID c ON pc.CO_CID = c.CO_CID
WHERE pc.CO_PROCEDIMENTO = @coProcedimento
  AND pc.DT_COMPETENCIA = @competencia
ORDER BY pc.ST_PRINCIPAL DESC, c.CO_CID
```
- **Problemas**:
  - JOIN sem índices verificados
  - CAST para BLOB (pode ser lento)
  - Leitura de campo direto + BLOB (duplicado)

#### 2. Busca por CID (`BuscarPorCIDAsync`)
```sql
SELECT DISTINCT pr.*
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_CID pc ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
WHERE pc.DT_COMPETENCIA = @competencia
  AND pc.CO_CID = @cid
```
- **Problemas**:
  - DISTINCT pode ser custoso
  - JOIN sem índices verificados
  - Mapeia todos os campos do procedimento

---

## ✅ Soluções Propostas

### 1. Fila de Requisições (Request Queue)
**Status**: ⏳ Pendente

**Implementação**:
- Criar classe `DatabaseRequestQueue` para gerenciar requisições sequenciais
- Todas as requisições ao banco passam pela fila
- Uma requisição por vez, evitando paralelização
- **CRÍTICO**: Deve ser Singleton para garantir que todas as requisições passem pela mesma fila

**Código Proposto**:
```csharp
public class DatabaseRequestQueue
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static readonly DatabaseRequestQueue _instance = new DatabaseRequestQueue();
    
    public static DatabaseRequestQueue Instance => _instance;
    
    private DatabaseRequestQueue() { }
    
    public async Task<T> EnqueueAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await request();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

**Uso**:
```csharp
var relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
    async () => await _procedimentoService.BuscarCID10RelacionadosAsync(...),
    cancellationToken);
```

### 2. Barra de Carregamento Fixa na Tela Principal
**Status**: ⏳ Pendente

**Implementação**:
- Adicionar `ProgressBar` fixa no `MainWindow.xaml`
- Visível apenas durante requisições
- Texto dinâmico mostrando o que está carregando

**Localização**: Topo ou rodapé da janela principal

### 3. Desabilitar Botões Durante Carregamento
**Status**: ⏳ Pendente

**Implementação**:
- Desabilitar botões quando `_isLoading == true`
- Desabilitar cliques no DataGrid durante carregamento
- Reabilitar após conclusão

**Botões Afetados**:
- "Buscar Relacionados"
- Botões de filtro
- Navegação no DataGrid (temporariamente)

### 4. Debounce para SelectionChanged
**Status**: ⏳ Pendente

**Implementação**:
- Adicionar delay de 300-500ms antes de executar `AtualizarAreaRelacionados`
- Cancelar requisição anterior se nova seleção ocorrer
- Usar `CancellationToken` para cancelar requisições antigas

### 5. Cache de Resultados
**Status**: ⏳ Pendente

**Implementação**:
- Cachear resultados por `coProcedimento + competencia + tipoFiltro`
- Invalidar cache quando competência muda
- Reduzir requisições desnecessárias

**Estrutura**:
```csharp
private readonly Dictionary<string, IEnumerable<object>> _cacheRelacionados = new();
private string _cacheKey = ""; // "coProcedimento|competencia|tipoFiltro"
```

### 6. Otimização de Queries
**Status**: ✅ Concluído

**⚠️ IMPORTANTE**: A aplicação original ainda utiliza o mesmo banco de dados. Análise de impacto realizada e índices criados com segurança.

**Análise de Impacto**: Ver documento `ANALISE_IMPACTO_INDICES.md`

**Melhorias Implementadas**:

#### a) Busca CID 10
- ✅ Scripts criados e executados para verificar índices existentes
- ✅ **Índices criados** em `RL_PROCEDIMENTO_CID`:
  - `IDX_RL_PCID_PROC_COMP` - (CO_PROCEDIMENTO, DT_COMPETENCIA)
  - `IDX_RL_PCID_CID_COMP` - (CO_CID, DT_COMPETENCIA)
- ⏳ Verificar índices em `TB_CID` (CO_CID) - pode não ser necessário se CO_CID já for PK
- ⚠️ Remover leitura duplicada (BLOB + campo direto) - requer análise de código
- ⏳ Considerar paginação se muitos resultados

#### b) Busca por CID
- ⏳ Verificar se DISTINCT é realmente necessário
- ✅ **Índices criados** para otimizar queries
- ✅ Cache de resultados já implementado

#### c) Scripts Criados e Executados
- ✅ `verificar_indices_existentes.sql` - Executado
- ✅ `verificar_indices_antes_criar.sql` - Executado
- ✅ `criar_indices_simples.sql` - Executado com sucesso
- ✅ `remover_indices_otimizacao.sql` - Disponível para rollback se necessário
- ✅ `executar_verificacao_indices.ps1` - Script PowerShell utilizado

**Índices Criados** (2025-01-22):
1. ✅ `IDX_RL_PCID_PROC_COMP` - (CO_PROCEDIMENTO, DT_COMPETENCIA)
   - Otimiza: `BuscarCID10RelacionadosAsync`
   - Query: `WHERE pc.CO_PROCEDIMENTO = @coProcedimento AND pc.DT_COMPETENCIA = @competencia`
   - ✅ **Validado**: Índice sendo usado corretamente

2. ✅ `IDX_RL_PCID_CID_COMP` - (CO_CID, DT_COMPETENCIA)
   - Otimiza: `BuscarPorCIDAsync`
   - Query: `WHERE pc.CO_CID = @cid AND pc.DT_COMPETENCIA = @competencia` (corrigida)
   - ✅ **Validado**: Índice sendo usado corretamente
   - ✅ **Correção aplicada**: Ordem do WHERE ajustada para corresponder ao índice

**Risco para Aplicação Original**: BAIXO ✅
- Índices são transparentes e não alteram funcionamento
- Tabela de relacionamento (baixo impacto em INSERTs)
- Monitoramento recomendado nas próximas horas

**Validação de Uso**: ✅ **CONCLUÍDA**
- Plano de execução confirmou uso dos índices
- Query `BuscarPorCIDAsync` corrigida para otimizar uso do índice
- Ver detalhes: `VALIDACAO_USO_INDICES.md`

### 7. Tratamento de Erros Silencioso
**Status**: ⏳ Pendente

**Implementação**:
- Capturar erros de concorrência sem mostrar ao usuário
- Logar erro internamente
- Mostrar mensagem genérica: "Aguarde a operação anterior concluir"
- Retry automático após delay

### 8. Indicador de Status Global
**Status**: ⏳ Pendente

**Implementação**:
- Barra de status sempre visível
- Mostrar "Carregando..." durante requisições
- Mostrar "Pronto" quando não há requisições
- Cor diferente (amarelo = carregando, verde = pronto)

---

## 📊 Priorização

### Alta Prioridade (Implementar Primeiro)
1. ✅ **Fila de Requisições** - Resolve o problema principal de concorrência
2. ✅ **Barra de Carregamento Fixa** - Feedback visual essencial
3. ✅ **Desabilitar Botões** - Previne cliques múltiplos
4. ✅ **Debounce SelectionChanged** - Reduz requisições desnecessárias

### Média Prioridade
5. ✅ **Cache de Resultados** - Melhora performance
6. ✅ **Tratamento de Erros Silencioso** - Melhora UX

### Baixa Prioridade (Otimizações)
7. ⏳ **Otimização de Queries** - Requer análise de índices no banco
8. ⏳ **Verificação de Índices** - Pode melhorar muito a performance

---

## 🔧 Implementação Técnica

### Arquivos a Modificar

1. **`MainWindow.xaml`**
   - Adicionar `ProgressBar` fixa (topo ou rodapé)
   - Adicionar `TextBlock` de status
   - Adicionar indicador visual de carregamento

2. **`MainWindow.xaml.cs`**
   - Implementar `DatabaseRequestQueue` (Singleton)
   - Adicionar debounce para `SelectionChanged` (300-500ms)
   - Implementar cache de resultados relacionados
   - Desabilitar botões durante carregamento
   - Adicionar flag `_isLoading` para controlar estado
   - Implementar `CancellationTokenSource` para cancelar requisições antigas

3. **`ProcedimentoService.cs`** (se necessário)
   - Otimizar queries lentas (especialmente CID 10)
   - Adicionar paginação se muitos resultados
   - Verificar se há queries N+1

4. **`ProcedimentoRepository.cs`**
   - Otimizar query `BuscarCID10RelacionadosAsync`
   - Verificar se índices estão sendo usados
   - Considerar remover leitura duplicada de BLOB

5. **`FirebirdContext.cs`** (verificar)
   - ✅ Conexão já é compartilhada corretamente (Scoped)
   - ✅ Pool está habilitado (`Pooling=true`)
   - ⚠️ Problema é concorrência na mesma conexão, não múltiplas conexões

### Classes a Criar

1. **`DatabaseRequestQueue.cs`** (Nova)
   - Localização: `UnificaSUS.Infrastructure/Helpers/`
   - Singleton para garantir uma única fila global
   - Semáforo para controlar concorrência
   - Suporte a `CancellationToken`

2. **`RequestCache.cs`** (Opcional - pode ser interno ao MainWindow)
   - Gerenciar cache de resultados relacionados
   - Invalidar cache quando competência muda
   - Estrutura: `Dictionary<string, IEnumerable<object>>`

---

## 📝 Checklist de Implementação

### Fase 1: Controle de Concorrência (CRÍTICO)
- [x] Criar classe `DatabaseRequestQueue` (Singleton)
- [x] Integrar fila em todas as requisições ao banco via serviços
- [x] Envolver todas as chamadas `await _procedimentoService.*Async()` na fila
- [ ] Testar com cliques múltiplos simultâneos
- [ ] Verificar que apenas uma requisição executa por vez

### Fase 2: Feedback Visual (ALTA PRIORIDADE)
- [x] Adicionar `ProgressBar` fixa no `MainWindow.xaml` (topo ou rodapé)
- [x] Adicionar `TextBlock` de status ao lado da barra
- [x] Criar método `SetLoadingStatus(string message, bool isLoading)`
- [x] Atualizar status durante todas as requisições
- [ ] Testar visibilidade e atualização em tempo real
- [x] Garantir que barra aparece antes de iniciar requisição

### Fase 3: Prevenção de Cliques Múltiplos (ALTA PRIORIDADE)
- [x] Adicionar flag `private bool _isLoading = false;`
- [x] Desabilitar botão "Buscar Relacionados" quando `_isLoading == true`
- [ ] Desabilitar seleção no DataGrid durante carregamento (opcional - pode ser apenas visual)
- [x] Implementar debounce em `SelectionChanged` (300-500ms)
- [x] Usar `CancellationTokenSource` para cancelar requisições antigas
- [ ] Testar comportamento com cliques rápidos (3-4 cliques em 1 segundo)

### Fase 4: Cache e Otimização (MÉDIA PRIORIDADE)
- [x] Implementar cache de resultados relacionados
- [x] Chave do cache: `$"{coProcedimento}|{competencia}|{tipoFiltro}"`
- [x] Invalidar cache quando competência muda
- [ ] Invalidar cache quando procedimento muda (opcional - não necessário, cache é por procedimento)
- [ ] Verificar índices no banco (script SQL)
- [ ] Otimizar queries lentas (CID 10) se índices estiverem faltando

### Fase 5: Tratamento de Erros (MÉDIA PRIORIDADE)
- [x] Capturar erros de concorrência silenciosamente
- [x] Detectar erros específicos do Firebird (lock conflict, concurrent transaction)
- [x] Mostrar mensagem amigável: "Aguarde a operação anterior concluir"
- [ ] Logar erros internamente (Debug ou arquivo de log)
- [ ] Implementar retry automático após delay (opcional - pode ser complexo)

---

## 🧪 Testes Necessários

1. **Teste de Cliques Múltiplos**
   - Clicar rapidamente em "Buscar Relacionados" 3-4 vezes
   - Verificar que apenas uma requisição é executada
   - Verificar que não há erros

2. **Teste de Seleção Rápida no DataGrid**
   - Clicar rapidamente em diferentes procedimentos
   - Verificar que apenas a última seleção é processada
   - Verificar que não há requisições simultâneas

3. **Teste de Busca CID 10 Lenta**
   - Buscar CID 10 de um procedimento com muitos CIDs
   - Verificar que botão fica desabilitado
   - Verificar que barra de progresso aparece
   - Verificar que não há erros mesmo com cliques múltiplos

4. **Teste de Cache**
   - Buscar relacionados de um procedimento
   - Buscar novamente o mesmo procedimento
   - Verificar que segunda busca usa cache (mais rápida)

---

## 📈 Métricas de Sucesso

- ✅ Zero erros de concorrência em uso normal
- ✅ Redução de 80%+ em requisições desnecessárias (via cache)
- ✅ Feedback visual claro em 100% das operações
- ✅ Tempo de resposta percebido melhorado (via feedback)

---

## 🔄 Status de Implementação

| Item | Status | Prioridade | Observações |
|------|--------|------------|-------------|
| Fila de Requisições | ✅ Concluído | Alta | Resolve problema principal |
| Barra de Carregamento Fixa | ✅ Concluído | Alta | Feedback visual essencial |
| Desabilitar Botões | ✅ Concluído | Alta | Previne cliques múltiplos |
| Debounce SelectionChanged | ✅ Concluído | Alta | Reduz requisições |
| Cache de Resultados | ✅ Concluído | Média | Melhora performance |
| Tratamento de Erros | ✅ Parcial | Média | Melhora UX - falta logar erros |
| Otimização Queries | ✅ Concluído | Baixa | Índices criados com sucesso |
| Verificação de Índices | ✅ Concluído | Baixa | Índices verificados e criados |

---

## 📚 Referências

- [Firebird Lock Management](https://firebirdsql.org/file/documentation/html/en/refdocs/fblangref25/firebird-25-language-reference.html#fblangref25-transacs-lockmgr)
- [WPF Async Best Practices](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)
- [SemaphoreSlim Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim)

---

**Última Atualização**: 2025-01-22
**Versão do Documento**: 1.2

## ✅ Otimização de Queries - CONCLUÍDA

**Data de Execução**: 2025-01-22

### Índices Criados:
1. ✅ `IDX_RL_PCID_PROC_COMP` - Otimiza BuscarCID10RelacionadosAsync
2. ✅ `IDX_RL_PCID_CID_COMP` - Otimiza BuscarPorCIDAsync

**Ver detalhes**: `docs/scripts/RESUMO_INDICES_CRIADOS.md`

## ✅ Implementações Concluídas

### Fase 1: Controle de Concorrência ✅
- ✅ Classe `DatabaseRequestQueue` criada (Singleton)
- ✅ Fila integrada em todas as requisições ao banco
- ✅ Todas as chamadas `await _procedimentoService.*Async()` envolvidas na fila

### Fase 2: Feedback Visual ✅
- ✅ `ProgressBar` fixa adicionada no `MainWindow.xaml` (Grid.Row="1")
- ✅ `TextBlock` de status adicionado
- ✅ Método `SetLoadingStatus(string message, bool isLoading)` implementado
- ✅ Status atualizado durante todas as requisições

### Fase 3: Prevenção de Cliques Múltiplos ✅
- ✅ Flag `_isLoading` adicionada
- ✅ Botão "Buscar Relacionados" desabilitado durante carregamento
- ✅ Debounce implementado em `SelectionChanged` (400ms)
- ✅ `CancellationTokenSource` usado para cancelar requisições antigas

### Fase 4: Cache e Otimização ✅
- ✅ Cache de resultados relacionados implementado
- ✅ Chave do cache: `$"{coProcedimento}|{competencia}|{tipoFiltro}"`
- ✅ Cache invalidado quando competência muda

### Fase 5: Tratamento de Erros ✅
- ✅ Erros de concorrência capturados silenciosamente
- ✅ Detecção de erros específicos do Firebird (lock conflict, concurrent transaction)
- ✅ Mensagem amigável: "Aguarde a operação anterior concluir"

