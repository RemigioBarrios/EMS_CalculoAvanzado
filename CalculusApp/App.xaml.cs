using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CalculusApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        File.WriteAllText("args.txt", "Count: " + e.Args.Length + " -> " + string.Join(" ", e.Args));

        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--screenshot" && i + 1 < e.Args.Length)
            {
                string targetPath = e.Args[i + 1];
                try
                {

                    var win = new MainWindow();
                    win.Width = 1350;
                    win.Height = 850;
                    win.Show();

                    void SaveSnapshot(string filePath)
                    {
                        win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                        win.UpdateLayout();
                        var rtb = new RenderTargetBitmap(1350, 850, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(win);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        using var fs = File.Create(filePath);
                        encoder.Save(fs);
                    }

                    // 1. Spanish (Default)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    SaveSnapshot(targetPath);

                    // 2. Answer overlay (Spanish)
                    win.ViewModel.ShowAnswers = true;
                    string ansPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_app_answers.png");
                    SaveSnapshot(ansPath);

                    // 3. English
                    win.ViewModel.ShowAnswers = false;
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.English);
                    string enPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_app_en.png");
                    SaveSnapshot(enPath);

                    // 4. Portuguese
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Portuguese);
                    string ptPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_app_pt.png");
                    SaveSnapshot(ptPath);

                    // 5. Topic Options Dialog Snapshot (English - Absolute Extrema 1:1 match to reference screenshot)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.English);
                    var topicExtrema = Generators.TopicRegistry.GetById("app_abs_extrema") ?? Generators.TopicRegistry.AllTopics[0];
                    var dlg = new Dialogs.TopicOptionsDialog(topicExtrema, 4, Models.QuestionType.FreeResponse);
                    dlg.Width = 920;
                    dlg.Height = 705;
                    dlg.Show();
                    dlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlg.UpdateLayout();
                    var rtbDlg = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlg.Render(dlg);
                    string dlgPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog.png");
                    var encoderDlg = new PngBitmapEncoder();
                    encoderDlg.Frames.Add(BitmapFrame.Create(rtbDlg));
                    using (var fs = File.Create(dlgPath))
                    {
                        encoderDlg.Save(fs);
                    }
                    dlg.Close();

                    // 6. Topic Options Dialog Snapshot (Spanish - Limits Direct with Polynomial + Trig, exactly reproducing user's screenshot)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var topicLimits = Generators.TopicRegistry.GetById("limits_direct") ?? Generators.TopicRegistry.AllTopics[0];
                    var limitsParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        IncludeTrig = true,
                        IncludeRational = false,
                        IncludeRadical = false,
                        IncludeAbs = false,
                        IncludeExp = false
                    };
                    var dlgEs = new Dialogs.TopicOptionsDialog(topicLimits, 4, Models.QuestionType.FreeResponse, limitsParams);
                    dlgEs.Width = 920;
                    dlgEs.Height = 705;
                    dlgEs.Show();
                    dlgEs.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgEs.UpdateLayout();
                    var rtbDlgEs = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlgEs.Render(dlgEs);
                    string dlgEsPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_es.png");
                    var encoderDlgEs = new PngBitmapEncoder();
                    encoderDlgEs.Frames.Add(BitmapFrame.Create(rtbDlgEs));
                    using (var fs = File.Create(dlgEsPath))
                    {
                        encoderDlgEs.Save(fs);
                    }
                    dlgEs.Close();

                    // 7. Composite Worksheet Snapshot with Answers (Limits and Derivatives combining Poly + Trig)
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("limits_direct", 4, Models.QuestionType.FreeResponse, limitsParams);
                    var derivParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        IncludeTrig = true
                    };
                    win.ViewModel.AddTopic("deriv_power", 4, Models.QuestionType.FreeResponse, derivParams);
                    win.ViewModel.ShowAnswers = true;
                    string compositePath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_composite_worksheet.png");
                    SaveSnapshot(compositePath);

                    // 8. Test PDF Export
                    string pdfPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_exported.pdf");
                    Services.PdfExportService.ExportToPdf(win.WorksheetCanvas, pdfPath);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(targetPath)!, "pdf_export_ok.txt"), "PDF size: " + new FileInfo(pdfPath).Length);

                    // 9. Topic Options Dialog Snapshot with Function Graph Enabled
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var graphParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        ProvideFunctionGraph = true,
                        ProvideBlankGraph = false
                    };
                    var dlgGraph = new Dialogs.TopicOptionsDialog(topicLimits, 4, Models.QuestionType.FreeResponse, graphParams);
                    dlgGraph.Width = 920;
                    dlgGraph.Height = 705;
                    dlgGraph.Show();
                    dlgGraph.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgGraph.UpdateLayout();
                    var rtbDlgGraph = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlgGraph.Render(dlgGraph);
                    string dlgGraphPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_graph.png");
                    var encGraph = new PngBitmapEncoder();
                    encGraph.Frames.Add(BitmapFrame.Create(rtbDlgGraph));
                    using (var fs = File.Create(dlgGraphPath))
                    {
                        encGraph.Save(fs);
                    }
                    dlgGraph.Close();

                    // 10. Exam Worksheet Snapshot with Graphs (Both Function Graph and Blank Graph)
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    var examGraphParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 2,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        ProvideFunctionGraph = true
                    };
                    win.ViewModel.AddTopic("limits_direct", 2, Models.QuestionType.FreeResponse, examGraphParams);

                    var examBlankParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 2,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        ProvideBlankGraph = true
                    };
                    win.ViewModel.AddTopic("deriv_power", 2, Models.QuestionType.FreeResponse, examBlankParams);

                    win.ViewModel.ShowAnswers = false;
                    string examGraphsPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_exam_with_graphs.png");
                    SaveSnapshot(examGraphsPath);

                    // 11. Topic Options Dialog Snapshot for "Tasa de cambio promedio" (deriv_avg_rates)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var topicAvgRates = Generators.TopicRegistry.GetById("deriv_avg_rates")!;
                    var avgRatesParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludePolynomial = true,
                        IncludeTrig = true,
                        IncludeRational = false
                    };
                    var dlgAvgRates = new Dialogs.TopicOptionsDialog(topicAvgRates, 4, Models.QuestionType.FreeResponse, avgRatesParams);
                    dlgAvgRates.Width = 1060;
                    dlgAvgRates.Height = 720;
                    dlgAvgRates.Show();
                    dlgAvgRates.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgAvgRates.UpdateLayout();
                    var rtbAvgRates = new RenderTargetBitmap(1060, 720, 96, 96, PixelFormats.Pbgra32);
                    rtbAvgRates.Render(dlgAvgRates);
                    string avgRatesDlgPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_avg_rates.png");
                    var encAvgRates = new PngBitmapEncoder();
                    encAvgRates.Frames.Add(BitmapFrame.Create(rtbAvgRates));
                    using (var fs = File.Create(avgRatesDlgPath))
                    {
                        encAvgRates.Save(fs);
                    }
                    dlgAvgRates.Close();

                    // 12. Worksheet with "Tasa de cambio promedio" and step-by-step answers
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("deriv_avg_rates", 4, Models.QuestionType.FreeResponse, avgRatesParams);
                    win.ViewModel.ShowAnswers = true;
                    string avgRatesWorksheetPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_worksheet_avg_rates.png");
                    SaveSnapshot(avgRatesWorksheetPath);

                    // 13. Riemann Sum Tables Dialog Snapshot (Spanish - Left Endpoint LRAM reproducing user's exact screenshot)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var topicRiemann = Generators.TopicRegistry.GetById("def_riemann_tables")!;
                    var riemannParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        ComplexityOptionIndex = 0 // Extremo izquierdo (LRAM)
                    };
                    var dlgRiemann = new Dialogs.TopicOptionsDialog(topicRiemann, 4, Models.QuestionType.FreeResponse, riemannParams);
                    dlgRiemann.Width = 1060;
                    dlgRiemann.Height = 720;
                    dlgRiemann.Show();
                    dlgRiemann.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgRiemann.UpdateLayout();
                    var rtbRiemann = new RenderTargetBitmap(1060, 720, 96, 96, PixelFormats.Pbgra32);
                    rtbRiemann.Render(dlgRiemann);
                    string riemannDlgPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_riemann_tables_dialog.png");
                    var encRiemann = new PngBitmapEncoder();
                    encRiemann.Frames.Add(BitmapFrame.Create(rtbRiemann));
                    using (var fs = File.Create(riemannDlgPath))
                    {
                        encRiemann.Save(fs);
                    }
                    dlgRiemann.Close();

                    // 14. Riemann Sum Tables Worksheet with Answers and PDF Export
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("def_riemann_tables", 4, Models.QuestionType.FreeResponse, riemannParams);
                    win.ViewModel.ShowAnswers = true;
                    string riemannWorksheetPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_worksheet_riemann.png");
                    SaveSnapshot(riemannWorksheetPath);

                    // Hybrid PDF Export of Riemann worksheet
                    string hybridPdfPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_hybrid_exported.pdf");
                    Services.PdfExportService.ExportToPdf(win.WorksheetCanvas, hybridPdfPath);

                    // 15. Absolute Extrema test (Spanish, Quadratic, Closed Interval)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var topicAbsExtrema = Generators.TopicRegistry.GetById("app_abs_extrema") ?? Generators.TopicRegistry.AllTopics[0];
                    var absParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        IncludeQuadratic = true,
                        IncludeCubic = false,
                        IncludeQuartic = false,
                        IncludeRational = false,
                        ComplexityOptionIndex = 0 // Closed interval
                    };

                    var dlgAbs = new Dialogs.TopicOptionsDialog(topicAbsExtrema, 4, Models.QuestionType.FreeResponse, absParams);
                    dlgAbs.Width = 920;
                    dlgAbs.Height = 705;
                    dlgAbs.Show();
                    dlgAbs.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgAbs.UpdateLayout();
                    var rtbDlgAbs = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlgAbs.Render(dlgAbs);
                    string dlgAbsPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_abs_extrema.png");
                    var encoderDlgAbs = new PngBitmapEncoder();
                    encoderDlgAbs.Frames.Add(BitmapFrame.Create(rtbDlgAbs));
                    using (var fs = File.Create(dlgAbsPath))
                    {
                        encoderDlgAbs.Save(fs);
                    }
                    dlgAbs.Close();

                    // 15b. Definite Integration - Limits of Sums (matching user's screenshot exactly)
                    var topicLimitsSums = Generators.TopicRegistry.GetById("def_limits_sums") ?? Generators.TopicRegistry.AllTopics[0];
                    var limitsSumsParams = new Models.TopicGeneratorParameters
                    {
                        QuestionCount = 4,
                        QuestionType = Models.QuestionType.FreeResponse,
                        ComplexityOptionIndex = 4 // Cualquiera
                    };
                    var dlgLimitsSums = new Dialogs.TopicOptionsDialog(topicLimitsSums, 4, Models.QuestionType.FreeResponse, limitsSumsParams);
                    dlgLimitsSums.Width = 920;
                    dlgLimitsSums.Height = 705;
                    dlgLimitsSums.Show();
                    dlgLimitsSums.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgLimitsSums.UpdateLayout();
                    var rtbDlgLimitsSums = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlgLimitsSums.Render(dlgLimitsSums);
                    string dlgLimitsSumsPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_def_limits_sums.png");
                    var encLimitsSums = new PngBitmapEncoder();
                    encLimitsSums.Frames.Add(BitmapFrame.Create(rtbDlgLimitsSums));
                    using (var fs = File.Create(dlgLimitsSumsPath))
                    {
                        encLimitsSums.Save(fs);
                    }
                    dlgLimitsSums.Close();

                    // Generate Absolute Extrema Worksheet with answers and steps
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("app_abs_extrema", 4, Models.QuestionType.FreeResponse, absParams);
                    win.ViewModel.ShowAnswers = true;
                    string absWorksheetPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_worksheet_abs_extrema.png");
                    SaveSnapshot(absWorksheetPath);

                    // Multi-page document test (spans 3 pages)
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("app_abs_extrema", 6, Models.QuestionType.FreeResponse, absParams);
                    win.ViewModel.AddTopic("deriv_power", 6, Models.QuestionType.FreeResponse);
                    win.ViewModel.ShowAnswers = true;
                    string multiPdfPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_multipage_test.pdf");
                    Services.PdfExportService.ExportToPdf(win.WorksheetCanvas, multiPdfPath);

                    // 16. PDF Preview Dialog Snapshot
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var previewDlg = new Dialogs.PdfPreviewDialog(win.ViewModel.CurrentAssignment);
                    previewDlg.Width = 1160;
                    previewDlg.Height = 840;
                    previewDlg.Show();
                    previewDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    previewDlg.UpdateLayout();
                    var rtbPreview = new RenderTargetBitmap(1160, 840, 96, 96, PixelFormats.Pbgra32);
                    rtbPreview.Render(previewDlg);
                    string previewSnapPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_pdf_preview_dialog.png");
                    var encPreview = new PngBitmapEncoder();
                    encPreview.Frames.Add(BitmapFrame.Create(rtbPreview));
                    using (var fs = File.Create(previewSnapPath))
                    {
                        encPreview.Save(fs);
                    }
                    previewDlg.Close();
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(targetPath)!, "screenshot_error.txt"), ex.ToString());
                }

                Shutdown(0);
                return;
            }
        }

        new MainWindow().Show();
    }
}

