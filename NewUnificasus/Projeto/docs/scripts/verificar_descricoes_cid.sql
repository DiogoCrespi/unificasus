-- Verificar se descrições CID variam por competência
SELECT 
    'CIDs com descrições diferentes' AS VERIFICACAO,
    COUNT(*) AS TOTAL
FROM (
    SELECT 
        c.CO_CID
    FROM TB_CID c
    INNER JOIN RL_PROCEDIMENTO_CID rl ON c.CO_CID = rl.CO_CID
    GROUP BY c.CO_CID
    HAVING COUNT(DISTINCT rl.NO_CID) > 1
);

