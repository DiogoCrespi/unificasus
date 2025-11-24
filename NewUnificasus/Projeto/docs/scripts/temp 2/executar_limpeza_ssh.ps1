# Script para executar limpeza de duplicatas via SSH no servidor
# Executa diretamente no servidor para melhor performance

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$ServerUser = "AFSc\dvcrespi",
    [string]$ServerPassword = "Ufetly20#",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [int]$MaxIteracoes = 100
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza de Duplicatas via SSH" -ForegroundColor Cyan
Write-Host "Servidor: $ServerHost" -ForegroundColor Cyan
Write-Host "Processa 10 registros por vez" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Conectando ao servidor..." -ForegroundColor Gray

# Criar script SQL simplificado para executar no servidor
$sqlContent = @"
-- Limpar duplicatas em RL_PROCEDIMENTO_CID (10 registros por vez)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE IN (
      SELECT FIRST 10 INDICE
      FROM RL_PROCEDIMENTO_CID pc1
      WHERE pc1.DT_COMPETENCIA = '202510'
        AND EXISTS (
            SELECT 1
            FROM RL_PROCEDIMENTO_CID pc2
            WHERE pc2.DT_COMPETENCIA = '202510'
              AND pc2.CO_CID = pc1.CO_CID
              AND pc2.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc2.DT_COMPETENCIA = pc1.DT_COMPETENCIA
            GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
            HAVING COUNT(*) > 1
        )
        AND pc1.INDICE <> (
            SELECT MIN(INDICE)
            FROM RL_PROCEDIMENTO_CID pc3
            WHERE pc3.DT_COMPETENCIA = '202510'
              AND pc3.CO_CID = pc1.CO_CID
              AND pc3.CO_PROCEDIMENTO = pc1.CO_PROCEDIMENTO
              AND pc3.DT_COMPETENCIA = pc1.DT_COMPETENCIA
        )
      ORDER BY pc1.INDICE
  );

-- Verificar quantas duplicatas restam
SELECT 
    COUNT(*) AS GRUPOS_DUPLICATAS_RESTANTES
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);
"@

# Salvar script SQL temporário
$localSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($localSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Script SQL criado: $localSqlFile" -ForegroundColor Gray

# Tentar usar SSH via PowerShell (requer módulo Posh-SSH ou OpenSSH)
try {
    # Verificar se Posh-SSH está disponível
    $poshSshAvailable = Get-Module -ListAvailable -Name Posh-SSH
    
    if ($poshSshAvailable) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Usando módulo Posh-SSH..." -ForegroundColor Gray
        Import-Module Posh-SSH
        
        $securePassword = ConvertTo-SecureString $ServerPassword -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential($ServerUser, $securePassword)
        
        $session = New-SSHSession -ComputerName $ServerHost -Credential $credential -AcceptKey
        
        if ($session) {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Conectado ao servidor!" -ForegroundColor Green
            
            $iteracao = 0
            while ($iteracao -lt $MaxIteracoes) {
                $iteracao++
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iteração $iteracao..." -ForegroundColor Yellow
                
                # Copiar arquivo SQL para o servidor
                $remoteSqlFile = "/tmp/limpar_duplicatas_$iteracao.sql"
                Set-SCPFile -ComputerName $ServerHost -Credential $credential -LocalFile $localSqlFile -RemotePath $remoteSqlFile
                
                # Executar comando no servidor
                $command = "$IsqlPath -user SYSDBA -password masterkey `"$DatabasePath`" -i `"$remoteSqlFile`""
                $result = Invoke-SSHCommand -SessionId $session.SessionId -Command $command
                
                Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Resultado:" -ForegroundColor Gray
                $result.Output | ForEach-Object {
                    Write-Host "   $_" -ForegroundColor White
                }
                
                if ($result.Error) {
                    Write-Host "   [ERRO] $($result.Error)" -ForegroundColor Red
                }
                
                # Extrair número de duplicatas restantes
                $duplicatasRestantes = 0
                foreach ($line in $result.Output) {
                    if ($line -match '^\s*(\d+)\s*$') {
                        $duplicatasRestantes = [int]$Matches[1]
                        break
                    }
                }
                
                Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Duplicatas restantes: $duplicatasRestantes" -ForegroundColor $(if ($duplicatasRestantes -eq 0) { "Green" } else { "Yellow" })
                
                if ($duplicatasRestantes -eq 0) {
                    Write-Host ""
                    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Todas as duplicatas foram removidas!" -ForegroundColor Green
                    break
                }
                
                Start-Sleep -Milliseconds 200
            }
            
            Remove-SSHSession -SessionId $session.SessionId | Out-Null
        } else {
            Write-Host "[ERRO] Falha ao conectar via SSH" -ForegroundColor Red
        }
    } else {
        Write-Host "[AVISO] Módulo Posh-SSH não encontrado. Tentando método alternativo..." -ForegroundColor Yellow
        Write-Host "[INFO] Você pode instalar com: Install-Module -Name Posh-SSH" -ForegroundColor Yellow
        
        # Método alternativo: usar plink (PuTTY) se disponível
        $plinkPath = "plink.exe"
        if (Get-Command $plinkPath -ErrorAction SilentlyContinue) {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Usando plink.exe..." -ForegroundColor Gray
            
            # Criar comando para executar no servidor
            $remoteCommand = @"
cd "C:\Program Files\Firebird\Firebird_3_0"
$IsqlPath -user SYSDBA -password masterkey "$DatabasePath" -i "$localSqlFile"
"@
            
            $plinkCommand = "echo $($ServerPassword) | $plinkPath -ssh $ServerUser@$ServerHost -pw $ServerPassword `"$remoteCommand`""
            Write-Host "[AVISO] Execute manualmente no servidor ou instale Posh-SSH" -ForegroundColor Yellow
        } else {
            Write-Host "[ERRO] Nem Posh-SSH nem plink.exe encontrados" -ForegroundColor Red
            Write-Host "[INFO] Opções:" -ForegroundColor Yellow
            Write-Host "   1. Instalar Posh-SSH: Install-Module -Name Posh-SSH" -ForegroundColor Yellow
            Write-Host "   2. Copiar script SQL para o servidor e executar manualmente" -ForegroundColor Yellow
            Write-Host "   3. Usar RDP para acessar o servidor" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "[ERRO] Erro ao conectar: $_" -ForegroundColor Red
    Write-Host "[INFO] Criando script para executar manualmente no servidor..." -ForegroundColor Yellow
    
    # Criar script batch para executar no servidor
    $batchContent = @"
@echo off
echo Limpando duplicatas em RL_PROCEDIMENTO_CID...
echo.

set ITERACAO=0
:LOOP
set /a ITERACAO+=1
echo [%TIME%] Iteracao %ITERACAO%...

"$IsqlPath" -user SYSDBA -password masterkey "$DatabasePath" -i "$localSqlFile"

if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha na iteracao %ITERACAO%
    pause
    exit /b 1
)

echo [%TIME%] Iteracao %ITERACAO% concluida
echo.
if %ITERACAO% LSS 100 goto LOOP

echo Processo concluido!
pause
"@
    
    $batchFile = Join-Path $scriptsPath "executar_limpeza_servidor.bat"
    [System.IO.File]::WriteAllText($batchFile, $batchContent, [System.Text.Encoding]::ASCII)
    Write-Host "[INFO] Script batch criado: $batchFile" -ForegroundColor Green
    Write-Host "[INFO] Copie este arquivo e o SQL para o servidor e execute" -ForegroundColor Yellow
}

# Limpar arquivo temporário
Remove-Item $localSqlFile -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processo concluído!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

