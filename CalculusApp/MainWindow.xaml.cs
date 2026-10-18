using System;
using System.Windows;
using System.Windows.Controls;
using CalculusApp.Models;
using CalculusApp.Services;
using CalculusApp.ViewModels;
using CalculusApp.Generators;

namespace CalculusApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel.RequestRender += OnRequestRender;
            ViewModel.RequestExportPdf += OnRequestExportPdf;
            ViewModel.RequestPreviewPdf += OnRequestPreviewPdf;
            LocalizationService.LanguageChanged += SyncLanguageCombo;
            Loaded += MainWindow_Loaded;
        }

        private void OnRequestPreviewPdf()
        {
            var dlg = new Dialogs.PdfPreviewDialog(ViewModel.CurrentAssignment);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void OnRequestExportPdf()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Export to PDF",
                FileName = (ViewModel.CurrentAssignment?.Heading?.Title ?? "Calculus") + ".pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string path = PdfExportService.ExportToPdf(WorksheetCanvas, sfd.FileName);
                    string msg = LocalizationService.Get("pdf_export_success", path);
                    MessageBox.Show(msg, "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export PDF: " + ex.Message, "PDF Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SyncLanguageCombo();
            WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
        }

        private void OnRequestRender()
        {
            WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ToolBar toolBar)
            {
                var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
                if (overflowGrid != null)
                {
                    overflowGrid.Visibility = Visibility.Collapsed;
                }
                var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
                if (mainPanelBorder != null)
                {
                    mainPanelBorder.Margin = new Thickness(0);
                }
            }
        }

        private void OnTopicTreeDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedItem is TopicTreeItem item && !item.IsCategory)
            {
                var topic = TopicRegistry.GetById(item.TopicId);
                if (topic != null)
                {
                    var dlg = new Dialogs.TopicOptionsDialog(topic, 4, QuestionType.FreeResponse);
                    dlg.Owner = this;
                    dlg.QuestionsAdded += (count, type, parameters, acceptedQuestions) =>
                    {
                        ViewModel.AddTopic(item.TopicId, count, type, parameters, acceptedQuestions);
                    };
                    dlg.ShowDialog();
                }
            }
        }

        private void OnQuestionSetDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditSelectedQuestionSet();
        }

        private void OnEditSelectedQuestionSetClick(object sender, RoutedEventArgs e)
        {
            EditSelectedQuestionSet();
        }

        private void EditSelectedQuestionSet()
        {
            if (ViewModel?.SelectedQuestionSet == null) return;
            var qs = ViewModel.SelectedQuestionSet;
            var topic = TopicRegistry.GetById(qs.TopicId);
            if (topic != null)
            {
                var dlg = new Dialogs.TopicOptionsDialog(topic, qs.QuestionCount, qs.DefaultType, qs.Parameters);
                dlg.Owner = this;
                dlg.QuestionsAdded += (count, type, parameters, acceptedQuestions) =>
                {
                    qs.QuestionCount = count;
                    qs.DefaultType = type;
                    qs.Parameters = parameters;
                    if (!string.IsNullOrWhiteSpace(parameters.CustomDirections))
                    {
                        qs.Directions = parameters.CustomDirections;
                    }
                    if (acceptedQuestions != null && acceptedQuestions.Count > 0)
                    {
                        qs.Questions = acceptedQuestions;
                        ViewModel.RefreshView();
                    }
                    else
                    {
                        ViewModel.RegenerateSelectedSet();
                    }
                };
                dlg.ShowDialog();
            }
        }

        private void OnZoomSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi)
            {
                string text = cbi.Content?.ToString() ?? "100%";
                text = text.Replace("%", "").Trim();
                if (double.TryParse(text, out double percent))
                {
                    double factor = percent / 100.0;
                    WorksheetCanvas?.SetZoom(factor);
                    if (ViewModel != null) ViewModel.ZoomPercent = percent;
                }
            }
        }

        private void OnColumnsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi)
            {
                string text = cbi.Content?.ToString() ?? "2";
                if (int.TryParse(text, out int cols) && ViewModel != null)
                {
                    ViewModel.Columns = cols;
                }
            }
        }

        private void OnLanguageComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                var targetLang = lang switch
                {
                    "en" => AppLanguage.English,
                    "pt" => AppLanguage.Portuguese,
                    _ => AppLanguage.Spanish
                };
                if (LocalizationService.CurrentLanguage != targetLang)
                {
                    LocalizationService.SetLanguage(targetLang);
                }
            }
        }

        private void SyncLanguageCombo()
        {
            if (LanguageCombo == null) return;
            string targetTag = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.English => "en",
                AppLanguage.Portuguese => "pt",
                _ => "es"
            };
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if ((item.Tag as string) == targetTag)
                {
                    if (!item.IsSelected)
                    {
                        LanguageCombo.SelectedItem = item;
                    }
                    break;
                }
            }
        }

        private void OnEditHeadingClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Dialogs.HeadingDialog(ViewModel.CurrentAssignment.Heading);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
            }
        }

        private void OnMenuExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnMenuAboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Infinite Calculus - Native C# Edition\n\n" +
                "Recreated with full reverse engineering and modern .NET.\n" +
                "Includes complete mathematical generation, multiple choice / free response modes, " +
                "interactive workspace distribution, and vector printing.\n\n" +
                "Version 1.0 (Built from Calculus.exe)",
                "About Infinite Calculus",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}