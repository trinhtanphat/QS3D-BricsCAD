using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum ProjectInterchangeImportExecutionMode
    {
        AppendOnly = 0,
        KeepTarget = 1,
        ImportAsNew = 2,
        UseSourceSemanticData = 3
    }

    public sealed class ProjectInterchangeImportRequest
    {
        public ProjectInterchangeImportExecutionMode Mode { get; set; }
        public bool PreserveSourceHandleProvenance { get; set; }
    }

    public sealed class ProjectInterchangeImportCoordinatorPlan
    {
        private readonly ProjectInterchangeUseSourceSemanticPlan? _useSourceSemanticPlan;

        internal ProjectInterchangeImportCoordinatorPlan(
            ProjectInterchangeImportExecutionMode mode,
            bool preserveSourceHandleProvenance,
            string sourceProjectId,
            int validationWarnings,
            int semanticIdentitiesToAdd,
            int targetIdentitiesToKeep,
            int semanticIdentitiesToReplace,
            int idsToRemap,
            int namesToRemap,
            int sourceHandleCount,
            int blockerCount,
            IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> nativeCleanupRequirements,
            ProjectInterchangeUseSourceSemanticPlan? useSourceSemanticPlan)
        {
            Mode = mode;
            PreserveSourceHandleProvenance = preserveSourceHandleProvenance;
            SourceProjectId = sourceProjectId ?? string.Empty;
            ValidationWarnings = validationWarnings;
            SemanticIdentitiesToAdd = semanticIdentitiesToAdd;
            TargetIdentitiesToKeep = targetIdentitiesToKeep;
            SemanticIdentitiesToReplace = semanticIdentitiesToReplace;
            IdsToRemap = idsToRemap;
            NamesToRemap = namesToRemap;
            SourceHandleCount = sourceHandleCount;
            BlockerCount = blockerCount;
            NativeCleanupRequirements = nativeCleanupRequirements ?? throw new ArgumentNullException(nameof(nativeCleanupRequirements));
            _useSourceSemanticPlan = useSourceSemanticPlan;

            if (Mode == ProjectInterchangeImportExecutionMode.UseSourceSemanticData && _useSourceSemanticPlan == null)
                throw new InvalidOperationException("UseSource coordinator plan requires its canonical semantic plan.");
            if (Mode != ProjectInterchangeImportExecutionMode.UseSourceSemanticData && _useSourceSemanticPlan != null)
                throw new InvalidOperationException("Only UseSource coordinator plans may retain a canonical UseSource semantic plan.");

            var cleanupIds = new List<string>();
            foreach (var requirement in NativeCleanupRequirements)
            {
                if (requirement == null)
                    throw new InvalidOperationException("Coordinator cleanup requirements cannot contain null entries.");
                cleanupIds.Add(requirement.ElementId);
            }
            cleanupIds.Sort(StringComparer.OrdinalIgnoreCase);
            NativeCleanupElementIds = new ReadOnlyCollection<string>(cleanupIds);
        }

        public ProjectInterchangeImportExecutionMode Mode { get; }
        public bool PreserveSourceHandleProvenance { get; }
        public string SourceProjectId { get; }
        public int ValidationWarnings { get; }
        public int SemanticIdentitiesToAdd { get; }
        public int TargetIdentitiesToKeep { get; }
        public int SemanticIdentitiesToReplace { get; }
        public int IdsToRemap { get; }
        public int NamesToRemap { get; }
        public int SourceHandleCount { get; }
        public int BlockerCount { get; }
        public IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> NativeCleanupRequirements { get; }
        public IReadOnlyList<string> NativeCleanupElementIds { get; }
        public bool RequiresNativeCleanup => NativeCleanupRequirements.Count > 0;
        public bool CanExecute => BlockerCount == 0;

        public ProjectInterchangeNativeCleanupAuthorization CreateNativeCleanupAuthorization()
        {
            if (Mode != ProjectInterchangeImportExecutionMode.UseSourceSemanticData || _useSourceSemanticPlan == null)
                throw new InvalidOperationException("Native cleanup authorization can be created only from a reviewed UseSourceSemanticData coordinator plan.");
            return ProjectInterchangeNativeCleanupAuthorization.ForPlan(_useSourceSemanticPlan);
        }
    }

    public sealed class ProjectInterchangeImportCoordinatorResult
    {
        internal ProjectInterchangeImportCoordinatorResult(ProjectInterchangeImportCoordinatorPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            Mode = plan.Mode;
            PreserveSourceHandleProvenance = plan.PreserveSourceHandleProvenance;
            SourceProjectId = plan.SourceProjectId;
            SemanticIdentitiesAdded = plan.SemanticIdentitiesToAdd;
            TargetIdentitiesKept = plan.TargetIdentitiesToKeep;
            SemanticIdentitiesReplaced = plan.SemanticIdentitiesToReplace;
            IdsRemapped = plan.IdsToRemap;
            NamesRemapped = plan.NamesToRemap;
            SourceHandlesPreservedAsProvenance = plan.PreserveSourceHandleProvenance ? plan.SourceHandleCount : 0;
            NativeCleanupElementsAuthorized = plan.RequiresNativeCleanup ? plan.NativeCleanupElementIds.Count : 0;
        }

        public ProjectInterchangeImportExecutionMode Mode { get; }
        public bool PreserveSourceHandleProvenance { get; }
        public string SourceProjectId { get; }
        public int SemanticIdentitiesAdded { get; }
        public int TargetIdentitiesKept { get; }
        public int SemanticIdentitiesReplaced { get; }
        public int IdsRemapped { get; }
        public int NamesRemapped { get; }
        public int SourceHandlesPreservedAsProvenance { get; }
        public int NativeCleanupElementsAuthorized { get; }
    }

    /// <summary>
    /// Single Core entry point for selecting one explicit semantic import policy.
    /// It never silently falls back from the requested mode and never performs native CAD cleanup.
    /// </summary>
    public static class ProjectInterchangeImportCoordinator
    {
        public static ProjectInterchangeImportCoordinatorPlan Plan(
            ProjectState target,
            string json,
            ProjectInterchangeImportRequest request)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateMode(request.Mode);

            switch (request.Mode)
            {
                case ProjectInterchangeImportExecutionMode.AppendOnly:
                    return PlanAppend(target, json, request.PreserveSourceHandleProvenance);
                case ProjectInterchangeImportExecutionMode.KeepTarget:
                    return PlanKeepTarget(target, json, request.PreserveSourceHandleProvenance);
                case ProjectInterchangeImportExecutionMode.ImportAsNew:
                    return PlanImportAsNew(target, json, request.PreserveSourceHandleProvenance);
                case ProjectInterchangeImportExecutionMode.UseSourceSemanticData:
                    return PlanUseSource(target, json, request.PreserveSourceHandleProvenance);
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Mode));
            }
        }

        public static ProjectInterchangeImportCoordinatorResult Execute(
            ProjectState target,
            string json,
            ProjectInterchangeImportRequest request,
            ProjectInterchangeNativeCleanupAuthorization nativeCleanupAuthorization)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (nativeCleanupAuthorization == null) throw new ArgumentNullException(nameof(nativeCleanupAuthorization));
            ValidateMode(request.Mode);

            if (request.Mode != ProjectInterchangeImportExecutionMode.UseSourceSemanticData && nativeCleanupAuthorization.ElementIds.Count != 0)
                throw new InvalidOperationException("Native cleanup authorization is accepted only for UseSourceSemanticData. The requested import mode must not silently consume unrelated cleanup authority.");

            var plan = Plan(target, json, request);
            if (!plan.CanExecute)
                throw new InvalidOperationException("Interchange import plan is blocked for requested mode " + request.Mode + ": blockerCount=" + plan.BlockerCount + ". No fallback mode was attempted.");

            switch (request.Mode)
            {
                case ProjectInterchangeImportExecutionMode.AppendOnly:
                    if (request.PreserveSourceHandleProvenance)
                        ProjectInterchangeAppendProvenanceImporter.Import(target, json);
                    else
                        ProjectInterchangeAppendOnlyImporter.Import(target, json);
                    break;

                case ProjectInterchangeImportExecutionMode.KeepTarget:
                    if (request.PreserveSourceHandleProvenance)
                        ProjectInterchangeKeepTargetProvenanceImporter.Import(target, json);
                    else
                        ProjectInterchangeKeepTargetImporter.Import(target, json);
                    break;

                case ProjectInterchangeImportExecutionMode.ImportAsNew:
                    if (request.PreserveSourceHandleProvenance)
                        ProjectInterchangeRemapProvenanceImporter.Import(target, json);
                    else
                        ProjectInterchangeRemapAppendImporter.Import(target, json);
                    break;

                case ProjectInterchangeImportExecutionMode.UseSourceSemanticData:
                    if (request.PreserveSourceHandleProvenance)
                        ProjectInterchangeUseSourceProvenanceImporter.Import(target, json, nativeCleanupAuthorization);
                    else
                        ProjectInterchangeUseSourceSemanticImporter.Import(target, json, nativeCleanupAuthorization);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Mode));
            }

            return new ProjectInterchangeImportCoordinatorResult(plan);
        }

        private static ProjectInterchangeImportCoordinatorPlan PlanAppend(ProjectState target, string json, bool preserve)
        {
            if (preserve)
            {
                var combined = ProjectInterchangeAppendProvenanceImporter.Plan(target, json);
                var plan = combined.SemanticPlan;
                return Build(
                    ProjectInterchangeImportExecutionMode.AppendOnly,
                    true,
                    plan.SourceProjectId,
                    plan.ValidationWarnings,
                    plan.TotalSemanticIdentitiesToAdd,
                    0,
                    0,
                    0,
                    0,
                    plan.SourceHandlesToDiscard,
                    0,
                    Array.Empty<ProjectInterchangeNativeCleanupRequirement>());
            }

            var semantic = ProjectInterchangeAppendOnlyImporter.Plan(target, json);
            return Build(
                ProjectInterchangeImportExecutionMode.AppendOnly,
                false,
                semantic.SourceProjectId,
                semantic.ValidationWarnings,
                semantic.TotalSemanticIdentitiesToAdd,
                0,
                0,
                0,
                0,
                semantic.SourceHandlesToDiscard,
                0,
                Array.Empty<ProjectInterchangeNativeCleanupRequirement>());
        }

        private static ProjectInterchangeImportCoordinatorPlan PlanKeepTarget(ProjectState target, string json, bool preserve)
        {
            if (preserve)
            {
                var combined = ProjectInterchangeKeepTargetProvenanceImporter.Plan(target, json);
                var plan = combined.SemanticPlan;
                return Build(
                    ProjectInterchangeImportExecutionMode.KeepTarget,
                    true,
                    plan.SourceProjectId,
                    plan.ValidationWarnings,
                    plan.TotalSemanticIdentitiesToAdd,
                    plan.TotalSemanticIdentitiesToKeep,
                    0,
                    0,
                    0,
                    plan.SourceHandlesToDiscard,
                    0,
                    Array.Empty<ProjectInterchangeNativeCleanupRequirement>());
            }

            var semantic = ProjectInterchangeKeepTargetImporter.Plan(target, json);
            return Build(
                ProjectInterchangeImportExecutionMode.KeepTarget,
                false,
                semantic.SourceProjectId,
                semantic.ValidationWarnings,
                semantic.TotalSemanticIdentitiesToAdd,
                semantic.TotalSemanticIdentitiesToKeep,
                0,
                0,
                0,
                semantic.SourceHandlesToDiscard,
                0,
                Array.Empty<ProjectInterchangeNativeCleanupRequirement>());
        }

        private static ProjectInterchangeImportCoordinatorPlan PlanImportAsNew(ProjectState target, string json, bool preserve)
        {
            ProjectInterchangeRemapAppendPlan plan;
            if (preserve)
                plan = ProjectInterchangeRemapProvenanceImporter.Plan(target, json).SemanticPlan;
            else
                plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var additions = checked(checked(source.Zones.Count + source.Floors.Count) + checked(source.Families.Count + source.Elements.Count));
            return Build(
                ProjectInterchangeImportExecutionMode.ImportAsNew,
                preserve,
                plan.Remap.SourceProjectId,
                plan.Remap.ValidationWarnings,
                additions,
                0,
                0,
                plan.IdRemapCount,
                plan.NameRemapCount,
                plan.SourceHandleCount,
                plan.BlockerCount,
                Array.Empty<ProjectInterchangeNativeCleanupRequirement>());
        }

        private static ProjectInterchangeImportCoordinatorPlan PlanUseSource(ProjectState target, string json, bool preserve)
        {
            ProjectInterchangeUseSourceSemanticPlan plan;
            if (preserve)
                plan = ProjectInterchangeUseSourceProvenanceImporter.Plan(target, json).SemanticPlan;
            else
                plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);

            return Build(
                ProjectInterchangeImportExecutionMode.UseSourceSemanticData,
                preserve,
                plan.SourceProjectId,
                plan.ValidationWarnings,
                plan.TotalSemanticIdentitiesToAdd,
                0,
                plan.TotalSemanticIdentitiesToReplace,
                0,
                0,
                plan.SourceHandlesToDiscard,
                0,
                plan.NativeCleanupRequirements,
                plan);
        }

        private static ProjectInterchangeImportCoordinatorPlan Build(
            ProjectInterchangeImportExecutionMode mode,
            bool preserve,
            string sourceProjectId,
            int validationWarnings,
            int additions,
            int keeps,
            int replacements,
            int idRemaps,
            int nameRemaps,
            int sourceHandleCount,
            int blockers,
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> cleanupRequirements,
            ProjectInterchangeUseSourceSemanticPlan? useSourceSemanticPlan = null)
        {
            var cleanup = new List<ProjectInterchangeNativeCleanupRequirement>();
            foreach (var requirement in cleanupRequirements ?? Array.Empty<ProjectInterchangeNativeCleanupRequirement>())
            {
                if (requirement == null)
                    throw new InvalidOperationException("Coordinator cleanup requirements cannot contain null entries.");
                cleanup.Add(requirement);
            }
            cleanup.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId));
            return new ProjectInterchangeImportCoordinatorPlan(
                mode,
                preserve,
                sourceProjectId,
                validationWarnings,
                additions,
                keeps,
                replacements,
                idRemaps,
                nameRemaps,
                sourceHandleCount,
                blockers,
                new ReadOnlyCollection<ProjectInterchangeNativeCleanupRequirement>(cleanup),
                useSourceSemanticPlan);
        }

        private static void ValidateMode(ProjectInterchangeImportExecutionMode mode)
        {
            if (!Enum.IsDefined(typeof(ProjectInterchangeImportExecutionMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode), "Unsupported interchange import execution mode.");
        }
    }
}