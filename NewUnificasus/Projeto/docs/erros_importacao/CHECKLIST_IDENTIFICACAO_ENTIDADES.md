# Checklist de Identificação de Entidades no Banco de Dados

## 📋 Objetivo

Identificar onde e como as seguintes funcionalidades armazenam seus dados no banco de dados, seguindo o processo de identificação estabelecido para `TB_PROCOMUNS`.

---

## ✅ Status do Checklist

| Item | Status | Tabela Identificada | Documentação |
|------|--------|---------------------|--------------|
| Cid10 | ✅ Concluído | TB_CID, RL_PROCEDIMENTO_CID | PROCESSO_IDENTIFICACAO_CID10.md |
| Compatíveis | ✅ Concluído | RL_PROCEDIMENTO_COMPATIVEL | - |
| Habilitação | ✅ Concluído | TB_HABILITACAO, RL_PROCEDIMENTO_HABILITACAO | - |
| CBO | ✅ Concluído | TB_OCUPACAO, RL_PROCEDIMENTO_OCUPACAO | - |
| Serviços | ✅ Concluído | TB_SERVICO, RL_PROCEDIMENTO_SERVICO | - |
| Tipo de Leito | ✅ Concluído | TB_TIPO_LEITO, RL_PROCEDIMENTO_LEITO | - |
| Modalidade | ✅ Concluído | TB_MODALIDADE, RL_PROCEDIMENTO_MODALIDADE | - |
| Instrumento de Registro | ❌ Não Encontrado | - | - |
| Detalhes | ✅ Concluído | TB_DETALHE, RL_PROCEDIMENTO_DETALHE, TB_DESCRICAO_DETALHE | Descrição longa em TB_DESCRICAO_DETALHE.DE_DETALHE |
| Incremento | ✅ Concluído | RL_PROCEDIMENTO_INCREMENTO | - |
| Descrição | ✅ Concluído | Campo em várias tabelas (NO_*) | - |

**Legenda:**
- ⏳ Pendente: Ainda não foi identificado
- 🔍 Em Análise: Processo de identificação em andamento
- ✅ Concluído: Identificação completa e documentada
- ❌ Não Encontrado: Não existe no banco de dados

---

## 🔍 Processo de Identificação (Para cada item)

Para cada item da lista, seguir os seguintes passos:

### Passo 1: Análise do Código Existente

1. **Buscar referências no código-fonte**
   - Buscar por termos relacionados (maiúsculas, minúsculas, variações)
   - Verificar handlers de botões/menus
   - Verificar entidades no Core
   - Verificar repositórios e serviços

2. **Documentar resultados**
   - Listar arquivos encontrados
   - Listar classes/entidades relacionadas
   - Anotar funcionalidades existentes

### Passo 2: Busca por Referências no Banco de Dados

1. **Listar todas as tabelas do banco**
   ```sql
   SELECT RF.RDB$RELATION_NAME AS TABELA
   FROM RDB$RELATIONS RF
   WHERE RF.RDB$SYSTEM_FLAG = 0
     AND RF.RDB$RELATION_TYPE = 0
   ORDER BY RF.RDB$RELATION_NAME;
   ```

2. **Buscar tabelas relacionadas**
   - Procurar por nomes similares (ex: CID, CBO, HABILITACAO, etc.)
   - Verificar variações de nomenclatura
   - Verificar campos relacionados em outras tabelas

3. **Verificar campos em tabelas principais**
   - Verificar `TB_PROCEDIMENTO` para campos relacionados
   - Verificar outras tabelas principais do sistema

### Passo 3: Análise da Estrutura da Tabela

1. **Verificar estrutura completa**
   ```sql
   SELECT 
       RF.RDB$FIELD_NAME AS CAMPO,
       RF.RDB$FIELD_SOURCE AS TIPO_ORIGEM,
       CASE 
           WHEN F.RDB$FIELD_TYPE = 7 THEN 'SMALLINT'
           WHEN F.RDB$FIELD_TYPE = 8 THEN 'INTEGER'
           WHEN F.RDB$FIELD_TYPE = 10 THEN 'FLOAT'
           WHEN F.RDB$FIELD_TYPE = 12 THEN 'DATE'
           WHEN F.RDB$FIELD_TYPE = 13 THEN 'TIME'
           WHEN F.RDB$FIELD_TYPE = 14 THEN 'CHAR'
           WHEN F.RDB$FIELD_TYPE = 16 THEN 'BIGINT'
           WHEN F.RDB$FIELD_TYPE = 27 THEN 'DOUBLE PRECISION'
           WHEN F.RDB$FIELD_TYPE = 35 THEN 'TIMESTAMP'
           WHEN F.RDB$FIELD_TYPE = 37 THEN 'VARCHAR'
           WHEN F.RDB$FIELD_TYPE = 261 THEN 'BLOB'
           ELSE 'OUTRO'
       END AS TIPO,
       F.RDB$FIELD_LENGTH AS TAMANHO,
       RF.RDB$NULL_FLAG AS NOT_NULL
   FROM RDB$RELATION_FIELDS RF
   LEFT JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
   WHERE RF.RDB$RELATION_NAME = 'NOME_DA_TABELA'
   ORDER BY RF.RDB$FIELD_POSITION;
   ```

2. **Verificar índices e constraints**
   ```sql
   -- Chaves primárias
   SELECT 
       S.RDB$INDEX_NAME AS INDICE,
       S.RDB$FIELD_NAME AS CAMPO,
       S.RDB$FIELD_POSITION AS POSICAO
   FROM RDB$INDEX_SEGMENTS S
   JOIN RDB$INDICES I ON S.RDB$INDEX_NAME = I.RDB$INDEX_NAME
   WHERE I.RDB$RELATION_NAME = 'NOME_DA_TABELA'
     AND I.RDB$UNIQUE_FLAG = 1
   ORDER BY S.RDB$INDEX_NAME, S.RDB$FIELD_POSITION;
   
   -- Chaves estrangeiras
   SELECT 
       RC.RDB$CONSTRAINT_NAME AS CONSTRAINT_NAME,
       RC.RDB$CONSTRAINT_TYPE AS TIPO,
       S1.RDB$FIELD_NAME AS CAMPO_ORIGEM,
       I2.RDB$RELATION_NAME AS TABELA_DESTINO,
       S2.RDB$FIELD_NAME AS CAMPO_DESTINO
   FROM RDB$RELATION_CONSTRAINTS RC
   LEFT JOIN RDB$INDEX_SEGMENTS S1 ON RC.RDB$INDEX_NAME = S1.RDB$INDEX_NAME
   LEFT JOIN RDB$REF_CONSTRAINTS REF ON RC.RDB$CONSTRAINT_NAME = REF.RDB$CONSTRAINT_NAME
   LEFT JOIN RDB$RELATION_CONSTRAINTS RC2 ON REF.RDB$CONST_NAME_UQ = RC2.RDB$CONSTRAINT_NAME
   LEFT JOIN RDB$INDICES I2 ON RC2.RDB$INDEX_NAME = I2.RDB$INDEX_NAME
   LEFT JOIN RDB$INDEX_SEGMENTS S2 ON I2.RDB$INDEX_NAME = S2.RDB$INDEX_NAME
   WHERE RC.RDB$RELATION_NAME = 'NOME_DA_TABELA'
   ORDER BY RC.RDB$CONSTRAINT_NAME;
   ```

3. **Verificar dados existentes**
   ```sql
   SELECT COUNT(*) AS TOTAL_REGISTROS
   FROM NOME_DA_TABELA;
   
   SELECT *
   FROM NOME_DA_TABELA
   ROWS 10; -- Primeiros 10 registros
   ```

### Passo 4: Documentação da Estrutura

1. **Criar documento específico** (seguindo padrão de `PROCESSO_IDENTIFICACAO_TB_PROCOMUNS.md`)
2. **Documentar estrutura completa**
3. **Documentar relacionamentos**
4. **Documentar conclusões**

---

## 📝 Itens a Identificar

### 1. Cid10

**Objetivo**: Identificar tabela e estrutura relacionada a CID-10 (Classificação Internacional de Doenças, 10ª revisão).

**Termos de busca sugeridos**: CID, CID10, CID_10, DOENCA, DIAGNOSTICO

**Tabelas identificadas**:
- ✅ `TB_CID` - Tabela principal de CID-10
- ✅ `RL_PROCEDIMENTO_CID` - Tabela de relacionamento entre Procedimento e CID

**Estrutura identificada**:

**TB_CID**:
- `CO_CID` (VARCHAR(4), PK) - Código CID
- `NO_CID` (VARCHAR(100)) - Nome/Descrição do CID
- `TP_AGRAVO` (VARCHAR(1)) - Tipo de agravo
- `TP_SEXO` (VARCHAR(1)) - Tipo de sexo
- `TP_ESTADIO` (VARCHAR(1)) - Tipo de estádio
- `VL_CAMPOS_IRRADIADOS` (INTEGER) - Valor campos irradiados

**RL_PROCEDIMENTO_CID**:
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_CID` (VARCHAR(4)) - Código CID (FK)
- `ST_PRINCIPAL` (VARCHAR(1)) - Status principal (S/N)
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Entidades no código**:
- ✅ `CID` (Core/Entities/CID.cs)
- ✅ `ProcedimentoCID` (Core/Entities/ProcedimentoCID.cs)
- ✅ `BuscarCID10RelacionadosAsync` (ProcedimentoRepository.cs)

**Total de registros**: 14.230 CIDs no banco

**Status**: ✅ Concluído

---

### 2. Compatíveis

**Objetivo**: Identificar tabela e estrutura relacionada a procedimentos compatíveis.

**Termos de busca sugeridos**: COMPATIVEL, COMPATIVEL, COMPAT, PROCEDIMENTO_COMPATIVEL

**Tabelas identificadas**:
- ✅ `RL_PROCEDIMENTO_COMPATIVEL` - Tabela de relacionamento entre procedimentos compatíveis

**Estrutura identificada**:

**RL_PROCEDIMENTO_COMPATIVEL**:
- `CO_PROCEDIMENTO_PRINCIPAL` (VARCHAR(10)) - Código do procedimento principal (FK)
- `CO_PROCEDIMENTO_COMPATIVEL` (VARCHAR(10)) - Código do procedimento compatível (FK)
- `CO_REGISTRO_PRINCIPAL` (VARCHAR(2)) - Código de registro principal
- `CO_REGISTRO_COMPATIVEL` (VARCHAR(2)) - Código de registro compatível
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência
- `QT_PERMITIDA` (INTEGER) - Quantidade permitida
- `TP_COMPATIBILIDADE` (VARCHAR(1)) - Tipo de compatibilidade
- `NO_PROCEDIMENTO` (VARCHAR(250)) - Nome do procedimento (denormalizado)

**Métodos no código**:
- ✅ `BuscarCompativeisRelacionadosAsync` (ProcedimentoRepository.cs, ProcedimentoService.cs)

**Status**: ✅ Concluído

---

### 3. Habilitação

**Objetivo**: Identificar tabela e estrutura relacionada a habilitações de estabelecimentos.

**Termos de busca sugeridos**: HABILITACAO, HABILIT, HABIL, ESTABELECIMENTO_HABILITACAO

**Tabelas identificadas**:
- ✅ `TB_HABILITACAO` - Tabela principal de habilitações
- ✅ `RL_PROCEDIMENTO_HABILITACAO` - Tabela de relacionamento entre Procedimento e Habilitação

**Estrutura identificada**:

**TB_HABILITACAO**:
- `CO_HABILITACAO` (VARCHAR(4), PK) - Código da habilitação
- `NO_HABILITACAO` (VARCHAR(150)) - Nome da habilitação
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**RL_PROCEDIMENTO_HABILITACAO**:
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_HABILITACAO` (VARCHAR(4)) - Código da habilitação (FK)
- `NU_GRUPO_HABILITACAO` (VARCHAR(4)) - Número do grupo de habilitação
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência
- `NO_HABILITACAO` (VARCHAR(150)) - Nome da habilitação (denormalizado)

**Métodos no código**:
- ✅ `BuscarHabilitacoesRelacionadasAsync` (ProcedimentoRepository.cs, ProcedimentoService.cs)

**Status**: ✅ Concluído

---

### 4. CBO

**Objetivo**: Identificar tabela e estrutura relacionada a CBO (Classificação Brasileira de Ocupações).

**Termos de busca sugeridos**: CBO, OCUPACAO, PROFISSAO

**Tabelas identificadas**:
- ✅ `TB_OCUPACAO` - Tabela principal de ocupações (CBO)
- ✅ `RL_PROCEDIMENTO_OCUPACAO` - Tabela de relacionamento entre Procedimento e Ocupação

**Estrutura identificada**:

**TB_OCUPACAO**:
- `CO_OCUPACAO` (VARCHAR(6), PK) - Código da ocupação (CBO)
- `NO_OCUPACAO` (VARCHAR(150)) - Nome da ocupação

**RL_PROCEDIMENTO_OCUPACAO**:
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_OCUPACAO` (VARCHAR(6)) - Código da ocupação (FK)
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Métodos no código**:
- ✅ `BuscarCBOsRelacionadosAsync` (ProcedimentoRepository.cs, ProcedimentoService.cs)

**Status**: ✅ Concluído

---

### 5. Serviços

**Objetivo**: Identificar tabela e estrutura relacionada a serviços de saúde.

**Termos de busca sugeridos**: SERVICO, SERVICOS, TB_SERVICO

**Tabelas identificadas**:
- ✅ `TB_SERVICO` - Tabela principal de serviços
- ✅ `RL_PROCEDIMENTO_SERVICO` - Tabela de relacionamento entre Procedimento e Serviço
- ✅ `TB_SERVICO_CLASSIFICACAO` - Tabela de classificação de serviços

**Estrutura identificada**:

**TB_SERVICO**:
- `CO_SERVICO` (VARCHAR(3), PK) - Código do serviço
- `NO_SERVICO` (VARCHAR(120)) - Nome do serviço
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Métodos no código**:
- ✅ `Servico` (Core/Entities/Servico.cs)
- ✅ `ProcedimentoServico` (Core/Entities/ProcedimentoServico.cs)

**Status**: ✅ Concluído

---

### 6. Tipo de Leito

**Objetivo**: Identificar tabela e estrutura relacionada a tipos de leito hospitalar.

**Termos de busca sugeridos**: LEITO, TIPO_LEITO, TB_LEITO, LEITO_TIPO

**Tabelas identificadas**:
- ✅ `TB_TIPO_LEITO` - Tabela principal de tipos de leito
- ✅ `RL_PROCEDIMENTO_LEITO` - Tabela de relacionamento entre Procedimento e Tipo de Leito

**Estrutura identificada**:

**TB_TIPO_LEITO**:
- `CO_TIPO_LEITO` (VARCHAR(2), PK) - Código do tipo de leito
- `NO_TIPO_LEITO` (VARCHAR(60)) - Nome do tipo de leito
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**RL_PROCEDIMENTO_LEITO**:
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_TIPO_LEITO` (VARCHAR(2)) - Código do tipo de leito (FK)
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Status**: ✅ Concluído

---

### 7. Modalidade

**Objetivo**: Identificar tabela e estrutura relacionada a modalidades de atendimento.

**Termos de busca sugeridos**: MODALIDADE, MODAL, TB_MODALIDADE

**Tabelas identificadas**:
- ✅ `TB_MODALIDADE` - Tabela principal de modalidades
- ✅ `RL_PROCEDIMENTO_MODALIDADE` - Tabela de relacionamento entre Procedimento e Modalidade

**Estrutura identificada**:

**TB_MODALIDADE**:
- `CO_MODALIDADE` (VARCHAR(2), PK) - Código da modalidade
- `NO_MODALIDADE` (VARCHAR(100)) - Nome da modalidade
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**RL_PROCEDIMENTO_MODALIDADE**:
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_MODALIDADE` (VARCHAR(2)) - Código da modalidade (FK)
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Status**: ✅ Concluído

---

### 8. Instrumento de Registro

**Objetivo**: Identificar tabela e estrutura relacionada a instrumentos de registro.

**Termos de busca sugeridos**: INSTRUMENTO, REGISTRO, INSTRUMENTO_REGISTRO, TB_INSTRUMENTO

**Resultado da busca**: Nenhuma tabela encontrada com os termos de busca.

**Status**: ❌ Não Encontrado

---

### 9. Detalhes

**Objetivo**: Identificar tabela e estrutura relacionada a detalhes de procedimentos ou outras entidades.

**Termos de busca sugeridos**: DETALHE, DETALHES, TB_DETALHE, PROCEDIMENTO_DETALHE

**Tabelas identificadas**:
- ✅ `TB_DETALHE` - Tabela principal de detalhes
- ✅ `RL_PROCEDIMENTO_DETALHE` - Tabela de relacionamento entre Procedimento e Detalhe
- ✅ `TB_DESCRICAO_DETALHE` - Tabela de descrição de detalhes

**Status**: ✅ Concluído

---

### 10. Incremento

**Objetivo**: Identificar tabela e estrutura relacionada a incrementos de procedimentos.

**Termos de busca sugeridos**: INCREMENTO, INCREMENT, TB_INCREMENTO, PROCEDIMENTO_INCREMENTO

**Tabelas identificadas**:
- ✅ `RL_PROCEDIMENTO_INCREMENTO` - Tabela de relacionamento entre Procedimento e Incremento

**Estrutura identificada** (baseada no layout do DATASUS):
- `CO_PROCEDIMENTO` (VARCHAR(10)) - Código do procedimento (FK)
- `CO_HABILITACAO` (VARCHAR(4)) - Código da habilitação (FK)
- `VL_PERCENTUAL_SH` (NUMERIC(7)) - Valor percentual Serviço Hospitalar
- `VL_PERCENTUAL_SA` (NUMERIC(7)) - Valor percentual Serviço Ambulatorial
- `VL_PERCENTUAL_SP` (NUMERIC(7)) - Valor percentual Serviço Profissional
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência

**Status**: ✅ Concluído

---

### 11. Descrição

**Objetivo**: Identificar se existe uma tabela específica para descrições ou se é um campo em outras tabelas.

**Termos de busca sugeridos**: DESCRICAO, DESCR, TB_DESCRICAO

**Resultado**: Não existe uma tabela específica para descrições. As descrições são campos em outras tabelas, seguindo o padrão `NO_*` (Nome).

**Campos de descrição identificados**:
- `TB_PROCEDIMENTO.NO_PROCEDIMENTO` (VARCHAR(250)) - Descrição do procedimento
- `TB_CID.NO_CID` (VARCHAR(100)) - Descrição do CID
- `TB_HABILITACAO.NO_HABILITACAO` (VARCHAR(150)) - Descrição da habilitação
- `TB_OCUPACAO.NO_OCUPACAO` (VARCHAR(150)) - Descrição da ocupação
- `TB_SERVICO.NO_SERVICO` (VARCHAR(120)) - Descrição do serviço
- `TB_MODALIDADE.NO_MODALIDADE` (VARCHAR(100)) - Descrição da modalidade
- `TB_TIPO_LEITO.NO_TIPO_LEITO` (VARCHAR(60)) - Descrição do tipo de leito
- E outros campos `NO_*` em diversas tabelas

**Status**: ✅ Concluído

---

## 🛠️ Ferramentas Utilizadas

1. **isql.exe** (Firebird Interactive SQL) - Linha de comando
2. **PowerShell** - Automação de execução
3. **Scripts SQL** - Consultas de metadados do Firebird
4. **Codebase Search** - Busca semântica no código-fonte

---

## 📚 Scripts SQL Base

### Script 1: Listar Todas as Tabelas
```sql
-- Arquivo: verificar_todas_tabelas.sql
SELECT RF.RDB$RELATION_NAME AS TABELA
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
ORDER BY RF.RDB$RELATION_NAME;
```

### Script 2: Buscar Tabelas por Termo
```sql
-- Arquivo: buscar_tabelas_por_termo.sql
-- Substituir 'TERMO' pelo termo de busca
SELECT RF.RDB$RELATION_NAME AS TABELA
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
  AND RF.RDB$RELATION_NAME CONTAINING 'TERMO'
ORDER BY RF.RDB$RELATION_NAME;
```

### Script 3: Verificar Estrutura de Tabela
```sql
-- Arquivo: verificar_estrutura_tabela.sql
-- Substituir 'NOME_DA_TABELA' pelo nome da tabela
SELECT 
    RF.RDB$FIELD_NAME AS CAMPO,
    RF.RDB$FIELD_SOURCE AS TIPO_ORIGEM,
    CASE 
        WHEN F.RDB$FIELD_TYPE = 7 THEN 'SMALLINT'
        WHEN F.RDB$FIELD_TYPE = 8 THEN 'INTEGER'
        WHEN F.RDB$FIELD_TYPE = 10 THEN 'FLOAT'
        WHEN F.RDB$FIELD_TYPE = 12 THEN 'DATE'
        WHEN F.RDB$FIELD_TYPE = 13 THEN 'TIME'
        WHEN F.RDB$FIELD_TYPE = 14 THEN 'CHAR'
        WHEN F.RDB$FIELD_TYPE = 16 THEN 'BIGINT'
        WHEN F.RDB$FIELD_TYPE = 27 THEN 'DOUBLE PRECISION'
        WHEN F.RDB$FIELD_TYPE = 35 THEN 'TIMESTAMP'
        WHEN F.RDB$FIELD_TYPE = 37 THEN 'VARCHAR'
        WHEN F.RDB$FIELD_TYPE = 261 THEN 'BLOB'
        ELSE 'OUTRO'
    END AS TIPO,
    F.RDB$FIELD_LENGTH AS TAMANHO,
    CASE WHEN RF.RDB$NULL_FLAG = 1 THEN 'NOT NULL' ELSE 'NULL' END AS NULLABLE
FROM RDB$RELATION_FIELDS RF
LEFT JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
WHERE RF.RDB$RELATION_NAME = 'NOME_DA_TABELA'
ORDER BY RF.RDB$FIELD_POSITION;
```

### Script 4: Verificar Dados Existentes
```sql
-- Arquivo: ver_dados_tabela.sql
-- Substituir 'NOME_DA_TABELA' pelo nome da tabela
SELECT COUNT(*) AS TOTAL_REGISTROS
FROM NOME_DA_TABELA;

SELECT *
FROM NOME_DA_TABELA
ROWS 10;
```

---

## 📋 Template de Documentação Individual

Para cada item identificado, criar um arquivo seguindo o padrão:

**Arquivo**: `PROCESSO_IDENTIFICACAO_[NOME_ENTIDADE].md`

**Estrutura**:
1. Objetivo
2. Passo 1: Análise do Código Existente
3. Passo 2: Busca por Referências no Banco de Dados
4. Passo 3: Análise da Estrutura da Tabela
5. Estrutura Final Identificada
6. Conclusões
7. Ferramentas Utilizadas
8. Scripts Criados
9. Próximos Passos (Implementação)

---

## 🎯 Próximos Passos

1. ✅ Executar busca no código-fonte para cada item
2. ✅ Executar scripts SQL para identificar tabelas
3. ✅ Analisar estrutura de cada tabela encontrada
4. ⏳ Criar documentos individuais detalhados (seguindo padrão de `PROCESSO_IDENTIFICACAO_TB_PROCOMUNS.md`) para cada item identificado
5. ✅ Atualizar este checklist com os resultados

## 📊 Resumo da Identificação

**Total de itens**: 11
- ✅ **Concluídos**: 10 itens
- ❌ **Não Encontrados**: 1 item (Instrumento de Registro)

**Tabelas identificadas**:
- 20+ tabelas principais e de relacionamento identificadas
- Padrão identificado: Tabelas principais (`TB_*`) e tabelas de relacionamento (`RL_*`)

**Data da Identificação**: 19/11/2024

---

**Data de Criação**: 2024
**Autor**: Processo automatizado de análise
**Versão do Banco**: Firebird 3.0

