Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$targetExe = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$resDir = "c:\NEOSERV\FUENTES2\CALCULO\resources"
if (-not (Test-Path $resDir)) {
    New-Item -ItemType Directory -Force -Path $resDir | Out-Null
}

# 1. Save Icons
try {
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($targetExe)
    if ($icon) {
        $fs = [System.IO.File]::Create((Join-Path $resDir "app_icon.ico"))
        $icon.Save($fs)
        $fs.Close()
        $bmp = $icon.ToBitmap()
        $bmp.Save((Join-Path $resDir "app_icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "Icons saved successfully."
    }
} catch {
    Write-Host "Icon extraction error: $_"
}

# Win32 helpers for window capture
$code = @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;

public class Win32 {
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

# 2. Start Calculus.exe
Write-Host "Starting Calculus.exe..."
$proc = Start-Process -FilePath $targetExe -PassThru

# Monitor windows for 15 seconds
$captured = $false
for ($i = 0; $i -lt 15; $i++) {
    Start-Sleep -Seconds 1
    $proc.Refresh()
    
    $procId = $proc.Id
    $wndList = New-Object System.Collections.Generic.List[IntPtr]

    [Win32]::EnumWindows({
        param($hwnd, $lParam)
        $winPid = 0
        [Win32]::GetWindowThreadProcessId($hwnd, [ref]$winPid) | Out-Null
        if ($winPid -eq $procId -and [Win32]::IsWindowVisible($hwnd)) {
            $wndList.Add($hwnd)
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null

    Write-Host "Found $($wndList.Count) visible windows for PID $procId at iteration $i"
    
    foreach ($w in $wndList) {
        $sb = New-Object System.Text.StringBuilder 256
        [Win32]::GetWindowText($w, $sb, 256) | Out-Null
        $wTitle = $sb.ToString()
        
        $rect = New-Object Win32+RECT
        [Win32]::GetWindowRect($w, [ref]$rect) | Out-Null
        $width = $rect.Right - $rect.Left
        $height = $rect.Bottom - $rect.Top

        Write-Host "Window: Handle=$w, Title='$wTitle', Size=($width x $height) at ($($rect.Left),$($rect.Top))"

        if ($width -gt 100 -and $height -gt 100) {
            # Bring to front
            [Win32]::SetForegroundWindow($w) | Out-Null
            Start-Sleep -Milliseconds 200

            # Take screenshot
            $bounds = [System.Drawing.Rectangle]::new($rect.Left, $rect.Top, $width, $height)
            $bmp = New-Object System.Drawing.Bitmap $width, $height
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
            $scrFile = Join-Path $resDir "kuta_window_$($w.ToString('X'))_$i.png"
            $bmp.Save($scrFile, [System.Drawing.Imaging.ImageFormat]::Png)
            $g.Dispose()
            $bmp.Dispose()
            Write-Host "Saved screenshot: $scrFile"
            $captured = $true

            # Try UI Automation on this window
            try {
                $el = [System.Windows.Automation.AutomationElement]::FromHandle($w)
                if ($el) {
                    Write-Host "UIA Root Name: $($el.Current.Name), Type: $($el.Current.ControlType.ProgrammaticName)"
                    
                    # If this is a trial dialog, find "Continue" or "OK" or "Try" buttons
                    $buttons = $el.FindAll(
                        [System.Windows.Automation.TreeScope]::Descendants,
                        (New-Object System.Windows.Automation.PropertyCondition(
                            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                            [System.Windows.Automation.ControlType]::Button
                        ))
                    )
                    
                    foreach ($btn in $buttons) {
                        Write-Host "Button found: '$($btn.Current.Name)', Id='$($btn.Current.AutomationId)'"
                        if ($btn.Current.Name -match "Continue|Try|Eval|OK|Close|Agree") {
                            Write-Host "Clicking button: $($btn.Current.Name)"
                            try {
                                $invokePattern = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                                $invokePattern.Invoke()
                                Write-Host "Button invoked!"
                            } catch {
                                Write-Host "Invoke failed: $_"
                            }
                        }
                    }

                    # Dump elements
                    $descendants = $el.FindAll(
                        [System.Windows.Automation.TreeScope]::Descendants,
                        [System.Windows.Automation.Condition]::TrueCondition
                    )
                    $arr = @()
                    foreach ($d in $descendants) {
                        $arr += [PSCustomObject]@{
                            Name = $d.Current.Name
                            ControlType = $d.Current.ControlType.ProgrammaticName
                            ClassName = $d.Current.ClassName
                            AutomationId = $d.Current.AutomationId
                            Rect = "$($d.Current.BoundingRectangle.X),$($d.Current.BoundingRectangle.Y),$($d.Current.BoundingRectangle.Width),$($d.Current.BoundingRectangle.Height)"
                        }
                    }
                    $arr | Export-Csv -Path (Join-Path $resDir "ui_tree_$($w.ToString('X')).csv") -NoTypeInformation -Encoding utf8
                    Write-Host "Dumped $($arr.Count) elements for window $w"
                }
            } catch {
                Write-Host "UIA Error: $_"
            }
        }
    }
}

# Cleanup
if (-not $proc.HasExited) {
    Write-Host "Stopping process..."
    $proc.Kill()
}
Write-Host "Done capture script."
