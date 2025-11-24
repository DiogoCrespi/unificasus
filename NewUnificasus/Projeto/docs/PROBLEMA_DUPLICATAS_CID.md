# Problema: Duplicatas em RL_PROCEDIMENTO_CID

## 🔍 Problema Identificado

Durante a importação da competência **10/2025**, estão aparecendo CIDs duplicados que pertencem à competência **06/2020**. 

### Exemplos de Duplicatas Encontradas:
- **BLOQUEIO ATRIOVENTRICULA** aparece múltiplas vezes
- Alguns registros com encoding corrompido: "fascÃculo" em vez de "fascículo"
- Mesmo `CO_CID + CO_PROCEDIMENTO + DT_COMPETENCIA` aparecendo 3 vezes

## 📊 Análise da Estrutura

### Chave Primária Atual
```
RL_PROCEDIMENTO_CID:
- INDICE (INTEGER) - PRIMARY KEY (auto-incremento)
- CO_CID (VARCHAR(4))
- CO_PROCEDIMENTO (VARCHAR(10))
- DT_COMPETENCIA (VARCHAR(6))
- ST_PRINCIPAL (VARCHAR(1))
- NO_CID (VARCHAR(100))
```

**Problema**: A chave primária é apenas `INDICE`, **não há constraint de unicidade** em `CO_CID + CO_PROCEDIMENTO + DT_COMPETENCIA`.

### Resultados da Verificação

**Duplicatas encontradas na competência 202510**:
- Múltiplos registros com mesmo `CO_CID + CO_PROCEDIMENTO + DT_COMPETENCIA`
- Exemplo: `C498 + 0201010550 + 202510` aparece **3 vezes**
- Exemplo: `C450 + 0201010402 + 202510` aparece **3 vezes**

## 🔧 Causa Raiz

### 1. Identificação Incorreta de Chave Primária

O método `IdentifyPrimaryKeys` em `ImportRepository.cs` usa heurísticas para identificar chaves primárias:

```csharp
// Heurística 1: Para tabelas relacionais (RL_*), geralmente tem PK composta
// Ex: RL_PROCEDIMENTO_CID: CO_PROCEDIMENTO + CO_CID
if (coColumns.Count >= 2)
{
    var firstCoColumns = columns
        .Where(c => c.ColumnName.StartsWith("CO_", StringComparison.OrdinalIgnoreCase))
        .Take(3) // Máximo 3 colunas para PK composta
        .Select(c => c.ColumnName)
        .ToList();
    
    if (firstCoColumns.Any())
    {
        primaryKeys.AddRange(firstCoColumns);
        return primaryKeys;
    }
}
```

**Problema**: A heurística identifica `CO_PROCEDIMENTO + CO_CID` como chave primária, mas **não inclui `DT_COMPETENCIA`**, que é essencial para evitar duplicatas entre competências.

### 2. Falta de Constraint de Unicidade no Banco

O banco **não possui** uma constraint UNIQUE em `(CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA)`, permitindo que o mesmo relacionamento seja inserido múltiplas vezes.

### 3. Modo de Tratamento de Duplicatas

O `InsertOrUpdateAsync` usa `DuplicateHandlingMode.Update`, mas como a chave primária identificada está incorreta, ele não detecta duplicatas corretamente.

## 🎯 Soluções Propostas

### Solução 1: Corrigir Identificação de Chave Primária (Imediato)

Modificar `IdentifyPrimaryKeys` para incluir `DT_COMPETENCIA` em tabelas relacionais:

```csharp
// Para RL_PROCEDIMENTO_CID, a chave lógica é: CO_PROCEDIMENTO + CO_CID + DT_COMPETENCIA
if (tableName.StartsWith("RL_", StringComparison.OrdinalIgnoreCase))
{
    // Verifica se tem DT_COMPETENCIA
    var dtCompetenciaColumn = columns.FirstOrDefault(c => 
        c.ColumnName.Equals("DT_COMPETENCIA", StringComparison.OrdinalIgnoreCase));
    
    if (dtCompetenciaColumn != null)
    {
        // Para tabelas relacionais com competência, inclui DT_COMPETENCIA na chave
        var coColumns = columns
            .Where(c => c.ColumnName.StartsWith("CO_", StringComparison.OrdinalIgnoreCase))
            .Take(2) // CO_PROCEDIMENTO + CO_CID
            .Select(c => c.ColumnName)
            .ToList();
        
        coColumns.Add("DT_COMPETENCIA");
        return coColumns;
    }
}
```

### Solução 2: Criar Constraint UNIQUE no Banco (Recomendado)

Criar uma constraint UNIQUE para evitar duplicatas no nível do banco:

```sql
-- Criar índice único para evitar duplicatas
CREATE UNIQUE INDEX IDX_RL_PCID_UNIQUE 
ON RL_PROCEDIMENTO_CID (CO_PROCEDIMENTO, CO_CID, DT_COMPETENCIA);
```

**Vantagens**:
- Previne duplicatas no nível do banco
- Funciona mesmo se o código tiver bugs
- Melhora performance de buscas

**Desvantagens**:
- Requer acesso ao banco de produção
- Pode falhar se já houver duplicatas existentes

### Solução 3: Limpar Duplicatas Existentes

Antes de criar a constraint, remover duplicatas existentes:

```sql
-- Remover duplicatas, mantendo apenas o primeiro registro (menor INDICE)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE NOT IN (
    SELECT MIN(INDICE)
    FROM RL_PROCEDIMENTO_CID
    GROUP BY CO_PROCEDIMENTO, CO_CID, DT_COMPETENCIA
);
```

## 📝 Plano de Ação

1. ✅ **Verificar duplicatas** - CONCLUÍDO
2. ⏳ **Corrigir `IdentifyPrimaryKeys`** - Adicionar `DT_COMPETENCIA` para tabelas relacionais
3. ⏳ **Criar script de limpeza** - Remover duplicatas existentes
4. ⏳ **Criar constraint UNIQUE** - Prevenir futuras duplicatas
5. ⏳ **Validar importação** - Testar com competência de teste

## 🔍 Verificação Adicional Necessária

Verificar se o problema de encoding (UPPER vs não-UPPER) está causando duplicatas:

```sql
-- Verificar se há registros com mesmo conteúdo mas encoding diferente
SELECT 
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL,
    LIST(DISTINCT NO_CID, ' | ') AS NOMES_DIFERENTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1;
```

## ⚠️ Impacto

- **Dados corrompidos**: Duplicatas na competência 202510
- **Performance**: Queries mais lentas devido a duplicatas
- **Integridade**: Dados inconsistentes entre competências
- **Usuário**: CIDs aparecem duplicados na interface

