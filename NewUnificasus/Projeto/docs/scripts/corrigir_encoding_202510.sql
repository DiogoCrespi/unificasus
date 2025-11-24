-- ============================================
-- SCRIPT PARA CORRIGIR ENCODING DOS DADOS DA COMPETÊNCIA 202510
-- Problema: Textos aparecem como "ORIENTAÃ§ÃƒO" ao invés de "ORIENTAÇÃO"
-- Causa: Arquivo Windows-1252 foi lido como ISO-8859-1 ou UTF-8
-- ============================================

-- ============================================
-- DIAGNÓSTICO: Verificar dados corrompidos
-- ============================================

-- Verificar quantos registros têm caracteres corrompidos
SELECT 
    'TB_PROCEDIMENTO' AS TABELA,
    COUNT(*) AS TOTAL_CORROMPIDOS
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
ROWS 10;

-- ============================================
-- CORREÇÃO: Função para corrigir encoding
-- ============================================
-- NOTA: Firebird não tem função nativa para conversão de encoding
-- A correção deve ser feita reimportando os dados com encoding correto
-- OU usando uma função externa/UDF para conversão

-- ============================================
-- SOLUÇÃO RECOMENDADA: Reimportar com encoding correto
-- ============================================
-- 1. Deletar dados da competência 202510
-- 2. Reimportar com encoding Windows-1252 correto

-- Deletar dados da competência 202510 (CUIDADO: Execute apenas se tiver certeza!)
/*
DELETE FROM RL_PROCEDIMENTO_TUSS WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_RENASES WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_REGRA_COND WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_COMP_REDE WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_CID WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_SERVICO WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_MODALIDADE WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_LEITO WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_OCUPACAO WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_HABILITACAO WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_DETALHE WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_REGISTRO WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_COMPATIVEL WHERE DT_COMPETENCIA = '202510';
DELETE FROM RL_PROCEDIMENTO_INCREMENTO WHERE DT_COMPETENCIA = '202510';
DELETE FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA = '202510';
DELETE FROM TB_DESCRICAO WHERE DT_COMPETENCIA = '202510';
DELETE FROM TB_DESCRICAO_DETALHE WHERE DT_COMPETENCIA = '202510';
-- ... outras tabelas
*/

-- ============================================
-- ALTERNATIVA: Correção manual usando substituições
-- ============================================
-- ATENÇÃO: Esta é uma solução parcial, pode não corrigir todos os casos

-- Padrões de correção comuns:
-- Ã§ = ç
-- Ãƒ = Ã (mas pode ser parte de outro caractere)
-- Ã¡ = á
-- Ã© = é
-- Ã­ = í
-- Ã³ = ó
-- Ãº = ú
-- Ã£ = ã
-- Ãµ = õ
-- Ã‰ = É
-- Ã = Á

-- Exemplo de correção (execute com cuidado, teste primeiro em poucos registros):
/*
UPDATE TB_PROCEDIMENTO
SET NO_PROCEDIMENTO = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        NO_PROCEDIMENTO,
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
  AND (NO_PROCEDIMENTO CONTAINING 'Ã§' 
       OR NO_PROCEDIMENTO CONTAINING 'Ãƒ'
       OR NO_PROCEDIMENTO CONTAINING 'Ã¡');
*/

-- ============================================
-- VERIFICAÇÃO APÓS CORREÇÃO
-- ============================================
SELECT 
    CO_PROCEDIMENTO,
    NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
  AND CO_PROCEDIMENTO = '0101010010';

-- ============================================
-- RECOMENDAÇÃO FINAL
-- ============================================
-- A melhor solução é reimportar os dados com o encoding correto (Windows-1252)
-- O código foi atualizado para detectar e corrigir automaticamente na próxima importação
-- ============================================

