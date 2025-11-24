# Análise: CID10 e Uso de UPPER no Banco

## 📊 Resultados da Verificação

### 1. Estrutura de TB_CID

**Conclusão**: `TB_CID` **NÃO possui** `DT_COMPETENCIA`.

**Estrutura da tabela TB_CID**:
- `INDICE` (INTEGER) - Chave primária
- `CO_CID` (VARCHAR(4)) - Código CID
- `NO_CID` (VARCHAR(200)) - Nome/Descrição do CID
- `TP_AGRAVO` (VARCHAR(1)) - Tipo de agravo
- `TP_SEXO` (VARCHAR(1)) - Tipo de sexo
- `TP_ESTADIO` (VARCHAR(1)) - Tipo de estádio
- `VL_CAMPOS_IRRADIADOS` (INTEGER) - Valor campos irradiados

**Implicação**: 
- A tabela `TB_CID` contém uma lista **única** de CIDs que **não varia por competência**.
- Os CIDs são os mesmos para todas as competências.
- A relação entre procedimentos e CIDs **é que varia por competência** através da tabela `RL_PROCEDIMENTO_CID`.

### 2. Estrutura de RL_PROCEDIMENTO_CID

**Estrutura da tabela RL_PROCEDIMENTO_CID**:
- `INDICE` (INTEGER) - Chave primária
- `CO_CID` (VARCHAR(4)) - FK para TB_CID
- `CO_PROCEDIMENTO` (VARCHAR(10)) - FK para TB_PROCEDIMENTO
- `DT_COMPETENCIA` (VARCHAR(6)) - **Data de competência** ✅
- `ST_PRINCIPAL` (VARCHAR(1)) - Status principal (S/N)
- `NO_CID` (VARCHAR(100)) - Nome do CID (denormalizado)

**Estatísticas**:
- **Total de competências**: 93
- **Primeira competência**: 200801
- **Última competência**: 202510
- **Total de relacionamentos na última competência (202510)**: 187.912

**Conclusão**: 
- ✅ **CID10 é referente a cada competência** através da tabela `RL_PROCEDIMENTO_CID`.
- Cada competência pode ter diferentes relacionamentos entre procedimentos e CIDs.
- A query `BuscarCID10RelacionadosAsync` está correta ao filtrar por `DT_COMPETENCIA`.

### 3. Uso de UPPER no Banco

**Verificação realizada**:
- ✅ Procedures que usam UPPER
- ✅ Triggers que usam UPPER
- ✅ Views que usam UPPER
- ✅ Check constraints que usam UPPER
- ✅ Índices funcionais que usam UPPER

**Resultado**: 
- Nenhum objeto no banco (procedures, triggers, views) está usando UPPER.
- Isso significa que **o uso de UPPER está apenas no código da aplicação**.

### 4. Uso de UPPER no Código da Aplicação

**Arquivos que usam UPPER**:

#### RelatorioRepository.cs
- **Linha 210**: `UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) CONTAINING @filtro`
- **Linha 240**: `codigoOuNome.ToUpper()` (converte parâmetro para maiúsculas)
- **Linha 269**: `UPPER(CAST(g.NO_GRUPO AS VARCHAR(100))) CONTAINING @filtro`
- **Linha 291**: `filtro.ToUpper()` (converte parâmetro)
- **Linha 332**: `UPPER(CAST(sg.NO_SUB_GRUPO AS VARCHAR(100))) CONTAINING @filtro`
- **Linha 353**: `filtro.ToUpper()` (converte parâmetro)
- **Linha 383**: `UPPER(CAST(fo.NO_FORMA_ORGANIZACAO AS VARCHAR(100))) CONTAINING @filtro`
- **Linha 404**: `filtro.ToUpper()` (converte parâmetro)
- **Linha 434**: `UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) CONTAINING @filtro`
- **Linha 455**: `filtro.ToUpper()` (converte parâmetro)
- **Linha 690**: `UPPER(CAST(tl.NO_TIPO_LEITO AS VARCHAR(100))) CONTAINING @filtro`
- **Linha 711**: `filtro.ToUpper()` (converte parâmetro)
- **Linha 803**: `UPPER(CAST(reg.NO_REGISTRO AS VARCHAR(100))) CONTAINING @filtro`
- **Linha 823**: `filtro.ToUpper()` (converte parâmetro)

#### ImportRepository.cs
- **Linha 696**: `WHERE RDB$RELATION_NAME = UPPER(@tableName)`
- **Linha 865**: `WHERE RDB$RELATION_NAME = UPPER(@tableName)`
- **Linha 866**: `AND RDB$FIELD_NAME = UPPER(@columnName)`
- **Linha 896**: `WHERE RDB$RELATION_NAME = UPPER(@tableName)`
- **Linha 932**: `WHERE TRIM(RF.RDB$RELATION_NAME) = UPPER(@tableName)`
- **Linha 933**: `AND TRIM(RF.RDB$FIELD_NAME) = UPPER(@columnName)`

#### CompetenciaRepository.cs
- **Linha 186**: `WHERE RDB$RELATION_NAME = UPPER(@tabela)`

#### MainWindow.xaml.cs
- **Linha 837**: `var sexo = procedimento.TpSexo?.Trim()?.ToUpper();`
- **Linha 851**: `var financiamento = procedimento.Financiamento?.NoFinanciamento?.Trim() ?? procedimento.CoFinanciamento?.Trim()?.ToUpper() ?? "";`

**Observação**: 
- O uso de `UPPER` no código é principalmente para:
  1. **Busca case-insensitive** em campos de texto (NO_PROCEDIMENTO, NO_GRUPO, etc.)
  2. **Consulta de metadados** do Firebird (RDB$RELATION_NAME, RDB$FIELD_NAME)
  3. **Normalização de dados** na UI (ToUpper() em C#)

### 5. Identificação do Sistema Antigo vs Novo

**Sistema Novo (NewUnificasus)**:
- Usa `UPPER()` em queries SQL para busca case-insensitive
- Usa `ToUpper()` em C# para normalização
- Usa `CONTAINING` que já é case-insensitive no Firebird

**Sistema Antigo**:
- Não há evidências no banco de uso de UPPER em procedures/triggers/views
- Provavelmente usa queries diretas sem UPPER ou usa outra estratégia

**Recomendação**:
- O sistema novo está usando `UPPER()` + `CONTAINING`, o que pode ser redundante.
- `CONTAINING` já é case-insensitive no Firebird, então o `UPPER()` pode ser removido para melhor performance.
- No entanto, manter `UPPER()` no parâmetro de busca (`@filtro.ToUpper()`) pode ser útil para garantir consistência.

## 📝 Conclusões

1. ✅ **CID10 não é referente a cada competência diretamente** - A tabela `TB_CID` é única.
2. ✅ **A relação Procedimento-CID é referente a cada competência** - Através de `RL_PROCEDIMENTO_CID.DT_COMPETENCIA`.
3. ✅ **Nenhum objeto no banco usa UPPER** - O uso está apenas no código da aplicação.
4. ✅ **O sistema novo usa UPPER** - Principalmente em `RelatorioRepository.cs` e `ImportRepository.cs`.
5. ⚠️ **Possível otimização**: Remover `UPPER()` de queries que já usam `CONTAINING` (que é case-insensitive).

## 🔧 Próximos Passos

1. Verificar se o sistema antigo ainda está em uso e como ele faz buscas.
2. Considerar remover `UPPER()` de queries que usam `CONTAINING` para melhor performance.
3. Manter `UPPER()` apenas onde necessário (metadados do Firebird, normalização de parâmetros).

