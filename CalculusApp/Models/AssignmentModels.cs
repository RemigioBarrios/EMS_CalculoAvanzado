using System;
using System.Collections.Generic;

namespace CalculusApp.Models
{
    public enum QuestionType
    {
        FreeResponse,
        MultipleChoice
    }

    public class QuestionChoice
    {
        public char Letter { get; set; } // 'A', 'B', 'C', 'D'
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class Question
    {
        public int Number { get; set; }
        public string ProblemStatement { get; set; } = string.Empty;
        public string FormulaText { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.FreeResponse;
        public List<QuestionChoice> Choices { get; set; } = new();
        public string AnswerText { get; set; } = string.Empty;
        public string SolutionSteps { get; set; } = string.Empty;
        public double ExtraSpace { get; set; } = 40.0; // Space for student work

        // Graphing properties
        public bool ProvideBlankGraph { get; set; } = false;
        public bool ProvideFunctionGraph { get; set; } = false;
        public string GraphFormula { get; set; } = string.Empty;
        public double GraphXMin { get; set; } = -6;
        public double GraphXMax { get; set; } = 6;
        public double GraphYMin { get; set; } = -6;
        public double GraphYMax { get; set; } = 6;
        public List<System.Windows.Point> GraphHoles { get; set; } = new();
        public List<System.Windows.Point> GraphPoints { get; set; } = new();
    }

    public class QuestionSet : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private string _topicTitle = string.Empty;
        private string _category = string.Empty;
        private string _directions = string.Empty;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TopicId { get; set; } = string.Empty;
        public string TopicTitle
        {
            get => _topicTitle;
            set { _topicTitle = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TopicTitle))); }
        }
        public string Category
        {
            get => _category;
            set { _category = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Category))); }
        }
        public string Directions
        {
            get => _directions;
            set { _directions = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Directions))); }
        }
        public int QuestionCount { get; set; } = 4;
        public QuestionType DefaultType { get; set; } = QuestionType.FreeResponse;
        public TopicGeneratorParameters Parameters { get; set; } = new();
        public List<Question> Questions { get; set; } = new();

        public override string ToString() => $"{TopicTitle} ({Questions.Count} questions)";
    }

    public class TopicGeneratorParameters
    {
        public int QuestionCount { get; set; } = 4;
        public QuestionType QuestionType { get; set; } = QuestionType.FreeResponse;
        public string Preset { get; set; } = "Medium"; // "Easy", "Medium", "Hard"
        public string Notation { get; set; } = "y="; // "y=", "f(x)=", "Either"
        public bool VaryLetters { get; set; } = false;
        public bool ProvideBlankGraph { get; set; } = false;
        public bool ProvideFunctionGraph { get; set; } = false;

        public int ComplexityOptionIndex { get; set; } = 0;
        public bool IncludePolynomial { get; set; } = true;
        public bool IncludeQuadratic { get; set; } = true;
        public bool IncludeCubic { get; set; } = false;
        public bool IncludeQuartic { get; set; } = false;
        public bool IncludeRational { get; set; } = false;
        public bool IncludeRadical { get; set; } = false;
        public bool IncludeTrig { get; set; } = false;
        public bool IncludeExp { get; set; } = false;
        public bool IncludeLog { get; set; } = false;
        public bool IncludeAbs { get; set; } = false;
        public bool IncludeInverseTrig { get; set; } = false;
        public bool IncludeRationalExpEasy { get; set; } = false;
        public bool IncludeRationalExpHard { get; set; } = false;
        public bool IncludeExpLog { get; set; } = false;
        public string? CustomDirections { get; set; }
    }

    public class AssignmentHeading
    {
        public string Title { get; set; } = "Assignment";
        public string Subtitle { get; set; } = "Infinite Calculus";
        public bool ShowName { get; set; } = true;
        public bool ShowDate { get; set; } = true;
        public bool ShowPeriod { get; set; } = true;
        public string Teacher { get; set; } = string.Empty;
    }

    public class Assignment
    {
        public AssignmentHeading Heading { get; set; } = new();
        public List<QuestionSet> QuestionSets { get; set; } = new();
        public int Columns { get; set; } = 2; // Default 2 columns like Kuta
        public bool ShowAnswers { get; set; } = false;
        public bool ShowPunchHoles { get; set; } = false;
        public double ZoomPercent { get; set; } = 100.0;
        public int StartingQuestionNumber { get; set; } = 1;

        public int TotalQuestions
        {
            get
            {
                int count = 0;
                foreach (var qs in QuestionSets) count += qs.Questions.Count;
                return count;
            }
        }
    }
}
