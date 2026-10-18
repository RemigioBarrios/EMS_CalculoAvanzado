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
            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución directa",
                AppLanguage.Portuguese => "Substituição direta",
                _ => "Direct substitution"
            };

            var pool = new List<Func<Question>>();

            if (topicId == "limits_jumps")
            {
                // 1. Absolute value ratio right-sided
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    int k = rng.Next(1, 5);
                    string aStr = a == 0 ? "0" : a.ToString();
                    string subExpr = a == 0 ? "x" : (a > 0 ? $"x - {a}" : $"x + {Math.Abs(a)}");
                    string kStr = k == 1 ? "" : $"{k}*";
                    string formula = $"lim [x -> {aStr}^+] ({kStr}|{subExpr}|) / ({subExpr})";
                    string ans = k.ToString();
                    string steps = $"Para $x > {aStr}$, $|{subExpr}| = {subExpr} \\Rightarrow \\lim_{{x \\to {aStr}^+}} \\frac{{{kStr}({subExpr})}}{{{subExpr}}} = {k}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_abs_right" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (-k).ToString(), "0", "Does not exist", rng);
                    return q;
                });

                // 2. Absolute value ratio left-sided
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    int k = rng.Next(1, 5);
                    string aStr = a == 0 ? "0" : a.ToString();
                    string subExpr = a == 0 ? "x" : (a > 0 ? $"x - {a}" : $"x + {Math.Abs(a)}");
                    string kStr = k == 1 ? "" : $"{k}*";
                    string formula = $"lim [x -> {aStr}^-] ({kStr}|{subExpr}|) / ({subExpr})";
                    string ans = (-k).ToString();
                    string steps = $"Para $x < {aStr}$, $|{subExpr}| = -({subExpr}) \\Rightarrow \\lim_{{x \\to {aStr}^-}} \\frac{{-{kStr}({subExpr})}}{{{subExpr}}} = -{k}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_abs_left" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, k.ToString(), "0", "Does not exist", rng);
                    return q;
                });

                // 3. Piecewise linear jump (two-sided limit does not exist)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int m1 = rng.Next(1, 3);
                    int b1 = rng.Next(1, 4);
                    int m2 = rng.Next(2, 4);
                    int b2 = rng.Next(4, 8);
                    int leftVal = m1 * a + b1;
                    int rightVal = m2 * a + b2;
                    if (leftVal == rightVal) rightVal += 3;
                    string formula = $"f(x) = \\begin{{cases}} {m1}x + {b1}, & x < {a} \\\\ {m2}x + {b2}, & x \\ge {a} \\end{{cases}};\\quad \\lim_{{x \\to {a}}} f(x)";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "No existe",
                        AppLanguage.Portuguese => "Não existe",
                        _ => "Does not exist"
                    };
                    string steps = $"\\lim_{{x \\to {a}^-}} ({m1}x + {b1}) = {leftVal},\\quad \\lim_{{x \\to {a}^+}} ({m2}x + {b2}) = {rightVal}.\\; \\text{{Como }} {leftVal} \\ne {rightVal},\\; \\lim_{{x \\to {a}}} f(x) \\text{{ no existe.}}";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_piecewise_2sided" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, leftVal.ToString(), rightVal.ToString(), "0", rng);
                    return q;
                });

                // 4. Piecewise quadratic vs linear (one-sided evaluation)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int c = rng.Next(1, 4);
                    int leftVal = a * a + c;
                    string formula = $"f(x) = \\begin{{cases}} x^2 + {c}, & x < {a} \\\\ 2x + 1, & x \\ge {a} \\end{{cases}};\\quad \\lim_{{x \\to {a}^-}} f(x)";
                    string ans = leftVal.ToString();
                    string steps = $"\\lim_{{x \\to {a}^-}} f(x) = ({a})^2 + {c} = {leftVal}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_piecewise_1sided" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (2 * a + 1).ToString(), (leftVal + 2).ToString(), "Does not exist", rng);
                    return q;
                });

                // 5. Floor function jump
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    bool isRight = rng.Next(2) == 0;
                    string dir = isRight ? "^+" : "^-";
                    string formula = $"\\lim_{{x \\to {a}{dir}}} \\lfloor x \\rfloor";
                    int ansVal = isRight ? a : a - 1;
                    string ans = ansVal.ToString();
                    string steps = isRight
                        ? $"Para {a} < x < {a + 1},\\; \\lfloor x \\rfloor = {a} \\implies \\lim_{{x \\to {a}^+}} \\lfloor x \\rfloor = {a}$"
                        : $"Para {a - 1} < x < {a},\\; \\lfloor x \\rfloor = {a - 1} \\implies \\lim_{{x \\to {a}^-}} \\lfloor x \\rfloor = {a - 1}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_floor" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (isRight ? a - 1 : a).ToString(), (a + 1).ToString(), "0", rng);
                    return q;
                });

                // 6. Continuous Kink / Corner (two-sided limit exists)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(2, 5);
                    int c = rng.Next(1, 6);
                    string formula = $"\\lim_{{x \\to {a}}} ({k}|x - {a}| + {c})";
                    string ans = c.ToString();
                    string steps = $"\\lim_{{x \\to {a}^-}} ({k}(-x + {a}) + {c}) = {c},\\quad \\lim_{{x \\to {a}^+}} ({k}(x - {a}) + {c}) = {c} \\implies {c}";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_jump_kink" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (c + k).ToString(), (c - k).ToString(), "Does not exist", rng);
                    return q;
                });
            }
            else if (topicId == "limits_holes")
            {
                // 1. Quadratic factoring
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                    int b = rng.Next(1, 5);
                    while (b == -a) b = rng.Next(1, 5);
                    int sum = b - a;
                    int prod = -a * b;
                    string sumPart = sum == 0 ? "" : (sum > 0 ? $"+ {sum}x " : $"- {Math.Abs(sum)}x ");
                    string prodPart = prod == 0 ? "" : (prod > 0 ? $"+ {prod}" : $"- {Math.Abs(prod)}");
                    string numExpr = $"x^2 {sumPart}{prodPart}".Trim();
                    string denExpr = a == 0 ? "x" : (a > 0 ? $"x - {a}" : $"x + {Math.Abs(a)}");
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{{numExpr}}}{{{denExpr}}}";
                    int ansVal = a + b;
                    string ans = ansVal.ToString();
                    string bSign = b >= 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                    string steps = $"Factorizando: $\\frac{{({denExpr})(x {bSign})}}{{{denExpr}}} = x {bSign} \\implies {a} {bSign} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_quad_factor" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), "0", rng);
                    return q;
                });

                // 2. Radical conjugate
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int a2 = a * a;
                    string formula = $"\\lim_{{x \\to 0}} \\frac{{\\sqrt{{x + {a2}}} - {a}}}{{x}}";
                    int den = 2 * a;
                    string ans = $"1/{den}";
                    string steps = $"Multiplicando por el conjugado: $\\lim_{{x \\to 0}} \\frac{{x}}{{x(\\sqrt{{x + {a2}}} + {a})}} = \\frac{{1}}{{{a} + {a}}} = \\frac{{1}}{{{den}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_radical_conjugate" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", $"1/{a}", "Does not exist", rng);
                    return q;
                });

                // 3. Trig sinc limit: sin(kx) / mx
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    int m = rng.Next(2, 5);
                    while (m == k) m = rng.Next(2, 5);
                    string formula = $"\\lim_{{x \\to 0}} \\frac{{\\sin({k}x)}}{{{m}x}}";
                    int g = Gcd(k, m);
                    int num = k / g;
                    int den = m / g;
                    string ans = den == 1 ? num.ToString() : $"{num}/{den}";
                    string steps = $"Forma 0/0: $\\lim_{{x \\to 0}} \\frac{{\\sin({k}x)}}{{{m}x}} = \\frac{{{k}}}{{{m}}} \\lim_{{x \\to 0}} \\frac{{\\sin({k}x)}}{{{k}x}} = \\frac{{{k}}}{{{m}}} \\cdot 1 = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_trig_sinc" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", $"{den}/{num}", rng);
                    return q;
                });

                // 4. Difference of cubes
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int a3 = a * a * a;
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{x^3 - {a3}}}{{x - {a}}}";
                    int ansVal = 3 * a * a;
                    string ans = ansVal.ToString();
                    string steps = $"Factorizando: $\\frac{{(x - {a})(x^2 + {a}x + {a * a})}}{{x - {a}}} = x^2 + {a}x + {a * a} \\implies ({a})^2 + ({a})^2 + ({a})^2 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_diff_cubes" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + a).ToString(), (a * a).ToString(), "0", rng);
                    return q;
                });

                // 5. Complex fraction
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{\\frac{{1}}{{x}} - \\frac{{1}}{{{a}}}}}{{x - {a}}}";
                    int den = a * a;
                    string ans = $"-1/{den}";
                    string steps = $"Simplificando: $\\frac{{\\frac{{{a} - x}}{{{a}x}}}}{{x - {a}}} = -\\frac{{1}}{{{a}x}} \\implies -\\frac{{1}}{{{a}({a})}} = -\\frac{{1}}{{{den}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_complex_fraction" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{den}", $"-1/{a}", "0", rng);
                    return q;
                });

                // 6. Exponential indeterminate: (e^{kx} - 1) / x
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\lim_{{x \\to 0}} \\frac{{e^{{{k}x}} - 1}}{{x}}";
                    string ans = k.ToString();
                    string steps = $"Forma 0/0 (definición de derivada de $e^{{{k}x}}$ en $x=0$): $\\lim_{{x \\to 0}} \\frac{{e^{{{k}x}} - 1}}{{x}} = {k}e^0 = {k}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_hole_exp_rate" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (2 * k).ToString(), rng);
                    return q;
                });
            }
            else if (topicId == "limits_essential")
            {
                // 1. Simple pole right-sided
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}^+}} \\frac{{{k}}}{{x - {a}}}";
                    string ans = "+\\infty";
                    string steps = $"Cuando $x \\to {a}^+$, el numerador tiende a ${k} > 0$ y el denominador tiende a $0^+$. Por tanto, el cociente tiende a $+\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_simple_right" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\infty", "0", "1", rng);
                    return q;
                });

                // 2. Simple pole left-sided
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}^-}} \\frac{{{k}}}{{x - {a}}}";
                    string ans = "-\\infty";
                    string steps = $"Cuando $x \\to {a}^-$, el numerador tiende a ${k} > 0$ y el denominador tiende a $0^-$. Por tanto, el cociente tiende a $-\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_simple_left" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "+\\infty", "0", "-1", rng);
                    return q;
                });

                // 3. Even power pole positive
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{{k}}}{{(x - {a})^2}}";
                    string ans = "+\\infty";
                    string steps = $"Para $x \\to {a}$, $(x - {a})^2 > 0$ por ambos lados y tiende a $0^+$. Como el numerador es ${k} > 0$, el límite bilateral es $+\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_even_pos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\infty", "0", "Does not exist", rng);
                    return q;
                });

                // 4. Even power pole negative
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{-{k}}}{{(x - {a})^2}}";
                    string ans = "-\\infty";
                    string steps = $"Para $x \\to {a}$, $(x - {a})^2 \\to 0^+$. Con numerador $-{k} < 0$, el límite bilateral es $-\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_even_neg" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "+\\infty", "0", "Does not exist", rng);
                    return q;
                });

                // 5. Two-sided simple pole DNE
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{{k}}}{{x - {a}}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "No existe",
                        AppLanguage.Portuguese => "Não existe",
                        _ => "Does not exist"
                    };
                    string steps = $"Límite por la izquierda: $\\lim_{{x \\to {a}^-}} = -\\infty$. Límite por la derecha: $\\lim_{{x \\to {a}^+}} = +\\infty$. Al diferir los límites laterales, el límite bilateral no existe.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_two_sided_dne" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "+\\infty", "-\\infty", "0", rng);
                    return q;
                });

                // 6. Linear numerator over zero denominator
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int c = rng.Next(2, 6);
                    string formula = $"\\lim_{{x \\to {a}^+}} \\frac{{x + {c}}}{{x - {a}}}";
                    string ans = "+\\infty";
                    int numVal = a + c;
                    string steps = $"Al evaluar en $x = {a}^+$: numerador tiende a ${numVal} > 0$ y denominador a $0^+$. Por tanto, el límite es $+\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_essential_linear_over_root" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\infty", numVal.ToString(), "0", rng);
                    return q;
                });
            }
            else if (topicId == "limits_infinity")
            {
                // 1. Rational same degree
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int d = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    int c = rng.Next(1, 7);
                    string formula = $"\\lim_{{x \\to \\infty}} \\frac{{{a}x^2 + {b}x}}{{{d}x^2 - {c}}}";
                    int g = Gcd(a, d);
                    int num = a / g;
                    int den = d / g;
                    string ans = den == 1 ? num.ToString() : $"{num}/{den}";
                    string steps = $"Dividiendo numerador y denominador por $x^2$: $\\lim_{{x \\to \\infty}} \\frac{{{a} + {b}/x}}{{{d} - {c}/x^2}} = \\frac{{{a} + 0}}{{{d} - 0}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_rat_equal_deg" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "+\\infty", $"{den}/{num}", rng);
                    return q;
                });

                // 2. Rational bottom heavy
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(1, 5);
                    int d = rng.Next(2, 6);
                    string formula = $"\\lim_{{x \\to \\infty}} \\frac{{{a}x + {b}}}{{{d}x^2 + 1}}";
                    string ans = "0";
                    string steps = $"El grado del denominador (2) es mayor que el del numerador (1). Dividiendo por $x^2$: $\\lim_{{x \\to \\infty}} \\frac{{{a}/x + {b}/x^2}}{{{d} + 1/x^2}} = \\frac{{0}}{{{d}}} = 0$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_rat_bottom_heavy" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}/{d}", "+\\infty", "1", rng);
                    return q;
                });

                // 3. Rational top heavy
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int d = rng.Next(2, 5);
                    string formula = $"\\lim_{{x \\to \\infty}} \\frac{{{a}x^3 + 1}}{{{d}x^2 + 4}}";
                    string ans = "+\\infty";
                    string steps = $"El grado del numerador (3) supera al del denominador (2) con coeficientes líderes positivos $\\implies +\\infty$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_rat_top_heavy" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", $"{a}/{d}", "-\\infty", rng);
                    return q;
                });

                // 4. Radical ratio at infinity
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int a2 = a * a;
                    int c = rng.Next(2, 6);
                    string formula = $"\\lim_{{x \\to \\infty}} \\frac{{\\sqrt{{{a2}x^2 + 1}}}}{{{c}x + 2}}";
                    int g = Gcd(a, c);
                    int num = a / g;
                    int den = c / g;
                    string ans = den == 1 ? num.ToString() : $"{num}/{den}";
                    string steps = $"Para $x > 0$, $\\sqrt{{{a2}x^2 + 1}} = x\\sqrt{{{a2} + 1/x^2}}$. Por tanto: $\\lim_{{x \\to \\infty}} \\frac{{x\\sqrt{{{a2} + 1/x^2}}}}{{x({c} + 2/x)}} = \\frac{{\\sqrt{{{a2}}}}}{{{c}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_radical_rat" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", $"{a2}/{c}", "+\\infty", rng);
                    return q;
                });

                // 5. Exponential at infinity
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int c = rng.Next(2, 5);
                    string formula = $"\\lim_{{x \\to \\infty}} \\frac{{{a}e^x + 3}}{{{c}e^x - 1}}";
                    int g = Gcd(a, c);
                    int num = a / g;
                    int den = c / g;
                    string ans = den == 1 ? num.ToString() : $"{num}/{den}";
                    string steps = $"Dividiendo por $e^x$: $\\lim_{{x \\to \\infty}} \\frac{{{a} + 3e^{{-x}}}}{{{c} - e^{{-x}}}} = \\frac{{{a}}}{{{c}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "-3", "+\\infty", rng);
                    return q;
                });

                // 6. Difference of radicals: sqrt(x^2 + 2kx) - x
                pool.Add(() =>
                {
                    int k = rng.Next(2, 7);
                    int twoK = 2 * k;
                    string formula = $"\\lim_{{x \\to \\infty}} \\left(\\sqrt{{x^2 + {twoK}x}} - x\\right)";
                    string ans = k.ToString();
                    string steps = $"Racionalizando: $\\frac{{(x^2 + {twoK}x) - x^2}}{{\\sqrt{{x^2 + {twoK}x}} + x}} = \\frac{{{twoK}x}}{{x(\\sqrt{{1 + {twoK}/x}} + 1)}} \\implies \\frac{{{twoK}}}{{1 + 1}} = {k}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_inf_diff_radicals" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", twoK.ToString(), "+\\infty", rng);
                    return q;
                });
            }
            else
            {
                // Default: limits_direct (Direct Evaluation with 7 distinct function families)
                // 1. Linear
                pool.Add(() =>
                {
                    int a = rng.Next(-3, 4);
                    int c1 = rng.Next(2, 6);
                    int c2 = rng.Next(1, 8) * (rng.Next(2) == 0 ? 1 : -1);
                    string c2Str = c2 >= 0 ? $"+ {c2}" : $"- {Math.Abs(c2)}";
                    string formula = $"\\lim_{{x \\to {a}}} ({c1}x {c2Str})";
                    int ansVal = c1 * a + c2;
                    string ans = ansVal.ToString();
                    string steps = $"{substLabel}: ${c1}({a}) {c2Str} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 3).ToString(), rng);
                    return q;
                });

                // 2. Quadratic
                pool.Add(() =>
                {
                    int a = rng.Next(-2, 3);
                    int b = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                    int c = rng.Next(-5, 6);
                    string bStr = b >= 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x";
                    string cStr = c >= 0 ? $"+ {c}" : $"- {Math.Abs(c)}";
                    string formula = $"\\lim_{{x \\to {a}}} (x^2 {bStr} {cStr})";
                    int ansVal = a * a + b * a + c;
                    string ans = ansVal.ToString();
                    string steps = $"{substLabel}: $({a})^2 + ({b})({a}) + ({c}) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 1).ToString(), (-ansVal).ToString(), (ansVal - 4).ToString(), rng);
                    return q;
                });

                // 3. Cubic monomial / binomial
                pool.Add(() =>
                {
                    int a = rng.Next(-2, 3);
                    int c = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}}} (x^3 - {c}x)";
                    int ansVal = a * a * a - c * a;
                    string ans = ansVal.ToString();
                    string steps = $"{substLabel}: $({a})^3 - {c}({a}) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_cubic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (-ansVal).ToString(), (ansVal - 2).ToString(), rng);
                    return q;
                });

                // 4. Radical direct
                pool.Add(() =>
                {
                    int[] radRoots = { 2, 3, 4, 5 };
                    int r = radRoots[rng.Next(radRoots.Length)];
                    int a = rng.Next(1, 4);
                    int c = r * r - 2 * a;
                    string cStr = c >= 0 ? $"+ {c}" : $"- {Math.Abs(c)}";
                    string formula = $"\\lim_{{x \\to {a}}} \\sqrt{{2x {cStr}}}";
                    string ans = r.ToString();
                    string steps = $"{substLabel}: $\\sqrt{{2({a}) {cStr}}} = \\sqrt{{{r * r}}} = {r}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_radical" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (r + 1).ToString(), (r - 1).ToString(), (r * 2).ToString(), rng);
                    return q;
                });

                // 5. Rational direct (non-zero denominator)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int num = rng.Next(2, 6) * a + 1;
                    int den = a + 2;
                    string formula = $"\\lim_{{x \\to {a}}} \\frac{{2x + 1}}{{x + 2}}";
                    int top = 2 * a + 1;
                    int g = Gcd(top, den);
                    string ans = (den / g == 1) ? $"{top / g}" : $"{top / g}/{den / g}";
                    string steps = $"{substLabel}: $\\frac{{2({a}) + 1}}{{{a} + 2}} = \\frac{{{top}}}{{{den}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_rational" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", "Does not exist", rng);
                    return q;
                });

                // 6. Exponential direct
                pool.Add(() =>
                {
                    int a = rng.Next(0, 3);
                    int c = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to {a}}} (e^{{x - {a}}} + {c})";
                    int ansVal = 1 + c;
                    string ans = ansVal.ToString();
                    string steps = $"{substLabel}: $e^{{{a} - {a}}} + {c} = e^0 + {c} = 1 + {c} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, c.ToString(), (ansVal + 2).ToString(), "e", rng);
                    return q;
                });

                // 7. Trigonometric direct
                pool.Add(() =>
                {
                    int c1 = rng.Next(1, 4);
                    int c2 = rng.Next(1, 5);
                    string formula = $"\\lim_{{x \\to 0}} ({c1}\\sin(x) + {c2}\\cos(x))";
                    string ans = c2.ToString();
                    string steps = $"{substLabel}: ${c1}\\sin(0) + {c2}\\cos(0) = 0 + {c2}(1) = {c2}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_direct_trig" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", c1.ToString(), (c1 + c2).ToString(), rng);
                    return q;
                });
            }

            // Select from all available archetypes according to index
            int chosenIdx = Math.Abs(index) % pool.Count;
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
            {
                poly = true;
                trig = true;
                exp = true;
                log = true;
                rat = true;
                rad = true;
                invTrig = true;
            }

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
                var polyTrigPool = new List<Func<Question>>();
                var polyExpPool = new List<Func<Question>>();
                var polyLogPool = new List<Func<Question>>();
                var trigExpPool = new List<Func<Question>>();
                var polyRadPool = new List<Func<Question>>();
                var polyPolyPool = new List<Func<Question>>();

                // 1. Poly * Trig (Simple & Composite)
                polyTrigPool.Add(() =>
                {
                    int a = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                    string aStr = a > 0 ? $"+ {a}" : $"- {Math.Abs(a)}";
                    string formula = $"f(x) = (x {aStr})*sin(x)";
                    string ans = $"f'(x) = sin(x) + (x {aStr})*cos(x)";
                    string steps = $"{ruleLabel}: $u = x {aStr},\\; v = \\sin(x) \\Rightarrow u'v + uv' = \\sin(x) + (x {aStr})\\cos(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_trig_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = (x {aStr})*cos(x)", $"f'(x) = sin(x) - (x {aStr})*cos(x)", $"f'(x) = cos(x)", rng);
                    return q;
                });
                polyTrigPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = (x^2 + {a})*sin(x)";
                    string ans = $"f'(x) = 2x*sin(x) + (x^2 + {a})*cos(x)";
                    string steps = $"{ruleLabel}: $u = x^2 + {a},\\; v = \\sin(x) \\Rightarrow u'v + uv' = 2x\\sin(x) + (x^2 + {a})\\cos(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_trig_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x*cos(x)", $"f'(x) = (x^2 + {a})*cos(x)", $"f'(x) = 2x*sin(x) - (x^2 + {a})*cos(x)", rng);
                    return q;
                });
                polyTrigPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"f(x) = (x^3 - 2x)*sin({k}x)";
                    string ans = $"f'(x) = (3x^2 - 2)*sin({k}x) + {k}(x^3 - 2x)*cos({k}x)";
                    string steps = $"{ruleLabel}: $u'v + uv' = (3x^2 - 2)\\sin({k}x) + {k}(x^3 - 2x)\\cos({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_trig_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 3x^2*sin({k}x) + (x^3 - 2x)*cos({k}x)", $"f'(x) = (3x^2 - 2)*cos({k}x)", $"f'(x) = {k}(3x^2 - 2)*cos({k}x)", rng);
                    return q;
                });
                polyTrigPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"f(x) = ({a}x)*cos(x)";
                    string ans = $"f'(x) = {a}*cos(x) - {a}x*sin(x)";
                    string steps = $"{ruleLabel}: ${a}\\cos(x) + ({a}x)(-\\sin(x)) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_trig_3" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = -{a}x*sin(x)", $"f'(x) = {a}*cos(x) + {a}x*sin(x)", $"f'(x) = {a}*sin(x)", rng);
                    return q;
                });

                // 2. Poly * Exp (Simple & Composite)
                polyExpPool.Add(() =>
                {
                    string formula = "f(x) = x*e^x";
                    string ans = "f'(x) = (x + 1)*e^x";
                    string steps = $"{ruleLabel}: $u = x,\\; v = e^x \\Rightarrow u'v + uv' = (1)e^x + x e^x = (x + 1)e^x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_exp_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = e^x", "f'(x) = x*e^x", "f'(x) = (x - 1)*e^x", rng);
                    return q;
                });
                polyExpPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = (x^2 + {a})*e^({k}x)";
                    string ans = $"f'(x) = ({k}x^2 + 2x + {k * a})*e^({k}x)";
                    string steps = $"{ruleLabel}: $2x e^{{{k}x}} + {k}(x^2 + {a})e^{{{k}x}} = ({k}x^2 + 2x + {k * a})e^{{{k}x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_exp_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x*{k}*e^({k}x)", $"f'(x) = (2x + {a})*e^({k}x)", $"f'(x) = (x^2 + {a})*e^({k}x)", rng);
                    return q;
                });
                polyExpPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"f(x) = (2x - {a})*e^x";
                    string ans = $"f'(x) = (2x + {2 - a})*e^x";
                    string steps = $"{ruleLabel}: $2e^x + (2x - {a})e^x = (2x + {2 - a})e^x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_exp_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2e^x", $"f'(x) = (2x - {a})*e^x", $"f'(x) = (2x + {a})*e^x", rng);
                    return q;
                });

                // 3. Poly * Log (Simple & Composite)
                polyLogPool.Add(() =>
                {
                    string formula = "f(x) = x*ln(x)";
                    string ans = "f'(x) = ln(x) + 1";
                    string steps = $"{ruleLabel}: $(1)\\ln(x) + x\\left(\\frac{{1}}{{x}}\\right) = \\ln(x) + 1$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_log_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = ln(x)", "f'(x) = 1/x", "f'(x) = x*ln(x) + 1", rng);
                    return q;
                });
                polyLogPool.Add(() =>
                {
                    int c = rng.Next(2, 5);
                    string formula = $"f(x) = (x^{{{c}}})*ln(x)";
                    string ans = $"f'(x) = {c}x^{{{c - 1}}}*ln(x) + x^{{{c - 1}}}";
                    string steps = $"{ruleLabel}: ${c}x^{{{c - 1}}}\\ln(x) + x^{{{c}}}\\left(\\frac{{1}}{{x}}\\right) = x^{{{c - 1}}}({c}\\ln(x) + 1)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_log_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c}x^{{{c - 1}}}*ln(x)", $"f'(x) = x^{{{c - 1}}}", $"f'(x) = {c}x^{{{c - 1}}} / x", rng);
                    return q;
                });

                // 4. Trig * Exp (Simple & Composite)
                trigExpPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"f(x) = e^({k}x)*cos(x)";
                    string ans = $"f'(x) = e^({k}x)*({k}*cos(x) - sin(x))";
                    string steps = $"{ruleLabel}: ${k}e^{{{k}x}}\\cos(x) + e^{{{k}x}}(-\\sin(x)) = e^{{{k}x}}({k}\\cos(x) - \\sin(x))$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_trig_exp_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = -{k}*e^({k}x)*sin(x)", $"f'(x) = e^({k}x)*({k}*cos(x) + sin(x))", $"f'(x) = e^({k}x)*cos(x)", rng);
                    return q;
                });
                trigExpPool.Add(() =>
                {
                    string formula = "f(x) = e^x*sin(x)";
                    string ans = "f'(x) = e^x*(sin(x) + cos(x))";
                    string steps = $"{ruleLabel}: $e^x\\sin(x) + e^x\\cos(x) = e^x(\\sin(x) + \\cos(x))$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_trig_exp_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = e^x*cos(x)", "f'(x) = e^x*(sin(x) - cos(x))", "f'(x) = e^x*sin(x)", rng);
                    return q;
                });

                // 5. Poly * Rad (Simple & Composite)
                polyRadPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = x*sqrt(x + {a})";
                    string ans = $"f'(x) = (3x + {2 * a}) / (2*sqrt(x + {a}))";
                    string steps = $"{ruleLabel}: $1\\cdot\\sqrt{{x + {a}}} + x\\cdot\\frac{{1}}{{2\\sqrt{{x + {a}}}}} = \\frac{{2(x + {a}) + x}}{{2\\sqrt{{x + {a}}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_rad_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = sqrt(x + {a})", $"f'(x) = x / (2*sqrt(x + {a}))", $"f'(x) = (3x + {a}) / (2*sqrt(x + {a}))", rng);
                    return q;
                });

                // 6. Poly * Poly (6 Distinct Archetypes)
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = (x + {a})*(x + {b})";
                    string ans = $"f'(x) = 2x + {a + b}";
                    string steps = $"{ruleLabel}: $1(x + {b}) + (x + {a})(1) = 2x + {a + b}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_linear_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x + {a * b}", $"f'(x) = x + {a + b}", "f'(x) = 2x", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = (x^2 + {a})*(2x - {b})";
                    string ans = $"f'(x) = 6x^2 - {2 * b}x + {2 * a}";
                    string steps = $"{ruleLabel}: $2x(2x - {b}) + 2(x^2 + {a}) = 6x^2 - {2 * b}x + {2 * a}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_quad_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 4x^2 - {2 * b}x", $"f'(x) = 6x^2 + {2 * a}", $"f'(x) = 2x*(2x - {b})", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int b = rng.Next(1, 4);
                    string formula = $"f(x) = (x^2 - {a})*(x^2 + {b})";
                    int diff = 2 * (b - a);
                    string diffStr = diff == 0 ? "" : (diff > 0 ? $" + {diff}x" : $" - {Math.Abs(diff)}x");
                    string ans = $"f'(x) = 4x^3{diffStr}";
                    string steps = $"{ruleLabel}: $2x(x^2 + {b}) + (x^2 - {a})(2x) = 4x^3{diffStr}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_quad_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 4x^3", $"f'(x) = 2x^3{diffStr}", $"f'(x) = 4x^2 - {a * b}", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    int c = rng.Next(2, 5);
                    int d = rng.Next(1, 5);
                    string formula = $"f(x) = ({a}x - {b})*({c}x + {d})";
                    int term1 = 2 * a * c;
                    int term2 = a * d - b * c;
                    string t2Str = term2 == 0 ? "" : (term2 > 0 ? $" + {term2}" : $" - {Math.Abs(term2)}");
                    string ans = $"f'(x) = {term1}x{t2Str}";
                    string steps = $"{ruleLabel}: ${a}({c}x + {d}) + ({a}x - {b})({c}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_affine_affine" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {term1}x", $"f'(x) = {a * c}x{t2Str}", $"f'(x) = {term1}x - {b * d}", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = x^2*({a}x - {b})";
                    int d1 = 3 * a;
                    int d2 = 2 * b;
                    string ans = $"f'(x) = {d1}x^2 - {d2}x";
                    string steps = $"{ruleLabel}: $2x({a}x - {b}) + x^2({a}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_quad_monomial_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {d1}x^2 + {d2}x", $"f'(x) = {2 * a}x^2 - {b}x", $"f'(x) = {d1}x^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int b = rng.Next(1, 4);
                    string formula = $"f(x) = (x^3 + {a})*(x + {b})";
                    string ans = $"f'(x) = 4x^3 + {3 * b}x^2 + {a}";
                    string steps = $"{ruleLabel}: $3x^2(x + {b}) + (x^3 + {a})(1) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_prod_poly_cubic_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 3x^3 + {3 * b}x^2", $"f'(x) = 4x^3 + {a}", $"f'(x) = 3x^2*(x + {b})", rng);
                    return q;
                });

                var activeCategories = new List<List<Func<Question>>>();
                if (trig && polyTrigPool.Count > 0) activeCategories.Add(polyTrigPool);
                if (exp && polyExpPool.Count > 0) activeCategories.Add(polyExpPool);
                if (log && polyLogPool.Count > 0) activeCategories.Add(polyLogPool);
                if (trig && exp && trigExpPool.Count > 0) activeCategories.Add(trigExpPool);
                if (rad && polyRadPool.Count > 0) activeCategories.Add(polyRadPool);
                if (poly && polyPolyPool.Count > 0) activeCategories.Add(polyPolyPool);

                if (activeCategories.Count == 0)
                {
                    if (polyPolyPool.Count > 0) activeCategories.Add(polyPolyPool);
                    else if (polyTrigPool.Count > 0) activeCategories.Add(polyTrigPool);
                    else if (polyExpPool.Count > 0) activeCategories.Add(polyExpPool);
                }

                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }

            // -------------------------------------------------------------
            // B. Quotient Rule (deriv_quotient)
            // -------------------------------------------------------------
            else if (topicId == "deriv_quotient")
            {
                var trigPolyPool = new List<Func<Question>>();
                var expPolyPool = new List<Func<Question>>();
                var logPolyPool = new List<Func<Question>>();
                var radPolyPool = new List<Func<Question>>();
                var polyPolyPool = new List<Func<Question>>();

                // 1. Trig / Poly (Simple & Composite)
                trigPolyPool.Add(() =>
                {
                    string formula = "f(x) = sin(x) / x";
                    string ans = "f'(x) = (x*cos(x) - sin(x)) / x^2";
                    string steps = $"{ruleLabel}: $\\frac{{\\cos(x)\\cdot x - \\sin(x)\\cdot 1}}{{x^2}} = \\frac{{x\\cos(x) - \\sin(x)}}{{x^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_trig_poly_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = cos(x) / 1", "f'(x) = (x*cos(x) + sin(x)) / x^2", "f'(x) = -sin(x) / x^2", rng);
                    return q;
                });
                trigPolyPool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string formula = $"f(x) = sin(x) / (x^2 + {k})";
                    string ans = $"f'(x) = ((x^2 + {k})*cos(x) - 2x*sin(x)) / (x^2 + {k})^2";
                    string steps = $"{ruleLabel}: $\\frac{{u'v - uv'}}{{v^2}} \\Rightarrow \\frac{{(x^2 + {k})\\cos(x) - 2x\\sin(x)}}{{(x^2 + {k})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_trig_poly_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = cos(x) / (2x)", $"f'(x) = ((x^2 + {k})*cos(x) + 2x*sin(x)) / (x^2 + {k})^2", $"f'(x) = -2x*sin(x) / (x^2 + {k})^2", rng);
                    return q;
                });
                trigPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = cos(x) / (x + {a})";
                    string ans = $"f'(x) = (-(x + {a})*sin(x) - cos(x)) / (x + {a})^2";
                    string steps = $"{ruleLabel}: $\\frac{{-\\sin(x)(x + {a}) - \\cos(x)(1)}}{{(x + {a})^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_trig_poly_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = -sin(x) / 1", $"f'(x) = (-(x + {a})*sin(x) + cos(x)) / (x + {a})^2", $"f'(x) = -cos(x) / (x + {a})^2", rng);
                    return q;
                });

                // 2. Exp / Poly (Simple & Composite)
                expPolyPool.Add(() =>
                {
                    string formula = "f(x) = e^x / x";
                    string ans = "f'(x) = (e^x*(x - 1)) / x^2";
                    string steps = $"{ruleLabel}: $\\frac{{e^x \\cdot x - e^x \\cdot 1}}{{x^2}} = \\frac{{e^x(x - 1)}}{{x^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_exp_poly_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = e^x / 1", "f'(x) = (e^x*(x + 1)) / x^2", "f'(x) = e^x / x^2", rng);
                    return q;
                });
                expPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    string formula = $"f(x) = e^(2x) / (x^2 + {a})";
                    string ans = $"f'(x) = (2*e^(2x)*(x^2 - x + {a})) / (x^2 + {a})^2";
                    string steps = $"{ruleLabel}: $\\frac{{2e^{{2x}}(x^2 + {a}) - e^{{2x}}(2x)}}{{(x^2 + {a})^2}} = \\frac{{2e^{{2x}}(x^2 - x + {a})}}{{(x^2 + {a})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_exp_poly_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2*e^(2x) / (2x)", $"f'(x) = e^(2x) / (x^2 + {a})^2", $"f'(x) = (2*e^(2x)*(x^2 + {a})) / (x^2 + {a})^2", rng);
                    return q;
                });
                expPolyPool.Add(() =>
                {
                    string formula = "f(x) = x / e^x";
                    string ans = "f'(x) = (1 - x) / e^x";
                    string steps = $"{ruleLabel}: $\\frac{{1\\cdot e^x - x e^x}}{{(e^x)^2}} = \\frac{{1 - x}}{{e^x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_exp_poly_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = (1 + x) / e^x", "f'(x) = 1 / e^x", "f'(x) = -x / e^x", rng);
                    return q;
                });

                // 3. Log / Poly (Simple & Composite)
                logPolyPool.Add(() =>
                {
                    string formula = "f(x) = ln(x) / x";
                    string ans = "f'(x) = (1 - ln(x)) / x^2";
                    string steps = $"{ruleLabel}: $\\frac{{(1/x)\\cdot x - \\ln(x)\\cdot 1}}{{x^2}} = \\frac{{1 - \\ln(x)}}{{x^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_log_poly_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 1 / x^2", "f'(x) = (1 + ln(x)) / x^2", "f'(x) = -ln(x) / x^2", rng);
                    return q;
                });
                logPolyPool.Add(() =>
                {
                    string formula = "f(x) = ln(x) / x^2";
                    string ans = "f'(x) = (1 - 2*ln(x)) / x^3";
                    string steps = $"{ruleLabel}: $\\frac{{(1/x)x^2 - \\ln(x)(2x)}}{{x^4}} = \\frac{{x(1 - 2\\ln(x))}}{{x^4}} = \\frac{{1 - 2\\ln(x)}}{{x^3}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_log_poly_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = (1 + 2*ln(x)) / x^3", "f'(x) = 1 / x^3", "f'(x) = -2*ln(x) / x^3", rng);
                    return q;
                });

                // 4. Rad / Poly (Simple & Composite)
                radPolyPool.Add(() =>
                {
                    string formula = "f(x) = sqrt(x) / (x + 1)";
                    string ans = "f'(x) = (1 - x) / (2*sqrt(x)*(x + 1)^2)";
                    string steps = $"{ruleLabel}: $\\frac{{\\frac{{1}}{{2\\sqrt{{x}}}}(x + 1) - \\sqrt{{x}}(1)}}{{(x + 1)^2}} = \\frac{{(x + 1) - 2x}}{{2\\sqrt{{x}}(x + 1)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_rad_poly_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 1 / (2*sqrt(x)*(x + 1))", "f'(x) = (1 + x) / (2*sqrt(x)*(x + 1)^2)", "f'(x) = -x / (x + 1)^2", rng);
                    return q;
                });

                // 5. Poly / Poly (6 Distinct Archetypes)
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = (x + {a}) / (x + {b})";
                    int num = b - a;
                    string ans = $"f'(x) = {num} / (x + {b})^2";
                    string steps = $"{ruleLabel}: $\\frac{{1(x + {b}) - (x + {a})(1)}}{{(x + {b})^2}} = \\frac{{{num}}}{{(x + {b})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_linear_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {num + 2} / (x + {b})^2", "f'(x) = 1", $"f'(x) = {-num} / (x + {b})^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = (x^2 + {a}) / (x - {b})";
                    string ans = $"f'(x) = (x^2 - {2 * b}x - {a}) / (x - {b})^2";
                    string steps = $"{ruleLabel}: $\\frac{{2x(x - {b}) - (x^2 + {a})}}{{(x - {b})^2}} = \\frac{{x^2 - {2 * b}x - {a}}}{{(x - {b})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_quad_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x / 1", $"f'(x) = (x^2 + {a}) / (x - {b})^2", $"f'(x) = (x^2 - {a}) / (x - {b})^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = x / (x^2 + {a})";
                    string ans = $"f'(x) = ({a} - x^2) / (x^2 + {a})^2";
                    string steps = $"{ruleLabel}: $\\frac{{1(x^2 + {a}) - x(2x)}}{{(x^2 + {a})^2}} = \\frac{{{a} - x^2}}{{(x^2 + {a})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_linear_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (2x)", $"f'(x) = ({a} + x^2) / (x^2 + {a})^2", $"f'(x) = -x^2 / (x^2 + {a})^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    int c = rng.Next(2, 5);
                    int d = rng.Next(1, 5);
                    string formula = $"f(x) = ({a}x - {b}) / ({c}x + {d})";
                    int num = a * d + b * c;
                    string ans = $"f'(x) = {num} / ({c}x + {d})^2";
                    string steps = $"{ruleLabel}: $\\frac{{{a}({c}x + {d}) - ({a}x - {b}){c}}}{{({c}x + {d})^2}} = \\frac{{{num}}}{{({c}x + {d})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_affine_affine" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a} / {c}", $"f'(x) = {num - 2} / ({c}x + {d})^2", $"f'(x) = -{num} / ({c}x + {d})^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = (x^2 - {a}) / (x^2 + {a})";
                    int numCoeff = 4 * a;
                    string ans = $"f'(x) = {numCoeff}x / (x^2 + {a})^2";
                    string steps = $"{ruleLabel}: $\\frac{{2x(x^2 + {a}) - (x^2 - {a})(2x)}}{{(x^2 + {a})^2}} = \\frac{{{numCoeff}x}}{{(x^2 + {a})^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_quad_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x / (x^2 + {a})^2", "f'(x) = 1", $"f'(x) = -{numCoeff}x / (x^2 + {a})^2", rng);
                    return q;
                });
                polyPolyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = (x^3 + {a}) / x^2";
                    string ans = $"f'(x) = (x^3 - {2 * a}) / x^3";
                    string steps = $"{ruleLabel}: $\\frac{{3x^2(x^2) - (x^3 + {a})(2x)}}{{x^4}} = \\frac{{x^4 - {2 * a}x}}{{x^4}} = \\frac{{x^3 - {2 * a}}}{{x^3}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_quot_poly_cubic_monomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 3x^2 / (2x)", $"f'(x) = (x^3 + {2 * a}) / x^3", $"f'(x) = 3x^2", rng);
                    return q;
                });

                var activeCategories = new List<List<Func<Question>>>();
                if (trig && trigPolyPool.Count > 0) activeCategories.Add(trigPolyPool);
                if (exp && expPolyPool.Count > 0) activeCategories.Add(expPolyPool);
                if (log && logPolyPool.Count > 0) activeCategories.Add(logPolyPool);
                if (rad && radPolyPool.Count > 0) activeCategories.Add(radPolyPool);
                if (poly && polyPolyPool.Count > 0) activeCategories.Add(polyPolyPool);

                if (activeCategories.Count == 0)
                    activeCategories.Add(polyPolyPool);

                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }

            // -------------------------------------------------------------
            // C. Chain Rule (deriv_chain)
            // -------------------------------------------------------------
            else if (topicId == "deriv_chain")
            {
                var polyPool = new List<Func<Question>>();
                var trigPool = new List<Func<Question>>();
                var expPool = new List<Func<Question>>();
                var logPool = new List<Func<Question>>();
                var radPool = new List<Func<Question>>();
                var invTrigPool = new List<Func<Question>>();

                // 1. Poly Chain (6 Distinct Archetypes)
                polyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int n = rng.Next(3, 6);
                    string formula = $"f(x) = (2x + {a})^{{{n}}}";
                    int coeff = 2 * n;
                    string ans = $"f'(x) = {coeff}*(2x + {a})^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}(2x + {a})^{{{n - 1}}} \\cdot (2) = {coeff}(2x + {a})^{{{n - 1}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_affine" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {n}*(2x + {a})^{{{n - 1}}}", $"f'(x) = 2*(2x + {a})^{{{n - 1}}}", $"f'(x) = {coeff}*(2x + {a})^{{{n}}}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int n = rng.Next(3, 6);
                    string formula = $"f(x) = (x^2 + {a})^{{{n}}}";
                    int coeff = 2 * n;
                    string ans = $"f'(x) = {coeff}x*(x^2 + {a})^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}(x^2 + {a})^{{{n - 1}}} \\cdot (2x) = {coeff}x(x^2 + {a})^{{{n - 1}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_quad_pure" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {n}*(x^2 + {a})^{{{n - 1}}}", $"f'(x) = 2x*(x^2 + {a})^{{{n - 1}}}", $"f'(x) = {coeff}*(x^2 + {a})^{{{n - 1}}}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int b = rng.Next(1, 4);
                    int c = rng.Next(1, 5);
                    int n = rng.Next(3, 5);
                    string formula = $"f(x) = ({a}x^2 + {b}x + {c})^{{{n}}}";
                    string innerD = $"{2 * a}x + {b}";
                    string ans = $"f'(x) = {n}({innerD})*({a}x^2 + {b}x + {c})^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}({a}x^2 + {b}x + {c})^{{{n - 1}}} \\cdot ({innerD}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_quad_full" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = ({innerD})*({a}x^2 + {b}x + {c})^{{{n - 1}}}", $"f'(x) = {n}*({a}x^2 + {b}x + {c})^{{{n - 1}}}", $"f'(x) = {n * 2 * a}x*({a}x^2 + {b}x + {c})^{{{n - 1}}}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    int n = rng.Next(2, 4);
                    string formula = $"f(x) = (x^3 - {k}x)^{{{n}}}";
                    string ans = n == 2
                        ? $"f'(x) = 2(3x^2 - {k})*(x^3 - {k}x)"
                        : $"f'(x) = {n}(3x^2 - {k})*(x^3 - {k}x)^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}(x^3 - {k}x)^{{{n - 1}}} \\cdot (3x^2 - {k}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_cubic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = (3x^2 - {k})*(x^3 - {k}x)^{{{n - 1}}}", $"f'(x) = {n}*(x^3 - {k}x)^{{{n - 1}}}", $"f'(x) = 3x^2*(x^3 - {k}x)^{{{n - 1}}}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = ({a} - x^2)^3";
                    string ans = $"f'(x) = -6x*({a} - x^2)^2";
                    string steps = $"{ruleLabel}: $3({a} - x^2)^2 \\cdot (-2x) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_inv_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 6x*({a} - x^2)^2", $"f'(x) = -2x*({a} - x^2)^2", $"f'(x) = 3({a} - x^2)^2", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    int n = rng.Next(2, 4);
                    string formula = $"f(x) = ({a}x + {b})^{{-{n}}}";
                    int coeff = -n * a;
                    string ans = $"f'(x) = {coeff}*({a}x + {b})^{{-{n + 1}}}";
                    string steps = $"{ruleLabel}: $-{n}({a}x + {b})^{{-{n + 1}}} \\cdot ({a}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_poly_neg_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {-n}*({a}x + {b})^{{-{n + 1}}}", $"f'(x) = {coeff}*({a}x + {b})^{{-{n}}}", $"f'(x) = {-coeff}*({a}x + {b})^{{-{n + 1}}}", rng);
                    return q;
                });

                // 2. Trig Chain (Simple & Composite)
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"f(x) = sin({k}x)";
                    string ans = $"f'(x) = {k}*cos({k}x)";
                    string steps = $"{ruleLabel}: $u = {k}x \\Rightarrow u'\\cos(u) = {k}\\cos({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_trig_sin_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = cos({k}x)", $"f'(x) = -{k}*cos({k}x)", $"f'(x) = {k}*sin({k}x)", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"f(x) = cos({k}x)";
                    string ans = $"f'(x) = -{k}*sin({k}x)";
                    string steps = $"{ruleLabel}: $u = {k}x \\Rightarrow -u'\\sin(u) = -{k}\\sin({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_trig_cos_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {k}*sin({k}x)", $"f'(x) = -cos({k}x)", $"f'(x) = -{k}*cos({k}x)", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    int c2 = rng.Next(1, 5);
                    string formula = $"f(x) = sin({c1}x^2 + {c2}x)";
                    string ans = $"f'(x) = ({2 * c1}x + {c2})*cos({c1}x^2 + {c2}x)";
                    string steps = $"{ruleLabel}: $u = {c1}x^2 + {c2}x \\Rightarrow u'\\cos(u) = ({2 * c1}x + {c2})\\cos({c1}x^2 + {c2}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_trig_sin_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = cos({c1}x^2 + {c2}x)", $"f'(x) = ({2 * c1}x + {c2})*sin({c1}x^2 + {c2}x)", $"f'(x) = {2 * c1}x*cos({c1}x^2 + {c2}x)", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int n = rng.Next(2, 4);
                    string formula = n == 2 ? "f(x) = sin^2(x)" : "f(x) = cos^3(x)";
                    string ans = n == 2 ? "f'(x) = 2*sin(x)*cos(x)" : "f'(x) = -3*cos^2(x)*sin(x)";
                    string steps = n == 2 ? $"{ruleLabel}: $2\\sin(x)\\cos(x)$" : $"{ruleLabel}: $3\\cos^2(x)(-\\sin(x)) = -3\\cos^2(x)\\sin(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_trig_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, n == 2 ? "f'(x) = 2*sin(x)" : "f'(x) = 3*cos^2(x)", "f'(x) = cos^2(x)", "f'(x) = sin(2x)", rng);
                    return q;
                });

                // 3. Exp Chain (Simple & Composite)
                expPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"f(x) = e^({k}x)";
                    string ans = $"f'(x) = {k}*e^({k}x)";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}e^{{kx}} = k e^{{kx}} = {k}e^{{{k}x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_exp_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = e^({k}x)", $"f'(x) = {k}x*e^({k - 1}x)", $"f'(x) = {k}*e^x", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    string formula = $"f(x) = e^({a}x^2 + 2x)";
                    string ans = $"f'(x) = ({2 * a}x + 2)*e^({a}x^2 + 2x)";
                    string steps = $"{ruleLabel}: $u = {a}x^2 + 2x \\Rightarrow u' e^u = ({2 * a}x + 2)e^{{{a}x^2 + 2x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_exp_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {2 * a}x*e^({a}x^2 + 2x)", $"f'(x) = e^({a}x^2 + 2x)", $"f'(x) = ({2 * a}x + 2)*e^x", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    string formula = "f(x) = e^(-x^2)";
                    string ans = "f'(x) = -2x*e^(-x^2)";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}e^{{-x^2}} = (-2x)e^{{-x^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_exp_gauss" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 2x*e^(-x^2)", "f'(x) = -e^(-x^2)", "f'(x) = -2*e^(-x^2)", rng);
                    return q;
                });

                // 4. Log Chain (Simple & Composite)
                logPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = ln({k}x + {b})";
                    string ans = $"f'(x) = {k} / ({k}x + {b})";
                    string steps = $"{ruleLabel}: $\\frac{{u'}}{{u}} = \\frac{{{k}}}{{{k}x + {b}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_log_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / ({k}x + {b})", $"f'(x) = {k} / x", $"f'(x) = {k}", rng);
                    return q;
                });
                logPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"f(x) = ln(x^2 + {a})";
                    string ans = $"f'(x) = 2x / (x^2 + {a})";
                    string steps = $"{ruleLabel}: $\\frac{{u'}}{{u}} = \\frac{{2x}}{{x^2 + {a}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_log_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (x^2 + {a})", $"f'(x) = 2x / x", "f'(x) = 2x", rng);
                    return q;
                });

                // 5. Rad Chain (Simple & Composite)
                radPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(1, 6);
                    string formula = $"f(x) = sqrt({a}x + {b})";
                    string ans = $"f'(x) = {a} / (2*sqrt({a}x + {b}))";
                    string steps = $"{ruleLabel}: $\\frac{{1}}{{2\\sqrt{{{a}x + {b}}}}} \\cdot ({a}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_rad_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (2*sqrt({a}x + {b}))", $"f'(x) = {a} / sqrt({a}x + {b})", "f'(x) = 1 / sqrt(x)", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    int a = rng.Next(1, 6);
                    string formula = $"f(x) = sqrt(x^2 + {a})";
                    string ans = $"f'(x) = x / sqrt(x^2 + {a})";
                    string steps = $"{ruleLabel}: $\\frac{{1}}{{2\\sqrt{{x^2 + {a}}}}} \\cdot 2x = \\frac{{x}}{{\\sqrt{{x^2 + {a}}}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_rad_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 2x / sqrt(x^2 + {a})", $"f'(x) = 1 / (2*sqrt(x^2 + {a}))", "f'(x) = x", rng);
                    return q;
                });

                // 6. InvTrig Chain (Simple & Composite)
                invTrigPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"f(x) = arctan({k}x)";
                    string ans = $"f'(x) = {k} / (1 + {k * k}x^2)";
                    string steps = $"{ruleLabel}: $\\frac{{u'}}{{1 + u^2}} = \\frac{{{k}}}{{1 + ({k}x)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_invtrig_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = 1 / (1 + {k * k}x^2)", $"f'(x) = {k} / (1 + x^2)", $"f'(x) = {k} / sqrt(1 - {k * k}x^2)", rng);
                    return q;
                });
                invTrigPool.Add(() =>
                {
                    string formula = "f(x) = arctan(x^2)";
                    string ans = "f'(x) = 2x / (1 + x^4)";
                    string steps = $"{ruleLabel}: $\\frac{{2x}}{{1 + (x^2)^2}} = \\frac{{2x}}{{1 + x^4}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_chain_invtrig_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "f'(x) = 1 / (1 + x^4)", "f'(x) = 2x / (1 + x^2)", "f'(x) = x / (1 + x^4)", rng);
                    return q;
                });

                var activeCategories = new List<List<Func<Question>>>();
                if (poly && polyPool.Count > 0) activeCategories.Add(polyPool);
                if (trig && trigPool.Count > 0) activeCategories.Add(trigPool);
                if (exp && expPool.Count > 0) activeCategories.Add(expPool);
                if (log && logPool.Count > 0) activeCategories.Add(logPool);
                if (rad && radPool.Count > 0) activeCategories.Add(radPool);
                if (invTrig && invTrigPool.Count > 0) activeCategories.Add(invTrigPool);

                if (activeCategories.Count == 0)
                    activeCategories.Add(polyPool);

                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }

            // -------------------------------------------------------------
            // D. Higher Order Derivatives (deriv_higher_order)
            // -------------------------------------------------------------
            else if (topicId == "deriv_higher_order")
            {
                var polyPool = new List<Func<Question>>();
                var trigPool = new List<Func<Question>>();
                var expPool = new List<Func<Question>>();
                var ratPool = new List<Func<Question>>();
                var radPool = new List<Func<Question>>();

                // 1. Poly Higher Order (6 Distinct Archetypes)
                polyPool.Add(() =>
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {c1 * p}x^{{{p - 1}}} + {d2}", $"f''(x) = {term1} - {d2}", $"f''(x) = {c1}x^{{{dp}}} + {d2}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"Find f''(x) for f(x) = ({a}x + {b})^3";
                    int ansCoeff = 6 * a * a;
                    string ans = $"f''(x) = {ansCoeff}*({a}x + {b})";
                    string steps = $"$f'(x) = 3({a}x + {b})^2({a}) = {3 * a}({a}x + {b})^2 \\Rightarrow f''(x) = {ansCoeff}({a}x + {b})$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_cube_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {3 * a}*({a}x + {b})^2", $"f''(x) = {6 * a}*({a}x + {b})", $"f''(x) = {ansCoeff}x", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int c = rng.Next(2, 5);
                    int d = rng.Next(2, 6);
                    string formula = $"Find f''(x) for f(x) = x^4 - {c}x^3 + {d}x";
                    int d2 = 6 * c;
                    string ans = $"f''(x) = 12x^2 - {d2}x";
                    string steps = $"$f'(x) = 4x^3 - {3 * c}x^2 + {d} \\Rightarrow f''(x) = 12x^2 - {d2}x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_quartic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = 4x^3 - {3 * c}x^2", $"f''(x) = 12x^2 + {d2}x", $"f''(x) = 12x^2 - {d2}x + {d}", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    int c = rng.Next(2, 5);
                    string formula = $"Find f''(x) for f(x) = {c}*(x - {a})^4";
                    int ansCoeff = 12 * c;
                    string ans = $"f''(x) = {ansCoeff}*(x - {a})^2";
                    string steps = $"$f'(x) = {4 * c}(x - {a})^3 \\Rightarrow f''(x) = {ansCoeff}(x - {a})^2$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_quartic_chain" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {4 * c}*(x - {a})^3", $"f''(x) = {ansCoeff}*(x - {a})", $"f''(x) = {ansCoeff}x^2", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"Find f''(x) for f(x) = x^3*(x + {a})";
                    int d2 = 6 * a;
                    string ans = $"f''(x) = 12x^2 + {d2}x";
                    string steps = $"$f(x) = x^4 + {a}x^3 \\Rightarrow f'(x) = 4x^3 + {3 * a}x^2 \\Rightarrow f''(x) = 12x^2 + {d2}x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_product_expansion" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = 4x^3 + {3 * a}x^2", $"f''(x) = 12x^2 - {d2}x", $"f''(x) = 6x*(x + {a})", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    int a = rng.Next(2, 4);
                    int b = rng.Next(1, 5);
                    string formula = $"Find f''(x) for f(x) = ({a}x^2 + {b})^2";
                    int term1 = 12 * a * a;
                    int term2 = 4 * a * b;
                    string ans = $"f''(x) = {term1}x^2 + {term2}";
                    string steps = $"$f(x) = {a * a}x^4 + {2 * a * b}x^2 + {b * b} \\Rightarrow f'(x) = {4 * a * a}x^3 + {4 * a * b}x \\Rightarrow f''(x) = {term1}x^2 + {term2}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_poly_quad_sq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {4 * a * a}x^3 + {4 * a * b}x", $"f''(x) = {term1}x^2", $"f''(x) = {2 * a}*({a}x^2 + {b})", rng);
                    return q;
                });

                // 2. Trig Higher Order (Simple & Composite)
                trigPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    int c2 = rng.Next(2, 5);
                    string formula = $"Find f''(x) for f(x) = {c1}x^3 + {c2}*cos(x)";
                    string ans = $"f''(x) = {6 * c1}x - {c2}*cos(x)";
                    string steps = $"$f'(x) = {3 * c1}x^2 - {c2}\\sin(x) \\Rightarrow f''(x) = {6 * c1}x - {c2}\\cos(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_trig_poly_cos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {6 * c1}x + {c2}*cos(x)", $"f''(x) = {3 * c1}x^2 - {c2}*sin(x)", $"f''(x) = {6 * c1}x - {c2}*sin(x)", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int k = rng.Next(2, 5);
                    string formula = $"Find f''(x) for f(x) = {a}*sin({k}x)";
                    int coeff = -a * k * k;
                    string ans = $"f''(x) = {coeff}*sin({k}x)";
                    string steps = $"$f'(x) = {a * k}\\cos({k}x) \\Rightarrow f''(x) = {coeff}\\sin({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_trig_sin_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {a * k}*cos({k}x)", $"f''(x) = {-coeff}*sin({k}x)", $"f''(x) = {coeff}*cos({k}x)", rng);
                    return q;
                });

                // 3. Exp Higher Order (Simple & Composite)
                expPool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    int a = rng.Next(1, 4);
                    string formula = $"Find f''(x) for f(x) = {a}*e^({k}x)";
                    int ansCoeff = a * k * k;
                    string ans = $"f''(x) = {ansCoeff}*e^({k}x)";
                    string steps = $"$f'(x) = {a * k}e^{{{k}x}} \\Rightarrow f''(x) = {ansCoeff}e^{{{k}x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_exp_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {a * k}*e^({k}x)", $"f''(x) = {a}*e^({k}x)", $"f''(x) = {ansCoeff}x*e^({k}x)", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"Find f''(x) for f(x) = {a}*e^x + 3x^2";
                    string ans = $"f''(x) = {a}*e^x + 6";
                    string steps = $"$f'(x) = {a}e^x + 6x \\Rightarrow f''(x) = {a}e^x + 6$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_exp_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {a}*e^x + 6x", $"f''(x) = {a}*e^x", $"f''(x) = {a}*e^x + 3", rng);
                    return q;
                });

                // 4. Rat Higher Order (Simple & Composite)
                ratPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"Find f''(x) for f(x) = {a} / x";
                    string ans = $"f''(x) = {2 * a} / x^3";
                    string steps = $"$f'(x) = -{a}x^{{-2}} \\Rightarrow f''(x) = {2 * a}x^{{-3}} = \\frac{{{2 * a}}}{{x^3}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_rat_recip" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = -{2 * a} / x^3", $"f''(x) = {a} / x^2", $"f''(x) = {2 * a} / x^2", rng);
                    return q;
                });

                // 5. Rad Higher Order (Simple & Composite)
                radPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"Find f''(x) for f(x) = {a}*sqrt(x)";
                    string ans = $"f''(x) = -{a} / (4*x*sqrt(x))";
                    string steps = $"$f'(x) = \\frac{{{a}}}{{2}}x^{{-1/2}} \\Rightarrow f''(x) = -\\frac{{{a}}}{{4}}x^{{-3/2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_higher_rad_sqrt" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f''(x) = {a} / (4*x*sqrt(x))", $"f''(x) = -{a} / (2*sqrt(x))", "f''(x) = 0", rng);
                    return q;
                });

                var activeCategories = new List<List<Func<Question>>>();
                if (poly && polyPool.Count > 0) activeCategories.Add(polyPool);
                if (trig && trigPool.Count > 0) activeCategories.Add(trigPool);
                if (exp && expPool.Count > 0) activeCategories.Add(expPool);
                if (rat && ratPool.Count > 0) activeCategories.Add(ratPool);
                if (rad && radPool.Count > 0) activeCategories.Add(radPool);

                if (activeCategories.Count == 0)
                    activeCategories.Add(polyPool);

                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }

            // -------------------------------------------------------------
            // E. Pure Trig (deriv_trig)
            // -------------------------------------------------------------
            else if (topicId == "deriv_trig")
            {
                // Simple trig variants
                pool.Add(() =>
                {
                    int a = rng.Next(2, 7);
                    int b = rng.Next(2, 7);
                    string formula = $"f(x) = {a}*sin(x) + {b}*cos(x)";
                    string ans = $"f'(x) = {a}*cos(x) - {b}*sin(x)";
                    string steps = $"{ruleLabel}: ${a}(\\cos(x)) + {b}(-\\sin(x)) = {a}\\cos(x) - {b}\\sin(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_lin_comb" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}*cos(x) + {b}*sin(x)", $"f'(x) = -{a}*sin(x) + {b}*cos(x)", $"f'(x) = {a}*sin(x) - {b}*cos(x)", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = {a}*tan(x)";
                    string ans = $"f'(x) = {a}*sec^2(x)";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}[{a}\\tan(x)] = {a}\\sec^2(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_tan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}*sec(x)", $"f'(x) = {a}*tan^2(x)", $"f'(x) = -{a}*sec^2(x)", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = {a}*sec(x)";
                    string ans = $"f'(x) = {a}*sec(x)*tan(x)";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}[{a}\\sec(x)] = {a}\\sec(x)\\tan(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_sec" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}*sec^2(x)", $"f'(x) = {a}*tan(x)", $"f'(x) = -{a}*sec(x)*tan(x)", rng);
                    return q;
                });

                // Composite trig variants
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    int a = rng.Next(2, 5);
                    string formula = $"f(x) = {a}*sin({k}x)";
                    string ans = $"f'(x) = {a * k}*cos({k}x)";
                    string steps = $"{ruleLabel}: ${a} \\cdot \\cos({k}x) \\cdot ({k}) = {a * k}\\cos({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_sin_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}*cos({k}x)", $"f'(x) = -{a * k}*cos({k}x)", $"f'(x) = {a * k}*sin({k}x)", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"f(x) = tan({k}x)";
                    string ans = $"f'(x) = {k}*sec^2({k}x)";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}\\tan(u) = u'\\sec^2(u) = {k}\\sec^2({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_tan_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = sec^2({k}x)", $"f'(x) = {k}*tan({k}x)", $"f'(x) = -{k}*sec^2({k}x)", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = cos({a}x^2 + {b})";
                    string ans = $"f'(x) = -{2 * a}x*sin({a}x^2 + {b})";
                    string steps = $"{ruleLabel}: $-\\sin({a}x^2 + {b}) \\cdot ({2 * a}x) = -{2 * a}x\\sin({a}x^2 + {b})$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_cos_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {2 * a}x*sin({a}x^2 + {b})", $"f'(x) = -sin({a}x^2 + {b})", $"f'(x) = -{2 * a}*sin({a}x^2 + {b})", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int n = rng.Next(2, 4);
                    string formula = n == 2 ? "f(x) = sin^2(x)" : "f(x) = cos^3(x)";
                    string ans = n == 2 ? "f'(x) = 2*sin(x)*cos(x)" : "f'(x) = -3*cos^2(x)*sin(x)";
                    string steps = n == 2
                        ? $"{ruleLabel}: $2\\sin(x)(\\cos(x)) = 2\\sin(x)\\cos(x)$"
                        : $"{ruleLabel}: $3\\cos^2(x)(-\\sin(x)) = -3\\cos^2(x)\\sin(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_trig_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, n == 2 ? "f'(x) = 2*sin(x)" : "f'(x) = 3*cos^2(x)", n == 2 ? "f'(x) = cos^2(x)" : "f'(x) = -3*sin^2(x)*cos(x)", "f'(x) = sin(2x)", rng);
                    return q;
                });

                int pick = Math.Abs(index) % pool.Count;
                return pool[pick]();
            }

            // -------------------------------------------------------------
            // F. Default / General Sum / Power Rule (deriv_power)
            // -------------------------------------------------------------
            else
            {
                var polyPool = new List<Func<Question>>();
                var ratPool = new List<Func<Question>>();
                var radPool = new List<Func<Question>>();
                var trigPool = new List<Func<Question>>();
                var expPool = new List<Func<Question>>();

                // 1. Polynomial Pool: 6 Distinct Archetypes
                polyPool.Add(() =>
                {
                    int p1 = comp == 0 ? rng.Next(2, 5) : (comp == 1 ? rng.Next(3, 6) : rng.Next(4, 8));
                    int c1 = rng.Next(2, 9) * (rng.Next(2) == 0 ? 1 : -1);
                    string formula = $"f(x) = {c1}x^{{{p1}}}";
                    int d1 = c1 * p1;
                    int dp1 = p1 - 1;
                    string ans = dp1 == 1 ? $"f'(x) = {d1}x" : $"f'(x) = {d1}x^{{{dp1}}}";
                    string steps = $"{ruleLabel}: $\\frac{{d}}{{dx}}({c1}x^{{{p1}}}) = {c1} \\cdot {p1} x^{{{dp1}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_monomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x^{{{dp1}}}", $"f'(x) = {d1}x^{{{p1}}}", $"f'(x) = {-d1}x^{{{dp1}}}", rng);
                    return q;
                });

                polyPool.Add(() =>
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_multiterm" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x^{{{dp1}}} + {c2}x^{{{dp2}}}", $"f'(x) = {d1}x^{{{p1}}} + {d2}x^{{{p2}}}", $"f'(x) = {-d1}x^{{{dp1}}}", rng);
                    return q;
                });

                polyPool.Add(() =>
                {
                    // Composite Power: (ax + b)^n
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                    string bStr = b > 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                    int n = comp == 0 ? rng.Next(3, 5) : rng.Next(4, 7);
                    int coeff = n * a;
                    string formula = $"f(x) = ({a}x {bStr})^{{{n}}}";
                    string ans = $"f'(x) = {coeff}*({a}x {bStr})^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}({a}x {bStr})^{{{n - 1}}} \\cdot ({a}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_affine_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {n}*({a}x {bStr})^{{{n - 1}}}", $"f'(x) = {a}*({a}x {bStr})^{{{n - 1}}}", $"f'(x) = {coeff}*({a}x {bStr})^{{{n}}}", rng);
                    return q;
                });

                polyPool.Add(() =>
                {
                    // Composite Power of Quadratic: (ax^2 + b)^n
                    int a = rng.Next(2, 4);
                    int b = rng.Next(1, 5);
                    int n = rng.Next(2, 4);
                    int dCoeff = 2 * a * n;
                    string formula = $"f(x) = ({a}x^2 + {b})^{{{n}}}";
                    string ans = n == 2
                        ? $"f'(x) = {dCoeff}x*({a}x^2 + {b})"
                        : $"f'(x) = {dCoeff}x*({a}x^2 + {b})^{{{n - 1}}}";
                    string steps = $"{ruleLabel}: ${n}({a}x^2 + {b})^{{{n - 1}}} \\cdot ({2 * a}x) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_quad_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {2 * a}x*({a}x^2 + {b})", $"f'(x) = {dCoeff}*({a}x^2 + {b})", $"f'(x) = {n}x*({a}x^2 + {b})", rng);
                    return q;
                });

                polyPool.Add(() =>
                {
                    // Full Cubic: ax^3 + bx^2 + cx + d
                    int c1 = rng.Next(1, 4);
                    int c2 = rng.Next(1, 5);
                    int c3 = rng.Next(1, 6);
                    string formula = $"f(x) = {c1}x^3 - {c2}x^2 + {c3}x";
                    string ans = $"f'(x) = {3 * c1}x^2 - {2 * c2}x + {c3}";
                    string steps = $"{ruleLabel}: ${3 * c1}x^2 - {2 * c2}x + {c3}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_cubic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {3 * c1}x^2 + {2 * c2}x + {c3}", $"f'(x) = {c1}x^2 - {c2}x", $"f'(x) = {3 * c1}x^3 - {2 * c2}x^2", rng);
                    return q;
                });

                polyPool.Add(() =>
                {
                    // Reciprocal power: c / x^p
                    int a = rng.Next(2, 6);
                    int p = rng.Next(2, 5);
                    string formula = $"f(x) = \\frac{{{a}}}{{x^{{{p}}}}}";
                    string ans = $"f'(x) = -\\frac{{{a * p}}}{{x^{{{p + 1}}}}}";
                    string steps = $"{ruleLabel}: ${a}x^{{-{p}}} \\Rightarrow -{a * p}x^{{-{p + 1}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_poly_recip_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = \\frac{{{a * p}}}{{x^{{{p + 1}}}}}", $"f'(x) = -\\frac{{{a}}}{{x^{{{p + 1}}}}}", $"f'(x) = -\\frac{{{a * p}}}{{x^{{{p}}}}}", rng);
                    return q;
                });

                // 2. Rational Pool (Simple & Composite)
                ratPool.Add(() =>
                {
                    // Simple Reciprocal
                    int a = rng.Next(1, 6);
                    int p = rng.Next(1, 4);
                    string formula = p == 1 ? $"f(x) = {a}/x" : $"f(x) = {a}/x^{{{p}}}";
                    string ans = $"f'(x) = -{a * p}/x^{{{p + 1}}}";
                    string steps = $"{ruleLabel}: ${a}x^{{-{p}}} \\Rightarrow -{a * p}x^{{-{p + 1}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_rat_simple" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a * p}/x^{{{p + 1}}}", $"f'(x) = -{a}/x^{{{p + 1}}}", $"f'(x) = -{a * p}/x^{{{p}}}", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    // Composite Rational
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"f(x) = {a} / ({b}x + 1)";
                    string ans = $"f'(x) = -{a * b} / ({b}x + 1)^2";
                    string steps = $"{ruleLabel}: ${a}({b}x + 1)^{{-1}} \\Rightarrow -{a}({b}x + 1)^{{-2}} \\cdot ({b}) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_rat_composite" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a * b} / ({b}x + 1)^2", $"f'(x) = -{a} / ({b}x + 1)^2", $"f'(x) = -{a * b} / ({b}x + 1)", rng);
                    return q;
                });

                // 3. Radical Pool (Simple & Composite)
                radPool.Add(() =>
                {
                    // Simple Radical
                    int a = rng.Next(2, 6);
                    string formula = $"f(x) = {a}*sqrt(x)";
                    string ans = (a % 2 == 0) ? $"f'(x) = {a / 2}/sqrt(x)" : $"f'(x) = {a}/(2*sqrt(x))";
                    string steps = $"{ruleLabel}: ${a} \\cdot \\frac{{1}}{{2}}x^{{-1/2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_rad_simple" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {a}/sqrt(x)", $"f'(x) = {a}*sqrt(x)", $"f'(x) = 1/(2*sqrt(x))", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    // Composite Radical
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 6);
                    string formula = $"f(x) = sqrt({a}x^2 + {b})";
                    string ans = $"f'(x) = {a}x / sqrt({a}x^2 + {b})";
                    string steps = $"{ruleLabel}: $\\frac{{1}}{{2\\sqrt{{{a}x^2 + {b}}}}} \\cdot ({2 * a}x) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_rad_composite" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {2 * a}x / sqrt({a}x^2 + {b})", $"f'(x) = 1 / (2*sqrt({a}x^2 + {b}))", $"f'(x) = {a}x", rng);
                    return q;
                });

                // 4. Trigonometric Pool (Simple Sums & Composite Sums)
                trigPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    int c2 = rng.Next(2, 5);
                    string formula = $"f(x) = {c1}x^2 + {c2}*sin(x)";
                    string ans = $"f'(x) = {2 * c1}x + {c2}*cos(x)";
                    string steps = $"{ruleLabel}: ${2 * c1}x + {c2}\\cos(x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_trig_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x + {c2}*cos(x)", $"f'(x) = {2 * c1}x - {c2}*cos(x)", $"f'(x) = {2 * c1}x + {c2}*sin(x)", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    int k = rng.Next(2, 4);
                    string formula = $"f(x) = {c1}x + cos({k}x)";
                    string ans = $"f'(x) = {c1} - {k}*sin({k}x)";
                    string steps = $"{ruleLabel}: ${c1} - {k}\\sin({k}x)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_trig_comp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1} + {k}*sin({k}x)", $"f'(x) = {c1} - sin({k}x)", $"f'(x) = -{k}*sin({k}x)", rng);
                    return q;
                });

                // 5. Exponential Pool (Simple Sums & Composite Sums)
                expPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    string formula = $"f(x) = {c1}x^2 + e^x";
                    string ans = $"f'(x) = {2 * c1}x + e^x";
                    string steps = $"{ruleLabel}: ${2 * c1}x + e^x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_exp_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1}x + e^x", $"f'(x) = {2 * c1}x + x*e^(x-1)", $"f'(x) = {2 * c1}x", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    int k = rng.Next(2, 4);
                    string formula = $"f(x) = {c1}x + e^({k}x)";
                    string ans = $"f'(x) = {c1} + {k}*e^({k}x)";
                    string steps = $"{ruleLabel}: ${c1} + {k}e^{{{k}x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "deriv_power_exp_comp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"f'(x) = {c1} + e^({k}x)", $"f'(x) = {c1}x + {k}*e^({k}x)", $"f'(x) = {k}*e^({k}x)", rng);
                    return q;
                });

                // Collect active categories based on user's selected checkboxes
                var activeCategories = new List<List<Func<Question>>>();
                if (poly && polyPool.Count > 0) activeCategories.Add(polyPool);
                if ((rat || log) && ratPool.Count > 0) activeCategories.Add(ratPool);
                if (rad && radPool.Count > 0) activeCategories.Add(radPool);
                if (trig && trigPool.Count > 0) activeCategories.Add(trigPool);
                if (exp && expPool.Count > 0) activeCategories.Add(expPool);

                if (activeCategories.Count == 0)
                    activeCategories.Add(polyPool);

                // Sample fairly across all selected function categories
                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }
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
            bool poly = HasPoly(p);

            if (!poly && !trig && !rat && !rad && !exp)
            {
                poly = true;
                trig = true;
                rat = true;
                rad = true;
                exp = true;
            }

            var availableTypes = new List<string>();
            if (poly && trig) availableTypes.Add("poly_trig");
            if (poly && rat) availableTypes.Add("poly_rat");
            if (poly && exp) availableTypes.Add("poly_exp");
            if (poly && rad) availableTypes.Add("poly_rad");
            if (poly) availableTypes.Add("poly");
            if (trig) availableTypes.Add("trig");
            if (rat) availableTypes.Add("rat");
            if (rad) availableTypes.Add("rad");
            if (exp) availableTypes.Add("exp");

            if (availableTypes.Count == 0) availableTypes.Add("poly");

            string chosenType = availableTypes[Math.Abs(index) % availableTypes.Count];
            string templateId = $"avg_rate_{chosenType}_{Math.Abs(index) / availableTypes.Count}";

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
                int ptArch = Math.Abs(index) % 6;
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
                int prArch = Math.Abs(index) % 4;
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
                int peArch = Math.Abs(index) % 3;
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
                int tArch = Math.Abs(index) % 3;
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
                else if (tArch == 1)
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
                else
                {
                    funcLatex = "\\tan(x)";
                    evalExpr = "tan(x)";
                    aStr = "0";
                    bStr = "\\frac{\\pi}{4}";
                    faStr = "0";
                    fbStr = "1";
                    rateLatex = "\\frac{4}{\\pi}";
                    secantLineLatex = "y = \\frac{4}{\\pi}x";
                    isIntVal = false;
                }
            }
            else if (chosenType == "rat")
            {
                int rArch = Math.Abs(index) % 3;
                if (rArch == 0)
                {
                    int c = 1;
                    int k = 6;
                    a = 1; b = 5;
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
                else if (rArch == 1)
                {
                    a = 1; b = 4;
                    aStr = "1"; bStr = "4";
                    funcLatex = "\\frac{4}{x}";
                    evalExpr = "4/x";
                    faStr = "4"; fbStr = "1";
                    intRate = -1;
                    rateLatex = "-1";
                    secantLineLatex = "y = -x + 5";
                    isIntVal = true;
                }
                else
                {
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    funcLatex = "\\frac{2}{x + 2}";
                    evalExpr = "2/(x+2)";
                    faStr = "1"; fbStr = "\\frac{1}{2}";
                    rateLatex = "-\\frac{1}{4}";
                    secantLineLatex = "y = -\\frac{1}{4}x + 1";
                    isIntVal = false;
                }
            }
            else if (chosenType == "rad")
            {
                int radArch = Math.Abs(index) % 3;
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
                else if (radArch == 1)
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
                else
                {
                    a = 0; b = 5;
                    aStr = "0"; bStr = "5";
                    funcLatex = "\\sqrt{x + 4}";
                    evalExpr = "sqrt(x+4)";
                    faStr = "2"; fbStr = "3";
                    rateLatex = "\\frac{1}{5}";
                    secantLineLatex = "y = \\frac{1}{5}x + 2";
                    isIntVal = false;
                }
            }
            else if (chosenType == "exp")
            {
                int eArch = Math.Abs(index) % 3;
                if (eArch == 0)
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
                else if (eArch == 1)
                {
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    funcLatex = "3^x";
                    evalExpr = "3^x";
                    faStr = "1"; fbStr = "9";
                    intRate = 4;
                    rateLatex = "4";
                    secantLineLatex = "y = 4x + 1";
                    isIntVal = true;
                }
                else
                {
                    a = 0; b = 1;
                    aStr = "0"; bStr = "1";
                    funcLatex = "4^x";
                    evalExpr = "4^x";
                    faStr = "1"; fbStr = "4";
                    intRate = 3;
                    rateLatex = "3";
                    secantLineLatex = "y = 3x + 1";
                    isIntVal = true;
                }
            }
            else
            {
                int polyArch = Math.Abs(index) % 6;
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
                else if (polyArch == 3)
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
                else if (polyArch == 4)
                {
                    int c = rng.Next(2, 5);
                    a = 1; b = 3;
                    aStr = "1"; bStr = "3";
                    funcLatex = $"{c}x^2";
                    evalExpr = $"{c}*x^2";
                    int fa = c * 1;
                    int fb = c * 9;
                    faStr = fa.ToString(); fbStr = fb.ToString();
                    intRate = 4 * c;
                    rateLatex = intRate.ToString();
                    int yInt = fa - intRate * a;
                    secantLineLatex = $"y = {intRate}x {(yInt >= 0 ? "+ " + yInt : "- " + Math.Abs(yInt))}";
                    isIntVal = true;
                }
                else
                {
                    a = 0; b = 2;
                    aStr = "0"; bStr = "2";
                    funcLatex = "x^4 - 2x^2";
                    evalExpr = "x^4 - 2*x^2";
                    faStr = "0"; fbStr = "8";
                    intRate = 4;
                    rateLatex = "4";
                    secantLineLatex = "y = 4x";
                    isIntVal = true;
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
                SolutionSteps = steps,
                TemplateId = templateId
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
            bool poly = HasPoly(p);

            if (!poly && !rat && !rad)
            {
                poly = true;
                rat = true;
                rad = true;
            }

            var available = new List<string>();
            if (poly) available.Add("poly");
            if (rat) available.Add("rat");
            if (rad) available.Add("rad");
            if (available.Count == 0) available.Add("poly");

            string chosen = available[Math.Abs(index) % available.Count];
            int subIndex = Math.Abs(index) / available.Count;

            string formula = "";
            string ans = "";
            string steps = "";
            string graphFormula = "";
            int arch = 0;

            if (chosen == "rat")
            {
                arch = subIndex % 3;
                if (arch == 0)
                {
                    int a = rng.Next(1, 4);
                    formula = $"f(x) = \\frac{{1}}{{x + {a}}}";
                    graphFormula = $"1/(x+{a})";
                    ans = $"f'(x) = -\\frac{{1}}{{(x + {a})^2}}";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{\\frac{{1}}{{x + h + {a}}} - \\frac{{1}}{{x + {a}}}}}{{h}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{(x + {a}) - (x + h + {a})}}{{h(x + h + {a})(x + {a})}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{-h}}{{h(x + h + {a})(x + {a})}} = -\\frac{{1}}{{(x + {a})^2}}$";
                }
                else if (arch == 1)
                {
                    formula = "f(x) = \\frac{1}{x}";
                    graphFormula = "1/x";
                    ans = "f'(x) = -\\frac{1}{x^2}";
                    steps = "$\\lim_{h \\to 0} \\frac{\\frac{1}{x+h} - \\frac{1}{x}}{h} = \\lim_{h \\to 0} \\frac{x - (x+h)}{h x (x+h)} = \\lim_{h \\to 0} \\frac{-h}{h x (x+h)} = -\\frac{1}{x^2}$";
                }
                else
                {
                    formula = "f(x) = \\frac{1}{2x + 1}";
                    graphFormula = "1/(2*x+1)";
                    ans = "f'(x) = -\\frac{2}{(2x + 1)^2}";
                    steps = "$\\lim_{h \\to 0} \\frac{\\frac{1}{2(x+h)+1} - \\frac{1}{2x+1}}{h} = \\lim_{h \\to 0} \\frac{(2x+1) - (2x+2h+1)}{h(2x+2h+1)(2x+1)} = -\\frac{2}{(2x+1)^2}$";
                }
            }
            else if (chosen == "rad")
            {
                arch = subIndex % 3;
                if (arch == 0)
                {
                    int a = rng.Next(1, 4);
                    formula = $"f(x) = \\sqrt{{x + {a}}}";
                    graphFormula = $"sqrt(x+{a})";
                    ans = $"f'(x) = \\frac{{1}}{{2\\sqrt{{x + {a}}}}}";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{\\sqrt{{x + h + {a}}} - \\sqrt{{x + {a}}}}}{{h}} \\cdot \\frac{{\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}}}}{{\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}}}} " +
                            $"= \\lim_{{h \\to 0}} \\frac{{(x + h + {a}) - (x + {a})}}{{h(\\sqrt{{x + h + {a}}} + \\sqrt{{x + {a}}})}} " +
                            $"= \\frac{{1}}{{2\\sqrt{{x + {a}}}}}$";
                }
                else if (arch == 1)
                {
                    formula = "f(x) = \\sqrt{x}";
                    graphFormula = "sqrt(x)";
                    ans = "f'(x) = \\frac{1}{2\\sqrt{x}}";
                    steps = "$\\lim_{h \\to 0} \\frac{\\sqrt{x+h} - \\sqrt{x}}{h} \\cdot \\frac{\\sqrt{x+h} + \\sqrt{x}}{\\sqrt{x+h} + \\sqrt{x}} = \\lim_{h \\to 0} \\frac{h}{h(\\sqrt{x+h}+\\sqrt{x})} = \\frac{1}{2\\sqrt{x}}$";
                }
                else
                {
                    formula = "f(x) = \\sqrt{2x + 1}";
                    graphFormula = "sqrt(2*x+1)";
                    ans = "f'(x) = \\frac{1}{\\sqrt{2x + 1}}";
                    steps = "$\\lim_{h \\to 0} \\frac{\\sqrt{2(x+h)+1} - \\sqrt{2x+1}}{h} \\cdot \\frac{\\sqrt{2x+2h+1} + \\sqrt{2x+1}}{\\sqrt{2x+2h+1} + \\sqrt{2x+1}} = \\frac{2}{2\\sqrt{2x+1}} = \\frac{1}{\\sqrt{2x+1}}$";
                }
            }
            else
            {
                arch = subIndex % 6;
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
                else if (arch == 3)
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
                else if (arch == 4)
                {
                    int c = rng.Next(1, 4);
                    formula = c == 1 ? "f(x) = x^3" : $"f(x) = {c}x^3";
                    graphFormula = $"{c}*x^3";
                    ans = c == 1 ? "f'(x) = 3x^2" : $"f'(x) = {3 * c}x^2";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{{c}(x+h)^3 - {c}x^3}}{{h}} = \\lim_{{h \\to 0}} \\frac{{{c}(3x^2h + 3xh^2 + h^3)}}{{h}} = {ans}$";
                }
                else
                {
                    int m = rng.Next(2, 7) * (rng.Next(2) == 0 ? 1 : -1);
                    int b = rng.Next(1, 8);
                    formula = $"f(x) = {m}x + {b}";
                    graphFormula = $"{m}*x + {b}";
                    ans = $"f'(x) = {m}";
                    steps = $"$\\lim_{{h \\to 0}} \\frac{{[{m}(x+h) + {b}] - [{m}x + {b}]}}{{h}} = \\lim_{{h \\to 0}} \\frac{{{m}h}}{{h}} = {m}$";
                }
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                GraphFormula = graphFormula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps,
                TemplateId = $"deriv_def_{chosen}_{arch}"
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
            bool poly = HasPoly(p);

            if (!poly && !trig && !rat && !rad && !exp)
            {
                poly = true;
                trig = true;
                rat = true;
                rad = true;
                exp = true;
            }

            var available = new List<string>();
            if (poly && trig) available.Add("poly_trig");
            if (poly && rat) available.Add("poly_rat");
            if (poly && exp) available.Add("poly_exp");
            if (poly && rad) available.Add("poly_rad");
            if (poly) available.Add("poly");
            if (trig) available.Add("trig");
            if (rat) available.Add("rat");
            if (rad) available.Add("rad");
            if (exp) available.Add("exp");

            if (available.Count == 0) available.Add("poly");

            string chosen = available[Math.Abs(index) % available.Count];
            int subIndex = Math.Abs(index) / available.Count;

            string formula = "";
            string ans = "";
            string steps = "";
            string graphFormula = "";
            int arch = 0;

            if (chosen == "poly_trig")
            {
                arch = subIndex % 4;
                if (arch == 0)
                {
                    formula = "f(x) = x^2 - 2\\cos(x);\\; x = 0";
                    graphFormula = "x^2 - 2*cos(x)";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = 2x + 2\\sin(x) \\implies f'(0) = 2(0) + 2(0) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = f'(0) = 0$"; }
                    else if (mode == 2) { ans = "y = -2"; steps = "$f(0) = -2,\\; m = 0 \\implies y = -2$"; }
                    else if (mode == 3) { ans = "x = 0"; steps = "$m = 0 \\implies$ recta normal vertical: $x = 0$"; }
                    else { ans = "(0, -2)"; steps = "$f'(x) = 2x + 2\\sin(x) = 0 \\implies x = 0,\\; f(0) = -2 \\implies (0, -2)$"; }
                }
                else if (arch == 1)
                {
                    formula = "f(x) = 3x + \\sin(x);\\; x = 0";
                    graphFormula = "3*x + sin(x)";
                    if (mode == 0) { ans = "4"; steps = "$f'(x) = 3 + \\cos(x) \\implies f'(0) = 3 + 1 = 4$"; }
                    else if (mode == 1) { ans = "m = 4"; steps = "Pendiente: $m = f'(0) = 4$"; }
                    else if (mode == 2) { ans = "y = 4x"; steps = "$f(0) = 0,\\; m = 4 \\implies y - 0 = 4(x - 0) \\implies y = 4x$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{4}x"; steps = "$m_{\\perp} = -\\frac{1}{4} \\implies y = -\\frac{1}{4}x$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = 3 + \\cos(x) \\ge 2 > 0$"; }
                }
                else if (arch == 2)
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
                arch = subIndex % 3;
                if (arch == 0)
                {
                    formula = "f(x) = 2\\sin(x);\\; x = 0";
                    graphFormula = "2*sin(x)";
                    if (mode == 0) { ans = "2"; steps = "$f'(x) = 2\\cos(x) \\implies f'(0) = 2(1) = 2$"; }
                    else if (mode == 1) { ans = "m = 2"; steps = "Pendiente: $m = f'(0) = 2\\cos(0) = 2$"; }
                    else if (mode == 2) { ans = "y = 2x"; steps = "$f(0) = 0,\\; f'(0) = 2 \\implies y - 0 = 2(x - 0) \\implies y = 2x$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{2}x"; steps = "$m_{\\perp} = -\\frac{1}{2} \\implies y - 0 = -\\frac{1}{2}(x - 0) \\implies y = -\\frac{1}{2}x$"; }
                    else { ans = "x = \\frac{\\pi}{2} + k\\pi"; steps = "$f'(x) = 2\\cos(x) = 0 \\implies x = \\frac{\\pi}{2} + k\\pi$"; }
                }
                else if (arch == 1)
                {
                    formula = "f(x) = \\cos(x);\\; x = \\pi";
                    graphFormula = "cos(x)";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = -\\sin(x) \\implies f'(\\pi) = -\\sin(\\pi) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = f'(\\pi) = -\\sin(\\pi) = 0$"; }
                    else if (mode == 2) { ans = "y = -1"; steps = "$f(\\pi) = -1,\\; m = 0 \\implies y = -1$"; }
                    else if (mode == 3) { ans = "x = \\pi"; steps = "$m = 0 \\implies$ la recta normal es vertical: $x = \\pi$"; }
                    else { ans = "x = k\\pi"; steps = "$f'(x) = -\\sin(x) = 0 \\implies x = k\\pi$"; }
                }
                else
                {
                    formula = "f(x) = \\sin(2x);\\; x = \\frac{\\pi}{4}";
                    graphFormula = "sin(2*x)";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = 2\\cos(2x) \\implies f'(\\frac{\\pi}{4}) = 2\\cos(\\frac{\\pi}{2}) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = 0$"; }
                    else if (mode == 2) { ans = "y = 1"; steps = "$f(\\frac{\\pi}{4}) = 1,\\; m = 0 \\implies y = 1$"; }
                    else if (mode == 3) { ans = "x = \\frac{\\pi}{4}"; steps = "Normal vertical: $x = \\frac{\\pi}{4}$"; }
                    else { ans = "x = \\frac{\\pi}{4} + \\frac{k\\pi}{2}"; steps = "$f'(x) = 0 \\implies 2x = \\frac{\\pi}{2} + k\\pi \\implies x = \\frac{\\pi}{4} + \\frac{k\\pi}{2}$"; }
                }
            }
            else if (chosen == "rat")
            {
                arch = subIndex % 3;
                if (arch == 0)
                {
                    formula = "f(x) = \\frac{4}{x};\\; x = 2";
                    graphFormula = "4/x";
                    if (mode == 0) { ans = "-1"; steps = "$f'(x) = -\\frac{4}{x^2} \\implies f'(2) = -\\frac{4}{4} = -1$"; }
                    else if (mode == 1) { ans = "m = -1"; steps = "Pendiente: $m = f'(2) = -1$"; }
                    else if (mode == 2) { ans = "y = -x + 4"; steps = "$f(2) = 2,\\; m = -1 \\implies y - 2 = -1(x - 2) \\implies y = -x + 4$"; }
                    else if (mode == 3) { ans = "y = x"; steps = "$m_{\\perp} = 1 \\implies y - 2 = 1(x - 2) \\implies y = x$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = -\\frac{4}{x^2} \\ne 0$ para todo $x \\ne 0$"; }
                }
                else if (arch == 1)
                {
                    formula = "f(x) = \\frac{1}{x + 1};\\; x = 1";
                    graphFormula = "1/(x+1)";
                    if (mode == 0) { ans = "-\\frac{1}{4}"; steps = "$f'(x) = -\\frac{1}{(x+1)^2} \\implies f'(1) = -\\frac{1}{4}$"; }
                    else if (mode == 1) { ans = "m = -\\frac{1}{4}"; steps = "Pendiente: $m = f'(1) = -\\frac{1}{4}$"; }
                    else if (mode == 2) { ans = "y = -\\frac{1}{4}x + \\frac{3}{4}"; steps = "$f(1) = \\frac{1}{2},\\; m = -\\frac{1}{4} \\implies y = -\\frac{1}{4}x + \\frac{3}{4}$"; }
                    else if (mode == 3) { ans = "y = 4x - \\frac{7}{2}"; steps = "$m_{\\perp} = 4 \\implies y - \\frac{1}{2} = 4(x - 1)$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) \\ne 0$"; }
                }
                else
                {
                    formula = "f(x) = \\frac{6}{x};\\; x = 3";
                    graphFormula = "6/x";
                    if (mode == 0) { ans = "-\\frac{2}{3}"; steps = "$f'(x) = -\\frac{6}{x^2} \\implies f'(3) = -\\frac{6}{9} = -\\frac{2}{3}$"; }
                    else if (mode == 1) { ans = "m = -\\frac{2}{3}"; steps = "Pendiente: $m = -\\frac{2}{3}$"; }
                    else if (mode == 2) { ans = "y = -\\frac{2}{3}x + 4"; steps = "$f(3) = 2 \\implies y - 2 = -\\frac{2}{3}(x - 3) \\implies y = -\\frac{2}{3}x + 4$"; }
                    else if (mode == 3) { ans = "y = \\frac{3}{2}x - \\frac{5}{2}"; steps = "$m_{\\perp} = \\frac{3}{2} \\implies y - 2 = \\frac{3}{2}(x - 3)$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) \\ne 0$"; }
                }
            }
            else if (chosen == "rad")
            {
                arch = subIndex % 3;
                if (arch == 0)
                {
                    formula = "f(x) = \\sqrt{x};\\; x = 4";
                    graphFormula = "sqrt(x)";
                    if (mode == 0) { ans = "\\frac{1}{4}"; steps = "$f'(x) = \\frac{1}{2\\sqrt{x}} \\implies f'(4) = \\frac{1}{2(2)} = \\frac{1}{4}$"; }
                    else if (mode == 1) { ans = "m = \\frac{1}{4}"; steps = "Pendiente: $m = f'(4) = \\frac{1}{4}$"; }
                    else if (mode == 2) { ans = "y = \\frac{1}{4}x + 1"; steps = "$f(4) = 2,\\; m = \\frac{1}{4} \\implies y - 2 = \\frac{1}{4}(x - 4) \\implies y = \\frac{1}{4}x + 1$"; }
                    else if (mode == 3) { ans = "y = -4x + 18"; steps = "$m_{\\perp} = -4 \\implies y - 2 = -4(x - 4) \\implies y = -4x + 18$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = \\frac{1}{2\\sqrt{x}} > 0$ en su dominio"; }
                }
                else if (arch == 1)
                {
                    formula = "f(x) = \\sqrt{2x + 1};\\; x = 4";
                    graphFormula = "sqrt(2*x+1)";
                    if (mode == 0) { ans = "\\frac{1}{3}"; steps = "$f'(x) = \\frac{1}{\\sqrt{2x+1}} \\implies f'(4) = \\frac{1}{\\sqrt{9}} = \\frac{1}{3}$"; }
                    else if (mode == 1) { ans = "m = \\frac{1}{3}"; steps = "Pendiente: $m = \\frac{1}{3}$"; }
                    else if (mode == 2) { ans = "y = \\frac{1}{3}x + \\frac{5}{3}"; steps = "$f(4) = 3 \\implies y - 3 = \\frac{1}{3}(x - 4) \\implies y = \\frac{1}{3}x + \\frac{5}{3}$"; }
                    else if (mode == 3) { ans = "y = -3x + 15"; steps = "$m_{\\perp} = -3 \\implies y - 3 = -3(x - 4) \\implies y = -3x + 15$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = \\frac{1}{\\sqrt{2x+1}} > 0$"; }
                }
                else
                {
                    formula = "f(x) = \\sqrt{x + 5};\\; x = 4";
                    graphFormula = "sqrt(x+5)";
                    if (mode == 0) { ans = "\\frac{1}{6}"; steps = "$f'(x) = \\frac{1}{2\\sqrt{x+5}} \\implies f'(4) = \\frac{1}{2(3)} = \\frac{1}{6}$"; }
                    else if (mode == 1) { ans = "m = \\frac{1}{6}"; steps = "Pendiente: $m = \\frac{1}{6}$"; }
                    else if (mode == 2) { ans = "y = \\frac{1}{6}x + \\frac{7}{3}"; steps = "$f(4) = 3 \\implies y - 3 = \\frac{1}{6}(x - 4) \\implies y = \\frac{1}{6}x + \\frac{7}{3}$"; }
                    else if (mode == 3) { ans = "y = -6x + 27"; steps = "$m_{\\perp} = -6 \\implies y - 3 = -6(x - 4) \\implies y = -6x + 27$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) > 0$"; }
                }
            }
            else if (chosen == "exp")
            {
                arch = subIndex % 3;
                if (arch == 0)
                {
                    formula = "f(x) = e^x;\\; x = 0";
                    graphFormula = "exp(x)";
                    if (mode == 0) { ans = "1"; steps = "$f'(x) = e^x \\implies f'(0) = 1$"; }
                    else if (mode == 1) { ans = "m = 1"; steps = "Pendiente: $m = f'(0) = 1$"; }
                    else if (mode == 2) { ans = "y = x + 1"; steps = "$f(0) = 1,\\; m = 1 \\implies y = x + 1$"; }
                    else if (mode == 3) { ans = "y = -x + 1"; steps = "$m_{\\perp} = -1 \\implies y - 1 = -1(x - 0) \\implies y = -x + 1$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$f'(x) = e^x > 0$"; }
                }
                else if (arch == 1)
                {
                    formula = "f(x) = e^{2x};\\; x = 0";
                    graphFormula = "exp(2*x)";
                    if (mode == 0) { ans = "2"; steps = "$f'(x) = 2e^{2x} \\implies f'(0) = 2$"; }
                    else if (mode == 1) { ans = "m = 2"; steps = "Pendiente: $m = 2$"; }
                    else if (mode == 2) { ans = "y = 2x + 1"; steps = "$f(0) = 1,\\; m = 2 \\implies y = 2x + 1$"; }
                    else if (mode == 3) { ans = "y = -\\frac{1}{2}x + 1"; steps = "$m_{\\perp} = -\\frac{1}{2} \\implies y = -\\frac{1}{2}x + 1$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$2e^{2x} > 0$"; }
                }
                else
                {
                    formula = "f(x) = e^{-x};\\; x = 0";
                    graphFormula = "exp(-x)";
                    if (mode == 0) { ans = "-1"; steps = "$f'(x) = -e^{-x} \\implies f'(0) = -1$"; }
                    else if (mode == 1) { ans = "m = -1"; steps = "Pendiente: $m = -1$"; }
                    else if (mode == 2) { ans = "y = -x + 1"; steps = "$f(0) = 1,\\; m = -1 \\implies y = -x + 1$"; }
                    else if (mode == 3) { ans = "y = x + 1"; steps = "$m_{\\perp} = 1 \\implies y = x + 1$"; }
                    else { ans = "\\text{No existen tangentes horizontales}"; steps = "$-e^{-x} \\ne 0$"; }
                }
            }
            else
            {
                arch = subIndex % 6;
                if (arch == 1)
                {
                    // Cubic x^3 - 3x at x = 1
                    formula = "f(x) = x^3 - 3x;\\; x = 1";
                    graphFormula = "x^3 - 3*x";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = 3x^2 - 3 \\implies f'(1) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = 0$"; }
                    else if (mode == 2) { ans = "y = -2"; steps = "$f(1) = -2,\\; m = 0 \\implies y = -2$"; }
                    else if (mode == 3) { ans = "x = 1"; steps = "Normal vertical: $x = 1$"; }
                    else { ans = "x = \\pm 1"; steps = "$3x^2 - 3 = 0 \\implies x = \\pm 1$"; }
                }
                else if (arch == 2)
                {
                    // Monomial 3x^2 at x = 2
                    formula = "f(x) = 3x^2;\\; x = 2";
                    graphFormula = "3*x^2";
                    if (mode == 0) { ans = "12"; steps = "$f'(x) = 6x \\implies f'(2) = 12$"; }
                    else if (mode == 1) { ans = "m = 12"; steps = "Pendiente: $m = 12$"; }
                    else if (mode == 2) { ans = "y = 12x - 12"; steps = "$f(2) = 12,\\; m = 12 \\implies y - 12 = 12(x - 2) \\implies y = 12x - 12$"; }
                    else if (mode == 3) { ans = "y - 12 = -\\frac{1}{12}(x - 2)"; steps = "Normal: $y - 12 = -\\frac{1}{12}(x - 2)$"; }
                    else { ans = "(0, 0)"; steps = "$6x = 0 \\implies x = 0$"; }
                }
                else if (arch == 3)
                {
                    // -x^2 + 4 at x = 1
                    formula = "f(x) = -x^2 + 4;\\; x = 1";
                    graphFormula = "-x^2 + 4";
                    if (mode == 0) { ans = "-2"; steps = "$f'(x) = -2x \\implies f'(1) = -2$"; }
                    else if (mode == 1) { ans = "m = -2"; steps = "Pendiente: $m = -2$"; }
                    else if (mode == 2) { ans = "y = -2x + 5"; steps = "$f(1) = 3,\\; m = -2 \\implies y - 3 = -2(x - 1) \\implies y = -2x + 5$"; }
                    else if (mode == 3) { ans = "y - 3 = \\frac{1}{2}(x - 1)"; steps = "Normal: $y - 3 = \\frac{1}{2}(x - 1)$"; }
                    else { ans = "(0, 4)"; steps = "$-2x = 0 \\implies x = 0$"; }
                }
                else if (arch == 4)
                {
                    // Quartic x^4 - 2x^2 at x = 1
                    formula = "f(x) = x^4 - 2x^2;\\; x = 1";
                    graphFormula = "x^4 - 2*x^2";
                    if (mode == 0) { ans = "0"; steps = "$f'(x) = 4x^3 - 4x \\implies f'(1) = 0$"; }
                    else if (mode == 1) { ans = "m = 0"; steps = "Pendiente: $m = 0$"; }
                    else if (mode == 2) { ans = "y = -1"; steps = "$f(1) = -1,\\; m = 0 \\implies y = -1$"; }
                    else if (mode == 3) { ans = "x = 1"; steps = "Normal vertical: $x = 1$"; }
                    else { ans = "x = 0,\\; \\pm 1"; steps = "$4x(x^2 - 1) = 0 \\implies x = 0,\\; \\pm 1$"; }
                }
                else if (arch == 5)
                {
                    // Cubic 2x^3 - x at x = 1
                    formula = "f(x) = 2x^3 - x;\\; x = 1";
                    graphFormula = "2*x^3 - x";
                    if (mode == 0) { ans = "5"; steps = "$f'(x) = 6x^2 - 1 \\implies f'(1) = 5$"; }
                    else if (mode == 1) { ans = "m = 5"; steps = "Pendiente: $m = 5$"; }
                    else if (mode == 2) { ans = "y = 5x - 4"; steps = "$f(1) = 1,\\; m = 5 \\implies y - 1 = 5(x - 1) \\implies y = 5x - 4$"; }
                    else if (mode == 3) { ans = "y - 1 = -\\frac{1}{5}(x - 1)"; steps = "Normal: $y - 1 = -\\frac{1}{5}(x - 1)$"; }
                    else { ans = "x = \\pm \\frac{1}{\\sqrt{6}}"; steps = "$6x^2 - 1 = 0 \\implies x = \\pm \\frac{1}{\\sqrt{6}}$"; }
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
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                GraphFormula = graphFormula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps,
                TemplateId = $"instant_rate_{chosen}_{arch}"
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
                var tangentPool = new List<Func<Question>>();

                // 0. Quadratic
                tangentPool.Add(() =>
                {
                    int a = rng.Next(-2, 3);
                    int c1 = rng.Next(1, 4);
                    int c2 = rng.Next(-4, 5);
                    string formula = $"y = {c1}x^2 {(c2 >= 0 ? "+ " + c2 : "- " + Math.Abs(c2))}x  at  x = {a}";
                    int y0 = c1 * a * a + c2 * a;
                    int m = 2 * c1 * a + c2;
                    int b = y0 - m * a;
                    string ans = b == 0 ? $"y = {m}x" : (b > 0 ? $"y = {m}x + {b}" : $"y = {m}x - {Math.Abs(b)}");
                    string steps = $"{stepLabel}: $y({a}) = {y0},\\; y' = {2 * c1}x + {c2} \\Rightarrow m = y'({a}) = {m}.\\; y - ({y0}) = {m}(x - ({a})) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {m}x + {b + 2}", $"y = {m + 1}x + {b}", $"y = {2 * c1}x + {c2}", rng);
                    return q;
                });

                // 1. Cubic
                tangentPool.Add(() =>
                {
                    int a = rng.Next(-1, 2);
                    int k = rng.Next(1, 5);
                    string formula = $"y = x^3 - {k}x  at  x = {a}";
                    int y0 = a * a * a - k * a;
                    int m = 3 * a * a - k;
                    int b = y0 - m * a;
                    string ans = b == 0 ? $"y = {m}x" : (b > 0 ? $"y = {m}x + {b}" : $"y = {m}x - {Math.Abs(b)}");
                    string steps = $"{stepLabel}: $y({a}) = {y0},\\; y' = 3x^2 - {k} \\Rightarrow m = y'({a}) = {m}.\\; y - ({y0}) = {m}(x - ({a})) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_cubic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {m}x + {b + 2}", $"y = {m - 1}x + {b}", $"y = 3x^2 - {k}", rng);
                    return q;
                });

                // 2. Reciprocal / Rational
                tangentPool.Add(() =>
                {
                    int k = rng.Next(1, 5) * 2;
                    int a = rng.Next(1, 3);
                    string formula = $"y = \\frac{{{k}}}{{x}}  at  x = {a}";
                    int y0 = k / a;
                    int m = -k / (a * a);
                    int b = y0 - m * a;
                    string ans = b == 0 ? $"y = {m}x" : (b > 0 ? $"y = {m}x + {b}" : $"y = {m}x - {Math.Abs(b)}");
                    string steps = $"{stepLabel}: $y({a}) = {y0},\\; y' = -\\frac{{{k}}}{{x^2}} \\Rightarrow m = y'({a}) = {m}.\\; y - {y0} = {m}(x - {a}) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_recip" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {m}x + {b + 1}", $"y = {-m}x + {b}", $"y = {y0}x", rng);
                    return q;
                });

                // 3. Radical
                tangentPool.Add(() =>
                {
                    int rootA = rng.Next(1, 3);
                    int a = rootA * rootA;
                    string formula = $"y = \\sqrt{{x}}  at  x = {a}";
                    int y0 = rootA;
                    int denM = 2 * rootA;
                    string ans = denM == 2
                        ? (y0 - 0.5 * a == 0 ? "y = \\frac{1}{2}x" : (y0 - 0.5 * a > 0 ? $"y = \\frac{{1}}{{2}}x + {y0 - 0.5 * a}" : $"y = \\frac{{1}}{{2}}x - {Math.Abs(y0 - 0.5 * a)}"))
                        : $"y = \\frac{{1}}{{{denM}}}x + \\frac{{{rootA}}}{{2}}";
                    string steps = $"{stepLabel}: $y({a}) = {y0},\\; y' = \\frac{{1}}{{2\\sqrt{{x}}}} \\Rightarrow m = \\frac{{1}}{{{denM}}}.\\; y - {y0} = \\frac{{1}}{{{denM}}}(x - {a}) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_rad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = \\frac{{1}}{{{rootA}}}x + 1", $"y = 2x - {a}", "y = x", rng);
                    return q;
                });

                // 4. Exponential
                tangentPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"y = e^{{{k}x}}  at  x = 0";
                    string ans = $"y = {k}x + 1";
                    string steps = $"{stepLabel}: $y(0) = 1,\\; y' = {k}e^{{{k}x}} \\Rightarrow m = y'(0) = {k}.\\; y - 1 = {k}(x - 0) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {k}x", $"y = {k}x - 1", $"y = x + 1", rng);
                    return q;
                });

                // 5. Trigonometric sine
                tangentPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"y = \\sin({k}x)  at  x = 0";
                    string ans = $"y = {k}x";
                    string steps = $"{stepLabel}: $y(0) = 0,\\; y' = {k}\\cos({k}x) \\Rightarrow m = y'(0) = {k}.\\; y - 0 = {k}(x - 0) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_trig" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {k}x + 1", $"y = -{k}x", "y = x", rng);
                    return q;
                });

                // 6. Polynomial + Exponential
                tangentPool.Add(() =>
                {
                    int c1 = rng.Next(2, 5);
                    string formula = $"y = {c1}x + e^x  at  x = 0";
                    int m = c1 + 1;
                    string ans = $"y = {m}x + 1";
                    string steps = $"{stepLabel}: $y(0) = 1,\\; y' = {c1} + e^x \\Rightarrow m = y'(0) = {c1} + 1 = {m}.\\; y - 1 = {m}(x - 0) \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_tangent_poly_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y = {c1}x + 1", $"y = {m}x - 1", $"y = {c1 + 2}x + 1", rng);
                    return q;
                });

                int chosenIdx = Math.Abs(index) % tangentPool.Count;
                return tangentPool[chosenIdx]();
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

            // If no function type checkbox is selected, default to all
            if (!includeQuad && !includeCubic && !includeQuartic && !includeRational && !includeExpEasy && !includeExpHard && !includeTrig)
            {
                includeQuad = true;
                includeCubic = true;
                includeQuartic = true;
                includeRational = true;
                includeTrig = true;
                includeExpEasy = true;
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
                int pattern = Math.Abs(index) % 4;
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
                SolutionSteps = steps,
                TemplateId = (chosenType == "quad") ? $"app_abs_extrema_quad_{(index % 2 == 0 ? "up" : "down")}_{(index % 4 < 2 ? "asym" : "sym")}" : $"app_abs_extrema_{chosenType}"
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
                quartic = true;
                rat = true;
                rad = true;
                trig = true;
            }

            var pool = new List<Func<Question>>();

            if (quad)
            {
                pool.Add(() =>
                {
                    int xv = rng.Next(-3, 4);
                    int k = rng.Next(-4, 5);
                    int b = -2 * xv;
                    int c = xv * xv + k;
                    string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                    string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                    string formula = $"{lhs} = x^2 {bStr} {cStr}".Trim();
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Mínimo relativo en $x = {xv}$",
                        AppLanguage.Portuguese => $"Mínimo relativo em $x = {xv}$",
                        _ => $"Relative min at $x = {xv}$"
                    };
                    string steps = $"$f'(x) = 2x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$. Como $f''(x) = 2 > 0$, {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quad_up" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Extremo relativo en $x = {xv + 1}$", $"Extremo relativo en $x = {-xv}$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int xv = rng.Next(-3, 4);
                    int k = rng.Next(-4, 5);
                    int b = 2 * xv;
                    int c = -xv * xv + k;
                    string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                    string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                    string formula = $"{lhs} = -x^2 {bStr} {cStr}".Trim();
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Máximo relativo en $x = {xv}$",
                        AppLanguage.Portuguese => $"Máximo relativo em $x = {xv}$",
                        _ => $"Relative max at $x = {xv}$"
                    };
                    string steps = $"$f'(x) = -2x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$. Como $f''(x) = -2 < 0$, {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quad_down" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Extremo relativo en $x = {xv + 1}$", $"Extremo relativo en $x = {-xv}$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int xv = rng.Next(-2, 3);
                    int k = rng.Next(-3, 4);
                    int b = -4 * xv;
                    int c = 2 * xv * xv + k;
                    string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                    string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                    string formula = $"{lhs} = 2x^2 {bStr} {cStr}".Trim();
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Mínimo relativo en $x = {xv}$",
                        AppLanguage.Portuguese => $"Mínimo relativo em $x = {xv}$",
                        _ => $"Relative min at $x = {xv}$"
                    };
                    string steps = $"$f'(x) = 4x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$. Como $f''(x) = 4 > 0$, {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quad_scaled_up" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Extremo relativo en $x = {xv + 1}$", $"Extremo relativo en $x = {-xv}$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int xv = rng.Next(-2, 3);
                    int k = rng.Next(-3, 4);
                    int b = 4 * xv;
                    int c = -2 * xv * xv + k;
                    string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                    string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                    string formula = $"{lhs} = -2x^2 {bStr} {cStr}".Trim();
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Máximo relativo en $x = {xv}$",
                        AppLanguage.Portuguese => $"Máximo relativo em $x = {xv}$",
                        _ => $"Relative max at $x = {xv}$"
                    };
                    string steps = $"$f'(x) = -4x {(b >= 0 ? "+ " + b : "- " + Math.Abs(b))} = 0 \\Rightarrow x = {xv}$. Como $f''(x) = -4 < 0$, {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quad_scaled_down" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_cubic_pos" };
                    if (type == QuestionType.MultipleChoice)
                    {
                        string d1 = LocalizationService.CurrentLanguage == AppLanguage.Spanish
                            ? $"Máximo relativo en $x = {r2}$, Mínimo relativo en $x = {r1}$"
                            : $"Relative max at $x = {r2}$, Relative min at $x = {r1}$";
                        q.Choices = DistractorHelper.BuildChoices(ans, d1, $"Extremos en $x = 0,\\; {r1}$", "No existen extremos relativos", rng);
                    }
                    return q;
                });
                pool.Add(() =>
                {
                    int r1 = rng.Next(-3, 0);
                    int r2 = rng.Next(1, 4);
                    int B = 3 * (r1 + r2);
                    int C = -3 * r1 * r2;
                    string bSign = B >= 0 ? $"+ {B}" : $"- {Math.Abs(B)}";
                    string cSign = C >= 0 ? $"+ {C}" : $"- {Math.Abs(C)}";
                    string bTerm = (B / 2 == 0) ? "" : (B / 2 > 0 ? $"+ {B / 2}x^2" : $"- {Math.Abs(B / 2)}x^2");
                    string cTerm = C == 0 ? "" : (C > 0 ? $"+ {C}x" : $"- {Math.Abs(C)}x");
                    string formula = $"{lhs} = -x^3 {bTerm} {cTerm}".Trim();

                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Mínimo relativo en $x = {r1}$, Máximo relativo en $x = {r2}$",
                        AppLanguage.Portuguese => $"Mínimo relativo em $x = {r1}$, Máximo relativo em $x = {r2}$",
                        _ => $"Relative min at $x = {r1}$, Relative max at $x = {r2}$"
                    };
                    string r1Factor = r1 < 0 ? $"(x + {Math.Abs(r1)})" : $"(x - {r1})";
                    string r2Factor = r2 < 0 ? $"(x + {Math.Abs(r2)})" : $"(x - {r2})";
                    string steps = $"1) $f'(x) = -3x^2 {bSign}x {cSign} = -3{r1Factor}{r2Factor} = 0 \\Rightarrow x = {r1},\\; x = {r2}$.\n2) Criterio de la 1ª derivada: {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_cubic_neg" };
                    if (type == QuestionType.MultipleChoice)
                    {
                        string d1 = LocalizationService.CurrentLanguage == AppLanguage.Spanish
                            ? $"Máximo relativo en $x = {r1}$, Mínimo relativo en $x = {r2}$"
                            : $"Relative max at $x = {r1}$, Relative min at $x = {r2}$";
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quartic_w" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Mínimo en $x = 0$", $"Extremos en $x = \\pm {a2}$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 3);
                    int a2 = a * a;
                    string formula = $"{lhs} = -x^4 + {2 * a2}x^2";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => $"Mínimo relativo en $x = 0$, Máximos relativos en $x = -{a},\\; {a}$",
                        AppLanguage.Portuguese => $"Mínimo relativo em $x = 0$, Máximos relativos em $x = -{a},\\; {a}$",
                        _ => $"Relative min at $x = 0$, Relative maxes at $x = -{a},\\; {a}$"
                    };
                    string steps = $"$f'(x) = -4x^3 + {4 * a2}x = -4x(x^2 - {a2}) = 0 \\Rightarrow x = -{a},\\; 0,\\; {a}$. {ans}.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_quartic_m" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Máximo en $x = 0$", $"Extremos en $x = \\pm {a2}$", "No existen extremos relativos", rng);
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_rat_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"Mínimo en $x = -{a}$, Máximo en $x = {a}$", $"Extremo en $x = {a2}$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"{lhs} = \\frac{{2x}}{{x^2 + 1}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = 1$, Mínimo relativo en $x = -1$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = 1$, Mínimo relativo em $x = -1$",
                        _ => "Relative max at $x = 1$, Relative min at $x = -1$"
                    };
                    string steps = "$f'(x) = \\frac{2(1 - x^2)}{(x^2 + 1)^2} = 0 \\Rightarrow x = \\pm 1$. " + ans + ".";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_rat_quot" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Mínimo en $x = 1$, Máximo en $x = -1$", "Extremo en $x = 0$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            if (rad)
            {
                pool.Add(() =>
                {
                    string formula = $"{lhs} = x\\sqrt{{4 - x}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = \\frac{8}{3}$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = \\frac{8}{3}$",
                        _ => "Relative max at $x = \\frac{8}{3}$"
                    };
                    string steps = "$f'(x) = \\sqrt{4-x} - \\frac{x}{2\\sqrt{4-x}} = \\frac{8 - 3x}{2\\sqrt{4-x}} = 0 \\Rightarrow x = \\frac{8}{3}$. " + ans + ".";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_rad_sqrt_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Mínimo relativo en $x = \\frac{8}{3}$", "Máximo relativo en $x = 2$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"{lhs} = x\\sqrt{{6 - x}}";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = 4$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = 4$",
                        _ => "Relative max at $x = 4$"
                    };
                    string steps = "$f'(x) = \\sqrt{6-x} - \\frac{x}{2\\sqrt{6-x}} = \\frac{12 - 3x}{2\\sqrt{6-x}} = 0 \\Rightarrow x = 4$. " + ans + ".";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_rad_sqrt_linear2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Mínimo relativo en $x = 4$", "Máximo relativo en $x = 3$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            if (trig)
            {
                pool.Add(() =>
                {
                    string formula = $"{lhs} = \\sin(x) + \\cos(x);\\; [0, 2\\pi]";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = \\frac{\\pi}{4}$, Mínimo relativo en $x = \\frac{5\\pi}{4}$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = \\frac{\\pi}{4}$, Mínimo relativo em $x = \\frac{5\\pi}{4}$",
                        _ => "Relative max at $x = \\frac{\\pi}{4}$, Relative min at $x = \\frac{5\\pi}{4}$"
                    };
                    string steps = "$f'(x) = \\cos(x) - \\sin(x) = 0 \\Rightarrow \\tan(x) = 1 \\Rightarrow x = \\frac{\\pi}{4},\\; \\frac{5\\pi}{4}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_trig_sincos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Extremos en $x = \\frac{\\pi}{2},\\; \\pi$", "Máximo en $x = \\pi$", "No existen extremos relativos", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"{lhs} = \\sin(x) - \\cos(x);\\; [0, 2\\pi]";
                    string ans = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Máximo relativo en $x = \\frac{3\\pi}{4}$, Mínimo relativo en $x = \\frac{7\\pi}{4}$",
                        AppLanguage.Portuguese => "Máximo relativo em $x = \\frac{3\\pi}{4}$, Mínimo relativo em $x = \\frac{7\\pi}{4}$",
                        _ => "Relative max at $x = \\frac{3\\pi}{4}$, Relative min at $x = \\frac{7\\pi}{4}$"
                    };
                    string steps = "$f'(x) = \\cos(x) + \\sin(x) = 0 \\Rightarrow \\tan(x) = -1 \\Rightarrow x = \\frac{3\\pi}{4},\\; \\frac{7\\pi}{4}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_rel_extrema_trig_sincos_diff" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "Extremos en $x = \\frac{\\pi}{4},\\; \\frac{5\\pi}{4}$", "Máximo en $x = \\pi$", "No existen extremos relativos", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateCriticalPointsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
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
                quartic = true;
                rat = true;
                rad = true;
                trig = true;
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_quad" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_cubic" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_quartic" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_rat" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x = {a}", $"x = {a2}", "x = 0", rng);
                    return q;
                });
            }

            if (rad)
            {
                pool.Add(() =>
                {
                    string formula = $"{lhs} = x\\sqrt{{4 - x}}";
                    string ans = "x = \\frac{8}{3},\\; x = 4";
                    string steps = "$f'(x) = \\frac{8-3x}{2\\sqrt{4-x}}$. Puntos críticos donde $f'(x)=0$ ($x = 8/3$) o donde $f'(x)$ no existe ($x = 4$).";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_rad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x = \\frac{8}{3}", "x = 4", "x = 0", rng);
                    return q;
                });
            }

            if (trig)
            {
                pool.Add(() =>
                {
                    string formula = $"{lhs} = 2\\sin(x) - x;\\; [0, 2\\pi]";
                    string ans = "x = \\frac{\\pi}{3},\\; x = \\frac{5\\pi}{3}";
                    string steps = "$f'(x) = 2\\cos(x) - 1 = 0 \\Rightarrow \\cos(x) = \\frac{1}{2} \\Rightarrow x = \\frac{\\pi}{3},\\; \\frac{5\\pi}{3}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "app_crit_pts_trig" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x = \\frac{\\pi}{6},\\; \\frac{11\\pi}{6}", "x = \\frac{\\pi}{2}", "No existen puntos críticos", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateImplicitProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int comp = p?.ComplexityOptionIndex ?? 0;
            var pool = new List<Func<Question>>();

            if (comp == 0)
            {
                // 1. x^2 + a y = c
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int c = rng.Next(1, 10);
                    string formula = $"x^2 + {a}y = {c}";
                    string ans = $"dy/dx = -2x / {a}";
                    string steps = $"$2x + {a}\\frac{{dy}}{{dx}} = 0 \\Rightarrow {a}\\frac{{dy}}{{dx}} = -2x \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{2x}}{{{a}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"dy/dx = 2x / {a}", $"dy/dx = -x / {a}", "dy/dx = -2x", rng);
                    return q;
                });
                // 2. a x^2 + y = c
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int c = rng.Next(1, 10);
                    string formula = $"{a}x^2 + y = {c}";
                    string ans = $"dy/dx = -{2 * a}x";
                    string steps = $"${2 * a}x + \\frac{{dy}}{{dx}} = 0 \\Rightarrow \\frac{{dy}}{{dx}} = -{2 * a}x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"dy/dx = {2 * a}x", $"dy/dx = -{a}x", "dy/dx = -x", rng);
                    return q;
                });
                // 3. x^3 + a y = c
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int c = rng.Next(1, 10);
                    string formula = $"x^3 + {a}y = {c}";
                    string ans = $"dy/dx = -3x^2 / {a}";
                    string steps = $"$3x^2 + {a}\\frac{{dy}}{{dx}} = 0 \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{3x^2}}{{{a}}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"dy/dx = 3x^2 / {a}", $"dy/dx = -x^2 / {a}", "dy/dx = -3x^2", rng);
                    return q;
                });
                // 4. 2x - 3y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(1, 10);
                    string formula = $"2x - 3y^2 = {c}";
                    string ans = "dy/dx = 1 / (3y)";
                    string steps = "$2 - 6y\\frac{dy}{dx} = 0 \\Rightarrow 6y\\frac{dy}{dx} = 2 \\Rightarrow \\frac{dy}{dx} = \\frac{1}{3y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_3" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -1 / (3y)", "dy/dx = 3y", "dy/dx = 2 / (3y)", rng);
                    return q;
                });
                // 5. x^2 + 2y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(4, 20);
                    string formula = $"x^2 + 2y^2 = {c}";
                    string ans = "dy/dx = -x / (2y)";
                    string steps = "$2x + 4y\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = -\\frac{x}{2y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_4" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -2x / y", "dy/dx = x / (2y)", "dy/dx = -x / y", rng);
                    return q;
                });
                // 6. x^2 - y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(1, 15);
                    string formula = $"x^2 - y^2 = {c}";
                    string ans = "dy/dx = x / y";
                    string steps = "$2x - 2y\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = \\frac{x}{y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_0_5" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -x / y", "dy/dx = y / x", "dy/dx = -y / x", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                // 1. Circle
                pool.Add(() =>
                {
                    int r = rng.Next(2, 6);
                    string formula = $"x^2 + y^2 = {r * r}";
                    string ans = "dy/dx = -x / y";
                    string steps = $"$2x + 2y\\frac{{dy}}{{dx}} = 0 \\Rightarrow 2y\\frac{{dy}}{{dx}} = -2x \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{x}}{{y}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -y / x", "dy/dx = x / y", "dy/dx = -2x", rng);
                    return q;
                });
                // 2. Sum of cubes
                pool.Add(() =>
                {
                    int c = rng.Next(5, 25);
                    string formula = $"x^3 + y^3 = {c}";
                    string ans = "dy/dx = -x^2 / y^2";
                    string steps = $"$3x^2 + 3y^2\\frac{{dy}}{{dx}} = 0 \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{x^2}}{{y^2}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -y^2 / x^2", "dy/dx = x^2 / y^2", "dy/dx = -3x^2", rng);
                    return q;
                });
                // 3. Ellipse 2x^2 + 3y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(10, 30);
                    string formula = $"2x^2 + 3y^2 = {c}";
                    string ans = "dy/dx = -2x / (3y)";
                    string steps = "$4x + 6y\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = -\\frac{2x}{3y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -3x / (2y)", "dy/dx = 2x / (3y)", "dy/dx = -4x / (6y)", rng);
                    return q;
                });
                // 4. Quartic x^4 + y^4 = c
                pool.Add(() =>
                {
                    int c = rng.Next(10, 50);
                    string formula = $"x^4 + y^4 = {c}";
                    string ans = "dy/dx = -x^3 / y^3";
                    string steps = "$4x^3 + 4y^3\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = -\\frac{x^3}{y^3}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_3" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -y^3 / x^3", "dy/dx = x^3 / y^3", "dy/dx = -4x^3", rng);
                    return q;
                });
                // 5. Radical sqrt(x) + sqrt(y) = c
                pool.Add(() =>
                {
                    int c = rng.Next(3, 8);
                    string formula = $"\\sqrt{{x}} + \\sqrt{{y}} = {c}";
                    string ans = "dy/dx = -\\sqrt{y / x}";
                    string steps = "$\\frac{1}{2\\sqrt{x}} + \\frac{1}{2\\sqrt{y}}\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = -\\frac{\\sqrt{y}}{\\sqrt{x}} = -\\sqrt{\\frac{y}{x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_4" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -\\sqrt{x / y}", "dy/dx = \\sqrt{y / x}", "dy/dx = -1", rng);
                    return q;
                });
                // 6. Hyperbola x^2 - 4y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(4, 20);
                    string formula = $"x^2 - 4y^2 = {c}";
                    string ans = "dy/dx = x / (4y)";
                    string steps = "$2x - 8y\\frac{dy}{dx} = 0 \\Rightarrow \\frac{dy}{dx} = \\frac{x}{4y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_1_5" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -x / (4y)", "dy/dx = 4x / y", "dy/dx = x / (2y)", rng);
                    return q;
                });
            }
            else if (comp == 2)
            {
                // 1. xy = c
                pool.Add(() =>
                {
                    int c = rng.Next(2, 10);
                    string formula = $"x*y = {c}";
                    string ans = "dy/dx = -y / x";
                    string steps = $"$1\\cdot y + x\\frac{{dy}}{{dx}} = 0 \\Rightarrow x\\frac{{dy}}{{dx}} = -y \\Rightarrow \\frac{{dy}}{{dx}} = -\\frac{{y}}{{x}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -x / y", "dy/dx = y / x", "dy/dx = -1/x", rng);
                    return q;
                });
                // 2. x^2 + xy + y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(5, 20);
                    string formula = $"x^2 + x*y + y^2 = {c}";
                    string ans = "dy/dx = -(2x + y) / (x + 2y)";
                    string steps = $"$2x + (y + x y') + 2y y' = 0 \\Rightarrow y'(x + 2y) = -(2x + y) \\Rightarrow y' = -\\frac{{2x + y}}{{x + 2y}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -(x + 2y) / (2x + y)", "dy/dx = -(2x) / (2y)", "dy/dx = -1", rng);
                    return q;
                });
                // 3. x^2 - xy + y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(5, 20);
                    string formula = $"x^2 - x*y + y^2 = {c}";
                    string ans = "dy/dx = -(2x - y) / (2y - x)";
                    string steps = "$2x - (y + x y') + 2y y' = 0 \\Rightarrow y'(2y - x) = y - 2x \\Rightarrow y' = \\frac{y - 2x}{2y - x} = -\\frac{2x - y}{2y - x}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -(2x + y) / (2y - x)", "dy/dx = (2x - y) / (2y + x)", "dy/dx = -1", rng);
                    return q;
                });
                // 4. 2xy + y^2 = c
                pool.Add(() =>
                {
                    int c = rng.Next(4, 16);
                    string formula = $"2x*y + y^2 = {c}";
                    string ans = "dy/dx = -y / (x + y)";
                    string steps = "$2(y + x y') + 2y y' = 0 \\Rightarrow 2y'(x + y) = -2y \\Rightarrow y' = -\\frac{y}{x + y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_3" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = y / (x + y)", "dy/dx = -x / (x + y)", "dy/dx = -1", rng);
                    return q;
                });
                // 5. x^2 * y + y = c
                pool.Add(() =>
                {
                    int c = rng.Next(3, 15);
                    string formula = $"x^2*y + y = {c}";
                    string ans = "dy/dx = -2x*y / (x^2 + 1)";
                    string steps = "$2x y + x^2 y' + y' = 0 \\Rightarrow y'(x^2 + 1) = -2x y \\Rightarrow y' = -\\frac{2xy}{x^2 + 1}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_4" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -xy / (x^2 + 1)", "dy/dx = 2xy / (x^2 + 1)", "dy/dx = -2x", rng);
                    return q;
                });
                // 6. x * y^2 + x = c
                pool.Add(() =>
                {
                    int c = rng.Next(3, 15);
                    string formula = $"x*y^2 + x = {c}";
                    string ans = "dy/dx = -(y^2 + 1) / (2x*y)";
                    string steps = "$y^2 + 2x y y' + 1 = 0 \\Rightarrow 2xy y' = -(y^2 + 1) \\Rightarrow y' = -\\frac{y^2 + 1}{2xy}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_2_5" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -(y^2 - 1) / (2xy)", "dy/dx = (y^2 + 1) / (2xy)", "dy/dx = -1/(2y)", rng);
                    return q;
                });
            }
            else
            {
                // 1. sin(y) + x = y
                pool.Add(() =>
                {
                    string formula = "sin(y) + x = y";
                    string ans = "dy/dx = 1 / (1 - cos(y))";
                    string steps = $"$\\cos(y)y' + 1 = y' \\Rightarrow 1 = y'(1 - \\cos(y)) \\Rightarrow y' = \\frac{{1}}{{1 - \\cos(y)}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_0" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = 1 / (cos(y) - 1)", "dy/dx = cos(y) - 1", "dy/dx = -1 / cos(y)", rng);
                    return q;
                });
                // 2. cos(x) + sin(y) = 1
                pool.Add(() =>
                {
                    string formula = "\\cos(x) + \\sin(y) = 1";
                    string ans = "dy/dx = sin(x) / cos(y)";
                    string steps = "$-\\sin(x) + \\cos(y)y' = 0 \\Rightarrow y' = \\frac{\\sin(x)}{\\cos(y)}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_1" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = -sin(x) / cos(y)", "dy/dx = cos(y) / sin(x)", "dy/dx = tan(x)", rng);
                    return q;
                });
                // 3. e^y + x^2 = 5
                pool.Add(() =>
                {
                    string formula = "e^y + x^2 = 5";
                    string ans = "dy/dx = -2x / e^y";
                    string steps = "$e^y y' + 2x = 0 \\Rightarrow y' = -\\frac{2x}{e^y}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = 2x / e^y", "dy/dx = -x / e^y", "dy/dx = -2x e^y", rng);
                    return q;
                });
                // 4. x * e^y = 1
                pool.Add(() =>
                {
                    string formula = "x*e^y = 1";
                    string ans = "dy/dx = -1 / x";
                    string steps = "$e^y + x e^y y' = 0 \\Rightarrow e^y(1 + x y') = 0 \\Rightarrow y' = -\\frac{1}{x}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_3" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = 1 / x", "dy/dx = -e^y / x", "dy/dx = -x", rng);
                    return q;
                });
                // 5. ln(y) + x^2 = 4
                pool.Add(() =>
                {
                    string formula = "\\ln(y) + x^2 = 4";
                    string ans = "dy/dx = -2x*y";
                    string steps = "$\\frac{1}{y}y' + 2x = 0 \\Rightarrow \\frac{y'}{y} = -2x \\Rightarrow y' = -2xy$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_4" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = 2x*y", "dy/dx = -2x / y", "dy/dx = -x*y", rng);
                    return q;
                });
                // 6. tan(y) = x
                pool.Add(() =>
                {
                    string formula = "\\tan(y) = x";
                    string ans = "dy/dx = cos^2(y)";
                    string steps = "$\\sec^2(y) y' = 1 \\Rightarrow y' = \\frac{1}{\\sec^2(y)} = \\cos^2(y)$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "implicit_3_5" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "dy/dx = sec^2(y)", "dy/dx = -cos^2(y)", "dy/dx = 1", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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
                // 1. sin(ax) / bx
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] sin({a}x) / ({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}\\cos({a}x)}}{{{b}}} = \\frac{{{a}(1)}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_trig_sin_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "Does not exist", rng);
                    return q;
                });

                // 2. (1 - cos(ax)) / x^2
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] (1 - cos({a}x)) / (x^2)";
                    int num = a * a;
                    int den = 2;
                    string ans = num % den == 0 ? (num / den).ToString() : $"{num}/{den}";
                    string steps = $"Forma 0/0. L'Hôpital (1): $\\lim \\frac{{{a}\\sin({a}x)}}{{2x}}$. L'Hôpital (2): $\\lim \\frac{{{a * a}\\cos({a}x)}}{{2}} = {ans}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_trig_cos_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", a.ToString(), "1", rng);
                    return q;
                });

                // 3. tan(ax) / x
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> 0] tan({a}x) / x";
                    string ans = a.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}\\sec^2({a}x)}}{{1}} = {a}(1) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_trig_tan_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (a * 2).ToString(), rng);
                    return q;
                });

                // 4. sin(ax) / sin(bx)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 6);
                    if (a == b) b++;
                    string formula = $"lim [x -> 0] sin({a}x) / sin({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}\\cos({a}x)}}{{{b}\\cos({b}x)}} = \\frac{{{a}(1)}}{{{b}(1)}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_trig_sin_sin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", $"{a}", rng);
                    return q;
                });
            }

            if (exp)
            {
                // 1. (e^{ax} - 1) / x
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> 0] (e^({a}x) - 1) / (x)";
                    string ans = a.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}e^{{{a}x}}}}{{1}} = {a}e^0 = {a}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_exp_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (a * 2).ToString(), rng);
                    return q;
                });

                // 2. (e^{ax} - e^{-ax}) / x
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] (e^({a}x) - e^(-{a}x)) / (x)";
                    int ansVal = 2 * a;
                    string ans = ansVal.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}e^{{{a}x}} + {a}e^{{-{a}x}}}}{{1}} = {a} + {a} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_exp_diff" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, a.ToString(), "0", (ansVal + 2).ToString(), rng);
                    return q;
                });

                // 3. (e^x - 1 - x) / x^2
                pool.Add(() =>
                {
                    string formula = "lim [x -> 0] (e^x - 1 - x) / (x^2)";
                    string ans = "1/2";
                    string steps = @"Forma 0/0. L'Hôpital (1): $\lim \frac{e^x - 1}{2x}$. L'Hôpital (2): $\lim \frac{e^x}{2} = \frac{1}{2}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_exp_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", "2", rng);
                    return q;
                });

                // 4. (e^{ax} - 1) / (bx)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] (e^({a}x) - 1) / ({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}e^{{{a}x}}}}{{{b}}} = \\frac{{{a}}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_exp_linear_scaled" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", a.ToString(), rng);
                    return q;
                });
            }

            if (log)
            {
                // 1. ln(x) / (x - 1)
                pool.Add(() =>
                {
                    string formula = "lim [x -> 1] ln(x) / (x - 1)";
                    string ans = "1";
                    string steps = "Forma 0/0. L'Hôpital: $\\lim_{{x \\to 1}} \\frac{{1/x}}{{1}} = 1$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_log_shifted" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "Does not exist", "2", rng);
                    return q;
                });

                // 2. ln(1 + ax) / x
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> 0] ln(1 + {a}x) / x";
                    string ans = a.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}/(1 + {a}x)}}{{1}} = {a}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_log_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", (a + 1).ToString(), rng);
                    return q;
                });

                // 3. ln(x) / (x^2 - 1)
                pool.Add(() =>
                {
                    string formula = "lim [x -> 1] ln(x) / (x^2 - 1)";
                    string ans = "1/2";
                    string steps = @"Forma 0/0. L'Hôpital: $\lim_{x \to 1} \frac{1/x}{2x} = \frac{1}{2(1)} = \frac{1}{2}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_log_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", "2", rng);
                    return q;
                });

                // 4. ln(1 + ax) / (bx)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] ln(1 + {a}x) / ({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{{a}/(1 + {a}x)}}{{{b}}} = \\frac{{{a}}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_log_scaled" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", "0", a.ToString(), rng);
                    return q;
                });
            }

            if (rat || pool.Count == 0)
            {
                // 1. (x^2 - a^2) / (x - a)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"lim [x -> {a}] (x^2 - {a * a}) / (x - {a})";
                    int ansVal = 2 * a;
                    string ans = ansVal.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to {a}}} \\frac{{2x}}{{1}} = 2({a}) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_rat_quad_diff" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, a.ToString(), (ansVal + 2).ToString(), "0", rng);
                    return q;
                });

                // 2. (x^3 - a^3) / (x - a)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"lim [x -> {a}] (x^3 - {a * a * a}) / (x - {a})";
                    int ansVal = 3 * a * a;
                    string ans = ansVal.ToString();
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to {a}}} \\frac{{3x^2}}{{1}} = 3({a}^2) = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_rat_cubic_diff" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 3).ToString(), (2 * a * a).ToString(), "0", rng);
                    return q;
                });

                // 3. (x^2 + ax) / (bx)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    string formula = $"lim [x -> 0] (x^2 + {a}x) / ({b}x)";
                    int g = Gcd(a, b);
                    int na = a / g, nb = b / g;
                    string ans = nb == 1 ? na.ToString() : $"{na}/{nb}";
                    string steps = $"Forma 0/0. L'Hôpital: $\\lim_{{x \\to 0}} \\frac{{2x + {a}}}{{{b}}} = \\frac{{{a}}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_rat_quad_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", a.ToString(), rng);
                    return q;
                });

                // 4. (sqrt(x) - sqrt(a)) / (x - a)
                pool.Add(() =>
                {
                    int a = 4;
                    string formula = $"lim [x -> {a}] (sqrt(x) - 2) / (x - {a})";
                    string ans = "1/4";
                    string steps = @"Forma 0/0. L'Hôpital: $\lim_{x \to 4} \frac{1/(2\sqrt{x})}{1} = \frac{1}{2(2)} = \frac{1}{4}$.";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "lhopital_rat_sqrt_diff" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1/2", "0", "1", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateConcavityProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string lhs = (p?.Notation == "f(x)=") ? "f(x)" : (p?.Notation == "Either" ? (index % 2 == 0 ? "y" : "f(x)") : "y");
            var pool = new List<Func<Question>>();

            // 1. Quartic opening up
            pool.Add(() =>
            {
                int a = rng.Next(1, 6);
                int a2 = a * a;
                int c = rng.Next(-5, 6);
                string cStr = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                string formula = $"{lhs} = x^4 - {6 * a2}x^2{cStr}";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Cóncava hacia arriba: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Cóncava hacia abajo: $(-{a}, {a})$; Puntos de inflexión en $x = \\pm {a}$",
                    AppLanguage.Portuguese => $"Côncava para cima: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Côncava para baixo: $(-{a}, {a})$; Pontos de inflexão em $x = \\pm {a}$",
                    _ => $"Concave up: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Concave down: $(-{a}, {a})$; Inflection points at $x = \\pm {a}$"
                };
                string steps = $"$f'(x) = 4x^3 - {12 * a2}x \\Rightarrow f''(x) = 12x^2 - {12 * a2} = 12(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_quartic_up" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia arriba en todo $\\mathbb{R}$", $"Punto de inflexión en $x = 0$", $"Cóncava hacia abajo: $(-\\infty, {a})$", rng);
                return q;
            });

            // 2. Quartic opening down
            pool.Add(() =>
            {
                int a = rng.Next(1, 6);
                int a2 = a * a;
                string formula = $"{lhs} = -x^4 + {6 * a2}x^2";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Cóncava hacia arriba: $(-{a}, {a})$; Cóncava hacia abajo: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Puntos de inflexión en $x = \\pm {a}$",
                    AppLanguage.Portuguese => $"Côncava para cima: $(-{a}, {a})$; Côncava para baixo: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Pontos de inflexão em $x = \\pm {a}$",
                    _ => $"Concave up: $(-{a}, {a})$; Concave down: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Inflection points at $x = \\pm {a}$"
                };
                string steps = $"$f'(x) = -4x^3 + {12 * a2}x \\Rightarrow f''(x) = -12x^2 + {12 * a2} = -12(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_quartic_down" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia abajo en todo $\\mathbb{R}$", $"Puntos de inflexión en $x = 0$", $"Cóncava hacia arriba: $({a}, \\infty)$", rng);
                return q;
            });

            // 3. Cubic opening up
            pool.Add(() =>
            {
                int a = rng.Next(-5, 6);
                if (a == 0) a = 2;
                int c = rng.Next(-5, 6);
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_cubic_up" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia arriba en todo $\\mathbb{R}$", "Punto de inflexión en $x = 0$", $"Cóncava hacia abajo: $({a}, \\infty)$", rng);
                return q;
            });

            // 4. Cubic opening down
            pool.Add(() =>
            {
                int a = rng.Next(-5, 6);
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_cubic_down" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Cóncava hacia abajo en todo $\\mathbb{R}$", "Punto de inflexión en $x = 0$", $"Cóncava hacia arriba: $({a}, \\infty)$", rng);
                return q;
            });

            // 5. Rational function concavity
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                int a2 = a * a;
                string formula = $"{lhs} = \\frac{{1}}{{x^2 + {a2}}}";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Puntos de inflexión en $x = \\pm \\frac{{{a}}}{{\\sqrt{{3}}}}$",
                    AppLanguage.Portuguese => $"Pontos de inflexão em $x = \\pm \\frac{{{a}}}{{\\sqrt{{3}}}}$",
                    _ => $"Inflection points at $x = \\pm \\frac{{{a}}}{{\\sqrt{{3}}}}$"
                };
                string steps = $"$f'(x) = \\frac{{-2x}}{{(x^2 + {a2})^2}},\\; f''(x) = \\frac{{6x^2 - 2({a2})}}{{(x^2 + {a2})^3}} = 0 \\Rightarrow 6x^2 = {2 * a2} \\Rightarrow x = \\pm \\frac{{{a}}}{{\\sqrt{{3}}}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_rational" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Puntos de inflexión en $x = 0$", $"Puntos de inflexión en $x = \\pm {a}$", "No existen puntos de inflexión", rng);
                return q;
            });

            // 6. Quadratic concavity (always concave up or down, no inflection points)
            pool.Add(() =>
            {
                int a = rng.Next(1, 4) * (rng.Next(2) == 0 ? 1 : -1);
                int b = rng.Next(-4, 5);
                int c = rng.Next(-5, 6);
                string aStr = a == 1 ? "x^2" : (a == -1 ? "-x^2" : $"{a}x^2");
                string bStr = b == 0 ? "" : (b > 0 ? $"+ {b}x" : $"- {Math.Abs(b)}x");
                string cStr = c == 0 ? "" : (c > 0 ? $"+ {c}" : $"- {Math.Abs(c)}");
                string formula = $"{lhs} = {aStr} {bStr} {cStr}".Trim();
                bool isUp = a > 0;
                string ans = isUp
                    ? (LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Cóncava hacia arriba en todo $\\mathbb{R}$; sin puntos de inflexión" : "Concave up on all $\\mathbb{R}$; no inflection points")
                    : (LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Cóncava hacia abajo en todo $\\mathbb{R}$; sin puntos de inflexión" : "Concave down on all $\\mathbb{R}$; no inflection points");
                string steps = $"$f'(x) = {2 * a}x + {b},\\; f''(x) = {2 * a}$. Como $f''(x) {(isUp ? "> 0" : "< 0")}$ para todo $x$, la curva es cóncava hacia {(isUp ? "arriba" : "abajo")} en todo $\\mathbb{{R}}$ y no posee puntos de inflexión.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "concav_quad" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, isUp ? "Cóncava hacia abajo en todo $\\mathbb{R}$" : "Cóncava hacia arriba en todo $\\mathbb{R}$", $"Punto de inflexión en $x = 0$", "Cóncava hacia arriba en $(0, \\infty)$", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
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
                int b = rng.Next(-5, 6);
                int c = rng.Next(-5, 6);
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_quad_up" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-\\infty, {b})$; Decreciente: $({b}, \\infty)$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 2. Quadratic opening down
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                int b = rng.Next(-5, 6);
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_quad_down" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $({b}, \\infty)$; Decreciente: $(-\\infty, {b})$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 3. Cubic
            pool.Add(() =>
            {
                int a = rng.Next(1, 6);
                int c = rng.Next(-5, 6);
                string cPart = c == 0 ? "" : (c > 0 ? $" + {c}" : $" - {Math.Abs(c)}");
                string formula = $"{lhs} = x^3 - {3 * a * a}x{cPart}";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreciente: $(-{a}, {a})$",
                    AppLanguage.Portuguese => $"Crescente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decrescente: $(-{a}, {a})$",
                    _ => $"Increasing: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreasing: $(-{a}, {a})$"
                };
                string steps = $"$f'(x) = 3x^2 - {3 * a * a} = 3(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. Criterio de la 1ª derivada: {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_cubic_up" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-{a}, {a})$; Decreciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 4. Inverted cubic
            pool.Add(() =>
            {
                int a = rng.Next(1, 6);
                string formula = $"{lhs} = -x^3 + {3 * a * a}x";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-{a}, {a})$; Decreciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$",
                    AppLanguage.Portuguese => $"Crescente: $(-{a}, {a})$; Decrescente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$",
                    _ => $"Increasing: $(-{a}, {a})$; Decreasing: $(-\\infty, -{a}) \\cup ({a}, \\infty)$"
                };
                string steps = $"$f'(x) = -3x^2 + {3 * a * a} = -3(x - {a})(x + {a}) = 0 \\Rightarrow x = \\pm {a}$. Criterio de la 1ª derivada: {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_cubic_down" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreciente: $(-{a}, {a})$", "Creciente en todo $\\mathbb{R}$", "Decreciente en todo $\\mathbb{R}$", rng);
                return q;
            });

            // 5. Rational
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                int a2 = a * a;
                string formula = $"{lhs} = x + \\frac{{{a2}}}{{x}}";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreciente: $(-{a}, 0) \\cup (0, {a})$",
                    AppLanguage.Portuguese => $"Crescente: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decrescente: $(-{a}, 0) \\cup (0, {a})$",
                    _ => $"Increasing: $(-\\infty, -{a}) \\cup ({a}, \\infty)$; Decreasing: $(-{a}, 0) \\cup (0, {a})$"
                };
                string steps = $"$f'(x) = 1 - \\frac{{{a2}}}{{x^2}} = \\frac{{x^2 - {a2}}}{{x^2}} = 0 \\Rightarrow x = \\pm {a}$. Punto no definido en $x=0$. {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_rational" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "Creciente en todo $\\mathbb{R}$", $"Decreciente: $(-{a}, {a})$", $"Creciente: $(-{a}, {a})$", rng);
                return q;
            });

            // 6. Quartic
            pool.Add(() =>
            {
                int a = rng.Next(1, 3);
                int a2 = a * a;
                string formula = $"{lhs} = x^4 - {2 * a2}x^2";
                string ans = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Creciente: $(-{a}, 0) \\cup ({a}, \\infty)$; Decreciente: $(-\\infty, -{a}) \\cup (0, {a})$",
                    AppLanguage.Portuguese => $"Crescente: $(-{a}, 0) \\cup ({a}, \\infty)$; Decrescente: $(-\\infty, -{a}) \\cup (0, {a})$",
                    _ => $"Increasing: $(-{a}, 0) \\cup ({a}, \\infty)$; Decreasing: $(-\\infty, -{a}) \\cup (0, {a})$"
                };
                string steps = $"$f'(x) = 4x(x^2 - {a2}) = 0 \\Rightarrow x = -{a},\\; 0,\\; {a}$. {ans}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inc_dec_quartic" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"Creciente: $(-\\infty, 0)$; Decreciente: $(0, \\infty)$", "Creciente en todo $\\mathbb{R}$", $"Decreciente: $(-{a}, {a})$", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_quad_monomial" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_quad_poly" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_reciprocal" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_sqrt" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"c = {b / 2.0}", $"c = 1", $"c = {Math.Sqrt(b)}", rng);
                return q;
            });

            // 5. Cubic x^3 on [0, 3]
            pool.Add(() =>
            {
                string formula = $"{lhs} = x^3;\\; [0, 3]";
                string ans = @"c = \sqrt{3}";
                string steps = @"\frac{f(3) - f(0)}{3 - 0} = \frac{27}{3} = 9.\; f'(c) = 3c^2 = 9 \implies c^2 = 3 \implies c = \sqrt{3} \in (0, 3).";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_cubic" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 1.5", "c = 1", "c = 2", rng);
                return q;
            });

            // 6. Linear function ax + b
            pool.Add(() =>
            {
                int m = rng.Next(2, 6);
                int k = rng.Next(1, 5);
                string formula = $"{lhs} = {m}x + {k};\\; [1, 5]";
                string ans = @"\text{Cualquier } c \in (1, 5)";
                string steps = $@"f'(x) = {m} \text{{ es constante para todo }} x. \text{{ La tasa promedio es }} \frac{{f(5) - f(1)}}{{4}} = {m} \implies f'(c) = {m} \text{{ se cumple en todo el intervalo}}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "mvt_linear" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 3", "c = 2.5", @"\text{No existe } c", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_centered_quad" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_shifted_quad" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_inverted_quad" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_factored_quad" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_trig_sin" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 0", $"c = {bStr}", $"c = \\frac{{\\pi}}{{{k}}}", rng);
                return q;
            });

            // 6. Cubic x^3 - 4x on [0, 2]
            pool.Add(() =>
            {
                string formula = $"{lhs} = x^3 - 4x;\\; [0, 2]";
                string ans = @"c = \frac{2}{\sqrt{3}}";
                string steps = @"f(0) = 0,\; f(2) = 8 - 8 = 0.\; f'(c) = 3c^2 - 4 = 0 \implies c^2 = \frac{4}{3} \implies c = \frac{2}{\sqrt{3}} \in (0, 2).";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "rolle_cubic_roots" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "c = 1", @"c = \sqrt{2}", "c = 0", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_rectangle_perimeter" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_river_fence" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_sum_product" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_open_box" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"x = {cut + 1} cm", $"x = {S / 4} cm", $"x = {cut * 2} cm", rng);
                return q;
            });

            // 5. Cylindrical can of fixed volume 54*pi
            pool.Add(() =>
            {
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Halle el radio r de una lata cilíndrica cerrada de volumen 54*pi cm^3 que minimice el área superficial total.",
                    AppLanguage.Portuguese => "Encontre o raio r de uma lata cilíndrica fechada de volume 54*pi cm^3 que minimize a área total.",
                    _ => "Find the radius r of a closed cylindrical can with volume 54*pi cm^3 that minimizes total surface area."
                };
                string ans = "r = 3 cm, h = 6 cm";
                string steps = @"V = \pi r^2 h = 54\pi \implies h = \frac{54}{r^2}.\; S = 2\pi r^2 + 2\pi r h = 2\pi r^2 + \frac{108\pi}{r}.\; S'(r) = 4\pi r - \frac{108\pi}{r^2} = 0 \implies r^3 = 27 \implies r = 3\text{ cm},\; h = 6\text{ cm}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_cylindrical_can" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "r = 2 cm, h = 9 cm", "r = 4 cm, h = 3 cm", "r = 1 cm, h = 54 cm", rng);
                return q;
            });

            // 6. Minimum perimeter rectangle for fixed area
            pool.Add(() =>
            {
                int k = rng.Next(4, 12);
                int area = k * k;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Halle las dimensiones de un rectángulo de área {area} m^2 con perímetro mínimo.",
                    AppLanguage.Portuguese => $"Encontre as dimensões de um retângulo de área {area} m^2 com perímetro mínimo.",
                    _ => $"Find the dimensions of a rectangle with area {area} m^2 that minimizes perimeter."
                };
                string ans = $"{k} m x {k} m (Perímetro = {4 * k} m)";
                string steps = $@"A = xy = {area} \implies y = \frac{{{area}}}{{x}}.\; P(x) = 2x + \frac{{{2 * area}}}{{x}}.\; P'(x) = 2 - \frac{{{2 * area}}}{{x^2}} = 0 \implies x^2 = {area} \implies x = {k},\; y = {k}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "optim_min_perimeter_rect" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{k - 1} m x {k + 2} m", $"{area} m x 1 m", $"{2 * k} m x {k / 2} m", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
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
            bool invTrig = HasInverseTrig(p);

            if (!poly && !trig && !exp && !log && !rat && !rad && !invTrig)
            {
                poly = true;
                trig = true;
                exp = true;
                log = true;
                rat = true;
                rad = true;
            }

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
                var trigPool = new List<Func<Question>>();
                var expPool = new List<Func<Question>>();
                var logPool = new List<Func<Question>>();
                var invTrigPool = new List<Func<Question>>();

                // 1. Trig Parts (Simple & Composite)
                trigPool.Add(() =>
                {
                    string formula = "int x*cos(x) dx";
                    string ans = "x*sin(x) + cos(x) + C";
                    string steps = $"{intLabel} (Partes): $u = x,\\; dv = \\cos(x)dx \\implies x\\sin(x) - \\int \\sin(x)dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_trig_xcos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x*sin(x) - cos(x) + C", "x*cos(x) + sin(x) + C", "-x*sin(x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int k = rng.Next(2, 5);
                    string formula = $"int {a}x*cos({k}x) dx";
                    int k2 = k * k;
                    string sinTerm = (a % k == 0) ? $"{a / k}x*sin({k}x)" : $"\\frac{{{a}x}}{{{k}}}*sin({k}x)";
                    string cosTerm = (a % k2 == 0) ? $"{a / k2}*cos({k}x)" : $"\\frac{{{a}}}{{{k2}}}*cos({k}x)";
                    string ans = $"{sinTerm} + {cosTerm} + C";
                    string steps = $"{intLabel} (Partes): $u = {a}x,\\; dv = \\cos({k}x)dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_trig_axcos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{sinTerm} - {cosTerm} + C", $"{a}x*sin({k}x) + C", $"-{sinTerm} + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int k = rng.Next(2, 5);
                    string formula = $"int {a}x*sin({k}x) dx";
                    int k2 = k * k;
                    string cosTerm = (a % k == 0) ? $"-{a / k}x*cos({k}x)" : $"-\\frac{{{a}x}}{{{k}}}*cos({k}x)";
                    string sinTerm = (a % k2 == 0) ? $"{a / k2}*sin({k}x)" : $"\\frac{{{a}}}{{{k2}}}*sin({k}x)";
                    string ans = $"{cosTerm} + {sinTerm} + C";
                    string steps = $"{intLabel} (Partes): $u = {a}x,\\; dv = \\sin({k}x)dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_trig_axsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{cosTerm} - {sinTerm} + C", $"{a}x*sin({k}x) + C", $"-{cosTerm} + C", rng);
                    return q;
                });

                // 2. Exp Parts (Simple & Composite)
                expPool.Add(() =>
                {
                    string formula = "int x*e^x dx";
                    string ans = "(x - 1)*e^x + C";
                    string steps = $"{intLabel} (Partes): $u = x,\\; dv = e^x dx \\implies x e^x - e^x + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_exp_xex" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "(x + 1)*e^x + C", "x*e^x + C", "e^x + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int k = rng.Next(2, 5);
                    string aStr = a == 1 ? "" : $"{a}";
                    string formula = $"int {aStr}x*e^({k}x) dx";
                    int k2 = k * k;
                    string xTerm = (a % k == 0) ? $"{a / k}x*e^({k}x)" : $"\\frac{{{a}}}{{{k}}}x*e^({k}x)";
                    string constTerm = (a % k2 == 0) ? $"{a / k2}*e^({k}x)" : $"\\frac{{{a}}}{{{k2}}}*e^({k}x)";
                    string ans = $"{xTerm} - {constTerm} + C";
                    string steps = $"{intLabel} (Partes): $u = {aStr}x,\\; dv = e^{{{k}x}}dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_exp_axekx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{xTerm} + {constTerm} + C", $"{aStr}x*e^({k}x) + C", $"\\frac{{{aStr}x^2}}{{2}}*e^({k}x) + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"int ({a}x + {b})*e^x dx";
                    string ans = $"({a}x + {b - a})*e^x + C";
                    string steps = $"{intLabel} (Partes): $u = {a}x + {b},\\; dv = e^x dx \\implies ({a}x + {b})e^x - {a}e^x + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_exp_linear_ex" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"({a}x + {b + a})*e^x + C", $"({a}x + {b})*e^x + C", $"{a}*e^x + C", rng);
                    return q;
                });

                // 3. Log Parts (Simple & Composite)
                logPool.Add(() =>
                {
                    string formula = "int ln(x) dx";
                    string ans = "x*ln(x) - x + C";
                    string steps = $"{intLabel} (Partes): $u = \\ln(x),\\; dv = dx \\implies x\\ln(x) - \\int x \\cdot \\frac{{1}}{{x}}dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_log_lnx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x*ln(x) + x + C", "1/x + C", "x*ln(x) + C", rng);
                    return q;
                });
                logPool.Add(() =>
                {
                    int k = rng.Next(2, 8);
                    string formula = $"int ln({k}x) dx";
                    string ans = $"x*ln({k}x) - x + C";
                    string steps = $"{intLabel} (Partes): $u = \\ln({k}x),\\; dv = dx \\implies x\\ln({k}x) - \\int x \\cdot \\frac{{{k}}}{{{k}x}}dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_log_lnkx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"x*ln({k}x) + x + C", $"\\frac{{1}}{{{k}x}} + C", $"x*ln({k}x) + C", rng);
                    return q;
                });
                logPool.Add(() =>
                {
                    int n = rng.Next(1, 4);
                    int n1 = n + 1;
                    int n1Sq = n1 * n1;
                    string xPow = n == 1 ? "x" : $"x^{{{n}}}";
                    string formula = $"int {xPow}*ln(x) dx";
                    string ans = $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) - \\frac{{1}}{{{n1Sq}}}x^{{{n1}}} + C";
                    string steps = $"{intLabel} (Partes): $u = \\ln(x),\\; dv = {xPow}dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_log_xnpow_lnx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) + \\frac{{1}}{{{n1Sq}}}x^{{{n1}}} + C", $"x^{{{n1}}}*ln(x) - x^{{{n1}}} + C", $"\\frac{{1}}{{{n1}}}x^{{{n1}}}*ln(x) + C", rng);
                    return q;
                });

                // 4. InvTrig Parts (Simple & Composite)
                invTrigPool.Add(() =>
                {
                    string formula = "int arctan(x) dx";
                    string ans = "x*arctan(x) - 1/2*ln(1 + x^2) + C";
                    string steps = $"{intLabel} (Partes): $u = \\arctan(x),\\; dv = dx \\implies x\\arctan(x) - \\int \\frac{{x}}{{1 + x^2}}dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_invtrig_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x*arctan(x) + 1/2*ln(1 + x^2) + C", "x*arctan(x) - ln(1 + x^2) + C", "1/(1 + x^2) + C", rng);
                    return q;
                });
                invTrigPool.Add(() =>
                {
                    string formula = "int arcsin(x) dx";
                    string ans = "x*arcsin(x) + sqrt(1 - x^2) + C";
                    string steps = $"{intLabel} (Partes): $u = \\arcsin(x),\\; dv = dx \\implies x\\arcsin(x) - \\int \\frac{{x}}{{\\sqrt{{1-x^2}}}}dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_invtrig_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x*arcsin(x) - sqrt(1 - x^2) + C", "x*arcsin(x) + C", "\\frac{1}{\\sqrt{1-x^2}} + C", rng);
                    return q;
                });
                invTrigPool.Add(() =>
                {
                    string formula = "int x*arctan(x) dx";
                    string ans = "1/2*(x^2 + 1)*arctan(x) - 1/2*x + C";
                    string steps = $"{intLabel} (Partes): $u = \\arctan(x),\\; dv = x\\,dx \\implies \\frac{{x^2}}{{2}}\\arctan(x) - \\frac{{1}}{{2}}\\int \\frac{{x^2}}{{1+x^2}}dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_parts_invtrig_xarctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1/2*x^2*arctan(x) + C", "x*arctan(x) - 1/2*x + C", "1/2*arctan(x) + C", rng);
                    return q;
                });

                var activeCategories = new List<List<Func<Question>>>();
                if (trig && trigPool.Count > 0) activeCategories.Add(trigPool);
                if (exp && expPool.Count > 0) activeCategories.Add(expPool);
                if (log && logPool.Count > 0) activeCategories.Add(logPool);
                if (invTrig && invTrigPool.Count > 0) activeCategories.Add(invTrigPool);

                if (activeCategories.Count == 0)
                {
                    if (trigPool.Count > 0) activeCategories.Add(trigPool);
                    if (expPool.Count > 0) activeCategories.Add(expPool);
                    if (logPool.Count > 0) activeCategories.Add(logPool);
                    if (invTrigPool.Count > 0) activeCategories.Add(invTrigPool);
                }

                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }
            else
            {
                // Standard Indefinite Integration (indef_power and others)
                var polyPool = new List<Func<Question>>();
                var ratPool = new List<Func<Question>>();
                var radPool = new List<Func<Question>>();
                var trigPool = new List<Func<Question>>();
                var expPool = new List<Func<Question>>();
                var mixedPool = new List<Func<Question>>();

                // 1. Polynomial Pool: 6 distinct archetypes
                polyPool.Add(() =>
                {
                    int p1 = comp == 0 ? rng.Next(1, 4) : (comp == 1 ? rng.Next(2, 5) : rng.Next(3, 8));
                    int c1 = rng.Next(2, 9);
                    int newP = p1 + 1;
                    string formula = $"int {c1}x^{{{p1}}} dx";
                    string ans = (c1 % newP == 0) ? $"{c1 / newP}x^{{{newP}}} + C" : $"{c1}/{newP} x^{{{newP}}} + C";
                    string steps = $"{intLabel}: $\\int x^n\\,dx = \\frac{{x^{{n+1}}}}{{n+1}} \\Rightarrow {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_monomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{c1 * newP}x^{{{newP}}} + C", $"{c1}x^{{{p1}}} + C", $"{c1}x^{{{newP}}} + C", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    // Multi-term polynomial
                    int c1 = 3 * rng.Next(1, 4);
                    int c2 = 2 * rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
                    int c3 = rng.Next(1, 9) * (rng.Next(2) == 0 ? 1 : -1);
                    string c2Sign = c2 > 0 ? $"+ {c2}x" : $"- {Math.Abs(c2)}x";
                    string c3Sign = c3 > 0 ? $"+ {c3}" : $"- {Math.Abs(c3)}";
                    string formula = $"int ({c1}x^2 {c2Sign} {c3Sign}) dx";
                    int a1 = c1 / 3;
                    int a2 = c2 / 2;
                    string a1Str = a1 == 1 ? "x^3" : $"{a1}x^3";
                    string a2Str = a2 == 1 ? "+ x^2" : (a2 == -1 ? "- x^2" : (a2 > 0 ? $"+ {a2}x^2" : $"- {Math.Abs(a2)}x^2"));
                    string a3Str = c3 > 0 ? $"+ {c3}x" : $"- {Math.Abs(c3)}x";
                    string ans = $"{a1Str} {a2Str} {a3Str} + C";
                    string steps = $"{intLabel}: $\\int ({c1}x^2 {c2Sign} {c3Sign})\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_multiterm" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{c1}x^3 {a2Str} + C", $"{a1Str} {c2Sign} + C", $"{a1Str} - x^2 + C", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    // Composite Binomial Power: int (ax + b)^n dx
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);
                    string bStr = b > 0 ? $"+ {b}" : $"- {Math.Abs(b)}";
                    int n = comp == 0 ? rng.Next(2, 4) : rng.Next(3, 6);
                    int n1 = n + 1;
                    int den = a * n1;
                    string formula = $"int ({a}x {bStr})^{{{n}}} dx";
                    string ans = $"1/{den} * ({a}x {bStr})^{{{n1}}} + C";
                    string steps = $"{intLabel}: $u = {a}x {bStr},\\; du = {a}dx \\Rightarrow \\frac{{1}}{{{a}}}\\int u^{{{n}}}du = \\frac{{1}}{{{den}}}u^{{{n1}}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_composite_binomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{n1} * ({a}x {bStr})^{{{n1}}} + C", $"{a}/{n1} * ({a}x {bStr})^{{{n1}}} + C", $"1/{den} * ({a}x {bStr})^{{{n}}} + C", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    // Cubic polynomial: int (4x^3 - 3x^2 + 2x) dx
                    string formula = "int (4x^3 - 3x^2 + 2x) dx";
                    string ans = "x^4 - x^3 + x^2 + C";
                    string steps = $"{intLabel}: $\\int (4x^3 - 3x^2 + 2x)\\,dx = x^4 - x^3 + x^2 + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_cubic_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x^4 + x^3 + x^2 + C", "4x^4 - 3x^3 + 2x^2 + C", "x^4 - x^3 + C", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    // Negative / reciprocal power rewritten: int (3/x^2 + 2/x^3) dx
                    int a = rng.Next(2, 6);
                    string formula = $"int \\frac{{x^3 + {a}}}{{x^2}} dx";
                    string ans = $"1/2 x^2 - {a}/x + C";
                    string steps = $"{intLabel}: $\\int \\left(x + \\frac{{{a}}}{{x^2}}\\right)\\,dx = \\frac{{x^2}}{{2}} - \\frac{{{a}}}{{x}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_reciprocal_dist" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/2 x^2 + {a}/x + C", $"x^2 - {a}/x + C", $"{a}x^2 + C", rng);
                    return q;
                });
                polyPool.Add(() =>
                {
                    // Factored product: int x(x + a) dx
                    int a = rng.Next(2, 6);
                    string formula = $"int x(x + {a}) dx";
                    string ans = $"1/3 x^3 + {a}/2 x^2 + C";
                    string steps = $"{intLabel}: $\\int (x^2 + {a}x)\\,dx = \\frac{{x^3}}{{3}} + \\frac{{{a}x^2}}{{2}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_power_factored_product" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/3 x^3 + {a}x^2 + C", $"x^3 + {a}x^2 + C", $"1/2 x^2 + {a}x + C", rng);
                    return q;
                });

                // 2. Trigonometric Pool (6 archetypes)
                trigPool.Add(() =>
                {
                    int a = rng.Next(2, 7);
                    bool isSin = rng.Next(2) == 0;
                    string formula = isSin ? $"int {a}*sin(x) dx" : $"int {a}*cos(x) dx";
                    string ans = isSin ? $"-{a}*cos(x) + C" : $"{a}*sin(x) + C";
                    string steps = isSin
                        ? $"{intLabel}: $\\int {a}\\sin(x)\\,dx = -{a}\\cos(x) + C$"
                        : $"{intLabel}: $\\int {a}\\cos(x)\\,dx = {a}\\sin(x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = isSin ? "indef_trig_simple_sin" : "indef_trig_simple_cos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, isSin ? $"{a}*cos(x) + C" : $"-{a}*sin(x) + C", $"{a}*tan(x) + C", $"1/{a} * sin(x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    string formula = $"int cos({k}x) dx";
                    string ans = $"1/{k} * sin({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\cos({k}x)\\,dx = \\frac{{1}}{{{k}}}\\sin({k}x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_cos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-1/{k} * sin({k}x) + C", $"{k} * sin({k}x) + C", $"1/{k} * cos({k}x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    string formula = $"int sin({k}x) dx";
                    string ans = $"-1/{k} * cos({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sin({k}x)\\,dx = -\\frac{{1}}{{{k}}}\\cos({k}x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_sin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{k} * cos({k}x) + C", $"{k} * cos({k}x) + C", $"-sin({k}x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 8);
                    string formula = $"int sec^2({k}x) dx";
                    string ans = $"1/{k} * tan({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sec^2({k}x)\\,dx = \\frac{{1}}{{{k}}}\\tan({k}x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_sec2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-1/{k} * tan({k}x) + C", $"{k} * tan({k}x) + C", $"1/{k} * sec({k}x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    int k = rng.Next(2, 8);
                    string formula = $"int sec({k}x)*tan({k}x) dx";
                    string ans = $"1/{k} * sec({k}x) + C";
                    string steps = $"{intLabel}: $\\int \\sec({k}x)\\tan({k}x)\\,dx = \\frac{{1}}{{{k}}}\\sec({k}x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_sectan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-1/{k} * sec({k}x) + C", $"{k} * sec({k}x) + C", $"1/{k} * tan({k}x) + C", rng);
                    return q;
                });
                trigPool.Add(() =>
                {
                    string formula = "int (sin(x) + cos(x)) dx";
                    string ans = "-cos(x) + sin(x) + C";
                    string steps = $"{intLabel}: $\\int (\\sin(x) + \\cos(x))\\,dx = -\\cos(x) + \\sin(x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_sum_sincos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "cos(x) + sin(x) + C", "-cos(x) - sin(x) + C", "sin(x) + C", rng);
                    return q;
                });

                // 3. Exponential Pool (6 archetypes)
                expPool.Add(() =>
                {
                    int a = rng.Next(2, 8);
                    string formula = $"int {a}*e^x dx";
                    string ans = $"{a}*e^x + C";
                    string steps = $"{intLabel}: $\\int {a}e^x\\,dx = {a}e^x + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_simple" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{a} * e^x + C", $"{a}x*e^x + C", $"{a}*e^(x+1) + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int k = rng.Next(2, 9);
                    string formula = $"int e^({k}x) dx";
                    string ans = $"1/{k} * e^({k}x) + C";
                    string steps = $"{intLabel}: $\\int e^{{{k}x}}\\,dx = \\frac{{1}}{{{k}}}e^{{{k}x}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_comp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k} * e^({k}x) + C", $"e^({k}x) + C", $"1/{k} * e^x + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    string formula = "int (e^x + e^(-x)) dx";
                    string ans = "e^x - e^(-x) + C";
                    string steps = $"{intLabel}: $\\int (e^x + e^{{-x}})\\,dx = e^x - e^{{-x}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_hyperbolic_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "e^x + e^(-x) + C", "-e^x - e^(-x) + C", "2e^x + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    int a = rng.Next(1, 6);
                    string formula = $"int (e^({k}x) + {a}) dx";
                    string ans = $"1/{k} * e^({k}x) + {a}x + C";
                    string steps = $"{intLabel}: $\\int (e^{{{k}x}} + {a})\\,dx = \\frac{{1}}{{{k}}}e^{{{k}x}} + {a}x + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_linear_shift" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{k} * e^({k}x) + {a}x + C", $"1/{k} * e^({k}x) + {a} + C", $"e^({k}x) + {a}x + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int[] bases = { 2, 3, 5 };
                    int b = bases[rng.Next(bases.Length)];
                    string formula = $"int {b}^x dx";
                    string ans = $"{b}^x / ln({b}) + C";
                    string steps = $"{intLabel}: $\\int {b}^x\\,dx = \\frac{{{b}^x}}{{\\ln({b})}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_general_base" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{b}^x * ln({b}) + C", $"{b}^(x+1) / (x+1) + C", $"{b}^x + C", rng);
                    return q;
                });
                expPool.Add(() =>
                {
                    int a = rng.Next(1, 6);
                    string formula = $"int e^(x + {a}) dx";
                    string ans = $"e^(x + {a}) + C";
                    string steps = $"{intLabel}: $\\int e^{{x + {a}}}\\,dx = e^{{x + {a}}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_exp_shifted" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{a} * e^(x + {a}) + C", $"{a} * e^(x + {a}) + C", $"e^x + {a}x + C", rng);
                    return q;
                });

                // 4. Rational Pool (6 archetypes)
                ratPool.Add(() =>
                {
                    int a = rng.Next(2, 12);
                    string formula = $"int {a}/x dx";
                    string ans = $"{a}*ln|x| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{{a}}}{{x}}\\,dx = {a}\\ln|x| + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_ln" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/{a} * ln|x| + C", $"{a}/x^2 + C", $"-{a}/x^2 + C", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    int a = rng.Next(2, 9);
                    string formula = $"int {a}/x^2 dx";
                    string ans = $"-{a}/x + C";
                    string steps = $"{intLabel}: $\\int {a}x^{{-2}}\\,dx = -\\frac{{{a}}}{{x}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_inv_sq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}/x + C", $"-{a}/x^3 + C", $"{a}*ln|x^2| + C", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 5);
                    int c = rng.Next(1, 6);
                    string formula = $"int {a}/({b}x + {c})^2 dx";
                    string ans = $"-{a}/{b} * 1/({b}x + {c}) + C";
                    string steps = $"{intLabel}: $u = {b}x + {c},\\; du = {b}dx \\Rightarrow -\\frac{{{a}}}{{{b}({b}x + {c})}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_comp_linear_sq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}/{b} * 1/({b}x + {c}) + C", $"-{a} / ({b}x + {c}) + C", $"{a}*ln|{b}x + {c}| + C", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    int a = rng.Next(1, 9);
                    string formula = $"int 1/(x + {a}) dx";
                    string ans = $"ln|x + {a}| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{x + {a}}}\\,dx = \\ln|x + {a}| + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_shifted_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"1/(x + {a})^2 + C", $"-1/(x + {a}) + C", $"ln|x| + {a}x + C", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    int a = rng.Next(2, 8);
                    string formula = $"int {a}/x^3 dx";
                    string ans = $"-{a}/(2x^2) + C";
                    string steps = $"{intLabel}: $\\int {a}x^{{-3}}\\,dx = -\\frac{{{a}}}{{2x^2}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_cubic_recip" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}/(2x^2) + C", $"-{a}/x^2 + C", $"{a}*ln|x^3| + C", rng);
                    return q;
                });
                ratPool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"int ({a}x + 1)/x dx";
                    string ans = $"{a}x + ln|x| + C";
                    string steps = $"{intLabel}: $\\int \\left({a} + \\frac{{1}}{{x}}\\right)\\,dx = {a}x + \\ln|x| + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rat_split_fraction" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a}x - ln|x| + C", $"1/2*{a}x^2 + ln|x| + C", $"{a}*ln|x| + C", rng);
                    return q;
                });

                // 5. Radical Pool (6 archetypes)
                radPool.Add(() =>
                {
                    int a = rng.Next(1, 8);
                    string aStr = a == 1 ? "" : $"{a}";
                    string formula = $"int {aStr}sqrt(x) dx";
                    int num = 2 * a;
                    string ans = (num % 3 == 0) ? $"{num / 3} * x^(3/2) + C" : $"{num}/3 * x^(3/2) + C";
                    string steps = $"{intLabel}: $\\int {aStr}x^{{1/2}}\\,dx = {a}\\cdot\\frac{{x^{{3/2}}}}{{3/2}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_simple" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{num} * x^(3/2) + C", $"1/(2*sqrt(x)) + C", $"{a} * x^(1/2) + C", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    int a = rng.Next(1, 8);
                    string aStr = a == 1 ? "1" : $"{a}";
                    string formula = $"int {aStr}/sqrt(x) dx";
                    int num = 2 * a;
                    string ans = $"{num} * sqrt(x) + C";
                    string steps = $"{intLabel}: $\\int {aStr}x^{{-1/2}}\\,dx = {a}\\cdot\\frac{{x^{{1/2}}}}{{1/2}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_recip" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{a} * sqrt(x) + C", $"\\frac{{{a}}}{{2}} * sqrt(x) + C", $"{num}x + C", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 6);
                    string formula = $"int sqrt({a}x + {b}) dx";
                    int den = 3 * a;
                    string ans = $"2/{den} * ({a}x + {b})^(3/2) + C";
                    string steps = $"{intLabel}: $u = {a}x + {b},\\; du = {a}dx \\Rightarrow \\frac{{1}}{{{a}}}\\int u^{{1/2}}du = \\frac{{2}}{{{den}}}u^{{3/2}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_comp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"2/3 * ({a}x + {b})^(3/2) + C", $"1/{den} * ({a}x + {b})^(3/2) + C", $"2/{den} * sqrt({a}x + {b}) + C", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    string formula = "int x^(1/3) dx";
                    string ans = "3/4 * x^(4/3) + C";
                    string steps = $"{intLabel}: $\\int x^{{1/3}}\\,dx = \\frac{{x^{{4/3}}}}{{4/3}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_cbrt" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "4/3 * x^(4/3) + C", "3/4 * x^(1/3) + C", "1/3 * x^(4/3) + C", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    string formula = "int 1/x^(1/3) dx";
                    string ans = "3/2 * x^(2/3) + C";
                    string steps = $"{intLabel}: $\\int x^{{-1/3}}\\,dx = \\frac{{x^{{2/3}}}}{{2/3}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_recip_cbrt" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "2/3 * x^(2/3) + C", "3/2 * x^(-1/3) + C", "-3 * x^(2/3) + C", rng);
                    return q;
                });
                radPool.Add(() =>
                {
                    string formula = "int x*sqrt(x) dx";
                    string ans = "2/5 * x^(5/2) + C";
                    string steps = $"{intLabel}: $\\int x^{{3/2}}\\,dx = \\frac{{2}}{{5}}x^{{5/2}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_rad_x_sqrtx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "5/2 * x^(5/2) + C", "2/3 * x^(3/2) + C", "2/5 * x^2 + C", rng);
                    return q;
                });

                // 6. Mixed Sums Pool (6 archetypes)
                mixedPool.Add(() =>
                {
                    int c1 = rng.Next(2, 6) * (rng.Next(2) == 0 ? 1 : -1);
                    int c2 = rng.Next(2, 8);
                    string formula = $"int ({c1}x + {c2}*cos(x)) dx";
                    string ans = $"{c1}/2 x^2 + {c2}*sin(x) + C";
                    string steps = $"{intLabel}: $\\int ({c1}x + {c2}\\cos(x))\\,dx = \\frac{{{c1}}}{{2}}x^2 + {c2}\\sin(x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_poly_trig" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{c1}x^2 - {c2}*sin(x) + C", $"{c1}/2 x^2 - {c2}*sin(x) + C", $"{c1}x^2 + {c2}*cos(x) + C", rng);
                    return q;
                });
                mixedPool.Add(() =>
                {
                    int c1 = rng.Next(2, 8);
                    string formula = $"int ({c1}x + e^x) dx";
                    string ans = $"{c1}/2 x^2 + e^x + C";
                    string steps = $"{intLabel}: $\\int ({c1}x + e^x)\\,dx = \\frac{{{c1}}}{{2}}x^2 + e^x + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_poly_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{c1}x^2 + e^x + C", $"{c1}/2 x^2 + x*e^x + C", $"{c1}x + e^x + C", rng);
                    return q;
                });
                mixedPool.Add(() =>
                {
                    string formula = "int (2x + 1/x) dx";
                    string ans = "x^2 + ln|x| + C";
                    string steps = $"{intLabel}: $\\int (2x + \\frac{{1}}{{x}})\\,dx = x^2 + \\ln|x| + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_poly_rat" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "2x^2 + ln|x| + C", "x^2 - ln|x| + C", "x^2 + 1/x^2 + C", rng);
                    return q;
                });
                mixedPool.Add(() =>
                {
                    string formula = "int (2x + sqrt(x)) dx";
                    string ans = "x^2 + 2/3 * x^(3/2) + C";
                    string steps = $"{intLabel}: $\\int (2x + x^{{1/2}})\\,dx = x^2 + \\frac{{2}}{{3}}x^{{3/2}} + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_poly_rad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "x^2 + 3/2 * x^(3/2) + C", "2x^2 + 2/3 * x^(3/2) + C", "x^2 + sqrt(x) + C", rng);
                    return q;
                });
                mixedPool.Add(() =>
                {
                    string formula = "int (e^x + cos(x)) dx";
                    string ans = "e^x + sin(x) + C";
                    string steps = $"{intLabel}: $\\int (e^x + \\cos(x))\\,dx = e^x + \\sin(x) + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_exp_trig" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "e^x - sin(x) + C", "e^x + cos(x) + C", "x*e^x + sin(x) + C", rng);
                    return q;
                });
                mixedPool.Add(() =>
                {
                    string formula = "int (cos(x) + 1/x) dx";
                    string ans = "sin(x) + ln|x| + C";
                    string steps = $"{intLabel}: $\\int (\\cos(x) + \\frac{{1}}{{x}})\\,dx = \\sin(x) + \\ln|x| + C$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_mixed_trig_rat" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-sin(x) + ln|x| + C", "sin(x) - ln|x| + C", "cos(x) + ln|x| + C", rng);
                    return q;
                });

                // Collect active categories based on user's selected checkboxes
                var activeCategories = new List<List<Func<Question>>>();
                if (poly && polyPool.Count > 0) activeCategories.Add(polyPool);
                if (trig && trigPool.Count > 0) activeCategories.Add(trigPool);
                if (exp && expPool.Count > 0) activeCategories.Add(expPool);
                if ((rat || log) && ratPool.Count > 0) activeCategories.Add(ratPool);
                if (rad && radPool.Count > 0) activeCategories.Add(radPool);
                if (mixedPool.Count > 0) activeCategories.Add(mixedPool);

                if (activeCategories.Count == 0)
                {
                    if (polyPool.Count > 0) activeCategories.Add(polyPool);
                    if (trigPool.Count > 0) activeCategories.Add(trigPool);
                    if (expPool.Count > 0) activeCategories.Add(expPool);
                    if (ratPool.Count > 0) activeCategories.Add(ratPool);
                    if (radPool.Count > 0) activeCategories.Add(radPool);
                    if (mixedPool.Count > 0) activeCategories.Add(mixedPool);
                }

                // Sample fairly across all selected function categories
                var chosenCategory = activeCategories[Math.Abs(index) % activeCategories.Count];
                int pick = (Math.Abs(index) / activeCategories.Count) % chosenCategory.Count;
                return chosenCategory[pick]();
            }
        }

        public static Question GenerateDefiniteIntegrationProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng, string topicId = "def_eval")
        {
            bool poly = HasPoly(p);
            bool trig = HasTrig(p);
            bool exp = HasExp(p);
            bool rat = HasRational(p);

            if (!poly && !trig && !exp && !rat)
            {
                poly = true;
                trig = true;
                exp = true;
                rat = true;
            }

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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_trig_sin" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_trig_cos" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_trig_k_sin" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_exp_comp" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_exp_linear" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_rat_ln" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"ln({b})", $"\\frac{{{k}}}{{{b}}}", $"{k}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "int [0 to 1] 1/(1 + x^2) dx";
                    string ans = "\\frac{\\pi}{4}";
                    string steps = $"{intLabel}: $\\left[ \\arctan(x) \\right]_0^1 = \\arctan(1) - \\arctan(0) = \\frac{{\\pi}}{{4}}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_rat_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\frac{\\pi}{2}", "1", "\\pi", rng);
                    return q;
                });
            }

            if (poly || pool.Count == 0)
            {
                // 1. Linear
                pool.Add(() =>
                {
                    int c1 = rng.Next(1, 6);
                    int a = rng.Next(-2, 3);
                    int b = a + rng.Next(1, 5);
                    string formula = $"int [{a} to {b}] ({2 * c1}x) dx";
                    int ansVal = c1 * (b * b - a * a);
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ {c1}x^2 \\right]_{{{a}}}^{{{b}}} = {c1}({b})^2 - {c1}({a})^2 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), (ansVal - 3).ToString(), (ansVal * 2).ToString(), rng);
                    return q;
                });

                // 2. Pure quadratic monomial
                pool.Add(() =>
                {
                    int a = rng.Next(0, 3);
                    int b = a + rng.Next(1, 4);
                    string formula = $"int [{a} to {b}] (3x^2) dx";
                    int ansVal = b * b * b - a * a * a;
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ x^3 \\right]_{{{a}}}^{{{b}}} = {b}^3 - {a}^3 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_quad_monomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 5).ToString(), (ansVal - 5).ToString(), (ansVal * 2).ToString(), rng);
                    return q;
                });

                // 3. Quadratic polynomial
                pool.Add(() =>
                {
                    int b = rng.Next(2, 5);
                    string formula = $"int [0 to {b}] (3x^2 - 2x + 1) dx";
                    int ansVal = b * b * b - b * b + b;
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ x^3 - x^2 + x \\right]_0^{{{b}}} = {b}^3 - {b}^2 + {b} = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_quad_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 3).ToString(), (ansVal - 2).ToString(), (ansVal * 2).ToString(), rng);
                    return q;
                });

                // 4. Cubic polynomial
                pool.Add(() =>
                {
                    int b = rng.Next(2, 4);
                    string formula = $"int [0 to {b}] (4x^3 - 2x) dx";
                    int ansVal = b * b * b * b - b * b;
                    string ans = ansVal.ToString();
                    string steps = $"{intLabel}: $\\left[ x^4 - x^2 \\right]_0^{{{b}}} = {b}^4 - {b}^2 = {ansVal}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_cubic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 4).ToString(), (ansVal - 4).ToString(), (ansVal + 8).ToString(), rng);
                    return q;
                });

                // 5. Shifted binomial square
                pool.Add(() =>
                {
                    int b = rng.Next(1, 4);
                    string formula = $"int [0 to {b}] (x + 1)^2 dx";
                    int num = (b + 1) * (b + 1) * (b + 1) - 1;
                    string ans = (num % 3 == 0) ? (num / 3).ToString() : $"{num}/3";
                    string steps = $"{intLabel}: $\\left[ \\frac{{(x + 1)^3}}{{3}} \\right]_0^{{{b}}} = \\frac{{{b + 1}^3 - 1}}{{3}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_shifted_binomial" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, (num + 2).ToString(), "1", $"{num}/2", rng);
                    return q;
                });

                // 6. Reciprocal square
                pool.Add(() =>
                {
                    int b = rng.Next(2, 6);
                    string formula = $"int [1 to {b}] 1/x^2 dx";
                    int num = b - 1;
                    int den = b;
                    string ans = $"{num}/{den}";
                    string steps = $"{intLabel}: $\\left[ -\\frac{{1}}{{x}} \\right]_1^{{{b}}} = -\\frac{{1}}{{{b}}} - (-1) = 1 - \\frac{{1}}{{{b}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "def_ftc1_recip_sq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "1", $"1/{den}", $"{num + 1}/{den}", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateRiemannTableProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int method = p?.ComplexityOptionIndex ?? 4;
            if (method < 0 || method > 4) method = 4;
            if (method == 4)
            {
                method = Math.Abs(index) % 4;
            }

            int arch = Math.Abs(index) % 6;

            int[] xVals;
            int[] yVals;
            int dx = 1;
            int a, b;
            int ansVal;
            string methodLabel;
            string stepsFormula;
            string ansSymbol;

            if (method == 2) // Midpoint (MRAM)
            {
                int numSub = (arch % 2 == 0) ? 2 : 3;
                int step = (arch / 2) + 1; // 1, 2, or 3
                dx = 2 * step;
                int totalPoints = 2 * numSub + 1;
                int x0 = rng.Next(0, 3);
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
            else if (arch == 5) // Non-uniform (unequal) subintervals
            {
                int[] widths = (method == 3) ? new[] { 2, 4, 2, 6 } : new[] { 2, 3, 1, 4 };
                int n = widths.Length;
                int x0 = rng.Next(0, 3);
                xVals = new int[n + 1];
                xVals[0] = x0;
                for (int j = 0; j < n; j++) xVals[j + 1] = xVals[j] + widths[j];

                yVals = new int[n + 1];
                int curY = rng.Next(2, 7);
                for (int j = 0; j <= n; j++)
                {
                    yVals[j] = curY;
                    curY += rng.Next(1, 4) * (rng.Next(4) == 0 ? -1 : 1);
                    if (curY < 1) curY = 1;
                }

                a = xVals[0];
                b = xVals[^1];

                if (method == 0) // LRAM unequal
                {
                    int sum = 0;
                    var terms = new List<string>();
                    for (int i = 0; i < n; i++)
                    {
                        int termVal = yVals[i] * widths[i];
                        sum += termVal;
                        terms.Add($"{yVals[i]}({widths[i]})");
                    }
                    ansVal = sum;
                    ansSymbol = $"L_{{{n}}}";
                    stepsFormula = $"{string.Join(" + ", terms)} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Extremo izquierdo (LRAM, subintervalos desiguales)",
                        AppLanguage.Portuguese => "Extremo esquerdo (LRAM, subintervalos desiguais)",
                        _ => "Left endpoint (LRAM, unequal subintervals)"
                    };
                }
                else if (method == 1) // RRAM unequal
                {
                    int sum = 0;
                    var terms = new List<string>();
                    for (int i = 0; i < n; i++)
                    {
                        int termVal = yVals[i + 1] * widths[i];
                        sum += termVal;
                        terms.Add($"{yVals[i + 1]}({widths[i]})");
                    }
                    ansVal = sum;
                    ansSymbol = $"R_{{{n}}}";
                    stepsFormula = $"{string.Join(" + ", terms)} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Extremo derecho (RRAM, subintervalos desiguales)",
                        AppLanguage.Portuguese => "Extremo direito (RRAM, subintervalos desiguais)",
                        _ => "Right endpoint (RRAM, unequal subintervals)"
                    };
                }
                else // Trapezoid unequal
                {
                    int sum = 0;
                    var terms = new List<string>();
                    for (int i = 0; i < n; i++)
                    {
                        int termVal = ((yVals[i] + yVals[i + 1]) * widths[i]) / 2;
                        sum += termVal;
                        terms.Add($"\\frac{{{yVals[i]} + {yVals[i + 1]}}}{{2}}({widths[i]})");
                    }
                    ansVal = sum;
                    ansSymbol = $"T_{{{n}}}";
                    stepsFormula = $"{string.Join(" + ", terms)} = {ansVal}";
                    methodLabel = LocalizationService.CurrentLanguage switch
                    {
                        AppLanguage.Spanish => "Regla del trapecio (subintervalos desiguales)",
                        AppLanguage.Portuguese => "Regra do trapézio (subintervalos desiguais)",
                        _ => "Trapezoidal rule (unequal subintervals)"
                    };
                }
            }
            else // Uniform subintervals (arch 0, 1, 2, 3, 4)
            {
                int n = arch switch
                {
                    0 => 3,
                    1 => 3,
                    2 => 4,
                    3 => 4,
                    _ => 5
                };
                dx = arch switch
                {
                    0 => 1,
                    1 => 2,
                    2 => 1,
                    3 => 3,
                    _ => 2
                };
                int x0 = rng.Next(0, 4);
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
            string steps = (arch == 5 && method != 2)
                ? $"{methodLabel}: ${ansSymbol} = {stepsFormula}$"
                : $"{methodLabel}: $\\Delta x = {dx}$. ${ansSymbol} = {stepsFormula}$";

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps,
                TemplateId = $"riemann_table_m{method}_arch{arch}"
            };

            if (type == QuestionType.MultipleChoice)
            {
                int delta = (arch == 5 && method != 2) ? 4 : dx * 2;
                if (delta == 0) delta = 2;
                q.Choices = DistractorHelper.BuildChoices(
                    ans,
                    $"{ansSymbol} = {ansVal + delta}",
                    $"{ansSymbol} = {ansVal - delta}",
                    $"{ansSymbol} = {ansVal + delta / 2 + 1}",
                    rng);
            }

            return q;
        }

        public static Question GenerateApproxAreaProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int method = p?.ComplexityOptionIndex ?? 3;
            if (method < 0 || method > 4) method = 3;
            if (method == 4) method = Math.Abs(index) % 4;

            int variant = Math.Abs(index) % 6;
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
            else if (variant == 4)
            {
                int k = rng.Next(1, 4);
                funcLatex = $"f(x) = 2x^2 + {k}";
                evalFunc = x => 2 * x * x + k;
                a = 0; b = 3; n = 3;
            }
            else
            {
                int c = rng.Next(1, 4);
                funcLatex = $"f(x) = x^3 + {c}";
                evalFunc = x => x * x * x + c;
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
                SolutionSteps = steps,
                TemplateId = $"approx_area_m{method}_v{variant}"
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
            const int totalVariants = 12;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;
            string evalLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Evaluando como integral definida:",
                AppLanguage.Portuguese => "Avaliando como integral definida:",
                _ => "Evaluating as a definite integral:"
            };

            List<QuestionChoice>? customChoices = null;

            if (variant == 0)
            {
                templateId = "limits_sums_linear";
                int aHalf = rng.Next(1, 4);
                int a = aHalf * 2;
                int b = rng.Next(1, 6);
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({a}\left(\frac{{i}}{{n}}\right) + {b}\right)\left(\frac{{1}}{{n}}\right)";
                int ansVal = aHalf + b;
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 ({a}x + {b})\,dx = \left[ {aHalf}x^2 + {b}x \right]_0^1 = {aHalf} + {b} = {ansVal}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (ansVal + 1).ToString(), (ansVal - 1).ToString(), (ansVal + 2).ToString(), rng);
            }
            else if (variant == 1)
            {
                templateId = "limits_sums_quad_monomial";
                int cThird = rng.Next(1, 5);
                int c = cThird * 3;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{i}}{{n}}\right)^2\right)\left(\frac{{1}}{{n}}\right)";
                ans = cThird.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 {c}x^2\,dx = \left[ {cThird}x^3 \right]_0^1 = {cThird}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (cThird + 1).ToString(), (cThird > 1 ? cThird - 1 : cThird + 3).ToString(), (cThird * 2).ToString(), rng);
            }
            else if (variant == 2)
            {
                templateId = "limits_sums_scaled_interval";
                int k = rng.Next(2, 5);
                int cHalf = rng.Next(1, 3);
                int c = cHalf * 2;
                int ansVal = cHalf * k * k;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{{k}i}}{{n}}\right)\right)\left(\frac{{{k}}}{{n}}\right)";
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^{{{k}}} {c}x\,dx = \left[ {cHalf}x^2 \right]_0^{{{k}}} = {ansVal}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (ansVal + k).ToString(), (ansVal - k).ToString(), (ansVal * 2).ToString(), rng);
            }
            else if (variant == 3)
            {
                templateId = "limits_sums_quad_linear";
                int aThird = rng.Next(1, 3);
                int a = aThird * 3;
                int bHalf = rng.Next(1, 3);
                int b = bHalf * 2;
                int ansVal = aThird + bHalf;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({a}\left(\frac{{i}}{{n}}\right)^2 + {b}\left(\frac{{i}}{{n}}\right)\right)\left(\frac{{1}}{{n}}\right)";
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 ({a}x^2 + {b}x)\,dx = \left[ {aThird}x^3 + {bHalf}x^2 \right]_0^1 = {aThird} + {bHalf} = {ansVal}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (ansVal + 1).ToString(), (ansVal + 2).ToString(), (ansVal > 1 ? ansVal - 1 : ansVal + 3).ToString(), rng);
            }
            else if (variant == 4)
            {
                templateId = "limits_sums_cubic_monomial";
                int cFourth = rng.Next(1, 4);
                int c = cFourth * 4;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{i}}{{n}}\right)^3\right)\left(\frac{{1}}{{n}}\right)";
                ans = cFourth.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 {c}x^3\,dx = \left[ {cFourth}x^4 \right]_0^1 = {cFourth}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (cFourth + 1).ToString(), (cFourth * 2).ToString(), (cFourth + 3).ToString(), rng);
            }
            else if (variant == 5)
            {
                templateId = "limits_sums_shifted_interval";
                int aStart = rng.Next(1, 4);
                int width = rng.Next(1, 4);
                int bEnd = aStart + width;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({aStart} + \frac{{{width}i}}{{n}}\right)^2\left(\frac{{{width}}}{{n}}\right)";
                int num = (bEnd * bEnd * bEnd) - (aStart * aStart * aStart);
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = evalLabel + "\n" + $@"\int_{{{aStart}}}^{{{bEnd}}} x^2\,dx = \left[ \frac{{x^3}}{{3}} \right]_{{{aStart}}}^{{{bEnd}}} = \frac{{{bEnd}^3 - {aStart}^3}}{{3}} = {ans}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, $@"\frac{{{num + 3}}}{{3}}", $@"\frac{{{num - 2}}}{{3}}", $@"\frac{{{num * 2}}}{{3}}", rng);
            }
            else if (variant == 6)
            {
                templateId = "limits_sums_radical";
                formula = @"\lim_{n \to \infty} \sum_{i=1}^{n} \sqrt{\frac{i}{n}}\left(\frac{1}{n}\right)";
                ans = @"\frac{2}{3}";
                steps = evalLabel + "\n" + @"\int_0^1 \sqrt{x}\,dx = \left[ \frac{2}{3}x^{3/2} \right]_0^1 = \frac{2}{3}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, @"\frac{1}{2}", @"\frac{1}{3}", @"\frac{4}{3}", rng);
            }
            else if (variant == 7)
            {
                templateId = "limits_sums_rational";
                formula = @"\lim_{n \to \infty} \sum_{i=1}^{n} \frac{1}{1 + \frac{i}{n}}\left(\frac{1}{n}\right)";
                ans = @"\ln(2)";
                steps = evalLabel + "\n" + @"\int_0^1 \frac{1}{1 + x}\,dx = \left[ \ln|1 + x| \right]_0^1 = \ln(2) - \ln(1) = \ln(2)";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, @"\ln(3)", @"1", @"\frac{1}{2}", rng);
            }
            else if (variant == 8)
            {
                templateId = "limits_sums_exponential";
                int k = rng.Next(1, 3);
                if (k == 1)
                {
                    formula = @"\lim_{n \to \infty} \sum_{i=1}^{n} e^{\frac{i}{n}}\left(\frac{1}{n}\right)";
                    ans = "e - 1";
                    steps = evalLabel + "\n" + @"\int_0^1 e^x\,dx = \left[ e^x \right]_0^1 = e - 1";
                    if (type == QuestionType.MultipleChoice)
                        customChoices = DistractorHelper.BuildChoices(ans, "e", "e + 1", @"\frac{e - 1}{2}", rng);
                }
                else
                {
                    formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} e^{{\frac{{{k}i}}{{n}}}}\left(\frac{{{k}}}{{n}}\right)";
                    ans = $@"e^{{{k}}} - 1";
                    steps = evalLabel + "\n" + $@"\int_0^{{{k}}} e^x\,dx = \left[ e^x \right]_0^{{{k}}} = e^{{{k}}} - 1";
                    if (type == QuestionType.MultipleChoice)
                        customChoices = DistractorHelper.BuildChoices(ans, $@"e^{{{k}}}", $@"e^{{{k}}} + 1", $@"2(e^{{{k}}} - 1)", rng);
                }
            }
            else if (variant == 9)
            {
                templateId = "limits_sums_trig";
                formula = @"\lim_{n \to \infty} \sum_{i=1}^{n} \sin\left(\frac{\pi i}{n}\right)\left(\frac{\pi}{n}\right)";
                ans = "2";
                steps = evalLabel + "\n" + @"\int_0^\pi \sin(x)\,dx = \left[ -\cos(x) \right]_0^\pi = -\cos(\pi) - (-\cos(0)) = 1 + 1 = 2";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, "0", "1", @"\pi", rng);
            }
            else if (variant == 10)
            {
                templateId = "limits_sums_conversion";
                int aBase = rng.Next(1, 5);
                int w = rng.Next(2, 5);
                int top = aBase + w;
                int pwr = rng.Next(2, 4);
                string prompt = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Exprese como una integral definida:",
                    AppLanguage.Portuguese => "Expresse como uma integral definida:",
                    _ => "Express as a definite integral:"
                };
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({aBase} + \frac{{{w}i}}{{n}}\right)^{{{pwr}}}\left(\frac{{{w}}}{{n}}\right)";
                ans = $@"\int_{{{aBase}}}^{{{top}}} x^{{{pwr}}}\,dx";
                steps = $"{prompt}\n" + $@"\Delta x = \frac{{{w}}}{{n}} \implies b - a = {w}.\; a = {aBase} \implies b = {top}.\; f(x) = x^{{{pwr}}} \implies \int_{{{aBase}}}^{{{top}}} x^{{{pwr}}}\,dx";
                if (type == QuestionType.MultipleChoice)
                {
                    string d1 = $@"\int_0^{{{w}}} x^{{{pwr}}}\,dx";
                    string d2 = $@"\int_{{{aBase}}}^{{{top}}} x^{{{pwr + 1}}}\,dx";
                    string d3 = $@"\int_0^{{{top}}} ({aBase} + x)\,dx";
                    customChoices = DistractorHelper.BuildChoices(ans, d1, d2, d3, rng);
                }
            }
            else
            {
                templateId = "limits_sums_cubic_linear";
                int c = rng.Next(1, 3) * 4;
                int d = rng.Next(1, 3) * 2;
                formula = $@"\lim_{{n \to \infty}} \sum_{{i=1}}^{{n}} \left({c}\left(\frac{{i}}{{n}}\right)^3 + {d}\left(\frac{{i}}{{n}}\right)\right)\left(\frac{{1}}{{n}}\right)";
                int ansVal = (c / 4) + (d / 2);
                ans = ansVal.ToString();
                steps = evalLabel + "\n" + $@"\int_0^1 ({c}x^3 + {d}x)\,dx = \left[ \frac{{{c}x^4}}{{4}} + \frac{{{d}x^2}}{{2}} \right]_0^1 = {c / 4} + {d / 2} = {ansVal}";
                if (type == QuestionType.MultipleChoice)
                    customChoices = DistractorHelper.BuildChoices(ans, (ansVal + 1).ToString(), (ansVal - 1).ToString(), (ansVal * 2).ToString(), rng);
            }

            var q = new Question
            {
                ProblemStatement = formula,
                FormulaText = formula,
                Type = type,
                AnswerText = ans,
                SolutionSteps = steps,
                TemplateId = templateId
            };
            if (type == QuestionType.MultipleChoice)
                q.Choices = customChoices ?? DistractorHelper.BuildChoices(ans, "1", "2", "4", rng);
            return q;
        }

        public static Question GenerateChangeOfVarsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 7;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "subs_change_poly_quad";
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
                templateId = "subs_change_poly_cube";
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
                templateId = "subs_change_trig";
                int k = rng.Next(1, 4);
                formula = $@"\int_0^{{\frac{{\pi}}{{2}}}} \cos(x)\sin^{{{k}}}(x)\,dx";
                int k1 = k + 1;
                ans = $@"\frac{{1}}{{{k1}}}";
                steps = $@"u = \sin(x),\; du = \cos(x)\,dx.\; x=0 \implies u=0,\; x=\frac{{\pi}}{{2}} \implies u=1.\; \int_0^1 u^{{{k}}}\,du = \left[ \frac{{u^{{{k1}}}}}{{{k1}}} \right]_0^1 = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "subs_change_exp";
                int c = rng.Next(1, 4);
                formula = $@"\int_0^{{\ln(2)}} e^x(e^x + {c})\,dx";
                int u0 = 1 + c;
                int u1 = 2 + c;
                int diff = (u1 * u1 - u0 * u0);
                ans = diff % 2 == 0 ? (diff / 2).ToString() : $@"\frac{{{diff}}}{{2}}";
                steps = $@"u = e^x + {c},\; du = e^x\,dx.\; x=0 \implies u={u0},\; x=\ln(2) \implies u={u1}.\; \int_{{{u0}}}^{{{u1}}} u\,du = \left[ \frac{{u^2}}{{2}} \right]_{{{u0}}}^{{{u1}}} = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "subs_change_rational_log";
                formula = @"\int_0^1 \frac{2x}{x^2 + 1}\,dx";
                ans = @"\ln(2)";
                steps = @"u = x^2 + 1,\; du = 2x\,dx.\; x=0 \implies u=1,\; x=1 \implies u=2.\; \int_1^2 \frac{1}{u}\,du = [\ln|u|]_1^2 = \ln(2).";
            }
            else if (variant == 5)
            {
                templateId = "subs_change_radical";
                formula = @"\int_0^{\sqrt{3}} x\sqrt{x^2 + 1}\,dx";
                ans = @"\frac{7}{3}";
                steps = @"u = x^2 + 1,\; du = 2x\,dx.\; x=0 \implies u=1,\; x=\sqrt{3} \implies u=4.\; \frac{1}{2}\int_1^4 u^{1/2}\,du = \frac{1}{2}\left[\frac{2}{3}u^{3/2}\right]_1^4 = \frac{1}{3}(8 - 1) = \frac{7}{3}.";
            }
            else
            {
                templateId = "subs_change_log_power";
                formula = @"\int_1^e \frac{(\ln(x))^2}{x}\,dx";
                ans = @"\frac{1}{3}";
                steps = @"u = \ln(x),\; du = \frac{1}{x}\,dx.\; x=1 \implies u=0,\; x=e \implies u=1.\; \int_0^1 u^2\,du = \left[\frac{u^3}{3}\right]_0^1 = \frac{1}{3}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "1", "2", "3", rng);
            return q;
        }

        public static Question GenerateMvtIntegralsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "mvt_int_quad";
                int a = rng.Next(1, 4) * 3; // multiple of 3
                int b = rng.Next(1, 4);
                formula = $@"f(x) = {a}x^2;\; [0, {b}]";
                int ansVal = (a / 3) * b * b;
                ans = ansVal.ToString();
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}-0}}\int_0^{{{b}}} {a}x^2\,dx = \frac{{1}}{{{b}}}\left[ \frac{{{a}x^3}}{{3}} \right]_0^{{{b}}} = {ansVal}.";
            }
            else if (variant == 1)
            {
                templateId = "mvt_int_linear";
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
                templateId = "mvt_int_inv_quad";
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
            else if (variant == 3)
            {
                templateId = "mvt_int_radical";
                int k = rng.Next(1, 4);
                int b = k * k;
                formula = $@"f(x) = \sqrt{{x}};\; [0, {b}]";
                int num = 2 * k;
                int den = 3;
                ans = $@"\frac{{{num}}}{{{den}}}";
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}}}\int_0^{{{b}}} x^{{1/2}}\,dx = \frac{{1}}{{{b}}}\left[ \frac{{2}}{{3}}x^{{3/2}} \right]_0^{{{b}}} = \frac{{{num}}}{{{den}}}.";
            }
            else if (variant == 4)
            {
                templateId = "mvt_int_cubic";
                int b = rng.Next(1, 4);
                formula = $@"f(x) = x^3;\; [0, {b}]";
                int num = b * b * b;
                ans = num % 4 == 0 ? (num / 4).ToString() : $@"\frac{{{num}}}{{4}}";
                steps = $@"f_{{\text{{avg}}}} = \frac{{1}}{{{b}}}\int_0^{{{b}}} x^3\,dx = \frac{{1}}{{{b}}}\left[ \frac{{x^4}}{{4}} \right]_0^{{{b}}} = {ans}.";
            }
            else
            {
                templateId = "mvt_int_trig";
                formula = @"f(x) = \sin(x);\; [0, \pi]";
                ans = @"\frac{2}{\pi}";
                steps = @"f_{\text{avg}} = \frac{1}{\pi}\int_0^\pi \sin(x)\,dx = \frac{1}{\pi}[-\cos(x)]_0^\pi = \frac{1}{\pi}(1 - (-1)) = \frac{2}{\pi}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "2", "5", "8", rng);
            return q;
        }

        public static Question GenerateFtc2Problem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "ftc2_power_root";
                int k = rng.Next(2, 4);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_1^{{x^{{{k}}}}} \sqrt{{t^3 + 1}}\,dt \right]";
                int expInner = 3 * k;
                string xDeriv = k == 2 ? $"{k}x" : $"{k}x^{{{k - 1}}}";
                ans = $@"{xDeriv}\sqrt{{x^{{{expInner}}} + 1}}";
                steps = $@"f(u(x))\cdot u'(x) = \sqrt{{(x^{{{k}}})^3 + 1}}\cdot ({xDeriv}) = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "ftc2_linear_trig";
                int a = rng.Next(2, 5);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_0^{{{a}x}} \cos(t^2)\,dt \right]";
                int a2 = a * a;
                ans = $@"{a}\cos({a2}x^2)";
                steps = $@"f(u(x))\cdot u'(x) = \cos(({a}x)^2)\cdot {a} = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "ftc2_quad_recip";
                int c = rng.Next(1, 4);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_2^{{x^2}} \frac{{1}}{{t + {c}}}\,dt \right]";
                ans = $@"\frac{{2x}}{{x^2 + {c}}}";
                steps = $@"f(u(x))\cdot u'(x) = \frac{{1}}{{x^2 + {c}}}\cdot 2x = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "ftc2_linear_poly";
                int a = rng.Next(2, 5);
                formula = $@"\frac{{d}}{{dx}} \left[ \int_1^{{{a}x}} (t^2 + 4)^2\,dt \right]";
                int a2 = a * a;
                ans = $@"{a}({a2}x^2 + 4)^2";
                steps = $@"f(u(x))\cdot u'(x) = (({a}x)^2 + 4)^2\cdot {a} = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "ftc2_quad_exp";
                formula = @"\frac{d}{dx} \left[ \int_0^{x^2} e^{t^2}\,dt \right]";
                ans = @"2x e^{x^4}";
                steps = @"f(u(x))\cdot u'(x) = e^{(x^2)^2}\cdot 2x = 2x e^{x^4}.";
            }
            else
            {
                templateId = "ftc2_cube_rat";
                formula = @"\frac{d}{dx} \left[ \int_1^{x^3} \frac{1}{t^2 + 1}\,dt \right]";
                ans = @"\frac{3x^2}{x^6 + 1}";
                steps = @"f(u(x))\cdot u'(x) = \frac{1}{(x^3)^2 + 1}\cdot 3x^2 = \frac{3x^2}{x^6 + 1}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "2x", "x^2", "1", rng);
            return q;
        }

        public static Question GenerateAreaUnderCurveProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "area_under_parabola_sym";
                int a = rng.Next(2, 5);
                int a2 = a * a;
                formula = $@"f(x) = {a2} - x^2;\; [{-a}, {a}]";
                int num = 4 * a * a * a;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_{{-{a}}}^{{{a}}} ({a2} - x^2)\,dx = 2 \left[ {a2}x - \frac{{x^3}}{{3}} \right]_0^{{{a}}} = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "area_under_parabola_origin";
                int b = rng.Next(2, 5);
                formula = $@"f(x) = x^2;\; [0, {b}]";
                int num = b * b * b;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{b}}} x^2\,dx = \left[ \frac{{x^3}}{{3}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "area_under_parabola_arch";
                int a = rng.Next(1, 4);
                int twoA = 2 * a;
                formula = $@"f(x) = {twoA}x - x^2;\; [0, {twoA}]";
                int num = 4 * a * a * a;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{twoA}}} ({twoA}x - x^2)\,dx = \left[ {a}x^2 - \frac{{x^3}}{{3}} \right]_0^{{{twoA}}} = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "area_under_radical";
                int b = rng.Next(1, 3) * 4;
                formula = $@"f(x) = \sqrt{{x}};\; [0, {b}]";
                double rootB = Math.Sqrt(b);
                int num = (int)(2 * b * rootB);
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Área}} = \int_0^{{{b}}} x^{{1/2}}\,dx = \left[ \frac{{2}}{{3}}x^{{3/2}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "area_under_cubic";
                int b = rng.Next(1, 4);
                formula = $@"f(x) = x^3;\; [0, {b}]";
                int num = b * b * b * b;
                ans = num % 4 == 0 ? (num / 4).ToString() : $@"\frac{{{num}}}{{4}}";
                steps = $@"\text{{Área}} = \int_0^{{{b}}} x^3\,dx = \left[ \frac{{x^4}}{{4}} \right]_0^{{{b}}} = {ans}.";
            }
            else
            {
                templateId = "area_under_trig_sin";
                formula = @"f(x) = \sin(x);\; [0, \pi]";
                ans = "2";
                steps = @"\text{Área} = \int_0^\pi \sin(x)\,dx = [-\cos(x)]_0^\pi = -(-1 - 1) = 2.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "12", "8", "16/3", rng);
            return q;
        }

        public static Question GenerateAreaBetweenCurvesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "area_between_line_parabola";
                int m = rng.Next(1, 4);
                formula = $@"y = {m}x,\quad y = x^2";
                int num = m * m * m;
                ans = num % 6 == 0 ? (num / 6).ToString() : $@"\frac{{{num}}}{{6}}";
                steps = $@"{m}x = x^2 \implies x=0, {m}.\; \text{{Área}} = \int_0^{{{m}}} ({m}x - x^2)\,dx = \left[ \frac{{{m}x^2}}{{2}} - \frac{{x^3}}{{3}} \right]_0^{{{m}}} = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "area_between_inv_parabola_line";
                formula = @"y = 2 - x^2,\quad y = x";
                ans = @"\frac{9}{2}";
                steps = @"2 - x^2 = x \implies x^2 + x - 2 = 0 \implies x=-2, 1.\; \text{Área} = \int_{-2}^1 (2 - x - x^2)\,dx = \frac{9}{2}.";
            }
            else if (variant == 2)
            {
                templateId = "area_between_parabola_const";
                int a = rng.Next(1, 3);
                int a2 = a * a * 4;
                formula = $@"y = x^2,\quad y = {a2}";
                int root = 2 * a;
                int num = 4 * root * root * root;
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"x^2 = {a2} \implies x = \pm {root}.\; \text{{Área}} = \int_{{-{root}}}^{{{root}}} ({a2} - x^2)\,dx = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "area_between_two_parabolas";
                int a = rng.Next(2, 4);
                int a2 = a * a;
                formula = $@"y = {a2} - x^2,\quad y = x^2 - {a2}";
                int num = 8 * a * a * a;
                ans = $@"\frac{{{num}}}{{3}}";
                steps = $@"{a2} - x^2 = x^2 - {a2} \implies 2x^2 = {2 * a2} \implies x = \pm {a}.\; \text{{Área}} = \int_{{-{a}}}^{{{a}}} ({2 * a2} - 2x^2)\,dx = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "area_between_cubic_line";
                formula = @"y = 4x,\quad y = x^3\quad (x \ge 0)";
                ans = "4";
                steps = @"4x = x^3 \implies x(4 - x^2) = 0 \implies x = 0, 2.\; \text{Área} = \int_0^2 (4x - x^3)\,dx = \left[ 2x^2 - \frac{x^4}{4} \right]_0^2 = 8 - 4 = 4.";
            }
            else
            {
                templateId = "area_between_radical_line";
                formula = @"y = \sqrt{x},\quad y = x";
                ans = @"\frac{1}{6}";
                steps = @"\sqrt{x} = x \implies x = 0, 1.\; \text{Área} = \int_0^1 (\sqrt{x} - x)\,dx = \left[ \frac{2}{3}x^{3/2} - \frac{x^2}{2} \right]_0^1 = \frac{2}{3} - \frac{1}{2} = \frac{1}{6}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "1/3", "4/3", "9/2", rng);
            return q;
        }

        public static Question GenerateVolumeDisksProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "vol_disks_radical";
                int b = rng.Next(2, 6);
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = b * b;
                ans = num % 2 == 0 ? $"{num / 2}\\pi" : $@"\frac{{{num}\pi}}{{2}}";
                steps = $@"V = \pi \int_0^{{{b}}} (\sqrt{{x}})^2\,dx = \pi \left[ \frac{{x^2}}{{2}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "vol_disks_parabola";
                int b = rng.Next(1, 4);
                formula = $@"y = x^2,\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = (int)Math.Pow(b, 5);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = \pi \int_0^{{{b}}} (x^2)^2\,dx = \pi \left[ \frac{{x^5}}{{5}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "vol_disks_line";
                int m = rng.Next(2, 4);
                int b = rng.Next(2, 4);
                formula = $@"y = {m}x,\; y = 0,\; x = {b};\quad (\text{{eje }} x)";
                int num = m * m * b * b * b;
                ans = num % 3 == 0 ? $"{num / 3}\\pi" : $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = \pi \int_0^{{{b}}} ({m}x)^2\,dx = \pi \left[ \frac{{{m * m}x^3}}{{3}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "vol_disks_semicircle";
                int r = rng.Next(2, 4);
                int r2 = r * r;
                formula = $@"y = \sqrt{{{r2} - x^2}},\; y = 0;\quad (\text{{eje }} x)";
                int num = 4 * r * r * r;
                ans = num % 3 == 0 ? $"{num / 3}\\pi" : $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = \pi \int_{{-{r}}}^{{{r}}} ({r2} - x^2)\,dx = \pi \left[ {r2}x - \frac{{x^3}}{{3}} \right]_{{-{r}}}^{{{r}}} = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "vol_disks_washer";
                formula = @"y = x,\; y = x^2;\quad (\text{eje } x)";
                ans = @"\frac{2\pi}{15}";
                steps = @"V = \pi \int_0^1 (x^2 - x^4)\,dx = \pi \left[ \frac{x^3}{3} - \frac{x^5}{5} \right]_0^1 = \pi\left(\frac{1}{3} - \frac{1}{5}\right) = \frac{2\pi}{15}.";
            }
            else
            {
                templateId = "vol_disks_exponential";
                formula = @"y = e^x,\; y = 0,\; x \in [0, 1];\quad (\text{eje } x)";
                ans = @"\frac{\pi(e^2 - 1)}{2}";
                steps = @"V = \pi \int_0^1 (e^x)^2\,dx = \pi \int_0^1 e^{2x}\,dx = \pi \left[ \frac{e^{2x}}{2} \right]_0^1 = \frac{\pi(e^2 - 1)}{2}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "8pi", "16pi", "4pi", rng);
            return q;
        }

        public static Question GenerateVolumeShellsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "vol_shells_parabola";
                int b = rng.Next(1, 4);
                formula = $@"y = x^2,\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                int num = b * b * b * b;
                ans = num % 2 == 0 ? $"{num / 2}\\pi" : $@"\frac{{{num}\pi}}{{2}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(x^2)\,dx = 2\pi \left[ \frac{{x^4}}{{4}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "vol_shells_radical";
                int b = rng.Next(1, 4) * 4;
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                double rootB = Math.Sqrt(b);
                int num = (int)(4 * b * b * rootB);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(\sqrt{{x}})\,dx = 2\pi \left[ \frac{{2}}{{5}}x^{{5/2}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "vol_shells_parabola_arch";
                int a = rng.Next(1, 3);
                int twoA = 2 * a;
                formula = $@"y = {twoA}x - x^2,\; y = 0;\quad (\text{{eje }} y)";
                int num = 8 * a * a * a * a;
                ans = $@"\frac{{{num}\pi}}{{3}}";
                steps = $@"V = 2\pi \int_0^{{{twoA}}} x({twoA}x - x^2)\,dx = 2\pi \left[ \frac{{{twoA}x^3}}{{3}} - \frac{{x^4}}{{4}} \right]_0^{{{twoA}}} = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "vol_shells_cubic";
                int b = rng.Next(1, 3);
                formula = $@"y = x^3,\; y = 0,\; x = {b};\quad (\text{{eje }} y)";
                int num = 2 * (int)Math.Pow(b, 5);
                ans = $@"\frac{{{num}\pi}}{{5}}";
                steps = $@"V = 2\pi \int_0^{{{b}}} x(x^3)\,dx = 2\pi \left[ \frac{{x^5}}{{5}} \right]_0^{{{b}}} = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "vol_shells_linear";
                formula = @"y = 2x,\; y = 0,\; x = 3;\quad (\text{eje } y)";
                ans = @"36\pi";
                steps = @"V = 2\pi \int_0^3 x(2x)\,dx = 4\pi \left[ \frac{x^3}{3} \right]_0^3 = 4\pi(9) = 36\pi.";
            }
            else
            {
                templateId = "vol_shells_inv_parabola";
                formula = @"y = 4 - x^2,\; y = 0;\quad (x \ge 0,\; \text{eje } y)";
                ans = @"8\pi";
                steps = @"V = 2\pi \int_0^2 x(4 - x^2)\,dx = 2\pi \left[ 2x^2 - \frac{x^4}{4} \right]_0^2 = 2\pi(8 - 4) = 8\pi.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "8pi", "16pi", "2pi", rng);
            return q;
        }

        public static Question GenerateCrossSectionsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "cross_sections_circle_squares";
                int r = rng.Next(2, 4);
                int r2 = r * r;
                formula = $@"x^2 + y^2 = {r2};\quad (\text{{cuadrados }} \perp \text{{eje }} x)";
                int num = 16 * r * r * r;
                ans = num % 3 == 0 ? (num / 3).ToString() : $@"\frac{{{num}}}{{3}}";
                steps = $@"\text{{Lado}} = 2y = 2\sqrt{{{r2} - x^2}}.\; A(x) = (2y)^2 = 4({r2} - x^2).\; V = \int_{{-{r}}}^{{{r}}} 4({r2} - x^2)\,dx = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "cross_sections_parabola_squares";
                int a = rng.Next(1, 3);
                int a2 = a * a;
                formula = $@"y = {a2} - x^2,\; y = 0;\quad (\text{{cuadrados }} \perp \text{{eje }} x)";
                int num = 16 * (int)Math.Pow(a, 5);
                ans = $@"\frac{{{num}}}{{15}}";
                steps = $@"\text{{Lado}} = {a2} - x^2.\; A(x) = ({a2} - x^2)^2.\; V = \int_{{-{a}}}^{{{a}}} ({a2} - x^2)^2\,dx = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "cross_sections_radical_semicircles";
                int b = rng.Next(2, 6);
                formula = $@"y = \sqrt{{x}},\; y = 0,\; x = {b};\quad (\text{{semicírculos }} \perp \text{{eje }} x)";
                int num = b * b;
                ans = $@"\frac{{{num}\pi}}{{16}}";
                steps = $@"r = \frac{{\sqrt{{x}}}}{{2}}.\; A(x) = \frac{{\pi r^2}}{{2}} = \frac{{\pi x}}{{8}}.\; V = \int_0^{{{b}}} \frac{{\pi x}}{{8}}\,dx = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "cross_sections_parabola_triangles";
                int a = rng.Next(2, 4);
                formula = $@"y = {a * a} - x^2,\; y = 0;\quad (\text{{triángulos eq. }} \perp \text{{eje }} x)";
                int num = 16 * (int)Math.Pow(a, 5);
                ans = $@"\frac{{{num}\sqrt{{3}}}}{{15}}";
                steps = $@"\text{{Base}} = {a * a} - x^2.\; A(x) = \frac{{\sqrt{{3}}}}{{4}}({a * a} - x^2)^2.\; V = \frac{{\sqrt{{3}}}}{{4}}\int_{{-{a}}}^{{{a}}} ({a * a} - x^2)^2\,dx = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "cross_sections_iso_right_triangle_leg";
                int b = rng.Next(2, 6);
                formula = $@"y = x,\; y = 0,\; x = {b};\quad (\text{{triángulos rectángulos isósceles con cateto en la base }} \perp \text{{eje }} x)";
                int num = b * b * b;
                ans = num % 6 == 0 ? (num / 6).ToString() : $@"\frac{{{num}}}{{6}}";
                steps = $@"\text{{Cateto}} = x.\; A(x) = \frac{{1}}{{2}}x^2.\; V = \int_0^{{{b}}} \frac{{1}}{{2}}x^2\,dx = \frac{{{b}^3}}{{6}} = {ans}.";
            }
            else
            {
                templateId = "cross_sections_rectangles_height";
                formula = @"y = 4 - x^2,\; y = 0;\quad (\text{{rectángulos con altura igual a 3 veces la base }} \perp \text{{eje }} x)";
                ans = @"\frac{512}{5}";
                steps = @"\text{Base} = 4 - x^2,\; h = 3(4 - x^2) \implies A(x) = 3(4 - x^2)^2.\; V = 3\int_{-2}^2 (4 - x^2)^2\,dx = 3 \cdot \frac{512}{15} = \frac{512}{5}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "32", "64/3", "128/3", rng);
            return q;
        }

        public static Question GenerateMotionRevisitedProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "motion_linear_vel";
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
                templateId = "motion_quad_vel";
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
            else if (variant == 2)
            {
                templateId = "motion_shifted_linear_vel";
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
            else if (variant == 3)
            {
                templateId = "motion_inverted_quad_vel";
                // v(t) = 6t - 3t^2 on [0, 3]. Zero at t = 2.
                // desp = [3t^2 - t^3]_0^3 = 27 - 27 = 0.
                // dist = [3t^2 - t^3]_0^2 + |[3t^2 - t^3]_2^3| = (12 - 8) + |0 - 4| = 4 + 4 = 8.
                formula = @"v(t) = 6t - 3t^2;\; [0, 3]";
                ans = @"\text{Despl.} = 0;\; \text{Dist.} = 8";
                steps = @"v(t) = 3t(2 - t) = 0 \implies t = 2 \in (0, 3).\; \text{Despl.} = \int_0^3 (6t - 3t^2)\,dt = [3t^2 - t^3]_0^3 = 0.\; \text{Dist.} = \int_0^2 (6t - 3t^2)\,dt + \int_2^3 -(6t - 3t^2)\,dt = 4 + 4 = 8.";
            }
            else if (variant == 4)
            {
                templateId = "motion_trig_vel";
                int a = rng.Next(2, 5) * 2;
                formula = $@"v(t) = {a}\cos(t);\; [0, \pi]";
                // On [0, pi/2], cos >= 0; on [pi/2, pi], cos <= 0.
                // desp = a*sin(pi) - 0 = 0.
                // dist = a + a = 2a.
                ans = $@"\text{{Despl.}} = 0;\; \text{{Dist.}} = {2 * a}";
                steps = $@"v(t) = {a}\cos(t) = 0 \implies t = \frac{{\pi}}{{2}}.\; \text{{Despl.}} = \int_0^\pi {a}\cos(t)\,dt = [{a}\sin(t)]_0^\pi = 0.\; \text{{Dist.}} = \int_0^{{\pi/2}} {a}\cos(t)\,dt + \int_{{\pi/2}}^\pi -{a}\cos(t)\,dt = {a} + {a} = {2 * a}.";
            }
            else
            {
                templateId = "motion_cubic_vel";
                // v(t) = 4t^3 - 16t on [0, 3]. Zero at t = 2.
                formula = @"v(t) = 4t^3 - 16t;\; [0, 3]";
                // desp = [t^4 - 8t^2]_0^3 = 81 - 72 = 9.
                // dist = |[t^4 - 8t^2]_0^2| + [t^4 - 8t^2]_2^3 = |16 - 32| + (9 - (-16)) = 16 + 25 = 41.
                ans = @"\text{Despl.} = 9;\; \text{Dist.} = 41";
                steps = @"v(t) = 4t(t^2 - 4) = 0 \implies t = 2 \in (0, 3).\; \text{Despl.} = \int_0^3 (4t^3 - 16t)\,dt = [t^4 - 8t^2]_0^3 = 9.\; \text{Dist.} = \int_0^2 -(4t^3 - 16t)\,dt + \int_2^3 (4t^3 - 16t)\,dt = 16 + 25 = 41.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_point" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_diagonal_line" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_horiz_line" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_vert_line" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"m = {k}", "m = 1", @"\text{Indefinido}", rng);
                return q;
            });

            // 5. Autonomous equilibrium slopes
            pool.Add(() =>
            {
                int k = rng.Next(2, 6);
                string formula = $@"\frac{{dy}}{{dx}} = y({k} - y);\quad \text{{¿Para qué valores de }} y \text{{ las soluciones tienen pendientes horizontales?}}";
                string ans = $@"y = 0 \text{{ y }} y = {k}";
                string steps = $@"\frac{{dy}}{{dx}} = 0 \iff y({k} - y) = 0 \implies y = 0 \text{{ o }} y = {k}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_autonomous_equilibrium" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"y = {k}", "y = 0", $"y = -{k}", rng);
                return q;
            });

            // 6. Hyperbolic family
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                string aStr = a == 1 ? "" : a.ToString();
                string formula = $@"\frac{{dy}}{{dx}} = \frac{{{aStr}x}}{{y}};\quad \text{{¿Qué familia geométrica de curvas solución describe este campo?}}";
                string ans = @"\text{Familia de hipérbolas}";
                string steps = $@"y\,dy = {aStr}x\,dx \implies \frac{{y^2}}{{2}} - \frac{{{a}x^2}}{{2}} = C \implies y^2 - {a}x^2 = C_1 \text{{ (hipérbolas)}}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "slope_field_hyperbolas" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, @"\text{Familia de circunferencias}", @"\text{Familia de parábolas}", @"\text{Familia de rectas}", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateDiffEqIntroProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "diffeq_intro_exp";
                int k = rng.Next(2, 9);
                int c = rng.Next(2, 8);
                formula = $@"y = {c}e^{{{k}x}};\quad \text{{¿Es solución de }} y' - {k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {c * k}e^{{{k}x}}.\; y' - {k}y = {c * k}e^{{{k}x}} - {k}({c}e^{{{k}x}}) = 0.";
            }
            else if (variant == 1)
            {
                templateId = "diffeq_intro_sin";
                int k = rng.Next(2, 8);
                formula = $@"y = \sin({k}x);\quad \text{{¿Es solución de }} y'' + {k * k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {k}\cos({k}x),\; y'' = -{k * k}\sin({k}x).\; y'' + {k * k}y = -{k * k}\sin({k}x) + {k * k}\sin({k}x) = 0.";
            }
            else if (variant == 2)
            {
                templateId = "diffeq_intro_power";
                int n = rng.Next(2, 7);
                formula = $@"y = x^{{{n}}};\; (x > 0)\quad \text{{¿Es solución de }} xy' - {n}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = {n}x^{{{n - 1}}}.\; xy' = x({n}x^{{{n - 1}}}) = {n}x^{{{n}}} = {n}y \implies xy' - {n}y = 0.";
            }
            else if (variant == 3)
            {
                templateId = "diffeq_intro_cos";
                int k = rng.Next(2, 7);
                formula = $@"y = \cos({k}x);\quad \text{{¿Es solución de }} y'' + {k * k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = -{k}\sin({k}x),\; y'' = -{k * k}\cos({k}x).\; y'' + {k * k}y = 0.";
            }
            else if (variant == 4)
            {
                templateId = "diffeq_intro_rational";
                int c = rng.Next(1, 6);
                formula = $@"y = \frac{{1}}{{x + {c}}};\; (x \neq -{c})\quad \text{{¿Es solución de }} y' + y^2 = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = -\frac{{1}}{{(x + {c})^2}} = -y^2 \implies y' + y^2 = 0.";
            }
            else
            {
                templateId = "diffeq_intro_damped_exp";
                int k = rng.Next(2, 6);
                formula = $@"y = x e^{{{k}x}};\quad \text{{¿Es solución de }} y'' - {2 * k}y' + {k * k}y = 0?";
                ans = @"\text{Sí, es una solución válida}";
                steps = $@"y' = e^{{{k}x}} + {k}x e^{{{k}x}}.\; y'' = {2 * k}e^{{{k}x}} + {k * k}x e^{{{k}x}}.\; y'' - {2 * k}y' + {k * k}y = 0.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_linear_xy" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_ratio_xy" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_quad_y" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_quad_x_y" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $@"y = Ce^{{{k}x^2}}", $@"y = {k}x^3 + C", @"y = \ln(x^3) + C", rng);
                return q;
            });

            // 5. dy/dx = y * cos(x)
            pool.Add(() =>
            {
                string formula = @"\frac{dy}{dx} = y \cos(x)";
                string ans = @"y = Ce^{\sin(x)}";
                string steps = @"\frac{1}{y}\,dy = \cos(x)\,dx \implies \ln|y| = \sin(x) + C_1 \implies y = Ce^{\sin(x)}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_trig_cos" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, @"y = Ce^{\cos(x)}", @"y = C\sin(x)", @"y = \sin(x) + C", rng);
                return q;
            });

            // 6. dy/dx = e^{x - y}
            pool.Add(() =>
            {
                string formula = @"\frac{dy}{dx} = e^{x - y}";
                string ans = @"e^y = e^x + C";
                string steps = @"e^y\,dy = e^x\,dx \implies e^y = e^x + C \implies y = \ln(e^x + C).";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "separable_exp_xy" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, @"e^y = e^{-x} + C", @"y = e^x + C", @"y = x + C", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateGrowthDecayProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "growth_doubling";
                int p0 = rng.Next(2, 8) * 100;
                int doubleTime = rng.Next(2, 5);
                int multiplier = rng.Next(2, 4);
                int totalHours = doubleTime * multiplier;
                long finalPop = p0 * (long)Math.Pow(2, multiplier);

                formula = $"P(0) = {p0}.\\; \\text{{Población se duplica cada }} {doubleTime} \\text{{ h. Halle }} P({totalHours}).";
                ans = $"{finalPop}";
                steps = $@"P(t) = {p0} \cdot 2^{{t / {doubleTime}}}.\; P({totalHours}) = {p0} \cdot 2^{{{totalHours} / {doubleTime}}} = {p0} \cdot 2^{{{multiplier}}} = {finalPop}.";
            }
            else if (variant == 1)
            {
                templateId = "decay_halflife";
                int m0 = rng.Next(2, 9) * 16;
                int halfLife = rng.Next(3, 7);
                int periods = rng.Next(2, 5);
                int totalTime = halfLife * periods;
                int finalMass = m0 / (int)Math.Pow(2, periods);

                formula = $"m(0) = {m0}\\,\\text{{g}}.\\; \\text{{Vida media }} T_{{1/2}} = {halfLife} \\text{{ días. Halle masa a los }} {totalTime} \\text{{ días}}.";
                ans = $"{finalMass}\\,\\text{{g}}";
                steps = $@"m(t) = {m0} \\left(\\frac{{1}}{{2}}\\right)^{{t / {halfLife}}}.\\; m({totalTime}) = {m0} \\left(\\frac{{1}}{{2}}\\right)^{{{periods}}} = {finalMass}\\,\\text{{g}}.";
            }
            else if (variant == 2)
            {
                templateId = "growth_tripling";
                int p0 = rng.Next(1, 6) * 50;
                int tripleTime = rng.Next(2, 5);
                int multiplier = rng.Next(2, 4);
                int totalHours = tripleTime * multiplier;
                long finalPop = p0 * (long)Math.Pow(3, multiplier);

                formula = $"P(0) = {p0}.\\; \\text{{Cultivo se triplica cada }} {tripleTime} \\text{{ h. Halle }} P({totalHours}).";
                ans = $"{finalPop}";
                steps = $@"P(t) = {p0} \cdot 3^{{t / {tripleTime}}}.\; P({totalHours}) = {p0} \cdot 3^{{{totalHours} / {tripleTime}}} = {p0} \cdot 3^{{{multiplier}}} = {finalPop}.";
            }
            else if (variant == 3)
            {
                templateId = "growth_decay_rate_constant";
                int k = rng.Next(2, 6);
                int y0 = rng.Next(3, 10) * 10;
                formula = $@"\\frac{{dy}}{{dt}} = -{k}y,\\quad y(0) = {y0}.\\; \\text{{Halle la expresión para }} y(t).";
                ans = $@"y(t) = {y0}e^{{-{k}t}}";
                steps = $@"\\frac{{1}}{{y}}\\,dy = -{k}\\,dt \\implies \\ln|y| = -{k}t + C \\implies y(t) = y(0)e^{{-{k}t}} = {y0}e^{{-{k}t}}.";
            }
            else if (variant == 4)
            {
                templateId = "growth_continuous_compounding";
                int principal = rng.Next(1, 6) * 1000;
                int rPercent = rng.Next(4, 8);
                int tYears = rng.Next(2, 6);
                formula = $@"\text{{Capital }} P = \${principal} \text{{ a tasa continua del }} {rPercent}\% \text{{ anual. Halle el valor acumulado en }} {tYears} \text{{ años}}.";
                double r = rPercent / 100.0;
                double exponent = r * tYears;
                double accumulated = Math.Round(principal * Math.Exp(exponent), 2);
                ans = $@"\${accumulated}";
                steps = $@"A(t) = P e^{{rt}} = {principal} e^{{{r} \cdot {tYears}}} = {principal} e^{{{exponent:0.00}}} \approx \${accumulated}.";
            }
            else
            {
                templateId = "growth_newton_cooling";
                // T_s = 20, T_0 = 100. T(10) = 60. Then T(20) = 40.
                int ts = 20;
                int t0 = 80 + ts;
                formula = $@"\text{{Un café a }} {t0}^\circ\text{{C se enfría en una sala a }} {ts}^\circ\text{{C}}. \text{{A los 10 min está a }} 60^\circ\text{{C}}. \text{{Halle }} T(20).";
                ans = @"40^\circ\text{C}";
                steps = @"T(t) = 20 + 80e^{-kt}.\; T(10) = 20 + 80e^{-10k} = 60 \implies e^{-10k} = \frac{1}{2}.\; T(20) = 20 + 80(e^{-10k})^2 = 20 + 80\left(\frac{1}{4}\right) = 40^\circ\text{C}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "100", "200", "400", rng);
            return q;
        }

        public static Question GenerateDerivTablesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int a = rng.Next(1, 4);
            int fa = rng.Next(2, 6);
            int fpa = rng.Next(1, 5) * (rng.Next(2) == 0 ? 1 : -1);
            int ga = rng.Next(2, 5);
            int gpa = rng.Next(1, 6) * (rng.Next(2) == 0 ? 1 : -1);

            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "deriv_tables_product";
                formula = $@"f({a})={fa},\; f'({a})={fpa},\; g({a})={ga},\; g'({a})={gpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = f(x)g(x)";
                int ansVal = fpa * ga + fa * gpa;
                ans = ansVal.ToString();
                steps = $@"h'({a}) = f'({a})g({a}) + f({a})g'({a}) = ({fpa})({ga}) + ({fa})({gpa}) = {ansVal}.";
            }
            else if (variant == 1)
            {
                templateId = "deriv_tables_quotient";
                formula = $@"f({a})={fa},\; f'({a})={fpa},\; g({a})={ga},\; g'({a})={gpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = \frac{{f(x)}}{{g(x)}}";
                int num = fpa * ga - fa * gpa;
                int den = ga * ga;
                int g = Gcd(Math.Abs(num), Math.Abs(den));
                num /= g; den /= g;
                ans = den == 1 ? num.ToString() : $@"\frac{{{num}}}{{{den}}}";
                steps = $@"h'({a}) = \frac{{f'({a})g({a}) - f({a})g'({a})}}{{[g({a})]^2}} = \frac{{({fpa})({ga}) - ({fa})({gpa})}}{{{ga * ga}}} = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "deriv_tables_linear_combo";
                int c1 = rng.Next(2, 5);
                int c2 = rng.Next(2, 5);
                formula = $@"f({a})={fa},\; f'({a})={fpa},\; g({a})={ga},\; g'({a})={gpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = {c1}f(x) - {c2}g(x)";
                int ansVal = c1 * fpa - c2 * gpa;
                ans = ansVal.ToString();
                steps = $@"h'({a}) = {c1}f'({a}) - {c2}g'({a}) = {c1}({fpa}) - {c2}({gpa}) = {ansVal}.";
            }
            else if (variant == 3)
            {
                templateId = "deriv_tables_power";
                formula = $@"f({a})={fa},\; f'({a})={fpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = [f(x)]^2";
                int ansVal = 2 * fa * fpa;
                ans = ansVal.ToString();
                steps = $@"h'({a}) = 2f({a})f'({a}) = 2({fa})({fpa}) = {ansVal}.";
            }
            else if (variant == 4)
            {
                templateId = "deriv_tables_reciprocal";
                formula = $@"g({a})={ga},\; g'({a})={gpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = \frac{{1}}{{g(x)}}";
                int num = -gpa;
                int den = ga * ga;
                int g = Gcd(Math.Abs(num), Math.Abs(den));
                num /= g; den /= g;
                ans = den == 1 ? num.ToString() : $@"\frac{{{num}}}{{{den}}}";
                steps = $@"h'({a}) = -\frac{{g'({a})}}{{[g({a})]^2}} = -\frac{{{gpa}}}{{{ga * ga}}} = {ans}.";
            }
            else
            {
                templateId = "deriv_tables_poly_mult";
                formula = $@"f({a})={fa},\; f'({a})={fpa}.\; \text{{Halle }} h'({a}) \text{{ para }} h(x) = x^2 f(x)";
                int ansVal = 2 * a * fa + a * a * fpa;
                ans = ansVal.ToString();
                steps = $@"h'(x) = 2x f(x) + x^2 f'(x) \implies h'({a}) = 2({a})({fa}) + ({a})^2({fpa}) = {ansVal}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "10", "15", "-5", rng);
            return q;
        }

        public static Question GenerateLogarithmicDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "log_deriv_power_x";
                int k = rng.Next(1, 6);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = x^{{{kStr}x}}";
                ans = $@"y' = {kStr}x^{{{kStr}x}}(\ln(x) + 1)";
                steps = $@"\ln(y) = {kStr}x\ln(x) \implies \frac{{y'}}{{y}} = {kStr}(\ln(x) + 1) \implies y' = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "log_deriv_power_sinx";
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = x^{{\sin({kStr}x)}}";
                ans = $@"y' = x^{{\sin({kStr}x)}}\left({kStr}\cos({kStr}x)\ln(x) + \frac{{\sin({kStr}x)}}{{x}}\right)";
                steps = $@"\ln(y) = \sin({kStr}x)\ln(x) \implies \frac{{y'}}{{y}} = {kStr}\cos({kStr}x)\ln(x) + \frac{{\sin({kStr}x)}}{{x}} \implies y' = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "log_deriv_shifted_x";
                int a = rng.Next(1, 6);
                formula = $@"y = (x + {a})^x";
                ans = $@"y' = (x + {a})^x\left(\ln(x + {a}) + \frac{{x}}{{x + {a}}}\right)";
                steps = $@"\ln(y) = x\ln(x + {a}) \implies \frac{{y'}}{{y}} = \ln(x + {a}) + \frac{{x}}{{x + {a}}} \implies y' = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "log_deriv_sinx_x";
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                formula = $@"y = (\sin(x))^{{{kStr}x}}";
                ans = $@"y' = {kStr}(\sin(x))^{{{kStr}x}}(\ln(\sin(x)) + x\cot(x))";
                steps = $@"\ln(y) = {kStr}x\ln(\sin(x)) \implies \frac{{y'}}{{y}} = {kStr}\ln(\sin(x)) + {kStr}x\cot(x) \implies y' = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "log_deriv_lnx_x";
                int k = rng.Next(1, 5);
                string kStr = k == 1 ? "" : $"{k}";
                int k2 = 2 * k;
                formula = $@"y = x^{{{kStr}\ln(x)}}";
                ans = $@"y' = {k2}x^{{{kStr}\ln(x)-1}}\ln(x)";
                steps = $@"\ln(y) = {kStr}(\ln(x))^2 \implies \frac{{y'}}{{y}} = \frac{{{k2}\ln(x)}}{{x}} \implies y' = {ans}.";
            }
            else
            {
                templateId = "log_deriv_sqrt_x";
                formula = @"y = x^{\sqrt{x}}";
                ans = @"y' = x^{\sqrt{x}}\left(\frac{\ln(x) + 2}{2\sqrt{x}}\right)";
                steps = @"\ln(y) = \sqrt{x}\ln(x) \implies \frac{y'}{y} = \frac{\ln(x)}{2\sqrt{x}} + \frac{1}{\sqrt{x}} \implies y' = " + ans + ".";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"y' = x\cdot " + formula.Substring(4), @"y' = " + formula.Substring(4) + @"\ln(x)", @"y' = " + formula.Substring(4), rng);
            return q;
        }

        public static Question GenerateInverseFuncDerivProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "inv_func_cubic";
                int a = rng.Next(1, 4);
                int b = rng.Next(1, 4);
                int x0 = rng.Next(1, 3);
                int y0 = x0 * x0 * x0 + a * x0 + b;
                int deriv = 3 * x0 * x0 + a;

                formula = $@"f(x) = x^3 + {a}x + {b};\quad \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f({x0}) = {y0}";
                ans = $@"(f^{{-1}})'({y0}) = \frac{{1}}{{{deriv}}}";
                steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = 3x^2 + {a} \implies f'({x0}) = 3({x0})^2 + {a} = {deriv}.\; \text{{Resp: }} \frac{{1}}{{{deriv}}}.";
            }
            else if (variant == 1)
            {
                templateId = "inv_func_quintic";
                int a = rng.Next(1, 4);
                int x0 = 1;
                int y0 = 1 + a + 1;
                int deriv = 5 + a;
                formula = $@"f(x) = x^5 + {a}x + 1;\quad \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f({x0}) = {y0}";
                ans = $@"(f^{{-1}})'({y0}) = \frac{{1}}{{{deriv}}}";
                steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = 5x^4 + {a} \implies f'({x0}) = 5(1)^4 + {a} = {deriv}.\; \text{{Resp: }} \frac{{1}}{{{deriv}}}.";
            }
            else if (variant == 2)
            {
                templateId = "inv_func_exp";
                int a = rng.Next(2, 5);
                int y0 = 1;
                int deriv = 1 + a;
                formula = $@"f(x) = e^x + {a}x;\quad \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f(0) = {y0}";
                ans = $@"(f^{{-1}})'({y0}) = \frac{{1}}{{{deriv}}}";
                steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = e^x + {a} \implies f'(0) = 1 + {a} = {deriv}.\; \text{{Resp: }} \frac{{1}}{{{deriv}}}.";
            }
            else if (variant == 3)
            {
                templateId = "inv_func_radical";
                int a = rng.Next(1, 4);
                int x0 = 4;
                int y0 = 2 * x0 + (int)Math.Sqrt(x0) + a;
                formula = $@"f(x) = 2x + \sqrt{{x}} + {a};\quad \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f({x0}) = {y0}";
                ans = @"(f^{-1})'(" + y0 + @") = \frac{4}{9}";
                steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = 2 + \frac{{1}}{{2\sqrt{{x}}}} \implies f'({x0}) = 2 + \frac{{1}}{{4}} = \frac{{9}}{{4}}.\; \text{{Resp: }} \frac{{4}}{{9}}.";
            }
            else if (variant == 4)
            {
                templateId = "inv_func_rational";
                formula = @"f(x) = \frac{2x + 1}{x + 1};\; (x > 0)\quad \text{halle } (f^{-1})'\left(\frac{3}{2}\right) \text{ sabiendo que } f(1) = \frac{3}{2}";
                ans = @"(f^{-1})'\left(\frac{3}{2}\right) = 4";
                steps = @"f'(x) = \frac{2(x+1) - (2x+1)}{(x+1)^2} = \frac{1}{(x+1)^2} \implies f'(1) = \frac{1}{4}.\; (f^{-1})'\left(\frac{3}{2}\right) = \frac{1}{f'(1)} = 4.";
            }
            else
            {
                templateId = "inv_func_quadratic";
                int a = rng.Next(2, 5);
                int x0 = 2;
                int y0 = x0 * x0 + a * x0;
                int deriv = 2 * x0 + a;
                formula = $@"f(x) = x^2 + {a}x;\; (x > 0)\quad \text{{halle }} (f^{{-1}})'({y0}) \text{{ sabiendo que }} f({x0}) = {y0}";
                ans = $@"(f^{{-1}})'({y0}) = \frac{{1}}{{{deriv}}}";
                steps = $@"(f^{{-1}})'(b) = \frac{{1}}{{f'(f^{{-1}}(b))}}.\; f'(x) = 2x + {a} \implies f'({x0}) = {deriv}.\; \text{{Resp: }} \frac{{1}}{{{deriv}}}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"\frac{1}{2}", @"1", @"\frac{1}{5}", rng);
            return q;
        }

        public static Question GenerateGraphComparisonProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            int xVal = rng.Next(1, 6);
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "graph_fprime_pos_to_neg";
                formula = $@"\text{{Si }} f'(x) \text{{ cruza el eje }} x \text{{ de positivo a negativo en }} x = {xVal}, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un máximo relativo en }} x = {xVal}";
                steps = $@"f' \text{{ cambia de }} + \text{{ a }} -, \text{{ por lo que }} f \text{{ cambia de creciente a decreciente }} \implies \text{{Máximo relativo}}.";
            }
            else if (variant == 1)
            {
                templateId = "graph_fprime_neg_to_pos";
                formula = $@"\text{{Si }} f'(x) \text{{ cruza el eje }} x \text{{ de negativo a positivo en }} x = {xVal}, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un mínimo relativo en }} x = {xVal}";
                steps = $@"f' \text{{ cambia de }} - \text{{ a }} +, \text{{ por lo que }} f \text{{ cambia de decreciente a creciente }} \implies \text{{Mínimo relativo}}.";
            }
            else if (variant == 2)
            {
                templateId = "graph_fdouble_sign_change";
                formula = $@"\text{{Si }} f''(x) \text{{ cambia de signo en }} x = {xVal} \text{{ y }} f'({xVal}) \text{{ existe, ¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un punto de inflexión en }} x = {xVal}";
                steps = @"\text{Un cambio de signo en } f'' \text{ indica cambio de concavidad } \implies \text{Punto de inflexión}.";
            }
            else if (variant == 3)
            {
                templateId = "graph_second_deriv_test";
                formula = $@"\text{{Si }} f'({xVal}) = 0 \text{{ y }} f''({xVal}) < 0, \text{{¿qué ocurre en }} f(x)?";
                ans = $@"f(x) \text{{ tiene un máximo relativo local en }} x = {xVal}";
                steps = @"\text{Por el criterio de la segunda derivada: } f' = 0 \text{ y } f'' < 0 \implies \text{Cóncava hacia abajo } \implies \text{Máximo local}.";
            }
            else if (variant == 4)
            {
                templateId = "graph_fprime_positive_interval";
                formula = $@"\text{{Si }} f'(x) > 0 \text{{ para todo }} x \in ({xVal}, {xVal + 3}), \text{{¿cómo se comporta }} f(x)?";
                ans = $@"\text{{La función }} f(x) \text{{ es estrictamente creciente en }} ({xVal}, {xVal + 3})";
                steps = $@"f'(x) > 0 \implies \text{{pendiente positiva}} \implies f(x) \text{{ es estrictamente creciente en }} ({xVal}, {xVal + 3}).";
            }
            else
            {
                templateId = "graph_fdouble_positive_interval";
                formula = $@"\text{{Si }} f''(x) > 0 \text{{ para todo }} x \in ({xVal}, {xVal + 3}), \text{{¿cuál es la concavidad de }} f(x)?";
                ans = $@"\text{{La gráfica de }} f(x) \text{{ es cóncava hacia arriba en }} ({xVal}, {xVal + 3})";
                steps = $@"f''(x) > 0 \implies \text{{la tasa de cambio de la pendiente es positiva}} \implies f(x) \text{{ es cóncava hacia arriba}}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"f(x) \text{ tiene un mínimo relativo}", @"f(x) \text{ no tiene extremos}", @"\text{Indefinido}", rng);
            return q;
        }

        public static Question GenerateDifferentialsProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "differentials_sqrt";
                int[] bases = { 4, 9, 16, 25, 36, 49, 64, 81, 100 };
                int baseNum = bases[rng.Next(bases.Length)];
                double root = Math.Sqrt(baseNum);
                double[] dxChoices = { -0.3, -0.2, -0.1, 0.1, 0.2, 0.3, 0.4, 0.5 };
                double dx = dxChoices[rng.Next(dxChoices.Length)];
                double xVal = baseNum + dx;

                double deriv = 1.0 / (2.0 * root);
                double dy = deriv * dx;
                double approx = Math.Round(root + dy, 4);

                formula = $@"f(x) = \sqrt{{x}};\; \text{{Aproxime }} \sqrt{{{xVal:0.1}}} \text{{ mediante diferenciales}}";
                ans = $"{approx}";
                steps = $@"x_0 = {baseNum},\\; dx = {dx:0.1}.\\; f'({baseNum}) = \frac{{1}}{{2\sqrt{{{baseNum}}}}} = {deriv:0.000}.\\; dy = ({deriv:0.000})({dx:0.1}) = {dy:0.000}.\\; \text{{Aprox: }} {root} + {dy:0.000} = {approx}.";
            }
            else if (variant == 1)
            {
                templateId = "differentials_cbrt";
                int[] bases = { 8, 27, 64 };
                int baseNum = bases[rng.Next(bases.Length)];
                double root = Math.Round(Math.Pow(baseNum, 1.0 / 3.0));
                double dx = 0.2;
                double xVal = baseNum + dx;
                double deriv = 1.0 / (3.0 * root * root);
                double dy = deriv * dx;
                double approx = Math.Round(root + dy, 4);

                formula = $@"f(x) = \sqrt[3]{{x}};\; \text{{Aproxime }} \sqrt[3]{{{xVal:0.1}}} \text{{ mediante diferenciales}}";
                ans = $"{approx}";
                steps = $@"x_0 = {baseNum},\; dx = {dx:0.1}.\; f'(x_0) = \frac{{1}}{{3({root})^2}} = {deriv:0.000}.\; dy = ({deriv:0.000})({dx:0.1}) = {dy:0.000}.\; \text{{Aprox: }} {root} + {dy:0.000} = {approx}.";
            }
            else if (variant == 2)
            {
                templateId = "differentials_power3";
                int x0 = rng.Next(2, 5);
                double dx = 0.02;
                double xVal = x0 + dx;
                double fx0 = x0 * x0 * x0;
                double deriv = 3.0 * x0 * x0;
                double dy = deriv * dx;
                double approx = Math.Round(fx0 + dy, 3);

                formula = $@"f(x) = x^3;\; \text{{Aproxime }} ({xVal:0.00})^3 \text{{ mediante diferenciales}}";
                ans = $"{approx}";
                steps = $@"x_0 = {x0},\; dx = {dx:0.00}.\; f'({x0}) = 3({x0})^2 = {deriv}.\; dy = ({deriv})({dx:0.00}) = {dy:0.000}.\; \text{{Aprox: }} {fx0} + {dy:0.000} = {approx}.";
            }
            else if (variant == 3)
            {
                templateId = "differentials_power4";
                int x0 = 2;
                double dx = 0.01;
                double xVal = x0 + dx;
                double fx0 = 16;
                double deriv = 4.0 * Math.Pow(x0, 3); // 32
                double dy = deriv * dx; // 0.32
                double approx = Math.Round(fx0 + dy, 2);

                formula = $@"f(x) = x^4;\; \text{{Aproxime }} ({xVal:0.00})^4 \text{{ mediante diferenciales}}";
                ans = $"{approx}";
                steps = $@"x_0 = {x0},\; dx = {dx:0.00}.\; f'({x0}) = 4({x0})^3 = 32.\; dy = 32(0.01) = 0.32.\; \text{{Aprox: }} 16 + 0.32 = {approx}.";
            }
            else if (variant == 4)
            {
                templateId = "differentials_reciprocal";
                int x0 = 4;
                double dx = 0.04;
                double xVal = x0 + dx;
                double fx0 = 0.25;
                double deriv = -1.0 / 16.0; // -0.0625
                double dy = deriv * dx; // -0.0025
                double approx = Math.Round(fx0 + dy, 4);

                formula = $@"f(x) = \frac{{1}}{{x}};\; \text{{Aproxime }} \frac{{1}}{{{xVal:0.00}}} \text{{ mediante diferenciales}}";
                ans = $"{approx}";
                steps = $@"x_0 = 4,\; dx = 0.04.\; f'(4) = -\frac{{1}}{{16}} = -0.0625.\; dy = (-0.0625)(0.04) = -0.0025.\; \text{{Aprox: }} 0.25 - 0.0025 = {approx}.";
            }
            else
            {
                templateId = "differentials_sin";
                double dx = 0.05;
                formula = $@"f(x) = \sin(x);\; \text{{Aproxime }} \sin({dx:0.00}) \text{{ mediante diferenciales}}";
                ans = $"{dx:0.00}";
                steps = $@"x_0 = 0,\; dx = {dx:0.00}.\; f(0) = 0,\; f'(0) = \cos(0) = 1.\; dy = (1)({dx:0.00}) = {dx:0.00}.\; \text{{Aprox: }} 0 + {dx:0.00} = {dx:0.00}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "0.5", "1.0", "0.25", rng);
            return q;
        }

        public static Question GenerateNewtonMethodProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "newton_sqrt";
                int[] nonSquares = { 2, 3, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20 };
                int k = nonSquares[rng.Next(nonSquares.Length)];
                int x0 = (int)Math.Round(Math.Sqrt(k));
                if (x0 * x0 == k) x0++;

                double fx0 = x0 * x0 - k;
                double fpx0 = 2.0 * x0;
                double x1 = Math.Round(x0 - fx0 / fpx0, 3);

                formula = $@"f(x) = x^2 - {k};\; x_0 = {x0}.\; \text{{Halle }} x_1 \text{{ mediante el Método de Newton}}";
                ans = $"x_1 = {x1}";
                steps = $@"x_1 = x_0 - \frac{{f(x_0)}}{{f'(x_0)}} = {x0} - \frac{{{fx0}}}{{{fpx0}}} = {x1}.";
            }
            else if (variant == 1)
            {
                templateId = "newton_cbrt";
                int k = 9;
                int x0 = 2;
                double fx0 = 8 - k; // -1
                double fpx0 = 3.0 * x0 * x0; // 12
                double x1 = Math.Round(x0 - fx0 / fpx0, 3); // 2.083

                formula = $@"f(x) = x^3 - {k};\; x_0 = {x0}.\; \text{{Halle }} x_1 \text{{ mediante el Método de Newton}}";
                ans = $"x_1 = {x1}";
                steps = $@"f(2) = 8 - 9 = -1,\; f'(x) = 3x^2 \implies f'(2) = 12.\; x_1 = 2 - \frac{{-1}}{{12}} \approx {x1}.";
            }
            else if (variant == 2)
            {
                templateId = "newton_cubic_shifted";
                formula = @"f(x) = x^3 - x - 1;\; x_0 = 1.\; \text{Halle } x_1 \text{ mediante el Método de Newton}";
                ans = "x_1 = 1.5";
                steps = @"f(1) = 1 - 1 - 1 = -1,\; f'(x) = 3x^2 - 1 \implies f'(1) = 2.\; x_1 = 1 - \frac{-1}{2} = 1.5.";
            }
            else if (variant == 3)
            {
                templateId = "newton_poly_quadratic";
                formula = @"f(x) = x^2 + 2x - 4;\; x_0 = 1.\; \text{Halle } x_1 \text{ mediante el Método de Newton}";
                ans = "x_1 = 1.25";
                steps = @"f(1) = 1 + 2 - 4 = -1,\; f'(x) = 2x + 2 \implies f'(1) = 4.\; x_1 = 1 - \frac{-1}{4} = 1.25.";
            }
            else if (variant == 4)
            {
                templateId = "newton_transcendental_cos";
                formula = @"f(x) = \cos(x) - x;\; x_0 = 0.\; \text{Halle } x_1 \text{ mediante el Método de Newton}";
                ans = "x_1 = 1";
                steps = @"f(0) = \cos(0) - 0 = 1,\; f'(x) = -\sin(x) - 1 \implies f'(0) = -1.\; x_1 = 0 - \frac{1}{-1} = 1.";
            }
            else
            {
                templateId = "newton_cubic_linear";
                formula = @"f(x) = x^3 + 3x - 5;\; x_0 = 1.\; \text{Halle } x_1 \text{ mediante el Método de Newton}";
                ans = "x_1 = 1.167";
                steps = @"f(1) = 1 + 3 - 5 = -1,\; f'(x) = 3x^2 + 3 \implies f'(1) = 6.\; x_1 = 1 - \frac{-1}{6} \approx 1.167.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, "x_1 = 2", "x_1 = 0.5", "x_1 = 1.75", rng);
            return q;
        }

        public static Question GenerateIndefSubsLogExpProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            const int totalVariants = 6;
            int variant = Math.Abs(index) % totalVariants;
            string formula, ans, steps;
            string templateId;

            if (variant == 0)
            {
                templateId = "subs_logexp_e_quad";
                int k = rng.Next(1, 5);
                formula = $@"int x e^{{{k}x^2}}\,dx";
                int den = 2 * k;
                ans = den == 1 ? $@"e^{{x^2}} + C" : $@"\frac{{1}}{{{den}}}e^{{{k}x^2}} + C";
                steps = $@"u = {k}x^2,\; du = {2 * k}x\,dx \implies \frac{{1}}{{{2 * k}}}\int e^u\,du = {ans}.";
            }
            else if (variant == 1)
            {
                templateId = "subs_logexp_rational";
                int a = rng.Next(1, 5);
                formula = $@"int \frac{{x}}{{x^2 + {a}}}\,dx";
                ans = $@"\frac{{1}}{{2}}\ln|x^2 + {a}| + C";
                steps = $@"u = x^2 + {a},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int \frac{{1}}{{u}}\,du = \frac{{1}}{{2}}\ln|u| + C = {ans}.";
            }
            else if (variant == 2)
            {
                templateId = "subs_logexp_ln_power";
                formula = @"int \frac{\ln(x)}{x}\,dx";
                ans = @"\frac{(\ln(x))^2}{2} + C";
                steps = @"u = \ln(x),\; du = \frac{1}{x}\,dx \implies \int u\,du = \frac{u^2}{2} + C = \frac{(\ln(x))^2}{2} + C.";
            }
            else if (variant == 3)
            {
                templateId = "subs_logexp_exp_frac";
                int a = rng.Next(1, 6);
                formula = $@"int \frac{{e^x}}{{e^x + {a}}}\,dx";
                ans = $@"\ln(e^x + {a}) + C";
                steps = $@"u = e^x + {a},\; du = e^x\,dx \implies \int \frac{{1}}{{u}}\,du = \ln|u| + C = {ans}.";
            }
            else if (variant == 4)
            {
                templateId = "subs_logexp_recip_ln";
                formula = @"int \frac{1}{x\ln(x)}\,dx";
                ans = @"\ln|\ln(x)| + C";
                steps = @"u = \ln(x),\; du = \frac{1}{x}\,dx \implies \int \frac{1}{u}\,du = \ln|u| + C = \ln|\ln(x)| + C.";
            }
            else
            {
                templateId = "subs_logexp_e_cubic";
                int k = rng.Next(1, 4);
                formula = $@"int x^2 e^{{{k}x^3}}\,dx";
                int den = 3 * k;
                ans = den == 1 ? $@"e^{{x^3}} + C" : $@"\frac{{1}}{{{den}}}e^{{{k}x^3}} + C";
                steps = $@"u = {k}x^3,\; du = {3 * k}x^2\,dx \implies \frac{{1}}{{{3 * k}}}\int e^u\,du = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
            if (type == QuestionType.MultipleChoice)
                q.Choices = DistractorHelper.BuildChoices(ans, @"e^{x^2} + C", @"\ln|x| + C", @"\frac{x^2}{2} + C", rng);
            return q;
        }

        public static Question GenerateIndefSubsPowerProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            int variant = Math.Abs(index) % 6;
            string formula, ans, steps;
            string templateId;
            string substLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Sustitución",
                AppLanguage.Portuguese => "Substituição",
                _ => "Substitution"
            };

            if (variant == 0)
            {
                templateId = "subs_power_x_quad";
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
                templateId = "subs_power_x2_cube";
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
                templateId = "subs_power_radical";
                int c = rng.Next(1, 6);
                formula = $@"int x\sqrt{{x^2 + {c}}}\,dx";
                ans = $@"\frac{{1}}{{3}}(x^2 + {c})^{{3/2}} + C";
                steps = $@"{substLabel}: u = x^2 + {c},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int u^{{1/2}}\,du = \frac{{1}}{{2}}\left(\frac{{2}}{{3}}u^{{3/2}}\right) + C = {ans}.";
            }
            else if (variant == 3)
            {
                templateId = "subs_power_linear_comp";
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
                templateId = "subs_power_x_over_sqrt";
                int c = rng.Next(1, 6);
                formula = $@"int \frac{{x}}{{\sqrt{{x^2 + {c}}}}}\,dx";
                ans = $@"\sqrt{{x^2 + {c}}} + C";
                steps = $@"{substLabel}: u = x^2 + {c},\; du = 2x\,dx \implies \frac{{1}}{{2}}\int u^{{-1/2}}\,du = \frac{{1}}{{2}}(2u^{{1/2}}) + C = {ans}.";
            }
            else
            {
                templateId = "subs_power_quad_composite";
                int b = rng.Next(1, 5) * 2;
                int c = rng.Next(1, 6);
                int n = rng.Next(2, 4);
                int n1 = n + 1;
                formula = $@"int (2x + {b})(x^2 + {b}x + {c})^{{{n}}}\,dx";
                ans = $@"\frac{{1}}{{{n1}}}(x^2 + {b}x + {c})^{{{n1}}} + C";
                steps = $@"{substLabel}: u = x^2 + {b}x + {c},\; du = (2x + {b})\,dx \implies \int u^{{{n}}}\,du = \frac{{1}}{{{n1}}}u^{{{n1}}} + C = {ans}.";
            }

            var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = templateId };
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

            // 1. Easy pool (6 archetypes)
            if (comp == 0)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = \\arctan({a}x)";
                    string ans = $"y' = \\frac{{{a}}}{{1 + {a * a}x^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(u)] = \\frac{{u'}}{{1 + u^2}} = \\frac{{{a}}}{{1 + ({a}x)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arctan" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arcsin" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arccos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{{a}}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = -\\frac{{1}}{{\\sqrt{{1 - {a * a}x^2}}}}", $"y' = -\\frac{{{a}}}{{1 + {a * a}x^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int b = rng.Next(1, 6);
                    string formula = $"y = \\arctan(x + {b})";
                    string ans = $"y' = \\frac{{1}}{{1 + (x + {b})^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(x + {b})] = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arctan_shift" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{\\sqrt{{1 - (x + {b})^2}}}}", $"y' = \\frac{{{b}}}{{1 + (x + {b})^2}}", "y' = \\frac{1}{1 + x^2}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"y = {a}\\arcsin(x) + {b}x";
                    string ans = $"y' = \\frac{{{a}}}{{\\sqrt{{1 - x^2}}}} + {b}";
                    string steps = $"{stepLabel}: $y' = {a}\\cdot\\frac{{1}}{{\\sqrt{{1-x^2}}}} + {b} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arcsin_sum" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{{a}}}{{1 + x^2}} + {b}", $"y' = \\frac{{{a}}}{{\\sqrt{{1 - x^2}}}}", $"y' = \\frac{{1}}{{\\sqrt{{1 - x^2}}}} + {b}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = \\arctan\\left(\\frac{{x}}{{{a}}}\\right)";
                    string ans = $"y' = \\frac{{{a}}}{{{a * a} + x^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(x/{a})] = \\frac{{1/{a}}}{{1 + (x/{a})^2}} = \\frac{{{a}}}{{{a * a} + x^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_easy_arctan_scaled" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{{a * a} + x^2}}", $"y' = \\frac{{{a}}}{{1 + x^2}}", $"y' = \\frac{{1}}{{{a}(1 + x^2)}}", rng);
                    return q;
                });
            }
            // 2. Medium pool (6 archetypes)
            else if (comp == 1)
            {
                pool.Add(() =>
                {
                    string formula = "y = \\arctan(x^2)";
                    string ans = "y' = \\frac{2x}{1 + x^4}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(x^2)] = \\frac{{(x^2)'}}{{1 + (x^2)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_arctan_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{2x}{1 + x^2}", "y' = \\frac{1}{1 + x^4}", "y' = \\frac{2x}{\\sqrt{1 - x^4}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = x\\arctan(x)";
                    string ans = "y' = \\arctan(x) + \\frac{x}{1 + x^2}";
                    string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = (1)\\arctan(x) + x\\cdot\\frac{{1}}{{1 + x^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_x_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\arctan(x) - \\frac{x}{1 + x^2}", "y' = \\frac{x}{1 + x^2}", "y' = \\arctan(x) + \\frac{1}{1 + x^2}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = \\arcsin(x^2)";
                    string ans = "y' = \\frac{2x}{\\sqrt{1 - x^4}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(x^2)] = \\frac{{(x^2)'}}{{\\sqrt{{1 - (x^2)^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_arcsin_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{2x}{\\sqrt{1 - x^2}}", "y' = \\frac{x}{\\sqrt{1 - x^4}}", "y' = \\frac{2x}{1 + x^4}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = x^2\\arcsin(x)";
                    string ans = "y' = 2x\\arcsin(x) + \\frac{x^2}{\\sqrt{1 - x^2}}";
                    string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = 2x\\arcsin(x) + x^2\\cdot\\frac{{1}}{{\\sqrt{{1-x^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_xsq_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = 2x\\arcsin(x) + \\frac{x^2}{1 + x^2}", "y' = 2x\\arcsin(x)", "y' = \\frac{x^2}{\\sqrt{1 - x^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int b = rng.Next(1, 5);
                    string formula = $"y = \\arctan({a}x + {b})";
                    string ans = $"y' = \\frac{{{a}}}{{1 + ({a}x + {b})^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan({a}x + {b})] = \\frac{{{a}}}{{1 + ({a}x + {b})^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_arctan_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{1 + ({a}x + {b})^2}}", $"y' = \\frac{{{a}}}{{\\sqrt{{1 - ({a}x + {b})^2}}}}", $"y' = \\frac{{{a}}}{{1 + {a * a}x^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = (\\arcsin(x))^2";
                    string ans = "y' = \\frac{2\\arcsin(x)}{\\sqrt{1 - x^2}}";
                    string steps = $"{stepLabel}: $\\text{{Regla de la cadena: }} y' = 2\\arcsin(x)\\cdot\\frac{{1}}{{\\sqrt{{1-x^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_med_arcsin_power" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{2}{\\sqrt{1 - x^2}}", "y' = \\frac{2\\arcsin(x)}{1 + x^2}", "y' = 2\\arcsin(x)", rng);
                    return q;
                });
            }
            // 3. Hard pool (6 archetypes)
            else
            {
                pool.Add(() =>
                {
                    string formula = "y = \\arcsin(\\sqrt{x})";
                    string ans = "y' = \\frac{1}{2\\sqrt{x}\\sqrt{1 - x}}";
                    string steps = $"{stepLabel}: " + @"$\frac{d}{dx}[\arcsin(\sqrt{x})] = \frac{1/(2\sqrt{x})}{\sqrt{1 - x}} = " + ans + "$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arcsin_sqrt" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arctan_div" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{{a * a} + x^2}}", $"y' = \\frac{{{a}}}{{1 + x^2}}", $"y' = \\frac{{{a}}}{{\\sqrt{{{a * a} - x^2}}}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = \\arctan(e^x)";
                    string ans = "y' = \\frac{e^x}{1 + e^{2x}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(e^x)] = \\frac{{e^x}}{{1 + (e^x)^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arctan_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{e^x}{1 + e^x}", "y' = \\frac{1}{1 + e^{2x}}", "y' = \\frac{e^x}{\\sqrt{1 - e^{2x}}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = \\arcsin(\\ln(x))";
                    string ans = "y' = \\frac{1}{x\\sqrt{1 - (\\ln(x))^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(\\ln(x))] = \\frac{{1/x}}{{\\sqrt{{1 - (\\ln(x))^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arcsin_ln" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{1}{\\sqrt{1 - (\\ln(x))^2}}", "y' = \\frac{1}{x(1 + (\\ln(x))^2)}", "y' = \\frac{\\ln(x)}{x\\sqrt{1 - (\\ln(x))^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = \\arctan\\left(\\frac{1}{x}\\right)";
                    string ans = "y' = -\\frac{1}{1 + x^2}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arctan(1/x)] = \\frac{{-1/x^2}}{{1 + 1/x^2}} = -\\frac{{1}}{{x^2 + 1}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arctan_recip" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{1}{1 + x^2}", "y' = -\\frac{1}{x^2}", "y' = \\frac{1}{x\\sqrt{x^2 - 1}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"y = \\arcsin(e^{{{k}x}})";
                    string ans = $"y' = \\frac{{{k}e^{{{k}x}}}}{{\\sqrt{{1 - e^{{{2 * k}x}}Source}}}}".Replace("Source", "");
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\arcsin(e^{{{k}x}})] = \\frac{{{k}e^{{{k}x}}}}{{\\sqrt{{1 - (e^{{{k}x}})^2}}}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "inv_trig_deriv_hard_arcsin_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{e^{{{k}x}}}}{{\\sqrt{{1 - e^{{{2 * k}x}}}}}}", $"y' = \\frac{{{k}e^{{{k}x}}}}{{1 + e^{{{2 * k}x}}}}", $"y' = {k}e^{{{k}x}}", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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
                // Simple Natural Functions
                pool.Add(() =>
                {
                    int a = rng.Next(2, 7);
                    string formula = $"y = {a}e^x";
                    string ans = $"y' = {a}e^x";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[{a}e^x] = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_simple_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = e^x", $"y' = {a}x e^{{x-1}}", $"y' = \\frac{{{a}}}{{x}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 7);
                    string formula = $"y = {a}\\ln(x)";
                    string ans = $"y' = \\frac{{{a}}}{{x}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[{a}\\ln(x)] = {a}\\cdot\\frac{{1}}{{x}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_simple_ln" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = \\frac{1}{x}", $"y' = {a}\\ln(x)", $"y' = -\\frac{{{a}}}{{x^2}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 6);
                    string formula = $"y = {a}e^x + {b}x";
                    string ans = $"y' = {a}e^x + {b}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[{a}e^x + {b}x] = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_sum_exp_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {a}e^x", $"y' = e^x + {b}", $"y' = {a}x e^x + {b}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    string formula = $"y = {a}\\ln(x) + x^2";
                    string ans = $"y' = \\frac{{{a}}}{{x}} + 2x";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[{a}\\ln(x) + x^2] = \\frac{{{a}}}{{x}} + 2x$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_sum_ln_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{1}}{{x}} + 2x", $"y' = \\frac{{{a}}}{{x}} + x", $"y' = {a}\\ln(x) + 2x", rng);
                    return q;
                });

                // Composite Natural Functions
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"y = e^{{{k}x}}";
                    string ans = $"y' = {k}e^{{{k}x}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{u}}] = u'e^{{u}} = {k}e^{{{k}x}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_comp_exp_kx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_comp_ln_kx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{{k}}}{{x}}", $"y' = \\frac{{1}}{{{k}x}}", $"y' = {k}\\ln(x)", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(1, 4);
                    string formula = $"y = e^{{{a}x^2}}";
                    string ans = $"y' = {2 * a}x e^{{{a}x^2}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{{a}x^2}}] = (2\\cdot {a}x)e^{{{a}x^2}} = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_comp_exp_quad" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_comp_ln_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{x}}{{x^2 + {a}}}", $"y' = \\frac{{2x}}{{x + {a}}}", $"y' = \\frac{{1}}{{x^2 + {a}}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "y = x^2 e^x";
                    string ans = "y' = (x^2 + 2x)e^x";
                    string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = 2x e^x + x^2 e^x = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_prod_xsq_exp" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "y' = 2x e^x", "y' = (x^2 + x)e^x", "y' = x^2 e^x + 2", rng);
                    return q;
                });
                if (comp >= 1)
                {
                    pool.Add(() =>
                    {
                        string formula = "y = \\frac{e^x}{x^2 + 1}";
                        string ans = "y' = \\frac{e^x(x^2 - 2x + 1)}{(x^2 + 1)^2}";
                        string steps = $"{stepLabel}: $\\text{{Regla del cociente: }} y' = \\frac{{e^x(x^2 + 1) - e^x(2x)}}{{(x^2 + 1)^2}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_quot_exp" };
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
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_ln_sqrt" };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{2x}}{{x^2 + {a}}}", $"y' = \\frac{{1}}{{2\\sqrt{{x^2 + {a}}}}}", $"y' = \\frac{{x}}{{\\sqrt{{x^2 + {a}}}}}", rng);
                        return q;
                    });
                }
                if (HasTrig(p))
                {
                    pool.Add(() =>
                    {
                        string formula = "y = e^{\\sin(x)}";
                        string ans = "y' = \\cos(x)e^{\\sin(x)}";
                        string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[e^{{\\sin(x)}}] = (\\sin(x))'e^{{\\sin(x)}} = {ans}$";
                        var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_nat_exp_sin" };
                        if (type == QuestionType.MultipleChoice)
                            q.Choices = DistractorHelper.BuildChoices(ans, "y' = -\\cos(x)e^{\\sin(x)}", "y' = \\sin(x)e^{\\sin(x)}", "y' = e^{\\cos(x)}", rng);
                        return q;
                    });
                }
            }
            else
            {
                int[] bases = { 2, 3, 5, 10 };
                int b = bases[rng.Next(bases.Length)];

                // Simple general base
                pool.Add(() =>
                {
                    string formula = $"y = {b}^x";
                    string ans = $"y' = {b}^x \\ln({b})";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^x] = a^x\\ln(a) \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_simple_bx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {b}^x", $"y' = x {b}^{{x-1}}", $"y' = \\frac{{{b}^x}}{{\\ln({b})}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"y = \\log_{{{b}}}(x)";
                    string ans = $"y' = \\frac{{1}}{{x\\ln({b})}}";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[\\log_a(x)] = \\frac{{1}}{{x\\ln(a)}} \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_simple_logbx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{\\ln({b})}}{{x}}", $"y' = \\frac{{1}}{{x}}", $"y' = \\frac{{{b}}}{{x\\ln({b})}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"y = {a}\\cdot {b}^x + x";
                    string ans = $"y' = {a}\\cdot {b}^x \\ln({b}) + 1";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[{a}\\cdot {b}^x + x] = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_sum_bx_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {a}\\cdot {b}^x \\ln({b})", $"y' = {b}^x \\ln({b}) + 1", $"y' = {a}\\cdot {b}^x + 1", rng);
                    return q;
                });

                // Composite general base
                pool.Add(() =>
                {
                    int k = rng.Next(2, 5);
                    string formula = $"y = {b}^{{{k}x}}";
                    string ans = $"y' = {k}\\cdot {b}^{{{k}x}} \\ln({b})";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^{{kx}}] = k a^{{kx}}\\ln(a) \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_comp_bkx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_comp_logb_quad" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = \\frac{{2x}}{{x^2 + {c}}}", $"y' = \\frac{{1}}{{(x^2 + {c})\\ln({b})}}", $"y' = \\frac{{2x\\ln({b})}}{{x^2 + {c}}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"y = {b}^{{x^2}}";
                    string ans = $"y' = 2x\\cdot {b}^{{x^2}} \\ln({b})";
                    string steps = $"{stepLabel}: $\\frac{{d}}{{dx}}[a^{{u}}] = u' a^u \\ln(a) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_comp_b_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = 2x {b}^{{x^2}}", $"y' = {b}^{{x^2}} \\ln({b})", $"y' = x^2 {b}^{{x^2 - 1}}", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = $"y = x\\cdot {b}^x";
                    string ans = $"y' = {b}^x(1 + x\\ln({b}))";
                    string steps = $"{stepLabel}: $\\text{{Regla del producto: }} y' = (1){b}^x + x({b}^x\\ln({b})) = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "explog_deriv_gen_prod_x_bx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"y' = {b}^x(1 + \\ln({b}))", $"y' = x {b}^x \\ln({b})", $"y' = {b}^x + x", rng);
                    return q;
                });
            }

            int pickIdx = Math.Abs(index) % pool.Count;
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

            // Simple Exponentials and Logarithms
            pool.Add(() =>
            {
                int a = rng.Next(2, 7);
                string formula = $"\\int {a}e^x\\,dx";
                string ans = $"{a}e^x + C";
                string steps = $"{intLabel}: $\\int {a}e^x\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_simple_exp" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{{a}}}{{2}}e^x + C", $"{a}x e^x + C", $"e^x + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int a = rng.Next(2, 7);
                string formula = $"\\int \\frac{{{a}}}{{x}}\\,dx";
                string ans = $"{a}\\ln|x| + C";
                string steps = $"{intLabel}: $\\int \\frac{{{a}}}{{x}}\\,dx = {a}\\int \\frac{{1}}{{x}}\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_simple_recip_x" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{{a}}}{{x^2}} + C", $"\\frac{{1}}{{{a}}}\\ln|x| + C", $"\\ln|{a}x| + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int a = rng.Next(2, 6);
                int b = rng.Next(2, 6);
                string formula = $"\\int ({a}e^x + {b}x)\\,dx";
                string ans = $"{a}e^x + \\frac{{{b}}}{{2}}x^2 + C";
                string steps = $"{intLabel}: $\\int ({a}e^x + {b}x)\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_sum_exp_linear" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{a}e^x + {b}x^2 + C", $"{a}e^x + {b} + C", $"\\frac{{{a}}}{{2}}e^x + {b}x + C", rng);
                return q;
            });

            // Composite Exponentials and Logarithms
            pool.Add(() =>
            {
                int k = rng.Next(2, 6);
                string formula = $"\\int e^{{{k}x}}\\,dx";
                string ans = $"\\frac{{1}}{{{k}}}e^{{{k}x}} + C";
                string steps = $"{intLabel}: $\\int e^{{kx}}\\,dx = \\frac{{1}}{{k}}e^{{kx}} + C = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_comp_exp_kx" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{k}e^{{{k}x}} + C", $"e^{{{k}x}} + C", $"\\frac{{1}}{{{k}}}e^x + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int k = rng.Next(2, 5);
                int a = rng.Next(2, 6);
                string formula = $"\\int \\left(e^{{{k}x}} + \\frac{{{a}}}{{x}}\\right)\\,dx";
                string ans = $"\\frac{{1}}{{{k}}}e^{{{k}x}} + {a}\\ln|x| + C";
                string steps = $"{intLabel}: $\\int e^{{{k}x}}\\,dx + {a}\\int \\frac{{1}}{{x}}\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_sum_exp_recip" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{k}e^{{{k}x}} + {a}\\ln|x| + C", $"\\frac{{1}}{{{k}}}e^{{{k}x}} - \\frac{{{a}}}{{x^2}} + C", $"e^{{{k}x}} + \\ln|x| + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                string formula = "\\int x e^{x^2}\\,dx";
                string ans = "\\frac{1}{2}e^{x^2} + C";
                string steps = $"{intLabel}: $u = x^2,\\; du = 2x\\,dx \\implies \\frac{{1}}{{2}}\\int e^u\\,du = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_subs_x_exp_xsq" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_subs_frac_quad" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{2}}\\ln(x^2 + {a}) + C", $"2\\ln(x^2 + {a}) + C", $"\\ln|2x| + C", rng);
                return q;
            });
            if (comp >= 1)
            {
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string formula = $"\\int \\frac{{e^x}}{{e^x + {a}}}\\,dx";
                    string ans = $"\\ln(e^x + {a}) + C";
                    string steps = $"{intLabel}: $u = e^x + {a},\\; du = e^x\\,dx \\implies \\int \\frac{{1}}{{u}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_subs_exp_frac" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{e^x}}{{e^x + {a}}} + C", $"e^x\\ln(e^x + {a}) + C", $"\\ln|e^x| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{\\ln(x)}{x}\\,dx";
                    string ans = "\\frac{1}{2}(\\ln(x))^2 + C";
                    string steps = $"{intLabel}: $u = \\ln(x),\\; du = \\frac{{1}}{{x}}\\,dx \\implies \\int u\\,du = \\frac{{u^2}}{{2}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_subs_ln_over_x" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "(\\ln(x))^2 + C", "\\frac{1}{x^2} + C", "\\ln|\\ln(x)| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int x^2 e^{x^3}\\,dx";
                    string ans = "\\frac{1}{3}e^{x^3} + C";
                    string steps = $"{intLabel}: $u = x^3,\\; du = 3x^2\\,dx \\implies \\frac{{1}}{{3}}\\int e^u\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_logexp_subs_xsq_exp_xcubed" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "3e^{x^3} + C", "\\frac{1}{3}x^3 e^{x^3} + C", "e^{x^3} + C", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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

            // Simple Trigonometric Integrals
            pool.Add(() =>
            {
                int a = rng.Next(2, 7);
                string formula = $"\\int {a}*\\cos(x)\\,dx";
                string ans = $"{a}\\sin(x) + C";
                string steps = $"{intLabel}: $\\int {a}\\cos(x)\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_simple_cos" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"-{a}\\sin(x) + C", $"{a}\\cos(x) + C", $"\\frac{{{a}}}{{2}}\\sin(x) + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int a = rng.Next(2, 7);
                string formula = $"\\int {a}*\\sin(x)\\,dx";
                string ans = $"-{a}\\cos(x) + C";
                string steps = $"{intLabel}: $\\int {a}\\sin(x)\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_simple_sin" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{a}\\cos(x) + C", $"-{a}\\sin(x) + C", $"\\cos({a}x) + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int a = rng.Next(2, 6);
                string formula = $"\\int {a}*\\sec^2(x)\\,dx";
                string ans = $"{a}\\tan(x) + C";
                string steps = $"{intLabel}: $\\int {a}\\sec^2(x)\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_simple_sec2" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"-{a}\\tan(x) + C", $"{a}\\sec(x) + C", $"\\tan({a}x) + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int a = rng.Next(2, 5);
                int b = rng.Next(2, 5);
                string formula = $"\\int ({a}*\\sin(x) + {b}*\\cos(x))\\,dx";
                string ans = $"-{a}\\cos(x) + {b}\\sin(x) + C";
                string steps = $"{intLabel}: $\\int ({a}\\sin(x) + {b}\\cos(x))\\,dx = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_sum_sin_cos" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{a}\\cos(x) + {b}\\sin(x) + C", $"-{a}\\cos(x) - {b}\\sin(x) + C", $"{a}\\sin(x) - {b}\\cos(x) + C", rng);
                return q;
            });

            // Composite Trigonometric Integrals
            pool.Add(() =>
            {
                int k = rng.Next(2, 6);
                string formula = $"\\int \\cos({k}x)\\,dx";
                string ans = $"\\frac{{1}}{{{k}}}\\sin({k}x) + C";
                string steps = $"{intLabel}: $\\int \\cos(kx)\\,dx = \\frac{{1}}{{k}}\\sin(kx) + C = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_cos_kx" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_sin_kx" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_sec2_kx" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"-\\frac{{1}}{{{k}}}\\tan({k}x) + C", $"{k}\\tan({k}x) + C", $"\\frac{{1}}{{{k}}}\\sec({k}x) + C", rng);
                return q;
            });
            pool.Add(() =>
            {
                int k = rng.Next(2, 5);
                string formula = $"\\int \\sec({k}x)\\tan({k}x)\\,dx";
                string ans = $"\\frac{{1}}{{{k}}}\\sec({k}x) + C";
                string steps = $"{intLabel}: $\\int \\sec(kx)\\tan(kx)\\,dx = \\frac{{1}}{{k}}\\sec(kx) + C = {ans}$";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_comp_sec_tan_kx" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_sum_linear_cos" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{a}x^2 - \\sin(x) + C", $"\\frac{{{a}}}{{2}}x^2 - \\sin(x) + C", $"{a}x + \\sin(x) + C", rng);
                return q;
            });
            if (comp >= 1)
            {
                pool.Add(() =>
                {
                    string formula = "\\int \\tan(x)\\,dx";
                    string ans = "\\ln|\\sec(x)| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{\\sin(x)}}{{\\cos(x)}}\\,dx = -\\ln|\\cos(x)| + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_tan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\sec^2(x) + C", "-\\ln|\\sec(x)| + C", "\\ln|\\cos(x)| + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\cot(x)\\,dx";
                    string ans = "\\ln|\\sin(x)| + C";
                    string steps = $"{intLabel}: $\\int \\frac{{\\cos(x)}}{{\\sin(x)}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_trig_cot" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\csc^2(x) + C", "-\\ln|\\sin(x)| + C", "\\ln|\\cos(x)| + C", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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
                // 6 Basic Archetypes
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{{a * a} - x^2}}}}\\,dx";
                    string ans = $"\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{\\sqrt{{a^2 - x^2}}}}\\,dx = \\arcsin(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arcsin_a" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arctan_a" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"{a}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{1}{1 + x^2}\\,dx";
                    string ans = "\\arctan(x) + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{1 + x^2}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arctan_unit" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\arcsin(x) + C", "\\ln|1 + x^2| + C", "\\frac{1}{x} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{1}{\\sqrt{1 - x^2}}\\,dx";
                    string ans = "\\arcsin(x) + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{\\sqrt{{1 - x^2}}}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arcsin_unit" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\arctan(x) + C", "\\arccos(x) + C", "\\frac{1}{\\sqrt{1-x^2}} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{{k}}}{{1 + x^2}}\\,dx";
                    string ans = $"{k}\\arctan(x) + C";
                    string steps = $"{intLabel}: ${k}\\int \\frac{{1}}{{1 + x^2}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arctan_k_unit" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arctan(x) + C", $"{k}\\arcsin(x) + C", $"\\arctan({k}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int k = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{{k}}}{{\\sqrt{{1 - x^2}}}}\\,dx";
                    string ans = $"{k}\\arcsin(x) + C";
                    string steps = $"{intLabel}: ${k}\\int \\frac{{1}}{{\\sqrt{{1 - x^2}}}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_arcsin_k_unit" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arcsin(x) + C", $"{k}\\arctan(x) + C", $"\\arcsin({k}x) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                // 6 Medium Archetypes
                pool.Add(() =>
                {
                    int a = rng.Next(2, 5);
                    int k = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{{k}}}{{{a * a} + x^2}}\\,dx";
                    string ans = $"\\frac{{{k}}}{{{a}}}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $k\\int \\frac{{1}}{{a^2 + x^2}}\\,dx = \\frac{{k}}{{a}}\\arctan(x/a) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_k_over_asq_plus_xsq" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_k_over_sqrt_asq_minus_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{{k}}}{{{a}}}\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", $"{k}\\arctan\\left(\\frac{{x}}{{{a}}}\\right) + C", $"\\arcsin\\left(\\frac{{x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(1, 6);
                    string formula = $"\\int \\frac{{1}}{{(x + {c})^2 + 1}}\\,dx";
                    string ans = $"\\arctan(x + {c}) + C";
                    string steps = $"{intLabel}: $u = x + {c},\\; du = dx \\implies \\int \\frac{{1}}{{u^2 + 1}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_shifted_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin(x + {c}) + C", $"\\arctan(x) + {c} + C", $"\\frac{{1}}{{x + {c}}} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(1, 6);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{1 - (x - {c})^2}}}}\\,dx";
                    string ans = $"\\arcsin(x - {c}) + C";
                    string steps = $"{intLabel}: $u = x - {c},\\; du = dx \\implies \\int \\frac{{1}}{{\\sqrt{{1 - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_shifted_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arccos(x - {c}) + C", $"\\arctan(x - {c}) + C", $"\\arcsin(x) - {c} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int c = rng.Next(2, 6);
                    string formula = $"\\int \\left(\\frac{{1}}{{1 + x^2}} + {c}x\\right)\\,dx";
                    string ans = $"\\arctan(x) + \\frac{{{c}}}{{2}}x^2 + C";
                    string steps = $"{intLabel}: $\\int \\frac{{1}}{{1 + x^2}}\\,dx + \\int {c}x\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_sum_poly" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin(x) + \\frac{{{c}}}{{2}}x^2 + C", $"\\arctan(x) + {c}x^2 + C", $"\\frac{{1}}{{1+x^2}} + {c} + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{2}{4 + x^2}\\,dx";
                    string ans = "\\arctan\\left(\\frac{x}{2}\\right) + C";
                    string steps = $"{intLabel}: $2\\cdot\\frac{{1}}{{2}}\\arctan(x/2) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_scaled_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\frac{1}{2}\\arctan\\left(\\frac{x}{2}\\right) + C", "2\\arctan(x/2) + C", "\\arcsin(x/2) + C", rng);
                    return q;
                });
            }
            else
            {
                // 6 Hard / Composite Archetypes
                pool.Add(() =>
                {
                    int b = rng.Next(2, 5);
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{1 - {b * b}x^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arcsin({b}x) + C";
                    string steps = $"{intLabel}: $u = {b}x,\\; du = {b}\\,dx \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{\\sqrt{{1 - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_comp_arcsin_bx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_comp_arctan_bx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"{b}\\arctan({b}x) + C", $"\\arctan({b}x) + C", $"\\frac{{1}}{{{b}}}\\arcsin({b}x) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = 2;
                    int b = 3;
                    string formula = $"\\int \\frac{{1}}{{{a * a} + {b * b}x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{a * b}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $u = {b}x \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{{a}^2 + u^2}}\\,du = \\frac{{1}}{{{a * b}}}\\arctan({b}x/{a}) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_comp_arctan_ab" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{b}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    int a = 3;
                    int b = 2;
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{{a * a} - {b * b}x^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C";
                    string steps = $"{intLabel}: $u = {b}x \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{\\sqrt{{{a}^2 - u^2}}}}\\,du = \\frac{{1}}{{{b}}}\\arcsin({b}x/{a}) + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_comp_arcsin_ab" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{a * b}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{1}{x^2 + 2x + 2}\\,dx";
                    string ans = "\\arctan(x + 1) + C";
                    string steps = $"{intLabel}: $x^2 + 2x + 2 = (x + 1)^2 + 1 \\implies \\int \\frac{{1}}{{(x+1)^2 + 1}}\\,dx = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_complete_square_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\arctan(x) + 1 + C", "\\ln|x^2 + 2x + 2| + C", "\\arcsin(x + 1) + C", rng);
                    return q;
                });
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{1}{\\sqrt{3 - 2x - x^2}}\\,dx";
                    string ans = "\\arcsin\\left(\\frac{x + 1}{2}\\right) + C";
                    string steps = $"{intLabel}: $3 - 2x - x^2 = 4 - (x + 1)^2 = 2^2 - (x+1)^2 \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "indef_invtrig_complete_square_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "\\frac{1}{2}\\arcsin(x + 1) + C", "\\arcsin(x + 1) + C", "\\arctan\\left(\\frac{x+1}{2}\\right) + C", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sin_pow" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_cos_pow" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_tan_pow" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sin_cos_linear" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\sin^2({k}x) + C", $"-\\frac{{1}}{{{den}}}\\cos^2({k}x) + C", $"\\sin^2({k}x) + C", rng);
                    return q;
                });

                // 5. sin(x) / cos^2(x) = sec(x)
                pool.Add(() =>
                {
                    string formula = "\\int \\frac{\\sin(x)}{\\cos^2(x)}\\,dx";
                    string ans = "\\sec(x) + C";
                    string steps = $"{substLabel}: $u = \\cos(x),\\; du = -\\sin(x)\\,dx \\implies -\\int u^{{-2}}\\,du = u^{{-1}} + C = \\frac{{1}}{{\\cos(x)}} + C = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sec_recip_cos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, "-\\sec(x) + C", "\\cos(x) + C", "-\\frac{1}{3}\\cos^3(x) + C", rng);
                    return q;
                });

                // 6. (a + sin(x))^k * cos(x)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 6);
                    int k = rng.Next(2, 5);
                    int k1 = k + 1;
                    string formula = $"\\int ({a} + \\sin(x))^{{{k}}}\\cos(x)\\,dx";
                    string ans = $"\\frac{{1}}{{{k1}}}({a} + \\sin(x))^{{{k1}}} + C";
                    string steps = $"{substLabel}: $u = {a} + \\sin(x),\\; du = \\cos(x)\\,dx \\implies \\int u^{{{k}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_poly_sin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"({a} + \\sin(x))^{{{k1}}} + C", $"-\\frac{{1}}{{{k1}}}({a} + \\cos(x))^{{{k1}}} + C", $"\\frac{{1}}{{{k}}}({a} + \\sin(x))^{{{k}}} + C", rng);
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_xm_cos" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_xm_sin" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sec2_tann" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\tan^{{{n1}}}({k}x) + C", $"\\frac{{1}}{{{den}}}\\sec^{{{n1}}}({k}x) + C", $"\\tan^{{{n1}}}({k}x) + C", rng);
                    return q;
                });

                // 4. x * sec^2(kx^2)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int x\\sec^2({kStr}x^2)\\,dx";
                    int den = 2 * k;
                    string ans = den == 2 ? "\\frac{1}{2}\\tan(x^2) + C" : $"\\frac{{1}}{{{den}}}\\tan({k}x^2) + C";
                    string steps = $"{substLabel}: $u = {kStr}x^2,\\; du = {2 * k}x\\,dx \\implies \\frac{{1}}{{{den}}}\\int \\sec^2(u)\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_x_sec2_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\tan({kStr}x^2) + C", $"\\frac{{1}}{{{den}}}\\sec({kStr}x^2) + C", $"-\\frac{{1}}{{{den}}}\\tan({kStr}x^2) + C", rng);
                    return q;
                });

                // 5. cos(kx) / sqrt(sin(kx))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\cos({kStr}x)}}{{\\sqrt{{\\sin({kStr}x)}}}}\\,dx";
                    string ans = k == 1 ? "2\\sqrt{\\sin(x)} + C" : (k == 2 ? "\\sqrt{\\sin(2x)} + C" : $"\\frac{{2}}{{{k}}}\\sqrt{{\\sin({k}x)}} + C");
                    string steps = $"{substLabel}: $u = \\sin({kStr}x),\\; du = {k}\\cos({kStr}x)\\,dx \\implies \\frac{{1}}{{{k}}}\\int u^{{-1/2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_cos_sqrt_sin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{2}}\\sqrt{{\\sin({kStr}x)}} + C", $"-2\\sqrt{{\\cos({kStr}x)}} + C", $"\\sqrt{{\\sin({kStr}x)}} + C", rng);
                    return q;
                });

                // 6. cos(x) / (a + sin(x))
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{\\cos(x)}}{{{a} + \\sin(x)}}\\,dx";
                    string ans = $"\\ln({a} + \\sin(x)) + C";
                    string steps = $"{substLabel}: $u = {a} + \\sin(x),\\; du = \\cos(x)\\,dx \\implies \\int \\frac{{1}}{{u}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_cos_over_sin_ln" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\ln({a} + \\sin(x)) + C", $"\\ln({a} + \\cos(x)) + C", $"\\frac{{1}}{{{a}}}\\ln({a} + \\sin(x)) + C", rng);
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_cos_sqrtx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sin_lnx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_cos_lnx" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_exp_cos" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"-\\cos(e^{{{kStr}x}}) + C", $"{k}\\sin(e^{{{kStr}x}}) + C", $"\\cos(e^{{{kStr}x}}) + C", rng);
                    return q;
                });

                // 5. sec^2(sqrt(x)) / sqrt(x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\sec^2({kStr}\\sqrt{{x}})}}{{\\sqrt{{x}}}}\\,dx";
                    string ans = k == 1 ? "2\\tan(\\sqrt{x}) + C" : (2 % k == 0 ? $"{2 / k}\\tan({k}\\sqrt{{x}}) + C" : $"\\frac{{2}}{{{k}}}\\tan({k}\\sqrt{{x}}) + C");
                    string steps = $"{substLabel}: $u = {kStr}\\sqrt{{x}},\\; du = \\frac{{{k}}}{{2\\sqrt{{x}}}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_sec2_sqrtx" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{2}}\\tan({kStr}\\sqrt{{x}}) + C", $"-2\\tan({kStr}\\sqrt{{x}}) + C", $"2\\sec({kStr}\\sqrt{{x}}) + C", rng);
                    return q;
                });

                // 6. e^x * sin(e^x)
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int e^{{{kStr}x}}\\sin(e^{{{kStr}x}})\\,dx";
                    string ans = k == 1 ? "-\\cos(e^x) + C" : $"-\\frac{{1}}{{{k}}}\\cos(e^{{{k}x}}) + C";
                    string steps = $"{substLabel}: $u = e^{{{kStr}x}},\\; du = {k}e^{{{kStr}x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_trig_exp_sin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\cos(e^{{{kStr}x}}) + C", $"-\\frac{{1}}{{{k}}}\\sin(e^{{{kStr}x}}) + C", $"{k}\\cos(e^{{{kStr}x}}) + C", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arctan_ax" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arcsin_ax" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arctan_xb" };
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
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arcsin_xb" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{b}}}\\arcsin(x/{b}) + C", $"\\arcsin(x/{b * b}) + C", $"\\frac{{1}}{{{b}}}\\arctan(x/{b}) + C", rng);
                    return q;
                });
                // 5. 1 / (a^2 + b^2*x^2)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 6);
                    while (b == a) b = rng.Next(2, 6);
                    int a2 = a * a;
                    int b2 = b * b;
                    int ab = a * b;
                    string formula = $"\\int \\frac{{1}}{{{a2} + {b2}x^2}}\\,dx";
                    string ans = $"\\frac{{1}}{{{ab}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C";
                    string steps = $"{substLabel}: $u = \\frac{{{b}x}}{{{a}}},\\; du = \\frac{{{b}}}{{{a}}}\\,dx \\implies \\frac{{1}}{{{ab}}}\\int \\frac{{1}}{{1 + u^2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arctan_ab_basic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a2}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{ab}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{b}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
                // 6. 1 / sqrt(a^2 - b^2*x^2)
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    int b = rng.Next(2, 6);
                    while (b == a) b = rng.Next(2, 6);
                    int a2 = a * a;
                    int b2 = b * b;
                    string formula = $"\\int \\frac{{1}}{{\\sqrt{{{a2} - {b2}x^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{b}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C";
                    string steps = $"{substLabel}: $u = \\frac{{{b}x}}{{{a}}},\\; du = \\frac{{{b}}}{{{a}}}\\,dx \\implies \\frac{{1}}{{{b}}}\\int \\frac{{1}}{{\\sqrt{{1 - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_arcsin_ab_basic" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a}}}\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\arcsin\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", $"\\frac{{1}}{{{b}}}\\arctan\\left(\\frac{{{b}x}}{{{a}}}\\right) + C", rng);
                    return q;
                });
            }
            else if (comp == 1)
            {
                // 1. x / (1 + a^2*x^4)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 7);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x}}{{1 + {aStr}x^4}}\\,dx";
                    int den = 2 * a;
                    string ans = den == 2 && a == 1 ? "\\frac{1}{2}\\arctan(x^2) + C" : $"\\frac{{1}}{{{den}}}\\arctan({a}x^2) + C";
                    string steps = $"{substLabel}: $u = {a}x^2,\\; du = {2 * a}x\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_x_arctan_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan({a}x^2) + C", $"\\frac{{1}}{{{den}}}\\arcsin({a}x^2) + C", $"\\frac{{1}}{{{4 * a}}}\\arctan({a}x^4) + C", rng);
                    return q;
                });
                // 2. x / sqrt(1 - a^2*x^4)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 7);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x}}{{\\sqrt{{1 - {aStr}x^4}}}}\\,dx";
                    int den = 2 * a;
                    string ans = den == 2 && a == 1 ? "\\frac{1}{2}\\arcsin(x^2) + C" : $"\\frac{{1}}{{{den}}}\\arcsin({a}x^2) + C";
                    string steps = $"{substLabel}: $u = {a}x^2,\\; du = {2 * a}x\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_x_arcsin_xsq" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin({a}x^2) + C", $"\\frac{{1}}{{{den}}}\\arctan({a}x^2) + C", $"\\frac{{1}}{{{4 * a}}}\\arcsin({a}x^4) + C", rng);
                    return q;
                });
                // 3. e^(kx) / (1 + e^(2kx))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{e^{{{kStr}x}}}}{{1 + e^{{{2 * k}x}}}}\\,dx";
                    string ans = k == 1 ? "\\arctan(e^x) + C" : $"\\frac{{1}}{{{k}}}\\arctan(e^{{{k}x}}) + C";
                    string steps = $"{substLabel}: $u = e^{{{kStr}x}},\\; du = {k}e^{{{kStr}x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_exp_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arcsin(e^{{{k}x}}) + C", $"{k}\\arctan(e^{{{k}x}}) + C", $"\\ln(1 + e^{{{2 * k}x}}) + C", rng);
                    return q;
                });
                // 4. x^2 / (1 + a^2*x^6)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x^2}}{{1 + {aStr}x^6}}\\,dx";
                    int den = 3 * a;
                    string ans = den == 3 && a == 1 ? "\\frac{1}{3}\\arctan(x^3) + C" : $"\\frac{{1}}{{{den}}}\\arctan({a}x^3) + C";
                    string steps = $"{substLabel}: $u = {a}x^3,\\; du = {3 * a}x^2\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_xsq_arctan_xcube" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{den}}}\\arcsin({a}x^3) + C", $"\\arctan({a}x^3) + C", $"\\frac{{1}}{{{6 * a}}}\\arctan({a}x^6) + C", rng);
                    return q;
                });
                // 5. x^2 / sqrt(1 - a^2*x^6)
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    string aStr = a == 1 ? "" : $"{a * a}";
                    string formula = $"\\int \\frac{{x^2}}{{\\sqrt{{1 - {aStr}x^6}}}}\\,dx";
                    int den = 3 * a;
                    string ans = den == 3 && a == 1 ? "\\frac{1}{3}\\arcsin(x^3) + C" : $"\\frac{{1}}{{{den}}}\\arcsin({a}x^3) + C";
                    string steps = $"{substLabel}: $u = {a}x^3,\\; du = {3 * a}x^2\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_xsq_arcsin_xcube" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{den}}}\\arctan({a}x^3) + C", $"\\arcsin({a}x^3) + C", $"\\frac{{1}}{{{6 * a}}}\\arcsin({a}x^6) + C", rng);
                    return q;
                });
                // 6. e^(kx) / sqrt(1 - e^(2kx))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 5);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{e^{{{kStr}x}}}}{{\\sqrt{{1 - e^{{{2 * k}x}}}}}}\\,dx";
                    string ans = k == 1 ? "\\arcsin(e^x) + C" : $"\\frac{{1}}{{{k}}}\\arcsin(e^{{{k}x}}) + C";
                    string steps = $"{substLabel}: $u = e^{{{kStr}x}},\\; du = {k}e^{{{kStr}x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_exp_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arctan(e^{{{k}x}}) + C", $"\\arcsin(e^{{{kStr}x}}) + C", $"-\\frac{{1}}{{{k}}}\\sqrt{{1 - e^{{{2 * k}x}}}} + C", rng);
                    return q;
                });
            }
            else
            {
                // 1. cos(kx) / (1 + sin^2(kx))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{\\cos({kStr}x)}}{{1 + \\sin^2({kStr}x)}}\\,dx";
                    string ans = k == 1 ? "\\arctan(\\sin(x)) + C" : $"\\frac{{1}}{{{k}}}\\arctan(\\sin({k}x)) + C";
                    string steps = $"{substLabel}: $u = \\sin({kStr}x),\\; du = {k}\\cos({kStr}x)\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_cos_over_1_sin2" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin(\\sin({kStr}x)) + C", $"\\frac{{1}}{{{k}}}\\arctan(\\sin^2({kStr}x)) + C", $"\\ln(1 + \\sin^2({kStr}x)) + C", rng);
                    return q;
                });
                // 2. 1 / (x * (1 + k^2*(ln(x))^2))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{1}}{{x(1 + {k * k}(\\ln(x))^2)}}\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\arctan({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {k}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_lnx_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arcsin({k}\\ln(x)) + C", $"\\arctan({k}\\ln(x)) + C", $"\\ln(1 + {k * k}(\\ln(x))^2) + C", rng);
                    return q;
                });
                // 3. 1 / (x * sqrt(1 - k^2*(ln(x))^2))
                pool.Add(() =>
                {
                    int k = rng.Next(1, 6);
                    string kStr = k == 1 ? "" : k.ToString();
                    string formula = $"\\int \\frac{{1}}{{x\\sqrt{{1 - {k * k}(\\ln(x))^2}}}}\\,dx";
                    string ans = $"\\frac{{1}}{{{k}}}\\arcsin({k}\\ln(x)) + C";
                    string steps = $"{substLabel}: $u = {k}\\ln(x),\\; du = \\frac{{{k}}}{{x}}\\,dx \\implies {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_lnx_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{k}}}\\arctan({k}\\ln(x)) + C", $"\\arcsin({k}\\ln(x)) + C", $"\\ln|\\ln(x)| + C", rng);
                    return q;
                });
                // 4. sec^2(x) / (1 + a^2*tan^2(x))
                pool.Add(() =>
                {
                    int a = rng.Next(2, 6);
                    string formula = $"\\int \\frac{{\\sec^2(x)}}{{1 + {a * a}\\tan^2(x)}}\\,dx";
                    string ans = $"\\frac{{1}}{{{a}}}\\arctan({a}\\tan(x)) + C";
                    string steps = $"{substLabel}: $u = {a}\\tan(x),\\; du = {a}\\sec^2(x)\\,dx \\implies \\frac{{1}}{{{a}}}\\int \\frac{{1}}{{1 + u^2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_sec2_tan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arctan({a}\\tan(x)) + C", $"\\frac{{1}}{{{a}}}\\arcsin({a}\\tan(x)) + C", $"\\frac{{1}}{{{a * a}}}\\arctan({a}\\tan(x)) + C", rng);
                    return q;
                });
                // 5. 1 / (sqrt(x)*(a^2 + x))
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int a2 = a * a;
                    string formula = a == 1 ? "\\int \\frac{1}{\\sqrt{x}(1 + x)}\\,dx" : $"\\int \\frac{{1}}{{\\sqrt{{x}}({a2} + x)}}\\,dx";
                    string ans = a == 1 ? "2\\arctan(\\sqrt{x}) + C" : (a == 2 ? "\\arctan\\left(\\frac{\\sqrt{x}}{2}\\right) + C" : $"\\frac{{2}}{{{a}}}\\arctan\\left(\\frac{{\\sqrt{{x}}}}{{{a}}}\\right) + C");
                    string steps = $"{substLabel}: $u = \\sqrt{{x}},\\; du = \\frac{{1}}{{2\\sqrt{{x}}}}\\,dx \\implies 2\\int \\frac{{1}}{{{a2} + u^2}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_sqrtx_arctan" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a}}}\\arctan(\\sqrt{{x}}/{a}) + C", $"2\\arcsin(\\sqrt{{x}}/{a}) + C", $"\\ln|{a2} + x| + C", rng);
                    return q;
                });
                // 6. 1 / (sqrt(x)*sqrt(a^2 - x))
                pool.Add(() =>
                {
                    int a = rng.Next(1, 5);
                    int a2 = a * a;
                    string formula = a == 1 ? "\\int \\frac{1}{\\sqrt{x}\\sqrt{1 - x}}\\,dx" : $"\\int \\frac{{1}}{{\\sqrt{{x}}\\sqrt{{{a2} - x}}}}\\,dx";
                    string ans = a == 1 ? "2\\arcsin(\\sqrt{x}) + C" : $"2\\arcsin\\left(\\frac{{\\sqrt{{x}}}}{{{a}}}\\right) + C";
                    string steps = $"{substLabel}: $u = \\sqrt{{x}},\\; du = \\frac{{1}}{{2\\sqrt{{x}}}}\\,dx \\implies 2\\int \\frac{{1}}{{\\sqrt{{{a2} - u^2}}}}\\,du = {ans}$";
                    var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "subs_invtrig_sqrtx_arcsin" };
                    if (type == QuestionType.MultipleChoice)
                        q.Choices = DistractorHelper.BuildChoices(ans, $"\\arcsin(\\sqrt{{x}}/{a}) + C", $"2\\arctan(\\sqrt{{x}}/{a}) + C", $"\\frac{{1}}{{{a}}}\\arcsin(\\sqrt{{x}}) + C", rng);
                    return q;
                });
            }

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateContinuityClassifyProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_diff_squares_cancel" };
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
                    _ => $"Removable at x = -{a}; Infinite at x = {a}"
                };
                string steps = $"{stepLabel}: $(x + {a})$ se cancela $\\implies$ Evitable en $x = -{a}$. Denominador se anula en $x = {a} \\implies$ Infinita.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_diff_squares_cancel_neg" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans,
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = {a}; Infinita en x = -{a}" : $"Removable at x = {a}; Infinite at x = -{a}",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"De salto en x = -{a}" : $"Jump at x = -{a}",
                        rng);
                return q;
            });

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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_factored_cancel" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_pure_removable" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans,
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {c}" : $"Infinite at x = {c}",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"De salto en x = {c}" : $"Jump at x = {c}",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                        rng);
                return q;
            });

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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_jump_abs" };
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "continuity_quadratic_factor" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans,
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Infinita en x = {a}; Evitable en x = -{a}" : $"Infinite at x = {a}; Removable at x = -{a}",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? $"Evitable en x = 0" : $"Removable at x = 0",
                        LocalizationService.CurrentLanguage == AppLanguage.Spanish ? "Continua en todos los reales" : "Continuous everywhere",
                        rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateMotionProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            // 1. Quadratic position
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
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_quad_eval" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"v({t0}) = {v + 2},\\; a({t0}) = {acc}", $"v({t0}) = {v},\\; a({t0}) = {acc + 2}", $"v({t0}) = {acc},\\; a({t0}) = {v}", rng);
                return q;
            });

            // 2. Gravitational vertical motion (max height)
            pool.Add(() =>
            {
                int v0 = rng.Next(2, 8) * 10;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"s(t) = -5t^2 + {v0}t. \\text{{Halle el tiempo }} t \\text{{ en que alcanza la altura máxima.}}",
                    AppLanguage.Portuguese => $"s(t) = -5t^2 + {v0}t. \\text{{Encontre o tempo }} t \\text{{ em que atinge a altura máxima.}}",
                    _ => $"s(t) = -5t^2 + {v0}t. \\text{{Find the time }} t \\text{{ when it reaches maximum height.}}"
                };
                int tMax = v0 / 10;
                string ans = $"t = {tMax}\\text{{ s}}";
                string steps = $"{stepLabel}: $v(t) = s'(t) = -10t + {v0} = 0 \\implies t = \\frac{{{v0}}}{{10}} = {tMax}\\text{{ s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_gravity_max_height" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"t = {tMax + 1}\\text{{ s}}", $"t = {v0 / 5}\\text{{ s}}", $"t = {tMax * 2}\\text{{ s}}", rng);
                return q;
            });

            // 3. Cubic position
            pool.Add(() =>
            {
                int k = rng.Next(2, 6);
                int t0 = rng.Next(1, 5);
                string formula = $"s(t) = t^3 - {3 * k}t^2 + {3 * k * k}t;\\; t = {t0}";
                int v = 3 * t0 * t0 - 6 * k * t0 + 3 * k * k;
                int acc = 6 * t0 - 6 * k;
                string ans = $"v({t0}) = {v},\\; a({t0}) = {acc}";
                string steps = $"{stepLabel}: $v(t) = 3t^2 - {6 * k}t + {3 * k * k},\\; a(t) = 6t - {6 * k}$. Evaluando en $t = {t0}: v({t0}) = {v}, a({t0}) = {acc}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_cubic_eval" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"v({t0}) = {v + 4},\\; a({t0}) = {acc}", $"v({t0}) = {acc},\\; a({t0}) = {v}", $"v({t0}) = 0,\\; a({t0}) = 0", rng);
                return q;
            });

            // 4. Particle at rest
            pool.Add(() =>
            {
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "s(t) = 2t^3 - 9t^2 + 12t. \\text{¿Para qué valores de } t \\text{ la partícula está en reposo?}",
                    AppLanguage.Portuguese => "s(t) = 2t^3 - 9t^2 + 12t. \\text{Para quais valores de } t \\text{ a partícula está em repouso?}",
                    _ => "s(t) = 2t^3 - 9t^2 + 12t. \\text{For what values of } t \\text{ is the particle at rest?}"
                };
                string ans = "t = 1 \\text{ y } t = 2";
                string steps = $"{stepLabel}: Reposo $\\iff v(t) = s'(t) = 6t^2 - 18t + 12 = 6(t^2 - 3t + 2) = 6(t - 1)(t - 2) = 0 \\implies t = 1, 2$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_particle_at_rest" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "t = 0 \\text{ y } t = 3", "t = 1.5", "t = 2 \\text{ y } t = 3", rng);
                return q;
            });

            // 5. Trig harmonic motion
            pool.Add(() =>
            {
                int a = rng.Next(2, 6);
                string formula = $"s(t) = {a}\\cos(t);\\; t = \\pi";
                string ans = $"v(\\pi) = 0,\\; a(\\pi) = {a}";
                string steps = $"{stepLabel}: $v(t) = -{a}\\sin(t) \\implies v(\\pi) = 0.\\; a(t) = -{a}\\cos(t) \\implies a(\\pi) = -{a}(-1) = {a}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_trig_harmonic" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"v(\\pi) = -{a},\\; a(\\pi) = 0", $"v(\\pi) = {a},\\; a(\\pi) = -{a}", "v(\\pi) = 0,\\; a(\\pi) = 0", rng);
                return q;
            });

            // 6. Velocity when acceleration is zero
            pool.Add(() =>
            {
                int a = rng.Next(1, 4);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"s(t) = t^3 - {3 * a}t^2 + 4t. \\text{{Halle la velocidad cuando }} a(t) = 0.",
                    AppLanguage.Portuguese => $"s(t) = t^3 - {3 * a}t^2 + 4t. \\text{{Encontre a velocidade quando }} a(t) = 0.",
                    _ => $"s(t) = t^3 - {3 * a}t^2 + 4t. \\text{{Find the velocity when }} a(t) = 0."
                };
                int vVal = 4 - 3 * a * a;
                string ans = $"v = {vVal}";
                string steps = $"{stepLabel}: $v(t) = 3t^2 - {6 * a}t + 4.\\; a(t) = 6t - {6 * a} = 0 \\implies t = {a}.\\; v({a}) = 3({a})^2 - {6 * a}({a}) + 4 = {vVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "motion_diff_accel_zero_vel" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"v = {vVal + 2}", "v = 0", $"v = {4}", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateRelatedRatesProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            // 1. Circle area
            pool.Add(() =>
            {
                int dr = rng.Next(2, 8);
                int r0 = rng.Next(3, 12);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"El radio de un círculo aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su área cuando r = {r0} cm?",
                    AppLanguage.Portuguese => $"O raio de um círculo aumenta a {dr} cm/s. Com que rapidez sua área aumenta quando r = {r0} cm?",
                    _ => $"The radius of a circle increases at {dr} cm/s. How fast is its area increasing when r = {r0} cm?"
                };
                int ansVal = 2 * r0 * dr;
                string ans = $"{ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}";
                string steps = $"{stepLabel}: $A = \\pi r^2 \\implies \\frac{{dA}}{{dt}} = 2\\pi r\\frac{{dr}}{{dt}} = 2\\pi({r0})({dr}) = {ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_circle_area" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{ansVal / 2}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{r0 * dr}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{ansVal * 2}\\pi\\text{{ cm}}^2\\text{{/s}}", rng);
                return q;
            });

            // 2. Circle circumference
            pool.Add(() =>
            {
                int dr = rng.Next(2, 7);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"El radio de un círculo aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su perímetro o circunferencia?",
                    AppLanguage.Portuguese => $"O raio de um círculo aumenta a {dr} cm/s. Com que rapidez aumenta sua circunferência?",
                    _ => $"The radius of a circle increases at {dr} cm/s. How fast is its circumference increasing?"
                };
                int ansVal = 2 * dr;
                string ans = $"{ansVal}\\pi\\text{{ cm/s}}";
                string steps = $"{stepLabel}: $C = 2\\pi r \\implies \\frac{{dC}}{{dt}} = 2\\pi\\frac{{dr}}{{dt}} = 2\\pi({dr}) = {ansVal}\\pi\\text{{ cm/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_circle_circumference" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{dr}\\pi\\text{{ cm/s}}", $"{4 * dr}\\pi\\text{{ cm/s}}", $"{ansVal}\\text{{ cm/s}}", rng);
                return q;
            });

            // 3. Expanding cube volume
            pool.Add(() =>
            {
                int ds = rng.Next(2, 6);
                int s0 = rng.Next(3, 8);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Las aristas de un cubo aumentan a {ds} cm/s. ¿Con qué rapidez aumenta su volumen cuando la arista mide {s0} cm?",
                    AppLanguage.Portuguese => $"As arestas de um cubo aumentam a {ds} cm/s. Com que rapidez seu volume aumenta quando a aresta mede {s0} cm?",
                    _ => $"The edges of a cube increase at {ds} cm/s. How fast is its volume increasing when an edge is {s0} cm?"
                };
                int ansVal = 3 * s0 * s0 * ds;
                string ans = $"{ansVal}\\text{{ cm}}^3\\text{{/s}}";
                string steps = $"{stepLabel}: $V = s^3 \\implies \\frac{{dV}}{{dt}} = 3s^2\\frac{{ds}}{{dt}} = 3({s0}^2)({ds}) = {ansVal}\\text{{ cm}}^3\\text{{/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_cube_volume" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{s0 * s0 * ds}\\text{{ cm}}^3\\text{{/s}}", $"{ansVal * 2}\\text{{ cm}}^3\\text{{/s}}", $"{6 * s0 * ds}\\text{{ cm}}^3\\text{{/s}}", rng);
                return q;
            });

            // 4. Sphere volume
            pool.Add(() =>
            {
                int dr = rng.Next(1, 6);
                int r0 = rng.Next(2, 8);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"El radio de una esfera aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su volumen cuando r = {r0} cm?",
                    AppLanguage.Portuguese => $"O raio de uma esfera aumenta a {dr} cm/s. Com que rapidez seu volume aumenta quando r = {r0} cm?",
                    _ => $"The radius of a sphere increases at {dr} cm/s. How fast is its volume increasing when r = {r0} cm?"
                };
                int ansVal = 4 * r0 * r0 * dr;
                string ans = $"{ansVal}\\pi\\text{{ cm}}^3\\text{{/s}}";
                string steps = $"{stepLabel}: $V = \\frac{{4}}{{3}}\\pi r^3 \\implies \\frac{{dV}}{{dt}} = 4\\pi r^2\\frac{{dr}}{{dt}} = 4\\pi({r0}^2)({dr}) = {ansVal}\\pi\\text{{ cm}}^3\\text{{/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_sphere_vol" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{ansVal / 2}\\pi\\text{{ cm}}^3\\text{{/s}}", $"{2 * r0 * dr}\\pi\\text{{ cm}}^3\\text{{/s}}", $"{ansVal * 2}\\pi\\text{{ cm}}^3\\text{{/s}}", rng);
                return q;
            });

            // 5. Sphere surface area
            pool.Add(() =>
            {
                int dr = rng.Next(1, 5);
                int r0 = rng.Next(2, 6);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"El radio de una esfera aumenta a {dr} cm/s. ¿Con qué rapidez aumenta su área superficial cuando r = {r0} cm?",
                    AppLanguage.Portuguese => $"O raio de uma esfera aumenta a {dr} cm/s. Com que rapidez sua área superficial aumenta quando r = {r0} cm?",
                    _ => $"The radius of a sphere increases at {dr} cm/s. How fast is its surface area increasing when r = {r0} cm?"
                };
                int ansVal = 8 * r0 * dr;
                string ans = $"{ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}";
                string steps = $"{stepLabel}: $S = 4\\pi r^2 \\implies \\frac{{dS}}{{dt}} = 8\\pi r\\frac{{dr}}{{dt}} = 8\\pi({r0})({dr}) = {ansVal}\\pi\\text{{ cm}}^2\\text{{/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_sphere_area" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{4 * r0 * dr}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{16 * r0 * dr}\\pi\\text{{ cm}}^2\\text{{/s}}", $"{ansVal}\\text{{ cm}}^2\\text{{/s}}", rng);
                return q;
            });

            // 6. Sliding ladder
            pool.Add(() =>
            {
                var triples = new (int x, int y, int L)[]
                {
                    (3, 4, 5), (6, 8, 10), (5, 12, 13), (8, 15, 17)
                };
                var tr = triples[rng.Next(triples.Length)];
                int L = tr.L, x0 = tr.x, y0 = tr.y;
                int dx = rng.Next(1, 5);
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Una escalera de {L} m se desliza por una pared. La base se aleja a {dx} m/s. ¿Con qué rapidez cae la parte superior cuando la base está a {x0} m?",
                    AppLanguage.Portuguese => $"Uma escada de {L} m desliza por uma parede. A base se afasta a {dx} m/s. Com que rapidez o topo desce quando a base está a {x0} m?",
                    _ => $"A {L} m ladder slides down a wall. The base moves away at {dx} m/s. How fast does the top slide down when the base is {x0} m away?"
                };
                double dyVal = Math.Round((double)x0 * dx / y0, 2);
                string ans = $"-{dyVal}\\text{{ m/s}}";
                string steps = $"{stepLabel}: $x^2 + y^2 = {L}^2 \\implies 2x\\frac{{dx}}{{dt}} + 2y\\frac{{dy}}{{dt}} = 0 \\implies \\frac{{dy}}{{dt}} = -\\frac{{x}}{{y}}\\frac{{dx}}{{dt}} = -\\frac{{{x0}}}{{{y0}}}({dx}) = -{dyVal}\\text{{ m/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_sliding_ladder" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{dyVal}\\text{{ m/s}}", $"-{dx}\\text{{ m/s}}", $"-{dyVal * 2}\\text{{ m/s}}", rng);
                return q;
            });

            // 7. Conical tank drainage
            pool.Add(() =>
            {
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Un tanque cónico (con vértice hacia abajo) tiene radio de base igual a la mitad de su altura (r = h/2). Si se drena a 3 m^3/min, ¿con qué rapidez desciende el nivel cuando h = 4 m?",
                    AppLanguage.Portuguese => "Um tanque cônico tem raio igual à metade de sua altura (r = h/2). Se drena a 3 m^3/min, com que rapidez o nível desce quando h = 4 m?",
                    _ => "A conical tank has base radius equal to half its height (r = h/2). If water drains at 3 m^3/min, how fast does water level drop when h = 4 m?"
                };
                // V = 1/3 pi r^2 h = 1/3 pi (h/2)^2 h = pi/12 h^3
                // dV/dt = pi/4 h^2 dh/dt
                // -3 = pi/4 (16) dh/dt = 4 pi dh/dt => dh/dt = -3 / (4 pi)
                string ans = @"-\frac{3}{4\pi}\text{ m/min}";
                string steps = @"V = \frac{\pi}{12}h^3 \implies \frac{dV}{dt} = \frac{\pi}{4}h^2\frac{dh}{dt} \implies -3 = \frac{\pi}{4}(16)\frac{dh}{dt} \implies \frac{dh}{dt} = -\frac{3}{4\pi}\text{ m/min}.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_conical_tank" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, @"-\frac{4}{3\pi}\text{ m/min}", @"-\frac{3}{\pi}\text{ m/min}", @"\frac{3}{4\pi}\text{ m/min}", rng);
                return q;
            });

            // 8. Shadow lengthening from streetlight
            pool.Add(() =>
            {
                int v = rng.Next(1, 4) * 2;
                string formula = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => $"Una persona de 2 m de altura camina a {v} m/s alejándose de un farol de 6 m de altura. ¿Con qué rapidez se alarga su sombra?",
                    AppLanguage.Portuguese => $"Uma pessoa de 2 m de altura caminha a {v} m/s afastando-se de um poste de 6 m. Com que rapidez sua sombra cresce?",
                    _ => $"A person 2 m tall walks at {v} m/s away from a 6 m streetlight. How fast is their shadow lengthening?"
                };
                // (x + s) / 6 = s / 2 => 2x + 2s = 6s => 4s = 2x => s = x/2 => ds/dt = 1/2 dx/dt = v/2
                int ansVal = v / 2;
                string ans = $"{ansVal}\\text{{ m/s}}";
                string steps = $"{stepLabel}: Por triángulos semejantes: $\\frac{{x + s}}{{6}} = \\frac{{s}}{{2}} \\implies 4s = 2x \\implies s = \\frac{{x}}{{2}} \\implies \\frac{{ds}}{{dt}} = \\frac{{1}}{{2}}\\frac{{dx}}{{dt}} = \\frac{{{v}}}{{2}} = {ansVal}\\text{{ m/s}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "related_rates_shadow" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"{v}\\text{{ m/s}}", $"{ansVal * 2}\\text{{ m/s}}", "1\\text{ m/s}", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }

        public static Question GenerateLimitsDerivFormProblem(TopicGeneratorParameters? p, int index, QuestionType type, Random rng)
        {
            string stepLabel = LocalizationService.CurrentLanguage switch
            {
                AppLanguage.Spanish => "Resolución paso a paso",
                AppLanguage.Portuguese => "Resolução passo a paso",
                _ => "Step-by-step resolution"
            };

            var pool = new List<Func<Question>>();

            // 1. Quadratic monomial
            pool.Add(() =>
            {
                int a = rng.Next(1, 20);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{({a} + h)^2 - {a * a}}}{{h}}";
                int ansVal = 2 * a;
                string ans = ansVal.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'(a)$ con $f(x) = x^2$ y $a = {a}$. $f'(x) = 2x \\implies f'({a}) = 2({a}) = {ansVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_quad_monomial" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 2).ToString(), a.ToString(), "0", rng);
                return q;
            });

            // 2. Scaled quadratic
            pool.Add(() =>
            {
                int a = rng.Next(1, 10);
                int k = rng.Next(2, 6);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{{k}({a} + h)^2 - {k * a * a}}}{{h}}";
                int ansVal = 2 * k * a;
                string ans = ansVal.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = {k}x^2$. $f'(x) = {2 * k}x \\implies f'({a}) = {ansVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_quad_scaled" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + k).ToString(), (k * a).ToString(), "0", rng);
                return q;
            });

            // 3. Cubic monomial
            pool.Add(() =>
            {
                int a = rng.Next(1, 10);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{({a} + h)^3 - {a * a * a}}}{{h}}";
                int ansVal = 3 * a * a;
                string ans = ansVal.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = x^3$. $f'(x) = 3x^2 \\implies f'({a}) = 3({a}^2) = {ansVal}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_cubic" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, (ansVal + 3).ToString(), (2 * a * a).ToString(), "0", rng);
                return q;
            });

            // 4. Reciprocal function 1/x
            pool.Add(() =>
            {
                int a = rng.Next(1, 10);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{\\frac{{1}}{{{a} + h}} - \\frac{{1}}{{{a}}}}}{{h}}";
                string ans = $"-\\frac{{1}}{{{a * a}}}";
                string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\frac{{1}}{{x}}$. $f'(x) = -\\frac{{1}}{{x^2}} \\implies f'({a}) = -\\frac{{1}}{{{a * a}}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_reciprocal" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{a * a}}}", $"-\\frac{{1}}{{{a}}}", "0", rng);
                return q;
            });

            // 5. Square root function
            pool.Add(() =>
            {
                int[] perf = { 1, 4, 9, 16, 25, 36, 49 };
                int a = perf[rng.Next(perf.Length)];
                int rootA = (int)Math.Sqrt(a);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{\\sqrt{{{a} + h}} - {rootA}}}{{h}}";
                string ans = $"\\frac{{1}}{{{2 * rootA}}}";
                string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\sqrt{{x}}$. $f'(x) = \\frac{{1}}{{2\\sqrt{{x}}}} \\implies f'({a}) = \\frac{{1}}{{2({rootA})}} = {ans}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_sqrt" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, $"\\frac{{1}}{{{rootA}}}", rootA.ToString(), "0", rng);
                return q;
            });

            // 6. Sin function
            pool.Add(() =>
            {
                int k = rng.Next(2, 8);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{\\sin({k}h)}}{{h}}";
                string ans = k.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = \\sin({k}x)$. $f'(0) = {k}\\cos(0) = {k}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_sin" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "\\infty", rng);
                return q;
            });

            // 7. Exponential function
            pool.Add(() =>
            {
                int k = rng.Next(2, 8);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{e^{{{k}h}} - 1}}{{h}}";
                string ans = k.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = e^{{{k}x}}$. $f'(0) = {k}e^0 = {k}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_exp" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "e", rng);
                return q;
            });

            // 8. Natural logarithm
            pool.Add(() =>
            {
                int a = rng.Next(1, 10);
                string formula = $"\\lim_{{h \\to 0}} \\frac{{\\ln({a} + h) - \\ln({a})}}{{h}}";
                string ans = $"\\frac{{1}}{{{a}}}";
                string steps = $"{stepLabel}: Corresponde a $f'({a})$ con $f(x) = \\ln(x)$. $f'(x) = \\frac{{1}}{{x}} \\implies f'({a}) = \\frac{{1}}{{{a}}}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_ln" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", a.ToString(), rng);
                return q;
            });

            // 9. Tangent function
            pool.Add(() =>
            {
                int k = rng.Next(2, 8);
                string formula = $"\\lim_{{x \\to 0}} \\frac{{\\tan({k}x)}}{{x}}";
                string ans = k.ToString();
                string steps = $"{stepLabel}: Corresponde a $f'(0)$ con $f(x) = \\tan({k}x)$. $f'(x) = {k}\\sec^2({k}x) \\implies f'(0) = {k}(1)^2 = {k}$.";
                var q = new Question { ProblemStatement = formula, FormulaText = formula, Type = type, AnswerText = ans, SolutionSteps = steps, TemplateId = "limit_deriv_tan" };
                if (type == QuestionType.MultipleChoice)
                    q.Choices = DistractorHelper.BuildChoices(ans, "0", "1", "\\infty", rng);
                return q;
            });

            int pick = Math.Abs(index) % pool.Count;
            return pool[pick]();
        }
        #endregion
        #endregion
        #endregion
    }
}
