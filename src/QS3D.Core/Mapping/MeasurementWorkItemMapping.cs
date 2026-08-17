using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using QS3D.Core.Domain;

namespace QS3D.Core.Mapping
{
    public enum MeasurementWorkItemMappingResolutionKind
    {
        Unmapped = 0,
        Mapped = 1
    }

    public sealed class MeasurementWorkItemMapping
    {
        public MeasurementWorkItemMapping(
            string mappingId,
            ElementCategory category,
            string measurementItemId,
            string classificationId,
            string workItemId)
        {
            MappingId = MeasurementWorkItemMappingContract.RequireToken(mappingId, nameof(mappingId));
            Category = MeasurementWorkItemMappingContract.RequireCategory(category, nameof(category));
            MeasurementItemId = MeasurementWorkItemMappingContract.RequireToken(measurementItemId, nameof(measurementItemId));
            ClassificationId = MeasurementWorkItemMappingContract.RequireToken(classificationId, nameof(classificationId));
            WorkItemId = MeasurementWorkItemMappingContract.RequireToken(workItemId, nameof(workItemId));
        }

        public string MappingId { get; }
        public ElementCategory Category { get; }
        public string MeasurementItemId { get; }
        public string ClassificationId { get; }
        public string WorkItemId { get; }
    }

    public sealed class MeasurementWorkItemMappingResolution
    {
        private MeasurementWorkItemMappingResolution(
            MeasurementWorkItemMappingResolutionKind kind,
            ElementCategory category,
            string measurementItemId,
            MeasurementWorkItemMapping? mapping)
        {
            Kind = kind;
            Category = category;
            MeasurementItemId = measurementItemId;
            Mapping = mapping;
        }

        public MeasurementWorkItemMappingResolutionKind Kind { get; }
        public bool IsMapped => Kind == MeasurementWorkItemMappingResolutionKind.Mapped;
        public ElementCategory Category { get; }
        public string MeasurementItemId { get; }
        public MeasurementWorkItemMapping? Mapping { get; }

        internal static MeasurementWorkItemMappingResolution Mapped(MeasurementWorkItemMapping mapping) =>
            new MeasurementWorkItemMappingResolution(
                MeasurementWorkItemMappingResolutionKind.Mapped,
                mapping.Category,
                mapping.MeasurementItemId,
                mapping);

        internal static MeasurementWorkItemMappingResolution Unmapped(ElementCategory category, string measurementItemId) =>
            new MeasurementWorkItemMappingResolution(
                MeasurementWorkItemMappingResolutionKind.Unmapped,
                category,
                measurementItemId,
                null);
    }

    public sealed class MeasurementWorkItemMappingCatalog
    {
        private const int MaximumEntries = 10000;
        private readonly Dictionary<ElementCategory, Dictionary<string, MeasurementWorkItemMapping>> _byCategory;

        public MeasurementWorkItemMappingCatalog(IEnumerable<MeasurementWorkItemMapping> mappings)
        {
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
            if (TryGetKnownCount(mappings, out var knownCount) && knownCount > MaximumEntries)
                throw new InvalidOperationException("Measurement/work-item mapping catalog supports at most " + MaximumEntries + " entries.");

            var items = new List<MeasurementWorkItemMapping>();
            var mappingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _byCategory = new Dictionary<ElementCategory, Dictionary<string, MeasurementWorkItemMapping>>();

            var index = 0;
            foreach (var mapping in mappings)
            {
                if (index == MaximumEntries)
                    throw new InvalidOperationException("Measurement/work-item mapping catalog supports at most " + MaximumEntries + " entries.");
                if (mapping == null)
                    throw new ArgumentException("Measurement/work-item mapping collection contains a null entry at index " + index + ".", nameof(mappings));
                if (!mappingIds.Add(mapping.MappingId))
                    throw new ArgumentException("Duplicate measurement/work-item mapping id: " + mapping.MappingId + ".", nameof(mappings));

                if (!_byCategory.TryGetValue(mapping.Category, out var byMeasurementItem))
                {
                    byMeasurementItem = new Dictionary<string, MeasurementWorkItemMapping>(StringComparer.OrdinalIgnoreCase);
                    _byCategory.Add(mapping.Category, byMeasurementItem);
                }

                if (byMeasurementItem.ContainsKey(mapping.MeasurementItemId))
                    throw new ArgumentException(
                        "Ambiguous measurement/work-item mapping target: " + mapping.Category + "/" + mapping.MeasurementItemId + ".",
                        nameof(mappings));

                byMeasurementItem.Add(mapping.MeasurementItemId, mapping);
                items.Add(mapping);
                index++;
            }

            items.Sort(CompareMappings);
            Mappings = new ReadOnlyCollection<MeasurementWorkItemMapping>(items.ToArray());
        }

        public IReadOnlyList<MeasurementWorkItemMapping> Mappings { get; }

        public MeasurementWorkItemMappingResolution Resolve(ElementCategory category, string measurementItemId)
        {
            var canonicalCategory = MeasurementWorkItemMappingContract.RequireCategory(category, nameof(category));
            var canonicalMeasurementItemId = MeasurementWorkItemMappingContract.RequireToken(measurementItemId, nameof(measurementItemId));

            if (_byCategory.TryGetValue(canonicalCategory, out var byMeasurementItem) &&
                byMeasurementItem.TryGetValue(canonicalMeasurementItemId, out var mapping))
                return MeasurementWorkItemMappingResolution.Mapped(mapping);

            return MeasurementWorkItemMappingResolution.Unmapped(canonicalCategory, canonicalMeasurementItemId);
        }

        private static bool TryGetKnownCount(IEnumerable<MeasurementWorkItemMapping> mappings, out int count)
        {
            if (mappings is ICollection<MeasurementWorkItemMapping> genericCollection)
            {
                count = genericCollection.Count;
                return true;
            }

            if (mappings is System.Collections.ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private static int CompareMappings(MeasurementWorkItemMapping left, MeasurementWorkItemMapping right)
        {
            var compare = StringComparer.Ordinal.Compare(left.Category.ToString(), right.Category.ToString());
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.MeasurementItemId, right.MeasurementItemId);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.MeasurementItemId, right.MeasurementItemId);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.MappingId, right.MappingId);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.MappingId, right.MappingId);
        }
    }

    internal static class MeasurementWorkItemMappingContract
    {
        internal static ElementCategory RequireCategory(ElementCategory value, string parameterName)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), value))
                throw new ArgumentOutOfRangeException(parameterName, value, "Mapping category must be a defined ElementCategory.");
            return value;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Mapping identifier is required.", parameterName);

            var trimmed = value.Trim();
            if (!string.Equals(value, trimmed, StringComparison.Ordinal))
                throw new ArgumentException("Mapping identifier must not contain leading or trailing whitespace.", parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Mapping identifier must not contain control characters.", parameterName);
            }
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Mapping identifier contains characters that are invalid in XML.", parameterName, ex);
            }
            return value;
        }
    }
}
