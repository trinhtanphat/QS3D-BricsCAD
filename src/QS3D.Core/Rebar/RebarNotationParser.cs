using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QS3D.Core.Rebar
{
    public static class RebarNotationParser
    {
        private static readonly Regex SpacingPattern = new Regex(@"^\s*(?:Ø|Φ|D|d)?\s*(?<dia>\d+(?:\.\d+)?)\s*(?:@|a|A)\s*(?<spacing>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex CountPattern = new Regex(@"^\s*(?:(?<sets>\d+)\s*[xX]\s*)?(?<qty>\d+)\s*(?:Ø|Φ|D|d)\s*(?<dia>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex DiameterOnlyPattern = new Regex(@"^\s*(?:Ø|Φ|D|d)\s*(?<dia>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static IReadOnlyList<RebarGroup> Parse(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation)) throw new ArgumentException("Rebar notation is required.", nameof(notation));
            var normalized = notation.Replace(" ", string.Empty);
            var parts = normalized.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<RebarGroup>(parts.Length);
            foreach (var raw in parts)
            {
                var spacing = SpacingPattern.Match(raw);
                if (spacing.Success) { result.Add(new RebarGroup { DiameterMm = ParseDouble(spacing.Groups["dia"].Value), SpacingMm = ParseDouble(spacing.Groups["spacing"].Value) }); continue; }
                var count = CountPattern.Match(raw);
                if (count.Success)
                {
                    var barsPerSet = int.Parse(count.Groups["qty"].Value, CultureInfo.InvariantCulture);
                    var sets = count.Groups["sets"].Success ? int.Parse(count.Groups["sets"].Value, CultureInfo.InvariantCulture) : 1;
                    result.Add(new RebarGroup { Quantity = checked(sets * barsPerSet), Sets = count.Groups["sets"].Success ? (int?)sets : null, BarsPerSet = count.Groups["sets"].Success ? (int?)barsPerSet : null, DiameterMm = ParseDouble(count.Groups["dia"].Value) });
                    continue;
                }
                var diameter = DiameterOnlyPattern.Match(raw);
                if (diameter.Success) { result.Add(new RebarGroup { DiameterMm = ParseDouble(diameter.Groups["dia"].Value) }); continue; }
                throw new FormatException($"Unsupported rebar notation segment '{raw}'.");
            }
            return result;
        }
        private static double ParseDouble(string value) => double.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }
}
