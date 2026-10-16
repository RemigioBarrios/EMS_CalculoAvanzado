$exePath = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$bytes = [System.IO.File]::ReadAllBytes($exePath)

# UTF-16 strings
$chars = [System.Text.Encoding]::Unicode.GetString($bytes)
$matchesUnicode = [System.Text.RegularExpressions.Regex]::Matches($chars, "[\u0020-\u007E]{4,100}") | ForEach-Object { $_.Value.Trim() }

# ASCII strings
$asciiChars = [System.Text.Encoding]::ASCII.GetString($bytes)
$matchesAscii = [System.Text.RegularExpressions.Regex]::Matches($asciiChars, "[\u0020-\u007E]{4,100}") | ForEach-Object { $_.Value.Trim() }

$all = ($matchesUnicode + $matchesAscii) | Select-Object -Unique | Where-Object { $_.Length -ge 4 }
$all | Out-File -FilePath "c:\NEOSERV\FUENTES2\CALCULO\resources\all_strings.txt" -Encoding utf8
Write-Output "Extracted $($all.Count) strings to all_strings.txt"
