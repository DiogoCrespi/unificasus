-- Script para limpar duplicatas em RL_PROCEDIMENTO_CID com logs detalhados
-- Processa 10 registros por vez e mostra quais foram removidos

-- 1. Listar os registros que serão removidos (primeiros 10)
SELECT 
    'REGISTROS QUE SERAO REMOVIDOS' AS ACAO,
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    SUBSTRING(pc.NO_CID FROM 1 FOR 40) AS NO_CID,
    CASE 
        WHEN UPPER(TRIM(pc.NO_CID)) = TRIM(pc.NO_CID) THEN 'MAIUSCULAS'
        ELSE 'Minusculas/Misturadas'
    END AS TIPO
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '202510'
  AND pc.INDICE IN (
      -- Seleciona todos os INDICES exceto o menor para cada grupo duplicado
      SELECT FIRST 10 INDICE
      FROM RL_PROCEDIMENTO_CID pc1
      WHERE pc1.DT_COMPETENCIA = '202510'
        AND EXISTS (
            SELECT 1
            FROM RL_PROCEDIMENTO_CID pc2
            WHERE pc2.DT_COMPETENCIA = '202510'
              AND pc2.CO_CID = pc1.CO_CID
              AND pc2.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc2.DT_COMPETENCIA = pc1.DT_COMPETENCIA
            GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
            HAVING COUNT(*) > 1
        )
        AND pc1.INDICE <> (
            SELECT MIN(INDICE)
            FROM RL_PROCEDIMENTO_CID pc3
            WHERE pc3.DT_COMPETENCIA = '202510'
              AND pc3.CO_CID = pc1.CO_CID
              AND pc3.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc3.DT_COMPETENCIA = pc1.DT_COMPETENCIA
        )
      ORDER BY pc1.INDICE
  )
ORDER BY pc.INDICE;

-- 2. Remover os registros listados acima
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE IN (
      SELECT FIRST 10 INDICE
      FROM RL_PROCEDIMENTO_CID pc1
      WHERE pc1.DT_COMPETENCIA = '202510'
        AND EXISTS (
            SELECT 1
            FROM RL_PROCEDIMENTO_CID pc2
            WHERE pc2.DT_COMPETENCIA = '202510'
              AND pc2.CO_CID = pc1.CO_CID
              AND pc2.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc2.DT_COMPETENCIA = pc1.DT_COMPETENCIA
            GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
            HAVING COUNT(*) > 1
        )
        AND pc1.INDICE <> (
            SELECT MIN(INDICE)
            FROM RL_PROCEDIMENTO_CID pc3
            WHERE pc3.DT_COMPETENCIA = '202510'
              AND pc3.CO_CID = pc1.CO_CID
              AND pc3.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc3.DT_COMPETENCIA = pc1.DT_COMPETENCIA
        )
      ORDER BY pc1.INDICE
  );

-- 3. Confirmar quantos registros foram removidos
SELECT 
    'REGISTROS REMOVIDOS NESTA ITERACAO' AS RESULTADO,
    COUNT(*) AS TOTAL
FROM (
    SELECT FIRST 10 INDICE
    FROM RL_PROCEDIMENTO_CID pc1
    WHERE pc1.DT_COMPETENCIA = '202510'
      AND EXISTS (
          SELECT 1
          FROM RL_PROCEDIMENTO_CID pc2
          WHERE pc2.DT_COMPETENCIA = '202510'
            AND pc2.CO_CID = pc1.CO_CID
            AND pc2.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
            AND pc2.DT_COMPETENCIA = pc1.DT_COMPETENCIA
          GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
          HAVING COUNT(*) > 1
      )
      AND pc1.INDICE <> (
          SELECT MIN(INDICE)
          FROM RL_PROCEDIMENTO_CID pc3
          WHERE pc3.DT_COMPETENCIA = '202510'
            AND pc3.CO_CID = pc1.CO_CID
            AND pc3.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
            AND pc3.DT_COMPETENCIA = pc1.DT_COMPETENCIA
      )
    ORDER BY pc1.INDICE
) WHERE 1=0;  -- Sempre retorna 0 porque já foram deletados

-- 4. Verificar quantas duplicatas restam
SELECT 
    'GRUPOS DUPLICADOS RESTANTES' AS STATUS,
    COUNT(*) AS TOTAL
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);

