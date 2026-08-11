using System;
using System.Collections.Generic;
using System.Globalization;

namespace QS3D.Core.Formulas
{
    public sealed class ExpressionEvaluator
    {
        private const int MaxExpressionLength = 4096;

        public double Evaluate(string expression, IReadOnlyDictionary<string, double>? variables = null)
        {
            ValidateExpression(expression);
            return new Parser(expression, NormalizeVariables(variables)).Parse();
        }

        public IReadOnlyCollection<string> GetReferencedVariables(string expression)
        {
            ValidateExpression(expression);
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            while (index < expression.Length)
            {
                var current = expression[index];
                if (char.IsWhiteSpace(current)) { index++; continue; }
                if (char.IsDigit(current) || current == '.')
                {
                    SkipNumberToken(expression, ref index);
                    continue;
                }
                if (char.IsLetter(current) || current == '_')
                {
                    var start = index++;
                    while (index < expression.Length)
                    {
                        var c = expression[index];
                        if (char.IsLetterOrDigit(c) || c == '_' || c == '.') index++;
                        else break;
                    }
                    var name = expression.Substring(start, index - start);
                    var probe = index;
                    while (probe < expression.Length && char.IsWhiteSpace(expression[probe])) probe++;
                    if (probe >= expression.Length || expression[probe] != '(')
                        if (seen.Add(name)) result.Add(name);
                    continue;
                }
                index++;
            }
            return result;
        }

        private static void ValidateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Expression is required.", nameof(expression));
            if (expression.Length > MaxExpressionLength) throw new InvalidOperationException("Expression is too long.");
        }

        private static IReadOnlyDictionary<string, double> NormalizeVariables(IReadOnlyDictionary<string, double>? variables)
        {
            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (variables == null) return normalized;

            foreach (var pair in variables)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new InvalidOperationException("Variable names cannot be blank or whitespace-only.");

                var normalizedName = pair.Key.Trim();
                if (normalized.ContainsKey(normalizedName))
                    throw new InvalidOperationException($"Variable name '{pair.Key}' conflicts with another variable after trimming whitespace and ignoring casing.");
                normalized.Add(normalizedName, pair.Value);
            }

            return normalized;
        }

        private static void SkipNumberToken(string text, ref int index)
        {
            var seenDot = false;
            var seenExponent = false;
            while (index < text.Length)
            {
                var c = text[index];
                if (char.IsDigit(c)) { index++; continue; }
                if (c == '.' && !seenDot && !seenExponent) { seenDot = true; index++; continue; }
                if ((c == 'e' || c == 'E') && !seenExponent)
                {
                    seenExponent = true;
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                    continue;
                }
                break;
            }
        }

        private sealed class Parser
        {
            private const int MaxDepth = 64;
            private const int MaxArguments = 128;
            private readonly string _text;
            private readonly IReadOnlyDictionary<string, double> _variables;
            private int _index;

            public Parser(string text, IReadOnlyDictionary<string, double> variables)
            {
                _text = text;
                _variables = variables;
            }

            public double Parse()
            {
                var value = ParseExpression(0);
                SkipWhiteSpace();
                if (_index != _text.Length) throw Error($"Unexpected token '{_text[_index]}'.");
                return EnsureFinite(value, "Expression produced a non-finite result.");
            }

            private double ParseExpression(int depth)
            {
                GuardDepth(depth);
                var value = ParseTerm(depth);
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('+')) value = EnsureFinite(value + ParseTerm(depth), "Addition produced a non-finite result.");
                    else if (Match('-')) value = EnsureFinite(value - ParseTerm(depth), "Subtraction produced a non-finite result.");
                    else return value;
                }
            }

            private double ParseTerm(int depth)
            {
                var value = ParseUnary(depth);
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('*')) value = EnsureFinite(value * ParseUnary(depth), "Multiplication produced a non-finite result.");
                    else if (Match('/'))
                    {
                        var divisor = ParseUnary(depth);
                        if (divisor == 0d) throw Error("Division by zero.");
                        value = EnsureFinite(value / divisor, "Division produced a non-finite result.");
                    }
                    else return value;
                }
            }

            private double ParseUnary(int depth)
            {
                SkipWhiteSpace();
                if (Match('+')) return ParseUnary(depth + 1);
                if (Match('-')) return EnsureFinite(-ParseUnary(depth + 1), "Unary negation produced a non-finite result.");
                return ParsePrimary(depth);
            }

            private double ParsePrimary(int depth)
            {
                GuardDepth(depth);
                SkipWhiteSpace();
                if (Match('('))
                {
                    var value = ParseExpression(depth + 1);
                    Expect(')');
                    return value;
                }
                if (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.')) return ParseNumber();
                if (_index < _text.Length && (char.IsLetter(_text[_index]) || _text[_index] == '_'))
                {
                    var name = ParseIdentifier();
                    SkipWhiteSpace();
                    if (Match('(')) return ParseFunction(name, depth + 1);
                    if (_variables.TryGetValue(name, out var value)) return EnsureFinite(value, $"Variable '{name}' contains a non-finite value.");
                    throw Error($"Unknown variable '{name}'.");
                }
                throw Error("Expected a number, variable, function, or parenthesized expression.");
            }

            private double ParseFunction(string name, int depth)
            {
                GuardDepth(depth);
                var args = new List<double>();
                SkipWhiteSpace();
                if (!Peek(')'))
                {
                    while (true)
                    {
                        if (args.Count >= MaxArguments) throw Error("Function has too many arguments.");
                        args.Add(ParseExpression(depth));
                        SkipWhiteSpace();
                        if (Match(',')) continue;
                        break;
                    }
                }
                Expect(')');
                switch (name.ToLowerInvariant())
                {
                    case "abs": RequireArgCount(name, args, 1); return EnsureFinite(Math.Abs(args[0]), "abs produced a non-finite result.");
                    case "ceil": RequireArgCount(name, args, 1); return EnsureFinite(Math.Ceiling(args[0]), "ceil produced a non-finite result.");
                    case "floor": RequireArgCount(name, args, 1); return EnsureFinite(Math.Floor(args[0]), "floor produced a non-finite result.");
                    case "round":
                        if (args.Count == 1) return EnsureFinite(Math.Round(args[0], MidpointRounding.AwayFromZero), "round produced a non-finite result.");
                        if (args.Count == 2)
                        {
                            var digitsValue = args[1];
                            var roundedDigits = Math.Round(digitsValue, MidpointRounding.AwayFromZero);
                            if (digitsValue < 0d || digitsValue > 15d || Math.Abs(digitsValue - roundedDigits) > 1e-12)
                                throw Error("round(value, digits) requires an integer digits argument from 0 to 15.");
                            return EnsureFinite(Math.Round(args[0], (int)roundedDigits, MidpointRounding.AwayFromZero), "round produced a non-finite result.");
                        }
                        throw Error("round expects 1 or 2 arguments.");
                    case "min":
                        RequireAtLeast(name, args, 1);
                        var min = args[0];
                        for (var i = 1; i < args.Count; i++) min = Math.Min(min, args[i]);
                        return EnsureFinite(min, "min produced a non-finite result.");
                    case "max":
                        RequireAtLeast(name, args, 1);
                        var max = args[0];
                        for (var i = 1; i < args.Count; i++) max = Math.Max(max, args[i]);
                        return EnsureFinite(max, "max produced a non-finite result.");
                    default: throw Error($"Unknown function '{name}'.");
                }
            }

            private double ParseNumber()
            {
                var start = _index;
                var seenDot = false;
                var seenExponent = false;
                while (_index < _text.Length)
                {
                    var c = _text[_index];
                    if (char.IsDigit(c)) { _index++; continue; }
                    if (c == '.' && !seenDot && !seenExponent) { seenDot = true; _index++; continue; }
                    if ((c == 'e' || c == 'E') && !seenExponent)
                    {
                        seenExponent = true;
                        _index++;
                        if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-')) _index++;
                        continue;
                    }
                    break;
                }
                var token = _text.Substring(start, _index - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                    throw Error($"Invalid number '{token}'.");
                return value;
            }

            private string ParseIdentifier()
            {
                var start = _index++;
                while (_index < _text.Length)
                {
                    var c = _text[_index];
                    if (char.IsLetterOrDigit(c) || c == '_' || c == '.') _index++;
                    else break;
                }
                return _text.Substring(start, _index - start);
            }

            private double EnsureFinite(double value, string message)
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) throw Error(message);
                return value;
            }

            private void GuardDepth(int depth)
            {
                if (depth > MaxDepth) throw Error("Expression nesting is too deep.");
            }

            private bool Match(char c)
            {
                SkipWhiteSpace();
                if (_index < _text.Length && _text[_index] == c) { _index++; return true; }
                return false;
            }

            private bool Peek(char c)
            {
                SkipWhiteSpace();
                return _index < _text.Length && _text[_index] == c;
            }

            private void Expect(char c)
            {
                if (!Match(c)) throw Error($"Expected '{c}'.");
            }

            private void SkipWhiteSpace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) _index++;
            }

            private InvalidOperationException Error(string message) => new InvalidOperationException($"{message} Position {_index}.");
            private void RequireArgCount(string name, IReadOnlyCollection<double> args, int count) { if (args.Count != count) throw Error($"{name} expects {count} argument(s)."); }
            private void RequireAtLeast(string name, IReadOnlyCollection<double> args, int count) { if (args.Count < count) throw Error($"{name} expects at least {count} argument(s)."); }
        }
    }
}
