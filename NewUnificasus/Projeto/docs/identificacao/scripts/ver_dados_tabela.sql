-- Script: Verificar Dados Existentes em uma Tabela
-- Objetivo: Obter contagem e amostra de dados de uma tabela
-- Uso: Substituir 'NOME_DA_TABELA' pelo nome da tabela a analisar

-- Contagem total de registros
SELECT COUNT(*) AS TOTAL_REGISTROS
FROM NOME_DA_TABELA;

-- Primeiros 10 registros (amostra)
SELECT *
FROM NOME_DA_TABELA
ROWS 10;

