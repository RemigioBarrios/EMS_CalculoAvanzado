Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$targetExe = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$resDir = "c:\NEOSERV\FUENTES2\CALCULO\resources"

$source = @"
using System;
using System.Drawing;
using System.Runtime.InteropServices;

public class WindowCapturer {
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    public static Bitmap Capture(IntPtr hwnd) {
        RECT rc;
        GetWindowRect(hwnd, out rc);
        int w = rc.Right - rc.Left;
        int h = rc.Bottom - rc.Top;
        if (w <= 0 || h <= 0) return null;
        Bitmap bmp = new Bitmap(w, h);
        using (Graphics g = Graphics.FromImage(bmp)) {
            IntPtr hdc = g.GetHdc();
            try {
                // Try 2 (PW_RENDERFULLCONTENT), fallback to 0
                if (!PrintWindow(hwnd, hdc, 2)) {
                    PrintWindow(hwnd, hdc, 0);
                }
            } finally {
                g.ReleaseHdc(hdc);
            }
        }
        return bmp;
    }
}
"@
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing

Write-Host "Starting Calculus.exe..."
$proc = Start-Process -FilePath $targetExe -PassThru
Start-Sleep -Seconds 4

$hwnd = $proc.MainWindowHandle
Write-Host "Window Handle: $hwnd"

if ($hwnd -ne [IntPtr]::Zero) {
    $bmp = [WindowCapturer]::Capture($hwnd)
    if ($bmp -ne $null) {
        $savePath = Join-Path $resDir "kuta_calculus_window.png"
        $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "Window captured successfully to $savePath"
    } else {
        Write-Host "Failed to capture window."
    }
}

if (-not $proc.HasExited) {
    $proc.Kill()
}
