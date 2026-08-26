using System;
using QS3D.Core.Coordination;

namespace QS3D.BricsCAD.V25.UI
{
    internal static class CoordinationManagerFilterBuilder
    {
        private const string AllOption = "Tất cả";

        public static CoordinationManagerFilter Build(
            string statusText,
            string severityText,
            bool actionableOnly,
            string kindText,
            string floorText,
            string categoryText,
            string ruleText)
        {
            var filter = new CoordinationManagerFilter
            {
                IncludeNonActionable = !actionableOnly,
                FloorId = NormalizeOptionalToken(floorText),
                Category = NormalizeOptionalToken(categoryText),
                RuleId = NormalizeOptionalToken(ruleText)
            };

            filter.Status = ParseOptionalEnum<CoordinationFindingStatus>(statusText, nameof(statusText));
            filter.MinimumSeverity = ParseOptionalEnum<CoordinationFindingSeverity>(severityText, nameof(severityText));
            filter.Kind = ParseOptionalEnum<CoordinationFindingKind>(kindText, nameof(kindText));
            return filter;
        }

        private static TEnum? ParseOptionalEnum<TEnum>(string value, string parameterName)
            where TEnum : struct
        {
            var normalized = NormalizeOptionalToken(value);
            if (normalized.Length == 0 || string.Equals(normalized, AllOption, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!Enum.TryParse(normalized, true, out TEnum parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
                throw new ArgumentException("Unsupported Coordination Manager filter value: " + normalized, parameterName);

            return parsed;
        }

        private static string NormalizeOptionalToken(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
