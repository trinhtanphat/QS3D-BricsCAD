using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Mapping
{
    public enum MeasurementWorkItemCoverageIssue
    {
        MissingQuantity = 0,
        StaleQuantity = 1,
        UnmappedWorkItem = 2
    }

    public sealed class MeasurementWorkItemCoverageFinding
    {
        internal MeasurementWorkItemCoverageFinding(
            string elementId,
            ElementCategory category,
            string? quantityKey,
            double? quantityValue,
            MeasurementWorkItemMapping? mapping,
            IEnumerable<MeasurementWorkItemCoverageIssue> issues)
        {
            ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            Category = category;
            QuantityKey = quantityKey;
            QuantityValue = quantityValue;
            Mapping = mapping;
            Issues = new ReadOnlyCollection<MeasurementWorkItemCoverageIssue>(
                (issues ?? throw new ArgumentNullException(nameof(issues))).Distinct().OrderBy(x => (int)x).ToArray());
        }

        public string ElementId { get; }
        public ElementCategory Category { get; }
        public string? QuantityKey { get; }
        public double? QuantityValue { get; }
        public MeasurementWorkItemMapping? Mapping { get; }
        public IReadOnlyList<MeasurementWorkItemCoverageIssue> Issues { get; }
        public bool IsReady => QuantityKey != null && QuantityValue.HasValue && Mapping != null && Issues.Count == 0;
    }

    public static class MeasurementWorkItemCoverageEvaluator
    {
        public static IReadOnlyList<MeasurementWorkItemCoverageFinding> Evaluate(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Evaluate(project, new MeasurementWorkItemMappingCatalog(project.MeasurementWorkItemMappings));
        }

        public static IReadOnlyList<MeasurementWorkItemCoverageFinding> Evaluate(
            ProjectState project,
            MeasurementWorkItemMappingCatalog catalog)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var elements = SnapshotElements(project);
            elements.Sort(CompareElements);
            var findings = new List<MeasurementWorkItemCoverageFinding>();

            foreach (var element in elements)
            {
                if (element.Quantities.Count == 0)
                {
                    findings.Add(new MeasurementWorkItemCoverageFinding(
                        element.Id,
                        element.Category,
                        null,
                        null,
                        null,
                        new[] { MeasurementWorkItemCoverageIssue.MissingQuantity }));
                    continue;
                }

                foreach (var quantity in element.Quantities)
                {
                    var resolution = catalog.Resolve(element.Category, quantity.Key);
                    var issues = new List<MeasurementWorkItemCoverageIssue>(2);
                    if (element.QuantityStale)
                        issues.Add(MeasurementWorkItemCoverageIssue.StaleQuantity);
                    if (!resolution.IsMapped)
                        issues.Add(MeasurementWorkItemCoverageIssue.UnmappedWorkItem);

                    findings.Add(new MeasurementWorkItemCoverageFinding(
                        element.Id,
                        element.Category,
                        quantity.Key,
                        quantity.Value,
                        resolution.Mapping,
                        issues));
                }
            }

            return new ReadOnlyCollection<MeasurementWorkItemCoverageFinding>(findings.ToArray());
        }

        private static List<ElementCoverageSnapshot> SnapshotElements(ProjectState project)
        {
            var source = project.Elements.ToArray();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<ElementCoverageSnapshot>(source.Length);

            for (var index = 0; index < source.Length; index++)
            {
                var element = source[index];
                if (element == null)
                    throw new InvalidOperationException("Quantity coverage cannot inspect a null project element at index " + index + ".");
                var id = RequireCanonicalIdentity(element.Id, "element id");
                if (!seenIds.Add(id))
                    throw new InvalidOperationException("Quantity coverage cannot inspect duplicate element id: " + id + ".");
                if (!Enum.IsDefined(typeof(ElementCategory), element.Category))
                    throw new InvalidOperationException("Quantity coverage cannot inspect element " + id + " with an undefined category.");

                var quantities = SnapshotQuantities(element);
                result.Add(new ElementCoverageSnapshot(
                    id,
                    element.Category,
                    (element.Dirty & ElementDirtyFlags.Quantity) != 0,
                    quantities));
            }

            return result;
        }

        private static IReadOnlyList<QuantityCoverageSnapshot> SnapshotQuantities(ProjectElement element)
        {
            var source = element.Quantities.ToArray();
            var quantities = new List<QuantityCoverageSnapshot>(source.Length);
            for (var index = 0; index < source.Length; index++)
            {
                var item = source[index];
                var key = RequireCanonicalIdentity(item.Key, "quantity key for element " + element.Id);
                if (double.IsNaN(item.Value) || double.IsInfinity(item.Value))
                    throw new InvalidOperationException("Quantity coverage found a non-finite quantity: " + element.Id + "/" + key + ".");
                var value = item.Value == 0d ? 0d : item.Value;
                quantities.Add(new QuantityCoverageSnapshot(key, value));
            }
            quantities.Sort(CompareQuantities);
            return new ReadOnlyCollection<QuantityCoverageSnapshot>(quantities.ToArray());
        }

        private static string RequireCanonicalIdentity(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Quantity coverage " + label + " must not be blank.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity coverage " + label + " must not contain leading/trailing whitespace.");
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    throw new InvalidOperationException("Quantity coverage " + label + " must not contain control characters.");
            }
            return value;
        }

        private static int CompareElements(ElementCoverageSnapshot left, ElementCoverageSnapshot right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }

        private static int CompareQuantities(QuantityCoverageSnapshot left, QuantityCoverageSnapshot right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Key, right.Key);
        }

        private sealed class ElementCoverageSnapshot
        {
            public ElementCoverageSnapshot(
                string id,
                ElementCategory category,
                bool quantityStale,
                IReadOnlyList<QuantityCoverageSnapshot> quantities)
            {
                Id = id;
                Category = category;
                QuantityStale = quantityStale;
                Quantities = quantities;
            }

            public string Id { get; }
            public ElementCategory Category { get; }
            public bool QuantityStale { get; }
            public IReadOnlyList<QuantityCoverageSnapshot> Quantities { get; }
        }

        private sealed class QuantityCoverageSnapshot
        {
            public QuantityCoverageSnapshot(string key, double value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }
            public double Value { get; }
        }
    }
}
