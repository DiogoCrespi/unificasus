-- ============================================
-- SCRIPT DIRETO PARA CORRIGIR COMPETÊNCIA 202510
-- Execute este script no banco de dados Firebird
-- ============================================

-- PASSO 1: Verificar situação atual
SELECT 
    'Total de procedimentos' AS DESCRICAO,
    COUNT(*) AS VALOR
FROM TB_PROCEDIMENTO
UNION ALL
SELECT 
    'Com competência 202510',
    COUNT(*)
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510'
UNION ALL
SELECT 
    'Sem competência (NULL ou vazio)',
    COUNT(*)
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NULL OR TRIM(DT_COMPETENCIA) = '';

-- PASSO 2: Listar competências atuais (como o sistema faz)
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- PASSO 3: CORREÇÃO - Atualizar procedimentos sem competência para 202510
-- ATENÇÃO: Execute apenas se os procedimentos importados são realmente da competência 202510
-- Baseado no log, foram importados 4947 procedimentos para 202510
UPDATE TB_PROCEDIMENTO
SET DT_COMPETENCIA = '202510'
WHERE DT_COMPETENCIA IS NULL 
   OR TRIM(DT_COMPETENCIA) = '';

-- PASSO 4: Verificar se a correção funcionou
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- PASSO 5: Verificar TB_COMPETENCIA_ATIVA (registro de controle)
SELECT 
    DT_COMPETENCIA,
    ST_ATIVA,
    DT_ATIVACAO
FROM TB_COMPETENCIA_ATIVA
WHERE DT_COMPETENCIA = '202510';

-- Se não existir na TB_COMPETENCIA_ATIVA, inserir:
INSERT INTO TB_COMPETENCIA_ATIVA (DT_COMPETENCIA, ST_ATIVA, DT_ATIVACAO)
SELECT '202510', 'N', CURRENT_TIMESTAMP
FROM RDB$DATABASE
WHERE NOT EXISTS (
    SELECT 1 FROM TB_COMPETENCIA_ATIVA WHERE DT_COMPETENCIA = '202510'
);

-- ============================================
-- RESULTADO ESPERADO:
-- Após executar este script, a competência 202510 deve aparecer na listagem
-- Reinicie a aplicação e verifique o ComboBox de competências
-- ============================================

