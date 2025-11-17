# Script para configurar Firebird 3.0 para o banco UNIFICASUS
# Este script copia as DLLs do Firebird 3.0 para a aplicação

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURAR FIREBIRD 3.0" -ForegroundColor Cyan
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
$gds32Path = Join-Path $appPath "gds32.dll"

# 1. Verificar se Firebird 3.0 está instalado
Write-Host "[1/4] Verificando Firebird 3.0..." -ForegroundColor Cyan
if (-not (Test-Path $firebird3Path)) {
    Write-Host "  ERRO: Firebird 3.0 nao encontrado em: $firebird3Path" -ForegroundColor Red
    Write-Host "  Verifique se o Firebird 3.0 esta instalado corretamente." -ForegroundColor Yellow
    exit 1
}

# Procurar fbclient.dll no Firebird 3.0
$fbclient3 = Get-ChildItem $firebird3Path -Recurse -Filter "fbclient.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $fbclient3) {
    Write-Host "  ERRO: fbclient.dll nao encontrado no Firebird 3.0" -ForegroundColor Red
    Write-Host "  Verifique se o Firebird 3.0 esta instalado corretamente." -ForegroundColor Yellow
    exit 1
}
Write-Host "  OK: Firebird 3.0 encontrado" -ForegroundColor Green
Write-Host "  fbclient.dll: $($fbclient3.FullName)" -ForegroundColor Cyan

# 2. Parar TODOS os serviços do Firebird (se estiverem rodando)
Write-Host "[2/4] Verificando servicos Firebird..." -ForegroundColor Cyan
$firebirdServices = Get-Service -Name "*Firebird*" -ErrorAction SilentlyContinue
if ($firebirdServices) {
    $firebirdServices | ForEach-Object { 
        if ($_.Status -eq 'Running') {
            Write-Host "  Parando: $($_.Name)..." -ForegroundColor Yellow
            Stop-Service -Name $_.Name -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
        }
    }
    Write-Host "  OK: Servicos Firebird verificados" -ForegroundColor Green
} else {
    Write-Host "  OK: Nenhum servico Firebird encontrado" -ForegroundColor Green
}

# 3. Fechar aplicação se estiver rodando
Write-Host "[3/4] Verificando se aplicacao esta rodando..." -ForegroundColor Cyan
$process = Get-Process -Name "unificasus" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "  Aplicacao rodando. Fechando..." -ForegroundColor Yellow
    Stop-Process -Name "unificasus" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  OK: Aplicacao fechada" -ForegroundColor Green
} else {
    Write-Host "  OK: Aplicacao nao esta rodando" -ForegroundColor Green
}

# 4. Atualizar gds32.dll para usar Firebird 3.0
Write-Host "[4/4] Atualizando gds32.dll para Firebird 3.0..." -ForegroundColor Cyan

# Fazer backup do gds32.dll atual
if (Test-Path $gds32Path) {
    $backupPath = "$gds32Path.backup_antes_firebird3_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item $gds32Path $backupPath -Force
    Write-Host "  Backup criado: $backupPath" -ForegroundColor Cyan
}

# Copiar fbclient.dll do Firebird 3.0 como gds32.dll
try {
    Copy-Item $fbclient3.FullName $gds32Path -Force
    Write-Host "  OK: gds32.dll atualizado para Firebird 3.0" -ForegroundColor Green
} catch {
    Write-Host "  ERRO: Nao foi possivel copiar gds32.dll: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Verifique se voce tem permissoes de administrador" -ForegroundColor Yellow
    exit 1
}

# Verificar se precisa atualizar no SysWOW64 (para aplicações 32-bit)
$syswow64Path = "C:\Windows\SysWOW64\gds32.dll"
if (Test-Path $syswow64Path) {
    $backupSysWOW64 = "$syswow64Path.backup_antes_firebird3_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item $syswow64Path $backupSysWOW64 -Force
    Copy-Item $fbclient3.FullName $syswow64Path -Force
    Write-Host "  OK: gds32.dll atualizado no SysWOW64" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  CONFIGURACAO CONCLUIDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
# 5. Copiar DLLs adicionais necessárias para o Firebird funcionar
Write-Host "[5/5] Copiando DLLs adicionais do Firebird 3.0..." -ForegroundColor Cyan
$additionalDlls = @("ib_util.dll", "msvcp100.dll", "msvcr100.dll", "zlib1.dll", "icudt52.dll", "icuin52.dll", "icuuc52.dll")
foreach ($dll in $additionalDlls) {
    $srcDll = Get-ChildItem $firebird3Path -Recurse -Filter $dll -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($srcDll) {
        $dstDll = Join-Path $appPath $dll
        Copy-Item $srcDll.FullName $dstDll -Force -ErrorAction SilentlyContinue
        Write-Host "  OK: $dll copiado" -ForegroundColor Green
    }
}

# Copiar também da pasta WOW64 se existir (para aplicações 32-bit)
$wow64Path = Join-Path $firebird3Path "WOW64"
if (Test-Path $wow64Path) {
    foreach ($dll in $additionalDlls) {
        $srcDll = Join-Path $wow64Path $dll
        if (Test-Path $srcDll) {
            $dstDll = Join-Path $appPath $dll
            Copy-Item $srcDll $dstDll -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  CONFIGURACAO CONCLUIDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuracao atual:" -ForegroundColor Cyan
Write-Host "  - Banco: localhost:C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB" -ForegroundColor White
Write-Host "  - Firebird: C:\Program Files\Firebird\Firebird_3_0" -ForegroundColor White
Write-Host "  - Modo: Servidor (localhost)" -ForegroundColor White
Write-Host "  - gds32.dll: Atualizado para Firebird 3.0" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANTE: O Firebird Server precisa estar rodando!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "  1. Inicie o servico Firebird (se nao estiver rodando)" -ForegroundColor White
Write-Host "  2. Abra a aplicacao unificasus.exe" -ForegroundColor White
Write-Host "  3. A aplicacao deve conectar ao banco local usando Firebird 3.0" -ForegroundColor White
Write-Host "  4. Teste a conexao e funcionalidades" -ForegroundColor White
Write-Host ""

