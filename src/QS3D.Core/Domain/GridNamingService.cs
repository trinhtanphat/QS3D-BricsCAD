using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    public enum GridLabelSequence
    {
        Numeric = 0,
        Alphabetic = 1
    }

    public sealed class GridNamingOptions
    {
        public GridLabelSequence Sequence { get; set; } = GridLabelSequence.Numeric;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public int StartIndex { get; set; } = 1;
        public int NumericPadding { get; set; }
    }

    public sealed class GridLabelAssignment
    {
        public GridLabelAssignment(string elementId, string label, int sequenceIndex)
        {
            ElementId = elementId;
            Label = label;
            SequenceIndex = sequenceIndex;
        }

        public string ElementId { get; }
        public string Label { get; }
        public int SequenceIndex { get; }
    }

    public static class GridNamingService
    {
        public const string GridLabelKey = "GridLabel";
        public const string GridSequenceIndexKey = "GridSequenceIndex";

        private const int MaxGridBatch = 2000;
        private const int MaxAffixLength = 24;
        private const int MaxLabelLength = 64;
        private const int MaxSequenceIndex = 999999;
        private const int MaxNumericPadding = 6;

        public static IReadOnlyList<GridLabelAssignment> Renumber(
            ProjectState project,
            IEnumerable<string> orderedGridElementIds,
            GridNamingOptions? options = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (orderedGridElementIds == null) throw new ArgumentNullException(nameof(orderedGridElementIds));
            options ??= new GridNamingOptions();

            var targetEnumerationVersion = project.ChangeVersion;
            var projectElementsAtStart = project.Elements.ToList();
            var knownCount = TryGetKnownCount(orderedGridElementIds, out var conflictingKnownCounts, out var invalidNegativeKnownCount);
            var versionAfterKnownCount = project.ChangeVersion;
            if (versionAfterKnownCount != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.");
            if (knownCount.HasValue && knownCount.Value > MaxGridBatch)
                throw new InvalidOperationException("A Grid renumber batch supports at most " + MaxGridBatch + " elements.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Grid renumber target source exposes conflicting known Count values.");
            if (invalidNegativeKnownCount)
                throw new InvalidOperationException("Grid renumber target source exposes an invalid negative known count.");

            var ids = new List<string>();
            foreach (var value in orderedGridElementIds)
            {
                if (ids.Count == MaxGridBatch)
                    throw new InvalidOperationException("A Grid renumber batch supports at most " + MaxGridBatch + " elements.");
                ids.Add(Required(value, "orderedGridElementIds[" + ids.Count + "]", 128));
            }
            if (project.ChangeVersion != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.");
            if (ids.Count == 0) throw new InvalidOperationException("At least one Grid element is required for renumbering.");
            if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
                throw new InvalidOperationException("Grid renumber input contains duplicate element ids.");

            var originalTargets = ResolveOriginalTargets(projectElementsAtStart, ids);
            var prefix = Optional(options.Prefix, nameof(options.Prefix), MaxAffixLength);
            var suffix = Optional(options.Suffix, nameof(options.Suffix), MaxAffixLength);
            if (options.StartIndex < 1 || options.StartIndex > MaxSequenceIndex)
                throw new ArgumentOutOfRangeException(nameof(options.StartIndex), "Grid sequence start index must be between 1 and " + MaxSequenceIndex + ".");
            if (options.NumericPadding < 0 || options.NumericPadding > MaxNumericPadding)
                throw new ArgumentOutOfRangeException(nameof(options.NumericPadding), "Numeric Grid label padding must be between 0 and " + MaxNumericPadding + ".");
            if (!Enum.IsDefined(typeof(GridLabelSequence), options.Sequence))
                throw new ArgumentOutOfRangeException(nameof(options.Sequence));
            if (options.StartIndex > MaxSequenceIndex - (ids.Count - 1))
                throw new InvalidOperationException("Grid label sequence exceeds the supported sequence range.");

            var projectElements = ResolveProjectElements(project);
            var targets = new List<ProjectElement>(ids.Count);
            foreach (var id in ids)
            {
                if (!projectElements.TryGetValue(id, out var element))
                    throw new InvalidOperationException("Grid element does not exist: " + id);
                if (!originalTargets.TryGetValue(id, out var originalElement) ||
                    originalElement == null ||
                    !ReferenceEquals(originalElement, element))
                    throw new InvalidOperationException("Grid renumber target changed while Grid IDs were being enumerated: " + id + ". Retry against the current project state.");
                if (element.Category != ElementCategory.Grid)
                    throw new InvalidOperationException("Element is not a Grid reference: " + element.Id);
                targets.Add(element);
            }

            var targetIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
            var reservedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var grid in projectElements.Values.Where(x => x.Category == ElementCategory.Grid && !targetIds.Contains(x.Id)))
            {
                if (!grid.Properties.TryGetValue(GridLabelKey, out var existing) || string.IsNullOrWhiteSpace(existing)) continue;
                var normalizedExisting = existing.Trim();
                if (!reservedLabels.Add(normalizedExisting))
                    throw new InvalidOperationException("Grid label is duplicated outside the renumber batch: " + normalizedExisting);
            }

            var plannedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plan = new List<GridLabelAssignment>(targets.Count);
            for (var i = 0; i < targets.Count; i++)
            {
                var sequenceIndex = options.StartIndex + i;
                var core = options.Sequence == GridLabelSequence.Alphabetic
                    ? Alphabetic(sequenceIndex)
                    : Numeric(sequenceIndex, options.NumericPadding);
                var label = prefix + core + suffix;
                if (label.Length > MaxLabelLength)
                    throw new InvalidOperationException("Grid label exceeds " + MaxLabelLength + " characters: " + label);
                if (reservedLabels.Contains(label))
                    throw new InvalidOperationException("Grid label already exists outside the renumber batch: " + label);
                if (!plannedLabels.Add(label))
                    throw new InvalidOperationException("Grid renumber plan produced a duplicate label: " + label);
                plan.Add(new GridLabelAssignment(targets[i].Id, label, sequenceIndex));
            }

            var changed = false;
            for (var i = 0; i < targets.Count; i++)
            {
                var element = targets[i];
                var assignment = plan[i];
                changed |= WouldChange(element, GridLabelKey, assignment.Label);
                changed |= WouldChange(element, GridSequenceIndexKey, assignment.SequenceIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (changed)
            {
                project.Touch();
                for (var i = 0; i < targets.Count; i++)
                {
                    var element = targets[i];
                    var assignment = plan[i];
                    SetIfChanged(element, GridLabelKey, assignment.Label);
                    SetIfChanged(element, GridSequenceIndexKey, assignment.SequenceIndex.ToString(CultureInfo.InvariantCulture));
                }
            }
            return plan.AsReadOnly();
        }

        public static string FormatLabel(GridNamingOptions options, int sequenceIndex)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (sequenceIndex < 1 || sequenceIndex > MaxSequenceIndex) throw new ArgumentOutOfRangeException(nameof(sequenceIndex));
            var prefix = Optional(options.Prefix, nameof(options.Prefix), MaxAffixLength);
            var suffix = Optional(options.Suffix, nameof(options.Suffix), MaxAffixLength);
            if (options.NumericPadding < 0 || options.NumericPadding > MaxNumericPadding) throw new ArgumentOutOfRangeException(nameof(options.NumericPadding));
            if (!Enum.IsDefined(typeof(GridLabelSequence), options.Sequence)) throw new ArgumentOutOfRangeException(nameof(options.Sequence));
            var core = options.Sequence == GridLabelSequence.Alphabetic ? Alphabetic(sequenceIndex) : Numeric(sequenceIndex, options.NumericPadding);
            var result = prefix + core + suffix;
            if (result.Length > MaxLabelLength) throw new InvalidOperationException("Grid label exceeds " + MaxLabelLength + " characters.");
            return result;
        }

        private static int? TryGetKnownCount(
            IEnumerable<string> source,
            out bool conflictingKnownCounts,
            out bool invalidNegativeKnownCount)
        {
            conflictingKnownCounts = false;
            invalidNegativeKnownCount = false;
            int? knownCount = null;
            if (source is ICollection<string> collection)
                knownCount = ObserveKnownCount(knownCount, collection.Count, ref conflictingKnownCounts, ref invalidNegativeKnownCount);
            if (source is IReadOnlyCollection<string> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts, ref invalidNegativeKnownCount);
            if (source is ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts, ref invalidNegativeKnownCount);
            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool invalidNegativeKnownCount)
        {
            if (observed < 0)
                invalidNegativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }

        private static Dictionary<string, ProjectElement?> ResolveOriginalTargets(
            IEnumerable<ProjectElement> projectElementsAtStart,
            IEnumerable<string> targetIds)
        {
            var requested = new HashSet<string>(targetIds, StringComparer.OrdinalIgnoreCase);
            var resolved = new Dictionary<string, ProjectElement?>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in projectElementsAtStart)
            {
                if (element == null) continue;
                var elementId = (element.Id ?? string.Empty).Trim();
                if (!requested.Contains(elementId)) continue;
                if (resolved.ContainsKey(elementId))
                {
                    resolved[elementId] = null;
                    continue;
                }
                resolved[elementId] = element;
            }
            return resolved;
        }

        private static Dictionary<string, ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var resolved = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (resolved.ContainsKey(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                resolved.Add(elementId, element);
            }
            return resolved;
        }

        private static bool WouldChange(ProjectElement element, string key, string value)
        {
            return !element.Properties.TryGetValue(key, out var current) || !string.Equals(current, value, StringComparison.Ordinal);
        }

        private static bool SetIfChanged(ProjectElement element, string key, string value)
        {
            if (!WouldChange(element, key, value)) return false;
            element.SetProperty(key, value);
            return true;
        }

        private static string Numeric(int index, int padding)
        {
            return padding <= 0
                ? index.ToString(CultureInfo.InvariantCulture)
                : index.ToString("D" + padding.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        private static string Alphabetic(int index)
        {
            var value = index;
            var chars = new List<char>(8);
            while (value > 0)
            {
                value--;
                chars.Add((char)('A' + value % 26));
                value /= 26;
            }
            chars.Reverse();
            return new string(chars.ToArray());
        }

        private static string Required(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        private static string Optional(string? value, string name, int maxLength)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            try
            {
                XmlConvert.VerifyXmlChars(normalized);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Grid naming prefix/suffix contains characters that are invalid in XML.", name, ex);
            }
            return normalized;
        }
    }
}
