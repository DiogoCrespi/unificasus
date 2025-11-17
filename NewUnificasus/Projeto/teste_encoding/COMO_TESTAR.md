# Como Testar a Correção de Encoding

## Teste Visual na Aplicação

Como o terminal não suporta acentuação corretamente, use a interface gráfica da aplicação para validar:

### Passo 1: Execute a Aplicação
1. Abra o projeto no Visual Studio
2. Execute a aplicação (F5)
3. Aguarde a tela inicial carregar

### Passo 2: Carregue os Procedimentos
1. Selecione uma competência (se necessário)
2. Selecione uma categoria no TreeView à esquerda
3. Os procedimentos devem aparecer na grade à direita

### Passo 3: Verifique os Acentos

Procure por procedimentos que contenham estas palavras e verifique se os acentos aparecem corretamente:

#### Palavras de Teste:
- **CALÇADOS** - deve mostrar o Ç (cedilha)
- **ORTOPÉDICOS** - deve mostrar o É (e com acento agudo)
- **ATÉ** - deve mostrar o É (e com acento agudo)
- **NÚMERO** - deve mostrar o Ú (u com acento agudo)
- **CONFECÇÃO** - deve mostrar o Ç (cedilha)
- **MÉDICO** - deve mostrar o É (e com acento agudo)
- **ÓRTESE** - deve mostrar o Ó (o com acento agudo)

### Passo 4: Teste de Busca
1. Use o botão "Localizar"
2. Busque por "CALÇADOS" (com cedilha)
3. Verifique se encontra os procedimentos corretos
4. Verifique se os resultados mostram os acentos corretamente

### Passo 5: Verifique os Detalhes
1. Selecione um procedimento na grade
2. Verifique o campo "Procedimento" na área de detalhes
3. Os acentos devem aparecer corretamente

## Exemplo de Procedimento para Testar

Procure pelo código: **0701010061**

**Esperado:**
```
0701010061 CALÇADOS ORTOPÉDICOS CONFECCIONADOS SOB MEDIDA ATÉ NÚMERO 45 (PAR)
```

**Se aparecer assim (ERRADO):**
```
0701010061 CALADOS ORTOPDICOS CONFECCIONADOS SOB MEDIDA AT NMERO 45 (PAR)
```

ou

```
0701010061 CALADOS ORTOPDICOS CONFECCIONADOS SOB MEDIDA AT NMERO 45 (PAR)
```

Então ainda há problema de encoding.

## O Que Fazer Se Ainda Houver Problema

1. **Verifique o log da aplicação** - pode haver mensagens de erro
2. **Teste com outro procedimento** - pode ser específico de alguns registros
3. **Verifique a conexão** - confirme que está usando `Charset=NONE`
4. **Consulte o arquivo** `RESUMO_CORRECAO_ENCODING.md` para mais detalhes técnicos

## Validação Automática (Futuro)

Se necessário, podemos criar um teste unitário que:
- Conecta ao banco
- Lê um procedimento conhecido
- Valida se os acentos estão corretos
- Gera um relatório

Mas por enquanto, o teste visual é suficiente.

