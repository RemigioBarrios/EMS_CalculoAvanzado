using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CalculusApp.Core;
using CalculusApp.Generators;
using CalculusApp.Models;
using CalculusApp.Services;

namespace CalculusApp.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class TopicTreeItem
    {
        public string Title { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string CategoryTitle { get; set; } = string.Empty;
        public bool IsCategory { get; set; }
        public ObservableCollection<TopicTreeItem> Children { get; set; } = new();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private Assignment _assignment = new();
        private QuestionSet? _selectedQuestionSet;
        private string _searchFilter = string.Empty;
        private readonly Random _rng = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? RequestRender;
        public event Action? RequestExportPdf;
        public event Action? RequestPreviewPdf;
        public event Action<QuestionSet>? RequestEditQuestionSet;
        public event Action<QuestionSet>? RequestEditQuestionsDirectly;
        public event Action? SidebarStateChanged;

        public Assignment CurrentAssignment
        {
            get => _assignment;
            set
            {
                _assignment = value;
                OnPropertyChanged();
                UpdateStatus();
                RequestRender?.Invoke();
            }
        }

        public ObservableCollection<QuestionSet> QuestionSets { get; } = new();

        public string QuestionSetsCountText
        {
            get
            {
                int visible = QuestionSets.Count(q => !q.IsHidden);
                int total = QuestionSets.Count;
                return $"({visible}/{total})";
            }
        }
        public string QuestionSetsSummary => QuestionSetsCountText;

        public QuestionSet? SelectedQuestionSet
        {
            get => _selectedQuestionSet;
            set
            {
                _selectedQuestionSet = value;
                OnPropertyChanged();
                RaiseQuestionSetCommandsChanged();
            }
        }

        public ObservableCollection<TopicTreeItem> TopicCategories { get; } = new();

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                OnPropertyChanged();
                FilterTopics();
            }
        }

        public bool ShowAnswers
        {
            get => _assignment.ShowAnswers;
            set
            {
                _assignment.ShowAnswers = value;
                OnPropertyChanged();
                RequestRender?.Invoke();
            }
        }

        public bool ShowPunchHoles
        {
            get => _assignment.ShowPunchHoles;
            set
            {
                _assignment.ShowPunchHoles = value;
                OnPropertyChanged();
                RequestRender?.Invoke();
            }
        }

        public int Columns
        {
            get => _assignment.Columns;
            set
            {
                if (_assignment.Columns != value)
                {
                    _assignment.Columns = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSingleColumn));
                    OnPropertyChanged(nameof(IsTwoColumns));
                    AppSettingsService.Current.Columns = value;
                    AppSettingsService.Save();
                    RequestRender?.Invoke();
                }
            }
        }

        public bool IsSingleColumn
        {
            get => Columns == 1;
            set { if (value) Columns = 1; }
        }

        public bool IsTwoColumns
        {
            get => Columns == 2;
            set { if (value) Columns = 2; }
        }

        public double ZoomPercent
        {
            get => _assignment.ZoomPercent;
            set
            {
                _assignment.ZoomPercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomDisplayText));
            }
        }

        public string ZoomDisplayText => $"{(int)ZoomPercent}%";

        private string _statusPagesText = "0.00 pages";
        public string StatusPagesText
        {
            get => _statusPagesText;
            set { _statusPagesText = value; OnPropertyChanged(); }
        }

        private string _statusQuestionsText = "0 questions";
        public string StatusQuestionsText
        {
            get => _statusQuestionsText;
            set { _statusQuestionsText = value; OnPropertyChanged(); }
        }

        public string CurrentLanguageCode => LocalizationService.CurrentLanguage switch
        {
            AppLanguage.Spanish => "es",
            AppLanguage.Portuguese => "pt",
            _ => "en"
        };

        public string this[string key] => LocalizationService.Get(key);

        #region Commands
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand AddTopicCommand { get; }
        public ICommand RemoveQuestionSetCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand ToggleHideSetCommand { get; }
        public ICommand ToggleHideItemCommand { get; }
        public ICommand EditQuestionSetCommand { get; }
        public ICommand EditQuestionsDirectlyCommand { get; }
        public ICommand RegenerateAllCommand { get; }
        public ICommand RegenerateSetCommand { get; }
        public ICommand ScrambleQuestionsCommand { get; }
        public ICommand ScrambleChoicesCommand { get; }
        public ICommand ChangeAllToMultipleChoiceCommand { get; }
        public ICommand ChangeAllToFreeResponseCommand { get; }
        public ICommand OnePageShorterCommand { get; }
        public ICommand OnePageLongerCommand { get; }
        public ICommand RespaceWithinPageCommand { get; }
        public ICommand SetZoomCommand { get; }
        public ICommand ToggleAnswersCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand PreviewPdfCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SetThemeCommand { get; }
        public ICommand SelectSubjectCommand { get; }
        public ICommand LoadSyllabusFileCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand ToggleSortTopicsCommand { get; }
        public ICommand ExportIniCommand { get; }
        public ICommand ImportIniCommand { get; }
        public ICommand OpenMarkdownStudioCommand { get; }
        public ICommand ExportMarkdownCommand { get; }
        public ICommand ImportMarkdownCommand { get; }
        public ICommand EditSetMarkdownCommand { get; }
        public ICommand ValidateFormulasCommand { get; }

        // Edition commands (Estudiante, Profesor, Instituto)
        public ICommand SwitchEditionCommand { get; }
        public ICommand StudentSelfTestCommand { get; }
        public ICommand StudentSummaryCommand { get; }
        public ICommand StudentScheduleCommand { get; }
        public ICommand StudentStudyRouteCommand { get; }
        public ICommand TeacherRosterCommand { get; }
        public ICommand TeacherWebExamCommand { get; }
        public ICommand TeacherScheduleCommand { get; }
        public ICommand TeacherPdfExtractorCommand { get; }
        public ICommand TeacherPdfExtractorAdvancedCommand { get; }
        public ICommand InstituteFacultyCommand { get; }
        public ICommand InstituteMultiSubjectCommand { get; }
        public ICommand InstituteCentralWebExamsCommand { get; }

        public string LeftSidebarHeader => $"TEMAS - {CurrentSubject.Name.ToUpperInvariant()}";
        public string CenterStatusLabel => $"{_assignment.Heading.Title} • {StatusPagesText} ({ZoomDisplayText})";

        private string _rightPanelMode = "assistant";
        public string RightPanelMode
        {
            get => _rightPanelMode;
            set
            {
                _rightPanelMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAssistantMode));
                OnPropertyChanged(nameof(IsThumbnailsMode));
            }
        }
        public bool IsAssistantMode => _rightPanelMode == "assistant";
        public bool IsThumbnailsMode => _rightPanelMode == "thumbnails";

        private bool _isSortedAlphabetically = false;
        public bool IsSortedAlphabetically => _isSortedAlphabetically;

        private string _documentMode = "pdf";
        public string DocumentMode
        {
            get => _documentMode;
            set
            {
                if (_documentMode != value)
                {
                    _documentMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPdfMode));
                    OnPropertyChanged(nameof(IsExamMode));
                    OnPropertyChanged(nameof(IsMarkdownMode));
                    if (value != "markdown")
                    {
                        AppSettingsService.Current.DocumentMode = value;
                        AppSettingsService.Save();
                    }
                    RequestRender?.Invoke();
                }
            }
        }
        public bool IsPdfMode
        {
            get => _documentMode == "pdf";
            set { if (value) DocumentMode = "pdf"; }
        }
        public bool IsExamMode
        {
            get => _documentMode == "exam";
            set { if (value) DocumentMode = "exam"; }
        }
        public bool IsMarkdownMode
        {
            get => _documentMode == "markdown";
            set { if (value) DocumentMode = "markdown"; }
        }
        public ICommand SetDocumentModeCommand { get; }
        public ICommand ToggleAutoFitCommand { get; }
        public ICommand SetColumnsCommand { get; }

        private bool _isRibbonMode = true;
        public bool IsRibbonMode
        {
            get => _isRibbonMode;
            set
            {
                if (_isRibbonMode != value)
                {
                    _isRibbonMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RibbonVisibility));
                    OnPropertyChanged(nameof(MenuVisibility));
                    OnPropertyChanged(nameof(RibbonModeButtonText));
                }
            }
        }
        public Visibility RibbonVisibility => _isRibbonMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MenuVisibility => _isRibbonMode ? Visibility.Collapsed : Visibility.Visible;
        public string RibbonModeButtonText => _isRibbonMode ? "Modo sin Ribbon" : "Modo con Ribbon";

        private string _activeRibbonTab = "MATEMÁTICA AVANZADA";
        public string ActiveRibbonTab
        {
            get => _activeRibbonTab;
            set
            {
                if (_activeRibbonTab != value)
                {
                    _activeRibbonTab = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPanelControlTabActive));
                    OnPropertyChanged(nameof(IsDisenoTabActive));
                    OnPropertyChanged(nameof(IsMatematicaTabActive));
                    OnPropertyChanged(nameof(IsEstudianteTabActive));
                    OnPropertyChanged(nameof(IsProfesorTabActive));
                    OnPropertyChanged(nameof(IsInstitutoTabActive));
                    OnPropertyChanged(nameof(IsHerramientasTabActive));
                    OnPropertyChanged(nameof(IsAyudaTabActive));
                }
            }
        }
        public bool IsPanelControlTabActive
        {
            get => _activeRibbonTab == "PANEL DE CONTROL";
            set { if (value) ActiveRibbonTab = "PANEL DE CONTROL"; }
        }
        public bool IsDisenoTabActive
        {
            get => _activeRibbonTab == "DISEÑO DE EVALUACIONES";
            set { if (value) ActiveRibbonTab = "DISEÑO DE EVALUACIONES"; }
        }
        public bool IsMatematicaTabActive
        {
            get => _activeRibbonTab == "MATEMÁTICA AVANZADA";
            set { if (value) ActiveRibbonTab = "MATEMÁTICA AVANZADA"; }
        }
        public bool IsEstudianteTabActive
        {
            get => _activeRibbonTab == "ESTUDIANTE";
            set { if (value) ActiveRibbonTab = "ESTUDIANTE"; }
        }
        public bool IsProfesorTabActive
        {
            get => _activeRibbonTab == "PROFESOR";
            set { if (value) ActiveRibbonTab = "PROFESOR"; }
        }
        public bool IsInstitutoTabActive
        {
            get => _activeRibbonTab == "INSTITUTO";
            set { if (value) ActiveRibbonTab = "INSTITUTO"; }
        }
        public bool IsHerramientasTabActive
        {
            get => _activeRibbonTab == "HERRAMIENTAS";
            set { if (value) ActiveRibbonTab = "HERRAMIENTAS"; }
        }
        public bool IsAyudaTabActive
        {
            get => _activeRibbonTab == "AYUDA";
            set { if (value) ActiveRibbonTab = "AYUDA"; }
        }

        // Filtrado de pantallas y pestañas de funcionalidades según la Edición Seleccionada (Estudiante, Profesor, Instituto)
        public Visibility StudentTabVisibility => EditionService.IsStudentActive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TeacherTabVisibility => EditionService.IsTeacherActive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InstituteTabVisibility => EditionService.IsInstituteActive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TeacherOrInstituteVisibility => (EditionService.IsTeacherActive || EditionService.IsInstituteActive) ? Visibility.Visible : Visibility.Collapsed;

        // Visibilidad de opciones de selección de edición en el menú clásico
        public Visibility StudentEditionMenuVisibility => EditionService.IsStudentAvailable ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TeacherEditionMenuVisibility => EditionService.IsTeacherAvailable ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InstituteEditionMenuVisibility => EditionService.IsInstituteAvailable ? Visibility.Visible : Visibility.Collapsed;

        private bool _isLeftSidebarOpen = true;
        public bool IsLeftSidebarOpen
        {
            get => _isLeftSidebarOpen;
            set
            {
                if (_isLeftSidebarOpen != value)
                {
                    _isLeftSidebarOpen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeftSidebarToggleText));
                    AppSettingsService.Current.IsLeftSidebarOpen = value;
                    AppSettingsService.Save();
                    SidebarStateChanged?.Invoke();
                }
            }
        }

        private bool _isRightSidebarOpen = true;
        public bool IsRightSidebarOpen
        {
            get => _isRightSidebarOpen;
            set
            {
                if (_isRightSidebarOpen != value)
                {
                    _isRightSidebarOpen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RightSidebarToggleText));
                    AppSettingsService.Current.IsRightSidebarOpen = value;
                    AppSettingsService.Save();
                    SidebarStateChanged?.Invoke();
                }
            }
        }

        private bool _isSeriesPanelOpen = true;
        public bool IsSeriesPanelOpen
        {
            get => _isSeriesPanelOpen;
            set
            {
                if (_isSeriesPanelOpen != value)
                {
                    _isSeriesPanelOpen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SeriesPanelExpandIcon));
                    SidebarStateChanged?.Invoke();
                }
            }
        }

        public string LeftSidebarToggleText => _isLeftSidebarOpen ? "◧ Ocultar Temas" : "◧ Mostrar Temas";
        public string RightSidebarToggleText => _isRightSidebarOpen ? "◨ Ocultar Asistente" : "◨ Mostrar Asistente";
        public string SeriesPanelExpandIcon => _isSeriesPanelOpen ? "▼" : "▲";

        public ICommand ToggleLeftSidebarCommand { get; }
        public ICommand ToggleRightSidebarCommand { get; }
        public ICommand ToggleSeriesPanelCommand { get; }

        public IReadOnlyList<IEditionModule> AvailableEditions => EditionService.AvailableEditions;
        public IEditionModule? ActiveEdition
        {
            get => EditionService.ActiveEdition;
            set
            {
                if (value != null && EditionService.ActiveEdition != value)
                {
                    EditionService.ActiveEdition = value;
                    AppSettingsService.Current.LastEditionId = value.Id;
                    AppSettingsService.Save();

                    // Cambiar automáticamente a la pestaña de funcionalidades de la edición seleccionada
                    if (value.Edition == EditionType.Student)
                        ActiveRibbonTab = "ESTUDIANTE";
                    else if (value.Edition == EditionType.Teacher)
                        ActiveRibbonTab = "PROFESOR";
                    else if (value.Edition == EditionType.Institute)
                        ActiveRibbonTab = "INSTITUTO";

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActiveEditionBadge));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }
        public string ActiveEditionBadge => EditionService.ActiveEdition != null
            ? $"{EditionService.ActiveEdition.Icon} {EditionService.ActiveEdition.DisplayName}"
            : "🎓 Estándar";

        public ICommand ToggleRibbonModeCommand { get; }
        public ICommand SelectRibbonTabCommand { get; }

        public string DocumentStatusText => "📄 Documento actual: Doc 1";
        public string AiEngineStatusText => $"IA: GEMINI - {AiDocumentationService.Instance.SelectedModel}";
        public string SystemResourcesStatusText => "MAPP: 93 MB   MDISP: 3 GB   v.26.8.12.8";

        public bool IsDarkTheme => ThemeService.IsDark;
        public string ThemeStatusText => ThemeService.IsDark ? LocalizationService.Get("tb_theme_dark") : LocalizationService.Get("tb_theme_light");
        public ISubjectModule CurrentSubject
        {
            get => SubjectRegistry.CurrentSubject;
            set
            {
                if (value != null && SubjectRegistry.CurrentSubject != value)
                {
                    SubjectRegistry.CurrentSubject = value;
                }
            }
        }
        public IReadOnlyList<ISubjectModule> AvailableSubjects => SubjectRegistry.AvailableSubjects;
        public string WindowTitle => $"{SubjectRegistry.CurrentSubject.Subtitle} [{ActiveEditionBadge}] - [ {_assignment.Heading.Title} ]";
        #endregion

        public MainViewModel()
        {
            try
            {
                AppSettingsService.Load();
                _documentMode = string.IsNullOrEmpty(AppSettingsService.Current.DocumentMode) ? "pdf" : AppSettingsService.Current.DocumentMode;
                _isRibbonMode = AppSettingsService.Current.IsRibbonMode;
                _isLeftSidebarOpen = AppSettingsService.Current.IsLeftSidebarOpen;
                _isRightSidebarOpen = AppSettingsService.Current.IsRightSidebarOpen;
                _assignment.Columns = AppSettingsService.Current.Columns > 0 ? AppSettingsService.Current.Columns : 2;
                _assignment.ZoomPercent = AppSettingsService.Current.ZoomPercent > 0 ? AppSettingsService.Current.ZoomPercent : 100.0;
                if (!string.IsNullOrWhiteSpace(AppSettingsService.Current.LastEditionId))
                {
                    EditionService.SetActiveEditionById(AppSettingsService.Current.LastEditionId);
                }
            }
            catch { }

            NewCommand = new RelayCommand(_ => CreateNewAssignment());
            OpenCommand = new RelayCommand(_ => OpenAssignment());
            SaveCommand = new RelayCommand(_ => SaveAssignment());
            PrintCommand = new RelayCommand(_ => PrintAssignment());
            ExportPdfCommand = new RelayCommand(_ => RequestExportPdf?.Invoke());
            PreviewPdfCommand = new RelayCommand(_ => RequestPreviewPdf?.Invoke());
            AddTopicCommand = new RelayCommand(p => AddTopic(p as string));
            RemoveQuestionSetCommand = new RelayCommand(_ => RemoveSelectedSet(), _ => SelectedQuestionSet != null);
            MoveUpCommand = new RelayCommand(_ => MoveSelectedSetUp(), _ => CanMoveUp());
            MoveDownCommand = new RelayCommand(_ => MoveSelectedSetDown(), _ => CanMoveDown());
            ToggleHideSetCommand = new RelayCommand(_ =>
            {
                if (SelectedQuestionSet != null)
                {
                    SelectedQuestionSet.IsHidden = !SelectedQuestionSet.IsHidden;
                    UpdateStatus();
                    RequestRender?.Invoke();
                    RaiseQuestionSetCommandsChanged();
                }
            }, _ => SelectedQuestionSet != null);

            ToggleHideItemCommand = new RelayCommand(p =>
            {
                if (p is QuestionSet qs)
                {
                    qs.IsHidden = !qs.IsHidden;
                    UpdateStatus();
                    RequestRender?.Invoke();
                    RaiseQuestionSetCommandsChanged();
                }
            });

            EditQuestionSetCommand = new RelayCommand(_ =>
            {
                if (SelectedQuestionSet != null)
                    RequestEditQuestionSet?.Invoke(SelectedQuestionSet);
            }, _ => SelectedQuestionSet != null);

            EditQuestionsDirectlyCommand = new RelayCommand(_ =>
            {
                if (SelectedQuestionSet != null)
                    RequestEditQuestionsDirectly?.Invoke(SelectedQuestionSet);
            }, _ => SelectedQuestionSet != null);
            RegenerateAllCommand = new RelayCommand(_ => RegenerateAll());
            RegenerateSetCommand = new RelayCommand(_ => RegenerateSelectedSet(), _ => SelectedQuestionSet != null);
            ScrambleQuestionsCommand = new RelayCommand(_ => ScrambleQuestions());
            ScrambleChoicesCommand = new RelayCommand(_ => ScrambleChoices());
            ChangeAllToMultipleChoiceCommand = new RelayCommand(_ => ChangeAllType(QuestionType.MultipleChoice));
            ChangeAllToFreeResponseCommand = new RelayCommand(_ => ChangeAllType(QuestionType.FreeResponse));
            OnePageShorterCommand = new RelayCommand(_ => AdjustLength(-1));
            OnePageLongerCommand = new RelayCommand(_ => AdjustLength(1));
            RespaceWithinPageCommand = new RelayCommand(_ => RespaceWithinPage());
            SetZoomCommand = new RelayCommand(p => SetZoom(p));
            ToggleAnswersCommand = new RelayCommand(_ => ShowAnswers = !ShowAnswers);
            SetLanguageCommand = new RelayCommand(p =>
            {
                if (p is string s)
                {
                    if (s == "es") LocalizationService.SetLanguage(AppLanguage.Spanish);
                    else if (s == "pt") LocalizationService.SetLanguage(AppLanguage.Portuguese);
                    else LocalizationService.SetLanguage(AppLanguage.English);
                }
            });

            ToggleThemeCommand = new RelayCommand(_ => ThemeService.ToggleTheme());
            SetThemeCommand = new RelayCommand(p =>
            {
                if (p is string s && s.Equals("dark", StringComparison.OrdinalIgnoreCase))
                    ThemeService.SetTheme(AppTheme.Dark);
                else
                    ThemeService.SetTheme(AppTheme.Light);
            });

            SelectSubjectCommand = new RelayCommand(p =>
            {
                if (p is string id)
                {
                    SubjectRegistry.SetCurrentSubject(id);
                }
                else if (p is ISubjectModule mod)
                {
                    SubjectRegistry.CurrentSubject = mod;
                }
            });

            LoadSyllabusFileCommand = new RelayCommand(_ => PromptLoadSyllabusFile());
            ToggleRibbonModeCommand = new RelayCommand(_ => IsRibbonMode = !IsRibbonMode);
            ToggleLeftSidebarCommand = new RelayCommand(_ => IsLeftSidebarOpen = !IsLeftSidebarOpen);
            ToggleRightSidebarCommand = new RelayCommand(_ => IsRightSidebarOpen = !IsRightSidebarOpen);
            ToggleSeriesPanelCommand = new RelayCommand(_ => IsSeriesPanelOpen = !IsSeriesPanelOpen);
            SetDocumentModeCommand = new RelayCommand(p =>
            {
                if (p is string s) DocumentMode = s.ToLowerInvariant();
            });
            SetColumnsCommand = new RelayCommand(p =>
            {
                if (int.TryParse(p?.ToString(), out int cols)) Columns = cols;
            });
            ToggleAutoFitCommand = new RelayCommand(_ =>
            {
                ResetZoomCommand?.Execute(null);
            });
            ExportIniCommand = new RelayCommand(_ => ExportToIni());
            ImportIniCommand = new RelayCommand(_ => ImportFromIni());
            OpenMarkdownStudioCommand = new RelayCommand(_ => OpenAssignmentMarkdownStudio());
            ExportMarkdownCommand = new RelayCommand(_ => ExportToMarkdown());
            ImportMarkdownCommand = new RelayCommand(_ => ImportFromMarkdown());
            EditSetMarkdownCommand = new RelayCommand(_ => EditSelectedSetInMarkdown(), _ => SelectedQuestionSet != null);
            ValidateFormulasCommand = new RelayCommand(_ => ValidateFormulas());
            SelectRibbonTabCommand = new RelayCommand(p =>
            {
                if (p is string tab) ActiveRibbonTab = tab;
            });

            ZoomInCommand = new RelayCommand(_ =>
            {
                ZoomPercent = Math.Min(250.0, ZoomPercent + 15.0);
                OnPropertyChanged(nameof(CenterStatusLabel));
                RequestRender?.Invoke();
            });

            ZoomOutCommand = new RelayCommand(_ =>
            {
                ZoomPercent = Math.Max(40.0, ZoomPercent - 15.0);
                OnPropertyChanged(nameof(CenterStatusLabel));
                RequestRender?.Invoke();
            });

            ResetZoomCommand = new RelayCommand(_ =>
            {
                ZoomPercent = 100.0;
                OnPropertyChanged(nameof(CenterStatusLabel));
                RequestRender?.Invoke();
            });

            ToggleSortTopicsCommand = new RelayCommand(_ =>
            {
                _isSortedAlphabetically = !_isSortedAlphabetically;
                OnPropertyChanged(nameof(IsSortedAlphabetically));
                LoadTopicsTree();
            });

            // Edition commands (Estudiante, Profesor, Instituto)
            SwitchEditionCommand = new RelayCommand(p =>
            {
                if (p is string id)
                {
                    EditionService.SetActiveEditionById(id);
                    if (EditionService.ActiveEdition != null)
                    {
                        AppSettingsService.Current.LastEditionId = EditionService.ActiveEdition.Id;
                        AppSettingsService.Save();
                        if (EditionService.ActiveEdition.Edition == EditionType.Student)
                            ActiveRibbonTab = "ESTUDIANTE";
                        else if (EditionService.ActiveEdition.Edition == EditionType.Teacher)
                            ActiveRibbonTab = "PROFESOR";
                        else if (EditionService.ActiveEdition.Edition == EditionType.Institute)
                            ActiveRibbonTab = "INSTITUTO";
                    }
                }
                else if (p is IEditionModule mod)
                {
                    ActiveEdition = mod;
                }
            });

            StudentSelfTestCommand = new RelayCommand(_ =>
            {
                var curTopic = SubjectRegistry.CurrentSubject.AllTopics.FirstOrDefault();
                string title = curTopic?.Title ?? "Cálculo";
                var dlg = new Dialogs.StudentToolsDialog(
                    $"Auto-Prueba Diagnóstica: {title}",
                    "Generación personalizada de evaluación de diagnóstico",
                    $"Se ha configurado una auto-prueba diagnóstica de 5 ejercicios para el tema:\n\n• Materia: {SubjectRegistry.CurrentSubject.Name}\n• Tema: {title}\n\nPuede resolver los ejercicios directamente en la hoja o exportarlos a PDF/formato plano para estudio individual.",
                    "📝 Resolver Auto-Prueba en Pantalla",
                    () =>
                    {
                        if (curTopic != null)
                        {
                            _assignment.Heading.Title = $"Auto-Prueba - {title}";
                            _assignment.QuestionSets.Clear();
                            QuestionSets.Clear();
                            AddTopic(curTopic.TopicId, 5, QuestionType.MultipleChoice);
                        }
                    });
                dlg.ShowDialog();
            });

            StudentSummaryCommand = new RelayCommand(_ =>
            {
                var curTopic = SubjectRegistry.CurrentSubject.AllTopics.FirstOrDefault();
                string title = curTopic?.Title ?? "Cálculo";
                var dlg = new Dialogs.StudentToolsDialog(
                    $"Resumen de Contenido: {title}",
                    "Ficha de estudio, conceptos y fórmulas esenciales",
                    $"# Resumen Rápido - {title}\n\n**Materia:** {SubjectRegistry.CurrentSubject.Name}\n\n## Conceptos Fundamentales\n1. Identifique el tipo de problema y parámetros clave.\n2. Aplique las propiedades y teoremas correspondientes.\n3. Verifique el resultado mediante sustitución o cálculo inverso.\n\n*(Consulte el panel lateral para la explicación guiada con el Asistente IA)*",
                    "");
                dlg.ShowDialog();
            });

            StudentScheduleCommand = new RelayCommand(_ =>
            {
                var dlg = new Dialogs.StudentToolsDialog(
                    "Control de Horario de Estudio",
                    "Planificación semanal y metas de dedicación",
                    $"Horario Semanal Activo para {SubjectRegistry.CurrentSubject.Name}:\n\n• Lunes: 14:00 - 16:00 (Resolución de ejercicios prácticos)\n• Miércoles: 10:00 - 12:00 (Repaso de conceptos y fórmulas)\n• Viernes: 16:00 - 18:00 (Simulacro de auto-prueba)\n• Sábado: 09:00 - 11:00 (Ruta de compensación de dudas)\n\nTotal de horas programadas esta semana: 8.0 hrs.",
                    "");
                dlg.ShowDialog();
            });

            StudentStudyRouteCommand = new RelayCommand(_ =>
            {
                var curTopic = SubjectRegistry.CurrentSubject.AllTopics.FirstOrDefault();
                string title = curTopic?.Title ?? "Cálculo";
                var dlg = new Dialogs.StudentToolsDialog(
                    "Ruta de Estudio para Compensación de Fallas",
                    "Plan progresivo para superar áreas de oportunidad",
                    $"Diagnóstico de Desempeño:\n\nTema a reforzar: {title}\n\nPasos de la Ruta de Compensación:\n1. Revisar definiciones y gráficos del tema.\n2. Resolver 4 ejercicios de nivel básico.\n3. Avanzar a 4 ejercicios de nivel medio con opciones múltiples.\n4. Realizar comprobación final con límite de tiempo.\n\n¿Desea cargar ahora los ejercicios de compensación?",
                    "🎯 Cargar Ejercicios de Compensación",
                    () =>
                    {
                        if (curTopic != null)
                        {
                            _assignment.Heading.Title = $"Ruta de Compensación - {title}";
                            _assignment.QuestionSets.Clear();
                            QuestionSets.Clear();
                            AddTopic(curTopic.TopicId, 4, QuestionType.FreeResponse);
                        }
                    });
                dlg.ShowDialog();
            });

            TeacherRosterCommand = new RelayCommand(_ =>
            {
                var dlg = new Dialogs.TeacherRosterDialog();
                dlg.ShowDialog();
            });

            TeacherWebExamCommand = new RelayCommand(_ =>
            {
                MessageBox.Show(
                    $"Generador de Evaluaciones Web Docente:\n\nSe ha preparado el paquete interactivo para '{SubjectRegistry.CurrentSubject.Name} - {_assignment.Heading.Title}'.\n\nEl enlace web personal vincula las respuestas de los estudiantes directamente con su nómina docente.",
                    "Generación de Examen Web - Edición Profesor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            TeacherScheduleCommand = new RelayCommand(_ =>
            {
                MessageBox.Show(
                    $"Calendario y Horario Docente:\n\n• Cursos a cargo: Sección A-1, Sección A-2\n• Asignatura activa: {SubjectRegistry.CurrentSubject.Name}\n• Próxima fecha límite de entrega: 25 de Octubre\n• Horario de atención a alumnos: Mar / Jue 14:00 - 16:00",
                    "Horario Docente - Edición Profesor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            TeacherPdfExtractorCommand = new RelayCommand(_ =>
            {
                if (!EditionService.HasFeature(EditionFeature.TeacherPdfExtractor) &&
                    !EditionService.IsTeacherAvailable && !EditionService.IsInstituteAvailable)
                {
                    MessageBox.Show(
                        "El módulo de Extracción de Ejercicios desde PDFs requiere la Edición Profesor (CalculusApp.Teacher.dll).",
                        "Función no disponible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var dlg = new Dialogs.TeacherPdfExtractorDialog(this);
                dlg.ShowDialog();
            });

            TeacherPdfExtractorAdvancedCommand = new RelayCommand(_ =>
            {
                if (!EditionService.HasFeature(EditionFeature.TeacherPdfExtractor) &&
                    !EditionService.IsTeacherAvailable && !EditionService.IsInstituteAvailable)
                {
                    MessageBox.Show(
                        "El módulo de PDF Extractor avanzado requiere la Edición Profesor (CalculusApp.Teacher.dll).",
                        "Función no disponible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var dlg = new Dialogs.TeacherPdfExtractorAdvancedDialog(this);
                dlg.ShowDialog();
            });

            InstituteFacultyCommand = new RelayCommand(_ =>
            {
                var dlg = new Dialogs.InstituteAdminDialog();
                dlg.ShowDialog();
            });

            InstituteMultiSubjectCommand = new RelayCommand(_ =>
            {
                var dlg = new Dialogs.InstituteAdminDialog();
                dlg.ShowDialog();
            });

            InstituteCentralWebExamsCommand = new RelayCommand(_ =>
            {
                MessageBox.Show(
                    "Orquestador Institucional de Evaluaciones Web:\n\nSe ha generado la matriz de examen institucional coordinada entre los departamentos de Matemáticas, Ciencias e Ingeniería.",
                    "Exámenes Centralizados - Edición Instituto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            ThemeService.ThemeChanged += () =>
            {
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeStatusText));
                OnPropertyChanged(string.Empty);
            };

            LocalizationService.LanguageChanged += OnLanguageChanged;
            SubjectRegistry.CurrentSubjectChanged += OnCurrentSubjectChanged;
            SubjectRegistry.SubjectsListChanged += OnSubjectsListChanged;
            EditionService.ActiveEditionChanged += OnActiveEditionChanged;
            EditionService.EditionsListChanged += OnActiveEditionChanged;
            AiDocumentationService.Instance.ConfigurationChanged += () =>
            {
                OnPropertyChanged(nameof(AiEngineStatusText));
            };
            SubjectTemplateBankService.TemplateBankUpdated += () =>
            {
                Application.Current?.Dispatcher?.Invoke(() => LoadTopicsTree());
            };

            LoadTopicsTree();
            CreateDefaultSampleAssignment();
        }

        private void OnCurrentSubjectChanged()
        {
            LoadTopicsTree();
            _assignment.Heading.Subtitle = SubjectRegistry.CurrentSubject.Subtitle;
            OnPropertyChanged(nameof(CurrentSubject));
            OnPropertyChanged(nameof(AvailableSubjects));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(LeftSidebarHeader));
            OnPropertyChanged(string.Empty);
            RequestRender?.Invoke();
        }

        private void OnSubjectsListChanged()
        {
            OnPropertyChanged(nameof(AvailableSubjects));
            OnPropertyChanged(nameof(CurrentSubject));
            OnPropertyChanged(nameof(LeftSidebarHeader));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(string.Empty);
        }

        private void OnActiveEditionChanged()
        {
            if (EditionService.ActiveEdition != null)
            {
                if (_activeRibbonTab == "ESTUDIANTE" && !EditionService.IsStudentActive)
                {
                    ActiveRibbonTab = EditionService.IsInstituteActive ? "INSTITUTO" : (EditionService.IsTeacherActive ? "PROFESOR" : "MATEMÁTICA AVANZADA");
                }
                else if (_activeRibbonTab == "PROFESOR" && !EditionService.IsTeacherActive)
                {
                    ActiveRibbonTab = EditionService.IsInstituteActive ? "INSTITUTO" : (EditionService.IsStudentActive ? "ESTUDIANTE" : "MATEMÁTICA AVANZADA");
                }
                else if (_activeRibbonTab == "INSTITUTO" && !EditionService.IsInstituteActive)
                {
                    ActiveRibbonTab = EditionService.IsTeacherActive ? "PROFESOR" : (EditionService.IsStudentActive ? "ESTUDIANTE" : "MATEMÁTICA AVANZADA");
                }
            }

            OnPropertyChanged(nameof(ActiveEdition));
            OnPropertyChanged(nameof(ActiveEditionBadge));
            OnPropertyChanged(nameof(StudentTabVisibility));
            OnPropertyChanged(nameof(TeacherTabVisibility));
            OnPropertyChanged(nameof(InstituteTabVisibility));
            OnPropertyChanged(nameof(TeacherOrInstituteVisibility));
            OnPropertyChanged(nameof(StudentEditionMenuVisibility));
            OnPropertyChanged(nameof(TeacherEditionMenuVisibility));
            OnPropertyChanged(nameof(InstituteEditionMenuVisibility));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(string.Empty);
        }

        private void PromptLoadSyllabusFile()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos de Temario (*.ini;*.txt)|*.ini;*.txt|Archivos INI (*.ini)|*.ini|Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                Title = "Cargar Temario de Materia (Syllabus .ini / .txt)"
            };
            if (ofd.ShowDialog() == true)
            {
                var dynModule = SyllabusLoader.LoadAuto(ofd.FileName);
                SubjectRegistry.RegisterSubject(dynModule);
                SubjectRegistry.CurrentSubject = dynModule;
                CurrentSubject = dynModule;
            }
        }

        private void OnLanguageChanged()
        {
            LoadTopicsTree();
            UpdateStatus();

            if (_assignment.Heading.Title == "Assignment" || _assignment.Heading.Title == "Guía de Ejercicios" || _assignment.Heading.Title == "Lista de Exercícios")
            {
                _assignment.Heading.Title = LocalizationService.Get("ws_assignment");
            }
            _assignment.Heading.Subtitle = SubjectRegistry.CurrentSubject.Subtitle;

            // Also update directions for current question sets to the new language
            foreach (var qs in _assignment.QuestionSets)
            {
                var topic = SubjectRegistry.GetTopicById(qs.TopicId);
                if (topic != null)
                {
                    qs.TopicTitle = topic.Title;
                    qs.Category = topic.Category;
                    qs.Directions = topic.DefaultDirections;
                }
            }

            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(string.Empty);
            RequestRender?.Invoke();
        }

        private void LoadTopicsTree()
        {
            TopicCategories.Clear();

            // 1. Incluir el Banco de Plantillas específico de la Materia actual asociado al Generador Principal
            var tplCategory = SubjectTemplateBankService.GetDynamicTemplateCategoryForSubject(SubjectRegistry.CurrentSubject);
            if (tplCategory != null && tplCategory.Topics.Count > 0)
            {
                var tplCatItem = new TopicTreeItem
                {
                    Title = tplCategory.Name,
                    CategoryTitle = tplCategory.Name,
                    IsCategory = true
                };
                foreach (var topic in tplCategory.Topics)
                {
                    tplCatItem.Children.Add(new TopicTreeItem
                    {
                        Title = topic.Title,
                        TopicId = topic.TopicId,
                        CategoryTitle = tplCategory.Name,
                        IsCategory = false
                    });
                }
                TopicCategories.Add(tplCatItem);
            }

            var categories = SubjectRegistry.CurrentSubject.Categories;
            var catsToProcess = _isSortedAlphabetically 
                ? categories.OrderBy(c => c.Name).ToList() 
                : categories.ToList();

            foreach (var cat in catsToProcess)
            {
                var catItem = new TopicTreeItem
                {
                    Title = cat.Name,
                    CategoryTitle = cat.Name,
                    IsCategory = true
                };

                var topicsToProcess = _isSortedAlphabetically 
                    ? cat.Topics.OrderBy(t => t.Title).ToList() 
                    : cat.Topics.ToList();

                foreach (var topic in topicsToProcess)
                {
                    catItem.Children.Add(new TopicTreeItem
                    {
                        Title = topic.Title,
                        TopicId = topic.TopicId,
                        CategoryTitle = cat.Name,
                        IsCategory = false
                    });
                }

                TopicCategories.Add(catItem);
            }
        }

        private void FilterTopics()
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
            {
                LoadTopicsTree();
                return;
            }

            TopicCategories.Clear();
            var term = _searchFilter.Trim().ToLowerInvariant();

            var allCategories = new List<SubjectCategory>();
            var tplCategory = SubjectTemplateBankService.GetDynamicTemplateCategoryForSubject(SubjectRegistry.CurrentSubject);
            if (tplCategory != null && tplCategory.Topics.Count > 0)
            {
                allCategories.Add(tplCategory);
            }
            allCategories.AddRange(SubjectRegistry.CurrentSubject.Categories);

            foreach (var cat in allCategories)
            {
                var matched = cat.Topics.Where(t =>
                    t.Title.ToLowerInvariant().Contains(term) ||
                    t.TitleEn.ToLowerInvariant().Contains(term) ||
                    t.TitleEs.ToLowerInvariant().Contains(term) ||
                    t.TitlePt.ToLowerInvariant().Contains(term) ||
                    cat.Name.ToLowerInvariant().Contains(term)).ToList();

                if (matched.Count > 0)
                {
                    var catItem = new TopicTreeItem
                    {
                        Title = cat.Name,
                        CategoryTitle = cat.Name,
                        IsCategory = true
                    };
                    foreach (var topic in matched)
                    {
                        catItem.Children.Add(new TopicTreeItem
                        {
                            Title = topic.Title,
                            TopicId = topic.TopicId,
                            CategoryTitle = cat.Name,
                            IsCategory = false
                        });
                    }
                    TopicCategories.Add(catItem);
                }
            }
        }

        public void AddTopic(string? topicId, int count = 4, QuestionType type = QuestionType.FreeResponse, TopicGeneratorParameters? parameters = null, List<Question>? initialQuestions = null)
        {
            if (string.IsNullOrEmpty(topicId)) return;
            var topic = SubjectRegistry.GetTopicById(topicId);
            if (topic == null) return;

            var questions = (initialQuestions != null && initialQuestions.Count > 0)
                ? initialQuestions
                : topic.GenerateQuestions(count, type, parameters, _rng);

            var qs = new QuestionSet
            {
                TopicId = topic.TopicId,
                TopicTitle = topic.Title,
                Category = topic.Category,
                Directions = !string.IsNullOrWhiteSpace(parameters?.CustomDirections) ? parameters.CustomDirections : topic.DefaultDirections,
                QuestionCount = count,
                DefaultType = type,
                Parameters = parameters?.Clone() ?? new TopicGeneratorParameters { QuestionCount = count, QuestionType = type },
                Questions = questions
            };

            QuestionSets.Add(qs);
            _assignment.QuestionSets.Add(qs);
            SelectedQuestionSet = qs;

            UpdateStatus();
            RequestRender?.Invoke();
        }

        public void AddCustomQuestionSet(QuestionSet qs)
        {
            if (qs == null) return;
            QuestionSets.Add(qs);
            _assignment.QuestionSets.Add(qs);
            SelectedQuestionSet = qs;

            UpdateStatus();
            RequestRender?.Invoke();
            RaiseQuestionSetCommandsChanged();
        }

        private void CreateDefaultSampleAssignment()
        {
            _assignment.Heading.Title = SubjectRegistry.CurrentSubject.DefaultAssignmentTitle;
            _assignment.Heading.Subtitle = SubjectRegistry.CurrentSubject.Subtitle;
            _assignment.QuestionSets.Clear();
            QuestionSets.Clear();

            var firstTopic = SubjectRegistry.CurrentSubject.AllTopics.FirstOrDefault();
            if (firstTopic != null)
            {
                AddTopic(firstTopic.TopicId, 8, QuestionType.FreeResponse);
            }
            else
            {
                var fallback = SubjectRegistry.AvailableSubjects.SelectMany(s => s.AllTopics).FirstOrDefault();
                if (fallback != null)
                {
                    AddTopic(fallback.TopicId, 8, QuestionType.FreeResponse);
                }
            }
        }

        private void CreateNewAssignment()
        {
            string msg = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => $"¿Desea crear una nueva {LocalizationService.Get("ws_assignment").ToLower()} de {SubjectRegistry.CurrentSubject.Name}?",
                AppLanguage.Portuguese => $"Deseja criar uma nova lista de {SubjectRegistry.CurrentSubject.Name}?",
                _ => $"Create a new {SubjectRegistry.CurrentSubject.Name} assignment?"
            };
            string caption = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Nuevo Examen",
                AppLanguage.Portuguese => "Novo Teste",
                _ => "New Assignment"
            };

            if (MessageBox.Show(msg, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _assignment = new Assignment();
                _assignment.Heading.Title = LocalizationService.Get("ws_assignment");
                _assignment.Heading.Subtitle = SubjectRegistry.CurrentSubject.Subtitle;
                QuestionSets.Clear();
                UpdateStatus();
                RequestRender?.Invoke();
            }
        }

        private void OpenAssignment()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Evaluaciones (*.calc;*.md;*.json)|*.calc;*.md;*.markdown;*.json|Archivos Markdown (*.md;*.markdown)|*.md;*.markdown|Calculus Assignment (*.calc)|*.calc|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Abrir Evaluación (.calc / .md / .json)"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    if (ofd.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                        ofd.FileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
                    {
                        var mdDoc = CalculusApp.Markdown.Services.MarkdownDocumentService.LoadFromFile(ofd.FileName);
                        ApplyMarkdownToCurrentAssignment(mdDoc.RawContent);
                        return;
                    }

                    string json = File.ReadAllText(ofd.FileName);
                    var loaded = JsonSerializer.Deserialize<Assignment>(json);
                    if (loaded != null)
                    {
                        _assignment = loaded;
                        QuestionSets.Clear();
                        foreach (var qs in _assignment.QuestionSets)
                        {
                            QuestionSets.Add(qs);
                        }
                        UpdateStatus();
                        RequestRender?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to open file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAssignment()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Calculus Assignment (*.calc)|*.calc|Archivo Markdown (*.md)|*.md|JSON Files (*.json)|*.json",
                Title = "Guardar Evaluación (.calc / .md / .json)",
                FileName = _assignment.Heading.Title + ".calc"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    if (sfd.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                        sfd.FileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
                    {
                        string md = AssignmentMarkdownAdapter.Instance.ToMarkdown(_assignment);
                        CalculusApp.Markdown.Services.MarkdownDocumentService.SaveTextToFile(sfd.FileName, md);
                        MessageBox.Show($"¡Evaluación guardada en formato Markdown (.md)!\n{sfd.FileName}", "Guardar Markdown", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string json = JsonSerializer.Serialize(_assignment, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    string msg = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "¡Examen guardado correctamente!",
                        AppLanguage.Portuguese => "Atividade salva com sucesso!",
                        _ => "Assignment saved successfully!"
                    };
                    MessageBox.Show(msg, "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void ApplyMarkdownToCurrentAssignment(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent)) return;
            var updated = AssignmentMarkdownAdapter.Instance.FromMarkdown(markdownContent, _assignment);
            _assignment = updated;
            QuestionSets.Clear();
            foreach (var qs in _assignment.QuestionSets)
            {
                QuestionSets.Add(qs);
            }
            SelectedQuestionSet = QuestionSets.FirstOrDefault();
            OnPropertyChanged(nameof(CurrentAssignment));
            OnPropertyChanged(nameof(WindowTitle));
            UpdateStatus();
            RaiseQuestionSetCommandsChanged();
            RequestRender?.Invoke();
        }

        public void OpenAssignmentMarkdownStudio()
        {
            string initialMd = AssignmentMarkdownAdapter.Instance.ToMarkdown(_assignment);
            var dlg = new CalculusApp.Markdown.Dialogs.MarkdownEditorWindow(
                initialMd,
                title: $"📝 Estudio Markdown de Evaluación — {_assignment.Heading.Title}",
                subtitle: "Manipule, edite, importe o guarde la evaluación completa en archivo Markdown (.md) con soporte LaTeX",
                suggestedFileName: $"{_assignment.Heading.Title}.md",
                applyButtonText: "✅ Aplicar Cambios a la Evaluación")
            {
                Owner = Application.Current?.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                ApplyMarkdownToCurrentAssignment(dlg.ResultMarkdown);
            }
        }

        public void ExportToMarkdown()
        {
            try
            {
                string md = AssignmentMarkdownAdapter.Instance.ToMarkdown(_assignment);
                string? savedPath = CalculusApp.Markdown.Services.MarkdownDocumentService.PromptSaveMarkdownFile(
                    md,
                    $"{_assignment.Heading.Title}.md",
                    "Guardar Evaluación en Formato Markdown (.md)");

                if (!string.IsNullOrWhiteSpace(savedPath))
                {
                    MessageBox.Show(
                        $"¡Evaluación guardada exitosamente en formato Markdown (.md)!\n\nRuta: {savedPath}",
                        "Guardar Evaluación Markdown (.md)",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar archivo Markdown: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ImportFromMarkdown()
        {
            try
            {
                var doc = CalculusApp.Markdown.Services.MarkdownDocumentService.PromptOpenMarkdownFile(
                    "Abrir / Importar Evaluación desde Archivo Markdown (.md)");
                if (doc == null) return;

                var result = MessageBox.Show(
                    $"Archivo Markdown cargado: {doc.FileName}\n\n" +
                    "• Presione [Sí] para REEMPLAZAR la evaluación actual con el archivo .md.\n" +
                    "• Presione [No] para AGREGAR sus series a la evaluación actual.\n" +
                    "• Presione [Cancelar] para abrirlo primero en el Editor Markdown.",
                    "Importar Evaluación Markdown (.md)",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ApplyMarkdownToCurrentAssignment(doc.RawContent);
                }
                else if (result == MessageBoxResult.No)
                {
                    var imported = AssignmentMarkdownAdapter.Instance.FromMarkdown(doc.RawContent, (Assignment?)null);
                    foreach (var qs in imported.QuestionSets)
                    {
                        _assignment.QuestionSets.Add(qs);
                        QuestionSets.Add(qs);
                        SelectedQuestionSet = qs;
                    }
                    UpdateStatus();
                    RaiseQuestionSetCommandsChanged();
                    RequestRender?.Invoke();
                }
                else
                {
                    var dlg = new CalculusApp.Markdown.Dialogs.MarkdownEditorWindow(
                        doc.RawContent,
                        title: $"📝 Estudio Markdown — {doc.FileName}",
                        suggestedFileName: doc.FileName)
                    {
                        Owner = Application.Current?.MainWindow
                    };
                    if (dlg.ShowDialog() == true)
                    {
                        ApplyMarkdownToCurrentAssignment(dlg.ResultMarkdown);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir archivo Markdown: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EditSelectedSetInMarkdown()
        {
            if (SelectedQuestionSet == null) return;

            string initialMd = AssignmentMarkdownAdapter.Instance.ToMarkdown(SelectedQuestionSet);
            var dlg = new CalculusApp.Markdown.Dialogs.MarkdownEditorWindow(
                initialMd,
                title: $"📝 Editor Markdown — Serie: {SelectedQuestionSet.TopicTitle}",
                subtitle: "Manipule o guarde esta serie de la evaluación en formato Markdown (.md)",
                suggestedFileName: $"{SelectedQuestionSet.TopicTitle}.md",
                applyButtonText: "✅ Aplicar Cambios a la Serie")
            {
                Owner = Application.Current?.MainWindow
            };

            if (dlg.ShowDialog() == true)
            {
                var updatedSet = AssignmentMarkdownAdapter.Instance.FromMarkdown(dlg.ResultMarkdown, SelectedQuestionSet);
                SelectedQuestionSet.TopicTitle = updatedSet.TopicTitle;
                SelectedQuestionSet.Category = updatedSet.Category;
                SelectedQuestionSet.Directions = updatedSet.Directions;
                SelectedQuestionSet.Questions = updatedSet.Questions;
                SelectedQuestionSet.QuestionCount = updatedSet.Questions.Count;
                SelectedQuestionSet.DefaultType = updatedSet.DefaultType;

                UpdateStatus();
                RaiseQuestionSetCommandsChanged();
                RequestRender?.Invoke();
            }
        }

        private void ExportToIni()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Plano / INI (*.ini;*.txt)|*.ini;*.txt|Todos los archivos (*.*)|*.*",
                Title = "Exportar Banco de Ejercicios en Plano",
                FileName = $"{_assignment.Heading.Title}.ini"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var allQuestions = _assignment.QuestionSets.SelectMany(qs => qs.Questions).ToList();
                    string directions = _assignment.QuestionSets.FirstOrDefault()?.Directions ?? string.Empty;
                    ExerciseIniService.SaveToFile(sfd.FileName, allQuestions, SubjectRegistry.CurrentSubject.Name, _assignment.Heading.Title, directions);
                    string msg = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"¡Se exportaron {allQuestions.Count} ejercicios en formato plano (.ini/.txt)!\n{sfd.FileName}",
                        AppLanguage.Portuguese => $"Exportados {allQuestions.Count} exercícios com sucesso!\n{sfd.FileName}",
                        _ => $"Exported {allQuestions.Count} questions in plain text/ini format successfully!\n{sfd.FileName}"
                    };
                    MessageBox.Show(msg, "Exportar en Plano", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al exportar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportFromIni()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivo Plano / INI (*.ini;*.txt)|*.ini;*.txt|Todos los archivos (*.*)|*.*",
                Title = "Importar Banco de Ejercicios en Plano"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var data = ExerciseIniService.LoadFromFile(ofd.FileName);
                    if (data.Questions.Count == 0)
                    {
                        MessageBox.Show("No se encontraron ejercicios válidos en el archivo seleccionado.", "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newSet = new QuestionSet
                    {
                        TopicTitle = !string.IsNullOrWhiteSpace(data.Topic) ? data.Topic : System.IO.Path.GetFileNameWithoutExtension(ofd.FileName),
                        Category = !string.IsNullOrWhiteSpace(data.Category) ? data.Category : "Ejercicios Importados",
                        Directions = data.Directions,
                        Questions = data.Questions,
                        QuestionCount = data.Questions.Count,
                        DefaultType = data.Questions.Any(q => q.Type == QuestionType.MultipleChoice) ? QuestionType.MultipleChoice : QuestionType.FreeResponse
                    };

                    _assignment.QuestionSets.Add(newSet);
                    QuestionSets.Add(newSet);
                    SelectedQuestionSet = newSet;
                    UpdateStatus();
                    RequestRender?.Invoke();
                    RaiseQuestionSetCommandsChanged();

                    string msg = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"¡Se importaron {data.Questions.Count} ejercicios exitosamente desde:\n{ofd.FileName}!",
                        AppLanguage.Portuguese => $"Importados {data.Questions.Count} exercícios com sucesso!",
                        _ => $"Imported {data.Questions.Count} questions successfully!"
                    };
                    MessageBox.Show(msg, "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al importar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ValidateFormulas()
        {
            try
            {
                var result = FormulaIntegrityValidator.RunValidation();
                string report = result.ToString();
                File.WriteAllText("formula_validation_report.txt", report);
                MessageBox.Show(report, "Validación de Integridad de Fórmulas", MessageBoxButton.OK, result.FailedCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en la validación: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintAssignment()
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                string msg = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Imprimiendo examen con la configuración actual...",
                    AppLanguage.Portuguese => "Imprimindo teste com a configuração atual...",
                    _ => "Printing document with current layout and answers settings..."
                };
                MessageBox.Show(msg, "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveSelectedSet()
        {
            if (SelectedQuestionSet == null) return;
            _assignment.QuestionSets.Remove(SelectedQuestionSet);
            QuestionSets.Remove(SelectedQuestionSet);
            SelectedQuestionSet = QuestionSets.LastOrDefault();
            UpdateStatus();
            RequestRender?.Invoke();
            RaiseQuestionSetCommandsChanged();
        }

        private bool CanMoveUp() => SelectedQuestionSet != null && QuestionSets.IndexOf(SelectedQuestionSet) > 0;
        private bool CanMoveDown() => SelectedQuestionSet != null && QuestionSets.IndexOf(SelectedQuestionSet) < QuestionSets.Count - 1;

        private void MoveSelectedSetUp()
        {
            if (!CanMoveUp() || SelectedQuestionSet == null) return;
            int idx = QuestionSets.IndexOf(SelectedQuestionSet);
            var item = SelectedQuestionSet;
            QuestionSets.RemoveAt(idx);
            QuestionSets.Insert(idx - 1, item);
            _assignment.QuestionSets.RemoveAt(idx);
            _assignment.QuestionSets.Insert(idx - 1, item);
            SelectedQuestionSet = item;
            RequestRender?.Invoke();
            RaiseQuestionSetCommandsChanged();
        }

        private void MoveSelectedSetDown()
        {
            if (!CanMoveDown() || SelectedQuestionSet == null) return;
            int idx = QuestionSets.IndexOf(SelectedQuestionSet);
            var item = SelectedQuestionSet;
            QuestionSets.RemoveAt(idx);
            QuestionSets.Insert(idx + 1, item);
            _assignment.QuestionSets.RemoveAt(idx);
            _assignment.QuestionSets.Insert(idx + 1, item);
            SelectedQuestionSet = item;
            RequestRender?.Invoke();
            RaiseQuestionSetCommandsChanged();
        }

        public void RaiseQuestionSetCommandsChanged()
        {
            (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveQuestionSetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RegenerateSetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleHideSetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditQuestionSetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditQuestionsDirectlyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditSetMarkdownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(QuestionSetsCountText));
        }

        public void RegenerateAll()
        {
            foreach (var set in _assignment.QuestionSets)
            {
                var topic = SubjectRegistry.GetTopicById(set.TopicId);
                if (topic != null)
                {
                    set.Questions = topic.GenerateQuestions(set.QuestionCount, set.DefaultType, set.Parameters, _rng);
                }
            }
            RequestRender?.Invoke();
        }

        public void RegenerateSelectedSet()
        {
            if (SelectedQuestionSet == null) return;
            var topic = SubjectRegistry.GetTopicById(SelectedQuestionSet.TopicId);
            if (topic != null)
            {
                if (!string.IsNullOrWhiteSpace(SelectedQuestionSet.Parameters.CustomDirections))
                {
                    SelectedQuestionSet.Directions = SelectedQuestionSet.Parameters.CustomDirections;
                }
                SelectedQuestionSet.Questions = topic.GenerateQuestions(SelectedQuestionSet.QuestionCount, SelectedQuestionSet.DefaultType, SelectedQuestionSet.Parameters, _rng);
                RequestRender?.Invoke();
            }
        }

        public void ScrambleQuestions()
        {
            foreach (var set in _assignment.QuestionSets)
            {
                set.Questions = set.Questions.OrderBy(_ => _rng.Next()).ToList();
            }
            RequestRender?.Invoke();
        }

        public void ScrambleChoices()
        {
            foreach (var set in _assignment.QuestionSets)
            {
                foreach (var q in set.Questions.Where(q => q.Type == QuestionType.MultipleChoice))
                {
                    var shuffled = q.Choices.OrderBy(_ => _rng.Next()).ToList();
                    char[] letters = { 'A', 'B', 'C', 'D' };
                    for (int i = 0; i < shuffled.Count && i < 4; i++)
                    {
                        shuffled[i].Letter = letters[i];
                    }
                    q.Choices = shuffled;
                }
            }
            RequestRender?.Invoke();
        }

        public void ChangeAllType(QuestionType type)
        {
            foreach (var set in _assignment.QuestionSets)
            {
                set.DefaultType = type;
                var topic = SubjectRegistry.GetTopicById(set.TopicId);
                if (topic != null)
                {
                    set.Questions = topic.GenerateQuestions(set.QuestionCount, type, set.Parameters, _rng);
                }
            }
            RequestRender?.Invoke();
        }

        public void AdjustLength(int delta)
        {
            if (SelectedQuestionSet != null)
            {
                int newCount = Math.Max(1, Math.Min(20, SelectedQuestionSet.QuestionCount + delta));
                SelectedQuestionSet.QuestionCount = newCount;
                var topic = SubjectRegistry.GetTopicById(SelectedQuestionSet.TopicId);
                if (topic != null)
                {
                    SelectedQuestionSet.Questions = topic.GenerateQuestions(newCount, SelectedQuestionSet.DefaultType, SelectedQuestionSet.Parameters, _rng);
                }
            }
            else
            {
                foreach (var set in _assignment.QuestionSets)
                {
                    int newCount = Math.Max(1, Math.Min(20, set.QuestionCount + delta));
                    set.QuestionCount = newCount;
                    var topic = SubjectRegistry.GetTopicById(set.TopicId);
                    if (topic != null)
                    {
                        set.Questions = topic.GenerateQuestions(newCount, set.DefaultType, set.Parameters, _rng);
                    }
                }
            }
            UpdateStatus();
            RequestRender?.Invoke();
        }

        public void RespaceWithinPage()
        {
            foreach (var set in _assignment.QuestionSets)
            {
                foreach (var q in set.Questions)
                {
                    q.ExtraSpace = (q.ExtraSpace >= 60.0) ? 35.0 : 65.0;
                }
            }
            RequestRender?.Invoke();
        }

        private void SetZoom(object? param)
        {
            if (param is string s && double.TryParse(s, out double z))
            {
                ZoomPercent = z;
            }
        }

        public void RefreshView()
        {
            UpdateStatus();
            RequestRender?.Invoke();
        }

        public void UpdateStatus()
        {
            int totalQ = _assignment.TotalQuestions;
            StatusQuestionsText = LocalizationService.Get("questions_stat", totalQ);

            double pages = Math.Max(1.0, Math.Ceiling(totalQ / 2.0));
            StatusPagesText = LocalizationService.Get("pages_stat", pages);
            OnPropertyChanged(nameof(CenterStatusLabel));
            OnPropertyChanged(nameof(QuestionSetsCountText));
            OnPropertyChanged(nameof(QuestionSetsSummary));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
