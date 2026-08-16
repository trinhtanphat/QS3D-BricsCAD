using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    /// <summary>
    /// Semantic contract for the special slab-opening Family. Slab openings reuse the existing
    /// WallOpening category so persistence/schema remain stable, while HostSlabId keeps slab
    /// hosting distinct from the wall-opening HostWallId path.
    /// </summary>
    public static class SlabOpeningContract
    {
        public const string FamilyKey = "slabOpen";
        public const string ContractKey = "SlabOpeningContract";
        public const string ContractValue = "slabOpen";
        public const string HostSlabIdKey = "HostSlabId";
        public const string BooleanClearanceMKey = "BooleanClearanceM";
        public const string AppliedSolidHandleKey = "SlabOpeningAppliedSolidHandle";
        public const string AppliedFingerprintKey = "SlabOpeningAppliedFingerprint";

        public static bool IsSlabOpenFamily(ProjectFamily? family)
        {
            if (family == null || family.Category != ElementCategory.WallOpening) return false;
            return string.Equals(family.Id, FamilyKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(family.Name, FamilyKey, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSlabOpening(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Category != ElementCategory.WallOpening) return false;
            if (!IsSlabOpenFamily(project.FindFamily(element.FamilyId))) return false;
            return element.Properties.TryGetValue(ContractKey, out var contract) &&
                   string.Equals((contract ?? string.Empty).Trim(), ContractValue, StringComparison.OrdinalIgnoreCase) &&
                   TryGetHostSlabId(element, out _);
        }

        public static void Bind(ProjectState project, ProjectElement opening, ProjectElement hostSlab)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (hostSlab == null) throw new ArgumentNullException(nameof(hostSlab));

            var elements = ResolveProjectElements(project);
            EnsureOwnedElement(elements, opening, "slabOpen opening");
            EnsureOwnedElement(elements, hostSlab, "slabOpen host Slab");

            if (opening.Category != ElementCategory.WallOpening)
                throw new InvalidOperationException("slabOpen semantic element must use WallOpening category.");
            if (!IsSlabOpenFamily(project.FindFamily(opening.FamilyId)))
                throw new InvalidOperationException("slabOpen semantic element must belong to the exact slabOpen Family.");
            if (hostSlab.Category != ElementCategory.Slab)
                throw new InvalidOperationException("slabOpen host must be a semantic Slab.");
            if (string.Equals(opening.Id, hostSlab.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("slabOpen cannot host itself.");

            opening.SetProperty(ContractKey, ContractValue);
            opening.SetProperty(HostSlabIdKey, hostSlab.Id);
            if (!opening.DependsOn.Any(x => string.Equals(x, hostSlab.Id, StringComparison.OrdinalIgnoreCase)))
            {
                opening.DependsOn.Add(hostSlab.Id);
                opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity);
            }
        }

        public static bool TryGetHostSlabId(ProjectElement opening, out string hostSlabId)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            hostSlabId = string.Empty;
            if (!opening.Properties.TryGetValue(HostSlabIdKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            hostSlabId = raw.Trim();
            return true;
        }

        public static string RequireHostSlabId(ProjectElement opening)
        {
            if (!TryGetHostSlabId(opening, out var hostSlabId))
                throw new InvalidOperationException("slabOpen element is missing HostSlabId.");
            return hostSlabId;
        }

        private static IReadOnlyDictionary<string, ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var elements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (elements.ContainsKey(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                elements.Add(elementId, element);
            }
            return elements;
        }

        private static void EnsureOwnedElement(
            IReadOnlyDictionary<string, ProjectElement> elements,
            ProjectElement supplied,
            string label)
        {
            if (!elements.TryGetValue(supplied.Id, out var owned))
                throw new InvalidOperationException(label + " does not belong to the project: " + supplied.Id);
            if (!ReferenceEquals(owned, supplied))
                throw new InvalidOperationException(label + " instance does not belong to the project: " + supplied.Id);
        }
    }
}
