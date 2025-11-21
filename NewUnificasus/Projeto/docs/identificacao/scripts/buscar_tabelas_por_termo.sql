-- Script: Buscar Tabelas por Termo
-- Objetivo: Encontrar tabelas que contenham um termo específico no nome
-- Uso: Substituir 'TERMO' pelo termo de busca (ex: CID, CBO, HABILITACAO)
-- Exemplo: Buscar por 'CID' para encontrar tabelas relacionadas a CID-10

SELECT 
    RF.RDB$RELATION_NAME AS TABELA,
    CASE 
        WHEN RF.RDB$RELATION_TYPE = 0 THEN 'TABELA'
        WHEN RF.RDB$RELATION_TYPE = 1 THEN 'VIEW'
        ELSE 'OUTRO'
    END AS TIPO
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
  AND RF.RDB$RELATION_NAME CONTAINING 'TERMO'
ORDER BY RF.RDB$RELATION_NAME;

