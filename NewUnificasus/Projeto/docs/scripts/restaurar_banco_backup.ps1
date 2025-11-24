# Script para restaurar banco de dados a partir de um backup
# IMPORTANTE: Este script faz backup do banco atual antes de substituir
# Suporta banco local e remoto

param(
    [Parameter(Mandatory=$true)]
    [string]$CaminhoBackup,
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "C:\Program Files\claupers\unificasus\unificasus.ini",
    
    [Parameter(Mandatory=$false)]
    [switch]$ReiniciarAplicacao = $false
)

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "RESTAURAR BANCO DE DADOS A PARTIR DE BACKUP" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Ler configuração do banco
$bancoAtual = $null
if (Test-Path $ConfigFile) {
    $configContent = Get-Content $ConfigFile -Raw
    if ($configContent -match '(?m)^local\s*=\s*(.+)$') {
        $bancoAtual = $matches[1].Trim()
        # Remover comentários da linha
        $bancoAtual = $bancoAtual -replace ';.*$', ''
    }
}

if ([string]::IsNullOrEmpty($bancoAtual)) {
    Write-Host "ERRO: Não foi possível ler a configuração do banco de dados." -ForegroundColor Red
    Write-Host "Arquivo: $ConfigFile" -ForegroundColor Red
    exit 1
}

Write-Host "Banco configurado: $bancoAtual" -ForegroundColor Cyan
Write-Host ""

# Verificar se é banco remoto ou local
$isRemoto = $bancoAtual -match '^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|localhost):(.+)$'
$caminhoBanco = if ($isRemoto) { $matches[2] } else { $bancoAtual }
$servidor = if ($isRemoto) { $matches[1] } else { $null }

if ($isRemoto) {
    Write-Host "Tipo: BANCO REMOTO" -ForegroundColor Yellow
    Write-Host "Servidor: $servidor" -ForegroundColor Yellow
    Write-Host "Caminho no servidor: $caminhoBanco" -ForegroundColor Yellow
    Write-Host ""
    
    # Tentar mapear como unidade de rede temporária
    $networkPath = "\\$servidor\E$"
    
    Write-Host "Tentando mapear unidade de rede..." -ForegroundColor Yellow
    Write-Host "Caminho: $networkPath" -ForegroundColor Yellow
    
    # Tentar encontrar uma unidade disponível (Y, Z, X, W, V)
    $driveLetters = @("Y:", "Z:", "X:", "W:", "V:")
    $driveLetter = $null
    
    foreach ($letter in $driveLetters) {
        # Tentar desconectar se estiver mapeada
        net use $letter /delete 2>$null | Out-Null
        Start-Sleep -Milliseconds 500
        
        # Tentar mapear
        $result = net use $letter $networkPath /persistent:no 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Unidade $letter mapeada com sucesso!" -ForegroundColor Green
            $driveLetter = $letter
            break
        }
    }
    
    if (-not $driveLetter) {
        Write-Host "Não foi possível mapear nenhuma unidade disponível." -ForegroundColor Red
        Write-Host ""
        Write-Host "OPÇÕES:" -ForegroundColor Yellow
        Write-Host "1. Desconecte manualmente uma unidade de rede (Y, Z, X, W ou V)" -ForegroundColor White
        Write-Host "2. Verifique se tem acesso ao servidor $servidor" -ForegroundColor White
        Write-Host "3. Copie o backup manualmente para o servidor" -ForegroundColor White
        exit 1
    }
    
    $caminhoBanco = "$driveLetter\$($caminhoBanco -replace '^E:\\', '' -replace '\\', '\')"
    $isRemoto = $false
    
    # Copiar backup para pasta temporária no servidor
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host "COPIANDO BACKUP PARA SERVIDOR REMOTO" -ForegroundColor Cyan
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    $pastaTemp = "$driveLetter\claupers\unificasus\temp2"
    $nomeBackup = Split-Path $CaminhoBackup -Leaf
    $caminhoBackupNoServidor = Join-Path $pastaTemp $nomeBackup
    
    Write-Host "Pasta temporária no servidor: $pastaTemp" -ForegroundColor Yellow
    Write-Host "Arquivo de backup local: $CaminhoBackup" -ForegroundColor Yellow
    Write-Host "Destino no servidor: $caminhoBackupNoServidor" -ForegroundColor Yellow
    Write-Host ""
    
    try {
        # Criar pasta temp2 se não existir
        if (-not (Test-Path $pastaTemp)) {
            Write-Host "Criando pasta temporária no servidor..." -ForegroundColor Yellow
            New-Item -ItemType Directory -Path $pastaTemp -Force | Out-Null
            Write-Host "Pasta criada com sucesso!" -ForegroundColor Green
        }
        
        # Copiar arquivo
        Write-Host "Copiando backup para o servidor..." -ForegroundColor Yellow
        $backupInfo = Get-Item $CaminhoBackup
        $tamanhoMB = [math]::Round($backupInfo.Length / 1MB, 2)
        Write-Host "Tamanho do arquivo: $tamanhoMB MB" -ForegroundColor Yellow
        Write-Host "Isso pode demorar alguns minutos..." -ForegroundColor Yellow
        Write-Host ""
        
        Copy-Item -Path $CaminhoBackup -Destination $caminhoBackupNoServidor -Force
        
        Write-Host "Backup copiado com sucesso para o servidor!" -ForegroundColor Green
        Write-Host "Localização: $caminhoBackupNoServidor" -ForegroundColor Green
        Write-Host ""
        
        # Atualizar caminho do backup para usar o do servidor
        $CaminhoBackup = $caminhoBackupNoServidor
    }
    catch {
        Write-Host "ERRO ao copiar backup para o servidor: $_" -ForegroundColor Red
        Write-Host "Operação cancelada." -ForegroundColor Red
        
        # Desconectar unidade
        net use $driveLetter /delete 2>$null | Out-Null
        exit 1
    }
}
else {
    Write-Host "Tipo: BANCO LOCAL" -ForegroundColor Green
    Write-Host "Caminho: $caminhoBanco" -ForegroundColor Green
    Write-Host ""
}

# Verificar se o arquivo de backup existe
if (-not (Test-Path $CaminhoBackup)) {
    Write-Host "ERRO: Arquivo de backup não encontrado: $CaminhoBackup" -ForegroundColor Red
    exit 1
}

Write-Host "Arquivo de backup encontrado: $CaminhoBackup" -ForegroundColor Green
$backupInfo = Get-Item $CaminhoBackup
Write-Host "Tamanho: $([math]::Round($backupInfo.Length / 1MB, 2)) MB" -ForegroundColor Yellow
Write-Host "Data de modificação: $($backupInfo.LastWriteTime)" -ForegroundColor Yellow
Write-Host ""

# Verificar se o banco atual existe
$bancoExiste = Test-Path $caminhoBanco
if ($bancoExiste) {
    $bancoAtualInfo = Get-Item $caminhoBanco
    Write-Host "Banco atual encontrado: $caminhoBanco" -ForegroundColor Yellow
    Write-Host "Tamanho: $([math]::Round($bancoAtualInfo.Length / 1MB, 2)) MB" -ForegroundColor Yellow
    Write-Host "Data de modificação: $($bancoAtualInfo.LastWriteTime)" -ForegroundColor Yellow
    Write-Host ""
    
    # Criar backup do banco atual
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $diretorioBackup = Split-Path $caminhoBanco -Parent
    $nomeBanco = Split-Path $caminhoBanco -Leaf
    $caminhoBackupAtual = Join-Path $diretorioBackup "backup_antes_restauracao_$timestamp.GDB"
    
    Write-Host "Criando backup do banco atual..." -ForegroundColor Yellow
    Write-Host "Destino: $caminhoBackupAtual" -ForegroundColor Yellow
    
    try {
        Copy-Item -Path $caminhoBanco -Destination $caminhoBackupAtual -Force
        Write-Host "Backup do banco atual criado com sucesso!" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "ERRO ao criar backup do banco atual: $_" -ForegroundColor Red
        Write-Host "Operação cancelada por segurança." -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "AVISO: Banco atual não encontrado em: $caminhoBanco" -ForegroundColor Yellow
    Write-Host "Será criado um novo banco a partir do backup." -ForegroundColor Yellow
    Write-Host ""
}

# Confirmar operação
Write-Host "============================================================================" -ForegroundColor Yellow
Write-Host "ATENÇÃO: Esta operação irá SUBSTITUIR o banco de dados atual!" -ForegroundColor Yellow
Write-Host "============================================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Banco atual: $caminhoBanco" -ForegroundColor White
Write-Host "Backup a restaurar: $CaminhoBackup" -ForegroundColor White
if ($bancoExiste) {
    Write-Host "Backup do banco atual salvo em: $caminhoBackupAtual" -ForegroundColor White
}
Write-Host ""

$confirm = Read-Host "Deseja continuar? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operação cancelada pelo usuário." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Restaurando banco de dados..." -ForegroundColor Yellow

try {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    
    if ($bancoExiste) {
        # Renomear banco atual temporariamente
        Write-Host "Renomeando banco atual..." -ForegroundColor Yellow
        $bancoOld = Join-Path (Split-Path $caminhoBanco -Parent) "UNIFICASUS_OLD_$timestamp.GDB"
        Rename-Item -Path $caminhoBanco -NewName "UNIFICASUS_OLD_$timestamp.GDB" -ErrorAction Stop
    }
    
    # Copiar backup para o local do banco
    Write-Host "Copiando backup para o local do banco..." -ForegroundColor Yellow
    $diretorioDestino = Split-Path $caminhoBanco -Parent
    if (-not (Test-Path $diretorioDestino)) {
        New-Item -ItemType Directory -Path $diretorioDestino -Force | Out-Null
    }
    
    Copy-Item -Path $CaminhoBackup -Destination $caminhoBanco -Force
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Green
    Write-Host "RESTAURAÇÃO CONCLUÍDA COM SUCESSO!" -ForegroundColor Green
    Write-Host "============================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Banco restaurado: $caminhoBanco" -ForegroundColor Green
    if ($bancoExiste) {
        Write-Host "Backup do banco anterior salvo em: $caminhoBackupAtual" -ForegroundColor Green
        Write-Host "Banco antigo renomeado para: UNIFICASUS_OLD_$timestamp.GDB" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Se mapeou unidade, desconectar
    if ($driveLetter) {
        Write-Host "Desconectando unidade de rede mapeada..." -ForegroundColor Yellow
        net use $driveLetter /delete 2>$null | Out-Null
    }
    
    # Reiniciar aplicação se solicitado
    if ($ReiniciarAplicacao) {
        Write-Host "============================================================================" -ForegroundColor Cyan
        Write-Host "REINICIANDO APLICAÇÃO" -ForegroundColor Cyan
        Write-Host "============================================================================" -ForegroundColor Cyan
        Write-Host ""
        
        # Fechar processos da aplicação em execução
        Write-Host "Fechando processos da aplicação em execução..." -ForegroundColor Yellow
        $processos = Get-Process -Name "UnificaSUS.WPF" -ErrorAction SilentlyContinue
        if ($processos) {
            foreach ($proc in $processos) {
                Write-Host "Fechando processo: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Yellow
                try {
                    $proc.CloseMainWindow()
                    Start-Sleep -Seconds 2
                    if (-not $proc.HasExited) {
                        $proc.Kill()
                    }
                }
                catch {
                    Write-Host "Aviso: Não foi possível fechar o processo graciosamente. Tentando forçar..." -ForegroundColor Yellow
                    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                }
            }
            Start-Sleep -Seconds 2
        }
        else {
            Write-Host "Nenhum processo da aplicação encontrado em execução." -ForegroundColor Green
        }
        Write-Host ""
        
        # Aguardar um pouco antes de reiniciar
        Write-Host "Aguardando 3 segundos antes de reiniciar..." -ForegroundColor Yellow
        Start-Sleep -Seconds 3
        
        # Reiniciar aplicação
        $projetoPath = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
        $scriptExecutar = Join-Path $projetoPath "EXECUTAR_APLICACAO.ps1"
        
        if (Test-Path $scriptExecutar) {
            Write-Host "Iniciando aplicação usando: $scriptExecutar" -ForegroundColor Green
            Start-Process powershell.exe -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$scriptExecutar`"" -WorkingDirectory $projetoPath
        }
        else {
            # Tentar executar diretamente
            $exePath = Join-Path $projetoPath "src\UnificaSUS.WPF\bin\Debug\net8.0-windows\UnificaSUS.WPF.exe"
            if (Test-Path $exePath) {
                Write-Host "Iniciando aplicação: $exePath" -ForegroundColor Green
                Start-Process $exePath -WorkingDirectory (Split-Path $exePath -Parent)
            }
            else {
                Write-Host "AVISO: Não foi possível encontrar o executável da aplicação." -ForegroundColor Yellow
                Write-Host "Por favor, inicie a aplicação manualmente." -ForegroundColor Yellow
            }
        }
        Write-Host ""
        Write-Host "Aplicação reiniciada!" -ForegroundColor Green
    }
    else {
        Write-Host "IMPORTANTE: Reinicie a aplicação manualmente para usar o banco restaurado." -ForegroundColor Yellow
        Write-Host "Ou execute o script com -ReiniciarAplicacao para reiniciar automaticamente." -ForegroundColor Yellow
    }
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "ERRO durante a restauração: $_" -ForegroundColor Red
    
    # Tentar restaurar o banco original se houver erro
    if ($bancoExiste) {
        $bancoOld = Join-Path (Split-Path $caminhoBanco -Parent) "UNIFICASUS_OLD_$timestamp.GDB"
        if (Test-Path $bancoOld) {
            Write-Host "Tentando restaurar banco original..." -ForegroundColor Yellow
            try {
                if (Test-Path $caminhoBanco) {
                    Remove-Item $caminhoBanco -Force
                }
                Rename-Item -Path $bancoOld -NewName (Split-Path $caminhoBanco -Leaf) -ErrorAction Stop
                Write-Host "Banco original restaurado." -ForegroundColor Green
            }
            catch {
                Write-Host "ERRO ao restaurar banco original. Use o backup manual: $caminhoBackupAtual" -ForegroundColor Red
            }
        }
    }
    
    # Se mapeou unidade, desconectar
    if ($driveLetter) {
        net use $driveLetter /delete 2>$null | Out-Null
    }
    
    exit 1
}
