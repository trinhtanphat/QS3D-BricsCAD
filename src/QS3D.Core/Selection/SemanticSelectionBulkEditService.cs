using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Selection
{
    public sealed class SemanticSelectionBulkEditResult
    {
        internal SemanticSelectionBulkEditResult(string operation, string target, int selectedCount, IReadOnlyList<string> changedElementIds)
        {
            Operation = operation;
            Target = target;
            SelectedCount = selectedCount;
            ChangedElementIds = new List<string>(changedElementIds).AsReadOnly();
        }

        public string Operation { get; }
        public string Target { get; }
        public int SelectedCount { get; }
        public int ChangedCount => ChangedElementIds.Count;
        public IReadOnlyList<string> ChangedElementIds { get; }
    }

    public sealed class SemanticSelectionBulkEditService
    {
        public SemanticSelectionBulkEditResult SetProperty(ProjectState project, IEnumerable<string> elementIds, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var key = SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName);
            var selection = ResolveSelection(project, elementIds);
            var next = value ?? string.Empty;
            var updates = new List<ProjectElement>();

            foreach (var element in selection.Elements)
            {
                if (element.Properties.TryGetValue(key, out var current) &&
                    string.Equals(current ?? string.Empty, next, StringComparison.Ordinal))
                    continue;
                updates.Add(element);
            }

            if (updates.Count == 0) return Result("SetProperty", key, selection.Count, updates);
            return ProjectSemanticMutationExecutor.Execute(project, "selection.bulk.set-property", () =>
            {
                foreach (var element in updates) element.SetProperty(key, next);
                project.Touch();
                return Result("SetProperty", key, selection.Count, updates);
            });
        }

        public SemanticSelectionBulkEditResult MultiplyNumericProperty(ProjectState project, IEnumerable<string> elementIds, string propertyName, double factor)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (double.IsNaN(factor) || double.IsInfinity(factor)) throw new ArgumentOutOfRangeException(nameof(factor));
            var key = SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName);
            var selection = ResolveSelection(project, elementIds);
            var updates = new List<PendingValue>();

            foreach (var element in selection.Elements)
            {
                var current = EffectivePropertyValue(project, element, key, out var present);
                if (!present)
                    throw new InvalidOperationException("Selected element is missing numeric property " + key + ": " + element.Id + ".");
                if (!double.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || double.IsNaN(number) || double.IsInfinity(number))
                    throw new FormatException("Invalid numeric property " + key + " on " + element.Id + ": " + current);
                if (number == 0d && HasNonZeroSignificand(current))
                    throw new InvalidOperationException("Semantic selection numeric property underflow for " + element.Id + "/" + key + ": " + current);
                var next = number * factor;
                if (double.IsNaN(next) || double.IsInfinity(next))
                    throw new OverflowException("Bulk property multiplication overflow for " + element.Id + "/" + key + ".");
                if (next == 0d && number != 0d && factor != 0d)
                    throw new InvalidOperationException("Semantic selection property multiplication underflow for " + element.Id + "/" + key + ".");
                if (next.Equals(number) && number != 0d && factor != 1d)
                    throw new InvalidOperationException("Semantic selection property multiplication lost a non-unit factor at floating-point precision for " + element.Id + "/" + key + ".");
                if (next.Equals(number)) continue;
                var formatted = next.ToString("R", CultureInfo.InvariantCulture);
                updates.Add(new PendingValue(element, formatted));
            }

            if (updates.Count == 0)
                return new SemanticSelectionBulkEditResult("MultiplyNumericProperty", key, selection.Count, Array.Empty<string>());

            return ProjectSemanticMutationExecutor.Execute(project, "selection.bulk.multiply-numeric-property", () =>
            {
                foreach (var update in updates) update.Element.SetProperty(key, update.Value);
                project.Touch();
                return new SemanticSelectionBulkEditResult(
                    "MultiplyNumericProperty",
                    key,
                    selection.Count,
                    updates.Select(x => x.Element.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray());
            });
        }

        public SemanticSelectionBulkEditResult AssignFamily(ProjectState project, IEnumerable<string> elementIds, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(familyId)) throw new ArgumentException("Family id is required.", nameof(familyId));
            var selection = ResolveSelection(project, elementIds);
            var normalizedFamilyId = familyId.Trim();
            var family = project.FindFamily(normalizedFamilyId) ?? throw new KeyNotFoundException("Unknown family: " + normalizedFamilyId);
            if (!Enum.IsDefined(typeof(ElementCategory), family.Category))
                throw new InvalidOperationException("Target family has an undefined category: " + family.Id + ".");

            foreach (var element in selection.Elements)
                if (element.Category != family.Category)
                    throw new InvalidOperationException("Cannot assign family " + family.Id + " to mixed/incompatible selection; element " + element.Id + " is " + element.Category + " while family is " + family.Category + ".");

            var changedIds = selection.Elements
                .Where(x => !string.Equals((x.FamilyId ?? string.Empty).Trim(), family.Id, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();

            if (changedIds.Length > 0)
                new BulkEditService().AssignFamily(project, selection.ElementIds, family.Id);
            return new SemanticSelectionBulkEditResult("AssignFamily", family.Id, selection.Count, changedIds);
        }

        private static Selection ResolveSelection(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var inspection = SemanticSelectionInspector.Inspect(project, elementIds);
            var elements = inspection.ElementIds.Select(id => project.FindElement(id) ?? throw new InvalidOperationException("Selected element disappeared during bulk edit preflight: " + id + ".")).ToArray();
            return new Selection(inspection.ElementIds, elements);
        }

        private static string EffectivePropertyValue(ProjectState project, ProjectElement element, string key, out bool present)
        {
            if (element.Properties.TryGetValue(key, out var instanceValue))
            {
                present = true;
                return instanceValue ?? string.Empty;
            }

            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length > 0)
            {
                var family = project.FindFamily(familyId) ?? throw new InvalidOperationException("Selected element references missing family id: " + element.Id + "/" + familyId + ".");
                if (family.Properties.TryGetValue(key, out var familyValue))
                {
                    present = true;
                    return familyValue ?? string.Empty;
                }
            }

            present = false;
            return string.Empty;
        }

        private static bool HasNonZeroSignificand(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (character == 'e' || character == 'E') break;
                if (character >= '1' && character <= '9') return true;
            }
            return false;
        }

        private static SemanticSelectionBulkEditResult Result(string operation, string target, int selectedCount, IEnumerable<ProjectElement> changed)
        {
            return new SemanticSelectionBulkEditResult(
                operation,
                target,
                selectedCount,
                changed.Select(x => x.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray());
        }

        private sealed class Selection
        {
            public Selection(IReadOnlyList<string> elementIds, IReadOnlyList<ProjectElement> elements)
            {
                ElementIds = elementIds;
                Elements = elements;
            }

            public int Count => Elements.Count;
            public IReadOnlyList<string> ElementIds { get; }
            public IReadOnlyList<ProjectElement> Elements { get; }
        }

        private sealed class PendingValue
        {
            public PendingValue(ProjectElement element, string value)
            {
                Element = element;
                Value = value;
            }

            public ProjectElement Element { get; }
            public string Value { get; }
        }
    }
}
