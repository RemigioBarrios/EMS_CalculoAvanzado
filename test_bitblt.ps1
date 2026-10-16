$source = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class GdiCapture {
    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    public const int SRCCOPY = 0x00CC0020;

    public static Bitmap CaptureWindow(IntPtr hWnd) {
        RECT rect;
        GetWindowRect(hWnd, out rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        IntPtr hdcSrc = GetDC(hWnd);
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) {
            IntPtr hdcDest = g.GetHdc();
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            g.ReleaseHdc(hdcDest);
        }
        ReleaseDC(hWnd, hdcSrc);
        return bmp;
    }
}
"@
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing

$targetExe = "C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe"
$resDir = "c:\NEOSERV\FUENTES2\CALCULO\resources"

$proc = Start-Process -FilePath $targetExe -PassThru
Start-Sleep -Seconds 3

$hwnd = $proc.MainWindowHandle
[GdiCapture]::ShowWindow($hwnd, 9) # SW_RESTORE
[GdiCapture]::SetForegroundWindow($hwnd)
Start-Sleep -Seconds 1

$bmp = [GdiCapture]::CaptureWindow($hwnd)
if ($bmp) {
    $bmp.Save((Join-Path $resDir "calculus_captured_gdi.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Success! calculus_captured_gdi.png created."
} else {
    Write-Host "Failed to capture window."
}

if (-not $proc.HasExited) {
    $proc.Kill()
}
