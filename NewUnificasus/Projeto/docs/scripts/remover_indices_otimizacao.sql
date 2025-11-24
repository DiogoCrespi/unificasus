-- Script: Remover Índices de Otimização (ROLLBACK)
-- Objetivo: Remover índices criados caso haja problemas
-- ATENÇÃO: Use apenas se houver problemas após criação dos índices

-- ============================================
-- REMOVER ÍNDICES
-- ============================================

-- Remover índice 1 (se existir)
DROP INDEX IDX_RL_PCID_PROC_COMP;

-- Remover índice 2 (se existir)
DROP INDEX IDX_RL_PCID_CID_COMP;

-- ============================================
-- VERIFICAÇÃO PÓS-REMOÇÃO
-- ============================================

-- Verificar se índices foram removidos
SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM RDB$INDICES 
            WHERE RDB$INDEX_NAME = 'IDX_RL_PCID_PROC_COMP'
        ) THEN 'AINDA EXISTE'
        ELSE 'REMOVIDO'
    END AS STATUS_IDX_PROC_COMP
FROM RDB$DATABASE;

SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM RDB$INDICES 
            WHERE RDB$INDEX_NAME = 'IDX_RL_PCID_CID_COMP'
        ) THEN 'AINDA EXISTE'
        ELSE 'REMOVIDO'
    END AS STATUS_IDX_CID_COMP
FROM RDB$DATABASE;

