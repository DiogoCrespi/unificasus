-- Verificar distribuição de competências em TB_HABILITACAO para uma habilitação específica
SELECT FIRST 20 * FROM TB_HABILITACAO ORDER BY CO_HABILITACAO, DT_COMPETENCIA DESC;
