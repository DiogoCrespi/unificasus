-- Script para limpar duplicatas em RL_PROCEDIMENTO_CID
-- ESTRATÉGIA: Preservar linhas em MAIÚSCULAS (sistema antigo) e remover duplicatas em minúsculas/misturadas (sistema novo)

-- 1. Verificar quantas duplicatas existem antes da limpeza
SELECT 
    'ANTES DA LIMPEZA' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 2. Analisar duplicatas: quantas estão em MAIÚSCULAS vs minúsculas/misturadas
SELECT 
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS,
    SUM(CASE WHEN UPPER(TRIM(NO_CID)) = TRIM(NO_CID) THEN 1 ELSE 0 END) AS TOTAL_MAIUSCULAS,
    SUM(CASE WHEN UPPER(TRIM(NO_CID)) <> TRIM(NO_CID) THEN 1 ELSE 0 END) AS TOTAL_MINUSCULAS_MISTURADAS,
    MIN(CASE WHEN UPPER(TRIM(NO_CID)) = TRIM(NO_CID) THEN INDICE ELSE NULL END) AS INDICE_MAIUSCULA,
    MIN(INDICE) AS INDICE_PRIMEIRO
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 3. Listar exemplos de duplicatas (primeiras 20)
SELECT 
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    pc.NO_CID,
    CASE 
        WHEN UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID) THEN 'MAIÚSCULAS (ANTIGO)'
        ELSE 'Minúsculas/Misturadas (NOVO)'
    END AS TIPO,
    (SELECT COUNT(*) 
     FROM RL_PROCEDIMENTO_CID pc2 
     WHERE pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
       AND pc2.CO_CID = pc.CO_CID
       AND pc2.DT_COMPETENCIA = pc.DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '202510'
  AND EXISTS (
      SELECT 1 
      FROM RL_PROCEDIMENTO_CID pc3
      WHERE pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
        AND pc3.CO_CID = pc.CO_CID
        AND pc3.DT_COMPETENCIA = pc.DT_COMPETENCIA
        AND pc3.DT_COMPETENCIA = '202510'
        AND pc3.INDICE <> pc.INDICE
  )
ORDER BY pc.CO_CID, pc.CO_PROCEDIMENTO, 
         CASE WHEN UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID) THEN 0 ELSE 1 END,
         pc.INDICE
ROWS 20;

-- 4. REMOVER DUPLICATAS (preservar MAIÚSCULAS - sistema antigo)
-- Estratégia: 
--   - Se existe linha em MAIÚSCULAS: mantém apenas a primeira linha em MAIÚSCULAS (menor INDICE)
--   - Se não existe linha em MAIÚSCULAS: mantém apenas a primeira linha (menor INDICE)
-- ATENÇÃO: Execute apenas após validar os resultados acima!
-- Descomente as linhas abaixo para executar a remoção:

/*
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      -- Para cada combinação única de CO_PROCEDIMENTO + CO_CID + DT_COMPETENCIA,
      -- mantém apenas uma linha seguindo a prioridade:
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID pc
      WHERE pc.DT_COMPETENCIA = '202510'
        AND (
            -- Prioridade 1: Se existe linha em MAIÚSCULAS, mantém a primeira em MAIÚSCULAS
            (UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID)
             AND EXISTS (
                 SELECT 1 
                 FROM RL_PROCEDIMENTO_CID pc2
                 WHERE pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                   AND pc2.CO_CID = pc.CO_CID
                   AND pc2.DT_COMPETENCIA = pc.DT_COMPETENCIA
                   AND pc2.DT_COMPETENCIA = '202510'
                   AND UPPER(TRIM(pc2.NO_CID)) = TRIM(pc2.NO_CID)
             ))
            OR
            -- Prioridade 2: Se não existe linha em MAIÚSCULAS, mantém a primeira linha (menor INDICE)
            (NOT EXISTS (
                SELECT 1 
                FROM RL_PROCEDIMENTO_CID pc3
                WHERE pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                  AND pc3.CO_CID = pc.CO_CID
                  AND pc3.DT_COMPETENCIA = pc.DT_COMPETENCIA
                  AND pc3.DT_COMPETENCIA = '202510'
                  AND UPPER(TRIM(pc3.NO_CID)) = TRIM(pc3.NO_CID)
            ))
        )
      GROUP BY pc.CO_PROCEDIMENTO, pc.CO_CID, pc.DT_COMPETENCIA
  );
*/

-- 5. Verificar resultado após limpeza
SELECT 
    'APOS LIMPEZA' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS,
    SUM(CASE WHEN UPPER(TRIM(NO_CID)) = TRIM(NO_CID) THEN 1 ELSE 0 END) AS TOTAL_MAIUSCULAS_PRESERVADAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 6. Verificar se ainda há duplicatas após limpeza
SELECT 
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS_RESTANTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1;

