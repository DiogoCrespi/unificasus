# Estrutura de Dados - UnificaSUS

## 📊 Visão Geral do Banco de Dados

Este documento descreve a estrutura completa do banco de dados Firebird que a aplicação utiliza.

### Configuração Atual
- **Banco**: Firebird 5.0
- **ODS**: 13.1
- **Charset**: WIN1252
- **Dialect**: 3
- **Arquivo**: `UNIFICASUS.GDB`

## 🗂️ Estrutura Hierárquica Principal

### Navegação de Dados

```
TB_GRUPO (01-08)
  └─ TB_SUB_GRUPO (01-99)
      └─ TB_FORMA_ORGANIZACAO (01-99)
          └─ TB_PROCEDIMENTO (código 10 dígitos)
```

### Exemplo Real da Estrutura

```
01 - Ações de promoção e prevenção em saúde
  └─ 01 - Ações coletivas/individuais em saúde
      └─ 01 - Educação em saúde
          ├─ 0101010010 - ATIVIDADE EDUCATIVA / ORIENTAÇÃO EM GRUPO NA ATENÇÃO BÁSICA
          ├─ 0101010028 - ATIVIDADE EDUCATIVA / ORIENTAÇÃO EM GRUPO NA ATENÇÃO ESPECIALIZADA
          └─ 0101010036 - PRÁTICA CORPORAL / ATIVIDADE FÍSICA EM GRUPO
```

## 📋 Tabelas Principais

### 1. TB_PROCEDIMENTO
**Tabela principal de procedimentos do SUS**

**Estrutura**:
```sql
CREATE TABLE TB_PROCEDIMENTO (
    CO_PROCEDIMENTO VARCHAR(10) NOT NULL PRIMARY KEY,  -- Código do procedimento (ex: 0101010010)
    NO_PROCEDIMENTO VARCHAR(250),                      -- Nome/descrição do procedimento
    TP_COMPLEXIDADE VARCHAR(1),                        -- Tipo de complexidade (AB, AP, AM, AA)
    TP_SEXO VARCHAR(1),                                -- Sexo (M, F, I, null)
    QT_MAXIMA_EXECUCAO INTEGER,                        -- Quantidade máxima de execução
    QT_DIAS_PERMANENCIA INTEGER,                       -- Dias de permanência
    QT_PONTOS INTEGER,                                 -- Pontos
    VL_IDADE_MINIMA INTEGER,                           -- Idade mínima
    VL_IDADE_MAXIMA INTEGER,                           -- Idade máxima
    VL_SH NUMERIC(10,2),                               -- Valor Serviço Hospitalar
    VL_SA NUMERIC(10,2),                               -- Valor Serviço Ambulatorial
    VL_SP NUMERIC(10,2),                               -- Valor Serviço Profissional
    CO_FINANCIAMENTO VARCHAR(2),                       -- FK para TB_FINANCIAMENTO
    CO_RUBRICA VARCHAR(6),                             -- FK para TB_RUBRICA
    QT_TEMPO_PERMANENCIA INTEGER,                      -- Tempo de permanência
    DT_COMPETENCIA VARCHAR(6)                          -- Data de competência (AAAAMM)
);
```

**Exemplo de Dados**:
- `CO_PROCEDIMENTO`: "0101010010"
- `NO_PROCEDIMENTO`: "ATIVIDADE EDUCATIVA / ORIENTAÇÃO EM GRUPO NA ATENÇÃO BÁSICA"
- `VL_SA`: 0.00
- `VL_SH`: 0.00
- `VL_SP`: 0.00
- `QT_TEMPO_PERMANENCIA`: 9999
- `TP_COMPLEXIDADE`: "AB" (Atenção Básica)
- `CO_FINANCIAMENTO`: "01" (ATENÇÃO BÁSICA - PAB)

### 2. TB_GRUPO
**Grupos principais de procedimentos**

**Estrutura**:
```sql
CREATE TABLE TB_GRUPO (
    CO_GRUPO VARCHAR(2) NOT NULL PRIMARY KEY,          -- Código do grupo (01-08)
    NO_GRUPO VARCHAR(100),                             -- Nome do grupo
    DT_COMPETENCIA VARCHAR(6)                          -- Data de competência
);
```

**Grupos Principais**:
- **01**: Ações de promoção e prevenção em saúde
- **02**: Procedimentos com finalidade diagnóstica
- **03**: Procedimentos clínicos
- **04**: Procedimentos Cirúrgicos
- **05**: Transplantes de órgãos, tecidos e células
- **06**: Medicamentos
- **07**: Órteses, próteses e materiais especiais
- **08**: Ações complementares da atenção à saúde

### 3. TB_SUB_GRUPO
**Sub-grupos dentro de cada grupo**

**Estrutura**:
```sql
CREATE TABLE TB_SUB_GRUPO (
    CO_GRUPO VARCHAR(2) NOT NULL,                      -- FK para TB_GRUPO
    CO_SUB_GRUPO VARCHAR(2) NOT NULL,                  -- Código do sub-grupo (01-99)
    NO_SUB_GRUPO VARCHAR(100),                         -- Nome do sub-grupo
    DT_COMPETENCIA VARCHAR(6),                         -- Data de competência
    PRIMARY KEY (CO_GRUPO, CO_SUB_GRUPO)
);
```

**Exemplos**:
- Grupo **01**:
  - **01**: Ações coletivas/individuais em saúde
  - **02**: Vigilância em saúde
  - etc.

### 4. TB_FORMA_ORGANIZACAO
**Formas de organização dentro de cada sub-grupo**

**Estrutura**:
```sql
CREATE TABLE TB_FORMA_ORGANIZACAO (
    CO_GRUPO VARCHAR(2) NOT NULL,                      -- FK para TB_GRUPO
    CO_SUB_GRUPO VARCHAR(2) NOT NULL,                  -- FK para TB_SUB_GRUPO
    CO_FORMA_ORGANIZACAO VARCHAR(2) NOT NULL,          -- Código da forma (01-99)
    NO_FORMA_ORGANIZACAO VARCHAR(100),                 -- Nome da forma
    DT_COMPETENCIA VARCHAR(6),                         -- Data de competência
    PRIMARY KEY (CO_GRUPO, CO_SUB_GRUPO, CO_FORMA_ORGANIZACAO)
);
```

**Exemplos**:
- Grupo **01**, Sub-grupo **01**:
  - **01**: Educação em saúde
  - **02**: Saúde bucal
  - **03**: Visita domiciliar
  - **04**: Alimentação e nutrição

### 5. TB_COMPETENCIA_ATIVA
**Controla qual competência está ativa no sistema**

**Estrutura**:
```sql
CREATE TABLE TB_COMPETENCIA_ATIVA (
    DT_COMPETENCIA VARCHAR(6) NOT NULL PRIMARY KEY,    -- Competência ativa (AAAAMM)
    DT_ATIVACAO TIMESTAMP,                             -- Data/hora de ativação
    ST_ATIVA CHAR(1) DEFAULT 'S'                       -- Status (S/N)
);
```

**Importante**: Apenas uma competência deve ter `ST_ATIVA = 'S'` por vez.

### 6. TB_FINANCIAMENTO
**Tipos de financiamento**

**Estrutura**:
```sql
CREATE TABLE TB_FINANCIAMENTO (
    CO_FINANCIAMENTO VARCHAR(2) NOT NULL PRIMARY KEY,  -- Código (01, 02, etc.)
    NO_FINANCIAMENTO VARCHAR(100),                     -- Nome (ex: "ATENÇÃO BÁSICA (PAB)")
    DT_COMPETENCIA VARCHAR(6)
);
```

**Exemplos**:
- **01**: ATENÇÃO BÁSICA (PAB)
- **02**: MÉDIA COMPLEXIDADE
- **03**: ALTA COMPLEXIDADE
- etc.

## 🔗 Relacionamentos Principais

### Relacionamento Procedimento → Grupo

**Não há relação direta** - A relação é feita através do código:
- `CO_PROCEDIMENTO`: "0101010010"
  - Primeiros 2 dígitos (01) = `CO_GRUPO`
  - Próximos 2 dígitos (01) = `CO_SUB_GRUPO`
  - Próximos 2 dígitos (01) = `CO_FORMA_ORGANIZACAO`
  - Últimos 4 dígitos (0010) = Código específico do procedimento

### Tabelas Relacionais (RL_*)

#### RL_PROCEDIMENTO_CID
**Relaciona procedimentos com CID (Classificação Internacional de Doenças)**

```sql
CREATE TABLE RL_PROCEDIMENTO_CID (
    CO_PROCEDIMENTO VARCHAR(10) NOT NULL,              -- FK para TB_PROCEDIMENTO
    CO_CID VARCHAR(4) NOT NULL,                        -- FK para TB_CID
    ST_PRINCIPAL CHAR(1),                              -- Principal (S/N)
    DT_COMPETENCIA VARCHAR(6),
    PRIMARY KEY (CO_PROCEDIMENTO, CO_CID)
);
```

#### RL_PROCEDIMENTO_SERVICO
**Relaciona procedimentos com serviços**

```sql
CREATE TABLE RL_PROCEDIMENTO_SERVICO (
    CO_PROCEDIMENTO VARCHAR(10) NOT NULL,              -- FK para TB_PROCEDIMENTO
    CO_SERVICO VARCHAR(3) NOT NULL,                    -- FK para TB_SERVICO
    CO_CLASSIFICACAO VARCHAR(3) NOT NULL,              -- FK para TB_SERVICO_CLASSIFICACAO
    DT_COMPETENCIA VARCHAR(6),
    PRIMARY KEY (CO_PROCEDIMENTO, CO_SERVICO, CO_CLASSIFICACAO)
);
```

#### RL_PROCEDIMENTO_MODALIDADE
**Relaciona procedimentos com modalidades**

```sql
CREATE TABLE RL_PROCEDIMENTO_MODALIDADE (
    CO_PROCEDIMENTO VARCHAR(10) NOT NULL,              -- FK para TB_PROCEDIMENTO
    CO_MODALIDADE VARCHAR(2) NOT NULL,                 -- FK para TB_MODALIDADE
    DT_COMPETENCIA VARCHAR(6),
    PRIMARY KEY (CO_PROCEDIMENTO, CO_MODALIDADE)
);
```

## 📝 Queries Importantes

### 1. Buscar Procedimentos por Grupo/Sub-grupo/Forma de Organização

```sql
SELECT 
    pr.*
FROM TB_PROCEDIMENTO pr
WHERE pr.DT_COMPETENCIA = :competencia
  AND SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 2) = :coGrupo
  AND SUBSTRING(pr.CO_PROCEDIMENTO FROM 3 FOR 2) = :coSubGrupo
  AND SUBSTRING(pr.CO_PROCEDIMENTO FROM 5 FOR 2) = :coFormaOrganizacao
ORDER BY pr.CO_PROCEDIMENTO
```

### 2. Buscar Procedimentos com Detalhes de Financiamento

```sql
SELECT 
    pr.CO_PROCEDIMENTO,
    pr.NO_PROCEDIMENTO,
    pr.VL_SA,
    pr.VL_SH,
    pr.VL_SP,
    f.NO_FINANCIAMENTO
FROM TB_PROCEDIMENTO pr
LEFT JOIN TB_FINANCIAMENTO f ON pr.CO_FINANCIAMENTO = f.CO_FINANCIAMENTO
WHERE pr.DT_COMPETENCIA = :competencia
  AND pr.CO_PROCEDIMENTO LIKE :codigoInicial || '%'
ORDER BY pr.CO_PROCEDIMENTO
```

### 3. Buscar Procedimentos por CID

```sql
SELECT DISTINCT
    pr.*
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_CID pc ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
WHERE pc.DT_COMPETENCIA = :competencia
  AND pc.CO_CID = :cid
ORDER BY pr.CO_PROCEDIMENTO
```

### 4. Buscar Procedimentos por Serviço

```sql
SELECT DISTINCT
    pr.*
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_SERVICO ps ON pr.CO_PROCEDIMENTO = ps.CO_PROCEDIMENTO
WHERE ps.DT_COMPETENCIA = :competencia
  AND ps.CO_SERVICO = :servico
ORDER BY pr.CO_PROCEDIMENTO
```

### 5. Buscar Estrutura Hierárquica Completa

```sql
-- Buscar grupos
SELECT * FROM TB_GRUPO WHERE DT_COMPETENCIA = :competencia ORDER BY CO_GRUPO;

-- Buscar sub-grupos de um grupo
SELECT * FROM TB_SUB_GRUPO 
WHERE CO_GRUPO = :coGrupo AND DT_COMPETENCIA = :competencia 
ORDER BY CO_SUB_GRUPO;

-- Buscar formas de organização de um sub-grupo
SELECT * FROM TB_FORMA_ORGANIZACAO
WHERE CO_GRUPO = :coGrupo 
  AND CO_SUB_GRUPO = :coSubGrupo 
  AND DT_COMPETENCIA = :competencia
ORDER BY CO_FORMA_ORGANIZACAO;

-- Buscar procedimentos de uma forma de organização
SELECT * FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = :competencia
  AND SUBSTRING(CO_PROCEDIMENTO FROM 1 FOR 2) = :coGrupo
  AND SUBSTRING(CO_PROCEDIMENTO FROM 3 FOR 2) = :coSubGrupo
  AND SUBSTRING(CO_PROCEDIMENTO FROM 5 FOR 2) = :coFormaOrganizacao
ORDER BY CO_PROCEDIMENTO;
```

## 🎯 Estrutura de Código de Procedimento

### Formato do Código

**Formato**: `AABBCCDDDD`
- **AA** (2 dígitos): Código do Grupo (01-08)
- **BB** (2 dígitos): Código do Sub-grupo (01-99)
- **CC** (2 dígitos): Código da Forma de Organização (01-99)
- **DDDD** (4 dígitos): Código específico do procedimento

### Exemplo

**Código**: `0101010010`
- **01**: Grupo - Ações de promoção e prevenção em saúde
- **01**: Sub-grupo - Ações coletivas/individuais em saúde
- **01**: Forma - Educação em saúde
- **0010**: Procedimento específico

## 📊 Campos Importantes

### Valores Monetários
- **VL_SA**: Valor Serviço Ambulatorial
- **VL_SH**: Valor Serviço Hospitalar
- **VL_SP**: Valor Serviço Profissional
- **VL_TA**: Valor T.A. (se houver)
- **VL_TH**: Valor T.H. (se houver)

### Tipo de Complexidade (TP_COMPLEXIDADE)
- **AB**: Atenção Básica
- **AP**: Atenção Primária
- **AM**: Atenção Média
- **AA**: Atenção Alta
- **null**: Não se aplica

### Tipo de Sexo (TP_SEXO)
- **M**: Masculino
- **F**: Feminino
- **I**: Indiferente
- **null**: Não se aplica

### Tempo de Permanência
- **QT_TEMPO_PERMANENCIA**: Tempo de permanência (geralmente 9999 quando não se aplica)
- **QT_DIAS_PERMANENCIA**: Dias de permanência (quando aplicável)

## 🔍 Índices Importantes

Para melhor performance, considerar índices em:
- `TB_PROCEDIMENTO.DT_COMPETENCIA`
- `TB_PROCEDIMENTO.CO_PROCEDIMENTO`
- `TB_GRUPO.DT_COMPETENCIA`
- `TB_SUB_GRUPO.DT_COMPETENCIA`
- `TB_FORMA_ORGANIZACAO.DT_COMPETENCIA`
- `RL_PROCEDIMENTO_CID.CO_CID`
- `RL_PROCEDIMENTO_CID.DT_COMPETENCIA`
- `RL_PROCEDIMENTO_SERVICO.CO_SERVICO`
- `RL_PROCEDIMENTO_SERVICO.DT_COMPETENCIA`

## 📋 Observações Importantes

1. **Competência**: O campo `DT_COMPETENCIA` está presente em quase todas as tabelas e é usado para versionamento dos dados
2. **Estrutura Hierárquica**: A relação é feita pelo código do procedimento, não por chaves estrangeiras diretas
3. **Valores NULL**: Muitos campos podem ser NULL (idade, sexo, etc.) - sempre verificar antes de usar
4. **Valores Padrão**: Alguns campos têm valores padrão quando não se aplicam:
   - `QT_TEMPO_PERMANENCIA`: 9999
   - `VL_SA/VL_SH/VL_SP`: 0.00 quando não há valor
5. **Códigos**: Os códigos são sempre strings, mesmo quando numéricos

---

**Última atualização**: 14/11/2024

