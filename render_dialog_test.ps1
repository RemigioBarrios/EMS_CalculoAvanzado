Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase,System.Drawing

$appDll = "C:\NEOSERV\FUENTES2\CALCULO\CalculusApp\bin\Release\net10.0-windows\CalculusApp.dll"
[System.Reflection.Assembly]::LoadFrom($appDll)

[CalculusApp.Services.LocalizationService]::SetLanguage([CalculusApp.Services.AppLanguage]::Spanish)
$topic = [CalculusApp.Generators.TopicRegistry]::GetById("deriv_avg_rates")
$dlg = New-Object CalculusApp.Dialogs.TopicOptionsDialog($topic, 4, [CalculusApp.Models.QuestionType]::FreeResponse)
$dlg.Width = 1060
$dlg.Height = 720
$dlg.Measure([System.Windows.Size]::new(1060, 720))
$dlg.Arrange([System.Windows.Rect]::new(0, 0, 1060, 720))
$dlg.UpdateLayout()

$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(1060, 720, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
$rtb.Render($dlg)

$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
$outPath = "c:\NEOSERV\FUENTES2\CALCULO\topic_dialog_avg_rates.png"
$fs = [System.IO.File]::Create($outPath)
$encoder.Save($fs)
$fs.Close()
Write-Host "Saved dialog screenshot to $outPath"
