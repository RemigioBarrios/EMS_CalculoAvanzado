using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using CalculusApp.Core;
using CalculusApp.Generators;
using CalculusApp.Models;
using CalculusApp.Services;
using CalculusApp.ViewModels;

namespace CalculusApp
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;
        private bool _allExpanded = true;
        private double _lastLeftSidebarWidth = 325;
        private double _lastRightSidebarWidth = 415;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += (s, e) => UpdateDwmTitleBarTheme();
            ThemeService.ThemeChanged += () => Dispatcher.Invoke(UpdateDwmTitleBarTheme);

            ViewModel.RequestRender += OnRequestRender;
            ViewModel.RequestExportPdf += OnRequestExportPdf;
            ViewModel.RequestPreviewPdf += OnRequestPreviewPdf;
            ViewModel.SidebarStateChanged += ApplySidebarLayoutState;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            SubjectRegistry.CurrentSubjectChanged += OnSubjectChangedForAssistant;
            LocalizationService.LanguageChanged += OnLanguageChanged;
            Loaded += MainWindow_Loaded;

            ApplySidebarLayoutState();

            ThumbnailsPane.PageClicked += (pageIdx) =>
            {
                WorksheetCanvas.ScrollToPage(pageIdx);
            };

            Closing += (s, e) =>
            {
                try
                {
                    AppSettingsService.Current.LastSubjectId = SubjectRegistry.CurrentSubject.Id;
                    if (ViewModel.ActiveEdition != null)
                        AppSettingsService.Current.LastEditionId = ViewModel.ActiveEdition.Id;
                    AppSettingsService.Current.IsLeftSidebarOpen = ViewModel.IsLeftSidebarOpen;
                    AppSettingsService.Current.IsRightSidebarOpen = ViewModel.IsRightSidebarOpen;
                    AppSettingsService.Current.Theme = ThemeService.IsDark ? "dark" : "light";
                    AppSettingsService.Current.IsRibbonMode = ViewModel.IsRibbonMode;
                    AppSettingsService.Current.DocumentMode = ViewModel.DocumentMode;
                    AppSettingsService.Current.Columns = ViewModel.Columns;
                    AppSettingsService.Current.ZoomPercent = ViewModel.ZoomPercent;
                    AppSettingsService.Save();
                }
                catch { }
            };

            ViewModel.RequestEditQuestionSet += (qs) => EditQuestionSet(qs);
            ViewModel.RequestEditQuestionsDirectly += (qs) => EditQuestionsDirectly(qs);

            if (CenterMarkdownWorkspace != null)
            {
                CenterMarkdownWorkspace.MarkdownApplied += (md) =>
                {
                    ViewModel.ApplyMarkdownToCurrentAssignment(md);
                    ViewModel.IsPdfMode = true;
                };
                CenterMarkdownWorkspace.MarkdownFileLoaded += (doc) =>
                {
                    ViewModel.ApplyMarkdownToCurrentAssignment(doc.RawContent);
                };
            }
        }

        private void UpdateDwmTitleBarTheme()
        {
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                {
                    int darkMode = ThemeService.IsDark ? 1 : 0;
                    int hr = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                    }
                }
            }
            catch { }
        }

        private void ApplySidebarLayoutState()
        {
            if (LeftSidebarCol == null || RightSidebarCol == null) return;

            // Left Sidebar (Temas y Series Activas)
            if (ViewModel.IsLeftSidebarOpen)
            {
                LeftSidebarBorder.Visibility = Visibility.Visible;
                LeftGridSplitter.Visibility = Visibility.Visible;
                LeftCollapsedStrip.Visibility = Visibility.Collapsed;
                LeftSidebarCol.MinWidth = 250;
                LeftSidebarCol.Width = new GridLength(_lastLeftSidebarWidth > 250 ? _lastLeftSidebarWidth : 325);
                LeftSplitterCol.Width = new GridLength(5);
            }
            else
            {
                if (LeftSidebarCol.ActualWidth > 250)
                    _lastLeftSidebarWidth = LeftSidebarCol.ActualWidth;
                LeftSidebarBorder.Visibility = Visibility.Collapsed;
                LeftGridSplitter.Visibility = Visibility.Collapsed;
                LeftCollapsedStrip.Visibility = Visibility.Visible;
                LeftSidebarCol.MinWidth = 0;
                LeftSidebarCol.Width = new GridLength(0);
                LeftSplitterCol.Width = new GridLength(0);
            }

            // Right Sidebar (Asistente IA y Miniaturas)
            if (ViewModel.IsRightSidebarOpen)
            {
                RightSidebarBorder.Visibility = Visibility.Visible;
                RightGridSplitter.Visibility = Visibility.Visible;
                RightCollapsedStrip.Visibility = Visibility.Collapsed;
                RightSidebarCol.MinWidth = 280;
                RightSidebarCol.Width = new GridLength(_lastRightSidebarWidth > 280 ? _lastRightSidebarWidth : 415);
                RightSplitterCol.Width = new GridLength(5);
            }
            else
            {
                if (RightSidebarCol.ActualWidth > 280)
                    _lastRightSidebarWidth = RightSidebarCol.ActualWidth;
                RightSidebarBorder.Visibility = Visibility.Collapsed;
                RightGridSplitter.Visibility = Visibility.Collapsed;
                RightCollapsedStrip.Visibility = Visibility.Visible;
                RightSidebarCol.MinWidth = 0;
                RightSidebarCol.Width = new GridLength(0);
                RightSplitterCol.Width = new GridLength(0);
            }

            // Series Activas sub-panel inside Left Sidebar
            if (SeriesActivasRow != null && QuestionSetsListBox != null && SeriesGridSplitter != null)
            {
                if (ViewModel.IsSeriesPanelOpen)
                {
                    QuestionSetsListBox.Visibility = Visibility.Visible;
                    SeriesGridSplitter.Visibility = Visibility.Visible;
                    SeriesActivasRow.MinHeight = 165;
                    SeriesActivasRow.Height = new GridLength(2, GridUnitType.Star);
                }
                else
                {
                    QuestionSetsListBox.Visibility = Visibility.Collapsed;
                    SeriesGridSplitter.Visibility = Visibility.Collapsed;
                    SeriesActivasRow.MinHeight = 0;
                    SeriesActivasRow.Height = GridLength.Auto;
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"), "MainWindow_Loaded: start\n");
                WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"), "MainWindow_Loaded: RenderAssignment done\n");
                ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"), "MainWindow_Loaded: RefreshThumbnails done\n");

                // Initialize AI Assistant with the selected subject, subject title, category (tema) and item
                SyncAssistantWithCurrentSubjectDefaultTopic();
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"), "MainWindow_Loaded: done\n");
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loaded_error.txt"), ex.ToString());
            }
        }

        private bool _isSyncingCenterMarkdown = false;

        private void OnRequestRender()
        {
            if (ViewModel.IsMarkdownMode)
            {
                if (CenterMarkdownWorkspace != null)
                {
                    WorksheetCanvas.Visibility = Visibility.Collapsed;
                    CenterMarkdownWorkspace.Visibility = Visibility.Visible;
                    if (!_isSyncingCenterMarkdown)
                    {
                        _isSyncingCenterMarkdown = true;
                        try
                        {
                            CenterMarkdownWorkspace.SuggestedFileName = $"{ViewModel.CurrentAssignment.Heading.Title}.md";
                            string md = AssignmentMarkdownAdapter.Instance.ToMarkdown(ViewModel.CurrentAssignment);
                            CenterMarkdownWorkspace.SetMarkdownContent(md);
                        }
                        finally
                        {
                            _isSyncingCenterMarkdown = false;
                        }
                    }
                }
                return;
            }

            if (CenterMarkdownWorkspace != null && CenterMarkdownWorkspace.Visibility == Visibility.Visible)
            {
                if (CenterMarkdownWorkspace.CurrentDocument.IsDirty && !_isSyncingCenterMarkdown)
                {
                    _isSyncingCenterMarkdown = true;
                    try
                    {
                        ViewModel.ApplyMarkdownToCurrentAssignment(CenterMarkdownWorkspace.MarkdownText);
                    }
                    finally
                    {
                        _isSyncingCenterMarkdown = false;
                    }
                }
                CenterMarkdownWorkspace.Visibility = Visibility.Collapsed;
                WorksheetCanvas.Visibility = Visibility.Visible;
            }

            WorksheetCanvas.ViewMode = ViewModel.IsPdfMode ? Controls.DocumentViewMode.PdfMode : Controls.DocumentViewMode.ExamMode;
            WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
            if (ThumbnailsPane.Visibility == Visibility.Visible)
            {
                ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
            }
        }

        private void OnLanguageChanged()
        {
            WorksheetCanvas.RenderAssignment(ViewModel.CurrentAssignment);
            ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
        }

        private void SyncAssistantWithCurrentSubjectDefaultTopic()
        {
            if (AiAssistantPane == null) return;
            var subj = SubjectRegistry.CurrentSubject;
            var firstTopic = subj?.AllTopics?.FirstOrDefault();
            if (subj != null && firstTopic != null)
            {
                AiAssistantPane.SetTopicContext(
                    subj.Id,
                    subj.Name,
                    subj.Subtitle,
                    firstTopic.Category,
                    firstTopic.TopicId,
                    firstTopic.Title);
            }
            else if (subj != null)
            {
                AiAssistantPane.SetTopicContext(
                    subj.Id,
                    subj.Name,
                    subj.Subtitle,
                    "General",
                    "general",
                    subj.Name);
            }
            else
            {
                AiAssistantPane.SetTopicContext(
                    "basic_calculus",
                    "Cálculo Básico",
                    "EMS Cálculo Básico (Pre-Cálculo)",
                    "Funciones",
                    "basic_continuity",
                    "Continuidad");
            }
        }

        private void OnSubjectChangedForAssistant()
        {
            Dispatcher.Invoke(SyncAssistantWithCurrentSubjectDefaultTopic);
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedQuestionSet) && ViewModel.SelectedQuestionSet != null && AiAssistantPane != null)
            {
                var qs = ViewModel.SelectedQuestionSet;
                var subj = SubjectRegistry.CurrentSubject;
                string catName = !string.IsNullOrWhiteSpace(qs.Category)
                    ? qs.Category
                    : (SubjectRegistry.GetTopicById(qs.TopicId)?.Category ?? "General");
                string topicId = !string.IsNullOrWhiteSpace(qs.TopicId) ? qs.TopicId : qs.TopicTitle;
                AiAssistantPane.SetTopicContext(
                    subj?.Id ?? "calculus",
                    subj?.Name ?? "Cálculo",
                    subj?.Subtitle ?? "Infinite Calculus",
                    catName,
                    topicId,
                    qs.TopicTitle);
            }
        }

        private void OnTopicTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TopicTreeItem item && AiAssistantPane != null)
            {
                var subj = SubjectRegistry.CurrentSubject;
                if (!item.IsCategory)
                {
                    var topic = SubjectRegistry.GetTopicById(item.TopicId);
                    string catName = !string.IsNullOrWhiteSpace(item.CategoryTitle)
                        ? item.CategoryTitle
                        : (topic?.Category ?? "Funciones");

                    AiAssistantPane.SetTopicContext(
                        subj?.Id ?? "calculus",
                        subj?.Name ?? "Cálculo",
                        subj?.Subtitle ?? "Infinite Calculus",
                        catName,
                        item.TopicId,
                        item.Title);
                }
                else
                {
                    var firstChild = item.Children.FirstOrDefault();
                    if (firstChild != null)
                    {
                        AiAssistantPane.SetTopicContext(
                            subj?.Id ?? "calculus",
                            subj?.Name ?? "Cálculo",
                            subj?.Subtitle ?? "Infinite Calculus",
                            item.Title,
                            firstChild.TopicId,
                            firstChild.Title);
                    }
                }
            }
        }

        private void OnTopicTreeDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedItem is TopicTreeItem item && !item.IsCategory)
            {
                var topic = SubjectRegistry.GetTopicById(item.TopicId);
                if (topic != null)
                {
                    var dlg = new Dialogs.TopicOptionsDialog(topic, 4, QuestionType.FreeResponse);
                    dlg.Owner = this;
                    dlg.QuestionsAdded += (count, type, parameters, acceptedQuestions) =>
                    {
                        ViewModel.AddTopic(item.TopicId, count, type, parameters, acceptedQuestions);
                        ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
                    };
                    dlg.ShowDialog();
                }
            }
        }

        private void OnQuestionSetDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditQuestionSet(ViewModel.SelectedQuestionSet);
        }

        public void EditQuestionSet(QuestionSet? qs)
        {
            qs ??= ViewModel.SelectedQuestionSet;
            if (qs == null) return;

            var topic = SubjectRegistry.GetTopicById(qs.TopicId);
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
                    ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
                };
                dlg.ShowDialog();
            }
            else
            {
                EditQuestionsDirectly(qs);
            }
        }

        public void EditQuestionsDirectly(QuestionSet? qs)
        {
            qs ??= ViewModel.SelectedQuestionSet;
            if (qs == null) return;

            var dlg = new Dialogs.QuestionEditDialog(qs);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                ViewModel.RefreshView();
                ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
            }
        }

        private void OnRightTabChanged(object sender, RoutedEventArgs e)
        {
            if (TabMiniatura == null || TabAsistente == null || AiAssistantPane == null || ThumbnailsPane == null) return;

            if (TabMiniatura.IsChecked == true)
            {
                AiAssistantPane.Visibility = Visibility.Collapsed;
                ThumbnailsPane.Visibility = Visibility.Visible;
                ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
            }
            else
            {
                AiAssistantPane.Visibility = Visibility.Visible;
                ThumbnailsPane.Visibility = Visibility.Collapsed;
            }
        }

        private void OnToggleExpandAllClick(object sender, RoutedEventArgs e)
        {
            _allExpanded = !_allExpanded;
            SetExpandedRecursive(TopicsTree, _allExpanded);
        }

        private void SetExpandedRecursive(ItemsControl parent, bool isExpanded)
        {
            foreach (var item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                {
                    tvi.IsExpanded = isExpanded;
                    SetExpandedRecursive(tvi, isExpanded);
                }
            }
        }

        private void OnFolderDropdownClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                var cm = new ContextMenu();
                foreach (var subj in ViewModel.AvailableSubjects)
                {
                    var mi = new MenuItem { Header = $"{subj.Icon} {subj.Name}" };
                    mi.Click += (s, ev) =>
                    {
                        ViewModel.CurrentSubject = subj;
                        ThumbnailsPane.RefreshThumbnails(WorksheetCanvas.GetPageBorders());
                    };
                    cm.Items.Add(mi);
                }
                cm.Items.Add(new Separator());
                var miLoad = new MenuItem { Header = "📂 Cargar Temario desde Archivo..." };
                miLoad.Click += (s, ev) => ViewModel.LoadSyllabusFileCommand.Execute(null);
                cm.Items.Add(miLoad);
                cm.PlacementTarget = fe;
                cm.IsOpen = true;
            }
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
                Title = "Exportar a PDF",
                FileName = (ViewModel.CurrentAssignment?.Heading?.Title ?? "Calculus") + ".pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string path = PdfExportService.ExportToPdf(WorksheetCanvas, sfd.FileName);
                    string msg = LocalizationService.Get("pdf_export_success", path);
                    MessageBox.Show(msg, "Exportación a PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al exportar PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMenuExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnMenuAboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "EMS Cálculo Básico & Infinite Calculus con Asistente IA\n\n" +
                "Sistema integral de generación de ejercicios, exámenes y documentación matemática teórica asistida por Inteligencia Artificial.\n" +
                "Incluye soporte para Google Gemini, renderizado KaTeX/LaTeX, síntesis de voz y visor en tiempo real.\n\n" +
                "Versión 2.0",
                "Acerca de EMS Cálculo Básico",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
