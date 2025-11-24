-- ============================================================================
-- Script INTELIGENTE para DELETAR duplicatas de CID10
-- Estratégia: Mantém a melhor descrição (preferindo MAIÚSCULAS ou mais completa)
-- Processa TODAS as duplicatas de uma vez
-- ============================================================================

-- 1. VERIFICAR total de duplicatas ANTES
SELECT 
    'ANTES - Total duplicatas' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 2. DELETAR duplicatas - mantém a melhor descrição
-- Prioridade: 1) MAIÚSCULAS (sistema antigo), 2) Maior tamanho (mais completa), 3) Menor INDICE
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      SELECT 
          MIN(
              CASE 
                  -- Prioridade 1: Se tem MAIÚSCULAS, pega o menor INDICE entre elas
                  WHEN EXISTS (
                      SELECT 1 FROM RL_PROCEDIMENTO_CID pc2
                      WHERE pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                        AND pc2.CO_CID = pc.CO_CID
                        AND pc2.DT_COMPETENCIA = pc.DT_COMPETENCIA
                        AND pc2.DT_COMPETENCIA = '202510'
                        AND UPPER(TRIM(pc2.NO_CID)) = TRIM(pc2.NO_CID)
                  )
                  THEN CASE 
                      WHEN UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID) THEN pc.INDICE
                      ELSE 999999999
                  END
                  -- Prioridade 2: Se não tem MAIÚSCULAS, pega o com maior descrição (mais completa)
                  ELSE pc.INDICE
              END
          )
      FROM RL_PROCEDIMENTO_CID pc
      WHERE pc.DT_COMPETENCIA = '202510'
        AND (
            -- Se tem MAIÚSCULAS, mantém a primeira MAIÚSCULA
            (UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID)
             AND pc.INDICE = (
                 SELECT MIN(pc3.INDICE)
                 FROM RL_PROCEDIMENTO_CID pc3
                 WHERE pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                   AND pc3.CO_CID = pc.CO_CID
                   AND pc3.DT_COMPETENCIA = pc.DT_COMPETENCIA
                   AND pc3.DT_COMPETENCIA = '202510'
                   AND UPPER(TRIM(pc3.NO_CID)) = TRIM(pc3.NO_CID)
             ))
            OR
            -- Se não tem MAIÚSCULAS, mantém a com maior descrição
            (NOT EXISTS (
                SELECT 1 FROM RL_PROCEDIMENTO_CID pc4
                WHERE pc4.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                  AND pc4.CO_CID = pc.CO_CID
                  AND pc4.DT_COMPETENCIA = pc.DT_COMPETENCIA
                  AND pc4.DT_COMPETENCIA = '202510'
                  AND UPPER(TRIM(pc4.NO_CID)) = TRIM(pc4.NO_CID)
            )
            AND pc.INDICE = (
                SELECT MIN(pc5.INDICE)
                FROM RL_PROCEDIMENTO_CID pc5
                WHERE pc5.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                  AND pc5.CO_CID = pc.CO_CID
                  AND pc5.DT_COMPETENCIA = pc.DT_COMPETENCIA
                  AND pc5.DT_COMPETENCIA = '202510'
                  AND LENGTH(TRIM(pc5.NO_CID)) = (
                      SELECT MAX(LENGTH(TRIM(pc6.NO_CID)))
                      FROM RL_PROCEDIMENTO_CID pc6
                      WHERE pc6.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
                        AND pc6.CO_CID = pc.CO_CID
                        AND pc6.DT_COMPETENCIA = pc.DT_COMPETENCIA
                        AND pc6.DT_COMPETENCIA = '202510'
                  )
            ))
        )
      GROUP BY pc.CO_PROCEDIMENTO, pc.CO_CID, pc.DT_COMPETENCIA
  );

-- 3. VERIFICAR após deleção
SELECT 
    'DEPOIS - Verificacao' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS_RESTANTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

