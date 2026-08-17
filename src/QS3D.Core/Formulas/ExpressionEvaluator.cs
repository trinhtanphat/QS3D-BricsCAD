using System;
using System.Collections.Generic;
using System.Globalization;

namespace QS3D.Core.Formulas
{
    public sealed class ExpressionEvaluator
    {
        private const int MaxExpressionLength = 4096;
        private const int MaxVariableCount = MaxExpressionLength;

        public double Evaluate(string expression, IReadOnlyDictionary<string, double>? variables = null)
        {
            ValidateExpression(expression);
            var result = new Parser(expression, NormalizeVariables(variables)).Parse();
            return result == 0d ? 0d : result;
        }

        public IReadOnlyCollection<string> GetReferencedVariables(string expression)
        {
            ValidateExpression(expression);
            var result = new List<string>();
            new Parser(
                expression,
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
                result).Parse();
            return result.AsReadOnly();
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
            if (variables.Count > MaxVariableCount)
                throw new InvalidOperationException($"Variable count exceeds the supported maximum of {MaxVariableCount}.");

            var variableCount = 0;
            foreach (var pair in variables)
            {
                variableCount++;
                if (variableCount > MaxVariableCount)
                    throw new InvalidOperationException($"Variable count exceeds the supported maximum of {MaxVariableCount}.");
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new InvalidOperationException("Variable names cannot be blank or whitespace-only.");

                var normalizedName = pair.Key.Trim();
                if (!IsValidIdentifier(normalizedName))
                    throw new InvalidOperationException($"Variable name '{normalizedName}' is not a valid expression identifier.");
                if (normalized.ContainsKey(normalizedName))
                    throw new InvalidOperationException($"Variable name '{pair.Key}' conflicts with another variable after trimming whitespace and ignoring casing.");
                if (double.IsNaN(pair.Value) || double.IsInfinity(pair.Value))
                    throw new InvalidOperationException($"Variable '{normalizedName}' contains a non-finite value.");
                normalized.Add(normalizedName, pair.Value);
            }

            return normalized;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (value.Length == 0 || !IsIdentifierStart(value[0])) return false;
            for (var i = 1; i < value.Length; i++)
            {
                if (!IsIdentifierPart(value[i])) return false;
            }
            return true;
        }

        private static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_';

        private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_' || value == '.';

        private sealed class Parser
        {
            private const int MaxDepth = 64;
            private const int MaxArguments = 128;
            private readonly string _text;
            private readonly IReadOnlyDictionary<string, double> _variables;
            private readonly List<string>? _referencedVariables;
            private readonly HashSet<string>? _seenReferencedVariables;
            private readonly bool _evaluate;
            private int _index;

            public Parser(
                string text,
                IReadOnlyDictionary<string, double> variables,
                List<string>? referencedVariables = null)
            {
                _text = text;
                _variables = variables;
                _referencedVariables = referencedVariables;
                _evaluate = referencedVariables == null;
                if (referencedVariables != null)
                    _seenReferencedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                var sum = ParseTerm(depth);
                var compensation = 0d;
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('+'))
                    {
                        var right = ParseTerm(depth);
                        if (_evaluate) AddCompensated(ref sum, ref compensation, right, "Addition");
                        else sum = 0d;
                    }
                    else if (Match('-'))
                    {
                        var right = ParseTerm(depth);
                        if (_evaluate) AddCompensated(ref sum, ref compensation, -right, "Subtraction");
                        else sum = 0d;
                    }
                    else
                    {
                        return _evaluate
                            ? EnsureFinite(sum + compensation, "Addition/subtraction produced a non-finite result.")
                            : sum;
                    }
                }
            }

            private void AddCompensated(ref double sum, ref double compensation, double contribution, string operation)
            {
                var next = EnsureFinite(sum + contribution, operation + " produced a non-finite result.");
                var correction = Math.Abs(sum) >= Math.Abs(contribution)
                    ? (sum - next) + contribution
                    : (contribution - next) + sum;
                compensation = EnsureFinite(
                    compensation + correction,
                    operation + " compensation produced a non-finite result.");
                sum = next;
            }

            private double ParseTerm(int depth)
            {
                var value = ParseUnary(depth);
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('*'))
                    {
                        var right = ParseUnary(depth);
                        if (_evaluate)
                        {
                            var product = EnsureFinite(value * right, "Multiplication produced a non-finite result.");
                            if (product == 0d && value != 0d && right != 0d)
                                throw Error("Multiplication underflowed to zero.");
                            value = product;
                        }
                        else value = 0d;
                    }
                    else if (Match('/'))
                    {
                        var divisor = ParseUnary(depth);
                        if (_evaluate)
                        {
                            if (divisor == 0d) throw Error("Division by zero.");
                            var quotient = EnsureFinite(value / divisor, "Division produced a non-finite result.");
                            if (quotient == 0d && value != 0d)
                                throw Error("Division underflowed to zero.");
                            value = quotient;
                        }
                        else value = 0d;
                    }
                    else return value;
                }
            }

            private double ParseUnary(int depth)
            {
                GuardDepth(depth);
                SkipWhiteSpace();
                if (Match('+')) return ParseUnary(depth + 1);
                if (Match('-'))
                {
                    var value = ParseUnary(depth + 1);
                    return _evaluate
                        ? EnsureFinite(-value, "Unary negation produced a non-finite result.")
                        : 0d;
                }
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
                if (_index < _text.Length && IsIdentifierStart(_text[_index]))
                {
                    var name = ParseIdentifier();
                    SkipWhiteSpace();
                    if (Match('(')) return ParseFunction(name, depth + 1);
                    if (!_evaluate)
                    {
                        RecordReference(name);
                        return 0d;
                    }
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
                ValidateFunctionShape(name, args.Count);
                if (!_evaluate) return 0d;

                switch (name.ToLowerInvariant())
                {
                    case "abs": return EnsureFinite(Math.Abs(args[0]), "abs produced a non-finite result.");
                    case "ceil": return EnsureFinite(Math.Ceiling(args[0]), "ceil produced a non-finite result.");
                    case "floor": return EnsureFinite(Math.Floor(args[0]), "floor produced a non-finite result.");
                    case "round":
                        if (args.Count == 1) return EnsureFinite(Math.Round(args[0], MidpointRounding.AwayFromZero), "round produced a non-finite result.");
                        var digitsValue = args[1];
                        var roundedDigits = Math.Round(digitsValue, MidpointRounding.AwayFromZero);
                        if (digitsValue < 0d || digitsValue > 15d || digitsValue != roundedDigits)
                            throw Error("round(value, digits) requires an integer digits argument from 0 to 15.");
                        return EnsureFinite(Math.Round(args[0], (int)roundedDigits, MidpointRounding.AwayFromZero), "round produced a non-finite result.");
                    case "min":
                        var min = args[0];
                        for (var i = 1; i < args.Count; i++) min = Math.Min(min, args[i]);
                        return EnsureFinite(min, "min produced a non-finite result.");
                    case "max":
                        var max = args[0];
                        for (var i = 1; i < args.Count; i++) max = Math.Max(max, args[i]);
                        return EnsureFinite(max, "max produced a non-finite result.");
                    default: throw Error($"Unknown function '{name}'.");
                }
            }

            private void ValidateFunctionShape(string name, int argumentCount)
            {
                switch (name.ToLowerInvariant())
                {
                    case "abs":
                    case "ceil":
                    case "floor":
                        if (argumentCount != 1) throw Error($"{name} expects 1 argument(s).");
                        return;
                    case "round":
                        if (argumentCount != 1 && argumentCount != 2) throw Error("round expects 1 or 2 arguments.");
                        return;
                    case "min":
                    case "max":
                        if (argumentCount < 1) throw Error($"{name} expects at least 1 argument(s).");
                        return;
                    default:
                        throw Error($"Unknown function '{name}'.");
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
                if (value == 0d)
                {
                    for (var i = 0; i < token.Length; i++)
                    {
                        if (token[i] == 'e' || token[i] == 'E') break;
                        if (token[i] >= '1' && token[i] <= '9')
                            throw Error($"Number '{token}' underflowed to zero.");
                    }
                }
                return value;
            }

            private string ParseIdentifier()
            {
                var start = _index++;
                while (_index < _text.Length)
                {
                    var c = _text[_index];
                    if (IsIdentifierPart(c)) _index++;
                    else break;
                }
                return _text.Substring(start, _index - start);
            }

            private void RecordReference(string name)
            {
                if (_referencedVariables == null || _seenReferencedVariables == null) return;
                if (_seenReferencedVariables.Add(name)) _referencedVariables.Add(name);
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
        }
    }
}
