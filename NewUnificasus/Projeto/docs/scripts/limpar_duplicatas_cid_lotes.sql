-- Script para limpar duplicatas em RL_PROCEDIMENTO_CID em lotes pequenos
-- Processa 100 duplicatas por vez para melhor performance

-- 1. Verificar total de duplicatas
SELECT 
    'TOTAL DUPLICATAS' AS STATUS,
    COUNT(*) AS TOTAL_GRUPOS_DUPLICATAS
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);

-- 2. Remover duplicatas em lotes (primeiro lote de 100 grupos)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID po
      WHERE po.DT_COMPETENCIA = '202510'
        AND (
            (UPPER(TRIM(po.NO_CID)) = TRIM(po.NO_CID)
             AND EXISTS (
                 SELECT 1 
                 FROM RL_PROCEDIMENTO_CID po2
                 WHERE po2.CO_PROCEDIMENTO = po.CO_PROCEDIMENTO
                   AND po2.CO_CID = po.CO_CID
                   AND po2.DT_COMPETENCIA = po.DT_COMPETENCIA
                   AND po2.DT_COMPETENCIA = '202510'
                   AND UPPER(TRIM(po2.NO_CID)) = TRIM(po2.NO_CID)
             ))
            OR
            (NOT EXISTS (
                SELECT 1 
                FROM RL_PROCEDIMENTO_CID po3
                WHERE po3.CO_PROCEDIMENTO = po.CO_PROCEDIMENTO
                  AND po3.CO_CID = po.CO_CID
                  AND po3.DT_COMPETENCIA = po.DT_COMPETENCIA
                  AND po3.DT_COMPETENCIA = '202510'
                  AND UPPER(TRIM(po3.NO_CID)) = TRIM(po3.NO_CID)
            ))
        )
        AND (po.CO_CID, po.CO_PROCEDIMENTO, po.DT_COMPETENCIA) IN (
            SELECT FIRST 100 CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
            FROM RL_PROCEDIMENTO_CID
            WHERE DT_COMPETENCIA = '202510'
            GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
            HAVING COUNT(*) > 1
        )
      GROUP BY po.CO_PROCEDIMENTO, po.CO_CID, po.DT_COMPETENCIA
  );

-- 3. Verificar progresso após primeiro lote
SELECT 
    'APOS LOTE 1' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS_RESTANTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

