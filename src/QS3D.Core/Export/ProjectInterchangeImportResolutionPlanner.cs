using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum InterchangeExistingIdentityAction
    {
        Unspecified = 0,
        KeepTarget = 1,
        UseSourceSemanticData = 2
    }

    public enum InterchangeProjectIdPolicy
    {
        Unspecified = 0,
        RequireMatch = 1,
        AllowDifferent = 2
    }

    public enum InterchangeDrawingFingerprintPolicy
    {
        Unspecified = 0,
        RequireMatch = 1,
        AllowDifferentOrUnknown = 2
    }

    public enum InterchangeSourceHandlePolicy
    {
        Unspecified = 0,
        Discard = 1,
        PreserveAsProvenanceOnly = 2
    }

    public enum InterchangeGeneratedOutputResetPolicy
    {
        Unspecified = 0,
        ClearOwnershipAndRequireRebuild = 1
    }

    public enum InterchangeImportResolutionAction
    {
        AddSourceSemanticData = 0,
        KeepTarget = 1,
        UseSourceSemanticData = 2,
        BlockedIncompatible = 3,
        Unresolved = 4
    }

    public sealed class ProjectInterchangeImportPolicy
    {
        public InterchangeExistingIdentityAction ZoneCollision { get; set; }
        public InterchangeExistingIdentityAction FloorCollision { get; set; }
        public InterchangeExistingIdentityAction FamilyCollision { get; set; }
        public InterchangeExistingIdentityAction ElementCollision { get; set; }
        public InterchangeProjectIdPolicy ProjectId { get; set; }
        public InterchangeDrawingFingerprintPolicy DrawingFingerprint { get; set; }
        public InterchangeSourceHandlePolicy SourceHandles { get; set; }
        public InterchangeGeneratedOutputResetPolicy GeneratedOutputReset { get; set; }
    }

    public sealed class InterchangeImportResolutionItem
    {
        internal InterchangeImportResolutionItem(
            InterchangeIdentityKind kind,
            string id,
            InterchangeImportResolutionAction action,
            string reason,
            bool requiresGeneratedOutputReset)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Action = action;
            Reason = reason ?? string.Empty;
            RequiresGeneratedOutputReset = requiresGeneratedOutputReset;
        }

        public InterchangeIdentityKind Kind { get; }
        public string Id { get; }
        public InterchangeImportResolutionAction Action { get; }
        public string Reason { get; }
        public bool RequiresGeneratedOutputReset { get; }
    }

    public sealed class ProjectInterchangeImportResolutionPlan
    {
        internal ProjectInterchangeImportResolutionPlan(
            string sourceProjectId,
            string targetProjectId,
            InterchangeDrawingFingerprintRelation drawingFingerprintRelation,
            InterchangeSourceHandlePolicy sourceHandlePolicy,
            InterchangeGeneratedOutputResetPolicy generatedOutputResetPolicy,
            IEnumerable<string> policyErrors,
            IEnumerable<string> globalBlocks,
            IEnumerable<InterchangeImportResolutionItem> items)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            TargetProjectId = targetProjectId ?? string.Empty;
            DrawingFingerprintRelation = drawingFingerprintRelation;
            SourceHandlePolicy = sourceHandlePolicy;
            GeneratedOutputResetPolicy = generatedOutputResetPolicy;
            PolicyErrors = ReadOnlyStrings(policyErrors);
            GlobalBlocks = ReadOnlyStrings(globalBlocks);
            Items = (items ?? Enumerable.Empty<InterchangeImportResolutionItem>()).ToList().AsReadOnly();
        }

        public string SourceProjectId { get; }
        public string TargetProjectId { get; }
        public InterchangeDrawingFingerprintRelation DrawingFingerprintRelation { get; }
        public InterchangeSourceHandlePolicy SourceHandlePolicy { get; }
        public InterchangeGeneratedOutputResetPolicy GeneratedOutputResetPolicy { get; }
        public IReadOnlyList<string> PolicyErrors { get; }
        public IReadOnlyList<string> GlobalBlocks { get; }
        public IReadOnlyList<InterchangeImportResolutionItem> Items { get; }
        public bool HasUnresolvedPolicy => PolicyErrors.Count > 0 || Items.Any(x => x.Action == InterchangeImportResolutionAction.Unresolved);
        public bool HasBlocks => GlobalBlocks.Count > 0 || Items.Any(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible);
        public bool CanProceedToMutationDesign => !HasUnresolvedPolicy && !HasBlocks;

        private static IReadOnlyList<string> ReadOnlyStrings(IEnumerable<string> source)
        {
            return (source ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }
    }

    public static class ProjectInterchangeImportResolutionPlanner
    {
        private const int MaxPlanItems = 50000;
        private const int MaxZones = 2000;
        private const int MaxFloors = 2000;
        private const int MaxFamilies = 10000;
        private const int ZoneMaxIdLength = 64;
        private const int ZoneMaxNameLength = 120;
        private const int FloorMaxIdLength = 64;
        private const int FloorMaxNameLength = 120;
        private const int FamilyMaxIdLength = 80;
        private const int FamilyMaxNameLength = 160;
        private const int FamilyMaxPropertyKeyLength = 120;
        private const int FamilyMaxPropertyValueLength = 1000;

        public static ProjectInterchangeImportResolutionPlan Plan(
            ProjectState targetProject,
            string json,
            ProjectInterchangeImportPolicy policy)
        {
            if (targetProject == null) throw new ArgumentNullException(nameof(targetProject));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var policyErrors = ValidatePolicy(policy);
            var fingerprintRelation = CompareFingerprint(source.Project.DrawingFingerprint, targetProject.DrawingFingerprint);
            var globalBlocks = new List<string>();

            if (policy.ProjectId == InterchangeProjectIdPolicy.RequireMatch &&
                !string.Equals(source.Project.Id, targetProject.ProjectId, StringComparison.OrdinalIgnoreCase))
                globalBlocks.Add("Project ID policy requires source and target project IDs to match.");

            if (policy.DrawingFingerprint == InterchangeDrawingFingerprintPolicy.RequireMatch &&
                fingerprintRelation != InterchangeDrawingFingerprintRelation.Match)
                globalBlocks.Add("Drawing fingerprint policy requires an explicit source/target fingerprint match.");

            var targetZones = UniqueIndex(targetProject.Zones, x => x.Id, "target Zone");
            var targetFloors = UniqueIndex(targetProject.Floors, x => x.Id, "target Floor");
            var targetFamilies = UniqueIndex(targetProject.Families, x => x.Id, "target Family");
            var targetElements = UniqueIndex(targetProject.Elements, x => x.Id, "target element");
            var targetZoneNames = UniqueOwnerIndex(targetProject.Zones, x => x.Name, x => x.Id, "target Zone name");
            var targetFloorNames = UniqueOwnerIndex(targetProject.Floors, x => x.Name, x => x.Id, "target Floor name");
            var targetFamilyNames = UniqueOwnerIndex(targetProject.Families, x => FamilyNameKey(x.Category, x.Name), x => x.Id, "target same-category Family name");
            var sourceDuplicateZoneNames = DuplicateOwnerKeys(source.Zones, x => x.Name, x => x.Id);
            var sourceDuplicateFloorNames = DuplicateOwnerKeys(source.Floors, x => x.Name, x => x.Id);
            var sourceDuplicateFamilyNames = DuplicateOwnerKeys(source.Families, x => FamilyNameKey(x.Category, x.Name), x => x.Id);
            var items = new List<InterchangeImportResolutionItem>();

            foreach (var zone in source.Zones)
            {
                var zoneName = RequiredSnapshotValue(zone.Name, "Zone name", zone.Id);
                var exists = targetZones.ContainsKey(zone.Id);
                var appliesSource = !exists || policy.ZoneCollision == InterchangeExistingIdentityAction.UseSourceSemanticData;
                if (appliesSource && !CatalogIdentityFitsRuntime(zone.Id, zoneName, exists, ZoneMaxIdLength, ZoneMaxNameLength, out var runtimeReason))
                {
                    AddRuntimeCompatibilityBlock(items, InterchangeIdentityKind.Zone, zone.Id, runtimeReason);
                    continue;
                }
                if (sourceDuplicateZoneNames.Contains(zoneName.Trim()) && appliesSource)
                {
                    AddSourceBatchNameCollision(items, InterchangeIdentityKind.Zone, zone.Id, "Zone name", zoneName);
                    continue;
                }
                if (NameOwnedByDifferentIdentity(targetZoneNames, zoneName, zone.Id) && appliesSource)
                {
                    AddNameCollision(items, InterchangeIdentityKind.Zone, zone.Id, "Zone name", zoneName);
                    continue;
                }
                AddSimple(items, InterchangeIdentityKind.Zone, zone.Id, exists, policy.ZoneCollision);
            }

            foreach (var floor in source.Floors)
            {
                var floorName = RequiredSnapshotValue(floor.Name, "Floor name", floor.Id);
                var exists = targetFloors.ContainsKey(floor.Id);
                var appliesSource = !exists || policy.FloorCollision == InterchangeExistingIdentityAction.UseSourceSemanticData;
                if (appliesSource && !CatalogIdentityFitsRuntime(floor.Id, floorName, exists, FloorMaxIdLength, FloorMaxNameLength, out var runtimeReason))
                {
                    AddRuntimeCompatibilityBlock(items, InterchangeIdentityKind.Floor, floor.Id, runtimeReason);
                    continue;
                }
                if (sourceDuplicateFloorNames.Contains(floorName.Trim()) && appliesSource)
                {
                    AddSourceBatchNameCollision(items, InterchangeIdentityKind.Floor, floor.Id, "Floor name", floorName);
                    continue;
                }
                if (NameOwnedByDifferentIdentity(targetFloorNames, floorName, floor.Id) && appliesSource)
                {
                    AddNameCollision(items, InterchangeIdentityKind.Floor, floor.Id, "Floor name", floorName);
                    continue;
                }
                AddSimple(items, InterchangeIdentityKind.Floor, floor.Id, exists, policy.FloorCollision);
            }

            foreach (var family in source.Families)
            {
                var nameKey = FamilyNameKey(family.Category, family.Name);
                var exists = targetFamilies.TryGetValue(family.Id, out var existing);
                var appliesSource = !exists || policy.FamilyCollision == InterchangeExistingIdentityAction.UseSourceSemanticData;
                if (appliesSource && !CatalogIdentityFitsRuntime(family.Id, family.Name, exists, FamilyMaxIdLength, FamilyMaxNameLength, out var runtimeReason))
                {
                    AddRuntimeCompatibilityBlock(items, InterchangeIdentityKind.Family, family.Id, runtimeReason);
                    continue;
                }
                if (appliesSource && !FamilyPropertiesFitRuntime(family, out runtimeReason))
                {
                    AddRuntimeCompatibilityBlock(items, InterchangeIdentityKind.Family, family.Id, runtimeReason);
                    continue;
                }
                if (sourceDuplicateFamilyNames.Contains(nameKey) && appliesSource)
                {
                    AddSourceBatchNameCollision(items, InterchangeIdentityKind.Family, family.Id, family.Category + " Family name", family.Name);
                    continue;
                }

                if (!exists)
                {
                    if (NameOwnedByDifferentIdentity(targetFamilyNames, nameKey, family.Id))
                    {
                        AddNameCollision(items, InterchangeIdentityKind.Family, family.Id, family.Category + " Family name", family.Name);
                        continue;
                    }

                    Add(items, new InterchangeImportResolutionItem(
                        InterchangeIdentityKind.Family,
                        family.Id,
                        InterchangeImportResolutionAction.AddSourceSemanticData,
                        "No target Family uses this semantic ID.",
                        false));
                    continue;
                }

                if (existing.Category != family.Category)
                {
                    Add(items, new InterchangeImportResolutionItem(
                        InterchangeIdentityKind.Family,
                        family.Id,
                        InterchangeImportResolutionAction.BlockedIncompatible,
                        "Source/target Family categories differ for the same semantic ID; rename/remap policy is not defined.",
                        false));
                    continue;
                }

                if (policy.FamilyCollision == InterchangeExistingIdentityAction.UseSourceSemanticData &&
                    NameOwnedByDifferentIdentity(targetFamilyNames, nameKey, family.Id))
                {
                    AddNameCollision(items, InterchangeIdentityKind.Family, family.Id, family.Category + " Family name", family.Name);
                    continue;
                }

                AddCollision(items, InterchangeIdentityKind.Family, family.Id, policy.FamilyCollision, false);
            }

            foreach (var element in source.Elements)
            {
                if (!targetElements.TryGetValue(element.Id, out var existing))
                {
                    Add(items, new InterchangeImportResolutionItem(
                        InterchangeIdentityKind.Element,
                        element.Id,
                        InterchangeImportResolutionAction.AddSourceSemanticData,
                        "No target semantic element uses this ID.",
                        false));
                    continue;
                }

                if (existing.Category != element.Category)
                {
                    Add(items, new InterchangeImportResolutionItem(
                        InterchangeIdentityKind.Element,
                        element.Id,
                        InterchangeImportResolutionAction.BlockedIncompatible,
                        "Source/target element categories differ for the same semantic ID; automatic replacement is prohibited.",
                        false));
                    continue;
                }

                AddCollision(items, InterchangeIdentityKind.Element, element.Id, policy.ElementCollision, true);
            }

            AddCapacityBlock(globalBlocks, "Zone", targetProject.Zones.Count, Count(items, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.AddSourceSemanticData), MaxZones);
            AddCapacityBlock(globalBlocks, "Floor", targetProject.Floors.Count, Count(items, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.AddSourceSemanticData), MaxFloors);
            AddCapacityBlock(globalBlocks, "Family", targetProject.Families.Count, Count(items, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.AddSourceSemanticData), MaxFamilies);

            var replacingExistingElement = items.Any(x =>
                x.Kind == InterchangeIdentityKind.Element &&
                x.Action == InterchangeImportResolutionAction.UseSourceSemanticData &&
                x.RequiresGeneratedOutputReset);
            if (replacingExistingElement &&
                policy.GeneratedOutputReset != InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild)
                policyErrors.Add("Replacing existing element semantic data requires GeneratedOutputReset=ClearOwnershipAndRequireRebuild; keeping existing generated/native ownership is not an allowed plan.");

            return new ProjectInterchangeImportResolutionPlan(
                source.Project.Id,
                targetProject.ProjectId,
                fingerprintRelation,
                policy.SourceHandles,
                policy.GeneratedOutputReset,
                policyErrors,
                globalBlocks,
                items);
        }

        private static List<string> ValidatePolicy(ProjectInterchangeImportPolicy policy)
        {
            var errors = new List<string>();
            ValidateEnum(policy.ZoneCollision, nameof(policy.ZoneCollision), errors);
            ValidateEnum(policy.FloorCollision, nameof(policy.FloorCollision), errors);
            ValidateEnum(policy.FamilyCollision, nameof(policy.FamilyCollision), errors);
            ValidateEnum(policy.ElementCollision, nameof(policy.ElementCollision), errors);
            ValidateEnum(policy.ProjectId, nameof(policy.ProjectId), errors);
            ValidateEnum(policy.DrawingFingerprint, nameof(policy.DrawingFingerprint), errors);
            ValidateEnum(policy.SourceHandles, nameof(policy.SourceHandles), errors);
            ValidateEnum(policy.GeneratedOutputReset, nameof(policy.GeneratedOutputReset), errors, allowUnspecified: true);
            return errors;
        }

        private static void ValidateEnum<T>(T value, string name, ICollection<string> errors, bool allowUnspecified = false) where T : struct
        {
            var type = typeof(T);
            if (!type.IsEnum || !Enum.IsDefined(type, value))
            {
                errors.Add(name + " contains an unsupported policy value.");
                return;
            }

            if (!allowUnspecified && Convert.ToInt32(value) == 0)
                errors.Add(name + " must be explicitly selected; import planning has no implicit collision/provenance default.");
        }

        private static bool CatalogIdentityFitsRuntime(
            string sourceId,
            string? sourceName,
            bool existingTargetIdentity,
            int maxIdLength,
            int maxNameLength,
            out string reason)
        {
            var id = (sourceId ?? string.Empty).Trim();
            var name = (sourceName ?? string.Empty).Trim();
            if (!existingTargetIdentity && id.Length > maxIdLength)
            {
                reason = "Source ID length " + id.Length + " exceeds target runtime limit " + maxIdLength + "; this policy does not remap IDs.";
                return false;
            }
            if (name.Length > maxNameLength)
            {
                reason = "Source display-name length " + name.Length + " exceeds target runtime limit " + maxNameLength + "; this policy does not truncate semantic data.";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private static bool FamilyPropertiesFitRuntime(InterchangeFamilySnapshot family, out string reason)
        {
            foreach (var property in family.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = (property.Key ?? string.Empty).Trim();
                var value = property.Value ?? string.Empty;
                if (key.Length > FamilyMaxPropertyKeyLength)
                {
                    reason = "Source Family property key length " + key.Length + " exceeds target runtime limit " + FamilyMaxPropertyKeyLength + "; this policy does not truncate semantic data.";
                    return false;
                }
                if (value.Length > FamilyMaxPropertyValueLength)
                {
                    reason = "Source Family property value length " + value.Length + " exceeds target runtime limit " + FamilyMaxPropertyValueLength + "; this policy does not truncate semantic data.";
                    return false;
                }
            }
            reason = string.Empty;
            return true;
        }

        private static string RequiredSnapshotValue(string? value, string label, string ownerId)
        {
            if (value == null || value.Length == 0)
                throw new InvalidOperationException(
                    "Validated interchange snapshot contains an empty " + label + " for source identity " +
                    (ownerId ?? string.Empty) + ".");
            return value;
        }

        private static void AddCapacityBlock(ICollection<string> globalBlocks, string kind, int targetCount, int addCount, int maxCount)
        {
            var combined = checked(targetCount + addCount);
            if (combined <= maxCount) return;
            globalBlocks.Add(
                "Import policy would produce " + combined + " " + kind + " identities, exceeding target runtime limit " + maxCount + ".");
        }

        private static int Count(
            IEnumerable<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            InterchangeImportResolutionAction action) =>
            items.Count(x => x.Kind == kind && x.Action == action);

        private static void AddSimple(
            ICollection<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            string id,
            bool exists,
            InterchangeExistingIdentityAction collisionAction)
        {
            if (!exists)
            {
                Add(items, new InterchangeImportResolutionItem(
                    kind,
                    id,
                    InterchangeImportResolutionAction.AddSourceSemanticData,
                    "No target semantic definition uses this ID.",
                    false));
                return;
            }

            AddCollision(items, kind, id, collisionAction, false);
        }

        private static void AddCollision(
            ICollection<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            string id,
            InterchangeExistingIdentityAction action,
            bool resetGeneratedOutputWhenUsingSource)
        {
            InterchangeImportResolutionAction resolved;
            string reason;
            switch (action)
            {
                case InterchangeExistingIdentityAction.KeepTarget:
                    resolved = InterchangeImportResolutionAction.KeepTarget;
                    reason = "Explicit policy keeps the existing target semantic identity.";
                    break;
                case InterchangeExistingIdentityAction.UseSourceSemanticData:
                    resolved = InterchangeImportResolutionAction.UseSourceSemanticData;
                    reason = resetGeneratedOutputWhenUsingSource
                        ? "Explicit policy chooses source semantic data; any existing generated/native output must be cleared from ownership and rebuilt before it can be trusted."
                        : "Explicit policy chooses source semantic data for this existing identity.";
                    break;
                default:
                    resolved = InterchangeImportResolutionAction.Unresolved;
                    reason = "Collision policy is unresolved. No executable target/source choice has been selected.";
                    break;
            }

            Add(items, new InterchangeImportResolutionItem(
                kind,
                id,
                resolved,
                reason,
                resetGeneratedOutputWhenUsingSource && action == InterchangeExistingIdentityAction.UseSourceSemanticData));
        }

        private static void AddRuntimeCompatibilityBlock(
            ICollection<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            string id,
            string reason)
        {
            Add(items, new InterchangeImportResolutionItem(
                kind,
                id,
                InterchangeImportResolutionAction.BlockedIncompatible,
                "Source semantic data is not compatible with target runtime contracts: " + reason,
                false));
        }

        private static void AddNameCollision(
            ICollection<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            string id,
            string label,
            string? name)
        {
            Add(items, new InterchangeImportResolutionItem(
                kind,
                id,
                InterchangeImportResolutionAction.BlockedIncompatible,
                "Source " + label + " '" + (name ?? string.Empty).Trim() + "' is already owned by a different target semantic ID; rename/remap policy is not defined.",
                false));
        }

        private static void AddSourceBatchNameCollision(
            ICollection<InterchangeImportResolutionItem> items,
            InterchangeIdentityKind kind,
            string id,
            string label,
            string? name)
        {
            Add(items, new InterchangeImportResolutionItem(
                kind,
                id,
                InterchangeImportResolutionAction.BlockedIncompatible,
                "Source " + label + " '" + (name ?? string.Empty).Trim() + "' is shared by multiple source semantic IDs; this import mode has no source-name remap policy.",
                false));
        }

        private static void Add(ICollection<InterchangeImportResolutionItem> items, InterchangeImportResolutionItem item)
        {
            if (items.Count >= MaxPlanItems)
                throw new InvalidOperationException("Interchange import resolution exceeds the supported " + MaxPlanItems + " identity limit.");
            items.Add(item);
        }

        private static InterchangeDrawingFingerprintRelation CompareFingerprint(string source, string target)
        {
            var left = (source ?? string.Empty).Trim();
            var right = (target ?? string.Empty).Trim();
            if (left.Length == 0 || right.Length == 0) return InterchangeDrawingFingerprintRelation.Unknown;
            return string.Equals(left, right, StringComparison.Ordinal)
                ? InterchangeDrawingFingerprintRelation.Match
                : InterchangeDrawingFingerprintRelation.Different;
        }

        private static string FamilyNameKey(ElementCategory category, string? name) =>
            category + "\u001f" + (name ?? string.Empty).Trim();

        private static bool NameOwnedByDifferentIdentity(
            IReadOnlyDictionary<string, string> owners,
            string? key,
            string sourceId)
        {
            var normalizedKey = (key ?? string.Empty).Trim();
            if (!owners.TryGetValue(normalizedKey, out var ownerId)) return false;
            return !string.Equals(ownerId, (sourceId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static HashSet<string> DuplicateOwnerKeys<T>(
            IEnumerable<T> source,
            Func<T, string> keySelector,
            Func<T, string> idSelector) where T : class
        {
            var firstOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null) continue;
                var key = (keySelector(item) ?? string.Empty).Trim();
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (key.Length == 0 || id.Length == 0) continue;
                if (firstOwners.TryGetValue(key, out var existingId))
                {
                    if (!string.Equals(existingId, id, StringComparison.OrdinalIgnoreCase)) duplicates.Add(key);
                }
                else
                {
                    firstOwners[key] = id;
                }
            }
            return duplicates;
        }

        private static Dictionary<string, string> UniqueOwnerIndex<T>(
            IEnumerable<T> source,
            Func<T, string> keySelector,
            Func<T, string> idSelector,
            string label) where T : class
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidOperationException("Project contains a null " + label + " entry.");
                var key = (keySelector(item) ?? string.Empty).Trim();
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (key.Length == 0) throw new InvalidOperationException("Project contains an empty " + label + ".");
                if (id.Length == 0) throw new InvalidOperationException("Project contains an empty semantic ID while indexing " + label + ".");
                if (result.TryGetValue(key, out var existingId) && !string.Equals(existingId, id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Project contains duplicate " + label + ": " + key + ". Import resolution refuses ambiguous target naming.");
                result[key] = id;
            }
            return result;
        }

        private static Dictionary<string, T> UniqueIndex<T>(IEnumerable<T> source, Func<T, string> idSelector, string label) where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidOperationException("Project contains a null " + label + " entry.");
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (id.Length == 0) throw new InvalidOperationException("Project contains an empty " + label + " ID.");
                if (result.ContainsKey(id))
                    throw new InvalidOperationException("Project contains duplicate " + label + " ID: " + id + ". Import resolution refuses ambiguous target identity.");
                result[id] = item;
            }
            return result;
        }
    }
}
