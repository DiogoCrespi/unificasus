-- Script para testar query de relatório por Grupo
-- Objetivo: Verificar se conseguimos buscar procedimentos por grupo e exibir código, nome e valor SP

-- Parâmetros de teste (substituir pelos valores reais)
-- @grupo: Código do grupo (ex: '01')
-- @competencia: Competência (ex: '202401')

-- Exemplo 1: Buscar procedimentos do grupo '01' na competência '202401'
SELECT 
    pr.CO_PROCEDIMENTO AS CODIGO_PROCEDIMENTO,
    CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
    pr.NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    pr.VL_SP AS VALOR_SP,
    pr.DT_COMPETENCIA AS COMPETENCIA
FROM TB_PROCEDIMENTO pr
WHERE pr.CO_GRUPO = '01'
  AND pr.DT_COMPETENCIA = '202401'
  AND pr.VL_SP > 0  -- Não imprimir procedimentos com SP zerado (se checkbox marcado)
ORDER BY pr.CO_PROCEDIMENTO;  -- Ordenar por código (padrão)

-- Exemplo 2: Buscar procedimentos do grupo '01' incluindo SP zerado
SELECT 
    pr.CO_PROCEDIMENTO AS CODIGO_PROCEDIMENTO,
    CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
    pr.NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    pr.VL_SP AS VALOR_SP,
    pr.DT_COMPETENCIA AS COMPETENCIA
FROM TB_PROCEDIMENTO pr
WHERE pr.CO_GRUPO = '01'
  AND pr.DT_COMPETENCIA = '202401'
ORDER BY pr.CO_PROCEDIMENTO;

-- Exemplo 3: Ordenar por nome
SELECT 
    pr.CO_PROCEDIMENTO AS CODIGO_PROCEDIMENTO,
    CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
    pr.NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    pr.VL_SP AS VALOR_SP,
    pr.DT_COMPETENCIA AS COMPETENCIA
FROM TB_PROCEDIMENTO pr
WHERE pr.CO_GRUPO = '01'
  AND pr.DT_COMPETENCIA = '202401'
  AND pr.VL_SP > 0
ORDER BY pr.NO_PROCEDIMENTO;

-- Exemplo 4: Ordenar por valor SP (decrescente)
SELECT 
    pr.CO_PROCEDIMENTO AS CODIGO_PROCEDIMENTO,
    CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
    pr.NO_PROCEDIMENTO AS NOME_PROCEDIMENTO,
    pr.VL_SP AS VALOR_SP,
    pr.DT_COMPETENCIA AS COMPETENCIA
FROM TB_PROCEDIMENTO pr
WHERE pr.CO_GRUPO = '01'
  AND pr.DT_COMPETENCIA = '202401'
  AND pr.VL_SP > 0
ORDER BY pr.VL_SP DESC;

-- Exemplo 5: Contar total de procedimentos por grupo
SELECT 
    pr.CO_GRUPO AS CODIGO_GRUPO,
    COUNT(*) AS TOTAL_PROCEDIMENTOS,
    COUNT(CASE WHEN pr.VL_SP > 0 THEN 1 END) AS TOTAL_COM_SP_MAIOR_ZERO
FROM TB_PROCEDIMENTO pr
WHERE pr.DT_COMPETENCIA = '202401'
GROUP BY pr.CO_GRUPO
ORDER BY pr.CO_GRUPO;

