# Solução de Encoding - UTF-8 no Firebird

## Problema Identificado

A acentuação estava sendo corrompida ao ler dados do Firebird:
- **Esperado:** "AÇÕES RELACIONADAS A DOAÇÃO"
- **Obtido:** "AÃÇÕES RELACIONADAS A DOAÃÇÃO"

## Causa Raiz

Análise dos bytes mostrou claramente o problema:

### Bytes esperados (Windows-1252):
- Ç = `0xC7`
- Õ = `0xD5`
- Ó = `0xD3`
- Ã = `0xC3`

### Bytes realmente salvos (UTF-8):
- Ç = `0xC3 0x87` (UTF-8)
- Õ = `0xC3 0x95` (UTF-8)
- Ó = `0xC3 0x93` (UTF-8)
- Ã = `0xC3 0x83` (UTF-8)

**Conclusão:** O driver do Firebird .NET **sempre salva strings como UTF-8**, independentemente do `Charset` da conexão quando `Charset=NONE` está configurado.

## Solução Implementada

### 1. Inserção de Dados
- **Passa strings diretamente** via `AddWithValue`
- O driver converte automaticamente para UTF-8
- Não precisa normalização ou conversão manual

### 2. Leitura de Dados
- **Lê bytes do BLOB** usando `CAST(campo AS BLOB)`
- **Converte de UTF-8** para string: `Encoding.UTF8.GetString(bytes)`
- NÃO usa Windows-1252 na conversão

### 3. Arquivos Modificados

#### ProcedimentoComumRepository.cs
```csharp
// Leitura: Converte de UTF-8
prcNoProcedimento = Encoding.UTF8.GetString(validBytes);
```

#### ProcedimentoRepository.cs
```csharp
// Leitura: Converte de UTF-8
noProcedimento = Encoding.UTF8.GetString(validBytes);
```

#### GrupoRepository.cs
```csharp
// Leitura: Converte de UTF-8
resultado = Encoding.UTF8.GetString(validBytes);
```

#### RelatorioRepository.cs
```csharp
// Leitura: Converte de UTF-8
resultado = Encoding.UTF8.GetString(validBytes);
```

## Como Funciona

1. **Inserção:**
   - Aplicação envia string em Unicode (.NET): "AÇÕES"
   - Driver converte para UTF-8: `0xC3 0x87 0xC3 0x95 0x45 0x53`
   - Firebird salva os bytes UTF-8

2. **Leitura:**
   - Query usa `CAST(campo AS BLOB)` para acessar bytes brutos
   - Lê bytes do BLOB: `0xC3 0x87 0xC3 0x95 0x45 0x53`
   - Converte de UTF-8 para string: "AÇÕES"
   - Aplicação recebe string correta

## Teste de Validação

Foi criado um teste automatizado em `MainWindow.xaml.cs` (botão "🧪 Teste"):
- Insere texto com acentuação
- Lê de volta do banco
- Compara bytes original vs. lido
- Valida se os acentos foram preservados
- Mostra resultado detalhado em arquivo de log

### Resultado do Teste:
✓ Bytes são idênticos  
✓ Texto é idêntico  
✓ Todos os caracteres preservados (Ç, Õ, Ã, Ó)

## Configuração do Banco

- **Charset da Conexão:** `NONE`
- **Charset do Banco:** Provavelmente `NONE` ou sem charset definido
- **Encoding Real dos Dados:** UTF-8 (convertido automaticamente pelo driver)

## Por Que Funciona

- O driver Firebird .NET converte strings para UTF-8 automaticamente
- Lendo como UTF-8, preservamos os acentos corretamente
- Não precisa conversão manual ou normalização na inserção
- Simples e direto

## Aplicação em Outros Repositórios

A mesma lógica foi aplicada em:
- ✅ ProcedimentoComumRepository (testado e funcionando)
- ✅ ProcedimentoRepository (corrigido)
- ✅ GrupoRepository (corrigido)
- ✅ RelatorioRepository (corrigido)

Todos os repositórios agora leem dados do BLOB usando UTF-8, garantindo acentuação correta.

