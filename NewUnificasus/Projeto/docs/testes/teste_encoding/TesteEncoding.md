# Teste de Encoding - UnificaSUS

Este diretório contém utilitários e informações para testar e validar o encoding de caracteres do banco Firebird.

## Problema Identificado

Os acentos não estão sendo exibidos corretamente. Exemplo:
- **Esperado**: "0701010061 CALÇADOS ORTOPÉDICOS CONFECCIONADOS SOB MEDIDA ATÉ NÚMERO 45 (PAR)"
- **Atual**: "0701010061 CALADOS ORTOPDICOS CONFECCIONADOS SOB MEDIDA AT NMERO 45 (PAR)"

## Solução Implementada

1. **FirebirdReaderHelper melhorado**: Agora sempre tenta ler os bytes diretamente do banco usando `GetBytes()`, que é mais confiável
2. **Conversão de encoding**: Usa Windows-1252 (padrão para bancos brasileiros) com fallback para outras codificações
3. **Estratégias múltiplas**: Tenta 3 estratégias diferentes para garantir que os dados sejam lidos corretamente

## Como Testar

1. Execute a aplicação
2. Verifique se os acentos estão sendo exibidos corretamente nos procedimentos
3. Procure por procedimentos que contenham acentos (ex: "CALÇADOS", "ORTOPÉDICOS", "ATÉ", "NÚMERO")

## Arquivos de Referência

- `FirebirdReaderHelper.cs`: Helper principal para leitura de strings com encoding correto
- `ProcedimentoRepository.cs`: Repositório que usa o helper para ler procedimentos
- `ConfigurationReader.cs`: Configuração da conexão (usa Charset=NONE)

## Notas Técnicas

- A conexão usa `Charset=NONE` para evitar problemas de charset do Firebird
- Os dados são lidos como bytes brutos e convertidos manualmente para Windows-1252
- O helper tenta múltiplas estratégias para garantir compatibilidade

