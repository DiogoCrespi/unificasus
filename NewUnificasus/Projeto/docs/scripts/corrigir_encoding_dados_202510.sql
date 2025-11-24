-- ============================================
-- SCRIPT PARA CORRIGIR ENCODING DOS DADOS JÁ IMPORTADOS - COMPETÊNCIA 202510
-- Problema: Textos aparecem como "ORIENTAÃ§ÃƒO" ao invés de "ORIENTAÇÃO"
-- Causa: Arquivo Windows-1252 foi lido como ISO-8859-1 durante importação
-- ============================================

-- ============================================
-- PASSO 1: DIAGNÓSTICO
-- ============================================

-- Verificar quantos registros têm caracteres corrompidos em TB_PROCEDIMENTO
SELECT 
    COUNT(*) AS TOTAL_CORROMPIDOS,
    COUNT(*) * 100.0 / (SELECT COUNT(*) FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA = '202510') AS PERCENTUAL
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
  AND (NO_PROCEDIMENTO CONTAINING 'Ã§' 
       OR NO_PROCEDIMENTO CONTAINING 'Ãƒ'
       OR NO_PROCEDIMENTO CONTAINING 'Ã¡'
       OR NO_PROCEDIMENTO CONTAINING 'Ã©'
       OR NO_PROCEDIMENTO CONTAINING 'Ã­'
       OR NO_PROCEDIMENTO CONTAINING 'Ã³'
       OR NO_PROCEDIMENTO CONTAINING 'Ãº'
       OR NO_PROCEDIMENTO CONTAINING 'Ã£'
       OR NO_PROCEDIMENTO CONTAINING 'Ãµ');

-- Exemplo de dados corrompidos
SELECT 
    CO_PROCEDIMENTO,
    NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
  AND NO_PROCEDIMENTO CONTAINING 'Ã§'
ROWS 5;

-- ============================================
-- PASSO 2: CORREÇÃO EM TB_PROCEDIMENTO
-- ============================================
-- Corrige os padrões mais comuns de corrupção de encoding
-- Windows-1252 lido como ISO-8859-1 causa estas substituições:

UPDATE TB_PROCEDIMENTO
SET NO_PROCEDIMENTO = 
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        NO_PROCEDIMENTO,
        'Ã§', 'ç'),      -- ç
        'Ã¡', 'á'),      -- á
        'Ã©', 'é'),      -- é
        'Ã­', 'í'),      -- í
        'Ã³', 'ó'),      -- ó
        'Ãº', 'ú'),      -- ú
        'Ã£', 'ã'),      -- ã
        'Ãµ', 'õ'),      -- õ
        'Ã‰', 'É'),      -- É
        'Ã', 'Á'),       -- Á (cuidado: pode afetar outros)
        'Ã€', 'À'),      -- À
        'Ã‚', 'Â'),      -- Â
        'Ãƒ', 'Ã'),      -- Ã
        'Ã', 'Í'),       -- Í
        'Ã"', 'Ó'),      -- Ó
        'Ã"', 'Ô'),      -- Ô
        'Ã•', 'Õ'),      -- Õ
        'Ãš', 'Ú'),      -- Ú
        'Ã›', 'Û'),      -- Û
        'Ãœ', 'Ü'),      -- Ü
        'Ã', 'Ç'),       -- Ç
        'Ã±', 'ñ'),      -- ñ
        'Ã', 'Ñ')        -- Ñ
WHERE DT_COMPETENCIA = '202510'
  AND (NO_PROCEDIMENTO CONTAINING 'Ã§' 
       OR NO_PROCEDIMENTO CONTAINING 'Ãƒ'
       OR NO_PROCEDIMENTO CONTAINING 'Ã¡'
       OR NO_PROCEDIMENTO CONTAINING 'Ã©'
       OR NO_PROCEDIMENTO CONTAINING 'Ã­'
       OR NO_PROCEDIMENTO CONTAINING 'Ã³'
       OR NO_PROCEDIMENTO CONTAINING 'Ãº'
       OR NO_PROCEDIMENTO CONTAINING 'Ã£'
       OR NO_PROCEDIMENTO CONTAINING 'Ãµ');

-- ============================================
-- PASSO 3: CORREÇÃO EM OUTRAS TABELAS COM CAMPOS DE TEXTO
-- ============================================

-- TB_DESCRICAO
UPDATE TB_DESCRICAO
SET DE_PROCEDIMENTO = 
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        DE_PROCEDIMENTO,
        'Ã§', 'ç'),
        'Ã¡', 'á'),
        'Ã©', 'é'),
        'Ã­', 'í'),
        'Ã³', 'ó'),
        'Ãº', 'ú'),
        'Ã£', 'ã'),
        'Ãµ', 'õ'),
        'Ã‰', 'É'),
        'Ã', 'Á')
WHERE DT_COMPETENCIA = '202510'
  AND (DE_PROCEDIMENTO CONTAINING 'Ã§' 
       OR DE_PROCEDIMENTO CONTAINING 'Ãƒ'
       OR DE_PROCEDIMENTO CONTAINING 'Ã¡');

-- TB_DESCRICAO_DETALHE
UPDATE TB_DESCRICAO_DETALHE
SET DE_DETALHE = 
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        DE_DETALHE,
        'Ã§', 'ç'),
        'Ã¡', 'á'),
        'Ã©', 'é'),
        'Ã­', 'í'),
        'Ã³', 'ó'),
        'Ãº', 'ú'),
        'Ã£', 'ã'),
        'Ãµ', 'õ'),
        'Ã‰', 'É'),
        'Ã', 'Á')
WHERE DT_COMPETENCIA = '202510'
  AND (DE_DETALHE CONTAINING 'Ã§' 
       OR DE_DETALHE CONTAINING 'Ãƒ'
       OR DE_DETALHE CONTAINING 'Ã¡');

-- TB_CID (se tiver campo de descrição)
UPDATE TB_CID
SET NO_CID = 
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        NO_CID,
        'Ã§', 'ç'),
        'Ã¡', 'á'),
        'Ã©', 'é'),
        'Ã­', 'í'),
        'Ã³', 'ó'),
        'Ãº', 'ú'),
        'Ã£', 'ã'),
        'Ãµ', 'õ'),
        'Ã‰', 'É'),
        'Ã', 'Á')
WHERE DT_COMPETENCIA = '202510'
  AND (NO_CID CONTAINING 'Ã§' 
       OR NO_CID CONTAINING 'Ãƒ'
       OR NO_CID CONTAINING 'Ã¡');

-- ============================================
-- PASSO 4: VERIFICAÇÃO APÓS CORREÇÃO
-- ============================================

-- Verificar se ainda há caracteres corrompidos
SELECT 
    COUNT(*) AS AINDA_CORROMPIDOS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
  AND (NO_PROCEDIMENTO CONTAINING 'Ã§' 
       OR NO_PROCEDIMENTO CONTAINING 'Ãƒ'
       OR NO_PROCEDIMENTO CONTAINING 'Ã¡');

-- Verificar exemplo corrigido
SELECT 
    CO_PROCEDIMENTO,
    NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
  AND CO_PROCEDIMENTO = '0101010010';

-- ============================================
-- NOTA IMPORTANTE
-- ============================================
-- Esta correção é uma solução parcial usando substituições de strings
-- Para uma correção completa, recomenda-se reimportar os dados com encoding correto
-- O código foi atualizado para detectar e corrigir automaticamente na próxima importação
-- ============================================

