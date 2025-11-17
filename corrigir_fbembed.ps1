# Script para corrigir o erro "Unable to load DLL 'fbembed'"
# O Firebird 3.0 não tem fbembed.dll, então vamos usar modo servidor (localhost)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CORRIGIR ERRO: Unable to load fbembed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se está executando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "AVISO: Execute este script como Administrador!" -ForegroundColor Yellow
    Write-Host "Clique com botao direito e selecione 'Executar como administrador'" -ForegroundColor Yellow
    Write-Host ""
}

$firebird3Path = "C:\Program Files\Firebird\Firebird_3_0"
$appPath = "C:\Program Files\claupers\unificasus"
$iniPath = Join-Path $appPath "unificasus.ini"

# 1. Verificar se Firebird 3.0 está instalado
Write-Host "[1/4] Verificando Firebird 3.0..." -ForegroundColor Cyan
if (-not (Test-Path $firebird3Path)) {
    Write-Host "  ERRO: Firebird 3.0 nao encontrado em: $firebird3Path" -ForegroundColor Red
    exit 1
}
Write-Host "  OK: Firebird 3.0 encontrado" -ForegroundColor Green

# 2. Fechar aplicação se estiver rodando
Write-Host "[2/4] Verificando se aplicacao esta rodando..." -ForegroundColor Cyan
$process = Get-Process -Name "unificasus" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "  Aplicacao rodando. Fechando..." -ForegroundColor Yellow
    Stop-Process -Name "unificasus" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  OK: Aplicacao fechada" -ForegroundColor Green
} else {
    Write-Host "  OK: Aplicacao nao esta rodando" -ForegroundColor Green
}

# 3. Atualizar unificasus.ini para usar localhost (modo servidor)
Write-Host "[3/4] Atualizando unificasus.ini para usar modo servidor..." -ForegroundColor Cyan
if (Test-Path $iniPath) {
    $iniContent = Get-Content $iniPath -Raw
    $bancoPath = "C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB"
    
    # Verificar se já está usando localhost
    if ($iniContent -match "local=localhost:") {
        Write-Host "  OK: unificasus.ini ja esta configurado para usar localhost" -ForegroundColor Green
    } else {
        # Atualizar para usar localhost
        $newContent = "[DB]`r`n;local=C:\Program Files\claupers\unificasus\UNIFICASUS.GDB`r`n;local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB`r`nlocal=localhost:$bancoPath`r`n"
        Set-Content -Path $iniPath -Value $newContent -Encoding UTF8
        Write-Host "  OK: unificasus.ini atualizado para usar localhost" -ForegroundColor Green
    }
} else {
    Write-Host "  ERRO: unificasus.ini nao encontrado" -ForegroundColor Red
    exit 1
}

# 4. Verificar e iniciar serviço Firebird
Write-Host "[4/4] Verificando servico Firebird..." -ForegroundColor Cyan
$firebirdService = Get-Service -Name "*Firebird*" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($firebirdService) {
    if ($firebirdService.Status -ne 'Running') {
        Write-Host "  Iniciando servico Firebird..." -ForegroundColor Yellow
        Start-Service -Name $firebirdService.Name -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    if ($firebirdService.Status -eq 'Running') {
        Write-Host "  OK: Servico Firebird esta rodando" -ForegroundColor Green
    } else {
        Write-Host "  AVISO: Nao foi possivel iniciar o servico Firebird" -ForegroundColor Yellow
        Write-Host "  Tente iniciar manualmente: services.msc" -ForegroundColor Yellow
    }
} else {
    Write-Host "  AVISO: Servico Firebird nao encontrado" -ForegroundColor Yellow
    Write-Host "  O Firebird pode estar rodando como aplicacao ou precisa ser instalado como servico" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  CORRECAO CONCLUIDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "SOLUCAO APLICADA:" -ForegroundColor Cyan
Write-Host "  O Firebird 3.0 nao tem fbembed.dll (modo embedded)." -ForegroundColor White
Write-Host "  A configuracao foi alterada para usar modo SERVIDOR (localhost)." -ForegroundColor White
Write-Host ""
Write-Host "Configuracao atual:" -ForegroundColor Cyan
Write-Host "  - Banco: localhost:C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB" -ForegroundColor White
Write-Host "  - Modo: Servidor (ServerType=0)" -ForegroundColor White
Write-Host "  - Firebird: C:\Program Files\Firebird\Firebird_3_0" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANTE:" -ForegroundColor Yellow
Write-Host "  O servico Firebird precisa estar RODANDO para a aplicacao funcionar." -ForegroundColor Yellow
Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "  1. Verifique se o servico Firebird esta rodando (services.msc)" -ForegroundColor White
Write-Host "  2. Se nao estiver, inicie o servico manualmente" -ForegroundColor White
Write-Host "  3. Execute a aplicacao unificasus.exe" -ForegroundColor White
Write-Host "  4. O erro 'Unable to load DLL fbembed' nao deve mais aparecer" -ForegroundColor White
Write-Host ""

