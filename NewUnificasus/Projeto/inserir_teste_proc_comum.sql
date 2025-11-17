-- Script para inserir um procedimento comum de teste
-- Procedimento: 0301100012 - ADMINISTRACAO DE MEDICAMENTOS NA ATENCA(

-- Primeiro, verificar o próximo código disponível
SELECT MAX(PRC_COD) + 1 AS PROXIMO_CODIGO FROM TB_PROCOMUNS;

-- Inserir o procedimento comum de teste
-- Nota: Substitua {PROXIMO_CODIGO} pelo valor retornado acima, ou use um valor maior que o máximo existente
INSERT INTO TB_PROCOMUNS (PRC_COD, PRC_CODPROC, PRC_NO_PROCEDIMENTO, PRC_OBSERVACOES)
VALUES (
    (SELECT COALESCE(MAX(PRC_COD), 0) + 1 FROM TB_PROCOMUNS),  -- Próximo código disponível
    '0301100012',  -- Código do procedimento
    'ADMINISTRACAO DE MEDICAMENTOS NA ATENCA(',  -- Nome do procedimento
    'Teste de inserção manual - Data: ' || CAST(CURRENT_TIMESTAMP AS VARCHAR(50))  -- Observações com data/hora
);

-- Verificar se foi inserido corretamente
SELECT 
    PRC_COD AS CODIGO,
    PRC_CODPROC AS CODIGO_PROCEDIMENTO,
    PRC_NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    PRC_OBSERVACOES AS OBSERVACOES
FROM TB_PROCOMUNS
WHERE PRC_CODPROC = '0301100012'
ORDER BY PRC_COD DESC;

