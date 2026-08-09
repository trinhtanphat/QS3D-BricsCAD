using System;
using System.Collections.Generic;
using System.Globalization;

namespace QS3D.Core.Formulas
{
    public sealed class ExpressionEvaluator
    {
        private string _text = string.Empty;
        private int _index;
        private IReadOnlyDictionary<string, double> _variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public double Evaluate(string expression, IReadOnlyDictionary<string, double>? variables = null)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Expression is required.", nameof(expression));
            _text = expression; _index = 0;
            _variables = variables ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var value = ParseExpression(); SkipWhiteSpace();
            if (_index != _text.Length) throw Error($"Unexpected token '{_text[_index]}'.");
            if (double.IsNaN(value) || double.IsInfinity(value)) throw Error("Expression produced a non-finite result.");
            return value;
        }

        private double ParseExpression()
        {
            var value = ParseTerm();
            while (true) { SkipWhiteSpace(); if (Match('+')) value += ParseTerm(); else if (Match('-')) value -= ParseTerm(); else return value; }
        }

        private double ParseTerm()
        {
            var value = ParseUnary();
            while (true)
            {
                SkipWhiteSpace();
                if (Match('*')) value *= ParseUnary();
                else if (Match('/')) { var divisor = ParseUnary(); if (Math.Abs(divisor) < 1e-15) throw Error("Division by zero."); value /= divisor; }
                else return value;
            }
        }

        private double ParseUnary() { SkipWhiteSpace(); if (Match('+')) return ParseUnary(); if (Match('-')) return -ParseUnary(); return ParsePrimary(); }

        private double ParsePrimary()
        {
            SkipWhiteSpace();
            if (Match('(')) { var value = ParseExpression(); Expect(')'); return value; }
            if (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.')) return ParseNumber();
            if (_index < _text.Length && (char.IsLetter(_text[_index]) || _text[_index] == '_'))
            {
                var name = ParseIdentifier(); SkipWhiteSpace();
                if (Match('(')) return ParseFunction(name);
                if (_variables.TryGetValue(name, out var value)) return value;
                throw Error($"Unknown variable '{name}'.");
            }
            throw Error("Expected a number, variable, function, or parenthesized expression.");
        }

        private double ParseFunction(string name)
        {
            var args = new List<double>(); SkipWhiteSpace();
            if (!Peek(')')) { while (true) { args.Add(ParseExpression()); SkipWhiteSpace(); if (Match(',')) continue; break; } }
            Expect(')');
            switch (name.ToLowerInvariant())
            {
                case "abs": RequireArgCount(name, args, 1); return Math.Abs(args[0]);
                case "ceil": RequireArgCount(name, args, 1); return Math.Ceiling(args[0]);
                case "floor": RequireArgCount(name, args, 1); return Math.Floor(args[0]);
                case "round":
                    if (args.Count == 1) return Math.Round(args[0]);
                    if (args.Count == 2) { var digits = checked((int)args[1]); if (Math.Abs(args[1] - digits) > 1e-12 || digits < 0 || digits > 15) throw Error("round(value, digits) requires an integer digits argument from 0 to 15."); return Math.Round(args[0], digits); }
                    throw Error("round expects 1 or 2 arguments.");
                case "min": RequireAtLeast(name, args, 1); var min = args[0]; for (var i = 1; i < args.Count; i++) min = Math.Min(min, args[i]); return min;
                case "max": RequireAtLeast(name, args, 1); var max = args[0]; for (var i = 1; i < args.Count; i++) max = Math.Max(max, args[i]); return max;
                default: throw Error($"Unknown function '{name}'.");
            }
        }

        private double ParseNumber()
        {
            var start = _index; var seenDot = false;
            while (_index < _text.Length)
            {
                var c = _text[_index];
                if (char.IsDigit(c)) { _index++; continue; }
                if (c == '.' && !seenDot) { seenDot = true; _index++; continue; }
                break;
            }
            var token = _text.Substring(start, _index - start);
            if (!double.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)) throw Error($"Invalid number '{token}'.");
            return value;
        }

        private string ParseIdentifier()
        {
            var start = _index++;
            while (_index < _text.Length) { var c = _text[_index]; if (char.IsLetterOrDigit(c) || c == '_' || c == '.') _index++; else break; }
            return _text.Substring(start, _index - start);
        }
        private bool Match(char c) { SkipWhiteSpace(); if (_index < _text.Length && _text[_index] == c) { _index++; return true; } return false; }
        private bool Peek(char c) { SkipWhiteSpace(); return _index < _text.Length && _text[_index] == c; }
        private void Expect(char c) { if (!Match(c)) throw Error($"Expected '{c}'."); }
        private void SkipWhiteSpace() { while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) _index++; }
        private InvalidOperationException Error(string message) => new InvalidOperationException($"{message} Position {_index}.");
        private void RequireArgCount(string name, IReadOnlyCollection<double> args, int count) { if (args.Count != count) throw Error($"{name} expects {count} argument(s)."); }
        private void RequireAtLeast(string name, IReadOnlyCollection<double> args, int count) { if (args.Count < count) throw Error($"{name} expects at least {count} argument(s)."); }
    }
}
