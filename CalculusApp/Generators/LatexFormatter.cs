using System;
using System.Text.RegularExpressions;
using CalculusApp.Services;

namespace CalculusApp.Generators
{
    public static class LatexFormatter
    {
        public static string FormatSolutionSteps(string steps)
        {
            if (string.IsNullOrWhiteSpace(steps)) return string.Empty;

            // If already contains $ delimiters, format the math parts inside $
            if (steps.Contains('$'))
            {
                var parts = steps.Split('$');
                for (int i = 1; i < parts.Length; i += 2)
                {
                    parts[i] = ToLatex(parts[i]);
                }
                return string.Join("$", parts);
            }

            // If it has lines
            if (steps.Contains('\n') || steps.Contains('\r'))
            {
                var lines = steps.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = FormatSolutionStepsSingleLine(lines[i]);
                }
                return string.Join("\n", lines);
            }

            return FormatSolutionStepsSingleLine(steps);
        }

        private static string FormatSolutionStepsSingleLine(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();

            // If it's a rule formula like "d/dx [c*x^n] = c*n*x^(n-1): 4*5x^4 + -5*2x^1 = ans"
            if (s.StartsWith("d/dx") || s.StartsWith(@"\frac{d}{dx}"))
            {
                string ruleName = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Regla de la potencia",
                    AppLanguage.Portuguese => "Regra da potência",
                    _ => "Power rule"
                };

                int colonIdx = s.IndexOf(':');
                if (colonIdx > 0)
                {
                    string ruleFormula = s.Substring(0, colonIdx).Trim();
                    string calc = s.Substring(colonIdx + 1).Trim();
                    return $"{ruleName}: ${ToLatex(ruleFormula)} \\implies {ToLatex(calc)}$";
                }
            }

            // Check "Label: math"
            if (s.Contains(": ") && !s.StartsWith(@"\"))
            {
                int colonIdx = s.IndexOf(": ");
                string label = s.Substring(0, colonIdx).Trim();
                string math = s.Substring(colonIdx + 2).Trim();

                // If label is purely text
                if (Regex.IsMatch(label, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\(\),/\-]+$"))
                {
                    if (math.Contains(". ") && !Regex.IsMatch(math, @"\d\.\s*\d"))
                    {
                        var subs = math.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < subs.Length; i++)
                        {
                            string sub = subs[i].Trim();
                            if (sub.Contains('=') || sub.Contains("=>") || sub.Contains(@"\int") || sub.Contains(@"\lim"))
                                subs[i] = "$" + ToLatex(sub) + "$";
                        }
                        return $"{label}: {string.Join(". ", subs)}" + (math.EndsWith('.') ? "." : "");
                    }
                    return $"{label}: ${ToLatex(math)}$";
                }
            }

            // If it contains sentences separated by ". "
            if (s.Contains(". ") && !Regex.IsMatch(s, @"\d\.\s*\d"))
            {
                var parts = s.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim();
                    if (p.Contains('=') || p.Contains("=>") || p.Contains(@"\int") || p.Contains(@"\lim"))
                    {
                        parts[i] = "$" + ToLatex(p) + "$";
                    }
                    else
                    {
                        parts[i] = p;
                    }
                }
                return string.Join(". ", parts) + (s.EndsWith('.') ? "." : "");
            }

            // If it contains a formula with =, =>
            if (s.Contains('=') || s.Contains("=>"))
            {
                return $"${ToLatex(s)}$";
            }

            return s;
        }

        public static string ToLatex(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // If input already contains $ delimiters, format ONLY the math parts inside $
            if (input.Contains('$'))
            {
                var parts = input.Split('$');
                for (int i = 1; i < parts.Length; i += 2)
                {
                    parts[i] = ToLatex(parts[i]);
                }
                return string.Join("$", parts);
            }

            // Preserve multiline strings by formatting each line individually
            if (input.Contains('\n') || input.Contains('\r'))
            {
                var lines = input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = ToLatexSingleLine(lines[i]);
                }
                return string.Join("\n", lines);
            }

            return ToLatexSingleLine(input);
        }

        private static string ToLatexSingleLine(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();

            // 0. Immediate repair of any existing corrupted tokens
            s = Regex.Replace(s, @"\\infty(?:ty)+", @"\infty");

            // Normalize excessive inline spaces
            s = Regex.Replace(s, @"[ \t]+", " ");

            // Semicolons in math mode (e.g. y = ...; [0, 3])
            s = s.Replace("; ", @";\; ");

            // Normalize quad spacing for WpfMath
            s = s.Replace(@"\quad", @"\;\;\;");
            s = s.Replace(@"\qquad", @"\;\;\;\;\;\;");

            // Normalize implication arrows
            s = s.Replace(@"\implies", @"\Rightarrow");
            s = s.Replace("=>", @"\Rightarrow");

            // Translate legacy English extrema terms if present and user language is Spanish or Portuguese
            if (LocalizationService.CurrentLanguage == AppLanguage.Spanish)
            {
                s = Regex.Replace(s, @"\bRelative\s*max\b", "Máximo relativo", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bRelative\s*min\b", "Mínimo relativo", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bAbsolute\s*max\b", "Máximo absoluto", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bAbsolute\s*min\b", "Mínimo absoluto", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bCritical\s*points?\b", "Puntos críticos", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bNo\s*solution\b", "Sin solución", RegexOptions.IgnoreCase);
            }
            else if (LocalizationService.CurrentLanguage == AppLanguage.Portuguese)
            {
                s = Regex.Replace(s, @"\bRelative\s*max\b", "Máximo relativo", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bRelative\s*min\b", "Mínimo relativo", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bAbsolute\s*max\b", "Máximo absoluto", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bAbsolute\s*min\b", "Mínimo absoluto", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bCritical\s*points?\b", "Pontos críticos", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\bNo\s*solution\b", "Sem solução", RegexOptions.IgnoreCase);
            }

            // 1. Differential operators: dy/dx, d/dx, d2y/dx2 (guard against existing \frac)
            if (!s.Contains(@"\frac{dy}{dx}"))
                s = Regex.Replace(s, @"(?<!\\frac\{)\bdy/dx\b", @"\frac{dy}{dx}");
            if (!s.Contains(@"\frac{d}{dx}"))
                s = Regex.Replace(s, @"(?<!\\frac\{)\bd/dx\b", @"\frac{d}{dx}");
            if (!s.Contains(@"\frac{d^2y}{dx^2}"))
                s = Regex.Replace(s, @"(?<!\\frac\{)\bd\^2y/dx\^2\b", @"\frac{d^2y}{dx^2}");

            // 2. Limits: only match raw "lim [x -> a]" or "lim x->a" that does NOT already start with \lim
            s = Regex.Replace(s, @"(?<!\\)\blim\s*(?:\[|\{)?\s*([a-zA-Z]+)\s*(?:->|\\to)\s*([^\]}]+?)(?:\]|\})", m =>
            {
                string var = m.Groups[1].Value.Trim();
                string target = m.Groups[2].Value.Trim();
                if (!target.Contains(@"\infty"))
                {
                    target = target.Replace("+inf", @"\infty").Replace("-inf", @"-\infty");
                    target = Regex.Replace(target, @"\binf\b", @"\infty");
                }
                return $@"\lim_{{{var} \to {target}}} ";
            });
            // Clean any double \lim or corrupted \inftyty
            s = Regex.Replace(s, @"\\infty(?:ty)+", @"\infty");

            // 3. Integrals and limits of integrals: int_a^b, int [a to b], int ... dx
            s = Regex.Replace(s, @"(?<!\\)\bint\s*\[([^\]]+)\s+to\s+([^\]]+)\]", @"\int_{$1}^{$2} ");
            s = Regex.Replace(s, @"(?<!\\)\bint(?=[_\s\^\[]|$)", @"\int");
            s = Regex.Replace(s, @"\\int_\(([^)]+)\)", @"\int_{$1}");
            s = Regex.Replace(s, @"\\int\^\(([^)]+)\)", @"\int^{$1}");
            s = Regex.Replace(s, @"\\int_([a-zA-Z0-9\-]+)\^([a-zA-Z0-9\-]+)(?![a-zA-Z0-9])", @"\int_{$1}^{$2} ");
            s = Regex.Replace(s, @"\\int_([a-zA-Z0-9\-]+)\^\{([^}]+)\}", @"\int_{$1}^{$2} ");
            s = Regex.Replace(s, @"\\int_\{([^}]+)\}\^([a-zA-Z0-9\-]+)(?![a-zA-Z0-9])", @"\int_{$1}^{$2} ");
            s = Regex.Replace(s, @"\\int(?=[a-zA-Z0-9\(\\])", @"\int ");
            s = Regex.Replace(s, @"\s+(d[xtuyz])\b", @"\,$1");

            // Summation notation: Sum [i=1 to n]
            s = Regex.Replace(s, @"(?<!\\)\b(?:Sum|sum)\s*\[([^=]+)=([^\s\]]+)\s+to\s+([^\]]+)\]", @"\sum_{$1=$2}^{$3} ");

            // 4. Normalization of existing \left and \right delimiters (prevents \left\left\left)
            s = Regex.Replace(s, @"(?:\\left\s*)+\[", "[");
            s = Regex.Replace(s, @"(?:\\right\s*)+\]", "]");
            s = Regex.Replace(s, @"(?:\\left\s*)+", @"\left");
            s = Regex.Replace(s, @"(?:\\right\s*)+", @"\right");

            // Evaluation bar: [F(x)]_a^b or \left[ F(x) \right]_a^b
            s = Regex.Replace(s, @"\[([^\[\]]+)\]_\{?([^{}\^\s]+)\}?\^\{?([^{}\^\s]+)\}?", @"\left[ $1 \right]_{$2}^{$3}");
            s = Regex.Replace(s, @"\[([^\[\]]+)\]_\{?([^{}\^\s]+)\}?", @"\left[ $1 \right]_{$2}");

            // 5. Fractions:
            // -x^2/2 -> -\frac{x^2}{2}
            s = Regex.Replace(s, @"-\s*([a-zA-Z0-9\^]+)\s*/\s*([0-9]+)\b", m => $@"-\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");
            // x^2/2 -> \frac{x^2}{2}
            s = Regex.Replace(s, @"(?<![a-zA-Z0-9\^])([a-zA-Z0-9\^]+)\s*/\s*([0-9]+)\b", m => $@"\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");
            // (num) / (den) -> \frac{num}{den}
            s = Regex.Replace(s, @"\(([^\(\)]+)\)\s*/\s*\(([^\(\)]+)\)", m => $@"\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");
            // (num) / den -> \frac{num}{den}
            s = Regex.Replace(s, @"\(([^\(\)]+)\)\s*/\s*([a-zA-Z0-9\^]+)", m => $@"\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");
            // num / (den) -> \frac{num}{den}
            s = Regex.Replace(s, @"([a-zA-Z0-9\^]+)\s*/\s*\(([^\(\)]+)\)", m => $@"\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");
            // pure numeric fractions: 1/2, 3/4, 128/3
            s = Regex.Replace(s, @"\b(\d+)\s*/\s*(\d+)\b", m => $@"\frac{{{m.Groups[1].Value}}}{{{m.Groups[2].Value}}}");

            // 6. Radicals: sqrt(inner) -> \sqrt{inner}
            s = Regex.Replace(s, @"(?<!\\)sqrt\(([^)]+)\)", m => $@"\sqrt{{{m.Groups[1].Value}}}");
            s = Regex.Replace(s, @"(?<!\\)sqrt\[(\d+)\]\(([^)]+)\)", m => $@"\sqrt[{m.Groups[1].Value}]{{{m.Groups[2].Value}}}");

            // 7. Trig and standard math functions (ensure Spanish word 'sin' isn't converted)
            s = Regex.Replace(s, @"(?<!\\)\b(cos|tan|sec|csc|cot|ln|log|arcsin|arccos|arctan)\b(?=\s*[\(\dxyzabth\^])|(?<!\\)\bsin\b(?=\s*[\(\dxyzt\^])", m => "\\" + m.Value);

            // 8. Math symbols: pi, inf, *, !=, <=, >=
            s = Regex.Replace(s, @"(?<!\\)\bpi\b", @"\pi");
            s = Regex.Replace(s, @"(?<!\\)\binf\b", @"\infty");
            s = s.Replace(" * ", @" \cdot ");
            s = s.Replace("*", @" \cdot ");
            s = s.Replace("!=", @" \ne ");
            s = s.Replace("<=", @" \le ");
            s = s.Replace(">=", @" \ge ");

            // Format "on [a, b]" or "at x = ..." into clean mathematical delimiter ";\; [a, b]"
            s = Regex.Replace(s, @"\s+(?:on|en|em)\s+\[", @";\; [", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s+(?:at|en)\s+([a-zA-Z]\s*=)", @";\; $1", RegexOptions.IgnoreCase);

            // 8b. Textual phrases and natural language formatting
            if (s.Contains(';') && !s.Contains(@"\begin"))
            {
                var parts = s.Split(';');
                for (int p = 0; p < parts.Length; p++)
                {
                    parts[p] = FormatTextAndMathSegment(parts[p].Trim());
                }
                s = string.Join(@";\; ", parts);
            }
            else
            {
                s = FormatTextAndMathSegment(s);
            }

            // Clean coordinate spacing e.g. (-1, -6) -> (-1, -6)
            s = Regex.Replace(s, @"(?<=\d|\))\s*,\s*(?=[-\d\(])", ", ");

            // 8c. Algebraic polish: fix signs, 1x, double signs
            s = s.Replace("- -", "+ ");
            s = s.Replace("+ -", "- ");
            s = s.Replace("--", "+");
            s = s.Replace("+-", "-");
            s = Regex.Replace(s, @"(?<![0-9])1([a-zA-Z])", "$1");
            s = Regex.Replace(s, @"(?<![0-9])-1([a-zA-Z])", "-$1");

            // 9. Fix exponents: make sure variables with unbraced exponents have clean braces e.g. x^2 -> x^{2}
            // BUT do not touch existing braces x^{2} or upper limits of integrals \int_{0}^{1}
            s = Regex.Replace(s, @"(?<=[a-zA-Z0-9\)])\^([a-zA-Z0-9\+\-]+)", m =>
            {
                string val = m.Groups[1].Value;
                return $"^{{{val}}}";
            });
            s = Regex.Replace(s, @"\^\(([^)]+)\)", @"^{$1}");

            // Clean single powers ONLY on algebraic variables or closing parens: x^1 -> x, (x+1)^1 -> (x+1)
            // NEVER on integrals, sum, or evaluation bars! Do not strip exponents with expressions like e^{1 - 1}
            s = Regex.Replace(s, @"(?<=[a-zA-Z\)])(?:\^1(?![0-9a-zA-Z])|\^\{1\})", "");

            // 10. Clean duplicate slashes
            s = s.Replace(@"\\frac", @"\frac");
            s = s.Replace(@"\\lim", @"\lim");
            s = s.Replace(@"\\sum", @"\sum");
            s = s.Replace(@"\\int", @"\int");
            s = s.Replace(@"\\sqrt", @"\sqrt");
            s = s.Replace(@"\\sin", @"\sin");
            s = s.Replace(@"\\cos", @"\cos");
            s = s.Replace(@"\\tan", @"\tan");
            s = s.Replace(@"\\sec", @"\sec");
            s = s.Replace(@"\\csc", @"\csc");
            s = s.Replace(@"\\cot", @"\cot");
            s = s.Replace(@"\\ln", @"\ln");
            s = s.Replace(@"\\cdot", @"\cdot");
            s = s.Replace(@"\\pi", @"\pi");
            s = s.Replace(@"\\infty", @"\infty");

            // 11. Final strict sanitization for WpfMath safety
            s = Regex.Replace(s, @"\\infty(?:ty)+", @"\infty");
            s = Regex.Replace(s, @"(?:\\left\s*)+([\[\(\{\.\|])", @"\left$1");
            s = Regex.Replace(s, @"(?:\\right\s*)+([\]\)\{\.\|])", @"\right$1");
            s = Regex.Replace(s, @"\\left(?![\[\(\{\.\|])", "");
            s = Regex.Replace(s, @"\\right(?![\]\)\}\.\|])", "");

            return s;
        }

        private static string FormatTextAndMathSegment(string seg)
        {
            if (string.IsNullOrWhiteSpace(seg)) return string.Empty;
            if (seg.StartsWith(@"\text{") && seg.EndsWith("}")) return seg;

            // Check if segment is a pure natural text phrase (letters, spaces, punctuation, no math operators)
            bool hasMathOperators = seg.Contains('=') || seg.Contains('^') || seg.Contains('/') ||
                                    seg.Contains('+') || (seg.Contains('-') && !seg.Contains(" - ")) || seg.Contains('\\') ||
                                    seg.Contains('<') || seg.Contains('>') || seg.Contains('*');

            if (!hasMathOperators)
            {
                // Pure text e.g. "No asymptotes exist", "No existen asíntotas", "None", "Ninguno"
                return $@"\text{{{seg}}}";
            }

            // Check if segment has a text label followed by a colon, e.g. "Vertical asymptotes: x = -2, 2"
            if (seg.Contains(':'))
            {
                int colonIdx = seg.IndexOf(':');
                string label = seg.Substring(0, colonIdx).Trim();
                string math = seg.Substring(colonIdx + 1).Trim();

                // If label is purely words/spaces
                if (Regex.IsMatch(label, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\(\),-]+$"))
                {
                    math = Regex.Replace(math, @"\bonly\b", @"\text{only}");
                    math = Regex.Replace(math, @"\bsolo\b", @"\text{solo}");
                    math = Regex.Replace(math, @"\bsolamente\b", @"\text{solamente}");
                    return $@"\text{{{label}:}}\; {math}";
                }
            }

            // Fallback for known phrases within mixed text
            string[] knownPhrases = new[]
            {
                "No asymptotes exist",
                "No existen asíntotas",
                "Não existem assíntotas",
                "Vertical asymptotes:",
                "Vertical asymptote:",
                "Horizontal asymptotes:",
                "Horizontal asymptote:",
                "Slant asymptote:",
                "Asíntotas verticales:",
                "Asíntota vertical:",
                "Asíntotas horizontales:",
                "Asíntota horizontal:",
                "Asíntota oblicua:",
                "Absolute minima:",
                "Absolute minimum:",
                "Absolute maxima:",
                "Absolute maximum:",
                "Mínimos absolutos:",
                "Mínimo absoluto:",
                "Máximos absolutos:",
                "Máximo absoluto:",
                "Relative minima:",
                "Relative minimum:",
                "Relative maxima:",
                "Relative maximum:",
                "Puntos críticos:",
                "Punto crítico:",
                "Puntos de inflexión:",
                "Punto de inflexión:",
                "Critical points:",
                "Inflection points:",
                "Does not exist (DNE)",
                "Does not exist",
                "No existe",
                "Continuous everywhere",
                "Continua en todas partes",
                "Direct substitution",
                "Factor numerator",
                "Approximate",
                "Aproxime",
                "Aproximadamente",
                "Trapezoidal Rule",
                "Trapezoidal rule",
                "Regla del trapecio",
                "Regra do trapézio",
                "Left endpoint",
                "Right endpoint",
                "Midpoint rule",
                "Extremo izquierdo",
                "Extremo derecho",
                "Punto medio",
                "Extremo esquerdo",
                "Extremo direito",
                "Ponto médio",
                "Riemann sums",
                "Riemann sum",
                "Sumas de Riemann",
                "Suma de Riemann",
                "Somas de Riemann",
                "Soma de Riemann",
                "LRAM",
                "RRAM",
                "MRAM",
                "bacteria",
                "Displacement",
                "Desplazamiento",
                "Deslocamento",
                "Total Distance",
                "Total distance",
                "Distancia total",
                "Distância total",
                "Yes, it is a valid solution",
                "Removable at",
                "Vertical asymptote at",
                "Origin",
                "Origen",
                "Origem",
                "Area",
                "Área",
                "Volume",
                "Volumen",
                "None",
                "Ninguno"
            };

            foreach (var kp in knownPhrases)
            {
                if (seg.Contains(kp, StringComparison.OrdinalIgnoreCase) && !seg.Contains($@"\text{{{kp}"))
                {
                    seg = Regex.Replace(seg, Regex.Escape(kp), $@"\text{{{kp}}}\; ", RegexOptions.IgnoreCase);
                }
            }

            return seg;
        }
    }
}
