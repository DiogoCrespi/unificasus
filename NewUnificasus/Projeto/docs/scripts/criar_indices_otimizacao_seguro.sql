-- Script: Criar Índices para Otimização de Queries (VERSÃO SEGURA)
-- Objetivo: Criar índices compostos para melhorar performance das queries mais lentas
-- ATENÇÃO: Este script verifica se índices já existem antes de criar
-- ATENÇÃO: Execute em horário de baixo uso para evitar bloqueios

-- ============================================
-- VERIFICAÇÕES PRÉVIAS
-- ============================================

-- Verificar se índices já existem
SET TERM ^;

EXECUTE BLOCK
AS
BEGIN
    -- Verificar índice 1
    IF (NOT EXISTS (
        SELECT 1 FROM RDB$INDICES 
        WHERE RDB$INDEX_NAME = 'IDX_RL_PROCEDIMENTO_CID_PROC_COMP'
    )) THEN
    BEGIN
        EXECUTE STATEMENT 'CREATE INDEX IDX_RL_PROCEDIMENTO_CID_PROC_COMP ON RL_PROCEDIMENTO_CID 
            (CO_PROCEDIMENTO, DT_COMPETENCIA)';
    END
    
    -- Verificar índice 2
    IF (NOT EXISTS (
        SELECT 1 FROM RDB$INDICES 
        WHERE RDB$INDEX_NAME = 'IDX_RL_PROCEDIMENTO_CID_CID_COMP'
    )) THEN
    BEGIN
        EXECUTE STATEMENT 'CREATE INDEX IDX_RL_PROCEDIMENTO_CID_CID_COMP ON RL_PROCEDIMENTO_CID 
            (CO_CID, DT_COMPETENCIA)';
    END
END^

SET TERM ;^

-- ============================================
-- VERIFICAÇÃO PÓS-CRIAÇÃO
-- ============================================

-- Listar índices criados
SELECT 
    TRIM(I.RDB$INDEX_NAME) AS INDICE,
    TRIM(I.RDB$RELATION_NAME) AS TABELA,
    LIST(TRIM(S.RDB$FIELD_NAME), ', ') WITHIN GROUP (ORDER BY S.RDB$FIELD_POSITION) AS CAMPOS
FROM RDB$INDICES I
JOIN RDB$INDEX_SEGMENTS S ON I.RDB$INDEX_NAME = S.RDB$INDEX_NAME
WHERE I.RDB$RELATION_NAME = 'RL_PROCEDIMENTO_CID'
  AND I.RDB$SYSTEM_FLAG = 0
  AND I.RDB$INDEX_NAME IN ('IDX_RL_PROCEDIMENTO_CID_PROC_COMP', 'IDX_RL_PROCEDIMENTO_CID_CID_COMP')
GROUP BY I.RDB$INDEX_NAME, I.RDB$RELATION_NAME
ORDER BY I.RDB$INDEX_NAME;

-- ============================================
-- NOTAS IMPORTANTES:
-- ============================================
-- 1. Este script verifica se índices já existem antes de criar
-- 2. Execute em horário de baixo uso
-- 3. Faça backup antes de executar
-- 4. Monitore a aplicação original após criação
-- 5. Se houver problemas, use o script de remoção

