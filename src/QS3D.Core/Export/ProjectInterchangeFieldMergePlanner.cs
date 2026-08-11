using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum InterchangeFieldPrecedenceChoice
    {
        Unspecified = 0,
        KeepTarget = 1,
        UseSource = 2
    }

    public sealed class ProjectInterchangeFieldMergePolicy
    {
        public InterchangeFieldPrecedenceChoice ZoneName { get; set; }
        public InterchangeFieldPrecedenceChoice FloorName { get; set; }
        public InterchangeFieldPrecedenceChoice FloorElevation { get; set; }
        public InterchangeFieldPrecedenceChoice FamilyName { get; set; }
        public InterchangeFieldPrecedenceChoice FamilyProperties { get; set; }
        public InterchangeFieldPrecedenceChoice ElementFamily { get; set; }
        public InterchangeFieldPrecedenceChoice ElementFloor { get; set; }
        public InterchangeFieldPrecedenceChoice ElementZone { get; set; }
        public InterchangeFieldPrecedenceChoice ElementDependencies { get; set; }
        public InterchangeFieldPrecedenceChoice ElementProperties { get; set; }
        public InterchangeFieldPrecedenceChoice ElementQuantities { get; set; }
    }

    public sealed class InterchangeFieldMergeDecision
    {
        internal InterchangeFieldMergeDecision(
            InterchangeIdentityKind kind,
            string id,
            string field,
            InterchangeFieldPrecedenceChoice choice,
            bool targetHasValue,
            string targetValue,
            bool sourceHasValue,
            string sourceValue,
            bool requiresGeneratedOutputReset)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Field = field ?? string.Empty;
            Choice = choice;
            TargetHasValue = targetHasValue;
            TargetValue = targetValue ?? string.Empty;
            SourceHasValue = sourceHasValue;
            SourceValue = sourceValue ?? string.Empty;
            RequiresGeneratedOutputReset = requiresGeneratedOutputReset;
        }

        public InterchangeIdentityKind Kind { get; }
        public string Id { get; }
        public string Field { get; }
        public InterchangeFieldPrecedenceChoice Choice { get; }
        public bool TargetHasValue { get; }
        public string TargetValue { get; }
        public bool SourceHasValue { get; }
        public string SourceValue { get; }
        public bool RequiresGeneratedOutputReset { get; }
        public bool IsResolved => Choice != InterchangeFieldPrecedenceChoice.Unspecified;
    }

    public sealed class ProjectInterchangeFieldMergePlan
    {
        internal ProjectInterchangeFieldMergePlan(
            string sourceProjectId,
            string targetProjectId,
            int sourceOnlyIdentityCount,
            int collidingIdentityCount,
            IEnumerable<string> blockers,
            IEnumerable<InterchangeFieldMergeDecision> decisions)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            TargetProjectId = targetProjectId ?? string.Empty;
            SourceOnlyIdentityCount = sourceOnlyIdentityCount;
            CollidingIdentityCount = collidingIdentityCount;
            Blockers = (blockers ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
            Decisions = (decisions ?? Enumerable.Empty<InterchangeFieldMergeDecision>())
                .OrderBy(x => x.Kind)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public string SourceProjectId { get; }
        public string TargetProjectId { get; }
        public int SourceOnlyIdentityCount { get; }
        public int CollidingIdentityCount { get; }
        public IReadOnlyList<string> Blockers { get; }
        public IReadOnlyList<InterchangeFieldMergeDecision> Decisions { get; }
        public int UnresolvedDecisionCount => Decisions.Count(x => !x.IsResolved);
        public int SourceChoiceCount => Decisions.Count(x => x.Choice == InterchangeFieldPrecedenceChoice.UseSource);
        public int TargetChoiceCount => Decisions.Count(x => x.Choice == InterchangeFieldPrecedenceChoice.KeepTarget);
        public int GeneratedOutputResetDecisionCount => Decisions.Count(x => x.Choice == InterchangeFieldPrecedenceChoice.UseSource && x.RequiresGeneratedOutputReset);
        public bool HasBlocks => Blockers.Count > 0;
        public bool HasUnresolvedDecisions => UnresolvedDecisionCount > 0;
        public bool CanProceedToMutationDesign => !HasBlocks && !HasUnresolvedDecisions;
        public bool IsPreviewOnly => true;
    }

    /// <summary>
    /// Produces a deterministic, preview-only field precedence plan for same-ID semantic collisions.
    /// It deliberately does not mutate project/native state or imply that generated-output cleanup has occurred.
    /// </summary>
    public static class ProjectInterchangeFieldMergePlanner
    {
        public static ProjectInterchangeFieldMergePlan Plan(
            ProjectState target,
            string json,
            ProjectInterchangeFieldMergePolicy policy)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            ValidatePolicy(policy);

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var targetJson = ProjectInterchangeJsonExporter.Build(target);
            var targetSnapshot = ProjectInterchangeValidatedSnapshotReader.Read(targetJson);
            var blockers = new List<string>();
            var decisions = new List<InterchangeFieldMergeDecision>();
            var sourceOnly = 0;
            var collisions = 0;

            var targetZones = Index(targetSnapshot.Zones, x => x.Id);
            var targetFloors = Index(targetSnapshot.Floors, x => x.Id);
            var targetFamilies = Index(targetSnapshot.Families, x => x.Id);
            var targetElements = Index(targetSnapshot.Elements, x => x.Id);
            var sourceFamilies = Index(source.Families, x => x.Id);

            foreach (var sourceZone in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!targetZones.TryGetValue(sourceZone.Id, out var targetZone))
                {
                    sourceOnly++;
                    continue;
                }

                collisions++;
                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Zone,
                    sourceZone.Id,
                    "name",
                    policy.ZoneName,
                    targetZone.Name,
                    sourceZone.Name,
                    false);
                if (policy.ZoneName == InterchangeFieldPrecedenceChoice.UseSource &&
                    !string.Equals(targetZone.Name, sourceZone.Name, StringComparison.Ordinal) &&
                    targetSnapshot.Zones.Any(x =>
                        !string.Equals(x.Id, sourceZone.Id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((x.Name ?? string.Empty).Trim(), (sourceZone.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                    blockers.Add("Zone " + sourceZone.Id + " cannot use source name '" + sourceZone.Name + "' because another target Zone owns that display name.");
            }

            foreach (var sourceFloor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!targetFloors.TryGetValue(sourceFloor.Id, out var targetFloor))
                {
                    sourceOnly++;
                    continue;
                }

                collisions++;
                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Floor,
                    sourceFloor.Id,
                    "name",
                    policy.FloorName,
                    targetFloor.Name,
                    sourceFloor.Name,
                    false);
                AddNumberDecision(
                    decisions,
                    InterchangeIdentityKind.Floor,
                    sourceFloor.Id,
                    "elevationM",
                    policy.FloorElevation,
                    targetFloor.ElevationM,
                    sourceFloor.ElevationM,
                    true);
                if (policy.FloorName == InterchangeFieldPrecedenceChoice.UseSource &&
                    !string.Equals(targetFloor.Name, sourceFloor.Name, StringComparison.Ordinal) &&
                    targetSnapshot.Floors.Any(x =>
                        !string.Equals(x.Id, sourceFloor.Id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((x.Name ?? string.Empty).Trim(), (sourceFloor.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                    blockers.Add("Floor " + sourceFloor.Id + " cannot use source name '" + sourceFloor.Name + "' because another target Floor owns that display name.");
            }

            foreach (var sourceFamily in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!targetFamilies.TryGetValue(sourceFamily.Id, out var targetFamily))
                {
                    sourceOnly++;
                    continue;
                }

                collisions++;
                if (targetFamily.Category != sourceFamily.Category)
                {
                    blockers.Add("Family " + sourceFamily.Id + " has incompatible source/target categories; field merge cannot change Family category.");
                    continue;
                }

                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Family,
                    sourceFamily.Id,
                    "name",
                    policy.FamilyName,
                    targetFamily.Name,
                    sourceFamily.Name,
                    false);
                AddStringMapDecisions(
                    decisions,
                    InterchangeIdentityKind.Family,
                    sourceFamily.Id,
                    "properties",
                    policy.FamilyProperties,
                    targetFamily.Properties,
                    sourceFamily.Properties,
                    true);
                if (policy.FamilyName == InterchangeFieldPrecedenceChoice.UseSource &&
                    !string.Equals(targetFamily.Name, sourceFamily.Name, StringComparison.Ordinal) &&
                    targetSnapshot.Families.Any(x =>
                        !string.Equals(x.Id, sourceFamily.Id, StringComparison.OrdinalIgnoreCase) &&
                        x.Category == sourceFamily.Category &&
                        string.Equals((x.Name ?? string.Empty).Trim(), (sourceFamily.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                    blockers.Add("Family " + sourceFamily.Id + " cannot use source name '" + sourceFamily.Name + "' because another target " + sourceFamily.Category + " Family owns that display name.");
            }

            foreach (var sourceElement in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!targetElements.TryGetValue(sourceElement.Id, out var targetElement))
                {
                    sourceOnly++;
                    continue;
                }

                collisions++;
                if (targetElement.Category != sourceElement.Category)
                {
                    blockers.Add("Element " + sourceElement.Id + " has incompatible source/target categories; field merge cannot change element category.");
                    continue;
                }

                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "familyId",
                    policy.ElementFamily,
                    targetElement.FamilyId,
                    sourceElement.FamilyId,
                    true,
                    idComparison: true);
                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "floorId",
                    policy.ElementFloor,
                    targetElement.FloorId,
                    sourceElement.FloorId,
                    true,
                    idComparison: true);
                AddStringDecision(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "zoneId",
                    policy.ElementZone,
                    targetElement.ZoneId,
                    sourceElement.ZoneId,
                    true,
                    idComparison: true);
                AddSetDecision(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "dependencies",
                    policy.ElementDependencies,
                    targetElement.Dependencies,
                    sourceElement.Dependencies,
                    true);
                AddStringMapDecisions(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "properties",
                    policy.ElementProperties,
                    targetElement.Properties,
                    sourceElement.Properties,
                    true);
                AddNumberMapDecisions(
                    decisions,
                    InterchangeIdentityKind.Element,
                    sourceElement.Id,
                    "quantities",
                    policy.ElementQuantities,
                    targetElement.Quantities,
                    sourceElement.Quantities,
                    true);
            }

            AddSelectedSourceNameBatchCollisions(blockers, decisions, InterchangeIdentityKind.Zone, _ => string.Empty, "Zone");
            AddSelectedSourceNameBatchCollisions(blockers, decisions, InterchangeIdentityKind.Floor, _ => string.Empty, "Floor");
            AddSelectedSourceNameBatchCollisions(
                blockers,
                decisions,
                InterchangeIdentityKind.Family,
                id => sourceFamilies.TryGetValue(id, out var family) ? family.Category.ToString() : string.Empty,
                "Family");

            return new ProjectInterchangeFieldMergePlan(
                source.Project.Id,
                target.ProjectId,
                sourceOnly,
                collisions,
                blockers,
                decisions);
        }

        private static void ValidatePolicy(ProjectInterchangeFieldMergePolicy policy)
        {
            ValidateChoice(policy.ZoneName, nameof(policy.ZoneName));
            ValidateChoice(policy.FloorName, nameof(policy.FloorName));
            ValidateChoice(policy.FloorElevation, nameof(policy.FloorElevation));
            ValidateChoice(policy.FamilyName, nameof(policy.FamilyName));
            ValidateChoice(policy.FamilyProperties, nameof(policy.FamilyProperties));
            ValidateChoice(policy.ElementFamily, nameof(policy.ElementFamily));
            ValidateChoice(policy.ElementFloor, nameof(policy.ElementFloor));
            ValidateChoice(policy.ElementZone, nameof(policy.ElementZone));
            ValidateChoice(policy.ElementDependencies, nameof(policy.ElementDependencies));
            ValidateChoice(policy.ElementProperties, nameof(policy.ElementProperties));
            ValidateChoice(policy.ElementQuantities, nameof(policy.ElementQuantities));
        }

        private static void ValidateChoice(InterchangeFieldPrecedenceChoice choice, string name)
        {
            if (!Enum.IsDefined(typeof(InterchangeFieldPrecedenceChoice), choice))
                throw new ArgumentOutOfRangeException(name, "Unsupported field precedence choice.");
        }

        private static Dictionary<string, T> Index<T>(IEnumerable<T> source, Func<T, string> idSelector) where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source ?? Enumerable.Empty<T>())
            {
                if (item == null) throw new InvalidOperationException("Field merge snapshot contains a null semantic identity.");
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (id.Length == 0) throw new InvalidOperationException("Field merge snapshot contains an empty semantic identity.");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Field merge snapshot contains duplicate semantic identity: " + id + ".");
                result[id] = item;
            }
            return result;
        }

        private static void AddSelectedSourceNameBatchCollisions(
            ICollection<string> blockers,
            IEnumerable<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            Func<string, string> scopeSelector,
            string label)
        {
            var selected = decisions
                .Where(x =>
                    x.Kind == kind &&
                    string.Equals(x.Field, "name", StringComparison.OrdinalIgnoreCase) &&
                    x.Choice == InterchangeFieldPrecedenceChoice.UseSource &&
                    x.SourceHasValue &&
                    !string.IsNullOrWhiteSpace(x.SourceValue))
                .ToArray();

            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var decision in selected)
            {
                var scope = (scopeSelector(decision.Id) ?? string.Empty).Trim();
                var name = decision.SourceValue.Trim();
                var key = scope + "\u001f" + name;
                if (!owners.TryGetValue(key, out var firstId))
                {
                    owners[key] = decision.Id;
                    continue;
                }
                if (string.Equals(firstId, decision.Id, StringComparison.OrdinalIgnoreCase)) continue;

                blockers.Add(
                    label + " field merge cannot select source display name '" + name + "' for both semantic IDs " +
                    firstId + " and " + decision.Id +
                    (scope.Length == 0 ? "." : " in category " + scope + "."));
            }
        }

        private static void AddStringDecision(
            ICollection<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            string id,
            string field,
            InterchangeFieldPrecedenceChoice choice,
            string targetValue,
            string sourceValue,
            bool requiresGeneratedOutputReset,
            bool idComparison = false)
        {
            var left = targetValue ?? string.Empty;
            var right = sourceValue ?? string.Empty;
            var equal = idComparison
                ? string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase)
                : string.Equals(left, right, StringComparison.Ordinal);
            if (equal) return;
            decisions.Add(new InterchangeFieldMergeDecision(
                kind,
                id,
                field,
                choice,
                true,
                left,
                true,
                right,
                requiresGeneratedOutputReset));
        }

        private static void AddNumberDecision(
            ICollection<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            string id,
            string field,
            InterchangeFieldPrecedenceChoice choice,
            double targetValue,
            double sourceValue,
            bool requiresGeneratedOutputReset)
        {
            if (targetValue.Equals(sourceValue)) return;
            decisions.Add(new InterchangeFieldMergeDecision(
                kind,
                id,
                field,
                choice,
                true,
                targetValue.ToString("R", CultureInfo.InvariantCulture),
                true,
                sourceValue.ToString("R", CultureInfo.InvariantCulture),
                requiresGeneratedOutputReset));
        }

        private static void AddSetDecision(
            ICollection<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            string id,
            string field,
            InterchangeFieldPrecedenceChoice choice,
            IEnumerable<string> targetValues,
            IEnumerable<string> sourceValues,
            bool requiresGeneratedOutputReset)
        {
            var targetSet = new SortedSet<string>((targetValues ?? Enumerable.Empty<string>()).Select(x => (x ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            var sourceSet = new SortedSet<string>((sourceValues ?? Enumerable.Empty<string>()).Select(x => (x ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            if (targetSet.SetEquals(sourceSet)) return;
            decisions.Add(new InterchangeFieldMergeDecision(
                kind,
                id,
                field,
                choice,
                true,
                string.Join(";", targetSet),
                true,
                string.Join(";", sourceSet),
                requiresGeneratedOutputReset));
        }

        private static void AddStringMapDecisions(
            ICollection<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            string id,
            string fieldPrefix,
            InterchangeFieldPrecedenceChoice choice,
            IReadOnlyDictionary<string, string> target,
            IReadOnlyDictionary<string, string> source,
            bool requiresGeneratedOutputReset)
        {
            var keys = new SortedSet<string>(target.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(source.Keys);
            foreach (var key in keys)
            {
                var hasTarget = TryGet(target, key, out var targetValue);
                var hasSource = TryGet(source, key, out var sourceValue);
                if (hasTarget == hasSource && string.Equals(targetValue ?? string.Empty, sourceValue ?? string.Empty, StringComparison.Ordinal))
                    continue;
                decisions.Add(new InterchangeFieldMergeDecision(
                    kind,
                    id,
                    fieldPrefix + "." + key,
                    choice,
                    hasTarget,
                    targetValue ?? string.Empty,
                    hasSource,
                    sourceValue ?? string.Empty,
                    requiresGeneratedOutputReset));
            }
        }

        private static void AddNumberMapDecisions(
            ICollection<InterchangeFieldMergeDecision> decisions,
            InterchangeIdentityKind kind,
            string id,
            string fieldPrefix,
            InterchangeFieldPrecedenceChoice choice,
            IReadOnlyDictionary<string, double> target,
            IReadOnlyDictionary<string, double> source,
            bool requiresGeneratedOutputReset)
        {
            var keys = new SortedSet<string>(target.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(source.Keys);
            foreach (var key in keys)
            {
                var hasTarget = TryGet(target, key, out var targetValue);
                var hasSource = TryGet(source, key, out var sourceValue);
                if (hasTarget == hasSource && (!hasTarget || targetValue.Equals(sourceValue)))
                    continue;
                decisions.Add(new InterchangeFieldMergeDecision(
                    kind,
                    id,
                    fieldPrefix + "." + key,
                    choice,
                    hasTarget,
                    hasTarget ? targetValue.ToString("R", CultureInfo.InvariantCulture) : string.Empty,
                    hasSource,
                    hasSource ? sourceValue.ToString("R", CultureInfo.InvariantCulture) : string.Empty,
                    requiresGeneratedOutputReset));
            }
        }

        private static bool TryGet<T>(IReadOnlyDictionary<string, T> source, string key, out T value)
        {
            foreach (var pair in source)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                value = pair.Value;
                return true;
            }
            value = default(T)!;
            return false;
        }
    }
}
