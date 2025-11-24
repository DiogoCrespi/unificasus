-- Script para ver os dados da tabela TB_PROCOMUNS
-- Estrutura identificada:
-- PRC_COD (INTEGER, PK) - Código único
-- PRC_CODPROC (VARCHAR(10)) - Código do procedimento
-- PRC_NO_PROCEDIMENTO (VARCHAR(250)) - Nome do procedimento
-- PRC_OBSERVACOES (VARCHAR(255)) - Observações

-- Ver todos os registros
SELECT 
    PRC_COD AS CODIGO,
    PRC_CODPROC AS CODIGO_PROCEDIMENTO,
    PRC_NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    PRC_OBSERVACOES AS OBSERVACOES
FROM TB_PROCOMUNS
ORDER BY PRC_COD;

-- Contar total
SELECT COUNT(*) AS TOTAL FROM TB_PROCOMUNS;

