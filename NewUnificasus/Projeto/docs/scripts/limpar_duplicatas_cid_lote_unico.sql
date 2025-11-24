-- Script para limpar duplicatas em RL_PROCEDIMENTO_CID - Processa um grupo de duplicatas por vez
-- Execute este script múltiplas vezes até não haver mais duplicatas

-- 1. Identificar primeiro grupo de duplicatas
SELECT 
    FIRST 1
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 2. Remover duplicatas do primeiro grupo encontrado
-- Mantém apenas uma linha (priorizando MAIÚSCULAS se existir)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND CO_CID = (
      SELECT FIRST 1 CO_CID
      FROM RL_PROCEDIMENTO_CID
      WHERE DT_COMPETENCIA = '202510'
      GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
      HAVING COUNT(*) > 1
      ORDER BY COUNT(*) DESC
  )
  AND CO_PROCEDIMENTO = (
      SELECT FIRST 1 CO_PROCEDIMENTO
      FROM RL_PROCEDIMENTO_CID
      WHERE DT_COMPETENCIA = '202510'
      GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
      HAVING COUNT(*) > 1
      ORDER BY COUNT(*) DESC
  )
  AND INDICE NOT IN (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID po
      WHERE po.DT_COMPETENCIA = '202510'
        AND po.CO_CID = (
            SELECT FIRST 1 CO_CID
            FROM RL_PROCEDIMENTO_CID
            WHERE DT_COMPETENCIA = '202510'
            GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
            HAVING COUNT(*) > 1
            ORDER BY COUNT(*) DESC
        )
        AND po.CO_PROCEDIMENTO = (
            SELECT FIRST 1 CO_PROCEDIMENTO
            FROM RL_PROCEDIMENTO_CID
            WHERE DT_COMPETENCIA = '202510'
            GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
            HAVING COUNT(*) > 1
            ORDER BY COUNT(*) DESC
        )
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
  );

-- 3. Verificar quantas duplicatas restam
SELECT 
    COUNT(*) AS GRUPOS_DUPLICATAS_RESTANTES
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);

