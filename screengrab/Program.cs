using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr OpenWindowStation(string a, bool b, uint c);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetProcessWindowStation(IntPtr a);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr OpenDesktop(string a, uint b, bool c, uint d);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetThreadDesktop(IntPtr a);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [STAThread]
    static void Main()
    {
        try
        {
            IntPtr hWinsta = OpenWindowStation("WinSta0", false, 0x037F);
            if (hWinsta != IntPtr.Zero)
            {
                SetProcessWindowStation(hWinsta);
                IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
                if (hDesk != IntPtr.Zero)
                {
                    SetThreadDesktop(hDesk);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Desktop attach warning: " + ex.Message);
        }

        string targetExe = @"C:\Users\negro\Downloads\KUTA\TRIAL\ICA-Trial-3.26.12-x64\Calculus.exe";
        string outDir = @"c:\NEOSERV\FUENTES2\CALCULO\resources";

        Console.WriteLine("Starting Calculus.exe...");
        Process proc = Process.Start(new ProcessStartInfo
        {
            FileName = targetExe,
            WorkingDirectory = Path.GetDirectoryName(targetExe),
            UseShellExecute = true
        })!;

        Thread.Sleep(4000);
        IntPtr hwnd = proc.MainWindowHandle;
        Console.WriteLine("MainWindowHandle: " + hwnd);

        ShowWindow(hwnd, 3); // Maximize
        SetForegroundWindow(hwnd);
        Thread.Sleep(1000);

        try
        {
            var bounds = Screen.PrimaryScreen!.Bounds;
            using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            string path = Path.Combine(outDir, "calculus_screenshot_actual.png");
            bmp.Save(path, ImageFormat.Png);
            Console.WriteLine("Screenshot saved to: " + path);
        }
        catch (Exception ex)
        {
            Console.WriteLine("CopyFromScreen error: " + ex);
        }

        if (!proc.HasExited)
        {
            proc.Kill();
        }
    }
}
