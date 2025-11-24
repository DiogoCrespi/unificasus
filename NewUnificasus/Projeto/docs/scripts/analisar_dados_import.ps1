# Analisa os dados reais nos arquivos de importação

$encoding = [System.Text.Encoding]::GetEncoding('ISO-8859-1')

Write-Host "=== ANÁLISE TB_RUBRICA ===" -ForegroundColor Cyan
$rubricaLines = Get-Content "C:\Program Files\claupers\unificasus\TabelaUnificada_202510_v2510160954\tb_rubrica.txt" -Encoding Default
$rubricaLine = $rubricaLines[0]
$rubricaBytes = $encoding.GetBytes($rubricaLine)

Write-Host "Linha completa: $($rubricaLine.Length) caracteres, $($rubricaBytes.Length) bytes"
Write-Host ""

$coRubrica = $rubricaLine.Substring(0, 6)
$noRubrica = $rubricaLine.Substring(6, 100)
$dtCompetencia = $rubricaLine.Substring(106, 6)

$coRubricaBytes = $encoding.GetBytes($coRubrica)
$noRubricaBytes = $encoding.GetBytes($noRubrica)
$dtCompetenciaBytes = $encoding.GetBytes($dtCompetencia)

Write-Host "CO_RUBRICA: '$coRubrica'"
Write-Host "  - Caracteres: $($coRubrica.Length)"
Write-Host "  - Bytes: $($coRubricaBytes.Length)"
Write-Host "  - Esperado: 6 bytes"
Write-Host ""

Write-Host "NO_RUBRICA: '$noRubrica'"
Write-Host "  - Caracteres: $($noRubrica.Length)"
Write-Host "  - Bytes: $($noRubricaBytes.Length)"
Write-Host "  - Esperado: 100 bytes"
Write-Host ""

Write-Host "DT_COMPETENCIA: '$dtCompetencia'"
Write-Host "  - Caracteres: $($dtCompetencia.Length)"
Write-Host "  - Bytes: $($dtCompetenciaBytes.Length)"
Write-Host "  - Esperado: 6 bytes"
Write-Host ""

Write-Host "=== ANÁLISE TB_CID (linha 70) ===" -ForegroundColor Cyan
$cidLines = Get-Content "C:\Program Files\claupers\unificasus\TabelaUnificada_202510_v2510160954\tb_cid.txt" -Encoding Default
$cidLine = $cidLines[69]  # Linha 70 (índice 69)
$cidBytes = $encoding.GetBytes($cidLine)

Write-Host "Linha completa: $($cidLine.Length) caracteres, $($cidBytes.Length) bytes"
Write-Host ""

$coCid = $cidLine.Substring(0, 4)
$noCid = $cidLine.Substring(4, 100)
$tpAgravo = $cidLine.Substring(104, 1)
$tpSexo = $cidLine.Substring(105, 1)
$tpEstadio = $cidLine.Substring(106, 1)

$coCidBytes = $encoding.GetBytes($coCid)
$noCidBytes = $encoding.GetBytes($noCid)
$tpAgravoBytes = $encoding.GetBytes($tpAgravo)
$tpSexoBytes = $encoding.GetBytes($tpSexo)
$tpEstadioBytes = $encoding.GetBytes($tpEstadio)

Write-Host "CO_CID: '$coCid'"
Write-Host "  - Caracteres: $($coCid.Length)"
Write-Host "  - Bytes: $($coCidBytes.Length)"
Write-Host "  - Esperado: 4 bytes"
Write-Host ""

Write-Host "NO_CID: '$noCid'"
Write-Host "  - Caracteres: $($noCid.Length)"
Write-Host "  - Bytes: $($noCidBytes.Length)"
Write-Host "  - Esperado: 100 bytes"
Write-Host ""

Write-Host "TP_AGRAVO: '$tpAgravo'"
Write-Host "  - Caracteres: $($tpAgravo.Length)"
Write-Host "  - Bytes: $($tpAgravoBytes.Length)"
Write-Host "  - Esperado: 1 byte"
Write-Host ""

Write-Host "TP_SEXO: '$tpSexo'"
Write-Host "  - Caracteres: $($tpSexo.Length)"
Write-Host "  - Bytes: $($tpSexoBytes.Length)"
Write-Host "  - Esperado: 1 byte"
Write-Host ""

Write-Host "TP_ESTADIO: '$tpEstadio'"
Write-Host "  - Caracteres: $($tpEstadio.Length)"
Write-Host "  - Bytes: $($tpEstadioBytes.Length)"
Write-Host "  - Esperado: 1 byte"
Write-Host ""

