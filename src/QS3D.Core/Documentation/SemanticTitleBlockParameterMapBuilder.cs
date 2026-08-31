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
            var knownCount = TryGetKnownCount(definitions, out var conflictingKnownCounts, out var negativeKnownCount);
            if (negativeKnownCount)
                throw new InvalidOperationException("Semantic title-block mapping source exposes an invalid negative known Count value.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Semantic title-block mapping source exposes conflicting known Count values.");
            if (knownCount.HasValue && knownCount.Value > MaxParameters)
                throw ParameterCollectionTooLarge();

            var result = new List<SemanticTitleBlockParameterDefinition>(knownCount ?? MaxParameters);
            var observedCount = 0;
            using (var enumerator = definitions.GetEnumerator())
            {
                while (true)
                {
                    if (knownCount.HasValue)
                        RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);

                    var moved = enumerator.MoveNext();

                    if (knownCount.HasValue)
                        RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);
                    if (!moved)
                        break;

                    if (knownCount.HasValue && observedCount >= knownCount.Value)
                        throw new InvalidOperationException(
                            "Semantic title-block mapping source known Count was exceeded during traversal.");
                    if (observedCount >= MaxParameters)
                        throw ParameterCollectionTooLarge();

                    var definition = enumerator.Current;
                    if (knownCount.HasValue)
                        RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);

                    result.Add(definition);
                    observedCount++;
                }
            }

            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw new InvalidOperationException("Semantic title-block mapping source known Count does not match the number of definitions traversed.");

            if (knownCount.HasValue)
                RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);

            return result;
        }

        private static void RevalidateKnownCountAfterTraversal(
            IEnumerable<SemanticTitleBlockParameterDefinition> definitions,
            int admittedKnownCount)
        {
            var reboundKnownCount = TryGetKnownCount(
                definitions,
                out var conflictingKnownCounts,
                out var negativeKnownCount);
            if (negativeKnownCount)
                throw new InvalidOperationException(
                    "Semantic title-block mapping source exposes an invalid negative known Count value after traversal.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException(
                    "Semantic title-block mapping source exposes conflicting known Count values after traversal.");
            if (!reboundKnownCount.HasValue || reboundKnownCount.Value != admittedKnownCount)
                throw new InvalidOperationException(
                    "Semantic title-block mapping source known Count changed during traversal.");
        }

        private static int? TryGetKnownCount(
            IEnumerable<SemanticTitleBlockParameterDefinition> definitions,
            out bool conflictingKnownCounts,
            out bool negativeKnownCount)
        {
            conflictingKnownCounts = false;
            negativeKnownCount = false;
            int? knownCount = null;

            if (definitions is ICollection<SemanticTitleBlockParameterDefinition> collection)
                knownCount = ObserveKnownCount(knownCount, collection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (definitions is IReadOnlyCollection<SemanticTitleBlockParameterDefinition> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (definitions is System.Collections.ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);

            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool negativeKnownCount)
        {
            if (observed < 0)
                negativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }

        private static InvalidOperationException ParameterCollectionTooLarge()
        {
            return new InvalidOperationException(
                "Semantic title-block mapping supports at most " + MaxParameters + " parameters.");
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