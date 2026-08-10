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
            var items = new List<InterchangeImportResolutionItem>();

            foreach (var zone in source.Zones)
                AddSimple(items, InterchangeIdentityKind.Zone, zone.Id, targetZones.ContainsKey(zone.Id), policy.ZoneCollision);
            foreach (var floor in source.Floors)
                AddSimple(items, InterchangeIdentityKind.Floor, floor.Id, targetFloors.ContainsKey(floor.Id), policy.FloorCollision);

            foreach (var family in source.Families)
            {
                if (!targetFamilies.TryGetValue(family.Id, out var existing))
                {
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
