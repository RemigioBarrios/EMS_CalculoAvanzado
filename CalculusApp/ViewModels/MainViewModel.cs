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

        public QuestionSet? SelectedQuestionSet
        {
            get => _selectedQuestionSet;
            set
            {
                _selectedQuestionSet = value;
                OnPropertyChanged();
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
                _assignment.Columns = value;
                OnPropertyChanged();
                RequestRender?.Invoke();
            }
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
        public bool IsDarkTheme => ThemeService.IsDark;
        public string ThemeStatusText => ThemeService.IsDark ? LocalizationService.Get("tb_theme_dark") : LocalizationService.Get("tb_theme_light");
        #endregion

        public MainViewModel()
        {
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

            ThemeService.ThemeChanged += () =>
            {
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeStatusText));
                OnPropertyChanged(string.Empty);
            };

            LocalizationService.LanguageChanged += OnLanguageChanged;

            LoadTopicsTree();
            CreateDefaultSampleAssignment();
        }

        private void OnLanguageChanged()
        {
            LoadTopicsTree();
            UpdateStatus();

            if (_assignment.Heading.Title == "Assignment" || _assignment.Heading.Title == "Guía de Ejercicios" || _assignment.Heading.Title == "Lista de Exercícios")
            {
                _assignment.Heading.Title = LocalizationService.Get("ws_assignment");
            }

            // Also update directions for current question sets to the new language
            foreach (var qs in _assignment.QuestionSets)
            {
                var topic = TopicRegistry.GetById(qs.TopicId);
                if (topic != null)
                {
                    qs.TopicTitle = topic.Title;
                    qs.Category = topic.Category;
                    qs.Directions = topic.DefaultDirections;
                }
            }

            OnPropertyChanged(string.Empty);
            RequestRender?.Invoke();
        }

        private static int GetCategoryOrder(string catId) => catId switch
        {
            "limits" => 1,
            "continuity" => 2,
            "differentiation" => 3,
            "app_diff" => 4,
            "indef_int" => 5,
            "def_int" => 6,
            "app_int" => 7,
            "diff_eq" => 8,
            _ => 99
        };

        private void LoadTopicsTree()
        {
            TopicCategories.Clear();

            var groups = TopicRegistry.AllTopics
                .GroupBy(t => t.Category)
                .OrderBy(g => GetCategoryOrder(g.First().CategoryId));

            foreach (var group in groups)
            {
                var catItem = new TopicTreeItem
                {
                    Title = group.Key,
                    IsCategory = true
                };

                foreach (var topic in group)
                {
                    catItem.Children.Add(new TopicTreeItem
                    {
                        Title = topic.Title,
                        TopicId = topic.TopicId,
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

            var matched = TopicRegistry.AllTopics
                .Where(t => t.Title.ToLowerInvariant().Contains(term) ||
                            t.Category.ToLowerInvariant().Contains(term) ||
                            t.TitleEn.ToLowerInvariant().Contains(term) ||
                            t.TitleEs.ToLowerInvariant().Contains(term) ||
                            t.TitlePt.ToLowerInvariant().Contains(term))
                .GroupBy(t => t.Category)
                .OrderBy(g => GetCategoryOrder(g.First().CategoryId));

            foreach (var group in matched)
            {
                var catItem = new TopicTreeItem
                {
                    Title = group.Key,
                    IsCategory = true
                };

                foreach (var topic in group)
                {
                    catItem.Children.Add(new TopicTreeItem
                    {
                        Title = topic.Title,
                        TopicId = topic.TopicId,
                        IsCategory = false
                    });
                }

                TopicCategories.Add(catItem);
            }
        }

        public void AddTopic(string? topicId, int count = 4, QuestionType type = QuestionType.FreeResponse, TopicGeneratorParameters? parameters = null, List<Question>? initialQuestions = null)
        {
            if (string.IsNullOrEmpty(topicId)) return;
            var topic = TopicRegistry.GetById(topicId);
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

        private void CreateDefaultSampleAssignment()
        {
            _assignment.Heading.Title = LocalizationService.Get("ws_assignment");
            _assignment.QuestionSets.Clear();
            QuestionSets.Clear();

            AddTopic("deriv_power", 4, QuestionType.FreeResponse);
            AddTopic("limits_direct", 4, QuestionType.MultipleChoice);
        }

        private void CreateNewAssignment()
        {
            string msg = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "¿Desea crear un nuevo examen de cálculo?",
                AppLanguage.Portuguese => "Deseja criar um novo teste de cálculo?",
                _ => "Create a new calculus assignment?"
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
                QuestionSets.Clear();
                UpdateStatus();
                RequestRender?.Invoke();
            }
        }

        private void OpenAssignment()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Calculus Assignment (*.calc)|*.calc|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Calculus Assignment"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
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
                Filter = "Calculus Assignment (*.calc)|*.calc|JSON Files (*.json)|*.json",
                Title = "Save Calculus Assignment",
                FileName = _assignment.Heading.Title + ".calc"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
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
        }

        public void RegenerateAll()
        {
            foreach (var set in _assignment.QuestionSets)
            {
                var topic = TopicRegistry.GetById(set.TopicId);
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
            var topic = TopicRegistry.GetById(SelectedQuestionSet.TopicId);
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
                var topic = TopicRegistry.GetById(set.TopicId);
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
                var topic = TopicRegistry.GetById(SelectedQuestionSet.TopicId);
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
                    var topic = TopicRegistry.GetById(set.TopicId);
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

            double pages = Math.Max(1.0, Math.Ceiling(totalQ / 8.0));
            StatusPagesText = LocalizationService.Get("pages_stat", pages);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
