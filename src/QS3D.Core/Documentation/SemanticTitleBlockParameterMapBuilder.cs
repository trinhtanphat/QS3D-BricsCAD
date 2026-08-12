using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Documentation
{
    public enum SemanticTitleBlockSheetField
    {
        SheetId = 0,
        SheetNumber = 1,
        SheetName = 2,
        TitleBlockName = 3,
        PlacedViewCount = 4
    }

    public sealed class SemanticTitleBlockParameterDefinition
    {
        public SemanticTitleBlockParameterDefinition(string destinationTag, SemanticTitleBlockSheetField sourceField)
        {
            DestinationTag = destinationTag;
            SourceField = sourceField;
        }

        public string DestinationTag { get; }
        public SemanticTitleBlockSheetField SourceField { get; }
    }

    public sealed class SemanticTitleBlockParameterValue
    {
        internal SemanticTitleBlockParameterValue(string destinationTag, string value)
        {
            DestinationTag = destinationTag;
            Value = value;
        }

        public string DestinationTag { get; }
        public string Value { get; }
    }

    public sealed class SemanticTitleBlockParameterMap
    {
        internal SemanticTitleBlockParameterMap(IEnumerable<SemanticTitleBlockParameterValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            Values = new List<SemanticTitleBlockParameterValue>(values).AsReadOnly();
        }

        public IReadOnlyList<SemanticTitleBlockParameterValue> Values { get; }
    }

    public static class SemanticTitleBlockParameterMapBuilder
    {
        private const int MaxParameters = 128;
        private const int MaxDestinationTagLength = 128;

        public static SemanticTitleBlockParameterMap Build(
            SemanticSheetPlan sheet,
            IEnumerable<SemanticTitleBlockParameterDefinition> definitions)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var materialized = MaterializeDefinitionsBounded(definitions);
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var values = new List<SemanticTitleBlockParameterValue>(materialized.Count);
            for (var i = 0; i < materialized.Count; i++)
            {
                var definition = materialized[i];
                if (definition == null)
                    throw new ArgumentException("Semantic title-block mapping cannot contain a null definition at index " + i + ".", nameof(definitions));

                var tag = RequiredTag(definition.DestinationTag, i);
                if (!tags.Add(tag))
                    throw new InvalidOperationException("Semantic title-block mapping contains duplicate destination tag: " + tag + ".");

                values.Add(new SemanticTitleBlockParameterValue(tag, ResolveValue(sheet, definition.SourceField)));
            }

            return new SemanticTitleBlockParameterMap(values
                .OrderBy(x => x.DestinationTag, StringComparer.OrdinalIgnoreCase));
        }

        private static List<SemanticTitleBlockParameterDefinition> MaterializeDefinitionsBounded(
            IEnumerable<SemanticTitleBlockParameterDefinition> definitions)
        {
            var result = new List<SemanticTitleBlockParameterDefinition>(MaxParameters);
            using (var enumerator = definitions.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxParameters)
                        throw new InvalidOperationException("Semantic title-block mapping supports at most " + MaxParameters + " parameters.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static string ResolveValue(SemanticSheetPlan sheet, SemanticTitleBlockSheetField field)
        {
            switch (field)
            {
                case SemanticTitleBlockSheetField.SheetId:
                    return sheet.Id;
                case SemanticTitleBlockSheetField.SheetNumber:
                    return sheet.Number;
                case SemanticTitleBlockSheetField.SheetName:
                    return sheet.Name;
                case SemanticTitleBlockSheetField.TitleBlockName:
                    return sheet.TitleBlockName ?? string.Empty;
                case SemanticTitleBlockSheetField.PlacedViewCount:
                    return sheet.Placements.Count.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new InvalidOperationException("Unsupported semantic title-block source field: " + field + ".");
            }
        }

        private static string RequiredTag(string? value, int index)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Semantic title-block destination tag is required at index " + index + ".", nameof(value));
            var normalized = value!.Trim();
            if (normalized.Length > MaxDestinationTagLength)
                throw new ArgumentException(
                    "Semantic title-block destination tag exceeds " + MaxDestinationTagLength + " characters at index " + index + ".",
                    nameof(value));
            return normalized;
        }
    }
}
