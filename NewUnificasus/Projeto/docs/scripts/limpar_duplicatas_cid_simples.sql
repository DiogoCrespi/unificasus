-- Script simplificado para limpar duplicatas em RL_PROCEDIMENTO_CID
-- Processa um grupo de duplicatas por vez de forma mais direta

-- 1. Identificar primeiro grupo de duplicatas
SELECT 
    FIRST 1
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS,
    MIN(INDICE) AS INDICE_MANTIDO
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 2. Remover duplicatas do primeiro grupo (mantém apenas o menor INDICE)
-- Substitua os valores abaixo pelos resultados da query acima
/*
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND CO_CID = 'T845'  -- Substitua pelo CO_CID do resultado acima
  AND CO_PROCEDIMENTO = '0201010127'  -- Substitua pelo CO_PROCEDIMENTO do resultado acima
  AND INDICE <> (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID
      WHERE DT_COMPETENCIA = '202510'
        AND CO_CID = 'T845'
        AND CO_PROCEDIMENTO = '0201010127'
  );
*/

