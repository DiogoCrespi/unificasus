-- Script automático para limpar duplicatas em RL_PROCEDIMENTO_CID
-- Processa um grupo por vez, mantendo sempre o menor INDICE

-- Remover duplicatas do primeiro grupo encontrado
-- Mantém apenas o registro com menor INDICE para cada combinação única
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE IN (
      -- Seleciona todos os INDICES exceto o menor para cada grupo duplicado
      SELECT INDICE
      FROM RL_PROCEDIMENTO_CID pc1
      WHERE pc1.DT_COMPETENCIA = '202510'
        AND EXISTS (
            -- Verifica se este registro faz parte de um grupo duplicado
            SELECT 1
            FROM RL_PROCEDIMENTO_CID pc2
            WHERE pc2.DT_COMPETENCIA = '202510'
              AND pc2.CO_CID = pc1.CO_CID
              AND pc2.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc2.DT_COMPETENCIA = pc1.DT_COMPETENCIA
            GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
            HAVING COUNT(*) > 1
        )
        AND INDICE <> (
            -- Mantém apenas o menor INDICE de cada grupo
            SELECT MIN(INDICE)
            FROM RL_PROCEDIMENTO_CID pc3
            WHERE pc3.DT_COMPETENCIA = '202510'
              AND pc3.CO_CID = pc1.CO_CID
              AND pc3.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc3.DT_COMPETENCIA = pc1.DT_COMPETENCIA
        )
      -- Limita a 10 registros por execução para não sobrecarregar
      ROWS 10
  );

-- Verificar quantas duplicatas restam
SELECT 
    COUNT(*) AS GRUPOS_DUPLICATAS_RESTANTES
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);

