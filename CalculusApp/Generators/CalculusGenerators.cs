using System;
using System.Collections.Generic;
using System.Linq;
using CalculusApp.Models;
using CalculusApp.Services;

namespace CalculusApp.Generators
{
    public interface ITopicGenerator
    {
        string TopicId { get; }
        string CategoryId { get; }
        string Title { get; }
        string Category { get; }
        string DefaultDirections { get; }
        List<Question> GenerateQuestions(int count, QuestionType type, Random rng);
    }

    public class TopicDefinition : ITopicGenerator
    {
        public string TopicId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;

        public string TitleEn { get; set; } = string.Empty;
        public string TitleEs { get; set; } = string.Empty;
        public string TitlePt { get; set; } = string.Empty;

        public string CategoryEn { get; set; } = string.Empty;
        public string CategoryEs { get; set; } = string.Empty;
        public string CategoryPt { get; set; } = string.Empty;

        public string DirectionsEn { get; set; } = string.Empty;
        public string DirectionsEs { get; set; } = string.Empty;
        public string DirectionsPt { get; set; } = string.Empty;

        public Func<int, QuestionType, TopicGeneratorParameters?, Random, List<Question>>? GeneratorFuncWithParams { get; set; }
        public Func<int, QuestionType, Random, List<Question>> GeneratorFunc { get; set; } = null!;

        public string Title => LocalizationService.CurrentLanguage switch
        {
            AppLanguage.Spanish => TitleEs,
            AppLanguage.Portuguese => TitlePt,
            _ => TitleEn
        };

        public string Category => LocalizationService.CurrentLanguage switch
        {
            AppLanguage.Spanish => CategoryEs,
            AppLanguage.Portuguese => CategoryPt,
            _ => CategoryEn
        };

        public string DefaultDirections => LocalizationService.CurrentLanguage switch
        {
            AppLanguage.Spanish => DirectionsEs,
            AppLanguage.Portuguese => DirectionsPt,
            _ => DirectionsEn
        };

        public List<Question> GenerateQuestions(int count, QuestionType type, Random rng)
        {
            return GenerateQuestions(count, type, null, rng);
        }

        public static string GetQuestionArchetypeKey(Question q)
        {
            if (!string.IsNullOrWhiteSpace(q.TemplateId))
                return q.TemplateId;

            string raw = (string.IsNullOrWhiteSpace(q.ProblemStatement) ? q.FormulaText : q.ProblemStatement).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return Guid.NewGuid().ToString();

            // Normalize whitespace
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"\s+", " ");

            // Replace numbers with '#', preserving powers after ^ (e.g., ^2, ^3)
            string pattern = System.Text.RegularExpressions.Regex.Replace(raw, @"(?<!\^)\b\d+(\.\d+)?\b", "#");
            return pattern.Trim();
        }

        public List<Question> GenerateQuestions(int count, QuestionType type, TopicGeneratorParameters? parameters, Random rng)
        {
            var list = GeneratorFuncWithParams != null
                ? GeneratorFuncWithParams(count, type, parameters, rng)
                : GeneratorFunc(count, type, rng);

            // Ensure every generated question in the batch has a distinct structural archetype
            // and distinct mathematical formula
            if (list.Count > 1)
            {
                var seenFormulas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenArchetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var distinctList = new List<Question>();

                for (int i = 0; i < list.Count; i++)
                {
                    var q = list[i];
                    string formKey = (string.IsNullOrWhiteSpace(q.ProblemStatement) ? q.FormulaText : q.ProblemStatement).Trim();
                    string archKey = GetQuestionArchetypeKey(q);

                    if (!seenFormulas.Contains(formKey) && !seenArchetypes.Contains(archKey))
                    {
                        seenFormulas.Add(formKey);
                        seenArchetypes.Add(archKey);
                        distinctList.Add(q);
                    }
                    else
                    {
                        // Duplicate archetype or duplicate formula! Search for an unseen archetype.
                        bool replaced = false;
                        for (int attempt = 1; attempt <= 60; attempt++)
                        {
                            var altRng = new Random(rng.Next() ^ (attempt * 7919) ^ (i * 31));
                            int reqCount = i + attempt + 1;
                            var singleList = GeneratorFuncWithParams != null
                                ? GeneratorFuncWithParams(reqCount, type, parameters, altRng)
                                : GeneratorFunc(reqCount, type, altRng);

                            if (singleList.Count > 0)
                            {
                                var cand = singleList[singleList.Count - 1];
                                string candForm = (string.IsNullOrWhiteSpace(cand.ProblemStatement) ? cand.FormulaText : cand.ProblemStatement).Trim();
                                string candArch = GetQuestionArchetypeKey(cand);

                                if (!seenFormulas.Contains(candForm) && !seenArchetypes.Contains(candArch))
                                {
                                    seenFormulas.Add(candForm);
                                    seenArchetypes.Add(candArch);
                                    distinctList.Add(cand);
                                    replaced = true;
                                    break;
                                }
                            }
                        }

                        // If all distinct archetypes for this topic are already used in this batch
                        // (e.g. user requested more questions than available distinct templates),
                        // allow reusing an archetype but ensure the formula/numbers are strictly distinct!
                        if (!replaced)
                        {
                            for (int attempt = 1; attempt <= 40; attempt++)
                            {
                                var altRng = new Random(rng.Next() ^ (attempt * 6271) ^ (i * 17));
                                int reqCount = i + attempt + 1;
                                var singleList = GeneratorFuncWithParams != null
                                    ? GeneratorFuncWithParams(reqCount, type, parameters, altRng)
                                    : GeneratorFunc(reqCount, type, altRng);

                                if (singleList.Count > 0)
                                {
                                    var cand = singleList[singleList.Count - 1];
                                    string candForm = (string.IsNullOrWhiteSpace(cand.ProblemStatement) ? cand.FormulaText : cand.ProblemStatement).Trim();
                                    if (!seenFormulas.Contains(candForm))
                                    {
                                        seenFormulas.Add(candForm);
                                        distinctList.Add(cand);
                                        replaced = true;
                                        break;
                                    }
                                }
                            }

                            if (!replaced)
                            {
                                distinctList.Add(q);
                            }
                        }
                    }
                }
                list = distinctList;
            }

            if (parameters != null)
            {
                foreach (var q in list)
                {
                    q.ProvideBlankGraph = parameters.ProvideBlankGraph;
                    q.ProvideFunctionGraph = parameters.ProvideFunctionGraph;
                    q.GraphXMin = -6;
                    q.GraphXMax = 6;
                    q.GraphYMin = -6;
                    q.GraphYMax = 6;

                    if (string.IsNullOrWhiteSpace(q.GraphFormula))
                    {
                        q.GraphFormula = MathExpressionEvaluator.ExtractFormula(q.FormulaText);
                    }

                    if (parameters.Notation == "y=")
                    {
                        if (q.FormulaText.StartsWith("f(x) ="))
                            q.FormulaText = "y =" + q.FormulaText.Substring(6);
                    }
                    else if (parameters.Notation == "f(x)=")
                    {
                        if (q.FormulaText.StartsWith("y ="))
                            q.FormulaText = "f(x) =" + q.FormulaText.Substring(3);
                    }
                    else if (parameters.Notation == "Either")
                    {
                        if (rng.Next(2) == 0 && q.FormulaText.StartsWith("f(x) ="))
                            q.FormulaText = "y =" + q.FormulaText.Substring(6);
                    }
                }
            }

            foreach (var q in list)
            {
                if (string.IsNullOrWhiteSpace(q.GraphFormula))
                {
                    q.GraphFormula = MathExpressionEvaluator.ExtractFormula(q.FormulaText);
                }
                q.GraphXMin = -6;
                q.GraphXMax = 6;
                q.GraphYMin = -6;
                q.GraphYMax = 6;

                q.FormulaText = LatexFormatter.ToLatex(q.FormulaText);
                q.AnswerText = LatexFormatter.ToLatex(q.AnswerText);
                q.SolutionSteps = LatexFormatter.FormatSolutionSteps(q.SolutionSteps);
                foreach (var c in q.Choices)
                {
                    c.Text = LatexFormatter.ToLatex(c.Text);
                }
            }
            return list;
        }
    }

    public static class DistractorHelper
    {
        public static List<QuestionChoice> BuildChoices(string correct, string d1, string d2, string d3, Random rng)
        {
            var raw = new List<(string text, bool isCorrect)>
            {
                (correct, true),
                (d1, false),
                (d2, false),
                (d3, false)
            };

            for (int i = 1; i < raw.Count; i++)
            {
                if (raw[i].text == raw[0].text)
                {
                    raw[i] = ($"{raw[i].text} + 1", false);
                }
            }

            var shuffled = raw.OrderBy(_ => rng.Next()).ToList();
            var choices = new List<QuestionChoice>();
            char[] letters = { 'A', 'B', 'C', 'D' };

            for (int i = 0; i < shuffled.Count && i < 4; i++)
            {
                choices.Add(new QuestionChoice
                {
                    Letter = letters[i],
                    Text = shuffled[i].text,
                    IsCorrect = shuffled[i].isCorrect
                });
            }

            return choices;
        }
    }

    public static class TopicRegistry
    {
        private static readonly List<TopicDefinition> _topics = new();

        public static IReadOnlyList<TopicDefinition> AllTopics => _topics;

        public static TopicDefinition? GetById(string id) => _topics.FirstOrDefault(t => t.TopicId == id);

        static TopicRegistry()
        {
            InitAllTopics();
        }

        private static void InitAllTopics()
        {
            #region 1. Limits
            AddTopic("limits_direct", "limits",
                "By direct evaluation", "Por evaluación directa", "Por avaliação direta",
                "Limits", "Límites", "Limites",
                "Evaluate each limit.", "Evalúe cada límite.", "Avalie cada limite.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitProblem(parameters, startOffset + i, type, rng, "limits_direct"));
                    }
                    return list;
                });

            AddTopic("limits_jumps", "limits",
                "At jump discontinuities and kinks", "En discontinuidades de salto y picos", "Em descontinuidades de salto e bicos",
                "Limits", "Límites", "Limites",
                "Evaluate each one-sided or two-sided limit.", "Evalúe cada límite unilateral o bilateral.", "Avalie cada limite unilateral ou bilateral.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitProblem(parameters, startOffset + i, type, rng, "limits_jumps"));
                    }
                    return list;
                });

            AddTopic("limits_holes", "limits",
                "At removable discontinuities", "En discontinuidades evitables", "Em descontinuidades removíveis",
                "Limits", "Límites", "Limites",
                "Evaluate each limit by factoring or algebraic manipulation.", "Evalúe cada límite factorizando o manipulando algebraicamente.", "Avalie cada limite fatorando ou manipulando algebricamente.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitProblem(parameters, startOffset + i, type, rng, "limits_holes"));
                    }
                    return list;
                });

            AddTopic("limits_essential", "limits",
                "At essential discontinuities", "En discontinuidades esenciales", "Em descontinuidades essenciais",
                "Limits", "Límites", "Limites",
                "Determine the behavior of each limit at the vertical asymptote.", "Determine el comportamiento de cada límite en la asíntota vertical.", "Determine o comportamento de cada limite na assíntota vertical.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitProblem(parameters, startOffset + i, type, rng, "limits_essential"));
                    }
                    return list;
                });

            AddTopic("limits_infinity", "limits",
                "At infinity", "Al infinito", "No infinito",
                "Limits", "Límites", "Limites",
                "Evaluate each limit as x approaches infinity.", "Evalúe cada límite cuando x tiende a infinito.", "Avalie cada limite quando x tende ao infinito.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitProblem(parameters, startOffset + i, type, rng, "limits_infinity"));
                    }
                    return list;
                });
            #endregion

            #region 2. Continuity
            AddTopic("continuity_classify", "continuity",
                "Determining and classifying", "Determinación y clasificación", "Determinação e classificação",
                "Continuity", "Continuidad", "Continuidade",
                "Determine and classify each point of discontinuity.", "Determine y clasifique cada punto de discontinuidad.", "Determine e classifique cada ponto de descontinuidade.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateContinuityClassifyProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion

            #region 3. Differentiation
            AddTopic("deriv_avg_rates", "differentiation",
                "Average rates of change", "Tasa de cambio promedio", "Taxa de variação média",
                "Differentiation", "Derivación", "Derivação",
                "Find the average rate of change of each function over the given interval.", "Halle la tasa de cambio promedio de cada función en el intervalo dado.", "Encontre a taxa de variação média de cada função no intervalo dado.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateAverageRateOfChangeProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_definition", "differentiation",
                "Definition of the derivative", "Definición de la derivada", "Definição da derivada",
                "Differentiation", "Derivación", "Derivação",
                "Use the limit definition of the derivative to find f'(x).", "Use la definición de límite de la derivada para hallar f'(x).", "Use a definição de limite da derivada para encontrar f'(x).",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDerivativeDefinitionProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_instant_rates", "differentiation",
                "Instantaneous rates of change", "Tasa de cambio instantánea", "Taxa de variação instantânea",
                "Differentiation", "Derivación", "Derivação",
                "Find the instantaneous rate of change at the specified point.", "Halle la tasa de cambio instantánea en el punto especificado.", "Encontre a taxa de variação instantânea no ponto especificado.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateInstantRateProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_power", "differentiation",
                "Power Rule", "Regla de la potencia", "Regra da potência",
                "Differentiation", "Derivación", "Derivação",
                "For each problem, find the derivative of the function with respect to x.", "Para cada problema, halle la derivada de la función con respecto a x.", "Para cada problema, encontre a derivada da função em relação a x.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_power"));
                    }
                    return list;
                });

            AddTopic("deriv_higher_order", "differentiation",
                "Higher order derivatives", "Derivadas de orden superior", "Derivadas de ordem superior",
                "Differentiation", "Derivación", "Derivação",
                "Find the indicated higher order derivative.", "Halle la derivada de orden superior indicada.", "Encontre a derivada de ordem superior indicada.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_higher_order"));
                    }
                    return list;
                });

            AddTopic("deriv_product", "differentiation",
                "Product Rule", "Regla del producto", "Regra do produto",
                "Differentiation", "Derivación", "Derivação",
                "Differentiate each function using the product rule.", "Derive cada función usando la regla del producto.", "Derive cada função usando a regra do produto.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_product"));
                    }
                    return list;
                });

            AddTopic("deriv_quotient", "differentiation",
                "Quotient Rule", "Regla del cociente", "Regra do quociente",
                "Differentiation", "Derivación", "Derivação",
                "Differentiate each function using the quotient rule.", "Derive cada función usando la regla del cociente.", "Derive cada função usando a regra do quociente.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_quotient"));
                    }
                    return list;
                });

            AddTopic("deriv_chain", "differentiation",
                "Chain Rule", "Regla de la cadena", "Regra da cadeia",
                "Differentiation", "Derivación", "Derivação",
                "Find the derivative of each function using the chain rule.", "Halle la derivada de cada función usando la regla de la cadena.", "Encontre a derivada de cada função usando a regra da cadeia.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_chain"));
                    }
                    return list;
                });

            AddTopic("deriv_tables", "differentiation",
                "Rules, using tables", "Reglas usando tablas", "Regras usando tabelas",
                "Differentiation", "Derivación", "Derivação",
                "Use the table of values to find the requested derivative value.", "Use la tabla de valores para hallar el valor de la derivada solicitada.", "Use a tabela de valores para encontrar o valor da derivada solicitada.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDerivTablesProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_trig", "differentiation",
                "Trigonometric", "Trigonométricas", "Trigonométricas",
                "Differentiation", "Derivación", "Derivação",
                "Differentiate each trigonometric function.", "Derive cada función trigonométrica.", "Derive cada función trigonométrica.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentiationProblem(parameters, startOffset + i, type, rng, "deriv_trig"));
                    }
                    return list;
                });

            AddTopic("deriv_inv_trig", "differentiation",
                "Inverse trigonometric", "Trigonométricas inversas", "Trigonométricas inversas",
                "Differentiation", "Derivación", "Derivação",
                "Find the derivative of each inverse trigonometric function.", "Halle la derivada de cada función trigonométrica inversa.", "Encontre a derivada de cada função trigonométrica inversa.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateInvTrigDerivProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_exp_log_natural", "differentiation",
                "Natural logarithms and exponentials", "Logaritmos naturales y exponenciales", "Logaritmos naturais e exponenciais",
                "Differentiation", "Derivación", "Derivação",
                "Differentiate each natural exponential and logarithmic function.", "Derive cada función exponencial natural y logarítmica.", "Derive cada función exponencial natural e logarítmica.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateExpLogDerivProblem(parameters, startOffset + i, type, rng, isNatural: true));
                    }
                    return list;
                });

            AddTopic("deriv_exp_log_other", "differentiation",
                "Other base logarithms and exponentials", "Logaritmos y exponenciales de otra base", "Logaritmos e exponenciais de outra base",
                "Differentiation", "Derivación", "Derivação",
                "Find the derivative of each general base function.", "Halle la derivada de cada función con base general.", "Encontre a derivada de cada função com base geral.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateExpLogDerivProblem(parameters, startOffset + i, type, rng, isNatural: false));
                    }
                    return list;
                });

            AddTopic("deriv_logarithmic", "differentiation",
                "Logarithmic", "Logarítmicas", "Logarítmicas",
                "Differentiation", "Derivación", "Derivação",
                "Use logarithmic differentiation to find dy/dx.", "Use derivación logarítmica para hallar dy/dx.", "Use derivação logarítmica para encontrar dy/dx.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLogarithmicDerivProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_implicit", "differentiation",
                "Implicit", "Derivación implícita", "Derivação implícita",
                "Differentiation", "Derivación", "Derivação",
                "Find dy/dx implicitly for each relation.", "Halle dy/dx implícitamente para cada relación.", "Encontre dy/dx implicitamente para cada relação.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateImplicitProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("deriv_inverse_func", "differentiation",
                "Inverse functions", "Funciones inversas", "Funções inversas",
                "Differentiation", "Derivación", "Derivação",
                "Find the derivative of the inverse function at the given value.", "Halle la derivada de la función inversa en el valor dado.", "Encontre a derivada da função inversa no valor fornecido.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateInverseFuncDerivProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion

            #region 4. Applications of Differentiation
            AddTopic("app_tangent", "app_diff",
                "Slope, tangent, and normal lines", "Pendiente, rectas tangentes y normales", "Inclinação, retas tangentes e normais",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find the equation of the tangent line to the function at the given point.", "Halle la ecuación de la recta tangente a la función en el punto dado.", "Encontre a equação da reta tangente à função no ponto fornecido.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateApplicationProblem(parameters, startOffset + i, type, rng, "app_tangent"));
                    }
                    return list;
                });

            AddTopic("app_rolle", "app_diff",
                "Rolle's Theorem", "Teorema de Rolle", "Teorema de Rolle",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Determine if Rolle's Theorem applies and find all values of c.", "Determine si el Teorema de Rolle aplica y halle todos los valores de c.", "Determine se o Teorema de Rolle se aplica e encontre todos os valores de c.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateRolleProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_mvt", "app_diff",
                "Mean Value Theorem", "Teorema del Valor Medio", "Teorema do Valor Médio",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find all numbers c guaranteed by the Mean Value Theorem.", "Halle todos los valores c garantizados por el Teorema del Valor Medio.", "Encontre todos os valores c garantidos pelo Teorema do Valor Médio.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateMvtProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_increase_decrease", "app_diff",
                "Intervals of increase and decrease", "Intervalos de crecimiento y decrecimiento", "Intervalos de crescimento e decrescimento",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find the intervals on which each function is increasing and decreasing.", "Halle los intervalos en los cuales la función es creciente y decreciente.", "Encontre os intervalos nos quais a função é crescente e decrescente.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIncreaseDecreaseProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_concavity", "app_diff",
                "Intervals of concavity", "Intervalos de concavidad", "Intervalos de concavidade",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find intervals of concavity and inflection points.", "Halle los intervalos de concavidad y puntos de inflexión.", "Encontre os intervalos de concavidade e pontos de inflexão.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateConcavityProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_rel_extrema", "app_diff",
                "Relative extrema", "Extremos relativos", "Extremos relativos",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find all relative extrema for each function.", "Halle todos los extremos relativos para cada función.", "Encontre todos os extremos relativos para cada función.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateApplicationProblem(parameters, startOffset + i, type, rng, "app_rel_extrema"));
                    }
                    return list;
                });

            AddTopic("app_abs_extrema", "app_diff",
                "Absolute extrema", "Extremos absolutos", "Extremos absolutos",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "For each problem, find all points of absolute minima and maxima on the given interval.",
                "Para cada problema, halle todos los puntos de mínimos y máximos absolutos en el intervalo dado.",
                "Para cada problema, encontre todos os pontos de mínimos e máximos absolutos no intervalo dado.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateApplicationProblem(parameters, startOffset + i, type, rng, "app_abs_extrema"));
                    }
                    return list;
                });

            AddTopic("app_optimization", "app_diff",
                "Optimization", "Optimización", "Otimização",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Solve each optimization problem.", "Resuelva cada problema de optimización.", "Resolva cada problema de otimização.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateOptimizationProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_curve_sketching", "app_diff",
                "Curve sketching", "Trazado de curvas", "Esboço de curvas",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Determine key features to sketch the graph of each function.", "Determine características clave para trazar la gráfica.", "Determine características principais para esboçar o gráfico.",
                (count, type, parameters, rng) =>
                {
                    var quadratics = new[]
                    {
                        (
                            formula: "f(x) = x^2 - 4x + 3",
                            graph: "x^2 - 4*x + 3",
                            ansEn: "No asymptotes exist; Vertex: (2, -1)",
                            ansEs: "No existen asíntotas; Vértice: (2, -1)",
                            ansPt: "Não existem assíntotas; Vértice: (2, -1)",
                            stepsEn: "Polynomial of degree 2: continuous everywhere, no vertical or horizontal asymptotes. Vertex at x = 2, f(2) = -1.",
                            stepsEs: "Polinomio de grado 2: continua en todas partes, sin asíntotas. Vértice en x = 2, f(2) = -1.",
                            stepsPt: "Polinômio de grau 2: contínuo em toda parte, sem assíntotas. Vértice em x = 2, f(2) = -1.",
                            d1En: "Vertical asymptote: x = 2; Horizontal asymptote: y = -1",
                            d1Es: "Asíntota vertical: x = 2; Asíntota horizontal: y = -1",
                            d1Pt: "Assíntota vertical: x = 2; Assíntota horizontal: y = -1",
                            d2En: "Vertical asymptote: x = 3; y = 0",
                            d2Es: "Asíntota vertical: x = 3; y = 0",
                            d2Pt: "Assíntota vertical: x = 3; y = 0",
                            d3En: "Horizontal asymptote: y = 3",
                            d3Es: "Asíntota horizontal: y = 3",
                            d3Pt: "Assíntota horizontal: y = 3"
                        ),
                        (
                            formula: "f(x) = -x^2 + 2x + 8",
                            graph: "-x^2 + 2*x + 8",
                            ansEn: "No asymptotes exist; Vertex: (1, 9)",
                            ansEs: "No existen asíntotas; Vértice: (1, 9)",
                            ansPt: "Não existem assíntotas; Vértice: (1, 9)",
                            stepsEn: "Parabola opens downward: no asymptotes. Vertex at (1, 9), intercepts at x = -2, 4.",
                            stepsEs: "Parábola cóncava hacia abajo: sin asíntotas. Vértice en (1, 9), interceptos en x = -2, 4.",
                            stepsPt: "Parábola voltada para baixo: sem assíntotas. Vértice em (1, 9), raízes em x = -2, 4.",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = 9",
                            d2Es: "Asíntota horizontal: y = 9",
                            d2Pt: "Assíntota horizontal: y = 9",
                            d3En: "Vertical asymptotes: x = -2, 4",
                            d3Es: "Asíntotas verticales: x = -2, 4",
                            d3Pt: "Assíntotas verticais: x = -2, 4"
                        ),
                        (
                            formula: "f(x) = x^2 - 6x + 5",
                            graph: "x^2 - 6*x + 5",
                            ansEn: "No asymptotes exist; Vertex: (3, -4)",
                            ansEs: "No existen asíntotas; Vértice: (3, -4)",
                            ansPt: "Não existem assíntotas; Vértice: (3, -4)",
                            stepsEn: "Parabola opens upward: no asymptotes. Vertex at x = 3, f(3) = -4, intercepts at x = 1, 5.",
                            stepsEs: "Parábola cóncava hacia arriba: sin asíntotas. Vértice en (3, -4), interceptos en x = 1, 5.",
                            stepsPt: "Parábola voltada para cima: sem assíntotas. Vértice em (3, -4), raízes em x = 1, 5.",
                            d1En: "Vertical asymptote: x = 3",
                            d1Es: "Asíntota vertical: x = 3",
                            d1Pt: "Assíntota vertical: x = 3",
                            d2En: "Horizontal asymptote: y = -4",
                            d2Es: "Asíntota horizontal: y = -4",
                            d2Pt: "Assíntota horizontal: y = -4",
                            d3En: "Vertical asymptotes: x = 1, 5",
                            d3Es: "Asíntotas verticales: x = 1, 5",
                            d3Pt: "Assíntotas verticais: x = 1, 5"
                        ),
                        (
                            formula: "f(x) = -x^2 - 4x",
                            graph: "-x^2 - 4*x",
                            ansEn: "No asymptotes exist; Vertex: (-2, 4)",
                            ansEs: "No existen asíntotas; Vértice: (-2, 4)",
                            ansPt: "Não existem assíntotas; Vértice: (-2, 4)",
                            stepsEn: "Parabola opens downward: no asymptotes. Vertex at (-2, 4), intercepts at x = -4, 0.",
                            stepsEs: "Parábola cóncava hacia abajo: sin asíntotas. Vértice en (-2, 4), interceptos en x = -4, 0.",
                            stepsPt: "Parábola voltada para baixo: sem assíntotas. Vértice em (-2, 4), raízes em x = -4, 0.",
                            d1En: "Vertical asymptote: x = -2",
                            d1Es: "Asíntota vertical: x = -2",
                            d1Pt: "Assíntota vertical: x = -2",
                            d2En: "Horizontal asymptote: y = 4",
                            d2Es: "Asíntota horizontal: y = 4",
                            d2Pt: "Assíntota horizontal: y = 4",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = 2x^2 - 8x + 6",
                            graph: "2*x^2 - 8*x + 6",
                            ansEn: "No asymptotes exist; Vertex: (2, -2)",
                            ansEs: "No existen asíntotas; Vértice: (2, -2)",
                            ansPt: "Não existem assíntotas; Vértice: (2, -2)",
                            stepsEn: "Quadratic polynomial: no asymptotes. Vertex at (2, -2), intercepts at x = 1, 3.",
                            stepsEs: "Polinomio cuadrático: sin asíntotas. Vértice en (2, -2), interceptos en x = 1, 3.",
                            stepsPt: "Polinômio quadrático: sem assíntotas. Vértice em (2, -2), raízes em x = 1, 3.",
                            d1En: "Vertical asymptote: x = 2",
                            d1Es: "Asíntota vertical: x = 2",
                            d1Pt: "Assíntota vertical: x = 2",
                            d2En: "Horizontal asymptote: y = -2",
                            d2Es: "Asíntota horizontal: y = -2",
                            d2Pt: "Assíntota horizontal: y = -2",
                            d3En: "Vertical asymptotes: x = 1, 3",
                            d3Es: "Asíntotas verticales: x = 1, 3",
                            d3Pt: "Assíntotas verticais: x = 1, 3"
                        ),
                        (
                            formula: "f(x) = -2x^2 + 4x + 6",
                            graph: "-2*x^2 + 4*x + 6",
                            ansEn: "No asymptotes exist; Vertex: (1, 8)",
                            ansEs: "No existen asíntotas; Vértice: (1, 8)",
                            ansPt: "Não existen assíntotas; Vértice: (1, 8)",
                            stepsEn: "Parabola opens downward: no asymptotes. Vertex at (1, 8), intercepts at x = -1, 3.",
                            stepsEs: "Parábola cóncava hacia abajo: sin asíntotas. Vértice en (1, 8), interceptos en x = -1, 3.",
                            stepsPt: "Parábola voltada para baixo: sem assíntotas. Vértice em (1, 8), raízes em x = -1, 3.",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = 8",
                            d2Es: "Asíntota horizontal: y = 8",
                            d2Pt: "Assíntota horizontal: y = 8",
                            d3En: "Vertical asymptotes: x = -1, 3",
                            d3Es: "Asíntotas verticales: x = -1, 3",
                            d3Pt: "Assíntotas verticais: x = -1, 3"
                        )
                    };

                    var cubics = new[]
                    {
                        (
                            formula: "f(x) = x^3 - 3x",
                            graph: "x^3 - 3*x",
                            ansEn: "No asymptotes exist; Inflection point: (0, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (0, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão: (0, 0)",
                            stepsEn: "Odd polynomial function: lim x->+-inf = +-inf (no asymptotes). f''(x) = 6x = 0 => Inflection point at (0,0).",
                            stepsEs: "Función cúbica: sin asíntotas. f''(x) = 6x = 0 => Punto de inflexión en (0,0).",
                            stepsPt: "Função cúbica: sem assíntotas. f''(x) = 6x = 0 => Ponto de inflexão em (0,0).",
                            d1En: "Vertical asymptote: x = 0; Horizontal asymptote: y = 0",
                            d1Es: "Asíntota vertical: x = 0; Asíntota horizontal: y = 0",
                            d1Pt: "Assíntota vertical: x = 0; Assíntota horizontal: y = 0",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = -1, 1",
                            d3Es: "Asíntotas verticales: x = -1, 1",
                            d3Pt: "Assíntotas verticais: x = -1, 1"
                        ),
                        (
                            formula: "f(x) = -x^3 + 3x^2",
                            graph: "-x^3 + 3*x^2",
                            ansEn: "No asymptotes exist; Inflection point: (1, 2)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (1, 2)",
                            ansPt: "Não existen assíntotas; Ponto de inflexão: (1, 2)",
                            stepsEn: "Cubic polynomial: no asymptotes. Local min at (0,0), local max at (2,4). Inflection at (1,2).",
                            stepsEs: "Polinomio cúbico: sin asíntotas. Mínimo local en (0,0), máximo local en (2,4). Inflexión en (1,2).",
                            stepsPt: "Polinômio cúbico: sem assíntotas. Mínimo em (0,0), máximo em (2,4). Inflexão em (1,2).",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = 2",
                            d2Es: "Asíntota horizontal: y = 2",
                            d2Pt: "Assíntota horizontal: y = 2",
                            d3En: "Vertical asymptotes: x = 0, 3",
                            d3Es: "Asíntotas verticales: x = 0, 3",
                            d3Pt: "Assíntotas verticais: x = 0, 3"
                        ),
                        (
                            formula: "f(x) = x^3 - 3x^2",
                            graph: "x^3 - 3*x^2",
                            ansEn: "No asymptotes exist; Inflection point: (1, -2)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (1, -2)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão: (1, -2)",
                            stepsEn: "Cubic polynomial: no asymptotes. Local max at (0, 0), local min at (2, -4), inflection at (1, -2).",
                            stepsEs: "Polinomio cúbico: sin asíntotas. Máximo local en (0, 0), mínimo local en (2, -4), inflexión en (1, -2).",
                            stepsPt: "Polinômio cúbico: sem assíntotas. Máximo em (0, 0), mínimo em (2, -4), inflexão em (1, -2).",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = -2",
                            d2Es: "Asíntota horizontal: y = -2",
                            d2Pt: "Assíntota horizontal: y = -2",
                            d3En: "Vertical asymptotes: x = 0, 3",
                            d3Es: "Asíntotas verticales: x = 0, 3",
                            d3Pt: "Assíntotas verticais: x = 0, 3"
                        ),
                        (
                            formula: "f(x) = 2x^3 - 6x",
                            graph: "2*x^3 - 6*x",
                            ansEn: "No asymptotes exist; Inflection point: (0, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (0, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão: (0, 0)",
                            stepsEn: "Cubic polynomial: no asymptotes. Local max at (-1, 4), local min at (1, -4), inflection at (0, 0).",
                            stepsEs: "Polinomio cúbico: sin asíntotas. Máximo local en (-1, 4), mínimo local en (1, -4), inflexión en (0, 0).",
                            stepsPt: "Polinômio cúbico: sem assíntotas. Máximo em (-1, 4), mínimo em (1, -4), inflexão em (0, 0).",
                            d1En: "Vertical asymptote: x = 0",
                            d1Es: "Asíntota vertical: x = 0",
                            d1Pt: "Assíntota vertical: x = 0",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = -1, 1",
                            d3Es: "Asíntotas verticales: x = -1, 1",
                            d3Pt: "Assíntotas verticais: x = -1, 1"
                        ),
                        (
                            formula: "f(x) = x^3 - 12x",
                            graph: "x^3 - 12*x",
                            ansEn: "No asymptotes exist; Inflection point: (0, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (0, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão: (0, 0)",
                            stepsEn: "Cubic polynomial: no asymptotes. Local max at (-2, 16), local min at (2, -16), inflection at (0, 0).",
                            stepsEs: "Polinomio cúbico: sin asíntotas. Máximo local en (-2, 16), mínimo local en (2, -16), inflexión en (0, 0).",
                            stepsPt: "Polinômio cúbico: sem assíntotas. Máximo em (-2, 16), mínimo em (2, -16), inflexão em (0, 0).",
                            d1En: "Vertical asymptote: x = 2",
                            d1Es: "Asíntota vertical: x = 2",
                            d1Pt: "Assíntota vertical: x = 2",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = -2, 2",
                            d3Es: "Asíntotas verticales: x = -2, 2",
                            d3Pt: "Assíntotas verticais: x = -2, 2"
                        ),
                        (
                            formula: "f(x) = -x^3 + 12x",
                            graph: "-x^3 + 12*x",
                            ansEn: "No asymptotes exist; Inflection point: (0, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión: (0, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão: (0, 0)",
                            stepsEn: "Cubic polynomial: no asymptotes. Local min at (-2, -16), local max at (2, 16), inflection at (0, 0).",
                            stepsEs: "Polinomio cúbico: sin asíntotas. Mínimo local en (-2, -16), máximo local en (2, 16), inflexión en (0, 0).",
                            stepsPt: "Polinômio cúbico: sem assíntotas. Mínimo em (-2, -16), máximo em (2, 16), inflexão em (0, 0).",
                            d1En: "Vertical asymptote: x = -2",
                            d1Es: "Asíntota vertical: x = -2",
                            d1Pt: "Assíntota vertical: x = -2",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = -2, 2",
                            d3Es: "Asíntotas verticales: x = -2, 2",
                            d3Pt: "Assíntotas verticais: x = -2, 2"
                        )
                    };

                    var quartics = new[]
                    {
                        (
                            formula: "f(x) = x^4 - 4x^2",
                            graph: "x^4 - 4*x^2",
                            ansEn: "No asymptotes exist; Relative minimums: (-1.41, -4), (1.41, -4)",
                            ansEs: "No existen asíntotas; Mínimos relativos: (-1.41, -4), (1.41, -4)",
                            ansPt: "Não existem assíntotas; Mínimos relativos: (-1.41, -4), (1.41, -4)",
                            stepsEn: "Even polynomial of degree 4: no asymptotes. Relative max at (0,0), relative mins at (+-sqrt(2), -4).",
                            stepsEs: "Polinomio de grado 4: sin asíntotas. Máximo relativo en (0,0), mínimos relativos en (+-sqrt(2), -4).",
                            stepsPt: "Polinômio de grau 4: sem assíntotas. Máximo em (0,0), mínimos em (+-sqrt(2), -4).",
                            d1En: "Vertical asymptotes: x = -2, 2",
                            d1Es: "Asíntotas verticales: x = -2, 2",
                            d1Pt: "Assíntotas verticais: x = -2, 2",
                            d2En: "Horizontal asymptote: y = -4",
                            d2Es: "Asíntota horizontal: y = -4",
                            d2Pt: "Assíntota horizontal: y = -4",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = -x^4 + 4x^2",
                            graph: "-x^4 + 4*x^2",
                            ansEn: "No asymptotes exist; Relative maximums: (-1.41, 4), (1.41, 4)",
                            ansEs: "No existen asíntotas; Mínimo relativo: (0, 0); Máximos relativos: (-1.41, 4), (1.41, 4)",
                            ansPt: "Não existem assíntotas; Mínimo relativo: (0, 0); Máximos relativos: (-1.41, 4), (1.41, 4)",
                            stepsEn: "Quartic polynomial: no asymptotes. Relative min at (0, 0), relative maxes at (+-sqrt(2), 4).",
                            stepsEs: "Polinomio de grado 4: sin asíntotas. Mínimo relativo en (0, 0), máximos relativos en (+-sqrt(2), 4).",
                            stepsPt: "Polinômio de grau 4: sem assíntotas. Mínimo em (0, 0), máximos em (+-sqrt(2), 4).",
                            d1En: "Vertical asymptotes: x = -2, 2",
                            d1Es: "Asíntotas verticales: x = -2, 2",
                            d1Pt: "Assíntotas verticais: x = -2, 2",
                            d2En: "Horizontal asymptote: y = 4",
                            d2Es: "Asíntota horizontal: y = 4",
                            d2Pt: "Assíntota horizontal: y = 4",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = x^4 - 2x^2 + 1",
                            graph: "x^4 - 2*x^2 + 1",
                            ansEn: "No asymptotes exist; Relative minimums: (-1, 0), (1, 0); Relative max: (0, 1)",
                            ansEs: "No existen asíntotas; Mínimos relativos: (-1, 0), (1, 0); Máximo relativo: (0, 1)",
                            ansPt: "Não existem assíntotas; Mínimos relativos: (-1, 0), (1, 0); Máximo relativo: (0, 1)",
                            stepsEn: "Quartic (x^2 - 1)^2 >= 0: no asymptotes. Minima at (+-1, 0), local max at (0, 1).",
                            stepsEs: "Polinomio (x^2 - 1)^2: sin asíntotas. Mínimos en (+-1, 0), máximo local en (0, 1).",
                            stepsPt: "Polinômio (x^2 - 1)^2: sem assíntotas. Mínimos em (+-1, 0), máximo em (0, 1).",
                            d1En: "Vertical asymptotes: x = -1, 1",
                            d1Es: "Asíntotas verticales: x = -1, 1",
                            d1Pt: "Assíntotas verticais: x = -1, 1",
                            d2En: "Horizontal asymptote: y = 1",
                            d2Es: "Asíntota horizontal: y = 1",
                            d2Pt: "Assíntota horizontal: y = 1",
                            d3En: "No local extrema",
                            d3Es: "Sin extremos locales",
                            d3Pt: "Sem extremos locais"
                        ),
                        (
                            formula: "f(x) = 3x^4 - 4x^3",
                            graph: "3*x^4 - 4*x^3",
                            ansEn: "No asymptotes exist; Absolute minimum: (1, -1)",
                            ansEs: "No existen asíntotas; Mínimo absoluto: (1, -1)",
                            ansPt: "Não existem assíntotas; Mínimo absoluto: (1, -1)",
                            stepsEn: "f'(x) = 12x^2(x - 1). Stationary inflection at (0, 0), global min at (1, -1).",
                            stepsEs: "f'(x) = 12x^2(x - 1). Punto de inflexión con tangente horizontal en (0, 0), mínimo absoluto en (1, -1).",
                            stepsPt: "f'(x) = 12x^2(x - 1). Inflexão estacionária em (0, 0), mínimo em (1, -1).",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = -1",
                            d2Es: "Asíntota horizontal: y = -1",
                            d2Pt: "Assíntota horizontal: y = -1",
                            d3En: "Relative maximum: (0, 0)",
                            d3Es: "Máximo relativo: (0, 0)",
                            d3Pt: "Máximo relativo: (0, 0)"
                        ),
                        (
                            formula: "f(x) = x^4 - 8x^2",
                            graph: "x^4 - 8*x^2",
                            ansEn: "No asymptotes exist; Relative minimums: (-2, -16), (2, -16)",
                            ansEs: "No existen asíntotas; Mínimos relativos: (-2, -16), (2, -16)",
                            ansPt: "Não existem assíntotas; Mínimos relativos: (-2, -16), (2, -16)",
                            stepsEn: "Quartic function: no asymptotes. Local max at (0, 0), local mins at (+-2, -16).",
                            stepsEs: "Polinomio de grado 4: sin asíntotas. Máximo local en (0, 0), mínimos locales en (+-2, -16).",
                            stepsPt: "Polinômio de grau 4: sem assíntotas. Máximo em (0, 0), mínimos em (+-2, -16).",
                            d1En: "Vertical asymptotes: x = -2, 2",
                            d1Es: "Asíntotas verticales: x = -2, 2",
                            d1Pt: "Assíntotas verticais: x = -2, 2",
                            d2En: "Horizontal asymptote: y = -16",
                            d2Es: "Asíntota horizontal: y = -16",
                            d2Pt: "Assíntota horizontal: y = -16",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = -x^4 + 2x^2 + 3",
                            graph: "-x^4 + 2*x^2 + 3",
                            ansEn: "No asymptotes exist; Relative maximums: (-1, 4), (1, 4)",
                            ansEs: "No existen asíntotas; Máximos relativos: (-1, 4), (1, 4)",
                            ansPt: "Não existem assíntotas; Máximos relativos: (-1, 4), (1, 4)",
                            stepsEn: "Quartic polynomial: no asymptotes. Local min at (0, 3), local maxes at (+-1, 4).",
                            stepsEs: "Polinomio de grado 4: sin asíntotas. Mínimo local en (0, 3), máximos locales en (+-1, 4).",
                            stepsPt: "Polinômio de grau 4: sem assíntotas. Mínimo em (0, 3), máximos em (+-1, 4).",
                            d1En: "Vertical asymptotes: x = -1, 1",
                            d1Es: "Asíntotas verticales: x = -1, 1",
                            d1Pt: "Assíntotas verticais: x = -1, 1",
                            d2En: "Horizontal asymptote: y = 4",
                            d2Es: "Asíntota horizontal: y = 4",
                            d2Pt: "Assíntota horizontal: y = 4",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        )
                    };

                    var rationals = new[]
                    {
                        (
                            formula: "f(x) = x / (x^2 - 4)",
                            graph: "x / (x^2 - 4)",
                            ansEn: "Vertical asymptotes: x = -2, 2; Horizontal asymptote: y = 0",
                            ansEs: "Asíntotas verticales: x = -2, 2; Asíntota horizontal: y = 0",
                            ansPt: "Assíntotas verticais: x = -2, 2; Assíntota horizontal: y = 0",
                            stepsEn: "Denom = 0 => x^2 - 4 = 0 => x = +-2 (vertical asymptotes). Degree num < denom => y=0 horizontal asymptote. Intercept at (0,0).",
                            stepsEs: "Denominador = 0 => x^2 - 4 = 0 => x = +-2 (asíntotas verticales). Grado num < denom => asíntota horizontal y = 0.",
                            stepsPt: "Denominador = 0 => x = +-2 (assíntotas verticais). Grau num < denom => assíntota horizontal y = 0.",
                            d1En: "Vertical asymptote: x = 4; y = 1",
                            d1Es: "Asíntota vertical: x = 4; y = 1",
                            d1Pt: "Assíntota vertical: x = 4; y = 1",
                            d2En: "No asymptotes exist",
                            d2Es: "No existen asíntotas",
                            d2Pt: "Não existem assíntotas",
                            d3En: "Vertical asymptote: x = 2 only; y = 0",
                            d3Es: "Asíntota vertical: x = 2 solamente; y = 0",
                            d3Pt: "Assíntota vertical: apenas x = 2; y = 0"
                        ),
                        (
                            formula: "f(x) = (x^2 - 1) / (x^2 - 4)",
                            graph: "(x^2 - 1) / (x^2 - 4)",
                            ansEn: "Vertical asymptotes: x = -2, 2; Horizontal asymptote: y = 1",
                            ansEs: "Asíntotas verticales: x = -2, 2; Asíntota horizontal: y = 1",
                            ansPt: "Assíntotas verticais: x = -2, 2; Assíntota horizontal: y = 1",
                            stepsEn: "Denom = 0 => x = +-2 (vertical asymptotes). Ratio of leading coefficients 1/1 => y=1 horizontal asymptote.",
                            stepsEs: "Denom = 0 => x = +-2 (asíntotas verticales). Razón de coeficientes líderes 1/1 => asíntota horizontal y = 1.",
                            stepsPt: "Denom = 0 => x = +-2 (assíntotas verticais). Razão dos coeficientes líderes 1/1 => assíntota horizontal y = 1.",
                            d1En: "Vertical asymptote: x = 2; Horizontal asymptote: y = 0",
                            d1Es: "Asíntota vertical: x = 2; Asíntota horizontal: y = 0",
                            d1Pt: "Assíntota vertical: x = 2; Assíntota horizontal: y = 0",
                            d2En: "Vertical asymptotes: x = -1, 1; Horizontal asymptote: y = 1",
                            d2Es: "Asíntotas verticales: x = -1, 1; Asíntota horizontal: y = 1",
                            d2Pt: "Assíntotas verticais: x = -1, 1; Assíntota horizontal: y = 1",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existem assíntotas"
                        ),
                        (
                            formula: "f(x) = (2x + 1) / (x - 3)",
                            graph: "(2*x + 1) / (x - 3)",
                            ansEn: "Vertical asymptote: x = 3; Horizontal asymptote: y = 2",
                            ansEs: "Asíntota vertical: x = 3; Asíntota horizontal: y = 2",
                            ansPt: "Assíntota vertical: x = 3; Assíntota horizontal: y = 2",
                            stepsEn: "Denom x - 3 = 0 => x = 3 (vertical asymptote). Degree num = denom => y = 2/1 = 2 (horizontal asymptote).",
                            stepsEs: "Denominador x - 3 = 0 => asíntota vertical x = 3. Grados iguales => asíntota horizontal y = 2.",
                            stepsPt: "Denominador x - 3 = 0 => assíntota vertical x = 3. Graus iguais => assíntota horizontal y = 2.",
                            d1En: "Vertical asymptote: x = -3; Horizontal asymptote: y = 2",
                            d1Es: "Asíntota vertical: x = -3; Asíntota horizontal: y = 2",
                            d1Pt: "Assíntota vertical: x = -3; Assíntota horizontal: y = 2",
                            d2En: "Vertical asymptote: x = 3; Horizontal asymptote: y = 0",
                            d2Es: "Asíntota vertical: x = 3; Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota vertical: x = 3; Assíntota horizontal: y = 0",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existem assíntotas"
                        ),
                        (
                            formula: "f(x) = 1 / (x^2 + 1)",
                            graph: "1 / (x^2 + 1)",
                            ansEn: "No vertical asymptotes; Horizontal asymptote: y = 0",
                            ansEs: "Sin asíntotas verticales; Asíntota horizontal: y = 0",
                            ansPt: "Sem assíntotas verticais; Assíntota horizontal: y = 0",
                            stepsEn: "x^2 + 1 != 0 for all real x => No vertical asymptotes. lim x->inf 1/(x^2+1) = 0 => Horizontal asymptote y = 0.",
                            stepsEs: "x^2 + 1 != 0 para todo x real => Sin asíntotas verticales. Límite al infinito = 0 => Asíntota horizontal y = 0.",
                            stepsPt: "x^2 + 1 != 0 para todo x real => Sem assíntotas verticais. Limite ao infinito = 0 => Assíntota horizontal y = 0.",
                            d1En: "Vertical asymptotes: x = -1, 1; Horizontal asymptote: y = 0",
                            d1Es: "Asíntotas verticales: x = -1, 1; Asíntota horizontal: y = 0",
                            d1Pt: "Assíntotas verticais: x = -1, 1; Assíntota horizontal: y = 0",
                            d2En: "Vertical asymptote: x = 0; Horizontal asymptote: y = 1",
                            d2Es: "Asíntota vertical: x = 0; Asíntota horizontal: y = 1",
                            d2Pt: "Assíntota vertical: x = 0; Assíntota horizontal: y = 1",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existem assíntotas"
                        ),
                        (
                            formula: "f(x) = 2x / (x^2 + 1)",
                            graph: "2*x / (x^2 + 1)",
                            ansEn: "No vertical asymptotes; Horizontal asymptote: y = 0; Extrema: (1, 1), (-1, -1)",
                            ansEs: "Sin asíntotas verticales; Asíntota horizontal: y = 0; Extremos: (1, 1), (-1, -1)",
                            ansPt: "Sem assíntotas verticais; Assíntota horizontal: y = 0; Extremos: (1, 1), (-1, -1)",
                            stepsEn: "Denom never 0 => no VA. lim x->+-inf = 0 => HA y = 0. f'(x) = 2(1 - x^2)/(x^2 + 1)^2 = 0 => max at (1, 1), min at (-1, -1).",
                            stepsEs: "Denominador nunca es 0 => sin AV. Límite al infinito = 0 => AH y = 0. Máximo en (1, 1), mínimo en (-1, -1).",
                            stepsPt: "Denominador nunca é 0 => sem AV. Limite no infinito = 0 => AH y = 0. Máximo em (1, 1), mínimo em (-1, -1).",
                            d1En: "Vertical asymptotes: x = -1, 1",
                            d1Es: "Asíntotas verticales: x = -1, 1",
                            d1Pt: "Assíntotas verticais: x = -1, 1",
                            d2En: "Horizontal asymptote: y = 1",
                            d2Es: "Asíntota horizontal: y = 1",
                            d2Pt: "Assíntota horizontal: y = 1",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existen assíntotas"
                        ),
                        (
                            formula: "f(x) = 1 / (x^2 - 1)",
                            graph: "1 / (x^2 - 1)",
                            ansEn: "Vertical asymptotes: x = -1, 1; Horizontal asymptote: y = 0; Local max: (0, -1)",
                            ansEs: "Asíntotas verticales: x = -1, 1; Asíntota horizontal: y = 0; Máximo local: (0, -1)",
                            ansPt: "Assíntotas verticais: x = -1, 1; Assíntota horizontal: y = 0; Máximo local: (0, -1)",
                            stepsEn: "x^2 - 1 = 0 => x = +-1 (VA). Degree num < denom => HA y = 0. f'(x) = -2x/(x^2-1)^2 => local max at (0, -1).",
                            stepsEs: "x^2 - 1 = 0 => x = +-1 (AV). Grado num < denom => AH y = 0. Máximo local en (0, -1).",
                            stepsPt: "x^2 - 1 = 0 => x = +-1 (AV). Grau num < denom => AH y = 0. Máximo em (0, -1).",
                            d1En: "Vertical asymptote: x = 0",
                            d1Es: "Asíntota vertical: x = 0",
                            d1Pt: "Assíntota vertical: x = 0",
                            d2En: "Horizontal asymptote: y = 1",
                            d2Es: "Asíntota horizontal: y = 1",
                            d2Pt: "Assíntota horizontal: y = 1",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existen assíntotas"
                        )
                    };

                    var rationalExps = new[]
                    {
                        (
                            formula: "f(x) = x^(2/3)",
                            graph: "x^(2/3)",
                            ansEn: "No asymptotes exist; Cusp at (0, 0)",
                            ansEs: "No existen asíntotas; Cúspide en (0, 0)",
                            ansPt: "Não existem assíntotas; Cúspide em (0, 0)",
                            stepsEn: "Continuous on R: lim x->inf = inf, no vertical or horizontal asymptotes. Cusp at origin (0,0).",
                            stepsEs: "Continua en R: sin asíntotas verticales ni horizontales. Cúspide en el origen (0,0).",
                            stepsPt: "Contínua em R: sem assíntotas. Cúspide na origem (0,0).",
                            d1En: "Vertical asymptote: x = 0",
                            d1Es: "Asíntota vertical: x = 0",
                            d1Pt: "Assíntota vertical: x = 0",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptote: x = 2; Horizontal asymptote: y = 1",
                            d3Es: "Asíntota vertical: x = 2; Asíntota horizontal: y = 1",
                            d3Pt: "Assíntota vertical: x = 2; Assíntota horizontal: y = 1"
                        ),
                        (
                            formula: "f(x) = (x - 1)^(2/3)",
                            graph: "(x - 1)^(2/3)",
                            ansEn: "No asymptotes exist; Cusp at (1, 0)",
                            ansEs: "No existen asíntotas; Cúspide en (1, 0)",
                            ansPt: "Não existem assíntotas; Cúspide em (1, 0)",
                            stepsEn: "Continuous on R: cusp at x = 1 where derivative is undefined.",
                            stepsEs: "Continua en R: cúspide en x = 1 donde la derivada no está definida.",
                            stepsPt: "Contínua em R: cúspide em x = 1 onde a derivada não existe.",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = x^(1/3)",
                            graph: "x^(1/3)",
                            ansEn: "No asymptotes exist; Inflection point with vertical tangent at (0, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión con tangente vertical en (0, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão com tangente vertical em (0, 0)",
                            stepsEn: "Cube root function: continuous everywhere on R. Derivative f'(x) -> inf as x -> 0, giving a vertical tangent at origin.",
                            stepsEs: "Función raíz cúbica: continua en R. f'(x) -> inf cuando x -> 0, tangente vertical en el origen.",
                            stepsPt: "Raiz cúbica: contínua em R. Tangente vertical na origem.",
                            d1En: "Vertical asymptote: x = 0",
                            d1Es: "Asíntota vertical: x = 0",
                            d1Pt: "Assíntota vertical: x = 0",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Cusp at (0, 0)",
                            d3Es: "Cúspide en (0, 0)",
                            d3Pt: "Cúspide em (0, 0)"
                        ),
                        (
                            formula: "f(x) = x^(4/3) - 4x^(1/3)",
                            graph: "x^(4/3) - 4*x^(1/3)",
                            ansEn: "No asymptotes exist; Relative minimum at (1, -3)",
                            ansEs: "No existen asíntotas; Mínimo relativo en (1, -3)",
                            ansPt: "Não existem assíntotas; Mínimo relativo em (1, -3)",
                            stepsEn: "Continuous on R: critical points at x = 0 (vertical tangent) and x = 1 (f'(1)=0, local min at (1,-3)).",
                            stepsEs: "Continua en R: puntos críticos en x = 0 (tangente vertical) y x = 1 (mínimo relativo en (1,-3)).",
                            stepsPt: "Contínua em R: mínimo relativo em (1,-3).",
                            d1En: "Vertical asymptote: x = 1",
                            d1Es: "Asíntota vertical: x = 1",
                            d1Pt: "Assíntota vertical: x = 1",
                            d2En: "Horizontal asymptote: y = -3",
                            d2Es: "Asíntota horizontal: y = -3",
                            d2Pt: "Assíntota horizontal: y = -3",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = (x + 2)^(2/3)",
                            graph: "(x + 2)^(2/3)",
                            ansEn: "No asymptotes exist; Cusp at (-2, 0)",
                            ansEs: "No existen asíntotas; Cúspide en (-2, 0)",
                            ansPt: "Não existem assíntotas; Cúspide em (-2, 0)",
                            stepsEn: "Shifted power function: continuous on R. Cusp at x = -2 where derivative is undefined.",
                            stepsEs: "Función potencia desplazada: continua en R. Cúspide en x = -2 donde la derivada no está definida.",
                            stepsPt: "Função potência deslocada: contínua em R. Cúspide em x = -2.",
                            d1En: "Vertical asymptote: x = -2",
                            d1Es: "Asíntota vertical: x = -2",
                            d1Pt: "Assíntota vertical: x = -2",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptote: x = 0",
                            d3Es: "Asíntota vertical: x = 0",
                            d3Pt: "Assíntota vertical: x = 0"
                        ),
                        (
                            formula: "f(x) = (x - 2)^(1/3)",
                            graph: "(x - 2)^(1/3)",
                            ansEn: "No asymptotes exist; Inflection point with vertical tangent at (2, 0)",
                            ansEs: "No existen asíntotas; Punto de inflexión con tangente vertical en (2, 0)",
                            ansPt: "Não existem assíntotas; Ponto de inflexão com tangente vertical em (2, 0)",
                            stepsEn: "Cube root shifted: continuous on R. Vertical tangent at x = 2.",
                            stepsEs: "Raíz cúbica desplazada: continua en R. Tangente vertical en x = 2.",
                            stepsPt: "Raiz cúbica deslocada: contínua em R. Tangente vertical em x = 2.",
                            d1En: "Vertical asymptote: x = 2",
                            d1Es: "Asíntota vertical: x = 2",
                            d1Pt: "Assíntota vertical: x = 2",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Cusp at (2, 0)",
                            d3Es: "Cúspide en (2, 0)",
                            d3Pt: "Cúspide em (2, 0)"
                        )
                    };

                    var trigs = new[]
                    {
                        (
                            formula: "f(x) = tan(x)",
                            graph: "tan(x)",
                            ansEn: "Vertical asymptotes: x = pi/2 + pi*k; No horizontal asymptotes",
                            ansEs: "Asíntotas verticales: x = pi/2 + pi*k; Sin asíntotas horizontales",
                            ansPt: "Assíntotas verticais: x = pi/2 + pi*k; Sem assíntotas horizontais",
                            stepsEn: "tan(x) = sin(x)/cos(x). cos(x) = 0 at x = pi/2 + pi*k (infinite vertical asymptotes). Range is R.",
                            stepsEs: "tan(x) = sen(x)/cos(x). cos(x) = 0 en x = pi/2 + pi*k (asíntotas verticales periódicas). Sin asíntotas horizontales.",
                            stepsPt: "tan(x) = sen(x)/cos(x). cos(x) = 0 em x = pi/2 + pi*k (assíntotas verticais). Sem assíntotas horizontais.",
                            d1En: "No asymptotes exist",
                            d1Es: "No existen asíntotas",
                            d1Pt: "Não existem assíntotas",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptote: x = 0 only",
                            d3Es: "Asíntota vertical: x = 0 solamente",
                            d3Pt: "Assíntota vertical: apenas x = 0"
                        ),
                        (
                            formula: "f(x) = sec(x)",
                            graph: "sec(x)",
                            ansEn: "Vertical asymptotes: x = pi/2 + pi*k; Relative minimums at y = 1, maximums at y = -1",
                            ansEs: "Asíntotas verticales: x = pi/2 + pi*k; Mínimos relativos en y = 1, máximos en y = -1",
                            ansPt: "Assíntotas verticais: x = pi/2 + pi*k; Mínimos em y = 1, máximos em y = -1",
                            stepsEn: "sec(x) = 1/cos(x). cos(x) = 0 at x = pi/2 + pi*k (vertical asymptotes). Range is (-inf, -1] U [1, inf).",
                            stepsEs: "sec(x) = 1/cos(x). cos(x) = 0 en x = pi/2 + pi*k (asíntotas verticales). Rango (-inf, -1] U [1, inf).",
                            stepsPt: "sec(x) = 1/cos(x). cos(x) = 0 em x = pi/2 + pi*k (assíntotas verticais).",
                            d1En: "No asymptotes exist",
                            d1Es: "No existen asíntotas",
                            d1Pt: "Não existem assíntotas",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = pi*k",
                            d3Es: "Asíntotas verticales: x = pi*k",
                            d3Pt: "Assíntotas verticais: x = pi*k"
                        ),
                        (
                            formula: "f(x) = cot(x)",
                            graph: "cot(x)",
                            ansEn: "Vertical asymptotes: x = pi*k; Decreasing everywhere on domain",
                            ansEs: "Asíntotas verticales: x = pi*k; Decreciente en todo su dominio",
                            ansPt: "Assíntotas verticais: x = pi*k; Decrescente em todo o domínio",
                            stepsEn: "cot(x) = cos(x)/sin(x). sin(x) = 0 at x = pi*k (vertical asymptotes).",
                            stepsEs: "cot(x) = cos(x)/sen(x). sen(x) = 0 en x = pi*k (asíntotas verticales).",
                            stepsPt: "cot(x) = cos(x)/sen(x). sen(x) = 0 em x = pi*k (assíntotas verticais).",
                            d1En: "Vertical asymptotes: x = pi/2 + pi*k",
                            d1Es: "Asíntotas verticales: x = pi/2 + pi*k",
                            d1Pt: "Assíntotas verticais: x = pi/2 + pi*k",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existem assíntotas"
                        ),
                        (
                            formula: "f(x) = csc(x)",
                            graph: "csc(x)",
                            ansEn: "Vertical asymptotes: x = pi*k; Relative minimums at y = 1, maximums at y = -1",
                            ansEs: "Asíntotas verticales: x = pi*k; Mínimos relativos en y = 1, máximos en y = -1",
                            ansPt: "Assíntotas verticais: x = pi*k; Mínimos em y = 1, máximos em y = -1",
                            stepsEn: "csc(x) = 1/sin(x). sin(x) = 0 at x = pi*k (vertical asymptotes).",
                            stepsEs: "csc(x) = 1/sen(x). sen(x) = 0 en x = pi*k (asíntotas verticales).",
                            stepsPt: "csc(x) = 1/sen(x). sen(x) = 0 em x = pi*k (assíntotas verticais).",
                            d1En: "Vertical asymptotes: x = pi/2 + pi*k",
                            d1Es: "Asíntotas verticales: x = pi/2 + pi*k",
                            d1Pt: "Assíntotas verticais: x = pi/2 + pi*k",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "No asymptotes exist",
                            d3Es: "No existen asíntotas",
                            d3Pt: "Não existen assíntotas"
                        ),
                        (
                            formula: "f(x) = 2sin(x)",
                            graph: "2*sin(x)",
                            ansEn: "No asymptotes exist; Amplitude: 2, Period: 2*pi",
                            ansEs: "No existen asíntotas; Amplitud: 2, Periodo: 2*pi",
                            ansPt: "Não existem assíntotas; Amplitude: 2, Período: 2*pi",
                            stepsEn: "Sine wave: bounded in [-2, 2], continuous everywhere on R. No asymptotes.",
                            stepsEs: "Onda senoidal: acotada en [-2, 2], continua en todo R. Sin asíntotas.",
                            stepsPt: "Onda senoidal: limitada em [-2, 2], contínua em R. Sem assíntotas.",
                            d1En: "Vertical asymptotes: x = pi*k",
                            d1Es: "Asíntotas verticales: x = pi*k",
                            d1Pt: "Assíntotas verticais: x = pi*k",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = pi/2 + pi*k",
                            d3Es: "Asíntotas verticales: x = pi/2 + pi*k",
                            d3Pt: "Assíntotas verticais: x = pi/2 + pi*k"
                        ),
                        (
                            formula: "f(x) = 2cos(x)",
                            graph: "2*cos(x)",
                            ansEn: "No asymptotes exist; Amplitude: 2, Period: 2*pi",
                            ansEs: "No existen asíntotas; Amplitud: 2, Periodo: 2*pi",
                            ansPt: "Não existem assíntotas; Amplitude: 2, Período: 2*pi",
                            stepsEn: "Cosine wave: bounded in [-2, 2], continuous everywhere on R. No asymptotes.",
                            stepsEs: "Onda cosenoidal: acotada en [-2, 2], continua en todo R. Sin asíntotas.",
                            stepsPt: "Onda cossenoidal: limitada em [-2, 2], contínua em R. Sem assíntotas.",
                            d1En: "Vertical asymptotes: x = pi*k",
                            d1Es: "Asíntotas verticales: x = pi*k",
                            d1Pt: "Assíntotas verticais: x = pi*k",
                            d2En: "Horizontal asymptote: y = 0",
                            d2Es: "Asíntota horizontal: y = 0",
                            d2Pt: "Assíntota horizontal: y = 0",
                            d3En: "Vertical asymptotes: x = pi/2 + pi*k",
                            d3Es: "Asíntotas verticales: x = pi/2 + pi*k",
                            d3Pt: "Assíntotas verticais: x = pi/2 + pi*k"
                        )
                    };

                    var pool = new List<(string formula, string graph, string ansEn, string ansEs, string ansPt, string stepsEn, string stepsEs, string stepsPt, string d1En, string d1Es, string d1Pt, string d2En, string d2Es, string d2Pt, string d3En, string d3Es, string d3Pt)>();

                    if (parameters?.IncludeQuadratic == true) pool.AddRange(quadratics);
                    if (parameters?.IncludeCubic == true) pool.AddRange(cubics);
                    if (parameters?.IncludeQuartic == true) pool.AddRange(quartics);
                    if (parameters?.IncludeRational == true) pool.AddRange(rationals);
                    if (parameters?.IncludeRationalExpEasy == true || parameters?.IncludeRationalExpHard == true) pool.AddRange(rationalExps);
                    if (parameters?.IncludeTrig == true) pool.AddRange(trigs);

                    if (pool.Count == 0)
                    {
                        pool.AddRange(quadratics);
                        pool.AddRange(cubics);
                        pool.AddRange(quartics);
                        pool.AddRange(rationals);
                        pool.AddRange(rationalExps);
                        pool.AddRange(trigs);
                    }

                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        var item = pool[(startOffset + i) % pool.Count];
                        string ans = LocalizationService.CurrentLanguage switch
                        {
                            AppLanguage.Spanish => item.ansEs,
                            AppLanguage.Portuguese => item.ansPt,
                            _ => item.ansEn
                        };
                        string steps = LocalizationService.CurrentLanguage switch
                        {
                            AppLanguage.Spanish => item.stepsEs,
                            AppLanguage.Portuguese => item.stepsPt,
                            _ => item.stepsEn
                        };
                        string d1 = LocalizationService.CurrentLanguage switch
                        {
                            AppLanguage.Spanish => item.d1Es,
                            AppLanguage.Portuguese => item.d1Pt,
                            _ => item.d1En
                        };
                        string d2 = LocalizationService.CurrentLanguage switch
                        {
                            AppLanguage.Spanish => item.d2Es,
                            AppLanguage.Portuguese => item.d2Pt,
                            _ => item.d2En
                        };
                        string d3 = LocalizationService.CurrentLanguage switch
                        {
                            AppLanguage.Spanish => item.d3Es,
                            AppLanguage.Portuguese => item.d3Pt,
                            _ => item.d3En
                        };

                        var q = new Question
                        {
                            ProblemStatement = item.formula,
                            FormulaText = item.formula,
                            Type = type,
                            AnswerText = ans,
                            SolutionSteps = steps,
                            GraphFormula = item.graph,
                            ProvideBlankGraph = parameters?.ProvideBlankGraph ?? false,
                            ProvideFunctionGraph = parameters?.ProvideFunctionGraph ?? false,
                            TemplateId = $"curve_sketch_{item.formula}"
                        };

                        if (type == QuestionType.MultipleChoice)
                        {
                            q.Choices = DistractorHelper.BuildChoices(ans, d1, d2, d3, rng);
                        }
                        list.Add(q);
                    }
                    return list;
                });

            AddTopic("app_graph_comparison", "app_diff",
                "Graphical comparison of f, f', and f''", "Comparación gráfica de f, f' y f''", "Comparação gráfica de f, f' e f''",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Relate features between the graphs of f, f', and f''.", "Relacione características entre las gráficas de f, f' y f''.", "Relacione características entre os gráficos de f, f' e f''.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateGraphComparisonProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_motion", "app_diff",
                "Motion along a line", "Movimiento rectilíneo", "Movimento retilíneo",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Find position, velocity, and acceleration for a particle.", "Halle posición, velocidad y aceleración para una partícula.", "Encontre posição, velocidade e aceleração para uma partícula.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateMotionProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_related_rates", "app_diff",
                "Related rates", "Tasas de cambio relacionadas", "Taxas relacionadas",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Solve each related rates problem.", "Resuelva cada problema de tasas de cambio relacionadas.", "Resolva cada problema de taxas relacionadas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateRelatedRatesProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_differentials", "app_diff",
                "Differentials", "Diferenciales", "Diferenciais",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Use differentials to approximate each value.", "Use diferenciales para aproximar cada valor.", "Use diferenciais para aproximar cada valor.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDifferentialsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_newton", "app_diff",
                "Newton's Method", "Método de Newton", "Método de Newton",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Apply Newton's Method to find x1.", "Aplique el Método de Newton para hallar x1.", "Aplique o Método de Newton para encontrar x1.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateNewtonMethodProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_limits_deriv_form", "app_diff",
                "Limits in form of definition of derivative", "Límites en forma de definición de derivada", "Limites na forma de definição da derivada",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Evaluate each limit by recognizing it as a derivative.", "Evalúe cada límite reconociéndolo como una derivada.", "Avalie cada limite reconociendo-o como uma derivada.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitsDerivFormProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_lhopital", "app_diff",
                "L'Hôpital's Rule", "Regla de L'Hôpital", "Regra de L'Hôpital",
                "Applications of Differentiation", "Aplicaciones de la Derivada", "Aplicações da Derivação",
                "Evaluate each limit using L'Hôpital's Rule.", "Evalúe cada límite usando la Regla de L'Hôpital.", "Avalie cada limite usando a Regra de L'Hôpital.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLHopitalProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion

            #region 5. Indefinite Integration
            AddTopic("indef_power", "indef_int",
                "Power Rule", "Regla de la potencia", "Regra da potência",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each indefinite integral.", "Evalúe cada integral indefinida.", "Avalie cada integral indefinida.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIntegrationProblem(parameters, startOffset + i, type, rng, "indef_power"));
                    }
                    return list;
                });

            AddTopic("indef_log_exp", "indef_int",
                "Logarithmic Rule and Exponentials", "Regla logarítmica y exponenciales", "Regra logarítmica e exponenciais",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each integral involving e^x and 1/x.", "Evalúe cada integral que involucra e^x y 1/x.", "Avalie cada integral envolvendo e^x e 1/x.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefLogExpProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_trig", "indef_int",
                "Trigonometric", "Trigonométricas", "Trigonométricas",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each trigonometric indefinite integral.", "Evalúe cada integral trigonométrica indefinida.", "Avalie cada integral trigonométrica indefinida.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefTrigProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_inv_trig", "indef_int",
                "Inverse trigonometric", "Trigonométricas inversas", "Trigonométricas inversas",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each integral yielding inverse trig functions.", "Evalúe cada integral que produce funciones trigonométricas inversas.", "Avalie cada integral que produz funções trigonométricas inversas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefInvTrigProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_subs_power", "indef_int",
                "Power rule with substitution", "Regla de potencia con sustitución", "Regra da potência com substituição",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each indefinite integral using u-substitution.", "Evalúe cada integral indefinida usando sustitución u.", "Avalie cada integral indefinida usando substituição u.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefSubsPowerProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_subs_log_exp", "indef_int",
                "Logarithmic rule and exponentials with subs.", "Regla logarítmica y exponenciales con sust.", "Regra logarítmica e exponenciais com subst.",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each integral using substitution.", "Evalúe cada integral usando sustitución.", "Avalie cada integral usando substituição.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefSubsLogExpProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_subs_trig", "indef_int",
                "Trigonometric with substitution", "Trigonométricas con sustitución", "Trigonométricas com substituição",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each trigonometric integral with u-substitution.", "Evalúe cada integral trigonométrica con sustitución u.", "Avalie cada integral trigonométrica com substituição u.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefSubsTrigProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_subs_inv_trig", "indef_int",
                "Inverse trigonometric with substitution", "Trigonométricas inversas con sustitución", "Trigonométricas inversas com substituição",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate using substitution leading to inverse trig functions.", "Evalúe usando sustitución para obtener funciones trigonométricas inversas.", "Avalie usando substituição que leva a funções trigonométricas inversas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIndefSubsInvTrigProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("indef_parts", "indef_int",
                "Integration by parts", "Integración por partes", "Integração por partes",
                "Indefinite Integration", "Integración Indefinida", "Integração Indefinida",
                "Evaluate each integral using integration by parts.", "Evalúe cada integral usando integración por partes.", "Avalie cada integral usando integração por partes.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateIntegrationProblem(parameters, startOffset + i, type, rng, "indef_parts"));
                    }
                    return list;
                });
            #endregion

            #region 6. Definite Integration
            AddTopic("def_approx_area", "def_int",
                "Approximating area under a curve", "Aproximación del área bajo la curva", "Aproximação da área sob a curva",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Approximate the area under each curve using Riemann sums.", "Aproxime el área bajo cada curva usando sumas de Riemann.", "Aproxime a área sob cada curva usando somas de Riemann.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateApproxAreaProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("def_limits_sums", "def_int",
                "Area under a curve by limit of sums", "Área bajo la curva por límites de sumas", "Área sob a curva por limites de somas",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Express or evaluate the area as a limit of Riemann sums.", "Exprese o evalúe el área como límite de sumas de Riemann.", "Expresse ou avalie a área como limite de somas de Riemann.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateLimitsOfSumsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("def_riemann_tables", "def_int",
                "Riemann sum tables", "Tablas de sumas de Riemann", "Tabelas de somas de Riemann",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Approximate the definite integral from the table of values.", "Aproxime la integral definida a partir de la tabla de valores.", "Aproxime a integral definida a partir da tabela de valores.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateRiemannTableProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("def_ftc1", "def_int",
                "First Fundamental Theorem of Calculus", "Primer Teorema Fundamental del Cálculo", "Primeiro Teorema Fundamental do Cálculo",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Evaluate each definite integral.", "Evalúe cada integral definida.", "Avalie cada integral definida.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDefiniteIntegrationProblem(parameters, startOffset + i, type, rng, "def_ftc1"));
                    }
                    return list;
                });

            AddTopic("def_subs_change_vars", "def_int",
                "Substitution with change of variables", "Sustitución con cambio de variables", "Substituição com mudança de variáveis",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Evaluate by substituting and changing limits of integration.", "Evalúe sustituyendo y cambiando los límites de integración.", "Avalie substituindo e mudando os limites de integração.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateChangeOfVarsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("def_mvt_integrals", "def_int",
                "Mean Value Theorem", "Teorema del Valor Medio", "Teorema do Valor Médio",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Find the average value of each function on the given interval.", "Halle el valor promedio de cada función en el intervalo dado.", "Encontre o valor médio de cada função no intervalo dado.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateMvtIntegralsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("def_ftc2", "def_int",
                "Second Fundamental Theorem of Calculus", "Segundo Teorema Fundamental del Cálculo", "Segundo Teorema Fundamental do Cálculo",
                "Definite Integration", "Integración Definida", "Integração Definida",
                "Find the derivative of each accumulation function.", "Halle la derivada de cada función de acumulación.", "Encontre a derivada de cada função de acumulação.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateFtc2Problem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion

            #region 7. Applications of Integration
            AddTopic("app_int_area_curve", "app_int",
                "Area under a curve", "Área bajo una curva", "Área sob uma curva",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Find the area under the curve on the given interval.", "Halle el área bajo la curva en el intervalo dado.", "Encontre a área sob a curva no intervalo dado.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateAreaUnderCurveProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_int_area_between", "app_int",
                "Area between curves", "Área entre curvas", "Área entre curvas",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Find the area of the region enclosed by the curves.", "Halle el área de la región encerrada por las curvas.", "Encontre a área da región delimitada pelas curvas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateAreaBetweenCurvesProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_int_volume_disks", "app_int",
                "Volume by slicing, disks and washers", "Volumen por secciones, discos y arandelas", "Volume por fatiamento, discos e anéis",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Find the volume of the solid of revolution using disks/washers.", "Halle el volumen del sólido de revolución usando discos/arandelas.", "Encontre o volume do sólido de revolução usando discos/anéis.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateVolumeDisksProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_int_volume_shells", "app_int",
                "Volume by cylinders", "Volumen por cascarones cilíndricos", "Volume por cascas cilíndricas",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Find the volume of revolution using cylindrical shells.", "Halle el volumen de revolución usando cascarones cilíndricos.", "Encontre o volume de revolução usando cascas cilíndricas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateVolumeShellsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_int_cross_sections", "app_int",
                "Volume of solids with known cross sections", "Volumen de sólidos con secciones conocidas", "Volume de sólidos com seções transversais conhecidas",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Find the volume of the solid with the specified cross sections.", "Halle el volumen del sólido con las secciones transversales especificadas.", "Encontre o volume do sólido com as seções transversais especificadas.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateCrossSectionsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("app_int_motion_revisited", "app_int",
                "Motion along a line revisited", "Movimiento rectilíneo revisado", "Movimento retilíneo revisitado",
                "Applications of Integration", "Aplicaciones de la Integral", "Aplicações da Integração",
                "Given velocity, find displacement and total distance traveled.", "Dada la velocidad, halle el desplazamiento y la distancia total recorrida.", "Dada a velocidade, encontre o deslocamento e a distância total percorrida.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateMotionRevisitedProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion

            #region 8. Differential Equations
            AddTopic("diff_eq_slope_fields", "diff_eq",
                "Slope fields", "Campos de pendientes", "Campos de direções",
                "Differential Equations", "Ecuaciones Diferenciales", "Equações Diferenciais",
                "Match the differential equation with its slope field.", "Relacione la ecuación diferencial con su campo de pendientes.", "Associe a equação diferencial ao seu campo de direções.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateSlopeFieldsProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("diff_eq_intro", "diff_eq",
                "Introduction", "Introducción", "Introdução",
                "Differential Equations", "Ecuaciones Diferenciales", "Equações Diferenciais",
                "Verify whether the given function is a solution to the differential equation.", "Verifique si la función dada es solución de la ecuación diferencial.", "Verifique se a função dada é uma solução da equação diferencial.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateDiffEqIntroProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("diff_eq_separable", "diff_eq",
                "Separable", "Separables", "Separáveis",
                "Differential Equations", "Ecuaciones Diferenciales", "Equações Diferenciais",
                "Find the general solution to each separable differential equation.", "Halle la solución general para cada ecuación diferencial separable.", "Encontre a solução general para cada equação diferencial separável.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateSeparableProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });

            AddTopic("diff_eq_growth_decay", "diff_eq",
                "Exponential growth and decay", "Crecimiento y decaimiento exponencial", "Crescimento e decaimento exponencial",
                "Differential Equations", "Ecuaciones Diferenciales", "Equações Diferenciais",
                "Solve each exponential growth or decay problem.", "Resuelva cada problema de crecimiento o decaimiento exponencial.", "Resolva cada problema de crescimento ou decaimento exponencial.",
                (count, type, parameters, rng) =>
                {
                    int startOffset = rng.Next(100);
                    var list = new List<Question>();
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(CompositeFunctionBuilder.GenerateGrowthDecayProblem(parameters, startOffset + i, type, rng));
                    }
                    return list;
                });
            #endregion
        }

        private static void AddTopic(
            string topicId, string categoryId,
            string titleEn, string titleEs, string titlePt,
            string catEn, string catEs, string catPt,
            string dirEn, string dirEs, string dirPt,
            Func<int, QuestionType, TopicGeneratorParameters?, Random, List<Question>> genFunc)
        {
            _topics.Add(new TopicDefinition
            {
                TopicId = topicId,
                CategoryId = categoryId,
                TitleEn = titleEn,
                TitleEs = titleEs,
                TitlePt = titlePt,
                CategoryEn = catEn,
                CategoryEs = catEs,
                CategoryPt = catPt,
                DirectionsEn = dirEn,
                DirectionsEs = dirEs,
                DirectionsPt = dirPt,
                GeneratorFuncWithParams = genFunc,
                GeneratorFunc = (cnt, tp, r) => genFunc(cnt, tp, null, r)
            });
        }

        private static void AddTopic(
            string topicId, string categoryId,
            string titleEn, string titleEs, string titlePt,
            string catEn, string catEs, string catPt,
            string dirEn, string dirEs, string dirPt,
            Func<int, QuestionType, Random, List<Question>> genFunc)
        {
            AddTopic(topicId, categoryId, titleEn, titleEs, titlePt, catEn, catEs, catPt, dirEn, dirEs, dirPt,
                (cnt, tp, p, r) => genFunc(cnt, tp, r));
        }
    }
}
