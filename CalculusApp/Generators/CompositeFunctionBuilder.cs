using System;
using System.Collections.Generic;
using CalculusApp.Models;
using CalculusApp.Services;

namespace CalculusApp.Generators
{
    public static class CompositeFunctionBuilder
    {
        public static bool HasPoly(TopicGeneratorParameters? p) => p == null || p.IncludePolynomial || p.IncludeQuadratic || p.IncludeCubic || p.IncludeQuartic;
        public static bool HasTrig(TopicGeneratorParameters? p) => p != null && p.IncludeTrig;
        public static bool HasRational(TopicGeneratorParameters? p) => p != null && p.IncludeRational;
        public static bool HasRadical(TopicGeneratorParameters? p) => p != null && p.IncludeRadical;
        public static bool HasExp(TopicGeneratorParameters? p) => p != null && (p.IncludeExp || p.IncludeExpLog);
        public static bool HasLog(TopicGeneratorParameters? p) => p != null && (p.IncludeLog || p.IncludeExpLog);
        public static bool HasAbs(TopicGeneratorParameters? p) => p != null && p.IncludeAbs;
        public static bool HasInverseTrig(TopicGeneratorParameters? p) => p != null && p.IncludeInverseTrig;

        #region Limits Generation with Composite Types
        public static Question GenerateLimitProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "limits_direct")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool rat = HasRational(p);
            bool rad = HasRadical(p);
            bool exp = HasExp(p);
            bool log = HasLog(p);
            bool abs = HasAbs(p);

            if (!poly && !trig && !rat && !rad && !exp && !log && !abs)
                poly = true;

            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));

            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución directa",
                AppLanguage.Portuguese => "Substituição direta",
                _ => "Direct substitution"
            };

            var pool = new List<Func<Question>>();

            // 1. Jump Discontinuities (always included if abs is true, or if topicId == "limits_jumps")
            if (abs || topicId == "limits_jumps")
            {
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    int k = rng.Next(1, 4);
                    bool isRight = rng.Next(2) == 0;
                    string dir = isRight ? "^+" : "^-";
                    string aStr = a == 0 ? "0" : a.ToString();
                    string subExpr = a == 0 ? "x" : (a > 0 ? $"x - {a}" : $"x + {Math.Abs(a)}");
                    string kStr = k == 1 ? "" : $"{k}";
                    string formula = $"lim [x -> {aStr}{dir}] ({kStr}*|{subExpr}|) / ({subExpr})";
                    int ansVal = isRight ? k : -k;
                    string ans = ansVal.ToString();
                    string steps = isRight
                        ? $"Para $x > {aStr}$, $|{subExpr}| = ({subExpr}) \\Rightarrow \\lim_{{x \\to {aStr}^+}} \\frac{{{kStr}({subExpr})}}{{{subExpr}}} = {ansVal}$"
                        : $"Para $x < {aStr}$, $|{subExpr}| = -({subExpr}) \\Rightarrow \\lim_{{x \\to {aStr}^-}} \\frac{{-{kStr}({subExpr})}}{{{subExpr}}} = {ansVal}$";

                    var q = new Question
                    {
                        ProblemStatement = formula,
                        FormulaText = formula,
                        GraphFormula = $"({kStr}*abs({subExpr})) / ({subExpr})",
                        Type = type,
                        AnswerText = ans,
                        SolutionSteps = steps
                    };
                    q.GraphHoles.Add(new System.Windows.Point(a, k));
                    q.GraphHoles.Add(new System.Windows.Point(a, -k));
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (-ansVal).ToString(), "0", "Does not exist", rng);
                    return q;
                });
            }

            // 2. Removable Holes / Indeterminate Forms (0/0)
            if (rat || poly || topicId == "limits_holes")
            {
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    int b = rng.Next(-3, 4);
                    while (b == -a) b = rng.Next(-3, 4);

                    int sum = b - a;
                    int prod = -a * b;
                    string sumPart = sum == 0 ? "" : (sum > 0 ? $"+ {sum}x " : $"- {Math.Abs(sum)}x ");
                    string prodPart = prod == 0 ? "" : (prod > 0 ? $"+ {prod}" : $"- {Math.Abs(prod)}");
                    string numExpr = $"x^2 {sumPart}{prodPart}".Trim();
                    string denExpr = a == 0 ? "x" : (a > 0 ? $"x - {a}" : $"x + {Math.Abs(a)}");

                    string formula = $"lim [x -> {a}] ({numExpr}) / ({denExpr})";
                    int ansVal = a + b;
                    string ans = ansVal.ToString();
                    string bSign = b >= 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                    string steps = $"Factorizando: $\\frac{{({denExpr})(x {bSign})}}{{{denExpr}}} = x {bSign}$. Luego $\\lim_{{x \\to {a}}} = {a} {bSign} = {ansVal}$";

                    var q = new Question
                    {
                        ProblemStatement = formula,
                        FormulaText = formula,
                        GraphFormula = $"({numExpr}) / ({denExpr})",
                        Type = type,
                        AnswerText = ans,
                        SolutionSteps = steps
                    };
                    q.GraphHoles.Add(new System.Windows.Point(a, ansVal));
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 1).ToString(), (-ansVal).ToString(), "0", rng);
                    return q;
                });
            }

            // 3. Trigonometric Limits
            if (trig)
            {
                if (comp == 0 && topicId != "limits_holes")
                {
                    // Simple direct evaluation trig limit
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(1, 4);
                        int c2 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                        string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                        string formula = $"lim [x -> 0] ({c1}*sin(x) {c2Str}*cos(x))";
                        int ansVal = c2;
                        string ans = ansVal.ToString();
                        string steps = $"{substLabel}: ${c1}\\sin(0) {c2Str}\\cos(0) = 0 {c2Str}(1) = {ansVal}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "0", c1.ToString(), (-c2).ToString(), rng);
                        return q;
                    });
                }
                else
                {
                    // Special trig limits: sin(kx)/(mx) or (1 - cos(kx))/x^2
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 6);
                        int m = rng.Next(2, 5);
                        string formula = $"lim [x -> 0] sin({k}x) / ({m}x)";
                        int g = Gcd(k, m);
                        int num = k / g;
                        int den = m / g;
                        string ans = den == 1 ? num.ToString() : $"{num}/{den}";
                        string steps = $"Forma 0/0: $\\lim_{{x \\to 0}} \\frac{{\\sin({k}x)}}{{{m}x}} = \\frac{{{k}}}{{{m}}} \\lim_{{x \\to 0}} \\frac{{\\sin({k}x)}}{{{k}x}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "Does not exist", rng);
                        return q;
                    });

                    if (comp >= 2)
                    {
                        pool.Add(() =>
                        {
                            int k = rng.Next(2, 5);
                            string formula = $"lim [x -> 0] (1 - cos({k}x)) / (x^2)";
                            int num = k * k;
                            int den = 2;
                            string ans = (num % den == 0) ? (num / den).ToString() : $"{num}/{den}";
                            string steps = $"Multiplicando por el conjugado: $\\lim_{{x \\to 0}} \\frac{{1 - \\cos^2({k}x)}}{{x^2(1 + \\cos({k}x))}} = \\lim_{{x \\to 0}} \\left(\\frac{{\\sin({k}x)}}{{x}}\\right)^2 \\frac{{1}}{{1 + \\cos({k}x)}} = ({k})^2 \\cdot \\frac{{1}}{{2}} = {ans}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "0", k.ToString(), "Does not exist", rng);
                            return q;
                        });
                    }
                }
            }

            // 4. Radical Limits (Conjugate or direct)
            if (rad)
            {
                if (comp >= 1 || topicId == "limits_holes")
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        int a2 = a * a;
                        string formula = $"lim [x -> 0] (sqrt(x + {a2}) - {a}) / x";
                        int den = 2 * a;
                        string ans = $"1/{den}";
                        string steps = $"Racionalizando numerador: $\\frac{{(\\sqrt{{x + {a2}}} - {a})(\\sqrt{{x + {a2}}} + {a})}}{{x(\\sqrt{{x + {a2}}} + {a})}} = \\frac{{x}}{{x(\\sqrt{{x + {a2}}} + {a})}} = \\frac{{1}}{{\\sqrt{{0 + {a2}}} + {a}}} = \\frac{{1}}{{{den}}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "0", $"1/{a}", "Does not exist", rng);
                        return q;
                    });
                }
                else
                {
                    pool.Add(() =>
                    {
                        int[] perfects = { 1, 4, 9 };
                        int a = perfects[rng.Next(perfects.Length)];
                        int sqrtA = (int)Math.Round(Math.Sqrt(a));
                        int c1 = rng.Next(1, 3);
                        int c2 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                        string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                        string formula = $"lim [x -> {a}] ({c1}x {c2Str}*sqrt(x))";
                        int ansVal = c1 * a + c2 * sqrtA;
                        string ans = ansVal.ToString();
                        string steps = $"{substLabel}: ${c1}({a}) {c2Str}\\sqrt{{{a}}} = {c1 * a} {c2Str}({sqrtA}) = {ansVal}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 4).ToString(), rng);
                        return q;
                    });
                }
            }

            // 5. Exponential Limits
            if (exp)
            {
                if (comp == 0)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(1, 4);
                        int c2 = rng.Next(-3, 4);
                        string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                        string formula = $"lim [x -> 0] ({c1}*e^x {c2Str})";
                        int ansVal = c1 + c2;
                        string ans = ansVal.ToString();
                        string steps = $"{substLabel}: ${c1}e^0 {c2Str} = {c1}(1) {c2Str} = {ansVal}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), "0", rng);
                        return q;
                    });
                }
                else
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 6);
                        string formula = $"lim [x -> 0] (e^({k}x) - 1) / x";
                        string ans = k.ToString();
                        string steps = $"Forma 0/0: Por la definición de derivada o L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{e^{{{k}x}} - 1}}{{x}} = {k}e^0 = {k}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (k * 2).ToString(), rng);
                        return q;
                    });
                }
            }

            // 6. Logarithmic Limits
            if (log)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"lim [x -> 1] (ln({k}x) / ln(x + {k - 1}))";
                    string ans = "1";
                    string steps = $"{substLabel}: $\\frac{{\\ln({k}(1))}}{{\\ln(1 + {k - 1})}} = \\frac{{\\ln({k})}}{{\\ln({k})}} = 1$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", k.ToString(), "Does not exist", rng);
                    return q;
                });
            }

            // 7. Composite Polynomial + Trig/Exp/Rat
            if (poly && trig)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> 0] (x + {a})*sin(x) / x";
                    string ans = a.ToString();
                    string steps = $"Forma 0/0: $\\lim_{{x \\to 0}} (x + {a}) \\cdot \\frac{{\\sin(x)}}{{x}} = (0 + {a}) \\cdot 1 = {a}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", (a + 1).ToString(), rng);
                    return q;
                });
            }

            if (poly && exp)
            {
                pool.Add(() =>
                {
                    int c1 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                    int c2 = rng.Next(2, 6) * (rng.Next(2) == 0 ? 1 : -1);
                    int c3 = rng.Next(-5, 6);
                    string c1Str = c1 == 1 ? "" : (c1 == -1 ? "-" : $"{c1}");
                    string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                    string c3Str = c3 >= 0 ? $"+ {c3}" : $"- {Math.Abs(c3)}";
                    string formula = $"lim [x -> 0] ({c1Str}x^2 {c2Str}*e^(x) {c3Str})";
                    int ansVal = c2 + c3;
                    string ans = ansVal.ToString();
                    string steps = $"{substLabel}: ${c1Str}(0)^2 {c2Str}e^{{0}} {c3Str} = 0 {c2Str}(1) {c3Str} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 1).ToString(), rng);
                    return q;
                });
            }

            // 8. Polynomial Limits (scaling with complexity)
            if (poly || pool.Count == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    if (comp == 0)
                    {
                        // Linear
                        int c1 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                        int c2 = rng.Next(-4, 5);
                        string c1Str = c1 == 1 ? "" : (c1 == -1 ? "-" : $"{c1}");
                        string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                        string polyExpr = $"{c1Str}x {c2Str}".Trim();
                        string formula = $"lim [x -> {a}] ({polyExpr})";
                        int ansVal = c1 * a + c2;
                        string ans = ansVal.ToString();
                        string steps = $"{substLabel}: ${c1Str}({a}) {c2Str} = {ansVal}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, GraphFormula = polyExpr, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 1).ToString(), rng);
                        return q;
                    }
                    else
                    {
                        // Quadratic / Cubic
                        int c1 = (rng.Next(2) == 0 ? 1 : -1);
                        int c2 = rng.Next(-2, 3);
                        int L = rng.Next(-4, 5);
                        int c3 = L - (c1 * a * a + c2 * a);
                        if (Math.Abs(c3) > 5)
                        {
                            c3 = rng.Next(-3, 4);
                            L = c1 * a * a + c2 * a + c3;
                        }
                        string c1Part = c1 == 1 ? "" : "-";
                        string c2Part = c2 == 0 ? "" : (c2 > 0 ? $"+ {c2}x " : $"- {Math.Abs(c2)}x ");
                        string c3Part = c3 == 0 ? "" : (c3 > 0 ? $"+ {c3}" : $"- {Math.Abs(c3)}");
                        string polyExpr = $"{c1Part}x^2 {c2Part}{c3Part}".Trim();
                        string formula = $"lim [x -> {a}] ({polyExpr})";
                        int ansVal = L;
                        string ans = ansVal.ToString();
                        string c2Eval = c2 == 0 ? "" : (c2 > 0 ? $"+ {c2}({a}) " : $"- {Math.Abs(c2)}({a}) ");
                        string c3Eval = c3 == 0 ? "" : (c3 > 0 ? $"+ {c3}" : $"- {Math.Abs(c3)}");
                        string steps = $"{substLabel}: ${c1Part}({a})^2 {c2Eval}{c3Eval} = {ansVal}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, GraphFormula = polyExpr, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 3).ToString(), rng);
                        return q;
                    }
                });
            }

            // Select randomly from all available archetypes according to index and random offset
            int chosenIdx = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[chosenIdx]();
        }
        #endregion

        #region Differentiation with Composite Types
        public static Question GenerateDifferentiationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "deriv_power")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);
            bool log = HasLog(p);
            bool rat = HasRational(p);
            bool rad = HasRadical(p);
            bool invTrig = HasInverseTrig(p);

            if (!poly && !trig && !exp && !log && !rat && !rad && !invTrig)
                poly = true;

            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));

            string ruleLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Regla de derivación",
                AppLanguage.Portuguese => "Regra de derivação",
                _ => "Differentiation rule"
            };

            var pool = new List<Func<Question>>();

            // -------------------------------------------------------------
            // A. Product Rule (deriv_product)
            // -------------------------------------------------------------
            if (topicId == "deriv_product")
            {
                if (poly && trig)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            int a = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                            string aStr = a > 0 ? $"+ {a}" : $"- {Math.Abs(a)}";
                            string formula = $"f(x) = (x {aStr})*sin(x)";
                            string ans = $"f'(x) = sin(x) + (x {aStr})*cos(x)";
                            string steps = $"{ruleLabel}: $u = x {aStr},\\; v = \\sin(x) \\Rightarrow u'v + uv' = \\sin(x) + (x {aStr})\\cos(x)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = (x {aStr})*cos(x)", $"f'(x) = sin(x) - (x {aStr})*cos(x)", $"f'(x) = cos(x)", rng);
                            return q;
                        }
                        else if (comp == 1)
                        {
                            int a = rng.Next(2, 6);
                            string formula = $"f(x) = (x^2 + {a})*sin(x)";
                            string ans = $"f'(x) = 2x*sin(x) + (x^2 + {a})*cos(x)";
                            string steps = $"{ruleLabel}: $u = x^2 + {a},\\; v = \\sin(x) \\Rightarrow u'v + uv' = 2x\\sin(x) + (x^2 + {a})\\cos(x)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x*cos(x)", $"f'(x) = (x^2 + {a})*cos(x)", $"f'(x) = 2x*sin(x) - (x^2 + {a})*cos(x)", rng);
                            return q;
                        }
                        else
                        {
                            int k = rng.Next(2, 5);
                            string formula = $"f(x) = (x^3 - 2x)*sin({k}x)";
                            string ans = $"f'(x) = (3x^2 - 2)*sin({k}x) + {k}(x^3 - 2x)*cos({k}x)";
                            string steps = $"{ruleLabel}: $u'v + uv' = (3x^2 - 2)\\sin({k}x) + {k}(x^3 - 2x)\\cos({k}x)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 3x^2*sin({k}x) + (x^3 - 2x)*cos({k}x)", $"f'(x) = (3x^2 - 2)*cos({k}x)", $"f'(x) = {k}(3x^2 - 2)*cos({k}x)", rng);
                            return q;
                        }
                    });
                }

                if (poly && exp)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            string formula = "f(x) = x*e^x";
                            string ans = "f'(x) = (x + 1)*e^x";
                            string steps = $"{ruleLabel}: $u = x,\\; v = e^x \\Rightarrow u'v + uv' = (1)e^x + x e^x = (x + 1)e^x$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = e^x", "f'(x) = x*e^x", "f'(x) = (x - 1)*e^x", rng);
                            return q;
                        }
                        else
                        {
                            int k = rng.Next(2, 5);
                            int a = rng.Next(1, 5);
                            string formula = $"f(x) = (x^2 + {a})*e^({k}x)";
                            string ans = $"f'(x) = (2x + {k}x^2 + {k * a})*e^({k}x)";
                            string steps = $"{ruleLabel}: $2x e^{{{k}x}} + {k}(x^2 + {a})e^{{{k}x}} = ({k}x^2 + 2x + {k * a})e^{{{k}x}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x*{k}*e^({k}x)", $"f'(x) = (2x + {a})*e^({k}x)", $"f'(x) = (x^2 + {a})*e^({k}x)", rng);
                            return q;
                        }
                    });
                }

                if (poly && log)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            string formula = "f(x) = x*ln(x)";
                            string ans = "f'(x) = ln(x) + 1";
                            string steps = $"{ruleLabel}: $(1)\\ln(x) + x\\left(\\frac{{1}}{{x}}\\right) = \\ln(x) + 1$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = ln(x)", "f'(x) = 1/x", "f'(x) = x*ln(x) + 1", rng);
                            return q;
                        }
                        else
                        {
                            int c = rng.Next(2, 5);
                            string formula = $"f(x) = (x^{{{c}}})*ln(x)";
                            string ans = $"f'(x) = {c}x^{{{c - 1}}}*ln(x) + x^{{{c - 1}}}";
                            string steps = $"{ruleLabel}: ${c}x^{{{c - 1}}}\\ln(x) + x^{{{c}}}\\left(\\frac{{1}}{{x}}\\right) = x^{{{c - 1}}}({c}\\ln(x) + 1)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c}x^{{{c - 1}}}*ln(x)", $"f'(x) = x^{{{c - 1}}}", $"f'(x) = {c}x^{{{c - 1}}} / x", rng);
                            return q;
                        }
                    });
                }

                if (trig && exp)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 5);
                        string formula = $"f(x) = e^({k}x)*cos(x)";
                        string ans = $"f'(x) = e^({k}x)*({k}*cos(x) - sin(x))";
                        string steps = $"{ruleLabel}: ${k}e^{{{k}x}}\\cos(x) + e^{{{k}x}}(-\\sin(x)) = e^{{{k}x}}({k}\\cos(x) - \\sin(x))$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = -{k}*e^({k}x)*sin(x)", $"f'(x) = e^({k}x)*({k}*cos(x) + sin(x))", $"f'(x) = e^({k}x)*cos(x)", rng);
                        return q;
                    });
                }

                if (pool.Count == 0 || (poly && !trig && !exp && !log))
                {
                    // Polynomial Product
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        int b = rng.Next(1, 5);
                        if (comp == 0)
                        {
                            string formula = $"f(x) = (x + {a})*(x + {b})";
                            string ans = $"f'(x) = 2x + {a + b}";
                            string steps = $"{ruleLabel}: $1(x + {b}) + (x + {a})(1) = 2x + {a + b}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x + {a * b}", $"f'(x) = x + {a + b}", "f'(x) = 2x", rng);
                            return q;
                        }
                        else
                        {
                            string formula = $"f(x) = (x^2 + {a})*(2x - {b})";
                            string ans = $"f'(x) = 6x^2 - {2 * b}x + {2 * a}";
                            string steps = $"{ruleLabel}: $2x(2x - {b}) + 2(x^2 + {a}) = 4x^2 - {2 * b}x + 2x^2 + {2 * a} = 6x^2 - {2 * b}x + {2 * a}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 4x^2 - {2 * b}x", $"f'(x) = 6x^2 + {2 * a}", $"f'(x) = 2x*(2x - {b})", rng);
                            return q;
                        }
                    });
                }
            }

            // -------------------------------------------------------------
            // B. Quotient Rule (deriv_quotient)
            // -------------------------------------------------------------
            else if (topicId == "deriv_quotient")
            {
                if (trig && poly)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            string formula = "f(x) = sin(x) / x";
                            string ans = "f'(x) = (x*cos(x) - sin(x)) / x^2";
                            string steps = $"{ruleLabel}: $\\frac{{\\cos(x)\\cdot x - \\sin(x)\\cdot 1}}{{x^2}} = \\frac{{x\\cos(x) - \\sin(x)}}{{x^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = cos(x) / 1", "f'(x) = (x*cos(x) + sin(x)) / x^2", "f'(x) = -sin(x) / x^2", rng);
                            return q;
                        }
                        else
                        {
                            int k = rng.Next(1, 5);
                            string formula = $"f(x) = sin(x) / (x^2 + {k})";
                            string ans = $"f'(x) = ((x^2 + {k})*cos(x) - 2x*sin(x)) / (x^2 + {k})^2";
                            string steps = $"{ruleLabel}: $\\frac{{u'v - uv'}}{{v^2}} \\Rightarrow \\frac{{(x^2 + {k})\\cos(x) - 2x\\sin(x)}}{{(x^2 + {k})^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = cos(x) / (2x)", $"f'(x) = ((x^2 + {k})*cos(x) + 2x*sin(x)) / (x^2 + {k})^2", $"f'(x) = -2x*sin(x) / (x^2 + {k})^2", rng);
                            return q;
                        }
                    });
                }

                if (exp && poly)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            string formula = "f(x) = e^x / x";
                            string ans = "f'(x) = (e^x*(x - 1)) / x^2";
                            string steps = $"{ruleLabel}: $\\frac{{e^x \\cdot x - e^x \\cdot 1}}{{x^2}} = \\frac{{e^x(x - 1)}}{{x^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = e^x / 1", "f'(x) = (e^x*(x + 1)) / x^2", "f'(x) = e^x / x^2", rng);
                            return q;
                        }
                        else
                        {
                            int a = rng.Next(1, 4);
                            string formula = $"f(x) = e^(2x) / (x^2 + {a})";
                            string ans = $"f'(x) = (2*e^(2x)*(x^2 - x + {a})) / (x^2 + {a})^2";
                            string steps = $"{ruleLabel}: $\\frac{{2e^{{2x}}(x^2 + {a}) - e^{{2x}}(2x)}}{{(x^2 + {a})^2}} = \\frac{{2e^{{2x}}(x^2 - x + {a})}}{{(x^2 + {a})^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2*e^(2x) / (2x)", $"f'(x) = e^(2x) / (x^2 + {a})^2", $"f'(x) = (2*e^(2x)*(x^2 + {a})) / (x^2 + {a})^2", rng);
                            return q;
                        }
                    });
                }

                if (log && poly)
                {
                    pool.Add(() =>
                    {
                        string formula = "f(x) = ln(x) / x";
                        string ans = "f'(x) = (1 - ln(x)) / x^2";
                        string steps = $"{ruleLabel}: $\\frac{{(1/x)\\cdot x - \\ln(x)\\cdot 1}}{{x^2}} = \\frac{{1 - \\ln(x)}}{{x^2}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 1 / x^2", "f'(x) = (1 + ln(x)) / x^2", "f'(x) = -ln(x) / x^2", rng);
                        return q;
                    });
                }

                if (pool.Count == 0 || poly)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        int b = rng.Next(1, 5);
                        if (comp == 0)
                        {
                            string formula = $"f(x) = (x + {a}) / (x + {b})";
                            int num = b - a;
                            string ans = $"f'(x) = {num} / (x + {b})^2";
                            string steps = $"{ruleLabel}: $\\frac{{1(x + {b}) - (x + {a})(1)}}{{(x + {b})^2}} = \\frac{{{num}}}{{(x + {b})^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {num + 2} / (x + {b})^2", "f'(x) = 1", $"f'(x) = {-num} / (x + {b})^2", rng);
                            return q;
                        }
                        else
                        {
                            string formula = $"f(x) = (x^2 + {a}) / (x - {b})";
                            string ans = $"f'(x) = (x^2 - {2 * b}x - {a}) / (x - {b})^2";
                            string steps = $"{ruleLabel}: $\\frac{{2x(x - {b}) - (x^2 + {a})}}{{(x - {b})^2}} = \\frac{{2x^2 - {2 * b}x - x^2 - {a}}}{{(x - {b})^2}} = \\frac{{x^2 - {2 * b}x - {a}}}{{(x - {b})^2}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x / 1", $"f'(x) = (x^2 + {a}) / (x - {b})^2", $"f'(x) = (x^2 - {a}) / (x - {b})^2", rng);
                            return q;
                        }
                    });
                }
            }

            // -------------------------------------------------------------
            // C. Chain Rule (deriv_chain)
            // -------------------------------------------------------------
            else if (topicId == "deriv_chain")
            {
                if (trig)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            int k = rng.Next(2, 6);
                            string formula = $"f(x) = sin({k}x)";
                            string ans = $"f'(x) = {k}*cos({k}x)";
                            string steps = $"{ruleLabel}: $u = {k}x \\Rightarrow u'\\cos(u) = {k}\\cos({k}x)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = cos({k}x)", $"f'(x) = -{k}*cos({k}x)", $"f'(x) = {k}*sin({k}x)", rng);
                            return q;
                        }
                        else
                        {
                            int c1 = rng.Next(2, 5);
                            int c2 = rng.Next(1, 5);
                            string formula = $"f(x) = sin({c1}x^2 + {c2}x)";
                            string ans = $"f'(x) = ({2 * c1}x + {c2})*cos({c1}x^2 + {c2}x)";
                            string steps = $"{ruleLabel}: $u = {c1}x^2 + {c2}x \\Rightarrow u'\\cos(u) = ({2 * c1}x + {c2})\\cos({c1}x^2 + {c2}x)$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = cos({c1}x^2 + {c2}x)", $"f'(x) = ({2 * c1}x + {c2})*sin({c1}x^2 + {c2}x)", $"f'(x) = {2 * c1}x*cos({c1}x^2 + {c2}x)", rng);
                            return q;
                        }
                    });
                }

                if (exp)
                {
                    pool.Add(() =>
                    {
                        if (comp == 0)
                        {
                            int k = rng.Next(2, 6);
                            string formula = $"f(x) = e^({k}x)";
                            string ans = $"f'(x) = {k}*e^({k}x)";
                            string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}e^{{kx}} = k e^{{kx}} = {k}e^{{{k}x}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = e^({k}x)", $"f'(x) = {k}x*e^({k - 1}x)", $"f'(x) = {k}*e^x", rng);
                            return q;
                        }
                        else
                        {
                            int a = rng.Next(1, 4);
                            string formula = $"f(x) = e^({a}x^2 + 2x)";
                            string ans = $"f'(x) = ({2 * a}x + 2)*e^({a}x^2 + 2x)";
                            string steps = $"{ruleLabel}: $u = {a}x^2 + 2x \\Rightarrow u' e^u = ({2 * a}x + 2)e^{{{a}x^2 + 2x}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {2 * a}x*e^({a}x^2 + 2x)", $"f'(x) = e^({a}x^2 + 2x)", $"f'(x) = ({2 * a}x + 2)*e^x", rng);
                            return q;
                        }
                    });
                }

                if (log)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        string formula = $"f(x) = ln(x^2 + {a})";
                        string ans = $"f'(x) = 2x / (x^2 + {a})";
                        string steps = $"{ruleLabel}: $\\frac{{u'}}{{u}} = \\frac{{2x}}{{x^2 + {a}}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (x^2 + {a})", $"f'(x) = 2x / x", "f'(x) = 2x", rng);
                        return q;
                    });
                }

                if (invTrig)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 5);
                        string formula = $"f(x) = arctan({k}x)";
                        string ans = $"f'(x) = {k} / (1 + {k * k}x^2)";
                        string steps = $"{ruleLabel}: $\\frac{{u'}}{{1 + u^2}} = \\frac{{{k}}}{{1 + ({k}x)^2}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (1 + {k * k}x^2)", $"f'(x) = {k} / (1 + x^2)", $"f'(x) = {k} / sqrt(1 - {k * k}x^2)", rng);
                        return q;
                    });
                }

                if (rad)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 6);
                        string formula = $"f(x) = sqrt(x^2 + {a})";
                        string ans = $"f'(x) = x / sqrt(x^2 + {a})";
                        string steps = $"{ruleLabel}: $\\frac{{1}}{{2\\sqrt{{x^2 + {a}}}}} \\cdot 2x = \\frac{{x}}{{\\sqrt{{x^2 + {a}}}}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x / sqrt(x^2 + {a})", $"f'(x) = 1 / (2*sqrt(x^2 + {a}))", "f'(x) = x", rng);
                        return q;
                    });
                }

                if (pool.Count == 0 || poly)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        int n = rng.Next(3, 6);
                        if (comp == 0)
                        {
                            string formula = $"f(x) = (2x + {a})^{{{n}}}";
                            int coeff = 2 * n;
                            string ans = $"f'(x) = {coeff}*(2x + {a})^{{{n - 1}}}";
                            string steps = $"{ruleLabel}: ${n}(2x + {a})^{{{n - 1}}} \\cdot (2) = {coeff}(2x + {a})^{{{n - 1}}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {n}*(2x + {a})^{{{n - 1}}}", $"f'(x) = 2*(2x + {a})^{{{n - 1}}}", $"f'(x) = {coeff}*(2x + {a})^{{{n}}}", rng);
                            return q;
                        }
                        else
                        {
                            string formula = $"f(x) = (x^2 + {a})^{{{n}}}";
                            int coeff = 2 * n;
                            string ans = $"f'(x) = {coeff}x*(x^2 + {a})^{{{n - 1}}}";
                            string steps = $"{ruleLabel}: ${n}(x^2 + {a})^{{{n - 1}}} \\cdot (2x) = {coeff}x(x^2 + {a})^{{{n - 1}}}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {n}*(x^2 + {a})^{{{n - 1}}}", $"f'(x) = 2x*(x^2 + {a})^{{{n - 1}}}", $"f'(x) = {coeff}*(x^2 + {a})^{{{n - 1}}}", rng);
                            return q;
                        }
                    });
                }
            }

            // -------------------------------------------------------------
            // D. Higher Order Derivatives (deriv_higher_order)
            // -------------------------------------------------------------
            else if (topicId == "deriv_higher_order")
            {
                if (trig)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 5);
                        int c2 = rng.Next(2, 5);
                        string formula = $"Find f''(x) for f(x) = {c1}x^3 + {c2}*cos(x)";
                        string ans = $"f''(x) = {6 * c1}x - {c2}*cos(x)";
                        string steps = $"$f'(x) = {3 * c1}x^2 - {c2}\\sin(x) \\Rightarrow f''(x) = {6 * c1}x - {c2}\\cos(x)$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {6 * c1}x + {c2}*cos(x)", $"f''(x) = {3 * c1}x^2 - {c2}*sin(x)", $"f''(x) = {6 * c1}x - {c2}*sin(x)", rng);
                        return q;
                    });
                }

                if (exp)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 5);
                        int a = rng.Next(1, 4);
                        string formula = $"Find f''(x) for f(x) = {a}*e^({k}x)";
                        int ansCoeff = a * k * k;
                        string ans = $"f''(x) = {ansCoeff}*e^({k}x)";
                        string steps = $"$f'(x) = {a * k}e^{{{k}x}} \\Rightarrow f''(x) = {ansCoeff}e^{{{k}x}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {a * k}*e^({k}x)", $"f''(x) = {a}*e^({k}x)", $"f''(x) = {ansCoeff}x*e^({k}x)", rng);
                        return q;
                    });
                }

                if (rat)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        string formula = $"Find f''(x) for f(x) = {a} / x";
                        string ans = $"f''(x) = {2 * a} / x^3";
                        string steps = $"$f'(x) = -{a}x^{{-2}} \\Rightarrow f''(x) = {2 * a}x^{{-3}} = \\frac{{{2 * a}}}{{x^3}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = -{2 * a} / x^3", $"f''(x) = {a} / x^2", $"f''(x) = {2 * a} / x^2", rng);
                        return q;
                    });
                }

                if (pool.Count == 0 || poly)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 5);
                        int c2 = rng.Next(2, 6);
                        int p = comp == 0 ? 3 : (comp == 1 ? 4 : 5);
                        string formula = $"Find f''(x) for f(x) = {c1}x^{{{p}}} + {c2}x^2";
                        int d1 = c1 * p * (p - 1);
                        int dp = p - 2;
                        int d2 = 2 * c2;
                        string term1 = dp == 1 ? $"{d1}x" : (dp == 0 ? $"{d1}" : $"{d1}x^{{{dp}}}");
                        string ans = $"f''(x) = {term1} + {d2}";
                        string steps = $"$f'(x) = {c1 * p}x^{{{p - 1}}} + {2 * c2}x \\Rightarrow f''(x) = {term1} + {d2}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {c1 * p}x^{{{p - 1}}} + {d2}", $"f''(x) = {term1} - {d2}", $"f''(x) = {c1}x^{{{dp}}} + {d2}", rng);
                        return q;
                    });
                }
            }

            // -------------------------------------------------------------
            // E. Pure Trig (deriv_trig)
            // -------------------------------------------------------------
            else if (topicId == "deriv_trig")
            {
                pool.Add(() =>
                {
                    if (comp == 0)
                    {
                        int a = rng.Next(2, 6);
                        int b = rng.Next(2, 6);
                        string formula = $"f(x) = {a}*sin(x) + {b}*cos(x)";
                        string ans = $"f'(x) = {a}*cos(x) - {b}*sin(x)";
                        string steps = $"{ruleLabel}: ${a}(\\cos(x)) + {b}(-\\sin(x)) = {a}\\cos(x) - {b}\\sin(x)$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}*cos(x) + {b}*sin(x)", $"f'(x) = -{a}*sin(x) + {b}*cos(x)", $"f'(x) = {a}*sin(x) - {b}*cos(x)", rng);
                        return q;
                    }
                    else
                    {
                        int k = rng.Next(2, 5);
                        string formula = $"f(x) = tan({k}x)";
                        string ans = $"f'(x) = {k}*sec^2({k}x)";
                        string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}\\tan(u) = u'\\sec^2(u) = {k}\\sec^2({k}x)$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = sec^2({k}x)", $"f'(x) = {k}*tan({k}x)", $"f'(x) = -{k}*sec^2({k}x)", rng);
                        return q;
                    }
                });
            }

            // -------------------------------------------------------------
            // F. Default / General Sum / Power Rule (deriv_power)
            // -------------------------------------------------------------
            else
            {
                if (trig)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 5);
                        int c2 = rng.Next(2, 5);
                        string formula = $"f(x) = {c1}x^2 + {c2}*sin(x)";
                        string ans = $"f'(x) = {2 * c1}x + {c2}*cos(x)";
                        string steps = $"{ruleLabel}: ${2 * c1}x + {c2}\\cos(x)$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x + {c2}*cos(x)", $"f'(x) = {2 * c1}x - {c2}*cos(x)", $"f'(x) = {2 * c1}x + {c2}*sin(x)", rng);
                        return q;
                    });
                }

                if (exp)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 5);
                        string formula = $"f(x) = {c1}x^2 + e^x";
                        string ans = $"f'(x) = {2 * c1}x + e^x";
                        string steps = $"{ruleLabel}: ${2 * c1}x + e^x$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x + e^x", $"f'(x) = {2 * c1}x + x*e^(x-1)", $"f'(x) = {2 * c1}x", rng);
                        return q;
                    });
                }

                if (rat)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        string formula = $"f(x) = x^2 + {a}/x";
                        string ans = $"f'(x) = 2x - {a}/x^2";
                        string steps = $"{ruleLabel}: $2x - {a}x^{{-2}} = 2x - \\frac{{{a}}}{{x^2}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x + {a}/x^2", $"f'(x) = 2x - {a}", "f'(x) = 2x", rng);
                        return q;
                    });
                }

                if (rad)
                {
                    pool.Add(() =>
                    {
                        string formula = "f(x) = x^2 + 4*sqrt(x)";
                        string ans = "f'(x) = 2x + 2/sqrt(x)";
                        string steps = $"{ruleLabel}: $2x + 4\\left(\\frac{{1}}{{2}}x^{{-1/2}}\\right) = 2x + \\frac{{2}}{{\\sqrt{{x}}}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 2x + 4/sqrt(x)", "f'(x) = 2x + 2*sqrt(x)", "f'(x) = 2x + 1/(2*sqrt(x))", rng);
                        return q;
                    });
                }

                if (pool.Count == 0 || poly)
                {
                    pool.Add(() =>
                    {
                        int p1 = comp == 0 ? rng.Next(2, 4) : (comp == 1 ? rng.Next(3, 5) : rng.Next(4, 7));
                        int c1 = rng.Next(2, 6) * (rng.Next(2) == 0 ? 1 : -1);
                        int p2 = rng.Next(1, p1);
                        int c2 = rng.Next(2, 7) * (rng.Next(2) == 0 ? 1 : -1);

                        string formula = $"f(x) = {c1}x^{{{p1}}} {(c2 >= 0 ? "+ " + c2 : "- " + Math.Abs(c2))}x^{{{p2}}}";
                        int d1 = c1 * p1;
                        int dp1 = p1 - 1;
                        int d2 = c2 * p2;
                        int dp2 = p2 - 1;
                        string term1 = dp1 == 1 ? $"{d1}x" : $"{d1}x^{{{dp1}}}";
                        string term2 = dp2 == 0 ? (d2 >= 0 ? $"+ {d2}" : $"- {Math.Abs(d2)}") : (d2 >= 0 ? $"+ {d2}x^{{{dp2}}}" : $"- {Math.Abs(d2)}x^{{{dp2}}}");
                        string ans = $"f'(x) = {term1} {term2}".Trim();
                        string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}(c x^n) = c \\cdot n x^{{n-1}} \\Rightarrow {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x^{{{dp1}}} + {c2}x^{{{dp2}}}", $"f'(x) = {d1}x^{{{p1}}} + {d2}x^{{{p2}}}", $"f'(x) = {-d1}x^{{{dp1}}}", rng);
                        return q;
                    });
                }
            }

            // Pick fairly from all archetypes
            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0) { int t = b; b = a % b; a = t; }
            return a == 0 ? 1 : Math.Abs(a);
        }

        public static Question GenerateAverageRateOfChangeProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int mode = p?.ComplexityOptionIndex ?? 0;
            if (mode > 2) mode = 0;

            bool trig = HasTrig(p);
            bool rat = HasRational(p);
            bool rad = HasRadical(p);
            bool exp = HasExp(p);
            bool poly = HasPoly(p) || (!trig && !rat && !rad && !exp);

            var availableTypes = new List<string>();
            if (poly && trig)
            {
                availableTypes.Add("poly_trig");
                availableTypes.Add("poly_trig");
            }
            if (poly && rat) availableTypes.Add("poly_rat");
            if (poly && exp) availableTypes.Add("poly_exp");
            if (poly && rad) availableTypes.Add("poly_rad");

            if (availableTypes.Count == 0)
            {
                if (poly) availableTypes.Add("poly");
                if (trig) availableTypes.Add("trig");
                if (rat) availableTypes.Add("rat");
                if (rad) availableTypes.Add("rad");
                if (exp) availableTypes.Add("exp");
            }
            if (availableTypes.Count == 0) availableTypes.Add("poly");

            string chosenType = availableTypes[index % availableTypes.Count];

            string funcLatex = "";
            string evalExpr = "";
            int a = 1, b = 3;
            string aStr = "1", bStr = "3";
            string faStr = "", fbStr = "";
            bool isIntVal = true;
            int intRate = 0;
            string rateLatex = "";
            string secantLineLatex = "";

            if (chosenType == "poly_trig")
            {
                int ptArch = (index + rng.Next(6)) % 6;
                if (ptArch == 0)
                {
                    // f(x) = x + sin(x) on [0, \pi]
                    funcLatex = "x + \\sin(x)";
                    evalExpr = "x + sin(x)";
                    aStr = "0";
                    bStr = "\\pi";
                    faStr = "0";
                    fbStr = "\\pi";
                    rateLatex = "1";
                    secantLineLatex = "y = x";
                    intRate = 1;
                    isIntVal = true;
                }
                else if (ptArch == 1)
                {
                    // f(x) = 2x - sin(2x) on [0, \pi/2]
                    funcLatex = "2x - \\sin(2x)";
                    evalExpr = "2*x - sin(2*x)";
                    aStr = "0";
                    bStr = "\\frac{\\pi}{2}";
                    faStr = "0";
                    fbStr = "\\pi";
                    rateLatex = "2";
                    secantLineLatex = "y = 2x";
                    intRate = 2;
                    isIntVal = true;
                }
                else if (ptArch == 2)
                {
                    // f(x) = 3x + 2\cos(\pi x) on [0, 2]
                    funcLatex = "3x + 2\\cos(\\pi x)";
                    evalExpr = "3*x + 2*cos(pi*x)";
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    faStr = "2"; fbStr = "8";
                    rateLatex = "3";
                    secantLineLatex = "y = 3x + 2";
                    intRate = 3;
                    isIntVal = true;
                }
                else if (ptArch == 3)
                {
                    // f(x) = x^2 - 2\sin(\frac{\pi}{2} x) on [0, 2]
                    funcLatex = "x^2 - 2\\sin\\left(\\frac{\\pi}{2}x\\right)";
                    evalExpr = "x^2 - 2*sin(pi/2*x)";
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    faStr = "0"; fbStr = "4";
                    rateLatex = "2";
                    secantLineLatex = "y = 2x";
                    intRate = 2;
                    isIntVal = true;
                }
                else if (ptArch == 4)
                {
                    // f(x) = 2x + \cos(2\pi x) on [0, 1]
                    funcLatex = "2x + \\cos(2\\pi x)";
                    evalExpr = "2*x + cos(2*pi*x)";
                    a = 0; b = 1;
                    aStr = "0"; bStr = "1";
                    faStr = "1"; fbStr = "3";
                    rateLatex = "2";
                    secantLineLatex = "y = 2x + 1";
                    intRate = 2;
                    isIntVal = true;
                }
                else
                {
                    // f(x) = x^2 + 4\sin(\pi x) on [0, 1]
                    funcLatex = "x^2 + 4\\sin(\\pi x)";
                    evalExpr = "x^2 + 4*sin(pi*x)";
                    a = 0; b = 1;
                    aStr = "0"; bStr = "1";
                    faStr = "0"; fbStr = "1";
                    rateLatex = "1";
                    secantLineLatex = "y = x";
                    intRate = 1;
                    isIntVal = true;
                }
            }
            else if (chosenType == "poly_rat")
            {
                int prArch = (index + rng.Next(4)) % 4;
                if (prArch == 0)
                {
                    funcLatex = "x + \\frac{4}{x}";
                    evalExpr = "x + 4/x";
                    a = 1; b = 4;
                    aStr = "1"; bStr = "4";
                    faStr = "5"; fbStr = "5";
                    rateLatex = "0";
                    secantLineLatex = "y = 5";
                    intRate = 0;
                    isIntVal = true;
                }
                else if (prArch == 1)
                {
                    funcLatex = "x + \\frac{4}{x}";
                    evalExpr = "x + 4/x";
                    a = 1; b = 2;
                    aStr = "1"; bStr = "2";
                    faStr = "5"; fbStr = "4";
                    rateLatex = "-1";
                    secantLineLatex = "y = -x + 6";
                    intRate = -1;
                    isIntVal = true;
                }
                else if (prArch == 2)
                {
                    funcLatex = "x^2 - \\frac{2}{x}";
                    evalExpr = "x^2 - 2/x";
                    a = 1; b = 2;
                    aStr = "1"; bStr = "2";
                    faStr = "-1"; fbStr = "3";
                    rateLatex = "4";
                    secantLineLatex = "y = 4x - 5";
                    intRate = 4;
                    isIntVal = true;
                }
                else
                {
                    funcLatex = "2x + \\frac{3}{x + 1}";
                    evalExpr = "2*x + 3/(x+1)";
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    faStr = "3"; fbStr = "5";
                    rateLatex = "1";
                    secantLineLatex = "y = x + 3";
                    intRate = 1;
                    isIntVal = true;
                }
            }
            else if (chosenType == "poly_exp")
            {
                int peArch = (index + rng.Next(3)) % 3;
                if (peArch == 0)
                {
                    funcLatex = "x + 2^x";
                    evalExpr = "x + 2^x";
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    faStr = "1"; fbStr = "6";
                    rateLatex = "\\frac{5}{2}";
                    secantLineLatex = "y - 1 = \\frac{5}{2}x";
                    isIntVal = false;
                }
                else if (peArch == 1)
                {
                    funcLatex = "2x + 2^x";
                    evalExpr = "2*x + 2^x";
                    a = 0; b = 3;
                    aStr = "0"; bStr = "3";
                    faStr = "1"; fbStr = "14";
                    rateLatex = "\\frac{13}{3}";
                    secantLineLatex = "y - 1 = \\frac{13}{3}x";
                    isIntVal = false;
                }
                else
                {
                    funcLatex = "3x - 2^x";
                    evalExpr = "3*x - 2^x";
                    a = 1; b = 3;
                    aStr = "1"; bStr = "3";
                    faStr = "1"; fbStr = "1";
                    rateLatex = "0";
                    secantLineLatex = "y = 1";
                    intRate = 0;
                    isIntVal = true;
                }
            }
            else if (chosenType == "trig")
            {
                int tArch = (index + rng.Next(2)) % 2;
                if (tArch == 0)
                {
                    funcLatex = "\\sin(x)";
                    evalExpr = "sin(x)";
                    aStr = "0";
                    bStr = "\\frac{\\pi}{2}";
                    faStr = "0";
                    fbStr = "1";
                    rateLatex = "\\frac{2}{\\pi}";
                    secantLineLatex = "y = \\frac{2}{\\pi}x";
                    isIntVal = false;
                }
                else
                {
                    funcLatex = "2\\cos(x)";
                    evalExpr = "2*cos(x)";
                    aStr = "0";
                    bStr = "\\pi";
                    faStr = "2";
                    fbStr = "-2";
                    rateLatex = "-\\frac{4}{\\pi}";
                    secantLineLatex = "y - 2 = -\\frac{4}{\\pi}x";
                    isIntVal = false;
                }
            }
            else if (chosenType == "rat")
            {
                int c = 1;
                int k = 6;
                a = 1;
                b = 5;
                aStr = a.ToString();
                bStr = b.ToString();
                funcLatex = $"\\frac{{{k}}}{{x + {c}}}";
                evalExpr = $"{k}/(x+{c})";
                faStr = "3";
                fbStr = "1";
                rateLatex = "-\\frac{1}{2}";
                secantLineLatex = "y = -\\frac{1}{2}x + \\frac{7}{2}";
                isIntVal = false;
            }
            else if (chosenType == "rad")
            {
                int radArch = (index + rng.Next(2)) % 2;
                if (radArch == 0)
                {
                    a = 0; b = 4;
                    aStr = "0"; bStr = "4";
                    funcLatex = "\\sqrt{2x + 1}";
                    evalExpr = "sqrt(2*x+1)";
                    faStr = "1"; fbStr = "3";
                    rateLatex = "\\frac{1}{2}";
                    secantLineLatex = "y = \\frac{1}{2}x + 1";
                    isIntVal = false;
                }
                else
                {
                    a = 1; b = 9;
                    aStr = "1"; bStr = "9";
                    funcLatex = "\\sqrt{x}";
                    evalExpr = "sqrt(x)";
                    faStr = "1"; fbStr = "3";
                    rateLatex = "\\frac{1}{4}";
                    secantLineLatex = "y = \\frac{1}{4}x + \\frac{3}{4}";
                    isIntVal = false;
                }
            }
            else if (chosenType == "exp")
            {
                a = 1; b = 3;
                aStr = "1"; bStr = "3";
                funcLatex = "2^x";
                evalExpr = "2^x";
                faStr = "2"; fbStr = "8";
                intRate = 3;
                rateLatex = "3";
                secantLineLatex = "y = 3x - 1";
                isIntVal = true;
            }
            else
            {
                int polyArch = (index + rng.Next(4)) % 4;
                if (polyArch == 0)
                {
                    int c1 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                    int c2 = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                    int c3 = rng.Next(-4, 5);
                    a = rng.Next(-2, 1);
                    b = a + rng.Next(2, 4);
                    aStr = a.ToString();
                    bStr = b.ToString();

                    string c1Part = c1 == 1 ? "x^2" : (c1 == -1 ? "-x^2" : $"{c1}x^2");
                    string c2Part = c2 > 0 ? $" + {c2}x" : $" - {Math.Abs(c2)}x";
                    string c3Part = c3 == 0 ? "" : (c3 > 0 ? $" + {c3}" : $" - {Math.Abs(c3)}");
                    funcLatex = $"{c1Part}{c2Part}{c3Part}".Trim();
                    evalExpr = funcLatex;

                    int fa = c1 * a * a + c2 * a + c3;
                    int fb = c1 * b * b + c2 * b + c3;
                    faStr = fa.ToString();
                    fbStr = fb.ToString();
                    int diffY = fb - fa;
                    int diffX = b - a;
                    if (diffY % diffX == 0)
                    {
                        intRate = diffY / diffX;
                        rateLatex = intRate.ToString();
                        int yIntercept = fa - intRate * a;
                        string bPart = yIntercept == 0 ? "" : (yIntercept > 0 ? $" + {yIntercept}" : $" - {Math.Abs(yIntercept)}");
                        string mPart = intRate == 1 ? "x" : (intRate == -1 ? "-x" : (intRate == 0 ? "" : $"{intRate}x"));
                        secantLineLatex = mPart == "" ? $"y = {yIntercept}" : $"y = {mPart}{bPart}";
                    }
                    else
                    {
                        int g = Gcd(Math.Abs(diffY), Math.Abs(diffX));
                        int num = diffY / g;
                        int den = diffX / g;
                        if (den < 0) { num = -num; den = -den; }
                        rateLatex = $"\\frac{{{num}}}{{{den}}}";
                        secantLineLatex = $"y - {fa} = \\frac{{{num}}}{{{den}}}(x - {a})";
                        isIntVal = false;
                    }
                }
                else if (polyArch == 1)
                {
                    int c1 = rng.Next(2) == 0 ? 1 : -1;
                    int c2 = rng.Next(2, 5);
                    a = rng.Next(-1, 2);
                    b = a + 2;
                    aStr = a.ToString();
                    bStr = b.ToString();
                    string c1Str = c1 == 1 ? "x^3" : "-x^3";
                    funcLatex = $"{c1Str} - {c2}x";
                    evalExpr = funcLatex;
                    int fa = c1 * a * a * a - c2 * a;
                    int fb = c1 * b * b * b - c2 * b;
                    faStr = fa.ToString();
                    fbStr = fb.ToString();
                    int diffY = fb - fa;
                    int diffX = b - a;
                    if (diffY % diffX == 0)
                    {
                        intRate = diffY / diffX;
                        rateLatex = intRate.ToString();
                        int yIntercept = fa - intRate * a;
                        string bPart = yIntercept == 0 ? "" : (yIntercept > 0 ? $" + {yIntercept}" : $" - {Math.Abs(yIntercept)}");
                        string mPart = intRate == 1 ? "x" : (intRate == -1 ? "-x" : $"{intRate}x");
                        secantLineLatex = $"y = {mPart}{bPart}";
                    }
                    else
                    {
                        int g = Gcd(Math.Abs(diffY), Math.Abs(diffX));
                        int num = diffY / g;
                        int den = diffX / g;
                        if (den < 0) { num = -num; den = -den; }
                        rateLatex = $"\\frac{{{num}}}{{{den}}}";
                        secantLineLatex = $"y - {fa} = \\frac{{{num}}}{{{den}}}(x - {a})";
                        isIntVal = false;
                    }
                }
                else if (polyArch == 2)
                {
                    int c1 = rng.Next(1, 3);
                    int c2 = rng.Next(3, 8);
                    a = rng.Next(0, 2);
                    b = a + rng.Next(2, 4);
                    aStr = a.ToString();
                    bStr = b.ToString();
                    string c1Str = c1 == 1 ? "-x^2" : $"-{c1}x^2";
                    funcLatex = $"{c1Str} + {c2}";
                    evalExpr = funcLatex;
                    int fa = -c1 * a * a + c2;
                    int fb = -c1 * b * b + c2;
                    faStr = fa.ToString();
                    fbStr = fb.ToString();
                    int diffY = fb - fa;
                    int diffX = b - a;
                    if (diffY % diffX == 0)
                    {
                        intRate = diffY / diffX;
                        rateLatex = intRate.ToString();
                        int yIntercept = fa - intRate * a;
                        string bPart = yIntercept == 0 ? "" : (yIntercept > 0 ? $" + {yIntercept}" : $" - {Math.Abs(yIntercept)}");
                        string mPart = intRate == 1 ? "x" : (intRate == -1 ? "-x" : $"{intRate}x");
                        secantLineLatex = $"y = {mPart}{bPart}";
                    }
                    else
                    {
                        int g = Gcd(Math.Abs(diffY), Math.Abs(diffX));
                        int num = diffY / g;
                        int den = diffX / g;
                        if (den < 0) { num = -num; den = -den; }
                        rateLatex = $"\\frac{{{num}}}{{{den}}}";
                        secantLineLatex = $"y - {fa} = \\frac{{{num}}}{{{den}}}(x - {a})";
                        isIntVal = false;
                    }
                }
                else
                {
                    int c2 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                    a = rng.Next(0, 2);
                    b = a + 2;
                    aStr = a.ToString();
                    bStr = b.ToString();
                    string c2Str = c2 > 0 ? $"+ {c2}x^2" : $"- {Math.Abs(c2)}x^2";
                    funcLatex = $"x^3 {c2Str}";
                    evalExpr = funcLatex;
                    int fa = a * a * a + c2 * a * a;
                    int fb = b * b * b + c2 * b * b;
                    faStr = fa.ToString();
                    fbStr = fb.ToString();
                    int diffY = fb - fa;
                    int diffX = b - a;
                    if (diffY % diffX == 0)
                    {
                        intRate = diffY / diffX;
                        rateLatex = intRate.ToString();
                        int yIntercept = fa - intRate * a;
                        string bPart = yIntercept == 0 ? "" : (yIntercept > 0 ? $" + {yIntercept}" : $" - {Math.Abs(yIntercept)}");
                        string mPart = intRate == 1 ? "x" : (intRate == -1 ? "-x" : $"{intRate}x");
                        secantLineLatex = $"y = {mPart}{bPart}";
                    }
                    else
                    {
                        int g = Gcd(Math.Abs(diffY), Math.Abs(diffX));
                        int num = diffY / g;
                        int den = diffX / g;
                        if (den < 0) { num = -num; den = -den; }
                        rateLatex = $"\\frac{{{num}}}{{{den}}}";
                        secantLineLatex = $"y - {fa} = \\frac{{{num}}}{{{den}}}(x - {a})";
                        isIntVal = false;
                    }
                }
            }

            string formula = $"f(x) = {funcLatex};\\; [{aStr}, {bStr}]";

            string langPrompt = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Tasa de cambio promedio",
                AppLanguage.Portuguese => "Taxa de variação média",
                _ => "Average rate of change"
            };
            string langSecant = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Recta secante",
                AppLanguage.Portuguese => "Reta secante",
                _ => "Secant line"
            };

            string ans = "";
            string steps = "";

            if (mode == 1)
            {
                ans = secantLineLatex;
                steps = $"{langSecant}: $m = \\frac{{f({bStr}) - f({aStr})}}{{{bStr} - {aStr}}} = \\frac{{{fbStr} - ({faStr})}}{{{bStr} - {aStr}}} = {rateLatex}$. " +
                        $"Punto $({aStr}, {faStr})$: ${secantLineLatex}$";
            }
            else if (mode == 2)
            {
                ans = $"{rateLatex};\\; {secantLineLatex}";
                steps = $"{langPrompt}: $\\frac{{f({bStr}) - f({aStr})}}{{{bStr} - {aStr}}} = \\frac{{{fbStr} - ({faStr})}}{{{bStr} - {aStr}}} = {rateLatex}$. " +
                        $"{langSecant}: ${secantLineLatex}$";
            }
            else
            {
                ans = rateLatex;
                steps = $"{langPrompt}: $\\frac{{f(b) - f(a)}}{{b - a}} = \\frac{{f({bStr}) - f({aStr})}}{{{bStr} - {aStr}}} = \\frac{{{fbStr} - ({faStr})}}{{{bStr} - {aStr}}} = {rateLatex}$";
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                GraphFormula = evalExpr,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                if (mode == 1)
                {
                    q.Choices = DistractorHelper.BuildChoices(ans, "y = 2x + 1", "y = -x + 3", "y = x - 2", rng);
                }
                else if (mode == 2)
                {
                    q.Choices = DistractorHelper.BuildChoices(ans, $"2;\\; y = 2x", $"-1;\\; y = -x + 1", $"0;\\; y = 0", rng);
                }
                else
                {
                    if (isIntVal)
                    {
                        q.Choices = DistractorHelper.BuildChoices(ans, (intRate + 2).ToString(), (intRate - 3).ToString(), (-intRate).ToString(), rng);
                    }
                    else
                    {
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", "-1", rng);
                    }
                }
            }

            return q;
        }

        public static Question GenerateDerivativeDefinitionProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int mode = p?.ComplexityOptionIndex ?? 0;
            if (mode == 2) mode = rng.Next(2);

            bool rat = HasRational(p);
            bool rad = HasRadical(p);
            bool poly = HasPoly(p) || (!rat && !rad);

            var available = new List<string>();
            if (poly) available.Add("poly");
            if (rat) available.Add("rat");
            if (rad) available.Add("rad");
            if (available.Count == 0) available.Add("poly");

            string chosen = available[(index + rng.Next(available.Count)) % available.Count];

            string formula = "";
            string ans = "";
            string steps = "";
            string graphFormula = "";

            if (chosen == "rat")
            {
                int a = rng.Next(1, 4);
                formula = $"f(x) = \\frac{{1}}{{x + {a}}}";
                graphFormula = $"1/(x+{a})";
                ans = $"f'(x) = -\\frac{{1}}{{(x + {a})^2}}";
                steps = $"$\\lim_{{h \\to 0}} \\frac{{\\frac{{1}}{{x + h + {a}}} - \\frac{{1}}{{x + {a}}}}}{{h}} " +
                        $"= \\lim_{{h \\to 0}} \\frac{{(x + {a}) - (x + h + {a})}}{{h(x + h + {a})(x + {a})}} " +
                        $"= \\lim_{{h \\to 0}} \\frac{{-h}}{{h(x + h + {a})(x + {a})}} = -\\frac{{1}}{{(x + {a})^2}}$";
            }
            else if (chosen == "rad")
            {
                int a = rng.Next(1, 4);
                formula = $"f(x) = \\sqrt{{x + {a}}}";
                graphFormula = $"sqrt(x+{a})";
                ans = $"f'(x) = \\frac{{1}}{{2\\sqrt{{x + {a}}}}}";
                steps = $"$\\lim_{{h \\to 0}} \\frac{{\\sqrt{{x + h + {a}}} - \\sqrt{{x + {a}}}}}{{h}} \\cdot \\frac{{\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}}}}{{\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}}}} " +
                        $"= \\lim_{{h \\to 0}} \\frac{{(x + h + {a}) - (x + {a})}}{{h(\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}})}} " +
                        $"= \\frac{{1}}{{2\\sqrt{{x + {a}}}}}$";
            }
            else
            {
                int arch = (index + rng.Next(4)) % 4;
                if (arch == 0)
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                    string bSign = b >= 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x";
                    formula = $"f(x) = {a}x^2 {bSign}";
                    graphFormula = $"{a}*x^2 + {b}*x";
                    int d1 = 2 * a;
                    string dSign = b >= 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                    ans = $"f'(x) = {d1}x {dSign}";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{[{a}(x+h)^2 + ({b})(x+h)] - [{a}x^2 + ({b})x]}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{{a}(x^2 + 2xh + h^2) + {b}x + {b}h - {a}x^2 - {b}x}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{{d1}xh + {a}h^2 + {b}h}}{{h}} = \\lim_{{h \\to 0}} ({d1}x + {a}h + {b}) = {d1}x {dSign}$";
                }
                else if (arch == 1)
                {
                    int a = rng.Next(1, 4);
                    int c = rng.Next(2, 6);
                    string aPart = a == 1 ? "-x^2" : $"-{a}x^2";
                    formula = $"f(x) = {aPart} + {c}";
                    graphFormula = $"-{a}*x^2 + {c}";
                    int d1 = -2 * a;
                    ans = $"f'(x) = {d1}x";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{[-{a}(x+h)^2 + {c}] - [-{a}x^2 + {c}]}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{-{a}(2xh + h^2)}}{{h}} = \\lim_{{h \\to 0}} ({d1}x - {a}h) = {d1}x$";
                }
                else if (arch == 2)
                {
                    int b = rng.Next(1, 5);
                    formula = $"f(x) = x^3 - {b}x";
                    graphFormula = $"x^3 - {b}*x";
                    ans = $"f'(x) = 3x^2 - {b}";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{[(x+h)^3 - {b}(x+h)] - [x^3 - {b}x]}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{3x^2h + 3xh^2 + h^3 - {b}h}}{{h}} = \\lim_{{h \\to 0}} (3x^2 + 3xh + h^2 - {b}) = 3x^2 - {b}$";
                }
                else
                {
                    int a = rng.Next(2, 5);
                    int c = rng.Next(1, 5);
                    formula = $"f(x) = {a}x^2 - {c}";
                    graphFormula = $"{a}*x^2 - {c}";
                    int d1 = 2 * a;
                    ans = $"f'(x) = {d1}x";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{[{a}(x+h)^2 - {c}] - [{a}x^2 - {c}]}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{{a}(2xh + h^2)}}{{h}} = \\lim_{{h \\to 0}} ({d1}x + {a}h) = {d1}x$";
                }
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                GraphFormula = graphFormula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 2x", "f'(x) = x - 1", "f'(x) = 0", rng);
            }

            return q;
        }

        public static Question GenerateInstantRateProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int mode = p?.ComplexityOptionIndex ?? 0;

            bool trig = HasTrig(p);
            bool rat = HasRational(p);
            bool rad = HasRadical(p);
            bool exp = HasExp(p);
            bool poly = HasPoly(p) || (!trig && !rat && !rad && !exp);

            var available = new List<string>();
            if (poly && trig)
            {
                available.Add("poly_trig");
                available.Add("poly_trig");
            }
            if (poly && rat) available.Add("poly_rat");
            if (poly && exp) available.Add("poly_exp");
            if (poly && rad) available.Add("poly_rad");

            if (available.Count == 0)
            {
                if (poly) available.Add("poly");
                if (trig) available.Add("trig");
                if (rat) available.Add("rat");
                if (rad) available.Add("rad");
                if (exp) available.Add("exp");
            }
            if (available.Count == 0) available.Add("poly");

            string chosen = available[(index + rng.Next(available.Count)) % available.Count];

            string formula = "";
            string ans = "";
            string steps = "";
            string graphFormula = "";

            if (chosen == "poly_trig")
            {
                int ptArch = (index + rng.Next(2)) % 4;
                if (ptArch == 0)
                {
                    formula = "f(x) = x^2 - 2\\cos(x);\\; x = 0";
                    graphFormula = "x^2 - 2*cos(x)";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = 2x + 2\\sin(x) \\implies f'(0) = 2(0) + 2(0) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = f'(0) = 0$"; }
                    else if (mode == 2) { ans = "y = -2"; steps = "$f(0) = -2,\\; m = 0 \\implies y = -2$"; }
                    else if (mode == 3) { ans = "x = 0"; steps = "$m = 0 \\implies$ recta normal vertical: $x = 0$"; }
                    else { ans = "(0, -2)"; steps = "$f'(x) = 2x + 2\\sin(x) = 0 \\implies x = 0,\\; f(0) = -2 \\implies (0, -2)$"; }
                }
                else if (ptArch == 1)
                {
                    formula = "f(x) = 3x + \\sin(x);\\; x = 0";
                    graphFormula = "3*x + sin(x)";
                    if (mode == 0) { ans = "4"; steps = "$f'(x) = 3 + \\cos(x) \\implies f'(0) = 3 + 1 = 4$"; }
                    else if (mode == 1) { ans = "m = 4"; steps = "Pendiente: $m = f'(0) = 4$"; }
                    else if (mode == 2) { ans = "y = 4x"; steps = "$f(0) = 0,\\; m = 4 \\implies y - 0 = 4(x - 0) \\implies y = 4x$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{4}x"; steps = "$m_{\\perp} = -\\frac{1}{4} \\implies y = -\\frac{1}{4}x$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = 3 + \\cos(x) \\ge 2 > 0$"; }
                }
                else if (ptArch == 2)
                {
                    formula = "f(x) = 2x - \\cos(2x);\\; x = 0";
                    graphFormula = "2*x - cos(2*x)";
                    if (mode == 0) { ans = "2"; steps = "$f'(x) = 2 + 2\\sin(2x) \\implies f'(0) = 2 + 0 = 2$"; }
                    else if (mode == 1) { ans = "m = 2"; steps = "Pendiente: $m = f'(0) = 2$"; }
                    else if (mode == 2) { ans = "y = 2x - 1"; steps = "$f(0) = -1,\\; m = 2 \\implies y - (-1) = 2(x - 0) \\implies y = 2x - 1$"; }
                    else if (mode == 3) { ans = "y + 1 = -\\frac{1}{2}x"; steps = "$m_{\\perp} = -\\frac{1}{2} \\implies y + 1 = -\\frac{1}{2}x$"; }
                    else { ans = "x = -\\frac{\\pi}{4} + k\\pi"; steps = "$f'(x) = 2 + 2\\sin(2x) = 0 \\implies \\sin(2x) = -1 \\implies x = -\\frac{\\pi}{4} + k\\pi$"; }
                }
                else
                {
                    formula = "f(x) = x^2 + 2\\sin(x);\\; x = 0";
                    graphFormula = "x^2 + 2*sin(x)";
                    if (mode == 0) { ans = "2"; steps = "$f'(x) = 2x + 2\\cos(x) \\implies f'(0) = 0 + 2(1) = 2$"; }
                    else if (mode == 1) { ans = "m = 2"; steps = "Pendiente: $m = f'(0) = 2$"; }
                    else if (mode == 2) { ans = "y = 2x"; steps = "$f(0) = 0,\\; m = 2 \\implies y = 2x$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{2}x"; steps = "$m_{\\perp} = -\\frac{1}{2} \\implies y = -\\frac{1}{2}x$"; }
                    else { ans = "(0, 0)"; steps = "Tangente horizontal en puntos críticos de $f'(x) = 0$"; }
                }
            }
            else if (chosen == "trig")
            {
                int tArch = (index + rng.Next(2)) % 2;
                if (tArch == 0)
                {
                    formula = "f(x) = 2\\sin(x);\\; x = 0";
                    graphFormula = "2*sin(x)";
                    if (mode == 0) { ans = "2"; steps = "$f'(x) = 2\\cos(x) \\implies f'(0) = 2(1) = 2$"; }
                    else if (mode == 1) { ans = "m = 2"; steps = "Pendiente: $m = f'(0) = 2\\cos(0) = 2$"; }
                    else if (mode == 2) { ans = "y = 2x"; steps = "$f(0) = 0,\\; f'(0) = 2 \\implies y - 0 = 2(x - 0) \\implies y = 2x$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{2}x"; steps = "$m_{\\perp} = -\\frac{1}{2} \\implies y - 0 = -\\frac{1}{2}(x - 0) \\implies y = -\\frac{1}{2}x$"; }
                    else { ans = "x = \\frac{\\pi}{2} + k\\pi"; steps = "$f'(x) = 2\\cos(x) = 0 \\implies x = \\frac{\\pi}{2} + k\\pi$"; }
                }
                else
                {
                    formula = "f(x) = \\cos(x);\\; x = \\pi";
                    graphFormula = "cos(x)";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = -\\sin(x) \\implies f'(\\pi) = -\\sin(\\pi) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = f'(\\pi) = -\\sin(\\pi) = 0$"; }
                    else if (mode == 2) { ans = "y = -1"; steps = "$f(\\pi) = -1,\\; m = 0 \\implies y = -1$"; }
                    else if (mode == 3) { ans = "x = \\pi"; steps = "$m = 0 \\implies$ la recta normal es vertical: $x = \\pi$"; }
                    else { ans = "x = k\\pi"; steps = "$f'(x) = -\\sin(x) = 0 \\implies x = k\\pi$"; }
                }
            }
            else if (chosen == "rat")
            {
                formula = "f(x) = \\frac{4}{x};\\; x = 2";
                graphFormula = "4/x";
                if (mode == 0) { ans = "-1"; steps = "$f'(x) = -\\frac{4}{x^2} \\implies f'(2) = -\\frac{4}{4} = -1$"; }
                else if (mode == 1) { ans = "m = -1"; steps = "Pendiente: $m = f'(2) = -1$"; }
                else if (mode == 2) { ans = "y = -x + 4"; steps = "$f(2) = 2,\\; m = -1 \\implies y - 2 = -1(x - 2) \\implies y = -x + 4$"; }
                else if (mode == 3) { ans = "y = x"; steps = "$m_{\\perp} = 1 \\implies y - 2 = 1(x - 2) \\implies y = x$"; }
                else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = -\\frac{4}{x^2} \\ne 0$ para todo $x \\ne 0$"; }
            }
            else if (chosen == "rad")
            {
                formula = "f(x) = \\sqrt{x};\\; x = 4";
                graphFormula = "sqrt(x)";
                if (mode == 0) { ans = "\\frac{1}{4}"; steps = "$f'(x) = \\frac{1}{2\\sqrt{x}} \\implies f'(4) = \\frac{1}{2(2)} = \\frac{1}{4}$"; }
                else if (mode == 1) { ans = "m = \\frac{1}{4}"; steps = "Pendiente: $m = f'(4) = \\frac{1}{4}$"; }
                else if (mode == 2) { ans = "y = \\frac{1}{4}x + 1"; steps = "$f(4) = 2,\\; m = \\frac{1}{4} \\implies y - 2 = \\frac{1}{4}(x - 4) \\implies y = \\frac{1}{4}x + 1$"; }
                else if (mode == 3) { ans = "y = -4x + 18"; steps = "$m_{\\perp} = -4 \\implies y - 2 = -4(x - 4) \\implies y = -4x + 18$"; }
                else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = \\frac{1}{2\\sqrt{x}} > 0$ en su dominio"; }
            }
            else
            {
                int c1 = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                int c2 = rng.Next(-3, 4);
                int c3 = rng.Next(-3, 4);
                int x0 = rng.Next(-2, 3);

                string c1Part = c1 == 1 ? "x^2" : (c1 == -1 ? "-x^2" : $"{c1}x^2");
                string c2Part = c2 == 0 ? "" : (c2 > 0 ? $" + {c2}x" : $" - {Math.Abs(c2)}x");
                string c3Part = c3 == 0 ? "" : (c3 > 0 ? $" + {c3}" : $" - {Math.Abs(c3)}");
                string polyStr = $"{c1Part}{c2Part}{c3Part}".Trim();
                graphFormula = polyStr;

                int y0 = c1 * x0 * x0 + c2 * x0 + c3;
                int m = 2 * c1 * x0 + c2;

                if (mode == 4)
                {
                    formula = $"f(x) = {polyStr}";
                    if (c2 % (2 * c1) == 0)
                    {
                        int xh = -c2 / (2 * c1);
                        int yh = c1 * xh * xh + c2 * xh + c3;
                        ans = $"({xh}, {yh})";
                        steps = $"$f'(x) = {2 * c1}x + ({c2}) = 0 \\implies x = {xh}$. $f({xh}) = {yh} \\implies ({xh}, {yh})$";
                    }
                    else
                    {
                        int num = -c2;
                        int den = 2 * c1;
                        if (den < 0) { num = -num; den = -den; }
                        int g = Gcd(Math.Abs(num), Math.Abs(den));
                        num /= g; den /= g;
                        ans = $"x = \\frac{{{num}}}{{{den}}}";
                        steps = $"$f'(x) = {2 * c1}x + ({c2}) = 0 \\implies x = \\frac{{{num}}}{{{den}}}$";
                    }
                }
                else
                {
                    formula = $"f(x) = {polyStr};\\; x = {x0}";
                    if (mode == 0)
                    {
                        ans = m.ToString();
                        steps = $"$f'(x) = {2 * c1}x + ({c2})$. Evaluando en $x = {x0}$: $f'({x0}) = {2 * c1}({x0}) + ({c2}) = {m}$";
                    }
                    else if (mode == 1)
                    {
                        ans = $"m = {m}";
                        steps = $"Pendiente: $m = f'({x0}) = {2 * c1}({x0}) + ({c2}) = {m}$";
                    }
                    else if (mode == 2)
                    {
                        int bLine = y0 - m * x0;
                        string bPart = bLine == 0 ? "" : (bLine > 0 ? $" + {bLine}" : $" - {Math.Abs(bLine)}");
                        string mPart = m == 1 ? "x" : (m == -1 ? "-x" : (m == 0 ? "" : $"{m}x"));
                        ans = mPart == "" ? $"y = {bLine}" : $"y = {mPart}{bPart}";
                        steps = $"$f({x0}) = {y0},\\; f'({x0}) = {m}$. Recta tangente: $y - ({y0}) = {m}(x - ({x0})) \\implies {ans}$";
                    }
                    else
                    {
                        if (m == 0)
                        {
                            ans = $"x = {x0}";
                            steps = $"Como la recta tangente es horizontal ($m = 0$), la recta normal es vertical: $x = {x0}$";
                        }
                        else
                        {
                            int num = -1;
                            int den = m;
                            if (den < 0) { num = 1; den = -den; }
                            ans = $"y - {y0} = \\frac{{{num}}}{{{den}}}(x - {x0})";
                            steps = $"$m = {m} \\implies m_{{\\perp}} = -\\frac{{1}}{{{m}}}$. Recta normal en $({x0}, {y0})$: $y - ({y0}) = -\\frac{{1}}{{{m}}}(x - ({x0}))$";
                        }
                    }
                }
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                GraphFormula = graphFormula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "-1", rng);
            }

            return q;
        }
        #endregion

        #region Applications of Differentiation with Composite Types
        public static Question GenerateApplicationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "app_tangent")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);

            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a passo",
                _ => "Step-by-step resolution"
            };

            // 1. Tangent lines
            if (topicId == "app_tangent")
            {
                if (poly && trig)
                {
                    int arch = (index + rng.Next(2)) % 2;
                    if (arch == 0)
                    {
                        // f(x) = c1*x^2 + c2*sin(x) at x = 0
                        int c1 = rng.Next(1, 4);
                        int c2 = rng.Next(2, 6);
                        string formula = $"y = {c1}x^2 + {c2}*sin(x)  at  x = 0";
                        string ans = $"y = {c2}x";
                        string steps = $"{stepLabel}: $y(0) = 0,\\; y' = {2 * c1}x + {c2}\\cos(x) \\Rightarrow m = y'(0) = {c2}.\\; \\text{{Ecuación: }} y - 0 = {c2}(x - 0) \\Rightarrow y = {c2}x$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y = {c1}x", $"y = {2 * c1}x + {c2}", $"y = -{c2}x", rng);
                        return q;
                    }
                    else
                    {
                        // f(x) = c1*x - c2*cos(x) at x = 0
                        int c1 = rng.Next(2, 5);
                        int c2 = rng.Next(1, 5);
                        string formula = $"y = {c1}x - {c2}*cos(x)  at  x = 0";
                        string ans = $"y = {c1}x - {c2}";
                        string steps = $"{stepLabel}: $y(0) = -{c2},\\; y' = {c1} + {c2}\\sin(x) \\Rightarrow m = y'(0) = {c1}.\\; y - (-{c2}) = {c1}(x - 0) \\Rightarrow y = {c1}x - {c2}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y = {c1}x + {c2}", $"y = {c2}x - {c1}", $"y = {c1}x", rng);
                        return q;
                    }
                }
                else if (poly && exp)
                {
                    int c1 = rng.Next(2, 5);
                    string formula = $"y = {c1}x + e^x  at  x = 0";
                    int m = c1 + 1;
                    string ans = $"y = {m}x + 1";
                    string steps = $"{stepLabel}: $y(0) = 1,\\; y' = {c1} + e^x \\Rightarrow m = y'(0) = {c1} + 1 = {m}.\\; y - 1 = {m}(x - 0) \\Rightarrow y = {m}x + 1$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {c1}x + 1", $"y = {m}x - 1", $"y = {c1 + 2}x + 1", rng);
                    return q;
                }
                else
                {
                    // Polynomial tangent line
                    int a = rng.Next(-2, 3);
                    int c1 = rng.Next(1, 4);
                    int c2 = rng.Next(-4, 5);
                    string formula = $"y = {c1}x^2 {(c2 >= 0 ? "+ " + c2 : "- " + Math.Abs(c2))}x  at  x = {a}";
                    int y0 = c1 * a * a + c2 * a;
                    int m = 2 * c1 * a + c2;
                    int b = y0 - m * a;
                    string ans = b == 0 ? $"y = {m}x" : (b > 0 ? $"y = {m}x + {b}" : $"y = {m}x - {Math.Abs(b)}");
                    string steps = $"{stepLabel}: $y({a}) = {y0},\\; y' = {2 * c1}x + {c2} \\Rightarrow m = y'({a}) = {m}.\\; y - ({y0}) = {m}(x - ({a})) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {m}x + {b + 2}", $"y = {m + 1}x + {b}", $"y = {2 * c1}x + {c2}", rng);
                    return q;
                }
            }

            // 2. Extrema / Critical Points
            if (topicId == "app_abs_extrema")
            {
                return GenerateAbsoluteExtremaProblem(p, index, type, rng);
            }
            if (topicId == "app_rel_extrema")
            {
                return GenerateRelativeExtremaProblem(p, index, type, rng);
            }
            if (topicId == "app_crit_pts")
            {
                return GenerateCriticalPointsProblem(p, index, type, rng);
            }

            // Default fallback
            return GenerateAbsoluteExtremaProblem(p, index, type, rng);
        }

        public static Question GenerateAbsoluteExtremaProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            bool includeQuad = p?.IncludeQuadratic == true;
            bool includeCubic = p?.IncludeCubic == true;
            bool includeQuartic = p?.IncludeQuartic == true;
            bool includeRational = p?.IncludeRational == true;
            bool includeExpEasy = p?.IncludeRationalExpEasy == true;
            bool includeExpHard = p?.IncludeRationalExpHard == true;
            bool includeTrig = p?.IncludeTrig == true;

            // If no function type checkbox is selected, default to quadratic
            if (!includeQuad && !includeCubic && !includeQuartic && !includeRational && !includeExpEasy && !includeExpHard && !includeTrig)
            {
                includeQuad = true;
            }

            var enabledTypes = new List<string>();
            if (includeQuad) enabledTypes.Add("quad");
            if (includeCubic) enabledTypes.Add("cubic");
            if (includeQuartic) enabledTypes.Add("quartic");
            if (includeRational) enabledTypes.Add("rational");
            if (includeExpEasy) enabledTypes.Add("expEasy");
            if (includeExpHard) enabledTypes.Add("expHard");
            if (includeTrig) enabledTypes.Add("trig");

            string chosenType = enabledTypes[index % enabledTypes.Count];

            // Notation: y or f(x)
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");

            // Complexity / Interval: 0=Closed, 1=Open, 2=HalfOpen, 3=Infinity, 4=Any
            int comp = p?.ComplexityOptionIndex ?? 0;

            string funcLatex = "";
            string intervalStr = "";
            string derivLatex = "";
            string critLatex = "";
            string evalStepsLatex = "";
            string maxCoord = "";
            string minCoord = "";

            if (chosenType == "quad")
            {
                int xv = rng.Next(-2, 3); // vertex x
                int a = (index % 2 == 0) ? 1 : -1;
                int k = rng.Next(-3, 4); // vertex y
                int d1 = rng.Next(1, 3);
                int d2 = rng.Next(1, 3);
                int L = xv - d1;
                int R = xv + d2;

                int B = -2 * a * xv;
                int C = a * xv * xv + k;
                string aStr = a == 1 ? "x^2" : a == -1 ? "-x^2" : $"{a}x^2";
                string bStr = B == 0 ? "" : (B > 0 ? (B == 1 ? "+ x" : $"+ {B}x") : (B == -1 ? "- x" : $"- {Math.Abs(B)}x"));
                string cStr = C == 0 ? "" : (C > 0 ? $"+ {C}" : $"- {Math.Abs(C)}");
                funcLatex = $"{aStr} {bStr} {cStr}".Trim();

                derivLatex = (B == 0) ? $"{2 * a}x" : (B > 0 ? $"{2 * a}x + {B}" : $"{2 * a}x - {Math.Abs(B)}");
                critLatex = $"x = {xv}";

                intervalStr = comp switch
                {
                    1 => $"({L}, {R})",
                    2 => (index % 2 == 0) ? $"[{L}, {R})" : $"({L}, {R}]",
                    3 => (a > 0) ? $"[{L}, \\infty)" : $"(-\\infty, {R}]",
                    _ => $"[{L}, {R}]"
                };

                int fL = a * d1 * d1 + k;
                int fR = a * d2 * d2 + k;
                int fV = k;
                evalStepsLatex = $"f({L}) = {fL},\\; f({xv}) = {fV},\\; f({R}) = {fR}";

                if (comp == 0 || comp == 4) // Closed interval
                {
                    if (a > 0)
                    {
                        minCoord = $"({xv}, {fV})";
                        maxCoord = (fL >= fR) ? (fL == fR ? $"({L}, {fL}),\\; ({R}, {fR})" : $"({L}, {fL})") : $"({R}, {fR})";
                    }
                    else
                    {
                        maxCoord = $"({xv}, {fV})";
                        minCoord = (fL <= fR) ? (fL == fR ? $"({L}, {fL}),\\; ({R}, {fR})" : $"({L}, {fL})") : $"({R}, {fR})";
                    }
                }
                else if (comp == 1) // Open interval
                {
                    if (a > 0)
                    {
                        minCoord = $"({xv}, {fV})";
                        maxCoord = LocalizationService.CurrentLanguage switch { AppLanguage.Spanish => "\\text{no existe}", AppLanguage.Portuguese => "\\text{não existe}", _ => "\\text{none}" };
                    }
                    else
                    {
                        maxCoord = $"({xv}, {fV})";
                        minCoord = LocalizationService.CurrentLanguage switch { AppLanguage.Spanish => "\\text{no existe}", AppLanguage.Portuguese => "\\text{não existe}", _ => "\\text{none}" };
                    }
                }
                else
                {
                    if (a > 0)
                    {
                        minCoord = $"({xv}, {fV})";
                        maxCoord = (fL >= fR) ? $"({L}, {fL})" : $"({R}, {fR})";
                    }
                    else
                    {
                        maxCoord = $"({xv}, {fV})";
                        minCoord = (fL <= fR) ? $"({L}, {fL})" : $"({R}, {fR})";
                    }
                }
            }
            else if (chosenType == "cubic")
            {
                int pattern = (index + rng.Next(4)) % 4;
                if (pattern == 0)
                {
                    funcLatex = "x^3 - 3x^2 + 1";
                    derivLatex = "3x^2 - 6x = 3x(x - 2)";
                    critLatex = "x = 0,\\; x = 2";
                    int L = -1, R = 3;
                    intervalStr = $"[{L}, {R}]";
                    evalStepsLatex = "f(-1) = -3,\\; f(0) = 1,\\; f(2) = -3,\\; f(3) = 1";
                    maxCoord = "(0, 1),\\; (3, 1)";
                    minCoord = "(-1, -3),\\; (2, -3)";
                }
                else if (pattern == 1)
                {
                    funcLatex = "-x^3 + 3x";
                    derivLatex = "-3x^2 + 3 = -3(x^2 - 1)";
                    critLatex = "x = -1,\\; x = 1";
                    int L = -2, R = 2;
                    intervalStr = $"[{L}, {R}]";
                    evalStepsLatex = "f(-2) = 2,\\; f(-1) = -2,\\; f(1) = 2,\\; f(2) = -2";
                    maxCoord = "(-2, 2),\\; (1, 2)";
                    minCoord = "(-1, -2),\\; (2, -2)";
                }
                else if (pattern == 2)
                {
                    funcLatex = "x^3 - 3x + 2";
                    derivLatex = "3x^2 - 3 = 3(x^2 - 1)";
                    critLatex = "x = 1";
                    int L = 0, R = 2;
                    intervalStr = $"[{L}, {R}]";
                    evalStepsLatex = "f(0) = 2,\\; f(1) = 0,\\; f(2) = 4";
                    maxCoord = "(2, 4)";
                    minCoord = "(1, 0)";
                }
                else
                {
                    funcLatex = "x^3 + 3x^2 - 9x + 2";
                    derivLatex = "3x^2 + 6x - 9 = 3(x + 3)(x - 1)";
                    critLatex = "x = 1";
                    int L = 0, R = 3;
                    intervalStr = $"[{L}, {R}]";
                    evalStepsLatex = "f(0) = 2,\\; f(1) = -3,\\; f(3) = 29";
                    maxCoord = "(3, 29)";
                    minCoord = "(1, -3)";
                }
            }
            else if (chosenType == "quartic")
            {
                funcLatex = "x^4 - 2x^2 + 2";
                derivLatex = "4x^3 - 4x = 4x(x - 1)(x + 1)";
                critLatex = "x = -1,\\; x = 0,\\; x = 1";
                int L = -2, R = 2;
                intervalStr = $"[{L}, {R}]";
                evalStepsLatex = "f(\\pm 2) = 10,\\; f(\\pm 1) = 1,\\; f(0) = 2";
                maxCoord = "(-2, 10),\\; (2, 10)";
                minCoord = "(-1, 1),\\; (1, 1)";
            }
            else if (chosenType == "rational")
            {
                funcLatex = "\\frac{2x}{x^2 + 1}";
                derivLatex = "\\frac{2(1 - x^2)}{(x^2 + 1)^2}";
                critLatex = "x = 1";
                int L = 0, R = 3;
                intervalStr = $"[{L}, {R}]";
                evalStepsLatex = "f(0) = 0,\\; f(1) = 1,\\; f(3) = \\frac{3}{5}";
                maxCoord = "(1, 1)";
                minCoord = "(0, 0)";
            }
            else if (chosenType == "trig")
            {
                funcLatex = "2\\sin(x)";
                derivLatex = "2\\cos(x)";
                critLatex = "x = \\frac{\\pi}{2}";
                intervalStr = "[0, \\pi]";
                evalStepsLatex = "f(0) = 0,\\; f(\\frac{\\pi}{2}) = 2,\\; f(\\pi) = 0";
                maxCoord = "(\\frac{\\pi}{2}, 2)";
                minCoord = "(0, 0),\\; (\\pi, 0)";
            }
            else // expEasy / expHard
            {
                funcLatex = "3x^{2/3} - 2x";
                derivLatex = "2x^{-1/3} - 2";
                critLatex = "x = 1,\\; x = 0";
                intervalStr = "[0, 8]";
                evalStepsLatex = "f(0) = 0,\\; f(1) = 1,\\; f(8) = -4";
                maxCoord = "(1, 1)";
                minCoord = "(8, -4)";
            }

            string formulaText = $"{lhs} = {funcLatex};\\; {intervalStr}";

            string ansText = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => $"Máximo absoluto: ${maxCoord}$; Mínimo absoluto: ${minCoord}$",
                AppLanguage.Portuguese => $"Máximo absoluto: ${maxCoord}$; Mínimo absoluto: ${minCoord}$",
                _ => $"Absolute max: ${maxCoord}$; Absolute min: ${minCoord}$"
            };

            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso:",
                AppLanguage.Portuguese => "Resolução passo a passo:",
                _ => "Step-by-step solution:"
            };

            string steps = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => $"{stepLabel}\n1) Derivada: $f'(x) = {derivLatex}$.\n2) Puntos críticos: $f'(x) = 0 \\Rightarrow {critLatex}$.\n3) Evaluar en extremos y puntos críticos: ${evalStepsLatex}$.\n4) Comparar valores: Máximo absoluto: ${maxCoord}$; Mínimo absoluto: ${minCoord}$.",
                AppLanguage.Portuguese => $"{stepLabel}\n1) Derivada: $f'(x) = {derivLatex}$.\n2) Pontos críticos: $f'(x) = 0 \\Rightarrow {critLatex}$.\n3) Avaliar nos extremos e pontos críticos: ${evalStepsLatex}$.\n4) Comparar valores: Máximo absoluto: ${maxCoord}$; Mínimo absoluto: ${minCoord}$.",
                _ => $"{stepLabel}\n1) Derivative: $f'(x) = {derivLatex}$.\n2) Critical points: $f'(x) = 0 \\Rightarrow {critLatex}$.\n3) Evaluate at endpoints and critical points: ${evalStepsLatex}$.\n4) Compare values: Absolute max: ${maxCoord}$; Absolute min: ${minCoord}$."
            };

            var q = new Question
            {
                ProblemStatement = formulaText,
                FormulaText = formulaText,
                Type = type,
                AnswerText = ansText,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                string dist1 = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Máximo absoluto: ${minCoord}$; Mínimo absoluto: ${maxCoord}$",
                    AppLanguage.Portuguese => $"Máximo absoluto: ${minCoord}$; Mínimo absoluto: ${maxCoord}$",
                    _ => $"Absolute max: ${minCoord}$; Absolute min: ${maxCoord}$"
                };
                string dist2 = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Extremos en ${critLatex}$",
                    AppLanguage.Portuguese => $"Extremos em ${critLatex}$",
                    _ => $"Extrema at ${critLatex}$"
                };
                string dist3 = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "No existen extremos absolutos",
                    AppLanguage.Portuguese => "Não existem extremos absolutos",
                    _ => "No absolute extrema exist"
                };
                q.Choices = DistractorHelper.BuildChoices(ansText, dist1, dist2, dist3, rng);
            }

            return q;
        }

        public static Question GenerateRelativeExtremaProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            bool quad = p?.IncludeQuadratic == true;
            bool cubic = p?.IncludeCubic == true;
            bool quartic = p?.IncludeQuartic == true;
            bool rat = p?.IncludeRational == true;
            bool rad = p?.IncludeRadical == true;
            bool trig = p?.IncludeTrig == true;

            if (!quad && !cubic && !quartic && !rat && !rad && !trig)
            {
                quad = true;
                cubic = true;
            }

            var pool = new List<Func<Question>>();

            if (quad)
            {
                pool.Add(() =>
                {
                    int a = (rng.Next(2) == 0 ? 1 : -1);
                    int xv = rng.Next(-3, 4);
                    int k = rng.Next(-4, 5);
                    int b = -2 * a * xv;
                    int c = a * xv * xv + k;
                    string aStr = a == 1 ? "x^2" : "-x^2";
                    string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                    string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                    string formula = $"{lhs} = {aStr} {bStr} {cStr}".Trim();

                    string extremumType = a > 0
                        ? (LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Mínimo" : "Min")
                        : (LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Máximo" : "Max");
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"{extremumType} relativo en $x = {xv}$",
                        AppLanguage.Portuguese => $"{extremumType} relativo em $x = {xv}$",
                        _ => $"Relative {extremumType.ToLower()} at $x = {xv}$"
                    };
                    string steps = $"$f'(x) = {2 * a}x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$. Como $f''(x) = {2 * a}$, {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Extremo relativo en $x = {xv + 1}$", $"Extremo relativo en $x = {-xv}$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            if (cubic)
            {
                pool.Add(() =>
                {
                    int r1 = rng.Next(-3, 0);
                    int r2 = rng.Next(1, 4);
                    int B = -3 * (r1 + r2);
                    int C = 3 * r1 * r2;
                    string bSign = B >= 0 ? $"+ {B}" : $"- {Math.Abs(B)}";
                    string cSign = C >= 0 ? $"+ {C}" : $"- {Math.Abs(C)}";
                    string bTerm = (B / 2 == 0) ? "" : (B / 2 > 0 ? $"+ {B / 2}x^2" : $"- {Math.Abs(B / 2)}x^2");
                    string cTerm = C == 0 ? "" : (C > 0 ? $"+ {C}x" : $"- {Math.Abs(C)}x");
                    string formula = $"{lhs} = x^3 {bTerm} {cTerm}".Trim();

                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Máximo relativo en $x = {r1}$, Mínimo relativo en $x = {r2}$",
                        AppLanguage.Portuguese => $"Máximo relativo em $x = {r1}$, Mínimo relativo em $x = {r2}$",
                        _ => $"Relative max at $x = {r1}$, Relative min at $x = {r2}$"
                    };
                    string r1Factor = r1 < 0 ? $"(x + {Math.Abs(r1)})" : $"(x - {r1})";
                    string r2Factor = r2 < 0 ? $"(x + {Math.Abs(r2)})" : $"(x - {r2})";
                    string steps = $"1) $f'(x) = 3x^2 {bSign}x {cSign} = 3{r1Factor}{r2Factor} = 0 \\Rightarrow x = {r1},\\; x = {r2}$.\n2) Criterio de la 1ª derivada: {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                    {
                        string d1 = LocalizationService.CurrentLanguage == AppLanguage.Spanish
                            ? $"Máximo relativo en $x = {r2}$, Mínimo relativo en $x = {r1}$"
                            : $"Relative max at $x = {r2}$, Relative min at $x = {r1}$";
                        q.Choices = DistractorHelper.BuildChoices(ans, d1, $"Extremos en $x = 0,\\; {r1}$", "No existen extremos relativos", rng);
                    }
                    return q;
                });
            }

            if (quartic)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 3);
                    int a2 = a * a;
                    string formula = $"{lhs} = x^4 - {2 * a2}x^2";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Máximo relativo en $x = 0$, Mínimos relativos en $x = -{a},\\; {a}$",
                        AppLanguage.Portuguese => $"Máximo relativo em $x = 0$, Mínimos relativos em $x = -{a},\\; {a}$",
                        _ => $"Relative max at $x = 0$, Relative mins at $x = -{a},\\; {a}$"
                    };
                    string steps = $"$f'(x) = 4x^3 - {4 * a2}x = 4x(x^2 - {a2}) = 0 \\Rightarrow x = -{a},\\; 0,\\; {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Mínimo en $x = 0$", $"Extremos en $x = \\pm {a2}$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            if (rat)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int a2 = a * a;
                    string formula = $"{lhs} = x + \\frac{{{a2}}}{{x}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Máximo relativo en $x = -{a}$, Mínimo relativo en $x = {a}$",
                        AppLanguage.Portuguese => $"Máximo relativo em $x = -{a}$, Mínimo relativo em $x = {a}$",
                        _ => $"Relative max at $x = -{a}$, Relative min at $x = {a}$"
                    };
                    string steps = $"$f'(x) = 1 - \\frac{{{a2}}}{{x^2}} = 0 \\Rightarrow x^2 = {a2} \\Rightarrow x = \\pm {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Mínimo en $x = -{a}$, Máximo en $x = {a}$", $"Extremo en $x = {a2}$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            if (trig)
            {
                pool.Add(() =>
                {
                    string formula = $"{lhs} = sin(x) + cos(x);\\; [0, 2\\pi]";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = \\frac{\\pi}{4}$, Mínimo relativo en $x = \\frac{5\\pi}{4}$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = \\frac{\\pi}{4}$, Mínimo relativo em $x = \\frac{5\\pi}{4}$",
                        _ => "Relative max at $x = \\frac{\\pi}{4}$, Relative min at $x = \\frac{5\\pi}{4}$"
                    };
                    string steps = "$f'(x) = \\cos(x) - \\sin(x) = 0 \\Rightarrow \\tan(x) = 1 \\Rightarrow x = \\frac{\\pi}{4},\\; \\frac{5\\pi}{4}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Extremos en $x = \\frac{\\pi}{2},\\; \\pi$", "Máximo en $x = \\pi$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateCriticalPointsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            bool quad = p?.IncludeQuadratic == true;
            bool cubic = p?.IncludeCubic == true;
            bool quartic = p?.IncludeQuartic == true;
            bool rat = p?.IncludeRational == true;
            bool trig = p?.IncludeTrig == true;

            if (!quad && !cubic && !quartic && !rat && !trig)
            {
                quad = true;
                cubic = true;
            }

            var pool = new List<Func<Question>>();

            if (quad)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int xv = rng.Next(-3, 4);
                    int b = -2 * a * xv;
                    string formula = $"{lhs} = {a}x^2 {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))}x";
                    string ans = $"x = {xv}";
                    string steps = $"$f'(x) = {2 * a}x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x = {xv + 1}", $"x = {-xv}", "x = 0", rng);
                    return q;
                });
            }

            if (cubic)
            {
                pool.Add(() =>
                {
                    int r1 = rng.Next(-3, 0);
                    int r2 = rng.Next(1, 4);
                    int B = -3 * (r1 + r2);
                    int C = 3 * r1 * r2;
                    string bSign = B >= 0 ? $"+ {B}" : $"- {Math.Abs(B)}";
                    string cSign = C >= 0 ? $"+ {C}" : $"- {Math.Abs(C)}";
                    string bTerm = (B / 2 == 0) ? "" : (B / 2 > 0 ? $"+ {B / 2}x^2" : $"- {Math.Abs(B / 2)}x^2");
                    string cTerm = C == 0 ? "" : (C > 0 ? $"+ {C}x" : $"- {Math.Abs(C)}x");
                    string formula = $"{lhs} = x^3 {bTerm} {cTerm}".Trim();
                    string ans = $"x = {r1},\\; x = {r2}";
                    string steps = $"$f'(x) = 3x^2 {bSign}x {cSign} = 0 \\Rightarrow x = {r1},\\; x = {r2}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x = {r1}", $"x = {r2}", "x = 0", rng);
                    return q;
                });
            }

            if (quartic)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int a2 = a * a;
                    string formula = $"{lhs} = x^4 - {2 * a2}x^2";
                    string ans = $"x = -{a},\\; x = 0,\\; x = {a}";
                    string steps = $"$f'(x) = 4x(x^2 - {a2}) = 0 \\Rightarrow x = -{a},\\; 0,\\; {a}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x = \\pm {a}", "x = 0", $"x = \\pm {a2}", rng);
                    return q;
                });
            }

            if (rat)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int a2 = a * a;
                    string formula = $"{lhs} = x + \\frac{{{a2}}}{{x}}";
                    string ans = $"x = -{a},\\; x = {a}";
                    string steps = $"$f'(x) = 1 - \\frac{{{a2}}}{{x^2}} = 0 \\Rightarrow x^2 = {a2} \\Rightarrow x = \\pm {a}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x = {a}", $"x = {a2}", "x = 0", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateImplicitProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? 0;
            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int c = rng.Next(1, 10);
                    string formula = $"x^2 + {a}y = {c}";
                    string ans = $"dy/dx = -2x / {a}";
                    string steps = $"$2x + {a}\\frac{{dy}}{{dx}} = 0 \\Rightarrow {a}\\frac{{dy}}{{dx}} = -2x \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{2x}}{{{a}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"dy/dx = 2x / {a}", $"dy/dx = -x / {a}", "dy/dx = -2x", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int r = rng.Next(2, 6);
                    string formula = $"x^2 + y^2 = {r * r}";
                    string ans = "dy/dx = -x / y";
                    string steps = $"$2x + 2y\\frac{{dy}}{{dx}} = 0 \\Rightarrow 2y\\frac{{dy}}{{dx}} = -2x \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{x}}{{y}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -y / x", "dy/dx = x / y", "dy/dx = -2x", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(5, 25);
                    string formula = $"x^3 + y^3 = {c}";
                    string ans = "dy/dx = -x^2 / y^2";
                    string steps = $"$3x^2 + 3y^2\\frac{{dy}}{{dx}} = 0 \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{x^2}}{{y^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -y^2 / x^2", "dy/dx = x^2 / y^2", "dy/dx = -3x^2", rng);
                    return q;
                });
            }
            else if (comp == 2)
            {
                pool.Add(() =>
                {
                    int c = rng.Next(2, 10);
                    string formula = $"x*y = {c}";
                    string ans = "dy/dx = -y / x";
                    string steps = $"$1\\cdot y + x\\frac{{dy}}{{dx}} = 0 \\Rightarrow x\\frac{{dy}}{{dx}} = -y \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{y}}{{x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -x / y", "dy/dx = y / x", "dy/dx = -1/x", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(5, 20);
                    string formula = $"x^2 + x*y + y^2 = {c}";
                    string ans = "dy/dx = -(2x + y) / (x + 2y)";
                    string steps = $"$2x + (y + x y') + 2y y' = 0 \\Rightarrow y'(x + 2y) = -(2x + y) \\Rightarrow y' = -\\frac{{2x + y}}{{x + 2y}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -(x + 2y) / (2x + y)", "dy/dx = -(2x) / (2y)", "dy/dx = -1", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    string formula = "sin(y) + x = y";
                    string ans = "dy/dx = 1 / (1 - cos(y))";
                    string steps = $"$\\cos(y)y' + 1 = y' \\Rightarrow 1 = y'(1 - \\cos(y)) \\Rightarrow y' = \\frac{{1}}{{1 - \\cos(y)}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = 1 / (cos(y) - 1)", "dy/dx = cos(y) - 1", "dy/dx = -1 / cos(y)", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateLHopitalProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            bool rat = HasRational(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);
            bool log = HasLog(p);
            if (!rat && !trig && !exp && !log)
                trig = true;

            int comp = p?.ComplexityOptionIndex ?? 0;
            var pool = new List<Func<Question>>();

            if (trig)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] sin({a}x) / ({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}\\cos({a}x)}}{{{b}}} = \\frac{{{a}(1)}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "Does not exist", rng);
                    return q;
                });

                if (comp >= 1)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(2, 5);
                        string formula = $"lim [x -> 0] (1 - cos({a}x)) / (x^2)";
                        int num = a * a;
                        int den = 2;
                        string ans = num % den == 0 ? (num / den).ToString() : $"{num}/{den}";
                        string steps = $"Forma 0/0. L'Hôpital (1): $\\lim \\frac{{{a}\\sin({a}x)}}{{2x}}$. L'Hôpital (2): $\\lim \\frac{{{a * a}\\cos({a}x)}}{{2}} = {ans}$.";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "0", a.ToString(), "1", rng);
                        return q;
                    });
                }
            }

            if (exp)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> 0] (e^({a}x) - 1) / (x)";
                    string ans = a.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}e^{{{a}x}}}}{{1}} = {a}e^0 = {a}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (a * 2).ToString(), rng);
                    return q;
                });
            }

            if (log)
            {
                pool.Add(() =>
                {
                    string formula = "lim [x -> 1] ln(x) / (x - 1)";
                    string ans = "1";
                    string steps = "Forma 0/0. L'Hôpital: $\\lim_{{x \\to 1}} \\frac{{1/x}}{{1}} = 1$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "Does not exist", "2", rng);
                    return q;
                });
            }

            if (rat || pool.Count == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> {a}] (x^2 - {a * a}) / (x - {a})";
                    int ansVal = 2 * a;
                    string ans = ansVal.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to {a}}} \\frac{{2x}}{{1}} = 2({a}) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, a.ToString(), (ansVal + 2).ToString(), "0", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateConcavityProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            bool quartic = p?.IncludeQuartic == true;
            bool cubic = p?.IncludeCubic == true || !quartic;

            var pool = new List<Func<Question>>();

            if (quartic)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 9);
                    int a2 = a * a;
                    int c = rng.Next(-10, 11);
                    string cStr = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                    string formula = $"{lhs} = x^4 - {6 * a2}x^2{cStr}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Cóncava hacia arriba: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Cóncava hacia abajo: $(-{a}, {a})$; Puntos de inflexión en $x = \\pm {a}$",
                        AppLanguage.Portuguese => $"Côncava para cima: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Côncava para baixo: $(-{a}, {a})$; Pontos de inflexão em $x = \\pm {a}$",
                        _ => $"Concave up: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Concave down: $(-{a}, {a})$; Inflection points at $x = \\pm {a}$"
                    };
                    string steps = $"$f'(x) = 4x^3 - {12 * a2}x \\Rightarrow f''(x) = 12x^2 - {12 * a2} = 12(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia arriba en todo $\\mathbb{R}$", $"Punto de inflexión en $x = 0$", $"Cóncava hacia abajo: $(-\\infty, {a})$", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 9);
                    int a2 = a * a;
                    string formula = $"{lhs} = -x^4 + {6 * a2}x^2";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Cóncava hacia arriba: $(-{a}, {a})$; Cóncava hacia abajo: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Puntos de inflexión en $x = \\pm {a}$",
                        AppLanguage.Portuguese => $"Côncava para cima: $(-{a}, {a})$; Côncava para baixo: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Pontos de inflexão em $x = \\pm {a}$",
                        _ => $"Concave up: $(-{a}, {a})$; Concave down: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Inflection points at $x = \\pm {a}$"
                    };
                    string steps = $"$f'(x) = -4x^3 + {12 * a2}x \\Rightarrow f''(x) = -12x^2 + {12 * a2} = -12(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia abajo en todo $\\mathbb{R}$", $"Puntos de inflexión en $x = 0$", $"Cóncava hacia arriba: $(-\\infty, {a})$", rng);
                    return q;
                });
            }

            if (cubic || pool.Count == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(-8, 9);
                    if (a == 0) a = 2;
                    int c = rng.Next(-10, 11);
                    string aPart = a > 0 ? $"- {3 * a}x^2" : $"+ {3 * Math.Abs(a)}x^2";
                    string cPart = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                    string formula = $"{lhs} = x^3 {aPart}{cPart}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Cóncava hacia arriba: $({a}, \\infty)$; Cóncava hacia abajo: $(-\\infty, {a})$; Punto de inflexión en $x = {a}$",
                        AppLanguage.Portuguese => $"Côncava para cima: $({a}, \\infty)$; Côncava para baixo: $(-\\infty, {a})$; Ponto de inflexão em $x = {a}$",
                        _ => $"Concave up: $({a}, \\infty)$; Concave down: $(-\\infty, {a})$; Inflection point at $x = {a}$"
                    };
                    string steps = $"$f'(x) = 3x^2 - {6 * a}x \\Rightarrow f''(x) = 6x - {6 * a} = 6(x - {a}) = 0 \\Rightarrow x = {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia arriba en todo $\\mathbb{R}$", "Punto de inflexión en $x = 0$", $"Cóncava hacia abajo: $({a}, \\infty)$", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(-8, 9);
                    if (a == 0) a = 1;
                    string aPart = a > 0 ? $"+ {3 * a}x^2" : $"- {3 * Math.Abs(a)}x^2";
                    string formula = $"{lhs} = -x^3 {aPart}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Cóncava hacia arriba: $(-\\infty, {a})$; Cóncava hacia abajo: $({a}, \\infty)$; Punto de inflexión en $x = {a}$",
                        AppLanguage.Portuguese => $"Côncava para cima: $(-\\infty, {a})$; Côncava para baixo: $({a}, \\infty)$; Ponto de inflexão em $x = {a}$",
                        _ => $"Concave up: $(-\\infty, {a})$; Concave down: $({a}, \\infty)$; Inflection point at $x = {a}$"
                    };
                    string steps = $"$f'(x) = -3x^2 + {6 * a}x \\Rightarrow f''(x) = -6x + {6 * a} = -6(x - {a}) = 0 \\Rightarrow x = {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia abajo en todo $\\mathbb{R}$", "Punto de inflexión en $x = 0$", $"Cóncava hacia arriba: $({a}, \\infty)$", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateIncreaseDecreaseProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            var pool = new List<Func<Question>>();

            // 1. Quadratic opening up
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                int b = rng.Next(-8, 9);
                int c = rng.Next(-10, 11);
                string lead = a == 1 ? "x^2" : $"{a}x^2";
                int bCoeff = -2 * a * b;
                string bPart = bCoeff == 0 ? "" : (bCoeff > 0 ? $" + {bCoeff}x" : $" - {Math.Abs(bCoeff)}x");
                string cPart = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                string formula = $"{lhs} = {lead}{bPart}{cPart}".Trim();
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $({b}, \\infty)$; Decreciente: $(-\\infty, {b})$",
                    AppLanguage.Portuguese => $"Crescente: $({b}, \\infty)$; Decrescente: $(-\\infty, {b})$",
                    _ => $"Increasing: $({b}, \\infty)$; Decreasing: $(-\\infty, {b})$"
                };
                string steps = $"$f'(x) = {2 * a}x {(bCoeff >= 0 ? "+ " + bCoeff : "- " + Math.Abs(bCoeff))} = {2 * a}(x - {b}) = 0 \\Rightarrow x = {b}$. Para $x > {b},\\; f'(x) > 0$. Para $x < {b},\\; f'(x) < 0$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-\\infty, {b})$; Decreciente: $({b}, \\infty)$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 2. Quadratic opening down
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                int b = rng.Next(-8, 9);
                string lead = a == 1 ? "-x^2" : $"-{a}x^2";
                int bCoeff = 2 * a * b;
                string bPart = bCoeff == 0 ? "" : (bCoeff > 0 ? $" + {bCoeff}x" : $" - {Math.Abs(bCoeff)}x");
                string formula = $"{lhs} = {lead}{bPart}".Trim();
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-\\infty, {b})$; Decreciente: $({b}, \\infty)$",
                    AppLanguage.Portuguese => $"Crescente: $(-\\infty, {b})$; Decrescente: $({b}, \\infty)$",
                    _ => $"Increasing: $(-\\infty, {b})$; Decreasing: $({b}, \\infty)$"
                };
                string steps = $"$f'(x) = -{2 * a}x {(bCoeff >= 0 ? "+ " + bCoeff : "- " + Math.Abs(bCoeff))} = -{2 * a}(x - {b}) = 0 \\Rightarrow x = {b}$. Para $x < {b},\\; f'(x) > 0$. Para $x > {b},\\; f'(x) < 0$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $({b}, \\infty)$; Decreciente: $(-\\infty, {b})$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 3. Cubic
            pool.Add(() =>
            {
                int a = rng.Next(1, 8);
                int c = rng.Next(-10, 11);
                string cPart = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                string formula = $"{lhs} = x^3 - {3 * a * a}x{cPart}";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreciente: $(-{a}, {a})$",
                    AppLanguage.Portuguese => $"Crescente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decrescente: $(-{a}, {a})$",
                    _ => $"Increasing: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreasing: $(-{a}, {a})$"
                };
                string steps = $"$f'(x) = 3x^2 - {3 * a * a} = 3(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. Criterio de la 1ª derivada: {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-{a}, {a})$; Decreciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 4. Inverted cubic
            pool.Add(() =>
            {
                int a = rng.Next(1, 8);
                string formula = $"{lhs} = -x^3 + {3 * a * a}x";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-{a}, {a})$; Decreciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$",
                    AppLanguage.Portuguese => $"Crescente: $(-{a}, {a})$; Decrescente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$",
                    _ => $"Increasing: $(-{a}, {a})$; Decreasing: $(-\\infty, -{a}) \\cup ({a}, \\infty)$"
                };
                string steps = $"$f'(x) = -3x^2 + {3 * a * a} = -3(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. Criterio de la 1ª derivada: {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreciente: $(-{a}, {a})$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateMvtProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            var pool = new List<Func<Question>>();

            // 1. Quadratic x^2 on [a, b]
            pool.Add(() =>
            {
                int a = rng.Next(-6, 6);
                int span = rng.Next(2, 9);
                int b = a + span;
                string formula = $"{lhs} = x^2;\\; [{a}, {b}]";
                double cVal = (a + b) / 2.0;
                string ans = $"c = {cVal}";
                string steps = $"$f'(c) = \\frac{{f({b}) - f({a})}}{{{b} - ({a})}} = \\frac{{{b * b} - ({a * a})}}{{{b - a}}} = {a + b}$. Luego $2c = {a + b} \\Rightarrow c = {cVal} \\in ({a}, {b})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {a + 1}", $"c = {b - 1}", $"c = {(a + b)}", rng);
                return q;
            });

            // 2. Scaled quadratic k*x^2 + d*x on [a, b]
            pool.Add(() =>
            {
                int k = rng.Next(2, 5);
                int d = rng.Next(1, 5);
                int a = rng.Next(0, 5);
                int b = a + rng.Next(2, 6);
                string formula = $"{lhs} = {k}x^2 + {d}x;\\; [{a}, {b}]";
                double cVal = (a + b) / 2.0;
                string ans = $"c = {cVal}";
                string steps = $"$f'(c) = 2({k})c + {d} = {2 * k}c + {d}$. Tasa promedio $= {k}({a} + {b}) + {d}$. Igualando: $2kc = k({a} + {b}) \\Rightarrow c = \\frac{{{a} + {b}}}{{2}} = {cVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {cVal + 1}", $"c = {a}", $"c = {b}", rng);
                return q;
            });

            // 3. Reciprocal 1/x on [a, b] with a*b a perfect square
            pool.Add(() =>
            {
                var pairs = new (int a, int b, int c)[]
                {
                    (1, 4, 2), (1, 9, 3), (1, 16, 4), (1, 25, 5), (1, 36, 6),
                    (2, 8, 4), (2, 18, 6), (3, 12, 6), (3, 27, 9), (4, 9, 6),
                    (4, 16, 8), (4, 25, 10), (9, 16, 12)
                };
                var item = pairs[rng.Next(pairs.Length)];
                string formula = $"{lhs} = \\frac{{1}}{{x}};\\; [{item.a}, {item.b}]";
                string ans = $"c = {item.c}";
                string steps = $"$f'(c) = -\\frac{{1}}{{c^2}} = \\frac{{1/{item.b} - 1/{item.a}}}{{{item.b} - {item.a}}} = -\\frac{{1}}{{{item.a * item.b}}} \\Rightarrow c^2 = {item.a * item.b} \\Rightarrow c = {item.c} \\in ({item.a}, {item.b})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {item.c + 1}", $"c = {item.a}", $"c = \\sqrt{{{item.b}}}", rng);
                return q;
            });

            // 4. Square root sqrt(x) on [0, b] where b is square
            pool.Add(() =>
            {
                int[] bVals = { 4, 9, 16, 25, 36, 49, 64 };
                int b = bVals[rng.Next(bVals.Length)];
                string formula = $"{lhs} = \\sqrt{{x}};\\; [0, {b}]";
                double cVal = b / 4.0;
                string ans = $"c = {cVal}";
                string steps = $"$f'(c) = \\frac{{1}}{{2\\sqrt{{c}}}} = \\frac{{\\sqrt{{{b}}} - 0}}{{{b} - 0}} = \\frac{{1}}{{\\sqrt{{{b}}}}} \\Rightarrow 2\\sqrt{{c}} = \\sqrt{{{b}}} \\Rightarrow c = \\frac{{{b}}}{{4}} = {cVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {b / 2.0}", $"c = 1", $"c = {Math.Sqrt(b)}", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateRolleProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            var pool = new List<Func<Question>>();

            // 1. Centered quadratic x^2 - k^2 on [-k, k]
            pool.Add(() =>
            {
                int k = rng.Next(2, 16);
                string formula = $"{lhs} = x^2 - {k * k};\\; [{-k}, {k}]";
                string ans = "c = 0";
                string steps = $"$f(-{k}) = f({k}) = 0$. Función continua en $[{-k}, {k}]$ y diferenciable en $({-k}, {k})$. $f'(c) = 2c = 0 \\Rightarrow c = 0 \\in ({-k}, {k})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {k / 2.0}", $"c = {k}", "El teorema no aplica", rng);
                return q;
            });

            // 2. Shifted quadratic x^2 - 2ax on [0, 2a]
            pool.Add(() =>
            {
                int a = rng.Next(1, 16);
                int twoA = 2 * a;
                string formula = $"{lhs} = x^2 - {twoA}x;\\; [0, {twoA}]";
                string ans = $"c = {a}";
                string steps = $"$f(0) = f({twoA}) = 0$. $f'(c) = 2c - {twoA} = 0 \\Rightarrow c = {a} \\in (0, {twoA})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 0", $"c = {twoA}", $"c = {a / 2.0}", rng);
                return q;
            });

            // 3. Inverted quadratic -x^2 + 2ax on [0, 2a]
            pool.Add(() =>
            {
                int a = rng.Next(1, 16);
                int twoA = 2 * a;
                string formula = $"{lhs} = -x^2 + {twoA}x;\\; [0, {twoA}]";
                string ans = $"c = {a}";
                string steps = $"$f(0) = f({twoA}) = 0$. $f'(c) = -2c + {twoA} = 0 \\Rightarrow c = {a} \\in (0, {twoA})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 0", $"c = {twoA}", $"c = {a * 2}", rng);
                return q;
            });

            // 4. Factored quadratic (x - a)(x - b) on [a, b]
            pool.Add(() =>
            {
                int a = rng.Next(-5, 6);
                int span = rng.Next(1, 8) * 2;
                int b = a + span;
                int sum = a + b;
                int prod = a * b;
                string bPart = sum == 0 ? "" : (sum > 0 ? $" - {sum}x" : $" + {Math.Abs(sum)}x");
                string cPart = prod == 0 ? "" : (prod > 0 ? $" + {prod}" : $" - {Math.Abs(prod)}");
                string formula = $"{lhs} = x^2{bPart}{cPart};\\; [{a}, {b}]";
                double cVal = (a + b) / 2.0;
                string ans = $"c = {cVal}";
                string steps = $"$f({a}) = f({b}) = 0$. $f'(c) = 2c - ({sum}) = 0 \\Rightarrow c = \\frac{{{sum}}}{{2}} = {cVal} \\in ({a}, {b})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {a}", $"c = {b}", "c = 0", rng);
                return q;
            });

            // 5. Trig function sin(k*x) on [0, pi/k]
            pool.Add(() =>
            {
                int k = rng.Next(1, 7);
                string bStr = k == 1 ? "\\pi" : $"\\frac{{\\pi}}{{{k}}}";
                string formula = $"{lhs} = \\sin({(k == 1 ? "x" : $"{k}x")});\\; [0, {bStr}]";
                int den = 2 * k;
                string ans = $"c = \\frac{{\\pi}}{{{den}}}";
                string steps = $"$f(0) = \\sin(0) = 0,\\; f({bStr}) = \\sin(\\pi) = 0$. $f'(c) = {k}\\cos({k}c) = 0 \\Rightarrow {k}c = \\frac{{\\pi}}{{2}} \\Rightarrow c = \\frac{{\\pi}}{{{den}}} \\in (0, {bStr})$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 0", $"c = {bStr}", $"c = \\frac{{\\pi}}{{{k}}}", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateOptimizationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            var pool = new List<Func<Question>>();

            // 1. Rectangle with 4-side perimeter
            pool.Add(() =>
            {
                int pPerim = rng.Next(10, 45) * 4;
                int side = pPerim / 4;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Halle las dimensiones de un rectángulo de perímetro {pPerim} m con área máxima.",
                    AppLanguage.Portuguese => $"Encontre as dimensões de um retângulo de perímetro {pPerim} m com área máxima.",
                    _ => $"Find the dimensions of a rectangle with perimeter {pPerim} m that maximizes area."
                };
                string ans = $"{side} m x {side} m (Max Area = {side * side} m^2)";
                string steps = $"$P = 2x + 2y = {pPerim} \\Rightarrow y = {pPerim / 2} - x$. $A(x) = x({pPerim / 2} - x) \\Rightarrow A'(x) = {pPerim / 2} - 2x = 0 \\Rightarrow x = {side}, y = {side}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{side - 2} m x {side + 2} m", $"{pPerim / 2} m x 1 m", $"{side / 2} m x {side * 2} m", rng);
                return q;
            });

            // 2. Fence along river (3 sides, 2x + y = L)
            pool.Add(() =>
            {
                int L = rng.Next(10, 35) * 4;
                int xVal = L / 4;
                int yVal = L / 2;
                int maxArea = xVal * yVal;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Se cercará un campo rectangular junto a un río recto (no requiere cerca junto al río). Con {L} m de cerca, ¿cuáles son las dimensiones de área máxima?",
                    AppLanguage.Portuguese => $"Um campo retangular será cercado ao longo de um rio reto (sem cerca no rio). Com {L} m de cerca, quais são as dimensões de área máxima?",
                    _ => $"A rectangular field along a straight river needs fencing on 3 sides (no fence along the river). With {L} m of fence, find dimensions that maximize area."
                };
                string ans = $"{xVal} m x {yVal} m (Max Area = {maxArea} m^2)";
                string steps = $"$2x + y = {L} \\Rightarrow y = {L} - 2x$. $A(x) = x({L} - 2x) = {L}x - 2x^2$. $A'(x) = {L} - 4x = 0 \\Rightarrow x = {xVal}, y = {yVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{xVal + 2} m x {yVal - 4} m", $"{L / 3} m x {L / 3} m", $"{L / 2} m x {L / 4} m", rng);
                return q;
            });

            // 3. Two positive numbers whose sum is S with max product
            pool.Add(() =>
            {
                int S = rng.Next(6, 40) * 2;
                int half = S / 2;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Halle dos números positivos cuya suma sea {S} y cuyo producto sea máximo.",
                    AppLanguage.Portuguese => $"Encontre dois números positivos cuja soma seja {S} e cujo produto seja máximo.",
                    _ => $"Find two positive numbers whose sum is {S} and whose product is maximum."
                };
                string ans = $"{half} y {half} (Producto = {half * half})";
                string steps = $"$x + y = {S} \\Rightarrow y = {S} - x$. $P(x) = x({S} - x) = {S}x - x^2$. $P'(x) = {S} - 2x = 0 \\Rightarrow x = {half}, y = {half}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{half - 2} y {half + 2}", $"1 y {S - 1}", $"{half / 2} y {half * 2}", rng);
                return q;
            });

            // 4. Open-top box folded from square sheet of side S
            pool.Add(() =>
            {
                int[] sVals = { 12, 18, 24, 30, 36, 42, 48 };
                int S = sVals[rng.Next(sVals.Length)];
                int cut = S / 6;
                int baseSide = S - 2 * cut;
                int maxVol = cut * baseSide * baseSide;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"De una hoja cuadrada de {S} cm de lado se cortan cuadrados iguales de lado x en las esquinas para formar una caja abierta. Halle x para volumen máximo.",
                    AppLanguage.Portuguese => $"De uma folha quadrada de {S} cm de lado cortam-se quadrados iguais de lado x nos cantos para formar uma caixa aberta. Encontre x para volume máximo.",
                    _ => $"An open box is formed by cutting equal squares of side x from the corners of a {S} cm square sheet. Find x for maximum volume."
                };
                string ans = $"x = {cut} cm (Volumen = {maxVol} cm^3)";
                string steps = $"$V(x) = x({S} - 2x)^2 = 4x^3 - {4 * S}x^2 + {S * S}x$. $V'(x) = 12x^2 - {8 * S}x + {S * S} = (2x - {S})(6x - {S}) = 0$. Como $x < {S / 2}$, $x = \\frac{{{S}}}{{6}} = {cut} \\text{{ cm}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"x = {cut + 1} cm", $"x = {S / 4} cm", $"x = {cut * 2} cm", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }
        #endregion

        #region Integration with Composite Types
        public static Question GenerateIntegrationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "indef_power")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);
            bool log = HasLog(p);
            bool rat = HasRational(p);
            bool rad = HasRadical(p);

            if (!poly && !trig && !exp && !log && !rat && !rad)
                poly = true;

            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));

            string intLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Regla de integración",
                AppLanguage.Portuguese => "Regra de integração",
                _ => "Integration rule"
            };

            var pool = new List<Func<Question>>();

            // 1. Integration by Parts (indef_parts)
            if (topicId == "indef_parts")
            {
                if (trig)
                {
                    // 1.1 x * cos(k*x)
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 6);
                        int k = rng.Next(1, 5);
                        string aStr = a == 1 ? "" : $"{a}";
                        string kStr = k == 1 ? "x" : $"{k}x";
                        string formula = $"int {aStr}x*cos({kStr}) dx";
                        int k2 = k * k;
                        string sinTerm = k == 1 ? (a == 1 ? "x*sin(x)" : $"{a}x*sin(x)") : (a % k == 0 ? $"{a / k}x*sin({k}x)" : $"\\frac{{{a}x}}{{{k}}}*sin({k}x)");
                        string cosTerm = (a % k2 == 0) ? $"{a / k2}*cos({kStr})" : $"\\frac{{{a}}}{{{k2}}}*cos({kStr})";
                        string ans = $"{sinTerm} + {cosTerm} + C";
                        string steps = $"{intLabel} (Partes): $u = {aStr}x,\\; dv = \\cos({kStr})dx \\implies uv - \\int v du = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{sinTerm} - {cosTerm} + C", $"{aStr}x*cos({kStr}) + sin({kStr}) + C", $"-{sinTerm} + C", rng);
                        return q;
                    });

                    // 1.2 x * sin(k*x)
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 6);
                        int k = rng.Next(1, 5);
                        string aStr = a == 1 ? "" : $"{a}";
                        string kStr = k == 1 ? "x" : $"{k}x";
                        string formula = $"int {aStr}x*sin({kStr}) dx";
                        int k2 = k * k;
                        string cosTerm = k == 1 ? (a == 1 ? "-x*cos(x)" : $"-{a}x*cos(x)") : (a % k == 0 ? $"-{a / k}x*cos({k}x)" : $"-\\frac{{{a}x}}{{{k}}}*cos({k}x)");
                        string sinTerm = (a % k2 == 0) ? $"{a / k2}*sin({kStr})" : $"\\frac{{{a}}}{{{k2}}}*sin({kStr})";
                        string ans = $"{cosTerm} + {sinTerm} + C";
                        string steps = $"{intLabel} (Partes): $u = {aStr}x,\\; dv = \\sin({kStr})dx \\implies uv - \\int v du = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{cosTerm} - {sinTerm} + C", $"{aStr}x*sin({kStr}) + C", $"-{cosTerm} + C", rng);
                        return q;
                    });
                }

                if (exp)
                {
                    // 1.3 x * e^(k*x)
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 6);
                        int k = rng.Next(1, 5);
                        string aStr = a == 1 ? "" : $"{a}";
                        string expPart = k == 1 ? "e^x" : $"e^{{{k}x}}";
                        string formula = $"int {aStr}x*{expPart} dx";
                        int k2 = k * k;
                        string xTerm = (a % k == 0) ? $"{a / k}x*{expPart}" : $"\\frac{{{a}}}{{{k}}}x*{expPart}";
                        string constTerm = (a % k2 == 0) ? $"{a / k2}*{expPart}" : $"\\frac{{{a}}}{{{k2}}}*{expPart}";
                        string ans = $"{xTerm} - {constTerm} + C";
                        string steps = $"{intLabel} (Partes): $u = {aStr}x,\\; dv = {expPart}dx \\implies {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{xTerm} + {constTerm} + C", $"{aStr}x*{expPart} + C", $"\\frac{{{aStr}x^2}}{{2}}*{expPart} + C", rng);
                        return q;
                    });
                }

                if (log || pool.Count == 0)
                {
                    // 1.4 x^n * ln(x)
                    pool.Add(() =>
                    {
                        int n = rng.Next(1, 6);
                        int n1 = n + 1;
                        int n1Sq = n1 * n1;
                        string xPow = n == 1 ? "x" : $"x^{{{n}}}";
                        string formula = $"int {xPow}*ln(x) dx";
                        string ans = $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) - \\frac{{1}}{{{n1Sq}}}x^{{{n1}}} + C";
                        string steps = $"{intLabel} (Partes): $u = \\ln(x),\\; dv = {xPow}dx \\implies \\frac{{x^{{{n1}}}}}{{{n1}}}\\ln(x) - \\int \\frac{{x^{{{n1}}}}}{{{n1}}}\\cdot\\frac{{1}}{{x}}dx = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) + \\frac{{1}}{{{n1Sq}}}x^{{{n1}}} + C", $"x^{{{n1}}}*ln(x) - x^{{{n1}}} + C", $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) + C", rng);
                        return q;
                    });

                    // 1.5 ln(k*x)
                    pool.Add(() =>
                    {
                        int k = rng.Next(1, 9);
                        string kStr = k == 1 ? "x" : $"{k}x";
                        string formula = $"int ln({kStr}) dx";
                        string ans = $"x*ln({kStr}) - x + C";
                        string steps = $"{intLabel} (Partes): $u = \\ln({kStr}),\\; dv = dx \\implies x\\ln({kStr}) - \\int x\\cdot\\frac{{1}}{{x}}dx = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"x*ln({kStr}) + x + C", $"\\frac{{1}}{{{kStr}}} + C", $"x*ln({kStr}) + C", rng);
                        return q;
                    });
                }
            }
            else
            {
                // Standard Indefinite Integration (indef_power and others)
                if (trig)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 9);
                        string formula = $"int cos({k}x) dx";
                        string ans = $"1/{k} * sin({k}x) + C";
                        string steps = $"{intLabel}: $\\int \\cos({k}x)\\,dx = \\frac{{1}}{{{k}}}\\sin({k}x) + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"-1/{k} * sin({k}x) + C", $"{k} * sin({k}x) + C", $"1/{k} * cos({k}x) + C", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 9);
                        string formula = $"int sin({k}x) dx";
                        string ans = $"-1/{k} * cos({k}x) + C";
                        string steps = $"{intLabel}: $\\int \\sin({k}x)\\,dx = -\\frac{{1}}{{{k}}}\\cos({k}x) + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"1/{k} * cos({k}x) + C", $"{k} * cos({k}x) + C", $"-sin({k}x) + C", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 8);
                        string formula = $"int sec^2({k}x) dx";
                        string ans = $"1/{k} * tan({k}x) + C";
                        string steps = $"{intLabel}: $\\int \\sec^2({k}x)\\,dx = \\frac{{1}}{{{k}}}\\tan({k}x) + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"-1/{k} * tan({k}x) + C", $"{k} * tan({k}x) + C", $"1/{k} * sec({k}x) + C", rng);
                        return q;
                    });
                }

                if (exp)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 9);
                        string formula = $"int e^({k}x) dx";
                        string ans = $"1/{k} * e^({k}x) + C";
                        string steps = $"{intLabel}: $\\int e^{{{k}x}}\\,dx = \\frac{{1}}{{{k}}}e^{{{k}x}} + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{k} * e^({k}x) + C", $"e^({k}x) + C", $"1/{k} * e^x + C", rng);
                        return q;
                    });
                }

                if (rat || log)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(2, 12);
                        string formula = $"int {a}/x dx";
                        string ans = $"{a}*ln|x| + C";
                        string steps = $"{intLabel}: $\\int \\frac{{{a}}}{{x}}\\,dx = {a}\\ln|x| + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"1/{a} * ln|x| + C", $"{a}/x^2 + C", $"-{a}/x^2 + C", rng);
                        return q;
                    });
                }

                if (rad)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 8);
                        string aStr = a == 1 ? "" : $"{a}";
                        string formula = $"int {aStr}sqrt(x) dx";
                        int num = 2 * a;
                        string ans = (num % 3 == 0) ? $"{num / 3} * x^(3/2) + C" : $"{num}/3 * x^(3/2) + C";
                        string steps = $"{intLabel}: $\\int {aStr}x^{{1/2}}\\,dx = {a}\\cdot\\frac{{x^{{3/2}}}}{{3/2}} + C = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{num} * x^(3/2) + C", $"1/(2*sqrt(x)) + C", $"{a} * x^(1/2) + C", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 8);
                        string aStr = a == 1 ? "1" : $"{a}";
                        string formula = $"int {aStr}/sqrt(x) dx";
                        int num = 2 * a;
                        string ans = $"{num} * sqrt(x) + C";
                        string steps = $"{intLabel}: $\\int {aStr}x^{{-1/2}}\\,dx = {a}\\cdot\\frac{{x^{{1/2}}}}{{1/2}} + C = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{a} * sqrt(x) + C", $"\\frac{{{a}}}{{2}} * sqrt(x) + C", $"{num}x + C", rng);
                        return q;
                    });
                }

                if (poly && trig)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 6) * (rng.Next(2) == 0 ? 1 : -1);
                        int c2 = rng.Next(2, 8);
                        string formula = $"int ({c1}x + {c2}*cos(x)) dx";
                        string ans = $"{c1}/2 x^2 + {c2}*sin(x) + C";
                        string steps = $"{intLabel}: $\\int ({c1}x + {c2}\\cos(x))\\,dx = \\frac{{{c1}}}{{2}}x^2 + {c2}\\sin(x) + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{c1}x^2 - {c2}*sin(x) + C", $"{c1}/2 x^2 - {c2}*sin(x) + C", $"{c1}x^2 + {c2}*cos(x) + C", rng);
                        return q;
                    });
                }

                if (poly && exp)
                {
                    pool.Add(() =>
                    {
                        int c1 = rng.Next(2, 8);
                        string formula = $"int ({c1}x + e^x) dx";
                        string ans = $"{c1}/2 x^2 + e^x + C";
                        string steps = $"{intLabel}: $\\int ({c1}x + e^x)\\,dx = \\frac{{{c1}}}{{2}}x^2 + e^x + C$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{c1}x^2 + e^x + C", $"{c1}/2 x^2 + x*e^x + C", $"{c1}x + e^x + C", rng);
                        return q;
                    });
                }

                if (poly || pool.Count == 0)
                {
                    pool.Add(() =>
                    {
                        int p1 = comp == 0 ? rng.Next(1, 4) : (comp == 1 ? rng.Next(2, 5) : rng.Next(3, 8));
                        int c1 = rng.Next(2, 9);
                        int newP = p1 + 1;
                        string formula = $"int {c1}x^{{{p1}}} dx";
                        string ans = (c1 % newP == 0) ? $"{c1 / newP}x^{{{newP}}} + C" : $"{c1}/{newP} x^{{{newP}}} + C";
                        string steps = $"{intLabel}: $\\int x^n\\,dx = \\frac{{x^{{n+1}}}}{{n+1}} \\Rightarrow {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"{c1 * newP}x^{{{newP}}} + C", $"{c1}x^{{{p1}}} + C", $"{c1}x^{{{newP}}} + C", rng);
                        return q;
                    });
                }
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateDefiniteIntegrationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "def_eval")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);
            bool rat = HasRational(p);

            if (!poly && !trig && !exp && !rat)
                poly = true;

            string intLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Teorema fundamental del cálculo",
                AppLanguage.Portuguese => "Teorema fundamental do cálculo",
                _ => "Fundamental Theorem of Calculus"
            };

            var pool = new List<Func<Question>>();

            if (trig)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(1, 7);
                    string kStr = k == 1 ? "x" : $"{k}x";
                    string bStr = k == 1 ? "\\pi" : $"\\frac{{\\pi}}{{{k}}}";
                    string formula = $"int [0 to {bStr}] sin({kStr}) dx";
                    int ansVal = 2;
                    string ans = (ansVal % k == 0) ? $"{ansVal / k}" : $"\\frac{{{ansVal}}}{{{k}}}";
                    string steps = $"{intLabel}: $\\left[ -\\frac{{1}}{{{k}}}\\cos({kStr}) \\right]_0^{{{bStr}}} = -\\frac{{1}}{{{k}}}(-1 - 1) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "-1", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(1, 7);
                    string kStr = k == 1 ? "x" : $"{k}x";
                    int den = 2 * k;
                    string bStr = $"\\frac{{\\pi}}{{{den}}}";
                    string formula = $"int [0 to {bStr}] cos({kStr}) dx";
                    string ans = k == 1 ? "1" : $"\\frac{{1}}{{{k}}}";
                    string steps = $"{intLabel}: $\\left[ \\frac{{1}}{{{k}}}\\sin({kStr}) \\right]_0^{{{bStr}}} = \\frac{{1}}{{{k}}}(1 - 0) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "2", "-1", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(1, 7);
                    string formula = $"int [0 to pi] {k}*sin(x) dx";
                    int ansVal = 2 * k;
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ -{k}\\cos(x) \\right]_0^{{\\pi}} = -{k}(-1 - 1) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", k.ToString(), (ansVal + 2).ToString(), rng);
                    return q;
                });
            }

            if (exp)
            {
                pool.Add(() =>
                {
                    int b = rng.Next(1, 4);
                    int k = rng.Next(1, 4);
                    string kStr = k == 1 ? "x" : $"{k}x";
                    string formula = $"int [0 to {b}] e^({kStr}) dx";
                    int expVal = k * b;
                    string ans = k == 1 ? $"e^{{{expVal}}} - 1" : $"\\frac{{e^{{{expVal}}} - 1}}{{{k}}}";
                    string steps = $"{intLabel}: $\\left[ \\frac{{1}}{{{k}}}e^{{{kStr}}} \\right]_0^{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"e^{{{expVal}}}", $"\\frac{{e^{{{expVal}}}}}{{{k}}}", "1", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 7);
                    string formula = $"int [0 to 1] {k}*e^x dx";
                    string ans = $"{k}e - {k}";
                    string steps = $"{intLabel}: $\\left[ {k}e^x \\right]_0^1 = {k}(e - 1) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k}e", $"{k}e + {k}", $"{k}", rng);
                    return q;
                });
            }

            if (rat)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    int b = rng.Next(2, 9);
                    string kStr = k == 1 ? "1" : $"{k}";
                    string formula = $"int [1 to {b}] {kStr}/x dx";
                    string ans = k == 1 ? $"ln({b})" : $"{k}*ln({b})";
                    string steps = $"{intLabel}: $\\left[ {k}\\ln|x| \\right]_1^{{{b}}} = {k}\\ln({b}) - {k}\\ln(1) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"ln({b})", $"\\frac{{{k}}}{{{b}}}", $"{k}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "int [0 to 1] 1/(1 + x^2) dx";
                    string ans = "\\frac{\\pi}{4}";
                    string steps = $"{intLabel}: $\\left[ \\arctan(x) \\right]_0^1 = \\arctan(1) - \\arctan(0) = \\frac{{\\pi}}{{4}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\frac{\\pi}{2}", "1", "\\pi", rng);
                    return q;
                });
            }

            if (poly || pool.Count == 0)
            {
                pool.Add(() =>
                {
                    int c1 = rng.Next(1, 6);
                    int a = rng.Next(-2, 3);
                    int b = a + rng.Next(1, 5);
                    string formula = $"int [{a} to {b}] ({2 * c1}x) dx";
                    int ansVal = c1 * (b * b - a * a);
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ {c1}x^2 \\right]_{{{a}}}^{{{b}}} = {c1}({b})^2 - {c1}({a})^2 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (ansVal - 3).ToString(), (ansVal * 2).ToString(), rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(0, 3);
                    int b = a + rng.Next(1, 4);
                    string formula = $"int [{a} to {b}] (3x^2) dx";
                    int ansVal = b * b * b - a * a * a;
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ x^3 \\right]_{{{a}}}^{{{b}}} = {b}^3 - {a}^3 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 5).ToString(), (ansVal - 5).ToString(), (ansVal * 2).ToString(), rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateRiemannTableProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int method = p?.ComplexityOptionIndex ?? 3;
            if (method < 0 || method > 4) method = 3;
            if (method == 4)
            {
                method = (index + rng.Next(4)) % 4;
            }

            int[] xVals;
            int[] yVals;
            int dx;
            int a, b;
            int ansVal;
            string methodLabel;
            string stepsFormula;
            string ansSymbol;

            if (method == 2)
            {
                int numSub = rng.Next(2, 4); // 2 or 3 subintervals
                int totalPoints = 2 * numSub + 1;
                int x0 = rng.Next(0, 3);
                int step = rng.Next(1, 3);
                dx = 2 * step;
                xVals = new int[totalPoints];
                for (int j = 0; j < totalPoints; j++) xVals[j] = x0 + j * step;
                yVals = new int[totalPoints];
                int curY = rng.Next(2, 7);
                for (int j = 0; j < totalPoints; j++)
                {
                    yVals[j] = curY;
                    curY += rng.Next(1, 5) * (rng.Next(4) == 0 ? -1 : 1);
                    if (curY < 1) curY = 1;
                }

                a = xVals[0];
                b = xVals[^1];
                var midTerms = new List<string>();
                int midSum = 0;
                for (int j = 1; j < totalPoints; j += 2)
                {
                    midSum += yVals[j];
                    midTerms.Add(yVals[j].ToString());
                }
                ansVal = dx * midSum;
                stepsFormula = $"{dx} \\cdot [{string.Join(" + ", midTerms)}] = {ansVal}";

                ansSymbol = $"M_{{{numSub}}}";
                methodLabel = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Punto medio (MRAM)",
                    AppLanguage.Portuguese => "Ponto médio (MRAM)",
                    _ => "Midpoint rule (MRAM)"
                };
            }
            else
            {
                int n = rng.Next(3, 6);
                int x0 = rng.Next(0, 4);
                dx = rng.Next(1, 4);
                xVals = new int[n + 1];
                for (int j = 0; j <= n; j++) xVals[j] = x0 + j * dx;
                yVals = new int[n + 1];
                int curY = rng.Next(2, 7);
                for (int j = 0; j <= n; j++)
                {
                    yVals[j] = curY;
                    curY += rng.Next(1, 5) * (rng.Next(4) == 0 ? -1 : 1);
                    if (curY < 1) curY = 1;
                }

                a = xVals[0];
                b = xVals[^1];

                if (method == 0) // LRAM
                {
                    int sum = 0;
                    var terms = new List<string>();
                    for (int i = 0; i < n; i++)
                    {
                        sum += yVals[i];
                        terms.Add(yVals[i].ToString());
                    }
                    ansVal = dx * sum;
                    ansSymbol = $"L_{{{n}}}";
                    stepsFormula = $"{dx} \\cdot ({string.Join(" + ", terms)}) = {dx} \\cdot {sum} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Extremo izquierdo (LRAM)",
                        AppLanguage.Portuguese => "Extremo esquerdo (LRAM)",
                        _ => "Left endpoint (LRAM)"
                    };
                }
                else if (method == 1) // RRAM
                {
                    int sum = 0;
                    var terms = new List<string>();
                    for (int i = 1; i <= n; i++)
                    {
                        sum += yVals[i];
                        terms.Add(yVals[i].ToString());
                    }
                    ansVal = dx * sum;
                    ansSymbol = $"R_{{{n}}}";
                    stepsFormula = $"{dx} \\cdot ({string.Join(" + ", terms)}) = {dx} \\cdot {sum} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Extremo derecho (RRAM)",
                        AppLanguage.Portuguese => "Extremo direito (RRAM)",
                        _ => "Right endpoint (RRAM)"
                    };
                }
                else // Trapezoid
                {
                    int innerSum = 0;
                    var terms = new List<string> { yVals[0].ToString() };
                    innerSum += yVals[0];
                    for (int i = 1; i < n; i++)
                    {
                        innerSum += 2 * yVals[i];
                        terms.Add($"2({yVals[i]})");
                    }
                    innerSum += yVals[n];
                    terms.Add(yVals[n].ToString());

                    ansVal = (dx * innerSum) / 2;
                    ansSymbol = $"T_{{{n}}}";
                    string fracPrefix = (dx % 2 == 0) ? $"{dx / 2}" : $"\\frac{{{dx}}}{{2}}";
                    stepsFormula = $"{fracPrefix} \\cdot [{string.Join(" + ", terms)}] = {fracPrefix} \\cdot {innerSum} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Regla del trapecio",
                        AppLanguage.Portuguese => "Regra do trapézio",
                        _ => "Trapezoidal rule"
                    };
                }
            }

            string colSpec = new string('c', xVals.Length);
            string xRow = string.Join(" & ", xVals);
            string yRow = string.Join(" & ", yVals);
            string tableLatex = $"\\begin{{array}}{{|{string.Join("|", colSpec.ToCharArray())}|}}\\hline x & {xRow} \\\\\\hline f(x) & {yRow} \\\\\\hline\\end{{array}}";

            string formula = $"\\int_{{{a}}}^{{{b}}} f(x)\\,dx,\\;\\;\\; {tableLatex}";
            string ans = $"{ansSymbol} = {ansVal}";
            string steps = $"{methodLabel}: $\\Delta x = {dx}$. ${ansSymbol} = {stepsFormula}$";

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                q.Choices = DistractorHelper.BuildChoices(
                    ans,
                    $"{ansSymbol} = {ansVal + dx * 2}",
                    $"{ansSymbol} = {ansVal - dx * 2}",
                    $"{ansSymbol} = {ansVal + dx}",
                    rng);
            }

            return q;
        }

        public static Question GenerateApproxAreaProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int method = p?.ComplexityOptionIndex ?? 3;
            if (method < 0 || method > 4) method = 3;
            if (method == 4) method = (index + rng.Next(4)) % 4;

            int variant = (index + rng.Next(5)) % 5;
            string funcLatex;
            Func<double, double> evalFunc;
            int a, b, n;

            if (variant == 0)
            {
                int c = rng.Next(1, 6);
                funcLatex = $"f(x) = x^2 + {c}";
                evalFunc = x => x * x + c;
                a = 0; b = 4; n = 4;
            }
            else if (variant == 1)
            {
                int k = rng.Next(2, 5);
                int c = rng.Next(1, 5);
                funcLatex = $"f(x) = {k}x + {c}";
                evalFunc = x => k * x + c;
                a = 1; b = 5; n = 4;
            }
            else if (variant == 2)
            {
                int c = rng.Next(10, 18);
                funcLatex = $"f(x) = {c} - x^2";
                evalFunc = x => c - x * x;
                a = 0; b = 3; n = 3;
            }
            else if (variant == 3)
            {
                int c = rng.Next(3, 8);
                funcLatex = $"f(x) = x^2 - 2x + {c}";
                evalFunc = x => x * x - 2 * x + c;
                a = 0; b = 4; n = 4;
            }
            else
            {
                int k = rng.Next(1, 4);
                funcLatex = $"f(x) = 2x^2 + {k}";
                evalFunc = x => 2 * x * x + k;
                a = 0; b = 3; n = 3;
            }

            double dx = (double)(b - a) / n;
            string methodLabel;
            string ansSymbol;
            double ansVal;
            string stepsDetail;

            if (method == 0) // LRAM
            {
                double sum = 0;
                var terms = new List<string>();
                for (int i = 0; i < n; i++)
                {
                    double xVal = a + i * dx;
                    double y = evalFunc(xVal);
                    sum += y;
                    terms.Add(y.ToString());
                }
                ansVal = dx * sum;
                ansSymbol = $"L_{{{n}}}";
                stepsDetail = $"{dx} \\cdot ({string.Join(" + ", terms)}) = {ansVal}";
                methodLabel = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Extremo izquierdo (LRAM)",
                    AppLanguage.Portuguese => "Extremo esquerdo (LRAM)",
                    _ => "Left endpoint (LRAM)"
                };
            }
            else if (method == 1) // RRAM
            {
                double sum = 0;
                var terms = new List<string>();
                for (int i = 1; i <= n; i++)
                {
                    double xVal = a + i * dx;
                    double y = evalFunc(xVal);
                    sum += y;
                    terms.Add(y.ToString());
                }
                ansVal = dx * sum;
                ansSymbol = $"R_{{{n}}}";
                stepsDetail = $"{dx} \\cdot ({string.Join(" + ", terms)}) = {ansVal}";
                methodLabel = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Extremo derecho (RRAM)",
                    AppLanguage.Portuguese => "Extremo direito (RRAM)",
                    _ => "Right endpoint (RRAM)"
                };
            }
            else if (method == 2) // MRAM
            {
                double sum = 0;
                var terms = new List<string>();
                for (int i = 0; i < n; i++)
                {
                    double xVal = a + (i + 0.5) * dx;
                    double y = evalFunc(xVal);
                    sum += y;
                    terms.Add(y.ToString());
                }
                ansVal = dx * sum;
                ansSymbol = $"M_{{{n}}}";
                stepsDetail = $"{dx} \\cdot ({string.Join(" + ", terms)}) = {ansVal}";
                methodLabel = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Punto medio (MRAM)",
                    AppLanguage.Portuguese => "Ponto médio (MRAM)",
                    _ => "Midpoint rule (MRAM)"
                };
            }
            else // Trapezoid
            {
                double lSum = 0, rSum = 0;
                for (int i = 0; i < n; i++) lSum += evalFunc(a + i * dx);
                for (int i = 1; i <= n; i++) rSum += evalFunc(a + i * dx);
                double L = dx * lSum;
                double R = dx * rSum;
                ansVal = (L + R) / 2.0;
                ansSymbol = $"T_{{{n}}}";
                stepsDetail = $"\\frac{{{L} + {R}}}{{2}} = {ansVal}";
                methodLabel = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Regla del trapecio",
                    AppLanguage.Portuguese => "Regra do trapézio",
                    _ => "Trapezoidal rule"
                };
            }

            string formula = $"{funcLatex};\\; [{a}, {b}],\\; n = {n}";
            string ans = $"{ansSymbol} = {ansVal}";
            string steps = $"{methodLabel}: $\\Delta x = {dx}$. ${ansSymbol} = {stepsDetail}$";

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps
            };

            if (type == QuestionType.MultipleChoice)
            {
                q.Choices = DistractorHelper.BuildChoices(
                    ans,
                    $"{ansSymbol} = {ansVal + 4}",
                    $"{ansSymbol} = {ansVal - 4}",
                    $"{ansSymbol} = {ansVal + 8}",
                    rng);
            }

            return q;
        }

        #region New Comprehensive Topic Generators
        public static Question GenerateLimitsOfSumsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(6)) % 6;
            string formula, ans, steps;
            string evalLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Evaluando como integral definida:",
                AppLanguage.Portuguese => "Avaliando como integral definida:",
                _ => "Evaluating as a definite integral:"
            };

            if (variant == 0)
            {
                int aHalf = rng.Next(1, 4);
                int a = aHalf * 2;
                int b = rng.Next(1, 6);
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({a}\left(\frac{{i}}{{n}}\right) + {b}\right)\left(\frac{{1}}{{n}}\right)";
                int ansVal = aHalf + b;
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 ({a}x + {b})\,dx = \left[ {aHalf}x^2 + {b}x \right]_0^1 = {aHalf} + {b} = {ansVal}";
            }
            else if (variant == 1)
            {
                int cThird = rng.Next(1, 5);
                int c = cThird * 3;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{i}}{{n}}\right)^2\right)\left(\frac{{1}}{{n}}\right)";
                ans = cThird.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 {c}x^2\,dx = \left[ {cThird}x^3 \right]_0^1 = {cThird}";
            }
            else if (variant == 2)
            {
                int k = rng.Next(2, 4);
                int cHalf = rng.Next(1, 3);
                int c = cHalf * 2;
                int ansVal = cHalf * k * k;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{{k}i}}{{n}}\right)\right)\left(\frac{{{k}}}{{n}}\right)";
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^{{{k}}} {c}x\,dx = \left[ {cHalf}x^2 \right]_0^{{{k}}} = {ansVal}";
            }
            else if (variant == 3)
            {
                int a = rng.Next(1, 4);
                int b = rng.Next(1, 4) * 2 + 1;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({a} + {b}\left(\frac{{i}}{{n}}\right)\right)\left(\frac{{1}}{{n}}\right)";
                int num = 2 * a + b;
                ans = $@"\frac{{{num}}}{{2}}";
                steps = evalLabel + "\n" + $@"\int_0^1 ({a} + {b}x)\,dx = \left[ {a}x + \frac{{{b}x^2}}{{2}} \right]_0^1 = {a} + \frac{{{b}}}{{2}} = \frac{{{num}}}{{2}}";
            }
            else if (variant == 4)
            {
                int aThird = rng.Next(1, 3);
                int a = aThird * 3;
                int bHalf = rng.Next(1, 3);
                int b = bHalf * 2;
                int ansVal = aThird + bHalf;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({a}\left(\frac{{i}}{{n}}\right)^2 + {b}\left(\frac{{i}}{{n}}\right)\right)\left(\frac{{1}}{{n}}\right)";
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 ({a}x^2 + {b}x)\,dx = \left[ {aThird}x^3 + {bHalf}x^2 \right]_0^1 = {aThird} + {bHalf} = {ansVal}";
            }
            else
            {
                int cFourth = rng.Next(1, 4);
                int c = cFourth * 4;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{i}}{{n}}\right)^3\right)\left(\frac{{1}}{{n}}\right)";
                ans = cFourth.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 {c}x^3\,dx = \left[ {cFourth}x^4 \right]_0^1 = {cFourth}";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "1", "2", "4", rng);
            return q;
        }

        public static Question GenerateChangeOfVarsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int c = rng.Next(1, 4);
                int b = rng.Next(1, 3);
                int pwr = rng.Next(2, 4);
                formula = $@"\int_0^{{{b}}} 2x(x^2 + {c})^{{{pwr}}}\,dx";
                int u0 = c;
                int u1 = b * b + c;
                int pwr1 = pwr + 1;
                int val1 = (int)Math.Pow(u1, pwr1);
                int val0 = (int)Math.Pow(u0, pwr1);
                int diff = val1 - val0;
                ans = diff % pwr1 == 0 ? (diff / pwr1).ToString() : $@"\frac{{{diff}}}{{{pwr1}}}";
                steps = $@"u = x^2 + {c},\; du = 2x\,dx.\; x=0 \implies u={u0},\; x={b} \implies u={u1}.\; \int_{{{u0}}}^{{{u1}}} u^{{{pwr}}}\,du = \left[ \frac{{u^{{{pwr1}}}}}{{{pwr1}}} \right]_{{{u0}}}^{{{u1}}} = {ans}.";
            }
            else if (variant == 1)
            {
                int c = rng.Next(1, 4);
                int b = rng.Next(1, 3);
                formula = $@"\int_0^{{{b}}} 3x^2(x^3 + {c})^2\,dx";
                int u0 = c;
                int u1 = b * b * b + c;
                int diff = u1 * u1 * u1 - u0 * u0 * u0;
                ans = diff % 3 == 0 ? (diff / 3).ToString() : $@"\frac{{{diff}}}{{3}}";
                steps = $@"u = x^3 + {c},\; du = 3x^2\,dx.\; x=0 \implies u={u0},\; x={b} \implies u={u1}.\; \int_{{{u0}}}^{{{u1}}} u^2\,du = \left[ \frac{{u^3}}{{3}} \right]_{{{u0}}}^{{{u1}}} = {ans}.";
            }
            else if (variant == 2)
            {
                int k = rng.Next(1, 4);
                formula = $@"\int_0^{{\frac{{\pi}}{{2}}}} \cos(x)\sin^{{{k}}}(x)\,dx";
                int k1 = k + 1;
                ans = $@"\frac{{1}}{{{k1}}}";
                steps = $@"u = \sin(x),\; du = \cos(x)\,dx.\; x=0 \implies u=0,\; x=\frac{{\pi}}{{2}} \implies u=1.\; \int_0^1 u^{{{k}}}\,du = \left[ \frac{{u^{{{k1}}}}}{{{k1}}} \right]_0^1 = {ans}.";
            }
            else
            {
                int c = rng.Next(1, 4);
                formula = $@"\int_0^{{\ln(2)}} e^x(e^x + {c})\,dx";
                int u0 = 1 + c;
                int u1 = 2 + c;
                int diff = (u1 * u1 - u0 * u0);
                ans = diff % 2 == 0 ? (diff / 2).ToString() : $@"\frac{{{diff}}}{{2}}";
                steps = $@"u = e^x + {c},\; du = e^x\,dx.\; x=0 \implies u={u0},\; x=\ln(2) \implies u={u1}.\; \int_{{{u0}}}^{{{u1}}} u\,du = \left[ \frac{{u^2}}{{2}} \right]_{{{u0}}}^{{{u1}}} = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "1", "2", "3", rng);
            return q;
        }

        public static Question GenerateMvtIntegralsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int a = rng.Next(1, 4) * 3; // multiple of 3
                int b = rng.Next(1, 4);
                formula = $@"f(x) = {a}x^2;\; [0, {b}]";
                int ansVal = (a / 3) * b * b;
                ans = ansVal.ToString();
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}-0}}\int_0^{{{b}}} {a}x^2\,dx = \frac{{1}}{{{b}}}\left[ \frac{{{a}x^3}}{{3}} \right]_0^{{{b}}} = {ansVal}.";
            }
            else if (variant == 1)
            {
                int m = rng.Next(2, 5);
                int k = rng.Next(1, 5);
                int L = rng.Next(0, 3);
                int R = L + rng.Next(2, 5);
                formula = $@"f(x) = {m}x + {k};\; [{L}, {R}]";
                int midNum = m * (L + R) + 2 * k;
                ans = midNum % 2 == 0 ? (midNum / 2).ToString() : $@"\frac{{{midNum}}}{{2}}";
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{R}-{L}}}\int_{{{L}}}^{{{R}}} ({m}x + {k})\,dx = \frac{{1}}{{{R - L}}}\left[ \frac{{{m}x^2}}{{2}} + {k}x \right]_{{{L}}}^{{{R}}} = {ans}.";
            }
            else if (variant == 2)
            {
                int c = rng.Next(4, 9);
                int b = rng.Next(1, 4);
                formula = $@"f(x) = {c} - x^2;\; [0, {b}]";
                int num = 3 * c * b - b * b * b;
                int den = 3 * b;
                int g = Gcd(Math.Abs(num), Math.Abs(den));
                num /= g; den /= g;
                ans = den == 1 ? num.ToString() : $@"\frac{{{num}}}{{{den}}}";
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}}}\int_0^{{{b}}} ({c} - x^2)\,dx = \frac{{1}}{{{b}}}\left[ {c}x - \frac{{x^3}}{{3}} \right]_0^{{{b}}} = {ans}.";
            }
            else
            {
                int k = rng.Next(1, 4);
                int b = k * k;
                formula = $@"f(x) = \sqrt{{x}};\; [0, {b}]";
                int num = 2 * k;
                int den = 3;
                ans = $@"\frac{{{num}}}{{{den}}}";
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}}}\int_0^{{{b}}} x^{{1/2}}\,dx = \frac{{1}}{{{b}}}\left[ \frac{{2}}{{3}}x^{{3/2}} \right]_0^{{{b}}} = \frac{{{num}}}{{{den}}}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "2", "5", "8", rng);
            return q;
        }

        public static Question GenerateFtc2Problem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int k = rng.Next(2, 4);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_1^{{x^{{{k}}}}} \sqrt{{t^3 + 1}}\,dt \right]";
                int expInner = 3 * k;
                string xDeriv = k == 2 ? $"{k}x" : $"{k}x^{{{k - 1}}}";
                ans = $@"{xDeriv}\sqrt{{x^{{{expInner}}} + 1}}";
                steps = $@"f(u(x))\cdot u'(x) = \sqrt{{(x^{{{k}}})^3 + 1}}\cdot ({xDeriv}) = {ans}.";
            }
            else if (variant == 1)
            {
                int a = rng.Next(2, 5);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_0^{{{a}x}} \cos(t^2)\,dt \right]";
                int a2 = a * a;
                ans = $@"{a}\cos({a2}x^2)";
                steps = $@"f(u(x))\cdot u'(x) = \cos(({a}x)^2)\cdot {a} = {ans}.";
            }
            else if (variant == 2)
            {
                int c = rng.Next(1, 4);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_2^{{x^2}} \frac{{1}}{{t + {c}}}\,dt \right]";
                ans = $@"\frac{{2x}}{{x^2 + {c}}}";
                steps = $@"f(u(x))\cdot u'(x) = \frac{{1}}{{x^2 + {c}}}\cdot 2x = {ans}.";
            }
            else
            {
                int a = rng.Next(2, 5);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_1^{{{a}x}} (t^2 + 4)^2\,dt \right]";
                int a2 = a * a;
                ans = $@"{a}({a2}x^2 + 4)^2";
                steps = $@"f(u(x))\cdot u'(x) = (({a}x)^2 + 4)^2\cdot {a} = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "2x", "x^2", "1", rng);
            return q;
        }

        public static Question GenerateAreaUnderCurveProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int a = rng.Next(2, 5);
                int a2 = a * a;
                formula = $@"f(x) = {a2} - x^2;\; [{-a}, {a}]";
                int num = 4 * a * a * a;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_{{-{a}}}^{{{a}}} ({a2} - x^2)\,dx = 2 \left[ {a2}x - \frac{{x^3}}{{3}} \right]_0^{{{a}}} = {ans}.";
            }
            else if (variant == 1)
            {
                int b = rng.Next(2, 5);
                formula = $@"f(x) = x^2;\; [0, {b}]";
                int num = b * b * b;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{b}}} x^2\,dx = \left[ \frac{{x^3}}{{3}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                int a = rng.Next(1, 4);
                int twoA = 2 * a;
                formula = $@"f(x) = {twoA}x - x^2;\; [0, {twoA}]";
                int num = 4 * a * a * a;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{twoA}}} ({twoA}x - x^2)\,dx = \left[ {a}x^2 - \frac{{x^3}}{{3}} \right]_0^{{{twoA}}} = {ans}.";
            }
            else
            {
                int b = rng.Next(1, 3) * 4;
                formula = $@"f(x) = \sqrt{{x}};\; [0, {b}]";
                double rootB = Math.Sqrt(b);
                int num = (int)(2 * b * rootB);
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{b}}} x^{{1/2}}\,dx = \left[ \frac{{2}}{{3}}x^{{3/2}} \right]_0^{{{b}}} = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "12", "8", "16/3", rng);
            return q;
        }

        public static Question GenerateAreaBetweenCurvesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int m = rng.Next(1, 4);
                formula = $@"y = {m}x,\quad y = x^2";
                int num = m * m * m;
                ans = num % 6 == 0 ? (num / 6).ToString() : $@"\frac{{{num}}}{{6}}";
                steps = $@"{m}x = x^2 \implies x=0, {m}.\; \text{{Área}} = \int_0^{{{m}}} ({m}x - x^2)\,dx = \left[ \frac{{{m}x^2}}{{2}} - \frac{{x^3}}{{3}} \right]_0^{{{m}}} = {ans}.";
            }
            else if (variant == 1)
            {
                formula = @"y = 2 - x^2,\quad y = x";
                ans = @"\frac{9}{2}";
                steps = @"2 - x^2 = x \implies x^2 + x - 2 = 0 \implies x=-2, 1.\; \text{Área} = \int_{-2}^1 (2 - x - x^2)\,dx = \frac{9}{2}.";
            }
            else if (variant == 2)
            {
                int a = rng.Next(1, 3);
                int a2 = a * a * 4;
                formula = $@"y = x^2,\quad y = {a2}";
                int root = 2 * a;
                int num = 4 * root * root * root;
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"x^2 = {a2} \implies x = \pm {root}.\; \text{{Área}} = \int_{{-{root}}}^{{{root}}} ({a2} - x^2)\,dx = {ans}.";
            }
            else
            {
                int a = rng.Next(2, 4);
                int a2 = a * a;
                formula = $@"y = {a2} - x^2,\quad y = x^2 - {a2}";
                int num = 8 * a * a * a;
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"{a2} - x^2 = x^2 - {a2} \implies 2x^2 = {2 * a2} \implies x = \pm {a}.\; \text{{Área}} = \int_{{-{a}}}^{{{a}}} ({2 * a2} - 2x^2)\,dx = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "1/3", "4/3", "9/2", rng);
            return q;
        }

        public static Question GenerateVolumeDisksProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int b = rng.Next(2, 6);
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = b * b;
                ans = num % 2 == 0 ? $"{num / 2}\\pi" : $@"\frac{{{num}\pi}}{{2}}";
                steps = $@"V = \pi \int_0^{{{b}}} (\sqrt{{x}})^2\,dx = \pi \left[ \frac{{x^2}}{{2}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 1)
            {
                int b = rng.Next(1, 4);
                formula = $@"y = x^2,\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = (int)Math.Pow(b, 5);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = \pi \int_0^{{{b}}} (x^2)^2\,dx = \pi \left[ \frac{{x^5}}{{5}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                int m = rng.Next(2, 4);
                int b = rng.Next(2, 4);
                formula = $@"y = {m}x,\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = m * m * b * b * b;
                ans = num % 3 == 0 ? $"{num / 3}\\pi" : $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = \pi \int_0^{{{b}}} ({m}x)^2\,dx = \pi \left[ \frac{{{m * m}x^3}}{{3}} \right]_0^{{{b}}} = {ans}.";
            }
            else
            {
                int r = rng.Next(2, 4);
                int r2 = r * r;
                formula = $@"y = \sqrt{{{r2} - x^2}},\; y = 0;\quad (\text{{eje }} x)";
                int num = 4 * r * r * r;
                ans = num % 3 == 0 ? $"{num / 3}\\pi" : $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = \pi \int_{{-{r}}}^{{{r}}} ({r2} - x^2)\,dx = \pi \left[ {r2}x - \frac{{x^3}}{{3}} \right]_{{-{r}}}^{{{r}}} = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "8pi", "16pi", "4pi", rng);
            return q;
        }

        public static Question GenerateVolumeShellsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int b = rng.Next(1, 4);
                formula = $@"y = x^2,\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                int num = b * b * b * b;
                ans = num % 2 == 0 ? $"{num / 2}\\pi" : $@"\frac{{{num}\pi}}{{2}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(x^2)\,dx = 2\pi \left[ \frac{{x^4}}{{4}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 1)
            {
                int b = rng.Next(1, 4) * 4;
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                double rootB = Math.Sqrt(b);
                int num = (int)(4 * b * b * rootB);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(\sqrt{{x}})\,dx = 2\pi \left[ \frac{{2}}{{5}}x^{{5/2}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                int a = rng.Next(1, 3);
                int twoA = 2 * a;
                formula = $@"y = {twoA}x - x^2,\; y = 0;\quad (\text{{eje }} y)";
                int num = 8 * a * a * a * a;
                ans = $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = 2\pi \int_0^{{{twoA}}} x({twoA}x - x^2)\,dx = 2\pi \left[ \frac{{{twoA}x^3}}{{3}} - \frac{{x^4}}{{4}} \right]_0^{{{twoA}}} = {ans}.";
            }
            else
            {
                int b = rng.Next(1, 3);
                formula = $@"y = x^3,\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                int num = 2 * (int)Math.Pow(b, 5);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(x^3)\,dx = 2\pi \left[ \frac{{x^5}}{{5}} \right]_0^{{{b}}} = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "8pi", "16pi", "2pi", rng);
            return q;
        }

        public static Question GenerateCrossSectionsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int r = rng.Next(2, 4);
                int r2 = r * r;
                formula = $@"x^2 + y^2 = {r2};\quad (\text{{cuadrados }} \perp \text{{eje }} x)";
                int num = 16 * r * r * r;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Lado}} = 2y = 2\sqrt{{{r2} - x^2}}.\; A(x) = (2y)^2 = 4({r2} - x^2).\; V = \int_{{-{r}}}^{{{r}}} 4({r2} - x^2)\,dx = {ans}.";
            }
            else if (variant == 1)
            {
                int a = rng.Next(1, 3);
                int a2 = a * a;
                formula = $@"y = {a2} - x^2,\; y = 0;\quad (\text{{cuadrados }} \perp \text{{eje }} x)";
                int num = 16 * (int)Math.Pow(a, 5);
                ans = $@"\frac{{{num}}}{{15}}";
                steps = $@"\text{{Lado}} = {a2} - x^2.\; A(x) = ({a2} - x^2)^2.\; V = \int_{{-{a}}}^{{{a}}} ({a2} - x^2)^2\,dx = {ans}.";
            }
            else if (variant == 2)
            {
                int b = rng.Next(2, 6);
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{semicírculos }} \perp \text{{eje }} x)";
                int num = b * b;
                ans = $@"\frac{{{num}\pi}}{{16}}";
                steps = $@"r = \frac{{\sqrt{{x}}}}{{2}}.\; A(x) = \frac{{\pi r^2}}{{2}} = \frac{{\pi x}}{{8}}.\; V = \int_0^{{{b}}} \frac{{\pi x}}{{8}}\,dx = {ans}.";
            }
            else
            {
                int a = rng.Next(2, 4);
                formula = $@"y = {a * a} - x^2,\; y = 0;\quad (\text{{triángulos eq. }} \perp \text{{eje }} x)";
                int num = 16 * (int)Math.Pow(a, 5);
                ans = $@"\frac{{{num}\sqrt{{3}}}}{{15}}";
                steps = $@"\text{{Base}} = {a * a} - x^2.\; A(x) = \frac{{\sqrt{{3}}}}{{4}}({a * a} - x^2)^2.\; V = \frac{{\sqrt{{3}}}}{{4}}\int_{{-{a}}}^{{{a}}} ({a * a} - x^2)^2\,dx = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "32", "64/3", "128/3", rng);
            return q;
        }

        public static Question GenerateMotionRevisitedProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(3)) % 3;
            string formula, ans, steps;

            if (variant == 0)
            {
                int a = rng.Next(2, 4);
                int b = rng.Next(2, 5);
                int T = b + rng.Next(1, 3);
                formula = $@"v(t) = {a}t - {a * b};\; [0, {T}]";
                int t1 = b;
                int desp = (a * T * T) / 2 - a * b * T;
                int dist1 = (a * t1 * t1) / 2;
                int dist2 = (a * (T - t1) * (T - t1)) / 2;
                int totalDist = dist1 + dist2;
                ans = $@"\text{{Despl.}} = {desp};\; \text{{Dist.}} = {totalDist}";
                steps = $@"\text{{Despl.}} = \int_0^{{{T}}} ({a}t - {a * b})\,dt = {desp}.\; \text{{Dist.}} = \int_0^{{{b}}} ({a * b} - {a}t)\,dt + \int_{{{b}}}^{{{T}}} ({a}t - {a * b})\,dt = {totalDist}.";
            }
            else if (variant == 1)
            {
                int k = rng.Next(1, 3);
                int T = k + 1;
                formula = $@"v(t) = 3t^2 - {3 * k * k};\; [0, {T}]";
                int desp = T * T * T - 3 * k * k * T;
                int dist1 = 2 * k * k * k;
                int dist2 = (T * T * T - 3 * k * k * T) - (-2 * k * k * k);
                int totalDist = dist1 + dist2;
                ans = $@"\text{{Despl.}} = {desp};\; \text{{Dist.}} = {totalDist}";
                steps = $@"\text{{Despl.}} = \int_0^{{{T}}} (3t^2 - {3 * k * k})\,dt = {desp}.\; \text{{Dist.}} = \int_0^{{{k}}} ({3 * k * k} - 3t^2)\,dt + \int_{{{k}}}^{{{T}}} (3t^2 - {3 * k * k})\,dt = {totalDist}.";
            }
            else
            {
                int k = rng.Next(2, 5);
                formula = $@"v(t) = t - {k};\; [0, {k + 2}]";
                int T = k + 2;
                int despNum = T * T - 2 * k * T;
                string despStr = despNum % 2 == 0 ? (despNum / 2).ToString() : $@"\frac{{{despNum}}}{{2}}";
                int distNum = k * k + 4;
                string distStr = distNum % 2 == 0 ? (distNum / 2).ToString() : $@"\frac{{{distNum}}}{{2}}";
                ans = $@"\text{{Despl.}} = {despStr};\; \text{{Dist.}} = {distStr}";
                steps = $@"\text{{Despl.}} = \int_0^{{{T}}} (t - {k})\,dt = {despStr}.\; \text{{Dist.}} = {distStr}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"\text{Despl.} = 0;\; \text{Dist.} = 10", @"\text{Despl.} = 4;\; \text{Dist.} = 8", @"\text{Despl.} = -2;\; \text{Dist.} = 6", rng);
            return q;
        }

        public static Question GenerateSlopeFieldsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            var pool = new List<Func<Question>>();

            // 1. Value of slope at specific point (x0, y0)
            pool.Add(() =>
            {
                int a = rng.Next(-3, 4);
                if (a == 0) a = 1;
                int b = rng.Next(-3, 4);
                if (b == 0) b = -1;
                int x0 = rng.Next(-4, 5);
                int y0 = rng.Next(-4, 5);
                string aStr = a == 1 ? "x" : (a == -1 ? "-x" : $"{a}x");
                string bStr = b > 0 ? $"+ {b}y" : (b == -1 ? "- y" : $"- {Math.Abs(b)}y");
                if (b == 1) bStr = "+ y";
                string formula = $@"\frac{{dy}}{{dx}} = {aStr} {bStr};\quad \text{{Halle la pendiente en el punto }} ({x0}, {y0})";
                int slope = a * x0 + b * y0;
                string ans = $"m = {slope}";
                string steps = $@"\frac{{dy}}{{dx}}\Big|_{{({x0}, {y0})}} = {a}({x0}) + {b}({y0}) = {slope}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"m = {slope + 1}", $"m = {-slope}", $"m = {slope - 2}", rng);
                return q;
            });

            // 2. Slope along y = x
            pool.Add(() =>
            {
                int k = rng.Next(1, 6);
                string kStr = k == 1 ? "" : k.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = -\frac{{{kStr}x}}{{y}};\quad \text{{Describe las pendientes a lo largo de }} y = x \;(y \neq 0)";
                string ans = $@"m = -{k}";
                string steps = $@"y = x \implies \frac{{dy}}{{dx}} = -\frac{{{k}x}}{{x}} = -{k}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"m = {k}", "m = 0", @"\text{Indefinido}", rng);
                return q;
            });

            // 3. Slope along horizontal line y = k
            pool.Add(() =>
            {
                int k = rng.Next(-4, 5);
                string kSign = k >= 0 ? $"- {k}" : $"+ {Math.Abs(k)}";
                string formula = $@"\frac{{dy}}{{dx}} = y {kSign};\quad \text{{Describe las pendientes a lo largo de }} y = {k}";
                string ans = @"m = 0 \text{ (pendientes horizontales)}";
                string steps = $@"y = {k} \implies \frac{{dy}}{{dx}} = {k} {kSign} = 0.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "m = 1", $"m = {k}", @"\text{Verticales}", rng);
                return q;
            });

            // 4. Slope along vertical line x = k
            pool.Add(() =>
            {
                int k = rng.Next(1, 6);
                string formula = $@"\frac{{dy}}{{dx}} = {k}x;\quad \text{{Describe las pendientes a lo largo de }} x = 0 \text{{ (eje }} y)";
                string ans = @"m = 0 \text{ (pendientes horizontales)}";
                string steps = $@"x = 0 \implies \frac{{dy}}{{dx}} = {k}(0) = 0.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"m = {k}", "m = 1", @"\text{Indefinido}", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateDiffEqIntroProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            string formula, ans, steps;

            if (variant == 0)
            {
                int k = rng.Next(2, 9);
                int c = rng.Next(2, 8);
                formula = $@"y = {c}e^{{{k}x}};\quad \text{{¿Es solución de }} y' - {k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {c * k}e^{{{k}x}}.\; y' - {k}y = {c * k}e^{{{k}x}} - {k}({c}e^{{{k}x}}) = 0.";
            }
            else if (variant == 1)
            {
                int k = rng.Next(2, 8);
                formula = $@"y = \sin({k}x);\quad \text{{¿Es solución de }} y'' + {k * k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {k}\cos({k}x),\; y'' = -{k * k}\sin({k}x).\; y'' + {k * k}y = -{k * k}\sin({k}x) + {k * k}\sin({k}x) = 0.";
            }
            else if (variant == 2)
            {
                int n = rng.Next(2, 7);
                formula = $@"y = x^{{{n}}};\; (x > 0)\quad \text{{¿Es solución de }} xy' - {n}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {n}x^{{{n - 1}}}.\; xy' = x({n}x^{{{n - 1}}}) = {n}x^{{{n}}} = {n}y \implies xy' - {n}y = 0.";
            }
            else
            {
                int k = rng.Next(2, 7);
                formula = $@"y = \cos({k}x);\quad \text{{¿Es solución de }} y'' + {k * k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = -{k}\sin({k}x),\; y'' = -{k * k}\cos({k}x).\; y'' + {k * k}y = 0.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"\text{No, es falso}", @"\text{Solo para } x = 0", @"\text{Indefinido}", rng);
            return q;
        }

        public static Question GenerateSeparableProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            var pool = new List<Func<Question>>();

            // 1. dy/dx = k*x*y
            pool.Add(() =>
            {
                int k = rng.Next(1, 11);
                string kStr = k == 1 ? "" : k.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = {kStr}xy";
                string ans = k % 2 == 0 ? $@"y = Ce^{{{k / 2}x^2}}" : (k == 1 ? @"y = Ce^{\frac{1}{2}x^2}" : $@"y = Ce^{{\frac{{{k}}}{{2}}x^2}}");
                string steps = $@"\frac{{1}}{{y}}\,dy = {kStr}x\,dx \implies \ln|y| = \frac{{{k}x^2}}{{2}} + C_1 \implies {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $@"y = {k}x^2 + C", $@"y = Ce^{{{k}x}}", $@"y = \ln({k}x) + C", rng);
                return q;
            });

            // 2. dy/dx = (k*x) / y
            pool.Add(() =>
            {
                int k = rng.Next(1, 9);
                string kStr = k == 1 ? "" : k.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = \frac{{{kStr}x}}{{y}}";
                string ans = k == 1 ? @"y^2 - x^2 = C" : $@"y^2 - {k}x^2 = C";
                string steps = $@"y\,dy = {kStr}x\,dx \implies \frac{{y^2}}{{2}} = \frac{{{k}x^2}}{{2}} + C_1 \implies {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $@"y = {k}x + C", $@"y^2 + x^2 = C", $@"y = \sqrt{{{k}x}} + C", rng);
                return q;
            });

            // 3. dy/dx = k*y^2
            pool.Add(() =>
            {
                int k = rng.Next(1, 8);
                string kStr = k == 1 ? "" : k.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = {kStr}y^2";
                string ans = k == 1 ? @"y = -\frac{1}{x + C}" : $@"y = -\frac{{1}}{{{k}x + C}}";
                string steps = $@"\frac{{1}}{{y^2}}\,dy = {kStr}\,dx \implies -\frac{{1}}{{y}} = {kStr}x + C \implies {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $@"y = {k}x^2 + C", $@"y = \frac{{1}}{{{k}x + C}}", $@"y = e^{{{k}x}}", rng);
                return q;
            });

            // 4. dy/dx = k*x^2 * y
            pool.Add(() =>
            {
                int k = rng.Next(1, 8);
                string kStr = k == 1 ? "" : k.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = {kStr}x^2 y";
                string ans = k % 3 == 0 ? $@"y = Ce^{{{k / 3}x^3}}" : $@"y = Ce^{{\frac{{{k}}}{{3}}x^3}}";
                string steps = $@"\frac{{1}}{{y}}\,dy = {kStr}x^2\,dx \implies \ln|y| = \frac{{{k}x^3}}{{3}} + C_1 \implies {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $@"y = Ce^{{{k}x^2}}", $@"y = {k}x^3 + C", @"y = \ln(x^3) + C", rng);
                return q;
            });

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateGrowthDecayProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int p0 = rng.Next(2, 8) * 100;
            int doubleTime = rng.Next(2, 5);
            int multiplier = rng.Next(2, 4);
            int totalHours = doubleTime * multiplier;
            long finalPop = p0 * (long)Math.Pow(2, multiplier);

            string formula = $"P(0) = {p0}.\\; \\text{{Población se duplica cada }} {doubleTime} \\text{{ h. Halle }} P({totalHours}).";
            string ans = $"{finalPop}";
            string steps = $@"P(t) = {p0} \cdot 2^{{t / {doubleTime}}}.\; P({totalHours}) = {p0} \cdot 2^{{{totalHours} / {doubleTime}}} = {p0} \cdot 2^{{{multiplier}}} = {finalPop}.";

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, $"{finalPop / 2}", $"{finalPop * 2}", $"{finalPop + p0}", rng);
            return q;
        }

        public static Question GenerateDerivTablesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int a = rng.Next(1, 4);
            int fa = rng.Next(1, 5);
            int fpa = rng.Next(1, 5);
            int ga = rng.Next(-3, 4);
            if (ga == 0) ga = -1;
            int gpa = rng.Next(1, 6);

            int variant = (index + rng.Next(2)) % 2;
            string formula, ans, steps;

            if (variant == 0)
            {
                formula = $@"f({a})={fa},\\; f'({a})={fpa},\\; g({a})={ga},\\; g'({a})={gpa}.\\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = f(x)g(x)";
                int ansVal = fpa * ga + fa * gpa;
                ans = ansVal.ToString();
                steps = $@"h'({a}) = f'({a})g({a}) + f({a})g'({a}) = ({fpa})({ga}) + ({fa})({gpa}) = {ansVal}.";
            }
            else
            {
                formula = $@"f({a})={fa},\\; f'({a})={fpa},\\; g({a})={ga},\\; g'({a})={gpa}.\\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = {fa}f(x) + {gpa}g(x)";
                int ansVal = fa * fpa + gpa * gpa;
                ans = ansVal.ToString();
                steps = $@"h'({a}) = {fa}f'({a}) + {gpa}g'({a}) = {fa}({fpa}) + {gpa}({gpa}) = {ansVal}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "10", "15", "-5", rng);
            return q;
        }

        public static Question GenerateLogarithmicDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(6)) % 6;
            string formula, ans, steps;

            if (variant == 0)
            {
                int k = rng.Next(1, 6);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = x^{{{kStr}x}}";
                ans = $@"y' = {kStr}x^{{{kStr}x}}(\ln(x) + 1)";
                steps = $@"\ln(y) = {kStr}x\ln(x) \implies \frac{{y'}}{{y}} = {kStr}(\ln(x) + 1) \implies y' = {ans}.";
            }
            else if (variant == 1)
            {
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = x^{{\sin({kStr}x)}}";
                ans = $@"y' = x^{{\sin({kStr}x)}}\left({kStr}\cos({kStr}x)\ln(x) + \frac{{\sin({kStr}x)}}{{x}}\right)";
                steps = $@"\ln(y) = \sin({kStr}x)\ln(x) \implies \frac{{y'}}{{y}} = {kStr}\cos({kStr}x)\ln(x) + \frac{{\sin({kStr}x)}}{{x}} \implies y' = {ans}.";
            }
            else if (variant == 2)
            {
                int a = rng.Next(1, 6);
                formula = $@"y = (x + {a})^x";
                ans = $@"y' = (x + {a})^x\left(\ln(x + {a}) + \frac{{x}}{{x + {a}}}\right)";
                steps = $@"\ln(y) = x\ln(x + {a}) \implies \frac{{y'}}{{y}} = \ln(x + {a}) + \frac{{x}}{{x + {a}}} \implies y' = {ans}.";
            }
            else if (variant == 3)
            {
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = (\sin(x))^{{{kStr}x}}";
                ans = $@"y' = {kStr}(\sin(x))^{{{kStr}x}}(\ln(\sin(x)) + x\cot(x))";
                steps = $@"\ln(y) = {kStr}x\ln(\sin(x)) \implies \frac{{y'}}{{y}} = {kStr}\ln(\sin(x)) + {kStr}x\cot(x) \implies y' = {ans}.";
            }
            else if (variant == 4)
            {
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                int k2 = 2 * k;
                formula = $@"y = x^{{{kStr}\ln(x)}}";
                ans = $@"y' = {k2}x^{{{kStr}\ln(x)-1}}\ln(x)";
                steps = $@"\ln(y) = {kStr}(\ln(x))^2 \implies \frac{{y'}}{{y}} = \frac{{{k2}\ln(x)}}{{x}} \implies y' = {ans}.";
            }
            else
            {
                formula = @"y = x^{\sqrt{x}}";
                ans = @"y' = x^{\sqrt{x}}\left(\frac{\ln(x) + 2}{2\sqrt{x}}\right)";
                steps = @"\ln(y) = \sqrt{x}\ln(x) \implies \frac{y'}{y} = \frac{\ln(x)}{2\sqrt{x}} + \frac{1}{\sqrt{x}} \implies y' = " + ans + ".";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"y' = x\cdot " + formula.Substring(4), @"y' = " + formula.Substring(4) + @"\ln(x)", @"y' = " + formula.Substring(4), rng);
            return q;
        }

        public static Question GenerateInverseFuncDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int a = rng.Next(1, 4);
            int b = rng.Next(1, 4);
            int x0 = rng.Next(1, 3);
            int y0 = x0 * x0 * x0 + a * x0 + b;
            int deriv = 3 * x0 * x0 + a;

            string formula = $@"f(x) = x^3 + {a}x + {b};\\; \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f({x0}) = {y0}";
            string ans = $@"(f^{{-1}})'({y0}) = \frac{{1}}{{{deriv}}}";
            string steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = 3x^2 + {a} \implies f'({x0}) = 3({x0})^2 + {a} = {deriv}.\; \text{{Resp: }} \frac{{1}}{{{deriv}}}.";

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, $"{deriv}", $@"\frac{{-1}}{{{deriv}}}", @"\frac{1}{3}", rng);
            return q;
        }

        public static Question GenerateGraphComparisonProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(4)) % 4;
            int xVal = rng.Next(1, 6);
            string formula, ans, steps;

            if (variant == 0)
            {
                formula = $@"\text{{Si }} f'(x) \text{{ cruza el eje }} x \text{{ de positivo a negativo en }} x = {xVal}, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un máximo relativo en }} x = {xVal}";
                steps = $@"f' \text{{ cambia de }} + \text{{ a }} -, \text{{ por lo que }} f \text{{ cambia de creciente a decreciente }} \implies \text{{Máximo relativo}}.";
            }
            else if (variant == 1)
            {
                formula = $@"\text{{Si }} f'(x) \text{{ cruza el eje }} x \text{{ de negativo a positivo en }} x = {xVal}, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un mínimo relativo en }} x = {xVal}";
                steps = $@"f' \text{{ cambia de }} - \text{{ a }} +, \text{{ por lo que }} f \text{{ cambia de decreciente a creciente }} \implies \text{{Mínimo relativo}}.";
            }
            else if (variant == 2)
            {
                formula = $@"\text{{Si }} f''(x) \text{{ cambia de signo en }} x = {xVal} \text{{ y }} f'({xVal}) \text{{ existe, ¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un punto de inflexión en }} x = {xVal}";
                steps = @"\text{Un cambio de signo en } f'' \text{ indica cambio de concavidad } \implies \text{Punto de inflexión}.";
            }
            else
            {
                formula = $@"\text{{Si }} f'({xVal}) = 0 \text{{ y }} f''({xVal}) < 0, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un máximo relativo local en }} x = {xVal}";
                steps = @"\text{Por el criterio de la segunda derivada: } f' = 0 \text{ y } f'' < 0 \implies \text{Cóncava hacia abajo } \implies \text{Máximo local}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"f(x) \text{ tiene un mínimo relativo}", @"f(x) \text{ no tiene extremos}", @"\text{Indefinido}", rng);
            return q;
        }

        public static Question GenerateDifferentialsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int[] bases = { 4, 9, 16, 25, 36, 49, 64, 81, 100 };
            int baseNum = bases[rng.Next(bases.Length)];
            double root = Math.Sqrt(baseNum);
            double[] dxChoices = { -0.3, -0.2, -0.1, 0.1, 0.2, 0.3, 0.4, 0.5 };
            double dx = dxChoices[rng.Next(dxChoices.Length)];
            double xVal = baseNum + dx;

            double deriv = 1.0 / (2.0 * root);
            double dy = deriv * dx;
            double approx = Math.Round(root + dy, 4);

            string formula = $@"f(x) = \sqrt{{x}};\; \text{{Aproxime }} \sqrt{{{xVal:0.1}}} \text{{ mediante diferenciales}}";
            string ans = $"{approx}";
            string steps = $@"x_0 = {baseNum},\\; dx = {dx:0.1}.\\; f'({baseNum}) = \frac{{1}}{{2\sqrt{{{baseNum}}}}} = {deriv:0.000}.\\; dy = ({deriv:0.000})({dx:0.1}) = {dy:0.000}.\\; \text{{Aprox: }} {root} + {dy:0.000} = {approx}.";

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, $"{approx + 0.1:0.000}", $"{approx - 0.1:0.000}", $"{root}", rng);
            return q;
        }

        public static Question GenerateNewtonMethodProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int[] nonSquares = { 2, 3, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20, 21, 22, 23, 24, 26, 27, 28, 29, 31, 32, 33, 34, 35 };
            int k = nonSquares[rng.Next(nonSquares.Length)];
            int x0 = (int)Math.Round(Math.Sqrt(k));
            if (x0 * x0 == k) x0++;

            double fx0 = x0 * x0 - k;
            double fpx0 = 2.0 * x0;
            double x1 = Math.Round(x0 - fx0 / fpx0, 3);

            string formula = $@"f(x) = x^2 - {k};\\; x_0 = {x0}.\\; \text{{Halle }} x_1 \text{{ mediante el Método de Newton}}";
            string ans = $"x_1 = {x1}";
            string steps = $@"x_1 = x_0 - \frac{{f(x_0)}}{{f'(x_0)}} = {x0} - \frac{{{fx0}}}{{{fpx0}}} = {x1}.";

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, $"x_1 = {x1 + 0.25:0.000}", $"x_1 = {x1 - 0.25:0.000}", $"x_1 = {x0}", rng);
            return q;
        }

        public static Question GenerateIndefSubsLogExpProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(3)) % 3;
            string formula, ans, steps;

            if (variant == 0)
            {
                int k = rng.Next(1, 5);
                formula = $@"int x e^{{{k}x^2}}\,dx";
                int den = 2 * k;
                ans = den == 1 ? $@"e^{{x^2}} + C" : $@"\frac{{1}}{{{den}}}e^{{{k}x^2}} + C";
                steps = $@"u = {k}x^2,\; du = {2 * k}x\,dx \implies \frac{{1}}{{{2 * k}}}\int e^u\,du = {ans}.";
            }
            else if (variant == 1)
            {
                int a = rng.Next(1, 5);
                formula = $@"int \frac{{x}}{{x^2 + {a}}}\,dx";
                ans = $@"\frac{{1}}{{2}}\ln|x^2 + {a}| + C";
                steps = $@"u = x^2 + {a},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int \frac{{1}}{{u}}\,du = \frac{{1}}{{2}}\ln|u| + C = {ans}.";
            }
            else
            {
                formula = @"int \frac{\ln(x)}{x}\,dx";
                ans = @"\frac{(\ln(x))^2}{2} + C";
                steps = @"u = \ln(x),\; du = \frac{1}{x}\,dx \implies \int u\,du = \frac{u^2}{2} + C = \frac{(\ln(x))^2}{2} + C.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"e^{x^2} + C", @"\ln|x| + C", @"\frac{x^2}{2} + C", rng);
            return q;
        }

        public static Question GenerateIndefSubsPowerProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = (index + rng.Next(6)) % 6;
            string formula, ans, steps;
            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución",
                AppLanguage.Portuguese => "Substituição",
                _ => "Substitution"
            };

            if (variant == 0)
            {
                int c = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                string cSign = c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}";
                int n = rng.Next(2, 5);
                int n1 = n + 1;
                int den = 2 * n1;
                formula = $@"int x(x^2 {cSign})^{{{n}}}\,dx";
                ans = $@"\frac{{1}}{{{den}}}(x^2 {cSign})^{{{n1}}} + C";
                steps = $@"{substLabel}: u = x^2 {cSign},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int u^{{{n}}}\,du = \frac{{1}}{{{den}}}u^{{{n1}}} + C = {ans}.";
            }
            else if (variant == 1)
            {
                int c = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                string cSign = c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}";
                int n = rng.Next(2, 5);
                int n1 = n + 1;
                int den = 3 * n1;
                formula = $@"int x^2(x^3 {cSign})^{{{n}}}\,dx";
                ans = $@"\frac{{1}}{{{den}}}(x^3 {cSign})^{{{n1}}} + C";
                steps = $@"{substLabel}: u = x^3 {cSign},\; du = 3x^2\,dx \implies \frac{{1}}{{3}}\int u^{{{n}}}\,du = \frac{{1}}{{{den}}}u^{{{n1}}} + C = {ans}.";
            }
            else if (variant == 2)
            {
                int c = rng.Next(1, 6);
                formula = $@"int x\sqrt{{x^2 + {c}}}\,dx";
                ans = $@"\frac{{1}}{{3}}(x^2 + {c})^{{3/2}} + C";
                steps = $@"{substLabel}: u = x^2 + {c},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int u^{{1/2}}\,du = \frac{{1}}{{2}}\left(\frac{{2}}{{3}}u^{{3/2}}\right) + C = {ans}.";
            }
            else if (variant == 3)
            {
                int a = rng.Next(2, 6);
                int b = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                string bSign = b > 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                int n = rng.Next(3, 6);
                int n1 = n + 1;
                int den = a * n1;
                formula = $@"int ({a}x {bSign})^{{{n}}}\,dx";
                ans = $@"\frac{{1}}{{{den}}}({a}x {bSign})^{{{n1}}} + C";
                steps = $@"{substLabel}: u = {a}x {bSign},\; du = {a}\,dx \implies \frac{{1}}{{{a}}}\int u^{{{n}}}\,du = \frac{{1}}{{{den}}}u^{{{n1}}} + C = {ans}.";
            }
            else if (variant == 4)
            {
                int c = rng.Next(1, 6);
                formula = $@"int \frac{{x}}{{\sqrt{{x^2 + {c}}}}}\,dx";
                ans = $@"\sqrt{{x^2 + {c}}} + C";
                steps = $@"{substLabel}: u = x^2 + {c},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int u^{{-1/2}}\,du = \frac{{1}}{{2}}(2u^{{1/2}}) + C = {ans}.";
            }
            else
            {
                int b = rng.Next(1, 5) * 2;
                int c = rng.Next(1, 6);
                int n = rng.Next(2, 4);
                int n1 = n + 1;
                formula = $@"int (2x + {b})(x^2 + {b}x + {c})^{{{n}}}\,dx";
                ans = $@"\frac{{1}}{{{n1}}}(x^2 + {b}x + {c})^{{{n1}}} + C";
                steps = $@"{substLabel}: u = x^2 + {b}x + {c},\; du = (2x + {b})\,dx \implies \int u^{{{n}}}\,du = \frac{{1}}{{{n1}}}u^{{{n1}}} + C = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
            if (type == QuestionType.MultipleChoice)
            {
                q.Choices = DistractorHelper.BuildChoices(
                    ans,
                    ans.Replace("1/", "1/2*").Replace(" + C", "") + " + C",
                    ans.Replace(" + C", "").Replace("{3}", "{4}").Replace("{6}", "{8}") + " + C",
                    ans.Replace(" + C", "").Replace("{1}", "{2}") + " + C",
                    rng);
            }
            return q;
        }
        #region Additional Topics with Dynamic Pools and Complexity Scaling

        public static Question GenerateInvTrigDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a passo",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            // 1. Easy pool
            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = \\arctan({a}x)";
                    string ans = $"y' = \\frac{{{a}}}{{1 + {a * a}x^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(u)] = \\frac{{u'}}{{1 + u^2}} = \\frac{{{a}}}{{1 + ({a}x)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{1 + {a * a}x^2}}", $"y' = \\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = -\\frac{{{a}}}{{1 + x^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = \\arcsin({a}x)";
                    string ans = $"y' = \\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(u)] = \\frac{{u'}}{{\\sqrt{{1 - u^2}}}} = \\frac{{{a}}}{{\\sqrt{{1 - ({a}x)^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = \\frac{{{a}}}{{1 + {a * a}x^2}}", $"y' = -\\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = \\arccos({a}x)";
                    string ans = $"y' = -\\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arccos(u)] = -\\frac{{u'}}{{\\sqrt{{1 - u^2}}}} = -\\frac{{{a}}}{{\\sqrt{{1 - ({a}x)^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = -\\frac{{1}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = -\\frac{{{a}}}{{1 + {a * a}x^2}}", rng);
                    return q;
                });
            }
            // 2. Medium pool
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    string formula = "y = \\arctan(x^2)";
                    string ans = "y' = \\frac{2x}{1 + x^4}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(x^2)] = \\frac{{(x^2)'}}{{1 + (x^2)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{2x}{1 + x^2}", "y' = \\frac{1}{1 + x^4}", "y' = \\frac{2x}{\\sqrt{1 - x^4}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = x\\arctan(x)";
                    string ans = "y' = \\arctan(x) + \\frac{x}{1 + x^2}";
                    string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = (1)\\arctan(x) + x\\cdot\\frac{{1}}{{1 + x^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\arctan(x) - \\frac{x}{1 + x^2}", "y' = \\frac{x}{1 + x^2}", "y' = \\arctan(x) + \\frac{1}{1 + x^2}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = \\arcsin(x^2)";
                    string ans = "y' = \\frac{2x}{\\sqrt{1 - x^4}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(x^2)] = \\frac{{(x^2)'}}{{\\sqrt{{1 - (x^2)^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{2x}{\\sqrt{1 - x^2}}", "y' = \\frac{x}{\\sqrt{1 - x^4}}", "y' = \\frac{2x}{1 + x^4}", rng);
                    return q;
                });
            }
            // 3. Hard pool
            else
            {
                pool.Add(() =>
                {
                    string formula = "y = \\arcsin(\\sqrt{x})";
                    string ans = "y' = \\frac{1}{2\\sqrt{x}\\sqrt{1 - x}}";
                    string steps = $"{stepLabel}: " + @"$\frac{d}{dx}[\arcsin(\sqrt{x})] = \frac{1/(2\sqrt{x})}{\sqrt{1 - x}} = " + ans + "$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{1}{\\sqrt{x}\\sqrt{1 - x}}", "y' = \\frac{1}{2\\sqrt{1 - x}}", "y' = \\frac{1}{2\\sqrt{x}(1 - x)}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"y = \\arctan\\left(\\frac{{x}}{{{a}}}\\right)";
                    string ans = $"y' = \\frac{{{a}}}{{{a * a} + x^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(x/{a})] = \\frac{{1/{a}}}{{1 + (x/{a})^2}} = \\frac{{{a}}}{{{a * a} + x^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{{a * a} + x^2}}", $"y' = \\frac{{{a}}}{{1 + x^2}}", $"y' = \\frac{{{a}}}{{\\sqrt{{{a * a} - x^2}}}}", rng);
                    return q;
                });
                if (HasExp(p))
                {
                    pool.Add(() =>
                    {
                        string formula = "y = \\arctan(e^x)";
                        string ans = "y' = \\frac{e^x}{1 + e^{2x}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(e^x)] = \\frac{{e^x}}{{1 + (e^x)^2}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{e^x}{1 + e^x}", "y' = \\frac{1}{1 + e^{2x}}", "y' = \\frac{e^x}{\\sqrt{1 - e^{2x}}}", rng);
                        return q;
                    });
                }
                if (HasLog(p))
                {
                    pool.Add(() =>
                    {
                        string formula = "y = \\arcsin(\\ln(x))";
                        string ans = "y' = \\frac{1}{x\\sqrt{1 - (\\ln(x))^2}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(\\ln(x))] = \\frac{{1/x}}{{\\sqrt{{1 - (\\ln(x))^2}}}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{1}{\\sqrt{1 - (\\ln(x))^2}}", "y' = \\frac{1}{x(1 + (\\ln(x))^2)}", "y' = \\frac{\\ln(x)}{x\\sqrt{1 - (\\ln(x))^2}}", rng);
                        return q;
                    });
                }
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateExpLogDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, bool isNatural = true)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução paso a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            if (isNatural)
            {
                if (comp == 0)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 6);
                        string formula = $"y = e^{{{k}x}}";
                        string ans = $"y' = {k}e^{{{k}x}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{u}}] = u'e^{{u}} = {k}e^{{{k}x}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = e^{{{k}x}}", $"y' = \\frac{{1}}{{{k}}}e^{{{k}x}}", $"y' = {k}x e^{{{k}x - 1}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 6);
                        string formula = $"y = \\ln({k}x)";
                        string ans = "y' = \\frac{1}{x}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\ln({k}x)] = \\frac{{{k}}}{{{k}x}} = \\frac{{1}}{{x}}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{{k}}}{{x}}", $"y' = \\frac{{1}}{{{k}x}}", $"y' = {k}\\ln(x)", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 5);
                        string formula = $"y = e^{{{k}x}} + \\ln(x)";
                        string ans = $"y' = {k}e^{{{k}x}} + \\frac{{1}}{{x}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{{k}x}} + \\ln(x)] = {k}e^{{{k}x}} + \\frac{{1}}{{x}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = e^{{{k}x}} + \\frac{{1}}{{x}}", $"y' = {k}e^{{{k}x}} + \\frac{{{k}}}{{x}}", $"y' = e^{{{k}x}} + \\ln(x)", rng);
                        return q;
                    });
                }
                else if (comp == 1)
                {
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 4);
                        string formula = $"y = e^{{{a}x^2}}";
                        string ans = $"y' = {2 * a}x e^{{{a}x^2}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{{a}x^2}}] = (2\\cdot {a}x)e^{{{a}x^2}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {a}x e^{{{a}x^2}}", $"y' = 2x e^{{{a}x^2}}", $"y' = e^{{{a}x^2}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 6);
                        string formula = $"y = \\ln(x^2 + {a})";
                        string ans = $"y' = \\frac{{2x}}{{x^2 + {a}}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\ln(x^2 + {a})] = \\frac{{(x^2 + {a})'}}{{x^2 + {a}}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{x}}{{x^2 + {a}}}", $"y' = \\frac{{2x}}{{x + {a}}}", $"y' = \\frac{{1}}{{x^2 + {a}}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        string formula = "y = x^2 e^x";
                        string ans = "y' = (x^2 + 2x)e^x";
                        string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = 2x e^x + x^2 e^x = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "y' = 2x e^x", "y' = (x^2 + x)e^x", "y' = x^2 e^x + 2", rng);
                        return q;
                    });
                    if (HasTrig(p))
                    {
                        pool.Add(() =>
                        {
                            string formula = "y = e^x\\sin(x)";
                            string ans = "y' = e^x(\\sin(x) + \\cos(x))";
                            string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = e^x\\sin(x) + e^x\\cos(x) = {ans}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "y' = e^x\\cos(x)", "y' = e^x(\\sin(x) - \\cos(x))", "y' = e^x\\sin(x)", rng);
                            return q;
                        });
                    }
                }
                else
                {
                    pool.Add(() =>
                    {
                        string formula = "y = \\frac{e^x}{x^2 + 1}";
                        string ans = "y' = \\frac{e^x(x^2 - 2x + 1)}{(x^2 + 1)^2}";
                        string steps = $"{stepLabel}: $\\text{{Regla del cociente: }} y' = \\frac{{e^x(x^2 + 1) - e^x(2x)}}{{(x^2 + 1)^2}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{e^x(x^2 + 2x + 1)}{(x^2 + 1)^2}", "y' = \\frac{e^x}{2x}", "y' = \\frac{e^x(x^2 - 1)}{(x^2 + 1)^2}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int a = rng.Next(1, 5);
                        string formula = $"y = \\ln(\\sqrt{{x^2 + {a}}})";
                        string ans = $"y' = \\frac{{x}}{{x^2 + {a}}}";
                        string steps = $"{stepLabel}: $y = \\frac{{1}}{{2}}\\ln(x^2 + {a}) \\implies y' = \\frac{{1}}{{2}}\\cdot\\frac{{2x}}{{x^2 + {a}}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{2x}}{{x^2 + {a}}}", $"y' = \\frac{{1}}{{2\\sqrt{{x^2 + {a}}}}}", $"y' = \\frac{{x}}{{\\sqrt{{x^2 + {a}}}}}", rng);
                        return q;
                    });
                    if (HasTrig(p))
                    {
                        pool.Add(() =>
                        {
                            string formula = "y = e^{\\sin(x)}";
                            string ans = "y' = \\cos(x)e^{\\sin(x)}";
                            string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{\\sin(x)}}] = (\\sin(x))'e^{{\\sin(x)}} = {ans}$";
                            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                            if (type == QuestionType.MultipleChoice)
                                q.Choices = DistractorHelper.BuildChoices(ans, "y' = -\\cos(x)e^{\\sin(x)}", "y' = \\sin(x)e^{\\sin(x)}", "y' = e^{\\cos(x)}", rng);
                            return q;
                        });
                    }
                }
            }
            else
            {
                int[] bases = { 2, 3, 5, 10 };
                int b = bases[rng.Next(bases.Length)];

                if (comp == 0)
                {
                    pool.Add(() =>
                    {
                        string formula = $"y = {b}^x";
                        string ans = $"y' = {b}^x \\ln({b})";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^x] = a^x\\ln(a) \\implies {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {b}^x", $"y' = x {b}^{{x-1}}", $"y' = \\frac{{{b}^x}}{{\\ln({b})}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        string formula = $"y = \\log_{{{b}}}(x)";
                        string ans = $"y' = \\frac{{1}}{{x\\ln({b})}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\log_a(x)] = \\frac{{1}}{{x\\ln(a)}} \\implies {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{\\ln({b})}}{{x}}", $"y' = \\frac{{1}}{{x}}", $"y' = \\frac{{{b}}}{{x\\ln({b})}}", rng);
                        return q;
                    });
                }
                else if (comp == 1)
                {
                    pool.Add(() =>
                    {
                        int k = rng.Next(2, 5);
                        string formula = $"y = {b}^{{{k}x}}";
                        string ans = $"y' = {k}\\cdot {b}^{{{k}x}} \\ln({b})";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^{{kx}}] = k a^{{kx}}\\ln(a) \\implies {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {b}^{{{k}x}} \\ln({b})", $"y' = {k}\\cdot {b}^{{{k}x}}", $"y' = {k}x {b}^{{{k}x - 1}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        int c = rng.Next(1, 5);
                        string formula = $"y = \\log_{{{b}}}(x^2 + {c})";
                        string ans = $"y' = \\frac{{2x}}{{(x^2 + {c})\\ln({b})}}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\log_a(u)] = \\frac{{u'}}{{u\\ln(a)}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{2x}}{{x^2 + {c}}}", $"y' = \\frac{{1}}{{(x^2 + {c})\\ln({b})}}", $"y' = \\frac{{2x\\ln({b})}}{{x^2 + {c}}}", rng);
                        return q;
                    });
                }
                else
                {
                    pool.Add(() =>
                    {
                        string formula = $"y = {b}^{{x^2}}";
                        string ans = $"y' = 2x\\cdot {b}^{{x^2}} \\ln({b})";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^{{u}}] = u' a^u \\ln(a) = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = 2x {b}^{{x^2}}", $"y' = {b}^{{x^2}} \\ln({b})", $"y' = x^2 {b}^{{x^2 - 1}}", rng);
                        return q;
                    });
                    pool.Add(() =>
                    {
                        string formula = $"y = x\\cdot {b}^x";
                        string ans = $"y' = {b}^x(1 + x\\ln({b}))";
                        string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = (1){b}^x + x({b}^x\\ln({b})) = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {b}^x(1 + \\ln({b}))", $"y' = x {b}^x \\ln({b})", $"y' = {b}^x + x", rng);
                        return q;
                    });
                }
            }

            int pickIdx = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pickIdx]();
        }

        public static Question GenerateIndefLogExpProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string intLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Regla de integración",
                AppLanguage.Portuguese => "Regra de integração",
                _ => "Integration rule"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\int e^{{{k}x}}\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}e^{{{k}x}} + C";
                    string steps = $"{intLabel}: $\\int e^{{kx}}\\,dx = \\frac{{1}}{{k}}e^{{kx}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k}e^{{{k}x}} + C", $"e^{{{k}x}} + C", $"\\frac{{1}}{{{k}}}e^x + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 7);
                    string formula = $"\\int \\frac{{{a}}}{{x}}\\,dx";
                    string ans = $"{a}\\ln|x| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{{a}}}{{x}}\\,dx = {a}\\int \\frac{{1}}{{x}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{{a}}}{{x^2}} + C", $"\\frac{{1}}{{{a}}}\\ln|x| + C", $"\\ln|{a}x| + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\left(e^{{{k}x}} + \\frac{{{a}}}{{x}}\\right)\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}e^{{{k}x}} + {a}\\ln|x| + C";
                    string steps = $"{intLabel}: $\\int e^{{{k}x}}\\,dx + {a}\\int \\frac{{1}}{{x}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k}e^{{{k}x}} + {a}\\ln|x| + C", $"\\frac{{1}}{{{k}}}e^{{{k}x}} - \\frac{{{a}}}{{x^2}} + C", $"e^{{{k}x}} + \\ln|x| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int x e^{x^2}\\,dx";
                    string ans = "\\frac{1}{2}e^{x^2} + C";
                    string steps = $"{intLabel}: $u = x^2,\\; du = 2x\\,dx \\implies \\frac{{1}}{{2}}\\int e^u\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "2e^{x^2} + C", "e^{x^2} + C", "\\frac{1}{2}x^2 e^{x^2} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 6);
                    string formula = $"\\int \\frac{{2x}}{{x^2 + {a}}}\\,dx";
                    string ans = $"\\ln(x^2 + {a}) + C";
                    string steps = $"{intLabel}: $u = x^2 + {a},\\; du = 2x\\,dx \\implies \\int \\frac{{1}}{{u}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{2}}\\ln(x^2 + {a}) + C", $"2\\ln(x^2 + {a}) + C", $"\\ln|2x| + C", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"\\int \\frac{{e^x}}{{e^x + {a}}}\\,dx";
                    string ans = $"\\ln(e^x + {a}) + C";
                    string steps = $"{intLabel}: $u = e^x + {a},\\; du = e^x\\,dx \\implies \\int \\frac{{1}}{{u}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{e^x}}{{e^x + {a}}} + C", $"e^x\\ln(e^x + {a}) + C", $"\\ln|e^x| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{\\ln(x)}{x}\\,dx";
                    string ans = "\\frac{1}{2}(\\ln(x))^2 + C";
                    string steps = $"{intLabel}: $u = \\ln(x),\\; du = \\frac{{1}}{{x}}\\,dx \\implies \\int u\\,du = \\frac{{u^2}}{{2}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "(\\ln(x))^2 + C", "\\frac{1}{x^2} + C", "\\ln|\\ln(x)| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int x^2 e^{x^3}\\,dx";
                    string ans = "\\frac{1}{3}e^{x^3} + C";
                    string steps = $"{intLabel}: $u = x^3,\\; du = 3x^2\\,dx \\implies \\frac{{1}}{{3}}\\int e^u\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "3e^{x^3} + C", "\\frac{1}{3}x^3 e^{x^3} + C", "e^{x^3} + C", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateIndefTrigProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string intLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Regla de integración",
                AppLanguage.Portuguese => "Regra de integração",
                _ => "Integration rule"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\int \\cos({k}x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\sin({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\cos(kx)\\,dx = \\frac{{1}}{{k}}\\sin(kx) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\frac{{1}}{{{k}}}\\sin({k}x) + C", $"{k}\\sin({k}x) + C", $"\\frac{{1}}{{{k}}}\\cos({k}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\int \\sin({k}x)\\,dx";
                    string ans = $"-\\frac{{1}}{{{k}}}\\cos({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sin(kx)\\,dx = -\\frac{{1}}{{k}}\\cos(kx) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\cos({k}x) + C", $"-{k}\\cos({k}x) + C", $"-\\sin({k}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"\\int \\sec^2({k}x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\tan({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sec^2(kx)\\,dx = \\frac{{1}}{{k}}\\tan(kx) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\frac{{1}}{{{k}}}\\tan({k}x) + C", $"{k}\\tan({k}x) + C", $"\\frac{{1}}{{{k}}}\\sec({k}x) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    int m = rng.Next(2, 5);
                    string formula = $"\\int (\\sin({k}x) + \\cos({m}x))\\,dx";
                    string ans = $"-\\frac{{1}}{{{k}}}\\cos({k}x) + \\frac{{1}}{{{m}}}\\sin({m}x) + C";
                    string steps = $"{intLabel}: $\\int \\sin({k}x)\\,dx + \\int \\cos({m}x)\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\cos({k}x) - \\frac{{1}}{{{m}}}\\sin({m}x) + C", $"{k}\\cos({k}x) + {m}\\sin({m}x) + C", $"-\\cos({k}x) + \\sin({m}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"\\int \\sec({k}x)\\tan({k}x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\sec({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sec(kx)\\tan(kx)\\,dx = \\frac{{1}}{{k}}\\sec(kx) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\frac{{1}}{{{k}}}\\sec({k}x) + C", $"{k}\\sec({k}x) + C", $"\\frac{{1}}{{{k}}}\\tan({k}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"\\int ({a}x + \\cos(x))\\,dx";
                    string ans = $"\\frac{{{a}}}{{2}}x^2 + \\sin(x) + C";
                    string steps = $"{intLabel}: $\\int {a}x\\,dx + \\int \\cos(x)\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}x^2 - \\sin(x) + C", $"\\frac{{{a}}}{{2}}x^2 - \\sin(x) + C", $"{a}x + \\sin(x) + C", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    string formula = "\\int \\tan(x)\\,dx";
                    string ans = "\\ln|\\sec(x)| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{\\sin(x)}}{{\\cos(x)}}\\,dx = -\\ln|\\cos(x)| + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\sec^2(x) + C", "-\\ln|\\sec(x)| + C", "\\ln|\\cos(x)| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\cot(x)\\,dx";
                    string ans = "\\ln|\\sin(x)| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{\\cos(x)}}{{\\sin(x)}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\csc^2(x) + C", "-\\ln|\\sin(x)| + C", "\\ln|\\cos(x)| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"\\int (\\sec^2({a}x) - {2 * a}x)\\,dx";
                    string ans = $"\\frac{{1}}{{{a}}}\\tan({a}x) - {a}x^2 + C";
                    string steps = $"{intLabel}: $\\frac{{1}}{{{a}}}\\tan({a}x) - \\frac{{{2 * a}}}{{2}}x^2 + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}\\tan({a}x) - {a}x^2 + C", $"\\frac{{1}}{{{a}}}\\tan({a}x) + {a}x^2 + C", $"\\tan({a}x) - x^2 + C", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateIndefInvTrigProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string intLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Regla de integración",
                AppLanguage.Portuguese => "Regra de integração",
                _ => "Integration rule"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{{a * a} - x^2}}}}\\,dx";
                    string ans = $"\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{\\sqrt{{a^2 - x^2}}}}\\,dx = \\arcsin(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\arcsin(x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{1}}{{{a * a} + x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{a}}}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{a^2 + x^2}}\\,dx = \\frac{{1}}{{a}}\\arctan(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"{a}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int k = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{{k}}}{{{a * a} + x^2}}\\,dx";
                    string ans = $"\\frac{{{k}}}{{{a}}}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $k\\int \\frac{{1}}{{a^2 + x^2}}\\,dx = \\frac{{k}}{{a}}\\arctan(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{a}}}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\frac{{{k}}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int k = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{{k}}}{{\\sqrt{{{a * a} - x^2}}}}\\,dx";
                    string ans = $"{k}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $k\\int \\frac{{1}}{{\\sqrt{{a^2 - x^2}}}}\\,dx = k\\arcsin(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{{k}}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", $"{k}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int b = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{1 - {b * b}x^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arcsin({b}x) + C";
                    string steps = $"{intLabel}: $u = {b}x,\\; du = {b}\\,dx \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{\\sqrt{{1 - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{b}\\arcsin({b}x) + C", $"\\arcsin({b}x) + C", $"\\frac{{1}}{{{b}}}\\arctan({b}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int b = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{1}}{{1 + {b * b}x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arctan({b}x) + C";
                    string steps = $"{intLabel}: $u = {b}x,\\; du = {b}\\,dx \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{1 + u^2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{b}\\arctan({b}x) + C", $"\\arctan({b}x) + C", $"\\frac{{1}}{{{b}}}\\arcsin({b}x) + C", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateIndefSubsTrigProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución",
                AppLanguage.Portuguese => "Substituição",
                _ => "Substitution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                // 1. sin^k(x) * cos(x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 9);
                    int k1 = k + 1;
                    string sinPow = k == 1 ? "\\sin(x)" : $"\\sin^{{{k}}}(x)";
                    string formula = $"\\int {sinPow}\\cos(x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k1}}}\\sin^{{{k1}}}(x) + C";
                    string steps = $"{substLabel}: $u = \\sin(x),\\; du = \\cos(x)\\,dx \\implies \\int u^{{{k}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\sin^{{{k1}}}(x) + C", $"-\\frac{{1}}{{{k1}}}\\cos^{{{k1}}}(x) + C", $"\\frac{{1}}{{{k}}}\\sin^{{{k}}}(x) + C", rng);
                    return q;
                });

                // 2. cos^k(x) * sin(x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 9);
                    int k1 = k + 1;
                    string cosPow = k == 1 ? "\\cos(x)" : $"\\cos^{{{k}}}(x)";
                    string formula = $"\\int {cosPow}\\sin(x)\\,dx";
                    string ans = $"-\\frac{{1}}{{{k1}}}\\cos^{{{k1}}}(x) + C";
                    string steps = $"{substLabel}: $u = \\cos(x),\\; du = -\\sin(x)\\,dx \\implies -\\int u^{{{k}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k1}}}\\cos^{{{k1}}}(x) + C", $"-\\cos^{{{k1}}}(x) + C", $"-\\frac{{1}}{{{k}}}\\cos^{{{k}}}(x) + C", rng);
                    return q;
                });

                // 3. tan^k(x) * sec^2(x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 7);
                    int k1 = k + 1;
                    string tanPow = k == 1 ? "\\tan(x)" : $"\\tan^{{{k}}}(x)";
                    string formula = $"\\int {tanPow}\\sec^2(x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k1}}}\\tan^{{{k1}}}(x) + C";
                    string steps = $"{substLabel}: $u = \\tan(x),\\; du = \\sec^2(x)\\,dx \\implies \\int u^{{{k}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\tan^{{{k1}}}(x) + C", $"\\frac{{1}}{{{k1}}}\\sec^{{{k1}}}(x) + C", $"\\frac{{1}}{{{k}}}\\tan^{{{k}}}(x) + C", rng);
                    return q;
                });

                // 4. sin(kx) * cos(kx)
                pool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    string formula = $"\\int \\sin({k}x)\\cos({k}x)\\,dx";
                    int den = 2 * k;
                    string ans = $"\\frac{{1}}{{{den}}}\\sin^2({k}x) + C";
                    string steps = $"{substLabel}: $u = \\sin({k}x),\\; du = {k}\\cos({k}x)\\,dx \\implies \\frac{{1}}{{{k}}}\\int u\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\sin^2({k}x) + C", $"-\\frac{{1}}{{{den}}}\\cos^2({k}x) + C", $"\\sin^2({k}x) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                // 1. x^m * cos(x^{m+1})
                pool.Add(() =>
                {
                    int m = rng.Next(1, 5);
                    int m1 = m + 1;
                    string xPart = m == 1 ? "x" : $"x^{{{m}}}";
                    string inner = m1 == 2 ? "x^2" : $"x^{{{m1}}}";
                    string formula = $"\\int {xPart}\\cos({inner})\\,dx";
                    string ans = $"\\frac{{1}}{{{m1}}}\\sin({inner}) + C";
                    string steps = $"{substLabel}: $u = {inner},\\; du = {m1}{xPart}\\,dx \\implies \\frac{{1}}{{{m1}}}\\int \\cos(u)\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{m1}\\sin({inner}) + C", $"-\\frac{{1}}{{{m1}}}\\sin({inner}) + C", $"\\frac{{1}}{{{m1}}}\\cos({inner}) + C", rng);
                    return q;
                });

                // 2. x^m * sin(x^{m+1})
                pool.Add(() =>
                {
                    int m = rng.Next(1, 5);
                    int m1 = m + 1;
                    string xPart = m == 1 ? "x" : $"x^{{{m}}}";
                    string inner = m1 == 2 ? "x^2" : $"x^{{{m1}}}";
                    string formula = $"\\int {xPart}\\sin({inner})\\,dx";
                    string ans = $"-\\frac{{1}}{{{m1}}}\\cos({inner}) + C";
                    string steps = $"{substLabel}: $u = {inner},\\; du = {m1}{xPart}\\,dx \\implies \\frac{{1}}{{{m1}}}\\int \\sin(u)\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{m1}}}\\cos({inner}) + C", $"-{m1}\\cos({inner}) + C", $"-\\frac{{1}}{{{m1}}}\\sin({inner}) + C", rng);
                    return q;
                });

                // 3. sec^2(kx) * tan^n(kx)
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    int n = rng.Next(2, 6);
                    int n1 = n + 1;
                    int den = k * n1;
                    string formula = $"\\int \\sec^2({k}x)\\tan^{{{n}}}({k}x)\\,dx";
                    string ans = $"\\frac{{1}}{{{den}}}\\tan^{{{n1}}}({k}x) + C";
                    string steps = $"{substLabel}: $u = \\tan({k}x),\\; du = {k}\\sec^2({k}x)\\,dx \\implies \\frac{{1}}{{{k}}}\\int u^{{{n}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\tan^{{{n1}}}({k}x) + C", $"\\frac{{1}}{{{den}}}\\sec^{{{n1}}}({k}x) + C", $"\\tan^{{{n1}}}({k}x) + C", rng);
                    return q;
                });
            }
            else
            {
                // 1. cos(sqrt(x)) / sqrt(x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\cos({kStr}\\sqrt{{x}})}}{{\\sqrt{{x}}}}\\,dx";
                    int coeff = 2;
                    string ans = k == 1 ? "2\\sin(\\sqrt{x}) + C" : (coeff % k == 0 ? $"{coeff / k}\\sin({k}\\sqrt{{x}}) + C" : $"\\frac{{2}}{{{k}}}\\sin({k}\\sqrt{{x}}) + C");
                    string steps = $"{substLabel}: $u = {kStr}\\sqrt{{x}},\\; du = \\frac{{{k}}}{{2\\sqrt{{x}}}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{2}}\\sin({kStr}\\sqrt{{x}}) + C", $"-2\\sin({kStr}\\sqrt{{x}}) + C", $"2\\cos({kStr}\\sqrt{{x}}) + C", rng);
                    return q;
                });

                // 2. sin(ln(x)) / x
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\sin({kStr}\\ln(x))}}{{x}}\\,dx";
                    string ans = k == 1 ? "-\\cos(\\ln(x)) + C" : $"-\\frac{{1}}{{{k}}}\\cos({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {kStr}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\cos({kStr}\\ln(x)) + C", $"-\\sin({kStr}\\ln(x)) + C", $"\\frac{{1}}{{{k}}}\\sin({kStr}\\ln(x)) + C", rng);
                    return q;
                });

                // 3. cos(ln(x)) / x
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\cos({kStr}\\ln(x))}}{{x}}\\,dx";
                    string ans = k == 1 ? "\\sin(\\ln(x)) + C" : $"\\frac{{1}}{{{k}}}\\sin({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {kStr}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\cos({kStr}\\ln(x)) + C", $"\\cos({kStr}\\ln(x)) + C", $"-\\frac{{1}}{{{k}}}\\sin({kStr}\\ln(x)) + C", rng);
                    return q;
                });

                // 4. e^x * cos(e^x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int e^{{{kStr}x}}\\cos(e^{{{kStr}x}})\\,dx";
                    string ans = k == 1 ? "\\sin(e^x) + C" : $"\\frac{{1}}{{{k}}}\\sin(e^{{{k}x}}) + C";
                    string steps = $"{substLabel}: $u = e^{{{kStr}x}},\\; du = {k}e^{{{kStr}x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\cos(e^{{{kStr}x}}) + C", $"{k}\\sin(e^{{{kStr}x}}) + C", $"\\cos(e^{{{kStr}x}}) + C", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateIndefSubsInvTrigProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución",
                AppLanguage.Portuguese => "Substituição",
                _ => "Substitution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 13);
                    string formula = $"\\int \\frac{{1}}{{1 + {a * a}x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{a}}}\\arctan({a}x) + C";
                    string steps = $"{substLabel}: $u = {a}x,\\; du = {a}\\,dx \\implies \\frac{{1}}{{{a}}}\\int \\frac{{1}}{{1 + u^2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan({a}x) + C", $"{a}\\arctan({a}x) + C", $"\\frac{{1}}{{{a}}}\\arcsin({a}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 13);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{1 - {a * a}x^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{a}}}\\arcsin({a}x) + C";
                    string steps = $"{substLabel}: $u = {a}x,\\; du = {a}\\,dx \\implies \\frac{{1}}{{{a}}}\\int \\frac{{1}}{{\\sqrt{{1 - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin({a}x) + C", $"{a}\\arcsin({a}x) + C", $"\\frac{{1}}{{{a}}}\\arctan({a}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int b = rng.Next(2, 11);
                    string formula = $"\\int \\frac{{1}}{{{b * b} + x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arctan\\left(\\frac{{x}}{{{b}}}\\right) + C";
                    string steps = $"{substLabel}: $\\int \\frac{{1}}{{a^2 + x^2}}\\,dx = \\frac{{1}}{{a}}\\arctan(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan(x/{b}) + C", $"{b}\\arctan(x/{b}) + C", $"\\frac{{1}}{{{b}}}\\arcsin(x/{b}) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int b = rng.Next(2, 11);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{{b * b} - x^2}}}}\\,dx";
                    string ans = $"\\arcsin\\left(\\frac{{x}}{{{b}}}\\right) + C";
                    string steps = $"{substLabel}: $\\int \\frac{{1}}{{\\sqrt{{a^2 - x^2}}}}\\,dx = \\arcsin(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{b}}}\\arcsin(x/{b}) + C", $"\\arcsin(x/{b * b}) + C", $"\\frac{{1}}{{{b}}}\\arctan(x/{b}) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 7);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x}}{{1 + {aStr}x^4}}\\,dx";
                    int den = 2 * a;
                    string ans = den == 2 && a == 1 ? "\\frac{1}{2}\\arctan(x^2) + C" : $"\\frac{{1}}{{{den}}}\\arctan({a}x^2) + C";
                    string steps = $"{substLabel}: $u = {a}x^2,\\; du = {2 * a}x\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan({a}x^2) + C", $"\\frac{{1}}{{{den}}}\\arcsin({a}x^2) + C", $"\\frac{{1}}{{{4 * a}}}\\arctan({a}x^4) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 7);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x}}{{\\sqrt{{1 - {aStr}x^4}}}}\\,dx";
                    int den = 2 * a;
                    string ans = den == 2 && a == 1 ? "\\frac{1}{2}\\arcsin(x^2) + C" : $"\\frac{{1}}{{{den}}}\\arcsin({a}x^2) + C";
                    string steps = $"{substLabel}: $u = {a}x^2,\\; du = {2 * a}x\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin({a}x^2) + C", $"\\frac{{1}}{{{den}}}\\arctan({a}x^2) + C", $"\\frac{{1}}{{{4 * a}}}\\arcsin({a}x^4) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{e^{{{kStr}x}}}}{{1 + e^{{{2 * k}x}}}}\\,dx";
                    string ans = k == 1 ? "\\arctan(e^x) + C" : $"\\frac{{1}}{{{k}}}\\arctan(e^{{{k}x}}) + C";
                    string steps = $"{substLabel}: $u = e^{{{kStr}x}},\\; du = {k}e^{{{kStr}x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arcsin(e^{{{k}x}}) + C", $"{k}\\arctan(e^{{{k}x}}) + C", $"\\ln(1 + e^{{{2 * k}x}}) + C", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\cos({kStr}x)}}{{1 + \\sin^2({kStr}x)}}\\,dx";
                    string ans = k == 1 ? "\\arctan(\\sin(x)) + C" : $"\\frac{{1}}{{{k}}}\\arctan(\\sin({k}x)) + C";
                    string steps = $"{substLabel}: $u = \\sin({kStr}x),\\; du = {k}\\cos({kStr}x)\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin(\\sin({kStr}x)) + C", $"\\frac{{1}}{{{k}}}\\arctan(\\sin^2({kStr}x)) + C", $"\\ln(1 + \\sin^2({kStr}x)) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{1}}{{x(1 + {k * k}(\\ln(x))^2)}}\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\arctan({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {k}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arcsin({k}\\ln(x)) + C", $"\\arctan({k}\\ln(x)) + C", $"\\ln(1 + {k * k}(\\ln(x))^2) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{1}}{{x\\sqrt{{1 - {k * k}(\\ln(x))^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\arcsin({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {k}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arctan({k}\\ln(x)) + C", $"\\arcsin({k}\\ln(x)) + C", $"\\ln|\\ln(x)| + C", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateContinuityClassifyProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a passo",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 16);
                    string formula = $"f(x) = \\frac{{x - {a}}}{{x^2 - {a * a}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Evitable en x = {a}; Infinita en x = -{a}",
                        AppLanguage.Portuguese => $"Removível em x = {a}; Infinita em x = -{a}",
                        _ => $"Removable at x = {a}; Infinite at x = -{a}"
                    };
                    string steps = $"{stepLabel}: $x^2 - {a * a} = (x - {a})(x + {a})$. Factor $(x - {a})$ se cancela $\\implies$ Evitable en $x = {a}$. Factor $(x + {a})$ en denominador $\\implies$ asíntota vertical (Infinita) en $x = -{a}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {a}; Evitable en x = -{a}" : $"Infinite at x = {a}; Removable at x = -{a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"De salto en x = {a}" : $"Jump at x = {a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 16);
                    string formula = $"f(x) = \\frac{{x + {a}}}{{x^2 - {a * a}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Evitable en x = -{a}; Infinita en x = {a}",
                        AppLanguage.Portuguese => $"Removível em x = -{a}; Infinita em x = {a}",
                        _ => $"Removable at x = {a}; Infinite at x = {a}"
                    };
                    string steps = $"{stepLabel}: $(x + {a})$ se cancela $\\implies$ Evitable en $x = -{a}$. Denominador se anula en $x = {a} \\implies$ Infinita.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = {a}; Infinita en x = -{a}" : $"Removable at x = {a}; Infinite at x = -{a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"De salto en x = -{a}" : $"Jump at x = -{a}",
                            rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 16);
                    int b = a + rng.Next(1, 8);
                    string formula = $"f(x) = \\frac{{x - {b}}}{{(x - {a})(x - {b})}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Evitable en x = {b}; Infinita en x = {a}",
                        AppLanguage.Portuguese => $"Removível em x = {b}; Infinita em x = {a}",
                        _ => $"Removable at x = {b}; Infinite at x = {a}"
                    };
                    string steps = $"{stepLabel}: Factor $(x - {b})$ se cancela $\\implies$ Evitable en $x = {b}$. Factor $(x - {a})$ queda en denominador $\\implies$ Infinita en $x = {a}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = {a}; Infinita en x = {b}" : $"Removable at x = {a}; Infinite at x = {b}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {a} y x = {b}" : $"Infinite at x = {a} and x = {b}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(1, 16);
                    string formula = $"f(x) = \\frac{{x^2 - {c * c}}}{{x - {c}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Evitable en x = {c}",
                        AppLanguage.Portuguese => $"Removível em x = {c}",
                        _ => $"Removable at x = {c}"
                    };
                    string steps = $"{stepLabel}: $x^2 - {c * c} = (x - {c})(x + {c})$. El factor se simplifica para todo $x \\neq {c} \\implies$ Discontinuidad evitable en $x = {c}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {c}" : $"Infinite at x = {c}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"De salto en x = {c}" : $"Jump at x = {c}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 16);
                    string formula = $"f(x) = \\frac{{|x - {a}|}}{{x - {a}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Discontinuidad de salto en x = {a}",
                        AppLanguage.Portuguese => $"Descontinuidade de salto em x = {a}",
                        _ => $"Jump discontinuity at x = {a}"
                    };
                    string steps = $"{stepLabel}: $\\lim_{{x \\to {a}^+}} f(x) = 1$ y $\\lim_{{x \\to {a}^-}} f(x) = -1$. Los límites laterales son distintos $\\implies$ Salto en $x = {a}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = {a}" : $"Removable at x = {a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {a}" : $"Infinite at x = {a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 16);
                    string formula = $"f(x) = \\frac{{x^2 - {a}x}}{{x^2 - {a * a}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Evitable en x = {a}; Infinita en x = -{a}",
                        AppLanguage.Portuguese => $"Removível em x = {a}; Infinita em x = -{a}",
                        _ => $"Removable at x = {a}; Infinite at x = -{a}"
                    };
                    string steps = $"{stepLabel}: $\\frac{{x(x - {a})}}{{(x - {a})(x + {a})}} = \\frac{{x}}{{x + {a}}}$. Se cancela $(x - {a}) \\implies$ Evitable en $x = {a}$; denominador es 0 en $x = -{a} \\implies$ Infinita.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans,
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {a}; Evitable en x = -{a}" : $"Infinite at x = {a}; Removable at x = -{a}",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = 0" : $"Removable at x = 0",
                            LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                            rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateMotionProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução paso a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 7);
                    int b = rng.Next(2, 12);
                    int c = rng.Next(1, 20);
                    int t0 = rng.Next(1, 8);
                    string formula = $"s(t) = {a}t^2 - {b}t + {c};\\; t = {t0}";
                    int v = 2 * a * t0 - b;
                    int acc = 2 * a;
                    string ans = $"v({t0}) = {v},\\; a({t0}) = {acc}";
                    string steps = $"{stepLabel}: $v(t) = s'(t) = {2 * a}t - {b} \\implies v({t0}) = {v};\\; a(t) = v'(t) = {acc} \\implies a({t0}) = {acc}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"v({t0}) = {v + 2},\\; a({t0}) = {acc}", $"v({t0}) = {v},\\; a({t0}) = {acc + 2}", $"v({t0}) = {acc},\\; a({t0}) = {v}", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    int t0 = rng.Next(1, 7);
                    string formula = $"s(t) = t^3 - {3 * k}t^2 + {3 * k * k}t;\\; t = {t0}";
                    int v = 3 * t0 * t0 - 6 * k * t0 + 3 * k * k;
                    int acc = 6 * t0 - 6 * k;
                    string ans = $"v({t0}) = {v},\\; a({t0}) = {acc}";
                    string steps = $"{stepLabel}: $v(t) = 3t^2 - {6 * k}t + {3 * k * k},\\; a(t) = 6t - {6 * k}$. Evaluando en $t = {t0}: v({t0}) = {v}, a({t0}) = {acc}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"v({t0}) = {v + 4},\\; a({t0}) = {acc}", $"v({t0}) = {acc},\\; a({t0}) = {v}", $"v({t0}) = 0,\\; a({t0}) = 0", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int b = a + rng.Next(1, 5);
                    int t0 = rng.Next(1, 8);
                    int sum = a + b;
                    int prod = a * b;
                    string formula = $"s(t) = 2t^3 - {3 * sum}t^2 + {6 * prod}t;\\; t = {t0}";
                    int v = 6 * t0 * t0 - 6 * sum * t0 + 6 * prod;
                    int acc = 12 * t0 - 6 * sum;
                    string ans = $"v({t0}) = {v},\\; a({t0}) = {acc}";
                    string steps = $"{stepLabel}: $v(t) = 6t^2 - {6 * sum}t + {6 * prod} = 6(t - {a})(t - {b});\\; a(t) = 12t - {6 * sum}$. Evaluando en $t = {t0}: v({t0}) = {v}, a({t0}) = {acc}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"v({t0}) = {v + 6},\\; a({t0}) = {acc}", $"v({t0}) = {acc},\\; a({t0}) = {v}", $"v({t0}) = 0,\\; a({t0}) = 0", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateRelatedRatesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a passo",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int dr = rng.Next(2, 10);
                    int r0 = rng.Next(3, 16);
                    string formula = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"El radio de un círculo aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su área cuando r = {r0} cm?",
                        AppLanguage.Portuguese => $"O raio de um círculo aumenta a {dr} cm/s. Com que rapidez sua área aumenta quando r = {r0} cm?",
                        _ => $"The radius of a circle increases at {dr} cm/s. How fast is its area increasing when r = {r0} cm?"
                    };
                    int ansVal = 2 * r0 * dr;
                    string ans = $"{ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}";
                    string steps = $"{stepLabel}: $A = \\pi r^2 \\implies \\frac{{dA}}{{dt}} = 2\\pi r\\frac{{dr}}{{dt}} = 2\\pi({r0})({dr}) = {ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{ansVal / 2}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{r0 * dr}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{ansVal * 2}\\pi\\text{{ cm}}^2\\text{{/s}}", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int dr = rng.Next(1, 8);
                    int r0 = rng.Next(2, 10);
                    string formula = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"El radio de una esfera aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su volumen cuando r = {r0} cm?",
                        AppLanguage.Portuguese => $"O raio de uma esfera aumenta a {dr} cm/s. Com que rapidez seu volume aumenta quando r = {r0} cm?",
                        _ => $"The radius of a sphere increases at {dr} cm/s. How fast is its volume increasing when r = {r0} cm?"
                    };
                    int ansVal = 4 * r0 * r0 * dr;
                    string ans = $"{ansVal}\\pi\\text{{ cm}}^3\\text{{/s}}";
                    string steps = $"{stepLabel}: $V = \\frac{{4}}{{3}}\\pi r^3 \\implies \\frac{{dV}}{{dt}} = 4\\pi r^2\\frac{{dr}}{{dt}} = 4\\pi({r0}^2)({dr}) = {ansVal}\\pi\\text{{ cm}}^3\\text{{/s}}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{ansVal / 2}\\pi\\text{{ cm}}^3\\text{{/s}}", $"{2 * r0 * dr}\\pi\\text{{ cm}}^3\\text{{/s}}", $"{ansVal * 2}\\pi\\text{{ cm}}^3\\text{{/s}}", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    var triples = new (int x, int y, int L)[]
                    {
                        (3, 4, 5), (6, 8, 10), (9, 12, 15), (12, 16, 20),
                        (5, 12, 13), (10, 24, 26), (8, 15, 17), (7, 24, 25),
                        (15, 20, 25), (4, 3, 5), (8, 6, 10), (12, 9, 15)
                    };
                    var tr = triples[rng.Next(triples.Length)];
                    int L = tr.L;
                    int x0 = tr.x;
                    int y0 = tr.y;
                    int dx = rng.Next(1, 7);
                    string formula = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Una escalera de {L} m se desliza por una pared. La base se aleja a {dx} m/s. ¿Con qué rapidez cae la parte superior cuando la base está a {x0} m?",
                        AppLanguage.Portuguese => $"Uma escada de {L} m desliza por uma parede. A base se afasta a {dx} m/s. Com que rapidez o topo desce quando a base está a {x0} m?",
                        _ => $"A {L} m ladder slides down a wall. The base moves away at {dx} m/s. How fast does the top slide down when the base is {x0} m away?"
                    };
                    double dyVal = Math.Round((double)x0 * dx / y0, 2);
                    string ans = $"-{dyVal}\\text{{ m/s}}";
                    string steps = $"{stepLabel}: $x^2 + y^2 = {L}^2 \\implies 2x\\frac{{dx}}{{dt}} + 2y\\frac{{dy}}{{dt}} = 0 \\implies \\frac{{dy}}{{dt}} = -\\frac{{x}}{{y}}\\frac{{dx}}{{dt}} = -\\frac{{{x0}}}{{{y0}}}({dx}) = -{dyVal}\\text{{ m/s}}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{dyVal}\\text{{ m/s}}", $"-{dx}\\text{{ m/s}}", $"-{dyVal * 2}\\text{{ m/s}}", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateLimitsDerivFormProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? (p?.Preset == "Hard" ? 2 : (p?.Preset == "Medium" ? 1 : 0));
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 25);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{({a} + h)^2 - {a * a}}}{{h}}";
                    int ansVal = 2 * a;
                    string ans = ansVal.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'(a)$ con $f(x) = x^2$ y $a = {a}$. $f'(x) = 2x \\implies f'({a}) = 2({a}) = {ansVal}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), a.ToString(), "0", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 15);
                    int k = rng.Next(2, 6);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{{k}({a} + h)^2 - {k * a * a}}}{{h}}";
                    int ansVal = 2 * k * a;
                    string ans = ansVal.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = {k}x^2$. $f'(x) = {2 * k}x \\implies f'({a}) = {ansVal}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + k).ToString(), (k * a).ToString(), "0", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 12);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{({a} + h)^3 - {a * a * a}}}{{h}}";
                    int ansVal = 3 * a * a;
                    string ans = ansVal.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = x^3$. $f'(x) = 3x^2 \\implies f'({a}) = 3({a}^2) = {ansVal}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 3).ToString(), (2 * a * a).ToString(), "0", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 12);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{\\frac{{1}}{{{a} + h}} - \\frac{{1}}{{{a}}}}}{{h}}";
                    string ans = $"-\\frac{{1}}{{{a * a}}}";
                    string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\frac{{1}}{{x}}$. $f'(x) = -\\frac{{1}}{{x^2}} \\implies f'({a}) = -\\frac{{1}}{{{a * a}}}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a * a}}}", $"-\\frac{{1}}{{{a}}}", "0", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int[] perf = { 1, 4, 9, 16, 25, 36, 49 };
                    int a = perf[rng.Next(perf.Length)];
                    int rootA = (int)Math.Sqrt(a);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{\\sqrt{{{a} + h}} - {rootA}}}{{h}}";
                    string ans = $"\\frac{{1}}{{{2 * rootA}}}";
                    string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\sqrt{{x}}$. $f'(x) = \\frac{{1}}{{2\\sqrt{{x}}}} \\implies f'({a}) = \\frac{{1}}{{2({rootA})}} = {ans}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{rootA}}}", rootA.ToString(), "0", rng);
                    return q;
                });
            }
            else
            {
                pool.Add(() =>
                {
                    int k = rng.Next(2, 10);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{\\sin({k}h)}}{{h}}";
                    string ans = k.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = \\sin({k}x)$. $f'(0) = {k}\\cos(0) = {k}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "\\infty", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 10);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{e^{{{k}h}} - 1}}{{h}}";
                    string ans = k.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = e^{{{k}x}}$. $f'(0) = {k}e^0 = {k}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "e", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 15);
                    string formula = $"\\lim_{{h \\to 0}} \\frac{{\\ln({a} + h) - \\ln({a})}}{{h}}";
                    string ans = $"\\frac{{1}}{{{a}}}";
                    string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\ln(x)$. $f'(x) = \\frac{{1}}{{x}} \\implies f'({a}) = \\frac{{1}}{{{a}}}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", a.ToString(), rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    string formula = $"\\lim_{{x \\to 0}} \\frac{{\\tan({k}x)}}{{x}}";
                    string ans = k.ToString();
                    string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = \\tan({k}x)$. $f'(x) = {k}\\sec^2({k}x) \\implies f'(0) = {k}(1)^2 = {k}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "\\infty", rng);
                    return q;
                });
            }

            int pick = (index + rng.Next(pool.Count)) % pool.Count;
            return pool[pick]();
        }
        #endregion
        #endregion
        #endregion
    }
}
