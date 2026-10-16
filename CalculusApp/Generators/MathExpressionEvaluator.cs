using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CalculusApp.Generators
{
    public static class MathExpressionEvaluator
    {
        public static string ExtractFormula(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string s = input.Trim();

            // 1. If it has \lim_{x \to ...} or lim [x -> ...]
            var matchLim = Regex.Match(s, @"(?:\\lim_\{[^}]+\}|lim\s*\[[^\]]+\])\s*(.*)", RegexOptions.IgnoreCase);
            if (matchLim.Success)
            {
                s = matchLim.Groups[1].Value.Trim();
            }

            // 2. If it starts with f(x) = or y = or g(x) =
            var matchEq = Regex.Match(s, @"^[a-zA-Z](?:\([a-zA-Z]\))?\s*=\s*(.*)");
            if (matchEq.Success)
            {
                s = matchEq.Groups[1].Value.Trim();
            }

            // 3. If it has \int ... dx
            var matchInt = Regex.Match(s, @"\\int\s*(.*?)\s*(?:\\,|\\;|\\quad)?d[a-zA-Z]", RegexOptions.IgnoreCase);
            if (matchInt.Success)
            {
                s = matchInt.Groups[1].Value.Trim();
            }

            // Strip outer parentheses if balanced
            s = UnwrapOuterParens(s);
            return s;
        }

        private static string UnwrapOuterParens(string s)
        {
            s = s.Trim();
            while (s.Length >= 2 && s.StartsWith("(") && s.EndsWith(")"))
            {
                int depth = 0;
                bool balanced = true;
                for (int i = 0; i < s.Length - 1; i++)
                {
                    if (s[i] == '(') depth++;
                    else if (s[i] == ')') depth--;
                    if (depth == 0)
                    {
                        balanced = false;
                        break;
                    }
                }
                if (balanced)
                {
                    s = s.Substring(1, s.Length - 2).Trim();
                }
                else
                {
                    break;
                }
            }
            return s;
        }

        public static Func<double, double>? Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return null;

            string formula = ExtractFormula(expr);
            if (string.IsNullOrWhiteSpace(formula)) return null;

            string preprocessed = Preprocess(formula);
            try
            {
                var tokens = Tokenize(preprocessed);
                if (tokens.Count == 0) return null;

                var parser = new Parser(tokens);
                var ast = parser.ParseExpression();
                return x =>
                {
                    try
                    {
                        double val = ast.Evaluate(x);
                        return val;
                    }
                    catch
                    {
                        return double.NaN;
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        public static string Preprocess(string s)
        {
            // Normalize LaTeX fractions: \frac{num}{den} -> ((num)/(den))
            s = ReplaceFractions(s);

            // Replace LaTeX roots: \sqrt{arg} -> sqrt(arg), \sqrt[n]{arg}
            s = ReplaceRoots(s);

            // Replace LaTeX functions
            s = Regex.Replace(s, @"\\sin\b", "sin", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\cos\b", "cos", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\tan\b", "tan", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\sec\b", "sec", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\csc\b", "csc", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\cot\b", "cot", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\ln\b", "ln", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\log\b", "log", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\exp\b", "exp", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\\pi\b", "3.141592653589793", RegexOptions.IgnoreCase);

            // Absolute values: \left| ... \right| or | ... |
            s = Regex.Replace(s, @"\\left\|", "abs(");
            s = Regex.Replace(s, @"\\right\|", ")");
            s = ReplaceAbsoluteBars(s);

            // Replace { and } with ( and )
            s = s.Replace('{', '(').Replace('}', ')');

            // Replace \cdot with *
            s = s.Replace(@"\cdot", "*").Replace(@"\times", "*");

            // Clean other LaTeX formatting
            s = Regex.Replace(s, @"\\[a-zA-Z]+", ""); // remove remaining backslash commands

            // Exponential e^... -> exp(...)
            s = Regex.Replace(s, @"\be\s*\^\s*\(([^\)]+)\)", "exp($1)");
            s = Regex.Replace(s, @"\be\s*\^\s*x\b", "exp(x)");
            s = Regex.Replace(s, @"\be\s*\^\s*([0-9\.]+)", "exp($1)");

            // Add implicit multiplication:
            // 1. Number followed by x or variable: 2x -> 2*x
            s = Regex.Replace(s, @"(\d+)\s*([a-zA-Z])", "$1*$2");
            // 2. Number followed by open paren: 2( -> 2*(
            s = Regex.Replace(s, @"(\d+)\s*\(", "$1*(");
            // 3. Close paren followed by open paren: )( -> )*(
            s = Regex.Replace(s, @"\)\s*\(", ")*(");
            // 4. Close paren followed by variable: )x -> )*x
            s = Regex.Replace(s, @"\)\s*([a-zA-Z])", ")*$1");
            // 5. Close paren followed by number: )2 -> )*2
            s = Regex.Replace(s, @"\)\s*(\d+)", ")*$1");
            // 6. Variable x followed by function: x sin(x) -> x*sin(x)
            s = Regex.Replace(s, @"\bx\s*(sin|cos|tan|sec|csc|cot|ln|log|exp|sqrt|abs)\b", "x*$1");
            // 7. Variable x followed by open paren: x( -> x*(
            s = Regex.Replace(s, @"\bx\s*\(", "x*(");

            return s;
        }

        private static string ReplaceFractions(string s)
        {
            int fracIdx;
            while ((fracIdx = s.IndexOf(@"\frac")) >= 0)
            {
                int numStart = s.IndexOf('{', fracIdx);
                if (numStart < 0) break;
                int numEnd = FindMatchingBrace(s, numStart);
                if (numEnd < 0) break;

                int denStart = s.IndexOf('{', numEnd + 1);
                if (denStart < 0) break;
                int denEnd = FindMatchingBrace(s, denStart);
                if (denEnd < 0) break;

                string num = s.Substring(numStart + 1, numEnd - numStart - 1);
                string den = s.Substring(denStart + 1, denEnd - denStart - 1);

                string replacement = $"(({num})/({den}))";
                s = s.Substring(0, fracIdx) + replacement + s.Substring(denEnd + 1);
            }
            return s;
        }

        private static string ReplaceRoots(string s)
        {
            int sqrtIdx;
            while ((sqrtIdx = s.IndexOf(@"\sqrt")) >= 0)
            {
                int argStart = s.IndexOf('{', sqrtIdx);
                if (argStart < 0) break;
                int argEnd = FindMatchingBrace(s, argStart);
                if (argEnd < 0) break;

                string arg = s.Substring(argStart + 1, argEnd - argStart - 1);
                string replacement = $"sqrt({arg})";
                s = s.Substring(0, sqrtIdx) + replacement + s.Substring(argEnd + 1);
            }
            return s;
        }

        private static string ReplaceAbsoluteBars(string s)
        {
            var sb = new StringBuilder();
            bool inAbs = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '|')
                {
                    if (!inAbs)
                    {
                        sb.Append("abs(");
                        inAbs = true;
                    }
                    else
                    {
                        sb.Append(")");
                        inAbs = false;
                    }
                }
                else
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }

        private static int FindMatchingBrace(string s, int openIdx)
        {
            int depth = 0;
            for (int i = openIdx; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        #region Tokenizer and AST
        private enum TokenType
        {
            Number,
            Variable,
            Plus,
            Minus,
            Multiply,
            Divide,
            Power,
            OpenParen,
            CloseParen,
            Function
        }

        private class Token
        {
            public TokenType Type { get; set; }
            public double NumberValue { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || (c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
                {
                    int start = i;
                    while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                    {
                        i++;
                    }
                    string numStr = text.Substring(start, i - start);
                    if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        tokens.Add(new Token { Type = TokenType.Number, NumberValue = val, Text = numStr });
                    }
                    continue;
                }

                if (c == '+') { tokens.Add(new Token { Type = TokenType.Plus, Text = "+" }); i++; continue; }
                if (c == '-') { tokens.Add(new Token { Type = TokenType.Minus, Text = "-" }); i++; continue; }
                if (c == '*') { tokens.Add(new Token { Type = TokenType.Multiply, Text = "*" }); i++; continue; }
                if (c == '/') { tokens.Add(new Token { Type = TokenType.Divide, Text = "/" }); i++; continue; }
                if (c == '^') { tokens.Add(new Token { Type = TokenType.Power, Text = "^" }); i++; continue; }
                if (c == '(') { tokens.Add(new Token { Type = TokenType.OpenParen, Text = "(" }); i++; continue; }
                if (c == ')') { tokens.Add(new Token { Type = TokenType.CloseParen, Text = ")" }); i++; continue; }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < text.Length && char.IsLetter(text[i]))
                    {
                        i++;
                    }
                    string word = text.Substring(start, i - start).ToLowerInvariant();
                    if (word == "x" || word == "t")
                    {
                        tokens.Add(new Token { Type = TokenType.Variable, Text = "x" });
                    }
                    else if (word == "pi")
                    {
                        tokens.Add(new Token { Type = TokenType.Number, NumberValue = Math.PI, Text = "pi" });
                    }
                    else if (word == "e")
                    {
                        tokens.Add(new Token { Type = TokenType.Number, NumberValue = Math.E, Text = "e" });
                    }
                    else
                    {
                        tokens.Add(new Token { Type = TokenType.Function, Text = word });
                    }
                    continue;
                }

                i++;
            }
            return tokens;
        }

        private interface INode
        {
            double Evaluate(double x);
        }

        private class NumberNode : INode
        {
            private readonly double _val;
            public NumberNode(double val) => _val = val;
            public double Evaluate(double x) => _val;
        }

        private class VariableNode : INode
        {
            public double Evaluate(double x) => x;
        }

        private class BinaryOpNode : INode
        {
            private readonly INode _left;
            private readonly INode _right;
            private readonly TokenType _op;

            public BinaryOpNode(INode left, TokenType op, INode right)
            {
                _left = left;
                _op = op;
                _right = right;
            }

            public double Evaluate(double x)
            {
                double l = _left.Evaluate(x);
                double r = _right.Evaluate(x);

                if (double.IsNaN(l) || double.IsNaN(r)) return double.NaN;

                return _op switch
                {
                    TokenType.Plus => l + r,
                    TokenType.Minus => l - r,
                    TokenType.Multiply => l * r,
                    TokenType.Divide => Math.Abs(r) < 1e-12 ? double.NaN : l / r,
                    TokenType.Power => Math.Pow(l, r),
                    _ => double.NaN
                };
            }
        }

        private class UnaryOpNode : INode
        {
            private readonly INode _inner;
            private readonly TokenType _op;
            public UnaryOpNode(TokenType op, INode inner)
            {
                _op = op;
                _inner = inner;
            }
            public double Evaluate(double x)
            {
                double val = _inner.Evaluate(x);
                return _op == TokenType.Minus ? -val : val;
            }
        }

        private class FunctionNode : INode
        {
            private readonly string _fn;
            private readonly INode _arg;

            public FunctionNode(string fn, INode arg)
            {
                _fn = fn.ToLowerInvariant();
                _arg = arg;
            }

            public double Evaluate(double x)
            {
                double val = _arg.Evaluate(x);
                if (double.IsNaN(val)) return double.NaN;

                return _fn switch
                {
                    "sin" => Math.Sin(val),
                    "cos" => Math.Cos(val),
                    "tan" => Math.Abs(Math.Cos(val)) < 1e-10 ? double.NaN : Math.Tan(val),
                    "sec" => Math.Abs(Math.Cos(val)) < 1e-10 ? double.NaN : 1.0 / Math.Cos(val),
                    "csc" => Math.Abs(Math.Sin(val)) < 1e-10 ? double.NaN : 1.0 / Math.Sin(val),
                    "cot" => Math.Abs(Math.Sin(val)) < 1e-10 ? double.NaN : Math.Cos(val) / Math.Sin(val),
                    "sqrt" => val < 0 ? double.NaN : Math.Sqrt(val),
                    "abs" => Math.Abs(val),
                    "ln" or "log" => val <= 0 ? double.NaN : Math.Log(val),
                    "exp" => Math.Exp(val),
                    _ => val
                };
            }
        }

        private class Parser
        {
            private readonly List<Token> _tokens;
            private int _pos;

            public Parser(List<Token> tokens)
            {
                _tokens = tokens;
                _pos = 0;
            }

            private Token Current => _pos < _tokens.Count ? _tokens[_pos] : new Token { Type = TokenType.CloseParen };

            private Token Advance()
            {
                var cur = Current;
                _pos++;
                return cur;
            }

            public INode ParseExpression()
            {
                var left = ParseTerm();
                while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
                {
                    var op = Advance().Type;
                    var right = ParseTerm();
                    left = new BinaryOpNode(left, op, right);
                }
                return left;
            }

            private INode ParseTerm()
            {
                var left = ParseUnary();
                while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide)
                {
                    var op = Advance().Type;
                    var right = ParseUnary();
                    left = new BinaryOpNode(left, op, right);
                }
                return left;
            }

            private INode ParseUnary()
            {
                if (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
                {
                    var op = Advance().Type;
                    var inner = ParseUnary();
                    return new UnaryOpNode(op, inner);
                }
                return ParsePower();
            }

            private INode ParsePower()
            {
                var left = ParseFactor();
                if (Current.Type == TokenType.Power)
                {
                    Advance();
                    var right = ParseUnary();
                    return new BinaryOpNode(left, TokenType.Power, right);
                }
                return left;
            }

            private INode ParseFactor()
            {
                var token = Current;
                if (token.Type == TokenType.Number)
                {
                    Advance();
                    return new NumberNode(token.NumberValue);
                }
                if (token.Type == TokenType.Variable)
                {
                    Advance();
                    return new VariableNode();
                }
                if (token.Type == TokenType.Function)
                {
                    string fn = Advance().Text;
                    if (Current.Type == TokenType.OpenParen)
                    {
                        Advance();
                        var arg = ParseExpression();
                        if (Current.Type == TokenType.CloseParen) Advance();
                        return new FunctionNode(fn, arg);
                    }
                    else
                    {
                        var arg = ParseUnary();
                        return new FunctionNode(fn, arg);
                    }
                }
                if (token.Type == TokenType.OpenParen)
                {
                    Advance();
                    var expr = ParseExpression();
                    if (Current.Type == TokenType.CloseParen) Advance();
                    return expr;
                }

                // Fallback
                Advance();
                return new NumberNode(0);
            }
        }
        #endregion
    }
}
