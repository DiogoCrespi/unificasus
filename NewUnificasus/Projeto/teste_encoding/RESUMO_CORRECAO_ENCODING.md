# Resumo da Correção de Encoding

## Problema
Os acentos não estavam sendo exibidos corretamente nos dados lidos do banco Firebird.
Exemplo: "CALÇADOS ORTOPÉDICOS" aparecia como "CALADOS ORTOPDICOS"

## Causa
Com `Charset=NONE` na string de conexão, o Firebird retorna os dados como bytes brutos, mas o driver .NET pode estar interpretando incorretamente ou fazendo conversões intermediárias que corrompem os dados.

## Solução Implementada

### 1. Melhorias no FirebirdReaderHelper.cs

O helper agora usa **3 estratégias** em ordem de prioridade:

#### Estratégia 1: Leitura direta como byte[]
- Tenta ler o valor diretamente como `byte[]` do reader
- Mais confiável quando o driver retorna bytes diretamente

#### Estratégia 2: Leitura usando GetBytes()
- Usa `reader.GetBytes()` para ler os bytes brutos do campo
- Funciona mesmo quando o driver retorna como string
- Remove bytes nulos no final automaticamente

#### Estratégia 3: Reconversão via Latin1
- Se não conseguiu ler como bytes, lê como string
- Usa Latin1 (ISO-8859-1) para preservar os bytes originais (mapeamento 1:1)
- Converte os bytes para Windows-1252

### 2. Conversão de Encoding

- **Windows-1252** é usado como codificação principal (padrão para bancos brasileiros)
- Fallback automático para outras codificações se Windows-1252 não funcionar
- Validação para evitar caracteres de substituição (?)

### 3. Simplificação do ProcedimentoRepository

- Removida lógica duplicada de conversão
- Agora usa apenas o `FirebirdReaderHelper.GetStringSafe()` que já faz todo o trabalho
- Prioriza leitura do BLOB (mais confiável) e depois campo direto

## Arquivos Modificados

1. **FirebirdReaderHelper.cs**
   - Adicionadas 3 estratégias de leitura
   - Método `ConvertBytesToString()` para conversão robusta
   - Método `TryAlternativeEncodings()` para fallback

2. **ProcedimentoRepository.cs**
   - Simplificado método `MapProcedimento()`
   - Removido método `ConvertBlobToString()` (lógica movida para o helper)
   - Queries usam `CAST(NO_PROCEDIMENTO AS BLOB)` para acesso aos bytes brutos

3. **GrupoRepository.cs**
   - Todas as queries atualizadas para usar `CAST` para BLOB:
     - `NO_GRUPO` → `CAST(NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB`
     - `NO_SUB_GRUPO` → `CAST(NO_SUB_GRUPO AS BLOB) AS NO_SUB_GRUPO_BLOB`
     - `NO_FORMA_ORGANIZACAO` → `CAST(NO_FORMA_ORGANIZACAO AS BLOB) AS NO_FORMA_ORGANIZACAO_BLOB`
   - Leitura prioriza BLOB e faz fallback para campo direto

## Como Testar

1. Execute a aplicação
2. Carregue os procedimentos
3. Verifique se os acentos estão corretos:
   - "CALÇADOS" deve aparecer com Ç
   - "ORTOPÉDICOS" deve aparecer com É
   - "ATÉ" deve aparecer com É
   - "NÚMERO" deve aparecer com Ú

## Notas Técnicas

- **MUDANÇA IMPORTANTE**: A conexão agora usa `Charset=WIN1252` (como a aplicação original)
  - Anteriormente usávamos `Charset=NONE` e fazíamos conversão manual
  - Com `Charset=WIN1252`, o Firebird faz a conversão automaticamente
  - Isso resolve o problema de acentuação de forma mais simples e confiável
- Todos os repositórios já usam `FirebirdReaderHelper.GetStringSafe()`, então a correção se aplica automaticamente a todos
- **Todas as queries de texto usam `CAST` para BLOB** como fallback:
  - Procedimentos: `CAST(NO_PROCEDIMENTO AS BLOB)`
  - Grupos: `CAST(NO_GRUPO AS BLOB)`
  - Sub-Grupos: `CAST(NO_SUB_GRUPO AS BLOB)`
  - Formas de Organização: `CAST(NO_FORMA_ORGANIZACAO AS BLOB)`
- O helper prioriza leitura direta como string (com WIN1252 funciona), com fallbacks para bytes se necessário

## Próximos Passos (se necessário)

Se ainda houver problemas:
1. Verificar o charset real do banco usando o script `VERIFICAR_CHARSET_BANCO.sql`
2. Ajustar a codificação no helper se necessário
3. Considerar usar `Charset=WIN1252` na string de conexão (pode causar outros problemas)

