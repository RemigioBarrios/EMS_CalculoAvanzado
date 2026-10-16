$exePath = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$outDir = "c:\NEOSERV\FUENTES2\CALCULO\resources\extracted"
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$bytes = [System.IO.File]::ReadAllBytes($exePath)
$text = [System.Text.Encoding]::UTF8.GetString($bytes)

# Extract SVGs
$svgPattern = "(?s)<svg[^>]*>.*?</svg>"
$matches = [System.Text.RegularExpressions.Regex]::Matches($text, $svgPattern)
Write-Host "Found $($matches.Count) embedded SVG graphics!"

$idx = 0
foreach ($m in $matches) {
    $idx++
    $svgContent = $m.Value
    # Try to identify name from content or id
    $fileName = "icon_$idx.svg"
    if ($svgContent -match 'id="([^"]+)"') {
        $cleanId = $matches[1] -replace '[^a-zA-Z0-9_\-]', '_'
        $fileName = "$cleanId.svg"
    }
    $targetPath = Join-Path $outDir $fileName
    [System.IO.File]::WriteAllText($targetPath, $svgContent, [System.Text.Encoding]::UTF8)
}

# Also search for PNG signatures: 89 50 4E 47 0D 0A 1A 0A
$pngSig = [byte[]]@(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
$pngCount = 0
for ($i = 0; $i -lt ($bytes.Length - 8); $i++) {
    $matched = $true
    for ($j = 0; $j -lt 8; $j++) {
        if ($bytes[$i + $j] -ne $pngSig[$j]) {
            $matched = $false
            break
        }
    }
    if ($matched) {
        # Find IEND
        $iendSig = [byte[]]@(0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82)
        $endPos = -1
        for ($k = $i + 8; $k -lt [Math]::Min($bytes.Length - 8, $i + 500000); $k++) {
            $iendMatch = $true
            for ($m = 0; $m -lt 8; $m++) {
                if ($bytes[$k + $m] -ne $iendSig[$m]) {
                    $iendMatch = $false
                    break
                }
            }
            if ($iendMatch) {
                $endPos = $k + 8
                break
            }
        }
        if ($endPos -gt $i) {
            $pngCount++
            $len = $endPos - $i
            $pngBytes = New-Object byte[] $len
            [Array]::Copy($bytes, $i, $pngBytes, 0, $len)
            $pngFile = Join-Path $outDir "extracted_$pngCount.png"
            [System.IO.File]::WriteAllBytes($pngFile, $pngBytes)
            $i = $endPos
        }
    }
}
Write-Host "Found $pngCount embedded PNGs!"
