Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$targetExe = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$outDir = "c:\NEOSERV\FUENTES2\CALCULO"
$resDir = Join-Path $outDir "resources"
if (-not (Test-Path $resDir)) {
    New-Item -ItemType Directory -Force -Path $resDir | Out-Null
}

# 1. Extract application icon
try {
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($targetExe)
    if ($icon -ne $null) {
        $bmp = $icon.ToBitmap()
        $bmp.Save((Join-Path $resDir "app_icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $icon.Save((Join-Path $resDir "app_icon.ico"))
        Write-Host "Icon extracted successfully"
    }
} catch {
    Write-Host "Error extracting icon: $_"
}

# 2. Extract strings from binary to find topics, menus, and features
$rawBytes = [System.IO.File]::ReadAllBytes($targetExe)
$asciiStrings = [System.Text.RegularExpressions.Regex]::Matches([System.Text.Encoding]::ASCII.GetString($rawBytes), "[\x20-\x7E]{4,}") | 
    ForEach-Object { $_.Value }

# Filter interesting strings related to topics, calculus, worksheets, UI
$interesting = $asciiStrings | Where-Object { 
    $_ -match "limit|derivative|integral|tangent|continuity|series|polynomial|asymptote|critical|inflection|worksheet|question|answer|kuta|calculus|equation|graph" -and
    $_ -notmatch "[\\/]{2,}" -and
    $_.Length -lt 80
} | Select-Object -Unique

$interesting | Out-File -FilePath (Join-Path $resDir "calculus_strings.txt") -Encoding utf8
Write-Host "Extracted $($interesting.Count) relevant strings to calculus_strings.txt"

# 3. Launch Calculus.exe, capture screenshot and inspect UI automation tree
Write-Host "Launching Calculus.exe to capture UI and take screenshot..."
$proc = Start-Process -FilePath $targetExe -PassThru

Start-Sleep -Seconds 4

try {
    $hwnd = $proc.MainWindowHandle
    if ($hwnd -eq 0) {
        # Retry finding handle
        $proc.Refresh()
        $hwnd = $proc.MainWindowHandle
    }
    Write-Host "Main window handle: $hwnd"

    # UI Automation Root Element
    $rootElement = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    
    if ($rootElement -ne $null) {
        Write-Host "Window Title: $($rootElement.Current.Name)"
        $rect = $rootElement.Current.BoundingRectangle

        # Take screenshot of the window
        $bounds = [System.Drawing.Rectangle]::new([int]$rect.X, [int]$rect.Y, [int]$rect.Width, [int]$rect.Height)
        if ($bounds.Width -gt 50 -and $bounds.Height -gt 50) {
            $bmp = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
            $screenshotPath = Join-Path $resDir "calculus_screenshot.png"
            $bmp.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
            $g.Dispose()
            $bmp.Dispose()
            Write-Host "Screenshot saved to $screenshotPath"
        }

        # Dump UI elements
        $allDescendants = $rootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition
        )
        
        $uiElements = @()
        foreach ($el in $allDescendants) {
            $uiElements += [PSCustomObject]@{
                Name = $el.Current.Name
                ControlType = $el.Current.ControlType.ProgrammaticName
                ClassName = $el.Current.ClassName
                AutomationId = $el.Current.AutomationId
                BoundingRectangle = "$($el.Current.BoundingRectangle.X),$($el.Current.BoundingRectangle.Y),$($el.Current.BoundingRectangle.Width),$($el.Current.BoundingRectangle.Height)"
                IsEnabled = $el.Current.IsEnabled
                IsOffscreen = $el.Current.IsOffscreen
            }
        }
        
        $uiElements | Export-Csv -Path (Join-Path $resDir "ui_elements.csv") -NoTypeInformation -Encoding utf8
        Write-Host "Dumped $($uiElements.Count) UI elements to ui_elements.csv"
    } else {
        Write-Host "Could not get root automation element."
    }
} catch {
    Write-Host "UI Automation error: $_"
} finally {
    # Keep process for a few seconds or close if needed
    # We will close the trial process cleanly
    Start-Sleep -Seconds 2
    if (-not $proc.HasExited) {
        $proc.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 1
        if (-not $proc.HasExited) {
            $proc.Kill()
        }
    }
    Write-Host "Process cleanup complete."
}
