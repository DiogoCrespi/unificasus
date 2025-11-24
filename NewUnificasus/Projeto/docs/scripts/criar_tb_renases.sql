-- Script para criar a tabela TB_RENASES
-- Baseado no layout: tb_renases_layout.txt
-- Estrutura:
--   CO_RENASES: VARCHAR(10) - Código RENASES (chave primária)
--   NO_RENASES: VARCHAR(150) - Nome RENASES

CREATE TABLE TB_RENASES (
    CO_RENASES VARCHAR(10) NOT NULL,
    NO_RENASES VARCHAR(150),
    CONSTRAINT PK_TB_RENASES PRIMARY KEY (CO_RENASES)
);

-- Comentários (Firebird)
COMMENT ON TABLE TB_RENASES IS 'Tabela de códigos RENASES';
COMMENT ON COLUMN TB_RENASES.CO_RENASES IS 'Código RENASES (chave primária)';
COMMENT ON COLUMN TB_RENASES.NO_RENASES IS 'Nome RENASES';

