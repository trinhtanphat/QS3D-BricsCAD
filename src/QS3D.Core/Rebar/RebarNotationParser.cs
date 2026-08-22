using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QS3D.Core.Rebar
{
    public static class RebarNotationParser
    {
        private const int MaxNotationLength = 4096;
        private const int MaxCompoundGroups = 128;
        private static readonly Regex SpacingPattern = new Regex(@"^\s*(?:Ø|Φ|D|d)?\s*(?<dia>\d+(?:\.\d+)?)\s*(?:@|a|A)\s*(?<spacing>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex CountPattern = new Regex(@"^\s*(?:(?<sets>\d+)\s*[xX]\s*)?(?<qty>\d+)\s*(?:Ø|Φ|D|d)\s*(?<dia>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex DiameterOnlyPattern = new Regex(@"^\s*(?:Ø|Φ|D|d)\s*(?<dia>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static IReadOnlyList<RebarGroup> Parse(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation)) throw new ArgumentException("Rebar notation is required.", nameof(notation));
            if (notation.Length > MaxNotationLength)
                throw new FormatException("Rebar notation exceeds the supported " + MaxNotationLength + "-character limit.");
            var parts = notation.Split(new[] { '+' }, StringSplitOptions.None);
            if (parts.Length > MaxCompoundGroups)
                throw new FormatException("Rebar notation exceeds the supported " + MaxCompoundGroups + " compound-group limit.");
            var result = new List<RebarGroup>(parts.Length);
            foreach (var raw in parts)
            {
                if (string.IsNullOrWhiteSpace(raw)) throw new FormatException("Rebar notation contains an empty compound segment.");

                var spacing = SpacingPattern.Match(raw);
                if (spacing.Success)
                {
                    result.Add(new RebarGroup { DiameterMm = PositiveDouble(spacing.Groups["dia"].Value, "diameter"), SpacingMm = PositiveDouble(spacing.Groups["spacing"].Value, "spacing") });
                    continue;
                }

                var count = CountPattern.Match(raw);
                if (count.Success)
                {
                    var barsPerSet = PositiveInt(count.Groups["qty"].Value, "quantity");
                    var sets = count.Groups["sets"].Success ? PositiveInt(count.Groups["sets"].Value, "sets") : 1;
                    int quantity;
                    try { quantity = checked(sets * barsPerSet); }
                    catch (OverflowException ex) { throw new FormatException("Rebar quantity is too large.", ex); }
                    result.Add(new RebarGroup { Quantity = quantity, Sets = count.Groups["sets"].Success ? (int?)sets : null, BarsPerSet = count.Groups["sets"].Success ? (int?)barsPerSet : null, DiameterMm = PositiveDouble(count.Groups["dia"].Value, "diameter") });
                    continue;
                }

                var diameter = DiameterOnlyPattern.Match(raw);
                if (diameter.Success)
                {
                    result.Add(new RebarGroup { DiameterMm = PositiveDouble(diameter.Groups["dia"].Value, "diameter") });
                    continue;
                }
                throw new FormatException("Unsupported rebar notation segment '" + raw + "'.");
            }
            if (result.Count == 0) throw new FormatException("Rebar notation did not contain any bar group.");
            return result.AsReadOnly();
        }

        private static double PositiveDouble(string value, string label)
        {
            if (!double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException("Rebar " + label + " is too large.");
            if (result <= 0d) throw new FormatException("Rebar " + label + " must be greater than zero.");
            return result;
        }

        private static int PositiveInt(string value, string label)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                throw new FormatException("Rebar " + label + " is too large.");
            if (result <= 0) throw new FormatException("Rebar " + label + " must be greater than zero.");
            return result;
        }
    }
}
