# Instruções para Executar Limpeza de Duplicatas no Servidor

## Servidor
- **IP:** 192.168.0.3
- **Usuário:** AFSc\dvcrespi
- **Senha:** Ufetly20#

## Opção 1: Executar via RDP (Recomendado)

1. **Conecte ao servidor via RDP:**
   ```
   mstsc /v:192.168.0.3
   ```
   - Usuário: `AFSc\dvcrespi`
   - Senha: `Ufetly20#`

2. **Copie os seguintes arquivos para o servidor:**
   - `limpar_duplicatas_com_logs_detalhados.sql`
   - `executar_limpeza_servidor.bat`
   
   Coloque-os em uma pasta acessível, por exemplo: `C:\temp\limpeza\`

3. **Execute no servidor:**
   ```batch
   cd C:\temp\limpeza
   executar_limpeza_servidor.bat
   ```

4. **OU execute diretamente:**
   ```batch
   "C:\Program Files\Firebird\Firebird_3_0\isql.exe" -user SYSDBA -password masterkey "E:\claupers\unificasus\UNIFICASUS.GDB" -i "C:\temp\limpeza\limpar_duplicatas_com_logs_detalhados.sql"
   ```

## Opção 2: Executar via PowerShell Remoting (Se habilitado)

```powershell
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\docs\scripts"
.\executar_limpeza_remoto.ps1 -UsePowerShellRemoting
```

## Opção 3: Executar Manualmente (Script SQL)

Execute o script SQL múltiplas vezes até não haver mais duplicatas:

```batch
"C:\Program Files\Firebird\Firebird_3_0\isql.exe" -user SYSDBA -password masterkey "E:\claupers\unificasus\UNIFICASUS.GDB" -i "limpar_duplicatas_com_logs_detalhados.sql"
```

## O que o script faz:

1. **Lista os registros que serão removidos** (primeiros 10)
   - Mostra: INDICE, CO_CID, CO_PROCEDIMENTO, NO_CID
   
2. **Remove os registros duplicados**
   - Mantém apenas o registro com menor INDICE
   - Preserva registros com NO_CID totalmente em maiúsculas (sistema antigo)

3. **Mostra progresso**
   - Quantos grupos de duplicatas restam
   - Total de registros na competência 202510

## Logs Detalhados

O script mostra:
- `>>> REMOVENDO` - Lista de registros que serão removidos
- `PROGRESSO` - Status após cada iteração

## Observações

- O script processa **10 registros por vez** para evitar locks no banco
- Execute múltiplas vezes até `GRUPOS_DUPLICATAS_RESTANTES = 0`
- O processo pode levar várias iterações dependendo da quantidade de duplicatas
- **NÃO INTERROMPA** o processo no meio, deixe completar cada iteração

## Verificação Final

Após concluir, execute:

```sql
SELECT 
    COUNT(*) AS GRUPOS_DUPLICATAS_RESTANTES
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);
```

Se retornar `0`, todas as duplicatas foram removidas!

