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
        try
        {
            Services.AppSettingsService.Load();
            if (Services.AppSettingsService.Current.Theme == "light")
            {
                Services.ThemeService.SetTheme(Services.AppTheme.Light);
            }
            else
            {
                Services.ThemeService.SetTheme(Services.AppTheme.Dark);
            }
        }
        catch { }
        Services.ThemeService.Initialize();
        var allArgs = new System.Collections.Generic.List<string>();
        allArgs.AddRange(Environment.GetCommandLineArgs());
        if (e.Args != null) allArgs.AddRange(e.Args);
        string[] args = allArgs.ToArray();
        try
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "args.txt");
            File.WriteAllText(logPath, "Count: " + args.Length + " -> " + string.Join(" ", args));
        }
        catch { }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--validate-formulas")
            {
                try
                {
                    var result = Services.FormulaIntegrityValidator.RunValidation();
                    string report = result.ToString();
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "formula_validation_report.txt"), report);
                    File.WriteAllText("formula_validation_report.txt", report);
                    Shutdown(result.FailedCount == 0 ? 0 : 1);
                    return;
                }
                catch (Exception ex)
                {
                    File.WriteAllText("formula_validation_report.txt", "VALIDATION ERROR: " + ex);
                    Shutdown(2);
                    return;
                }
            }

            if (args[i] == "--screenshot" && i + 1 < args.Length)
            {
                string targetPath = args[i + 1];
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

                    // 1. Spanish Dark Theme (matching User Image 1: Main window + AI Documentation Assistant)
                    Services.ThemeService.SetTheme(Services.AppTheme.Dark);
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    win.TabAsistente.IsChecked = true;
                    SaveSnapshot(targetPath);
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_app_dark.png"));

                    // Snapshot of Modo Examen (Digital / Continuo)
                    win.ViewModel.IsExamMode = true;
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_exam_mode_dark.png"));
                    win.ViewModel.IsPdfMode = true;

                    // 2. Thumbnails View (matching User Image 2: Main window + Page Thumbnails)
                    win.TabMiniatura.IsChecked = true;
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_app_thumbnails.png"));


                    // Revert to Light default
                    Services.ThemeService.SetTheme(Services.AppTheme.Light);

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

                    // 4b. Geometry Subject Snapshot
                    Core.SubjectRegistry.SetCurrentSubject("geometry");
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("geom_pythagorean", 4, Models.QuestionType.FreeResponse);
                    win.ViewModel.AddTopic("geom_area_polygons", 4, Models.QuestionType.MultipleChoice);
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_geometry.png"));

                    // 4c. Algebra Subject Snapshot
                    Core.SubjectRegistry.SetCurrentSubject("algebra");
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("alg_one_two_step", 4, Models.QuestionType.FreeResponse);
                    win.ViewModel.AddTopic("alg_factoring_simple", 4, Models.QuestionType.MultipleChoice);
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_algebra.png"));

                    // 4d. Linear Systems Subject Snapshot
                    Core.SubjectRegistry.SetCurrentSubject("linear_systems");
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("lin_matrix_mult", 4, Models.QuestionType.FreeResponse);
                    win.ViewModel.AddTopic("lin_det_2x2", 4, Models.QuestionType.MultipleChoice);
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_linear_systems.png"));

                    // 4e. Algebra II Subject Snapshot (Parsed from Algebra_II.ini)
                    Core.SubjectRegistry.SetCurrentSubject("algebra_ii");
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_algebra_ii.png"));

                    // 4f. Classic Menu Mode (Modo sin Ribbon)
                    win.ViewModel.IsRibbonMode = false;
                    SaveSnapshot(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_classic_menu.png"));
                    win.ViewModel.IsRibbonMode = true;

                    // Revert to Calculus
                    Core.SubjectRegistry.SetCurrentSubject("calculus");
                    win.ViewModel.CurrentAssignment.QuestionSets.Clear();
                    win.ViewModel.QuestionSets.Clear();
                    win.ViewModel.AddTopic("deriv_power", 4, Models.QuestionType.FreeResponse);
                    win.ViewModel.AddTopic("limits_direct", 4, Models.QuestionType.MultipleChoice);

                    // 5. Topic Options Dialog Snapshot (English - Absolute Extrema 1:1 match to reference screenshot)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.English);
                    var topicExtrema = Core.SubjectRegistry.GetTopicById("app_abs_extrema") ?? Core.SubjectRegistry.CurrentSubject.AllTopics[0];
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
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_light.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbDlg));
                        enc.Save(fs);
                    }
                    dlg.Close();

                    // Topic Options Dialog in Dark Theme
                    Services.ThemeService.SetTheme(Services.AppTheme.Dark);
                    var dlgDark = new Dialogs.TopicOptionsDialog(topicExtrema, 4, Models.QuestionType.FreeResponse);
                    dlgDark.Width = 920;
                    dlgDark.Height = 705;
                    dlgDark.Show();
                    dlgDark.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    dlgDark.UpdateLayout();
                    var rtbDlgDark = new RenderTargetBitmap(920, 705, 96, 96, PixelFormats.Pbgra32);
                    rtbDlgDark.Render(dlgDark);
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "calculus_topic_dialog_dark.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbDlgDark));
                        enc.Save(fs);
                    }
                    dlgDark.Close();
                    Services.ThemeService.SetTheme(Services.AppTheme.Light);

                    // 6. Topic Options Dialog Snapshot (Spanish - Limits Direct with Polynomial + Trig, exactly reproducing user's screenshot)
                    Services.LocalizationService.SetLanguage(Services.AppLanguage.Spanish);
                    var topicLimits = Core.SubjectRegistry.GetTopicById("limits_direct") ?? Core.SubjectRegistry.CurrentSubject.AllTopics[0];
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
                    var topicAvgRates = Core.SubjectRegistry.GetTopicById("deriv_avg_rates")!;
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
                    var topicRiemann = Core.SubjectRegistry.GetTopicById("def_riemann_tables")!;
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
                    var topicAbsExtrema = Core.SubjectRegistry.GetTopicById("app_abs_extrema") ?? Core.SubjectRegistry.CurrentSubject.AllTopics[0];
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
                    var topicLimitsSums = Core.SubjectRegistry.GetTopicById("def_limits_sums") ?? Core.SubjectRegistry.CurrentSubject.AllTopics[0];
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

                    // 17. Teacher PDF Extractor Dialog Snapshot (Light & Dark)
                    Services.ThemeService.SetTheme(Services.AppTheme.Light);
                    var extractorDlg = new Dialogs.TeacherPdfExtractorDialog(win.ViewModel);
                    extractorDlg.PdfPathTextBox.Text = multiPdfPath;
                    extractorDlg.Width = 1120;
                    extractorDlg.Height = 780;
                    extractorDlg.Show();
                    try
                    {
                        var frame = new System.Windows.Threading.DispatcherFrame();
                        var scanTask = extractorDlg.StartScanAsync();
                        _ = scanTask.ContinueWith(_ => frame.Continue = false);
                        System.Windows.Threading.Dispatcher.PushFrame(frame);
                    }
                    catch { }
                    extractorDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    extractorDlg.UpdateLayout();
                    var rtbExt = new RenderTargetBitmap(1120, 780, 96, 96, PixelFormats.Pbgra32);
                    rtbExt.Render(extractorDlg);
                    string extSnapPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "app_teacher_pdf_extractor.png");
                    var encExt = new PngBitmapEncoder();
                    encExt.Frames.Add(BitmapFrame.Create(rtbExt));
                    using (var fs = File.Create(extSnapPath))
                    {
                        encExt.Save(fs);
                    }

                    Services.ThemeService.SetTheme(Services.AppTheme.Dark);
                    extractorDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    extractorDlg.UpdateLayout();
                    var rtbExtDark = new RenderTargetBitmap(1120, 780, 96, 96, PixelFormats.Pbgra32);
                    rtbExtDark.Render(extractorDlg);
                    string extDarkSnapPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "app_teacher_pdf_extractor_dark.png");
                    var encExtDark = new PngBitmapEncoder();
                    encExtDark.Frames.Add(BitmapFrame.Create(rtbExtDark));
                    using (var fs = File.Create(extDarkSnapPath))
                    {
                        encExtDark.Save(fs);
                    }

                    // Also capture the Telemetry Report tab explicitly
                    extractorDlg.TelemetryReportTab.IsSelected = true;
                    extractorDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    extractorDlg.UpdateLayout();
                    var rtbExtReport = new RenderTargetBitmap(1120, 780, 96, 96, PixelFormats.Pbgra32);
                    rtbExtReport.Render(extractorDlg);
                    string extReportSnapPath = Path.Combine(Path.GetDirectoryName(targetPath)!, "app_teacher_pdf_extractor_report.png");
                    var encExtReport = new PngBitmapEncoder();
                    encExtReport.Frames.Add(BitmapFrame.Create(rtbExtReport));
                    using (var fs = File.Create(extReportSnapPath))
                    {
                        encExtReport.Save(fs);
                    }
                    extractorDlg.Close();

                    // 18. Student Tools Dialog Snapshot (Light & Dark)
                    Services.ThemeService.SetTheme(Services.AppTheme.Light);
                    var studentDlg = new Dialogs.StudentToolsDialog(
                        "Ruta de Estudio Personalizada",
                        "Diagnóstico de Competencias y Compensación de Fallas",
                        "• Unidad 1: Límites y Continuidad — Dominio 92% (Completado)\n• Unidad 2: Regla de la Cadena y Derivación Implícita — Dominio 68% (Refuerzo sugerido)\n• Unidad 3: Aplicaciones de la Derivada y Optimización — Práctica programada: Jueves 16:00",
                        "🚀 Iniciar Práctica Guiada",
                        () => { });
                    studentDlg.Width = 740;
                    studentDlg.Height = 520;
                    studentDlg.Show();
                    studentDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    studentDlg.UpdateLayout();
                    var rtbStudLight = new RenderTargetBitmap(740, 520, 96, 96, PixelFormats.Pbgra32);
                    rtbStudLight.Render(studentDlg);
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_student_tools_light.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbStudLight));
                        enc.Save(fs);
                    }
                    Services.ThemeService.SetTheme(Services.AppTheme.Dark);
                    studentDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    studentDlg.UpdateLayout();
                    var rtbStudDark = new RenderTargetBitmap(740, 520, 96, 96, PixelFormats.Pbgra32);
                    rtbStudDark.Render(studentDlg);
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_student_tools_dark.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbStudDark));
                        enc.Save(fs);
                    }
                    studentDlg.Close();

                    // 19. Institute Admin & Teacher Roster Dialog Snapshots (Light & Dark)
                    Services.ThemeService.SetTheme(Services.AppTheme.Dark);
                    var instDlg = new Dialogs.InstituteAdminDialog();
                    instDlg.Width = 860;
                    instDlg.Height = 560;
                    instDlg.Show();
                    instDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    instDlg.UpdateLayout();
                    var rtbInstDark = new RenderTargetBitmap(860, 560, 96, 96, PixelFormats.Pbgra32);
                    rtbInstDark.Render(instDlg);
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_institute_admin_dark.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbInstDark));
                        enc.Save(fs);
                    }
                    instDlg.Close();

                    var rosterDlg = new Dialogs.TeacherRosterDialog();
                    rosterDlg.Width = 860;
                    rosterDlg.Height = 560;
                    rosterDlg.Show();
                    rosterDlg.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    rosterDlg.UpdateLayout();
                    var rtbRosterDark = new RenderTargetBitmap(860, 560, 96, 96, PixelFormats.Pbgra32);
                    rtbRosterDark.Render(rosterDlg);
                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(targetPath)!, "app_teacher_roster_dark.png")))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtbRosterDark));
                        enc.Save(fs);
                    }
                    rosterDlg.Close();
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(targetPath)!, "screenshot_error_latest.txt"), ex.ToString());
                }

                Shutdown(0);
                return;
            }
        }

        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), ev.ExceptionObject.ToString()); } catch { }
        };
        DispatcherUnhandledException += (s, ev) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dispatcher_crash.txt"), ev.Exception.ToString()); } catch { }
        };

        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "Creating MainWindow...\n");
            var mainWindow = new MainWindow();
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "MainWindow constructed.\n");
            MainWindow = mainWindow;
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "MainWindow assigned.\n");
            mainWindow.Closed += (s, ev) =>
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "MainWindow.Closed event fired!\n");
            };
            try
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "Calling mainWindow.Show()...\n");
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
                mainWindow.Focus();
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "mainWindow.Show() returned.\n");
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "show_error.txt"), ex.ToString());
            }
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "MainWindow shown successfully.\n");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup_error.txt"), ex.ToString());
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), $"App.OnExit called with code: {e.ApplicationExitCode}\n");
        base.OnExit(e);
    }
}

