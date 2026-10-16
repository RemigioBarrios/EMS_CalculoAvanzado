Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$targetExe = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$resDir = "c:\NEOSERV\FUENTES2\CALCULO\resources"

$code = @"
using System;
using System.Runtime.InteropServices;
public class Win32Helper {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@
Add-Type -TypeDefinition $code

Write-Host "Launching Calculus.exe..."
$proc = Start-Process -FilePath $targetExe -PassThru
Start-Sleep -Seconds 4

$hwnd = $proc.MainWindowHandle
[Win32Helper]::ShowWindow($hwnd, 3) # SW_MAXIMIZE
[Win32Helper]::SetForegroundWindow($hwnd)
Start-Sleep -Seconds 1

function TakeScreenshot($filename) {
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(0, 0, 0, 0, $screen.Size)
    $path = Join-Path $resDir $filename
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "Screenshot saved to $path"
}

TakeScreenshot("calculus_empty_workspace.png")

# Now use UIAutomation to find topic items and double click or add questions
try {
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    $topicList = $root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            "QApplication.MainWindow.dockWidget.dockWidgetContents.splitter.layoutWidget1.listWidget"
        ))
    )
    
    if ($topicList) {
        Write-Host "Found topic list! Items:"
        $items = $topicList.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition
        )
        
        Write-Host "Topic count: $($items.Count)"
        $allTopics = @()
        foreach ($it in $items) {
            $allTopics += $it.Current.Name
        }
        $allTopics | Out-File (Join-Path $resDir "all_topics_list.txt") -Encoding utf8

        # Find a topic item like "Absolute extrema" or "Curve sketching"
        $targetItem = $null
        foreach ($it in $items) {
            if ($it.Current.Name -match "Absolute extrema|Power rule|Limits|Continuity") {
                $targetItem = $it
                break
            }
        }
        
        if ($targetItem) {
            Write-Host "Double clicking item: $($targetItem.Current.Name)"
            $r = $targetItem.Current.BoundingRectangle
            $clickX = [int]($r.X + $r.Width / 2)
            $clickY = [int]($r.Y + $r.Height / 2)
            [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($clickX, $clickY)
            
            # Send Enter key to activate
            $targetItem.SetFocus()
            [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
            Start-Sleep -Seconds 2
            
            # If a question count dialog appears, press Enter to accept default
            [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
            Start-Sleep -Seconds 2
            
            TakeScreenshot("calculus_with_questions.png")
        }
    }
} catch {
    Write-Host "Error interacting with topics: $_"
}

# Cleanup
if (-not $proc.HasExited) {
    $proc.Kill()
}
Write-Host "Capture completed."
