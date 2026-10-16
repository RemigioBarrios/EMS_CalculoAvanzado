using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CalculusApp.Generators;
using CalculusApp.Models;
using CalculusApp.Services;
using WpfMath.Controls;

namespace CalculusApp.Controls
{
    public partial class WorksheetView : UserControl
    {
        private Assignment? _assignment;
        private double _currentZoom = 1.0;

        public const double PageWidth = 816.0;  // 8.5 inches at 96 DPI
        public const double PageHeight = 1056.0; // 11.0 inches at 96 DPI
        public const double PageMargin = 45.0;

        public WorksheetView()
        {
            InitializeComponent();
        }

        public Assignment? CurrentAssignment => _assignment;

        public List<Border> GetPageBorders()
        {
            var list = new List<Border>();
            foreach (var child in PagesContainer.Children)
            {
                if (child is Border b)
                {
                    list.Add(b);
                }
            }
            return list;
        }

        public static FrameworkElement RenderMath(string mathOrText, double scale = 14.0, Brush? foreground = null, bool isBold = false)
        {
            foreground ??= Brushes.Black;
            if (string.IsNullOrWhiteSpace(mathOrText))
            {
                return new TextBlock();
            }

            string trimmed = mathOrText.Trim();

            // 1. If it contains '$' delimiters, delegate to RenderMixedTextAndMath
            if (trimmed.Contains('$'))
            {
                return RenderMixedTextAndMath(trimmed, scale: scale, foreground: foreground, isBold: isBold);
            }

            // 2. If it contains known labels like "Máximo...", "Mínimo...", "Relative max...", "Critical...", "Sin solución", "No solution":
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"(máximo|mínimo|puntos?|extremos?|relative\s*m|absolute\s*m|critical\s*p|sin solución|no solution)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                // Normalize legacy math portions into $ delimiters
                string normalized = System.Text.RegularExpressions.Regex.Replace(trimmed, @"([A-Za-zÁÉÍÓÚáéíóúñÑüÜ\s]+[:;,]\s*)([^\$;\n]+)", m => $"{m.Groups[1].Value}${m.Groups[2].Value.Trim()}$");
                if (normalized.Contains('$'))
                {
                    return RenderMixedTextAndMath(normalized, scale: scale, foreground: foreground, isBold: isBold);
                }
            }

            // If it is wrapped in \text{...} entirely, extract the plain text and render as TextBlock for crisp typography
            if (trimmed.StartsWith(@"\text{") && trimmed.EndsWith("}") && !trimmed.Substring(6, trimmed.Length - 7).Contains('\\'))
            {
                string plain = trimmed.Substring(6, trimmed.Length - 7);
                return new TextBlock
                {
                    Text = plain,
                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                    FontSize = scale * 0.95,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = FontStyles.Normal,
                    Foreground = foreground,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            // If pure text without LaTeX commands or math symbols
            if (!trimmed.Contains('\\') && !trimmed.Contains('^') && !trimmed.Contains('_') &&
                !trimmed.Contains('=') && !trimmed.Contains('/') && !trimmed.Contains('+') &&
                trimmed.Contains(' '))
            {
                return new TextBlock
                {
                    Text = trimmed,
                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                    FontSize = scale * 0.95,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = FontStyles.Normal,
                    Foreground = foreground,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            if (trimmed.Contains(@"\begin{array}"))
            {
                int arrayIdx = trimmed.IndexOf(@"\begin{array}");
                string prefix = trimmed.Substring(0, arrayIdx).TrimEnd(',', ' ', ';', '\\');
                string arrayStr = trimmed.Substring(arrayIdx);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    var prefixMath = RenderMath(prefix, scale, foreground, isBold);
                    prefixMath.VerticalAlignment = VerticalAlignment.Center;
                    prefixMath.Margin = new Thickness(0, 0, 16, 0);
                    panel.Children.Add(prefixMath);
                }

                var tableGrid = CreateTableGrid(arrayStr, scale, foreground);
                tableGrid.VerticalAlignment = VerticalAlignment.Center;
                panel.Children.Add(tableGrid);

                return panel;
            }

            // Sanitize LaTeX artifacts before WpfMath parser
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\\infty(?:ty)+", @"\infty");
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"(?:\\left\s*)+(\[|\(|\{|\.\|)", @"\left$1");
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"(?:\\right\s*)+(\]|\)|\}|\.\|)", @"\right$1");
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\\left(?![\[\(\{\.\|])", "");
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\\right(?![\]\)\}\.\|])", "");

            try
            {
                var formula = new FormulaControl
                {
                    Formula = trimmed,
                    Scale = scale,
                    Foreground = foreground,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true
                };

                if (!formula.HasError)
                {
                    return formula;
                }
                else
                {
                    try { System.IO.File.AppendAllText("formula_errors.txt", $"[HasError] '{trimmed}'\n"); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText("formula_errors.txt", $"[Exception] '{trimmed}' -> {ex.Message}\n"); } catch { }
            }

            return new TextBlock
            {
                Text = trimmed,
                FontFamily = new FontFamily("Cambria Math, Times New Roman, Georgia"),
                FontSize = scale * 0.9,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = FontStyles.Normal,
                Foreground = foreground,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static FrameworkElement CreateTableGrid(string arrayLatex, double scale, Brush? foreground)
        {
            foreground ??= Brushes.Black;

            var match = System.Text.RegularExpressions.Regex.Match(arrayLatex, @"\\begin\{array\}[^}]*\}(.*?)\\end\{array\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            string body = match.Success ? match.Groups[1].Value : arrayLatex;

            var rawRows = body.Split(new[] { @"\\" }, StringSplitOptions.RemoveEmptyEntries);
            var rowsData = new List<List<string>>();

            foreach (var r in rawRows)
            {
                string cleanRow = r.Replace(@"\hline", "").Trim();
                if (string.IsNullOrWhiteSpace(cleanRow)) continue;

                var cells = cleanRow.Split('&');
                var cellList = new List<string>();
                foreach (var c in cells)
                {
                    cellList.Add(c.Trim());
                }
                if (cellList.Count > 0)
                {
                    rowsData.Add(cellList);
                }
            }

            if (rowsData.Count == 0)
            {
                return new TextBlock { Text = arrayLatex };
            }

            int numRows = rowsData.Count;
            int numCols = rowsData.Max(r => r.Count);

            var outerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(140, 140, 150)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(4, 2, 4, 2)
            };

            var grid = new Grid();
            for (int c = 0; c < numCols; c++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }
            for (int r = 0; r < numRows; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            for (int r = 0; r < numRows; r++)
            {
                for (int c = 0; c < rowsData[r].Count; c++)
                {
                    string cellVal = rowsData[r][c];

                    var cellBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(10, 4, 10, 4),
                        Background = (r == 0 || c == 0) ? new SolidColorBrush(Color.FromArgb(14, 0, 0, 0)) : Brushes.Transparent
                    };

                    var cellText = new TextBlock
                    {
                        Text = cellVal,
                        FontFamily = new FontFamily("Cambria Math, Times New Roman"),
                        FontSize = scale * 0.92,
                        FontWeight = (r == 0 || c == 0) ? FontWeights.SemiBold : FontWeights.Normal,
                        FontStyle = (c == 0) ? FontStyles.Italic : FontStyles.Normal,
                        Foreground = foreground,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    cellBorder.Child = cellText;
                    Grid.SetRow(cellBorder, r);
                    Grid.SetColumn(cellBorder, c);
                    grid.Children.Add(cellBorder);
                }
            }

            outerBorder.Child = grid;
            return outerBorder;
        }

        public static FrameworkElement RenderMixedTextAndMath(string content, double scale = 11.5, Brush? foreground = null, bool isBold = false)
        {
            foreground ??= new SolidColorBrush(Color.FromRgb(80, 20, 20));
            if (string.IsNullOrWhiteSpace(content))
            {
                return new TextBlock();
            }

            var stack = new StackPanel
            {
                Margin = new Thickness(0, 3, 0, 0)
            };

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var linePanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 1, 0, 1),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 1. Check if line has $ delimiters (e.g. "Sustitución directa: $2(4)^2 + 3(4) - 1 = 43$")
                if (line.Contains('$'))
                {
                    var tokens = line.Split('$');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string token = tokens[i];
                        if (string.IsNullOrEmpty(token)) continue;

                        if (i % 2 == 0)
                        {
                            // Text token
                            bool isPunctuation = token.StartsWith(".") || token.StartsWith(",");
                            var tb = new TextBlock
                            {
                                Text = token,
                                FontFamily = new FontFamily("Times New Roman, Cambria"),
                                FontSize = scale,
                                FontWeight = isBold ? FontWeights.Bold : ((token.TrimEnd().EndsWith(':')) ? FontWeights.SemiBold : FontWeights.Normal),
                                Foreground = foreground,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = isPunctuation ? new Thickness(-2, 0, 5, 0) : new Thickness(0, 0, 3, 0)
                            };
                            linePanel.Children.Add(tb);
                        }
                        else
                        {
                            // Math token in LaTeX
                            var mathElem = RenderMath(token, scale: scale * 1.05, foreground: foreground, isBold: isBold);
                            mathElem.VerticalAlignment = VerticalAlignment.Center;
                            mathElem.Margin = new Thickness(0, 0, 4, 0);
                            linePanel.Children.Add(mathElem);
                        }
                    }
                }
                else if (line.Contains(": ") && !line.StartsWith(@"\"))
                {
                    // Fallback for lines without $ like "Label: math"
                    int colonIdx = line.IndexOf(": ");
                    string label = line.Substring(0, colonIdx + 1);
                    string math = line.Substring(colonIdx + 2).Trim();

                    var tb = new TextBlock
                    {
                        Text = label + " ",
                        FontFamily = new FontFamily("Times New Roman, Cambria"),
                        FontSize = scale,
                        FontWeight = isBold ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = foreground,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 3, 0)
                    };
                    linePanel.Children.Add(tb);

                    string formattedMath = LatexFormatter.ToLatex(math);
                    var mathElem = RenderMath(formattedMath, scale: scale * 1.05, foreground: foreground, isBold: isBold);
                    mathElem.VerticalAlignment = VerticalAlignment.Center;
                    linePanel.Children.Add(mathElem);
                }
                else
                {
                    // Single math expression or plain text
                    bool hasMath = line.Contains('=') || line.Contains('^') || line.Contains('\\') || line.Contains('/') || line.Contains('+');
                    if (hasMath)
                    {
                        string formattedMath = LatexFormatter.ToLatex(line);
                        var mathElem = RenderMath(formattedMath, scale: scale * 1.05, foreground: foreground, isBold: isBold);
                        mathElem.VerticalAlignment = VerticalAlignment.Center;
                        linePanel.Children.Add(mathElem);
                    }
                    else
                    {
                        var tb = new TextBlock
                        {
                            Text = line,
                            FontFamily = new FontFamily("Times New Roman, Cambria"),
                            FontSize = scale,
                            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                            Foreground = foreground,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        };
                        linePanel.Children.Add(tb);
                    }
                }

                stack.Children.Add(linePanel);
            }

            if (stack.Children.Count == 1 && stack.Children[0] is FrameworkElement singleChild)
            {
                stack.Children.Clear();
                return singleChild;
            }

            return stack;
        }

        public void SetZoom(double zoom)
        {
            _currentZoom = zoom;
            PagesContainer.LayoutTransform = new ScaleTransform(_currentZoom, _currentZoom);
        }

        public void RenderAssignment(Assignment assignment)
        {
            _assignment = assignment;
            PagesContainer.Children.Clear();

            if (assignment == null) return;

            // Renumber all questions sequentially and collect them
            int currentNum = assignment.StartingQuestionNumber;
            var allFlatQuestions = new List<Question>();

            var sections = new List<(string directions, string topicTitle, List<List<Question>> rows)>();
            int colCount = Math.Max(1, Math.Min(3, assignment.Columns));

            foreach (var set in assignment.QuestionSets)
            {
                var setQuestions = new List<Question>();
                foreach (var q in set.Questions)
                {
                    q.Number = currentNum++;
                    allFlatQuestions.Add(q);
                    setQuestions.Add(q);
                }

                if (setQuestions.Count > 0)
                {
                    var rows = new List<List<Question>>();
                    for (int i = 0; i < setQuestions.Count; i += colCount)
                    {
                        var rowChunk = setQuestions.GetRange(i, Math.Min(colCount, setQuestions.Count - i));
                        rows.Add(rowChunk);
                    }

                    sections.Add((set.Directions, set.TopicTitle, rows));
                }
            }

            int pageNumber = 1;
            var page = CreateEmptyPage(assignment, pageNumber);
            var contentPanel = (StackPanel)page.Tag;
            contentPanel.Children.Add(CreateHeader(assignment.Heading));
            PagesContainer.Children.Add(page);

            double currentHeight = 0;
            double maxHeight = 800.0;

            void StartNewPage()
            {
                pageNumber++;
                page = CreateEmptyPage(assignment, pageNumber);
                contentPanel = (StackPanel)page.Tag;
                PagesContainer.Children.Add(page);
                currentHeight = 0;
                maxHeight = 910.0;
            }

            foreach (var section in sections)
            {
                double dirHeight = EstimateDirectionsHeight(section.directions);
                double firstRowHeight = section.rows.Count > 0 ? EstimateRowHeight(section.rows[0]) : 0;

                // If adding directions + first row would overflow current page, start a new page first
                if (currentHeight > 0 && (currentHeight + dirHeight + firstRowHeight > maxHeight))
                {
                    StartNewPage();
                }

                // Add section directions full-width across all columns
                if (!string.IsNullOrWhiteSpace(section.directions))
                {
                    var dirBlock = new TextBlock
                    {
                        Text = section.directions,
                        FontFamily = new FontFamily("Times New Roman, Cambria, Georgia"),
                        FontStyle = FontStyles.Italic,
                        FontSize = 13.0,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        Margin = new Thickness(0, currentHeight > 0 ? 14 : 6, 0, 8),
                        TextWrapping = TextWrapping.Wrap
                    };
                    contentPanel.Children.Add(dirBlock);
                    currentHeight += dirHeight;
                }

                // Add question rows
                foreach (var row in section.rows)
                {
                    double rowHeight = EstimateRowHeight(row);
                    if (currentHeight > 0 && (currentHeight + rowHeight > maxHeight))
                    {
                        StartNewPage();
                    }

                    var rowGrid = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0, 4, 0, 16)
                    };

                    for (int c = 0; c < colCount; c++)
                    {
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        if (c < colCount - 1)
                        {
                            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });
                        }
                    }

                    for (int c = 0; c < row.Count && c < colCount; c++)
                    {
                        var q = row[c];
                        var qView = CreateQuestionElement(q, assignment.ShowAnswers);
                        qView.VerticalAlignment = VerticalAlignment.Top;
                        qView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        Grid.SetColumn(qView, c * 2);
                        rowGrid.Children.Add(qView);
                    }

                    contentPanel.Children.Add(rowGrid);
                    currentHeight += rowHeight;
                }
            }

            // If ShowAnswers is true, add Answer Key Page
            if (assignment.ShowAnswers && allFlatQuestions.Count > 0)
            {
                var answerKeyPage = CreateAnswerKeyPage(assignment, allFlatQuestions, pageNumber + 1);
                PagesContainer.Children.Add(answerKeyPage);
            }
        }

        private double EstimateQuestionHeight(Question q)
        {
            double h = 34.0;
            if (q.Type == QuestionType.MultipleChoice && q.Choices.Count > 0)
            {
                bool isLongChoices = q.Choices.Any(c => c.Text.Length > 20 || c.Text.Contains('\n'));
                if (isLongChoices)
                {
                    h += q.Choices.Count * 22.0 + 8.0;
                }
                else
                {
                    int rowCount = (q.Choices.Count + 1) / 2;
                    h += rowCount * 22.0 + 8.0;
                }
            }
            else if (q.Type == QuestionType.FreeResponse)
            {
                h += Math.Max(25.0, q.ExtraSpace);
            }

            if (q.ProvideBlankGraph || q.ProvideFunctionGraph)
            {
                h += 228.0;
            }

            if (_assignment != null && _assignment.ShowAnswers)
            {
                h += 34.0;
                if (!string.IsNullOrEmpty(q.SolutionSteps))
                    h += 32.0;
            }

            return h + 10.0;
        }

        private double EstimateRowHeight(List<Question> rowQuestions)
        {
            double maxQ = 0;
            foreach (var q in rowQuestions)
            {
                double qh = EstimateQuestionHeight(q);
                if (qh > maxQ) maxQ = qh;
            }
            return maxQ + 20.0;
        }

        private double EstimateDirectionsHeight(string directions)
        {
            if (string.IsNullOrWhiteSpace(directions)) return 0;
            int lines = Math.Max(1, (directions.Length + 79) / 80);
            return lines * 18.0 + 22.0;
        }

        private Border CreateEmptyPage(Assignment assignment, int pageNum)
        {
            var pageBorder = new Border
            {
                Width = PageWidth,
                Height = PageHeight,
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 24),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 185)),
                BorderThickness = new Thickness(1),
                Effect = (System.Windows.Media.Effects.DropShadowEffect)FindResource("PageShadow")
            };

            var grid = new Grid();

            // Optional 3-hole punch markers
            if (assignment.ShowPunchHoles)
            {
                double[] holePositions = { 120.0, 528.0, 936.0 };
                foreach (double pos in holePositions)
                {
                    var hole = new Ellipse
                    {
                        Width = 24,
                        Height = 24,
                        Stroke = new SolidColorBrush(Color.FromArgb(90, 100, 100, 100)),
                        StrokeThickness = 1,
                        Fill = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(14, pos, 0, 0)
                    };
                    grid.Children.Add(hole);
                }
            }

            var contentStack = new StackPanel
            {
                Margin = new Thickness(PageMargin, PageMargin, PageMargin, 50.0)
            };

            grid.Children.Add(contentStack);

            var footer = CreateFooter(assignment.Heading.Subtitle, pageNum);
            footer.VerticalAlignment = VerticalAlignment.Bottom;
            footer.Margin = new Thickness(PageMargin, 0, PageMargin, 20.0);
            grid.Children.Add(footer);

            pageBorder.Child = grid;
            pageBorder.Tag = contentStack;

            return pageBorder;
        }

        private FrameworkElement CreateHeader(AssignmentHeading heading)
        {
            var headerGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12)
            };

            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Brand / Subtitle top line
            var topBrand = new TextBlock
            {
                Text = heading.Subtitle,
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(topBrand, 0);
            headerGrid.Children.Add(topBrand);

            // Name, Date, Period fields
            var studentInfoPanel = new DockPanel
            {
                Margin = new Thickness(0, 4, 0, 8)
            };

            var nameBlock = new TextBlock
            {
                Text = LocalizationService.Get("ws_name"),
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 12
            };
            DockPanel.SetDock(nameBlock, Dock.Left);
            studentInfoPanel.Children.Add(nameBlock);

            var datePeriodBlock = new TextBlock
            {
                Text = LocalizationService.Get("ws_date_period"),
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(datePeriodBlock, Dock.Right);
            studentInfoPanel.Children.Add(datePeriodBlock);

            Grid.SetRow(studentInfoPanel, 1);
            headerGrid.Children.Add(studentInfoPanel);

            // Assignment Title
            string titleText = (string.IsNullOrWhiteSpace(heading.Title) || heading.Title.Equals("Assignment", StringComparison.OrdinalIgnoreCase))
                ? LocalizationService.Get("ws_assignment")
                : heading.Title;
            var titleBlock = new TextBlock
            {
                Text = titleText,
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 8)
            };
            Grid.SetRow(titleBlock, 2);
            headerGrid.Children.Add(titleBlock);

            return headerGrid;
        }

        private FrameworkElement CreateQuestionElement(Question q, bool showAnswers)
        {
            var container = new Border
            {
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(0)
            };

            var sp = new StackPanel();

            // Question number & formula
            var qHeader = new DockPanel();

            var numBlock = new TextBlock
            {
                Text = $"{q.Number})  ",
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(numBlock, Dock.Left);
            qHeader.Children.Add(numBlock);

            var formulaElem = RenderMath(q.FormulaText, scale: 16.0);
            qHeader.Children.Add(formulaElem);

            sp.Children.Add(qHeader);

            // Optional function graph or blank grid
            if (q.ProvideBlankGraph || q.ProvideFunctionGraph)
            {
                var graph = new FunctionGraphControl
                {
                    IsBlank = q.ProvideBlankGraph && !q.ProvideFunctionGraph,
                    FunctionExpr = !string.IsNullOrWhiteSpace(q.GraphFormula) ? q.GraphFormula : MathExpressionEvaluator.ExtractFormula(q.FormulaText),
                    XMin = -6.0,
                    XMax = 6.0,
                    YMin = -6.0,
                    YMax = 6.0,
                    Holes = q.GraphHoles,
                    Points = q.GraphPoints,
                    Width = 210,
                    Height = 210,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 8)
                };
                sp.Children.Add(graph);
            }

            // Multiple Choice options
            if (q.Type == QuestionType.MultipleChoice && q.Choices.Count > 0)
            {
                bool isLongChoices = q.Choices.Any(c => c.Text.Length > 22 || c.Text.Contains('\n'));
                var choicesGrid = new Grid { Margin = new Thickness(22, 6, 0, 0) };

                if (isLongChoices)
                {
                    choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    for (int i = 0; i < q.Choices.Count; i++)
                    {
                        var c = q.Choices[i];
                        var choicePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                        var choiceLetter = new TextBlock
                        {
                            Text = $"{c.Letter})  ",
                            FontFamily = new FontFamily("Times New Roman, Cambria"),
                            FontSize = 13,
                            FontWeight = (showAnswers && c.IsCorrect) ? FontWeights.Bold : FontWeights.Normal,
                            Foreground = (showAnswers && c.IsCorrect) ? new SolidColorBrush(Color.FromRgb(190, 0, 0)) : Brushes.Black,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        choicePanel.Children.Add(choiceLetter);

                        var choiceMath = RenderMath(c.Text, scale: 13.0,
                            foreground: (showAnswers && c.IsCorrect) ? new SolidColorBrush(Color.FromRgb(190, 0, 0)) : Brushes.Black,
                            isBold: (showAnswers && c.IsCorrect));
                        choicePanel.Children.Add(choiceMath);

                        choicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        Grid.SetRow(choicePanel, i);
                        Grid.SetColumn(choicePanel, 0);
                        choicesGrid.Children.Add(choicePanel);
                    }
                }
                else
                {
                    choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    for (int i = 0; i < q.Choices.Count; i++)
                    {
                        var c = q.Choices[i];
                        var choicePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                        var choiceLetter = new TextBlock
                        {
                            Text = $"{c.Letter})  ",
                            FontFamily = new FontFamily("Times New Roman, Cambria"),
                            FontSize = 13,
                            FontWeight = (showAnswers && c.IsCorrect) ? FontWeights.Bold : FontWeights.Normal,
                            Foreground = (showAnswers && c.IsCorrect) ? new SolidColorBrush(Color.FromRgb(190, 0, 0)) : Brushes.Black,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        choicePanel.Children.Add(choiceLetter);

                        var choiceMath = RenderMath(c.Text, scale: 13.0,
                            foreground: (showAnswers && c.IsCorrect) ? new SolidColorBrush(Color.FromRgb(190, 0, 0)) : Brushes.Black,
                            isBold: (showAnswers && c.IsCorrect));
                        choicePanel.Children.Add(choiceMath);

                        int row = i / 2;
                        int col = i % 2;

                        if (row >= choicesGrid.RowDefinitions.Count)
                        {
                            choicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        }

                        Grid.SetRow(choicePanel, row);
                        Grid.SetColumn(choicePanel, col);
                        choicesGrid.Children.Add(choicePanel);
                    }
                }

                sp.Children.Add(choicesGrid);
            }

            // Student work space (if free response)
            if (q.Type == QuestionType.FreeResponse)
            {
                sp.Children.Add(new Border { Height = q.ExtraSpace });
            }

            // Answer reveal overlay (shown in red when ShowAnswers is toggled)
            if (showAnswers)
            {
                var ansBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 220, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 40, 40)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(20, 4, 0, 4)
                };

                var ansPanel = new StackPanel();
                var ansHeader = new StackPanel { Orientation = Orientation.Horizontal };
                var ansLabel = new TextBlock
                {
                    Text = $"{LocalizationService.Get("ws_ans_label")} ",
                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 0, 0)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                ansHeader.Children.Add(ansLabel);
                ansHeader.Children.Add(RenderMath(q.AnswerText, scale: 13.0, foreground: new SolidColorBrush(Color.FromRgb(180, 0, 0)), isBold: true));
                ansPanel.Children.Add(ansHeader);

                if (!string.IsNullOrEmpty(q.SolutionSteps))
                {
                    var stepsElem = RenderMixedTextAndMath(q.SolutionSteps, scale: 11.5, foreground: new SolidColorBrush(Color.FromRgb(80, 20, 20)));
                    ansPanel.Children.Add(stepsElem);
                }

                ansBorder.Child = ansPanel;
                sp.Children.Add(ansBorder);
            }

            container.Child = sp;
            return container;
        }

        private Border CreateAnswerKeyPage(Assignment assignment, List<Question> questions, int startPage)
        {
            var page = CreateEmptyPage(assignment, startPage);
            var contentPanel = (StackPanel)page.Tag;

            var keyTitle = new TextBlock
            {
                Text = LocalizationService.Get("ws_answers_to", assignment.Heading.Title),
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20)
            };
            contentPanel.Children.Add(keyTitle);

            var keyGrid = new Grid();
            keyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            keyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int count = questions.Count;
            int half = (count + 1) / 2;

            var col1 = new StackPanel();
            var col2 = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };

            Grid.SetColumn(col1, 0);
            Grid.SetColumn(col2, 1);
            keyGrid.Children.Add(col1);
            keyGrid.Children.Add(col2);

            for (int i = 0; i < count; i++)
            {
                var q = questions[i];
                string ansDisplay = (q.Type == QuestionType.MultipleChoice)
                    ? $"{q.Choices.FirstOrDefault(c => c.IsCorrect)?.Letter})  {q.AnswerText}"
                    : q.AnswerText;

                var ansRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
                var numBlock = new TextBlock
                {
                    Text = $"{q.Number})  ",
                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ansRow.Children.Add(numBlock);
                ansRow.Children.Add(RenderMath(ansDisplay, scale: 13.0));

                if (i < half) col1.Children.Add(ansRow);
                else col2.Children.Add(ansRow);
            }

            contentPanel.Children.Add(keyGrid);
            return page;
        }

        private FrameworkElement CreateFooter(string subtitle, int pageNumber)
        {
            var footer = new DockPanel
            {
                Margin = new Thickness(0, 24, 0, 0),
                LastChildFill = true
            };

            var copyBlock = new TextBlock
            {
                Text = LocalizationService.Get("ws_footer", subtitle),
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            DockPanel.SetDock(copyBlock, Dock.Left);
            footer.Children.Add(copyBlock);

            var pageNumBlock = new TextBlock
            {
                Text = $"-{pageNumber}-",
                FontFamily = new FontFamily("Times New Roman, Cambria"),
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(pageNumBlock, Dock.Right);
            footer.Children.Add(pageNumBlock);

            return footer;
        }
    }
}
