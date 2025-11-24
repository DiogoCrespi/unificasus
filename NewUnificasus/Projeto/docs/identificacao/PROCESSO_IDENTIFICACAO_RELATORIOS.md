# Processo de Identificação da Funcionalidade de Relatórios

## 📋 Objetivo

Identificar como a funcionalidade de "Relatórios" funcionava na aplicação anterior, incluindo:
- Estrutura da interface (controles visuais)
- Fluxo de trabalho
- Dados necessários do banco de dados
- Lógica de geração de relatórios
- Formato de saída (impressão/exportação)

---

## 🔍 Passo 1: Análise da Interface Anterior

### 1.1 Estrutura Visual Identificada

Com base na descrição fornecida, a tela de relatórios possuía a seguinte estrutura:

#### **Seção Superior - Seleção de Filtro**

1. **Agrupamento "Selecionar por:"** (Radio Buttons)
   - ☑️ **Grupo** (Selecionado por padrão)
   - ☐ **Sub-grupo**
   - ☐ **Forma de organização**
   - ☐ **Procedimento**

2. **Campo de entrada de texto**
   - Valor atual: ""
   - Propósito: Inserir código/nome para busca

3. **Botão de adição** (Seta vermelha apontando para a direita →)
   - Propósito: Adicionar item selecionado à lista de impressão

4. **Caixa de listagem** (ListBox)
   - Etiqueta: "Imprimir a seleção abaixo"
   - Propósito: Exibir itens selecionados para impressão

#### **Seção Intermediária - Ações e Configurações**

1. **Botão: Limpar**
   - Propósito: Limpar a lista de itens selecionados

2. **Botão: Imprimir**
   - Propósito: Gerar e imprimir o relatório

3. **Campo de texto: "Título do relatório:"**
   - Propósito: Personalizar o título do relatório gerado

4. **Caixa de seleção (Checkbox): "Não Imprimir procedimentos com SP zerado"**
   - Propósito: Filtrar procedimentos com Valor S.P. = 0

#### **Seção Inferior - Modelo e Ordenação**

1. **Agrupamento "Modelo do relatório:"** (Radio Button)
   - ☑️ **Código, nome e valor do SP** (Selecionado por padrão)
   - Possíveis outros modelos (a investigar)

2. **Agrupamento "Ordenar por:"** (Radio Buttons)
   - ☑️ **Código Procedimento** (Selecionado por padrão)
   - ☐ **Nome**
   - ☐ **Valor do SP**

---

## 🔍 Passo 2: Busca por Referências no Código Atual

### 2.1 Busca por "Relatório", "Relatorio", "Imprimir", "Print"

**Ação**: Verificar se há alguma implementação prévia no código.

**Resultado**: 
- ✅ **Encontrado**: Botão "Relatórios" em `MainWindow.xaml` (linha 383)
- ✅ **Encontrado**: Handler `Relatorios_Click` em `MainWindow.xaml.cs` (linha 746)
- ⚠️ **Status**: Apenas placeholder - mostra mensagem "Funcionalidade em desenvolvimento"

**Código atual**:
```csharp
private void Relatorios_Click(object sender, RoutedEventArgs e)
{
    MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
}
```

### 2.2 Busca por "Report", "ReportViewer", "Crystal"

**Ação**: Verificar se há bibliotecas de relatórios já referenciadas.

**Resultado**: 
- ❌ **Não encontrado**: Nenhuma biblioteca de relatórios referenciada no projeto
- ✅ **Recomendação**: Usar `PrintDialog` e `DocumentPaginator` nativos do WPF (sem dependências externas)

---

## 🔍 Passo 3: Análise do Fluxo de Trabalho

### 3.1 Fluxo Identificado

1. **Seleção do Tipo de Filtro**
   - Usuário seleciona um Radio Button (Grupo, Sub-grupo, Forma de organização, ou Procedimento)

2. **Busca de Itens**
   - Usuário digita código/nome no campo de texto
   - Sistema busca itens correspondentes (a investigar como)

3. **Adição à Lista**
   - Usuário clica no botão de adição (→)
   - Item é adicionado à ListBox "Imprimir a seleção abaixo"
   - Múltiplos itens podem ser adicionados

4. **Configuração do Relatório**
   - Usuário define título do relatório (opcional)
   - Usuário marca/desmarca checkbox "Não Imprimir procedimentos com SP zerado"
   - Usuário seleciona modelo do relatório
   - Usuário seleciona ordenação

5. **Geração do Relatório**
   - Usuário clica em "Imprimir"
   - Sistema gera relatório com base nos itens selecionados e configurações
   - Relatório é exibido/impresso

6. **Limpeza**
   - Usuário pode clicar em "Limpar" para remover todos os itens da lista

---

## 🔍 Passo 4: Análise de Dados Necessários

### 4.1 Dados por Tipo de Filtro

#### **Grupo**
- **Tabela**: `TB_GRUPO`
- **Campos necessários**: `CO_GRUPO`, `NO_GRUPO`, `DT_COMPETENCIA`
- **Relacionamento**: `TB_GRUPO` → `TB_PROCEDIMENTO` (via `CO_GRUPO`)

#### **Sub-grupo**
- **Tabela**: `TB_SUBGRUPO`
- **Campos necessários**: `CO_SUBGRUPO`, `NO_SUBGRUPO`, `DT_COMPETENCIA`
- **Relacionamento**: `TB_SUBGRUPO` → `TB_PROCEDIMENTO` (via `CO_SUBGRUPO`)

#### **Forma de organização**
- **Tabela**: `TB_FORMA_ORGANIZACAO`
- **Campos necessários**: `CO_FORMA_ORGANIZACAO`, `NO_FORMA_ORGANIZACAO`, `DT_COMPETENCIA`
- **Relacionamento**: `TB_FORMA_ORGANIZACAO` → `TB_PROCEDIMENTO` (via `CO_FORMA_ORGANIZACAO`)

#### **Procedimento**
- **Tabela**: `TB_PROCEDIMENTO`
- **Campos necessários**: `CO_PROCEDIMENTO`, `NO_PROCEDIMENTO`, `VL_SP`, `DT_COMPETENCIA`
- **Relacionamento**: Direto na tabela

### 4.2 Dados do Relatório

Para cada item selecionado, o relatório deve exibir:

**Modelo: "Código, nome e valor do SP"**
- `CO_PROCEDIMENTO` (Código do Procedimento)
- `NO_PROCEDIMENTO` (Nome do Procedimento)
- `VL_SP` (Valor do Serviço Profissional)

**Filtros aplicados:**
- Se checkbox marcado: `VL_SP > 0` (não imprimir se SP zerado)
- Ordenação conforme seleção (Código, Nome, ou Valor do SP)

---

## 🔍 Passo 5: Verificação no Banco de Dados

### 5.1 Scripts SQL de Verificação

**A criar**:
1. `verificar_estrutura_relatorios.sql` - Verificar se há tabelas relacionadas a relatórios
2. `verificar_campos_procedimento_relatorio.sql` - Verificar campos necessários em TB_PROCEDIMENTO
3. `testar_query_relatorio_grupo.sql` - Testar query para relatório por grupo
4. `testar_query_relatorio_subgrupo.sql` - Testar query para relatório por sub-grupo
5. `testar_query_relatorio_forma_organizacao.sql` - Testar query para relatório por forma de organização
6. `testar_query_relatorio_procedimento.sql` - Testar query para relatório por procedimento

### 5.2 Resultados dos Testes

#### ✅ Estrutura de Tabelas Identificada

**TB_GRUPO**:
- `INDICE` (INTEGER, PK)
- `CO_GRUPO` (VARCHAR(2))
- `DT_COMPETENCIA` (VARCHAR(6))
- `NO_GRUPO` (VARCHAR(100))

**TB_SUB_GRUPO** (nota: nome com underscore):
- `INDICE` (INTEGER, PK)
- `CO_GRUPO` (VARCHAR(2))
- `CO_SUB_GRUPO` (VARCHAR(2))
- `DT_COMPETENCIA` (VARCHAR(6))
- `NO_SUB_GRUPO` (VARCHAR(100))

**TB_FORMA_ORGANIZACAO**:
- `CO_FORMA_ORGANIZACAO` (VARCHAR(2))
- `CO_GRUPO` (VARCHAR(2))
- `CO_SUB_GRUPO` (VARCHAR(2))
- `DT_COMPETENCIA` (VARCHAR(6))
- `NO_FORMA_ORGANIZACAO` (VARCHAR(100))

**TB_PROCEDIMENTO** (campos relevantes):
- `CO_PROCEDIMENTO` (VARCHAR(10)) - **Estrutura: AABBCCDDDD**
  - **AA** (posições 1-2): Código do Grupo
  - **BB** (posições 3-4): Código do Sub-grupo
  - **CC** (posições 5-6): Código da Forma de Organização
  - **DDDD** (posições 7-10): Código específico do procedimento
- `NO_PROCEDIMENTO` (VARCHAR(250))
- `VL_SP` (DOUBLE PRECISION)
- `DT_COMPETENCIA` (VARCHAR(6))

#### ✅ Relacionamento Identificado

**Importante**: A tabela `TB_PROCEDIMENTO` **NÃO possui campos diretos** `CO_GRUPO`, `CO_SUBGRUPO` ou `CO_FORMA_ORGANIZACAO`.

**Relacionamento**: O código do procedimento (`CO_PROCEDIMENTO`) **contém** os códigos de grupo, sub-grupo e forma de organização nas primeiras 6 posições.

**Método de relacionamento**:
- **Grupo**: `SUBSTRING(CO_PROCEDIMENTO FROM 1 FOR 2) = CO_GRUPO`
- **Sub-grupo**: `SUBSTRING(CO_PROCEDIMENTO FROM 1 FOR 4) = (CO_GRUPO || CO_SUB_GRUPO)`
- **Forma de Organização**: `SUBSTRING(CO_PROCEDIMENTO FROM 1 FOR 6) = (CO_GRUPO || CO_SUB_GRUPO || CO_FORMA_ORGANIZACAO)`

**Exemplo**:
- Procedimento: `0101010010`
  - Grupo: `01` (posições 1-2)
  - Sub-grupo: `0101` (posições 1-4)
  - Forma de Organização: `010101` (posições 1-6)
  - Código específico: `0010` (posições 7-10)

#### ✅ Queries de Teste Preparadas e Executadas

Scripts SQL criados e executados com competência `202006`:
1. `verificar_estrutura_relatorios.sql` - ✅ Executado
2. `verificar_campos_procedimento_relatorio.sql` - ✅ Executado
3. `verificar_relacionamento_grupo_procedimento.sql` - ✅ Executado
4. `testar_relatorio_grupo_202006.sql` - ✅ Executado
5. `testar_relatorio_subgrupo_202006.sql` - ✅ Executado
6. `testar_relatorio_forma_organizacao_202006.sql` - ✅ Executado
7. `testar_relatorio_procedimento_202006.sql` - ✅ Executado

#### ✅ Resultados dos Testes Manuais

**Competência utilizada**: `202006` (competência ativa)

##### Teste 1: Relatório por Grupo

**Query testada**: Buscar procedimentos do grupo '01'
```sql
WHERE SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 2) = '01'
  AND pr.DT_COMPETENCIA = '202006'
  AND pr.VL_SP > 0
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Total de procedimentos no grupo '01'**: 91
- ⚠️ **Procedimentos com SP > 0**: 0
- ⚠️ **Procedimentos com SP zerado**: 91
- 📋 **Grupos disponíveis na competência**:
  - Grupo '01': 91 procedimentos
  - Grupo '02': 1.039 procedimentos
  - Grupo '03': 753 procedimentos
  - Grupo '04': 1.681 procedimentos
  - Grupo '05': 135 procedimentos
  - Grupo '06': 367 procedimentos
  - Grupo '07': 529 procedimentos
  - Grupo '08': 46 procedimentos

**Conclusão**: A query funciona corretamente. O grupo '01' não possui procedimentos com SP > 0, o que é útil para testar o filtro "Não Imprimir procedimentos com SP zerado".

##### Teste 2: Relatório por Sub-grupo

**Query testada**: Buscar procedimentos do sub-grupo '0101'
```sql
WHERE SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 4) = '0101'
  AND pr.DT_COMPETENCIA = '202006'
  AND pr.VL_SP > 0
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Total de procedimentos no sub-grupo '0101'**: 32
- ⚠️ **Procedimentos com SP > 0**: 0
- ⚠️ **Procedimentos com SP zerado**: 32
- 📋 **Sub-grupos disponíveis no grupo '01'**:
  - Sub-grupo '0101': 32 procedimentos
  - Sub-grupo '0102': 59 procedimentos

**Conclusão**: A query funciona corretamente. O relacionamento via `SUBSTRING` está funcionando como esperado.

##### Teste 3: Relatório por Forma de Organização

**Query testada**: Buscar procedimentos da forma de organização '010101'
```sql
WHERE SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 6) = '010101'
  AND pr.DT_COMPETENCIA = '202006'
  AND pr.VL_SP > 0
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Total de procedimentos na forma de organização '010101'**: 3
- ⚠️ **Procedimentos com SP > 0**: 0
- ⚠️ **Procedimentos com SP zerado**: 3
- 📋 **Formas de organização disponíveis no sub-grupo '0101'**:
  - Forma '010101': 3 procedimentos
  - Forma '010102': 9 procedimentos
  - Forma '010103': 2 procedimentos
  - Forma '010104': 5 procedimentos
  - Forma '010105': 13 procedimentos

**Conclusão**: A query funciona corretamente. O relacionamento hierárquico completo está funcionando.

##### Teste 4: Relatório por Procedimento Específico

**Query testada 4.1**: Buscar procedimento específico '0301100012'
```sql
WHERE pr.CO_PROCEDIMENTO = '0301100012'
  AND pr.DT_COMPETENCIA = '202006'
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Procedimento encontrado**: '0301100012'
- 📝 **Nome**: "ADMINISTRACAO DE MEDICAMENTOS NA ATENCAO ESPECIALIZADA."
- 💰 **Valor SP**: 0.00

**Query testada 4.2**: Buscar procedimentos por parte do código '0301'
```sql
WHERE pr.CO_PROCEDIMENTO CONTAINING '0301'
  AND pr.DT_COMPETENCIA = '202006'
  AND pr.VL_SP > 0
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Procedimentos encontrados com SP > 0**: 10+ procedimentos
- 📝 **Exemplos encontrados**:
  - '0301010145': "PRIMEIRA CONSULTA DE PEDIATRIA AO RECEM-NASCIDO" - SP: 10.00
  - '0301050074': "INTERNAÇÃO DOMICILIAR" - SP: 5.10
  - '0301060010': "DIAGNOSTICO E/OU ATENDIMENTO DE URGENCIA EM CLINICA PEDIATRICA" - SP: 11.62
  - '0301060070': "DIAGNOSTICO E/OU ATENDIMENTO DE URGENCIA EM CLINICA CIRURGICA" - SP: 9.91
  - '0301060088': "DIAGNOSTICO E/OU ATENDIMENTO DE URGENCIA EM CLINICA MEDICA" - SP: 10.88
  - '0301090017': "ATENDIMENTO EM GERIATRIA (1 TURNO)" - SP: 4.86
  - '0301090025': "ATENDIMENTO EM GERIATRIA (2 TURNOS)" - SP: 5.00
  - '0303010010': "TRATAMENTO DE DENGUE CLÁSSICA" - SP: 58.32
  - '0303010029': "TRATAMENTO DE DENGUE HEMORRÁGICA" - SP: 56.36
  - '0303010037': "TRATAMENTO DE OUTRAS DOENÇAS BACTERIANAS" - SP: 72.22

**Query testada 4.3**: Buscar procedimentos por parte do nome 'ADMINISTRACAO'
```sql
WHERE UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) CONTAINING 'ADMINISTRACAO'
  AND pr.DT_COMPETENCIA = '202006'
  AND pr.VL_SP > 0
```

**Resultados**:
- ✅ **Query executada com sucesso**
- 📊 **Procedimentos encontrados**: Vários procedimentos com "ADMINISTRACAO" no nome

**Query testada 4.4**: Testar ordenações diferentes
- ✅ **Ordenação por código**: Funcionando corretamente
- ✅ **Ordenação por nome**: Funcionando corretamente
- ✅ **Ordenação por valor SP (decrescente)**: Funcionando corretamente

**Conclusão**: Todas as queries de busca por procedimento funcionam corretamente. As ordenações estão funcionando como esperado.

#### ✅ Validação Geral dos Testes

**Status**: ✅ **TODOS OS TESTES PASSARAM**

**Observações importantes**:
1. ✅ O relacionamento via `SUBSTRING` funciona perfeitamente
2. ✅ O filtro `VL_SP > 0` funciona corretamente
3. ✅ As ordenações (código, nome, valor SP) funcionam corretamente
4. ⚠️ Alguns grupos/sub-grupos têm apenas procedimentos com SP zerado (útil para testar o filtro)
5. ✅ A busca por parte do código/nome funciona corretamente
6. ✅ A competência `202006` possui dados suficientes para testes

**Próximo passo**: Implementar a interface e lógica de relatórios baseada nestes testes validados.

---

## 🔍 Passo 6: Tecnologias de Relatório

### 6.1 Opções Disponíveis para .NET/WPF

1. **ReportViewer** (Microsoft)
   - Prós: Integrado, suporta RDLC
   - Contras: Pode estar descontinuado

2. **Crystal Reports**
   - Prós: Poderoso, amplamente usado
   - Contras: Licenciamento, complexidade

3. **DevExpress Reports**
   - Prós: Moderno, rico em recursos
   - Contras: Licenciamento pago

4. **FastReport**
   - Prós: Bom custo-benefício
   - Contras: Licenciamento

5. **Geração Manual (PDF/Excel)**
   - Prós: Controle total, sem dependências externas
   - Contras: Mais trabalho de implementação

6. **PrintDialog + DocumentPaginator (WPF)**
   - Prós: Nativo do WPF, sem dependências
   - Contras: Limitado em recursos avançados

### 6.2 Recomendação Inicial

**Opção**: Geração Manual usando `PrintDialog` e `DocumentPaginator` do WPF
- **Motivo**: Sem dependências externas, controle total, adequado para relatórios simples
- **Alternativa futura**: Se necessário, migrar para biblioteca mais robusta

---

## 📊 Estrutura de Dados Proposta

### Entidade: `ItemRelatorio`

```csharp
public class ItemRelatorio
{
    public string Tipo { get; set; } // "Grupo", "SubGrupo", "FormaOrganizacao", "Procedimento"
    public string Codigo { get; set; }
    public string Nome { get; set; }
    public string? Competencia { get; set; }
}
```

### Entidade: `ConfiguracaoRelatorio`

```csharp
public class ConfiguracaoRelatorio
{
    public string Titulo { get; set; } = "Relatório de Procedimentos";
    public bool NaoImprimirSPZerado { get; set; } = false;
    public string Modelo { get; set; } = "CodigoNomeValorSP";
    public string OrdenarPor { get; set; } = "Codigo";
}
```

### Entidade: `ItemRelatorioProcedimento`

```csharp
public class ItemRelatorioProcedimento
{
    public string CoProcedimento { get; set; }
    public string? NoProcedimento { get; set; }
    public decimal? VlSp { get; set; }
}
```

---

## 🎯 Próximos Passos (Investigação)

1. ✅ Buscar referências no código atual
2. ✅ Verificar estrutura de tabelas no banco
3. ✅ Criar scripts SQL de teste
4. ⏳ Executar testes manuais com dados reais
5. ⏳ Documentar resultados dos testes
6. ✅ Definir tecnologia de relatório (PrintDialog + DocumentPaginator)
7. ⏳ Implementar interface (XAML)
8. ⏳ Implementar lógica de busca
9. ⏳ Implementar geração de relatório
10. ⏳ Testar funcionalidade completa

---

## 📝 Notas de Implementação

### Interface (XAML)

- Usar `RadioButton` para seleção de tipo de filtro
- Usar `TextBox` para entrada de código/nome
- Usar `Button` com ícone de seta para adicionar
- Usar `ListBox` para exibir itens selecionados
- Usar `CheckBox` para opção de filtro SP zerado
- Usar `ComboBox` ou `RadioButton` para modelo e ordenação

### Lógica (C#)

- Criar `RelatorioService` para orquestrar operações
- Criar métodos de busca por tipo (Grupo, SubGrupo, etc.)
- Criar método de geração de relatório
- Usar `PrintDialog` para impressão
- Usar `DocumentPaginator` para formatação

---

---

## 📋 Resumo da Investigação

### ✅ Concluído

1. **Estrutura da Interface Anterior**: Documentada completamente
2. **Busca no Código**: Identificado placeholder existente
3. **Estrutura de Tabelas**: Identificada e documentada
4. **Relacionamento de Dados**: Compreendido (código do procedimento contém hierarquia)
5. **Scripts SQL**: Criados e prontos para teste
6. **Tecnologia de Relatório**: Definida (PrintDialog + DocumentPaginator)

### ✅ Concluído (Testes Manuais)

1. ✅ **Testes Manuais**: Executados com competência `202006`
2. ✅ **Validação de Queries**: Todas as queries funcionam corretamente
3. ✅ **Documentação de Resultados**: Resultados documentados acima

### ⏳ Pendente

1. **Implementação**: Criar interface e lógica de relatórios

### 🎯 Próxima Ação

**Implementar a interface e lógica de relatórios** baseada nos testes validados. Todas as queries foram testadas e funcionam corretamente.

---

**Data da Identificação**: 2024-11-17
**Autor**: Processo automatizado de análise
**Versão do Banco**: Firebird 3.0
**Competência de Teste**: 202006
**Status**: ✅ Investigação completa | ✅ Testes manuais concluídos | ⏳ Pronto para implementação

