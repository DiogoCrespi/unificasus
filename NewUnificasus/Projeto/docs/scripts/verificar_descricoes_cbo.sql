-- Verificar se descrições CBO variam por competência
SELECT 
    'CBOs com descrições diferentes' AS VERIFICACAO,
    COUNT(*) AS TOTAL
FROM (
    SELECT 
        o.CO_OCUPACAO
    FROM TB_OCUPACAO o
    INNER JOIN RL_PROCEDIMENTO_OCUPACAO rl ON o.CO_OCUPACAO = rl.CO_OCUPACAO
    GROUP BY o.CO_OCUPACAO
    HAVING COUNT(DISTINCT rl.NO_OCUPACAO) > 1
);

