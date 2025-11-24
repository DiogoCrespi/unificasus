-- Script para validar e garantir que a competência 202510 apareça na listagem
-- Baseado no log: ImportLog_20251122_070908.txt
-- Competência identificada: 202510 (Outubro de 2025)
-- 
-- IMPORTANTE: A listagem de competências busca de TB_PROCEDIMENTO (DISTINCT DT_COMPETENCIA)
-- Portanto, a competência só aparece se houver procedimentos com DT_COMPETENCIA = '202510'

-- ============================================
-- 1. VERIFICAR SE HÁ PROCEDIMENTOS COM COMPETÊNCIA 202510
-- ============================================
SELECT 
    COUNT(*) AS TOTAL_PROCEDIMENTOS,
    COUNT(DISTINCT DT_COMPETENCIA) AS COMPETENCIAS_DISTINTAS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- 2. LISTAR TODAS AS COMPETÊNCIAS DISPONÍVEIS (como o sistema faz)
-- ============================================
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- ============================================
-- 3. VERIFICAR SE A COMPETÊNCIA 202510 ESTÁ NA LISTAGEM
-- ============================================
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN 'COMPETÊNCIA 202510 ENCONTRADA ✓'
        ELSE 'COMPETÊNCIA 202510 NÃO ENCONTRADA ✗'
    END AS STATUS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- 4. VERIFICAR TB_COMPETENCIA_ATIVA (registro de controle)
-- ============================================
-- Verificar se a tabela existe
SELECT COUNT(*) AS TABELA_EXISTE
FROM RDB$RELATIONS 
WHERE RDB$RELATION_NAME = 'TB_COMPETENCIA_ATIVA'
  AND RDB$SYSTEM_FLAG = 0;

-- Verificar se a competência está registrada
SELECT 
    DT_COMPETENCIA,
    ST_ATIVA,
    DT_ATIVACAO
FROM TB_COMPETENCIA_ATIVA
WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- 5. DIAGNÓSTICO: Verificar outras tabelas com DT_COMPETENCIA
-- ============================================
-- Verificar quantas tabelas têm dados com competência 202510
SELECT 'TB_PROCEDIMENTO' AS TABELA, COUNT(*) AS REGISTROS
FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA = '202510'
UNION ALL
SELECT 'TB_CID', COUNT(*) FROM TB_CID WHERE DT_COMPETENCIA = '202510'
UNION ALL
SELECT 'TB_SERVICO', COUNT(*) FROM TB_SERVICO WHERE DT_COMPETENCIA = '202510'
UNION ALL
SELECT 'RL_PROCEDIMENTO_CID', COUNT(*) FROM RL_PROCEDIMENTO_CID WHERE DT_COMPETENCIA = '202510'
UNION ALL
SELECT 'RL_PROCEDIMENTO_RENASES', COUNT(*) FROM RL_PROCEDIMENTO_RENASES WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- 6. SOLUÇÃO: Se não houver procedimentos, verificar se a importação foi bem-sucedida
-- ============================================
-- Se a competência não aparecer, pode ser que:
-- 1. A importação de TB_PROCEDIMENTO falhou
-- 2. Os dados não têm DT_COMPETENCIA preenchida
-- 3. A competência está em outra tabela mas não em TB_PROCEDIMENTO

-- Verificar se há procedimentos sem competência
SELECT COUNT(*) AS PROCEDIMENTOS_SEM_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NULL OR DT_COMPETENCIA = '';

-- ============================================
-- 7. REGISTRAR NA TB_COMPETENCIA_ATIVA (se necessário)
-- ============================================
-- Criar tabela se não existir (executar apenas se necessário)
/*
CREATE TABLE TB_COMPETENCIA_ATIVA (
    DT_COMPETENCIA VARCHAR(6) NOT NULL PRIMARY KEY,
    DT_ATIVACAO TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ST_ATIVA CHAR(1) DEFAULT 'N'
);
*/

-- Inserir ou atualizar a competência 202510 na TB_COMPETENCIA_ATIVA
-- (Isso não faz aparecer na listagem, mas registra para controle)
/*
INSERT INTO TB_COMPETENCIA_ATIVA (DT_COMPETENCIA, ST_ATIVA, DT_ATIVACAO)
VALUES ('202510', 'N', CURRENT_TIMESTAMP)
WHERE NOT EXISTS (
    SELECT 1 FROM TB_COMPETENCIA_ATIVA WHERE DT_COMPETENCIA = '202510'
);
*/

