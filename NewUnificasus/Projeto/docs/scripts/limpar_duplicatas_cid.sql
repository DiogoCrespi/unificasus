-- Script para limpar duplicatas em RL_PROCEDIMENTO_CID
-- Mantém apenas o primeiro registro (menor INDICE) para cada combinação única
-- de CO_PROCEDIMENTO + CO_CID + DT_COMPETENCIA

-- 1. Verificar quantas duplicatas existem antes da limpeza
SELECT 
    'ANTES DA LIMPEZA' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 2. Listar duplicatas que serão removidas (apenas visualização)
SELECT 
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS,
    MIN(INDICE) AS INDICE_MANTIDO,
    LIST(INDICE, ', ') AS INDICES_REMOVIDOS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 3. REMOVER DUPLICATAS (priorizar linhas em MAIÚSCULAS - sistema antigo)
-- Estratégia: Manter linhas onde NO_CID está em MAIÚSCULAS (sistema antigo)
-- Remover apenas duplicatas em minúsculas/misturadas (sistema novo)
-- ATENÇÃO: Execute apenas após validar os resultados acima!
-- Descomente as linhas abaixo para executar a remoção:

/*
-- Passo 1: Identificar duplicatas e manter preferencialmente as em MAIÚSCULAS
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      -- Subquery: Para cada combinação única, mantém:
      -- 1. Se houver linha em MAIÚSCULAS, mantém a primeira em MAIÚSCULAS (menor INDICE)
      -- 2. Se não houver linha em MAIÚSCULAS, mantém a primeira linha (menor INDICE)
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID pc2
      WHERE pc2.DT_COMPETENCIA = '202510'
        AND (
            -- Caso 1: Existe linha em MAIÚSCULAS para esta combinação
            (EXISTS (
                SELECT 1 
                FROM RL_PROCEDIMENTO_CID pc3
                WHERE pc3.CO_PROCEDIMENTO = pc2.CO_PROCEDIMENTO
                  AND pc3.CO_CID = pc2.CO_CID
                  AND pc3.DT_COMPETENCIA = pc2.DT_COMPETENCIA
                  AND pc3.DT_COMPETENCIA = '202510'
                  AND UPPER(TRIM(pc3.NO_CID)) = TRIM(pc3.NO_CID)  -- Está em MAIÚSCULAS
            ) AND UPPER(TRIM(pc2.NO_CID)) = TRIM(pc2.NO_CID))  -- Esta linha está em MAIÚSCULAS
            OR
            -- Caso 2: Não existe linha em MAIÚSCULAS, mantém a primeira (menor INDICE)
            (NOT EXISTS (
                SELECT 1 
                FROM RL_PROCEDIMENTO_CID pc4
                WHERE pc4.CO_PROCEDIMENTO = pc2.CO_PROCEDIMENTO
                  AND pc4.CO_CID = pc2.CO_CID
                  AND pc4.DT_COMPETENCIA = pc2.DT_COMPETENCIA
                  AND pc4.DT_COMPETENCIA = '202510'
                  AND UPPER(TRIM(pc4.NO_CID)) = TRIM(pc4.NO_CID)  -- Está em MAIÚSCULAS
            ))
        )
      GROUP BY pc2.CO_PROCEDIMENTO, pc2.CO_CID, pc2.DT_COMPETENCIA
  );
*/

-- 4. Verificar resultado após limpeza
SELECT 
    'APOS LIMPEZA' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 5. Criar índice único para prevenir futuras duplicatas
-- ATENÇÃO: Execute apenas após limpar duplicatas existentes!
-- Descomente as linhas abaixo para criar o índice:

/*
CREATE UNIQUE INDEX IDX_RL_PCID_UNIQUE 
ON RL_PROCEDIMENTO_CID (CO_PROCEDIMENTO, CO_CID, DT_COMPETENCIA);
*/

