# Script para executar limpeza de duplicatas remotamente
# Opção 1: Executar diretamente no servidor via RDP
# Opção 2: Usar PowerShell Remoting (se habilitado)

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$ServerUser = "AFSc\dvcrespi",
    [string]$ServerPassword = "Ufetly20#",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [int]$MaxIteracoes = 100,
    [switch]$UsePowerShellRemoting = $false
)

$scriptsPath = $PSScriptRoot
$sqlFile = Join-Path $scriptsPath "limpar_duplicatas_servidor.sql"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza de Duplicatas - Execução Remota" -ForegroundColor Cyan
Write-Host "Servidor: $ServerHost" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($UsePowerShellRemoting) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Tentando PowerShell Remoting..." -ForegroundColor Gray
    
    $securePassword = ConvertTo-SecureString $ServerPassword -AsPlainText -Force
    $credential = New-Object System.Management.Automation.PSCredential($ServerUser, $securePassword)
    
    try {
        $session = New-PSSession -ComputerName $ServerHost -Credential $credential -ErrorAction Stop
        
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Conectado via PowerShell Remoting!" -ForegroundColor Green
        
        # Copiar arquivo SQL para o servidor
        $remoteSqlPath = "C:\temp\limpar_duplicatas_servidor.sql"
        Copy-Item -Path $sqlFile -Destination $remoteSqlPath -ToSession $session -Force
        
        # Criar script de execução no servidor
        $remoteScript = @"
`$iteracao = 0
`$maxIteracoes = $MaxIteracoes
`$isqlPath = '$IsqlPath'
`$dbPath = '$DatabasePath'
`$sqlFile = '$remoteSqlPath'

while (`$iteracao -lt `$maxIteracoes) {
    `$iteracao++
    Write-Host "[`$(Get-Date -Format 'HH:mm:ss')] Iteracao `$iteracao..." -ForegroundColor Yellow
    
    `$result = & "`$isqlPath" -user SYSDBA -password masterkey "`$dbPath" -i "`$sqlFile" 2>&1
    
    Write-Host `$result
    
    # Verificar se ainda há duplicatas
    `$checkSql = "SELECT COUNT(*) FROM (SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA FROM RL_PROCEDIMENTO_CID WHERE DT_COMPETENCIA = '202510' GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA HAVING COUNT(*) > 1);"
    `$checkResult = & "`$isqlPath" -user SYSDBA -password masterkey "`$dbPath" -q -x -z 2>&1 | Select-String -Pattern '^\s*(\d+)\s*$'
    
    if (`$checkResult) {
        `$duplicatas = [int]`$checkResult.Matches[0].Groups[1].Value
        Write-Host "[`$(Get-Date -Format 'HH:mm:ss')] Duplicatas restantes: `$duplicatas" -ForegroundColor `$(if (`$duplicatas -eq 0) { 'Green' } else { 'Yellow' })
        
        if (`$duplicatas -eq 0) {
            Write-Host "[`$(Get-Date -Format 'HH:mm:ss')] Todas as duplicatas foram removidas!" -ForegroundColor Green
            break
        }
    }
    
    Start-Sleep -Milliseconds 200
}
"@
        
        Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create($remoteScript))
        
        Remove-PSSession -Session $session
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processo concluído!" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERRO] Falha ao conectar via PowerShell Remoting: $_" -ForegroundColor Red
        Write-Host "[INFO] PowerShell Remoting pode não estar habilitado no servidor" -ForegroundColor Yellow
        Write-Host "[INFO] Use a opção de executar diretamente no servidor" -ForegroundColor Yellow
    }
}
else {
    Write-Host "[INFO] Para executar no servidor:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Acesse o servidor via RDP (192.168.0.3)" -ForegroundColor Cyan
    Write-Host "2. Copie os seguintes arquivos para o servidor:" -ForegroundColor Cyan
    Write-Host "   - $sqlFile" -ForegroundColor White
    Write-Host "   - $scriptsPath\executar_limpeza_servidor.bat" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Execute no servidor:" -ForegroundColor Cyan
    Write-Host "   executar_limpeza_servidor.bat" -ForegroundColor White
    Write-Host ""
    Write-Host "OU execute diretamente:" -ForegroundColor Cyan
    Write-Host "   `"$IsqlPath`" -user SYSDBA -password masterkey `"$DatabasePath`" -i `"$sqlFile`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Repita o comando até não haver mais duplicatas." -ForegroundColor Yellow
    Write-Host ""
    
    # Criar script simplificado para copiar
    Write-Host "[INFO] Arquivos prontos para copiar:" -ForegroundColor Green
    Write-Host "   SQL: $sqlFile" -ForegroundColor White
    Write-Host "   BAT: $scriptsPath\executar_limpeza_servidor.bat" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

