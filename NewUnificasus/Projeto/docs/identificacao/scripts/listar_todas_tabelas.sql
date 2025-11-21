-- Script: Listar Todas as Tabelas do Banco de Dados
-- Objetivo: Obter lista completa de todas as tabelas do sistema
-- Uso: Executar antes de iniciar a busca por tabelas específicas

SELECT 
    RF.RDB$RELATION_NAME AS TABELA,
    CASE 
        WHEN RF.RDB$RELATION_TYPE = 0 THEN 'TABELA'
        WHEN RF.RDB$RELATION_TYPE = 1 THEN 'VIEW'
        WHEN RF.RDB$RELATION_TYPE = 2 THEN 'TRIGGER'
        WHEN RF.RDB$RELATION_TYPE = 3 THEN 'COMPUTED'
        ELSE 'OUTRO'
    END AS TIPO
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
ORDER BY RF.RDB$RELATION_NAME;

