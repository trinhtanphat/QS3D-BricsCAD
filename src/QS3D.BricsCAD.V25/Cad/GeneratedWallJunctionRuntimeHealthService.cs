using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedWallJunctionRuntimeHealthService
    {
        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var records = ReadRecords(document, transaction, issues);
                var ownerByIdentity = BuildOwnerIndex(project, issues);
                var currentProjectIdentity = GeneratedWallJunctionNativeOwnershipService.ProjectIdentity(project.ProjectId);
                var currentDrawingIdentity = GeneratedWallJunctionNativeOwnershipService.DrawingIdentity(project.DrawingFingerprint);
                IReadOnlyList<WallJunctionOwnershipPlan> currentPlans;
                var currentPlanAvailable = true;
                try
                {
                    var toleranceM = MetadataNumber(project, "WallJunctionToleranceM", 0.005d);
                    var sagittaM = MetadataNumber(project, "WallArcSagittaM", 0.002d);
                    var planarityToleranceM = MetadataNumber(project, "WallJunctionPlanarityToleranceM", toleranceM);
                    var planned = new List<WallJunctionOwnershipPlan>();
                    foreach (var sourceIds in WallJunctionSelectionReader.ResolveProjectPlaneScopes(
                        document,
                        transaction,
                        project,
                        planarityToleranceM,
                        rejectUnsupportedSources: true))
                    {
                        if (sourceIds.Count < 2) continue;
                        var selection = WallJunctionSelectionReader.Read(
                            document,
                            transaction,
                            project,
                            sourceIds,
                            sagittaM,
                            planarityToleranceM);
                        planned.AddRange(WallJunctionOwnershipPlanner.Plan(
                            new WallJunctionPlanner().Plan(selection.Axes, toleranceM),
                            selection.OwnerMappings));
                    }
                    if (planned.GroupBy(x => x.OwnerToken, StringComparer.Ordinal).Any(x => x.Count() != 1))
                        throw new InvalidOperationException("Wall Junction project-wide plane scopes produced duplicate native owner tokens.");
                    currentPlans = planned.AsReadOnly();
                }
                catch (Exception)
                {
                    currentPlanAvailable = false;
                    currentPlans = Array.Empty<WallJunctionOwnershipPlan>();
                    issues.Add(new ModelHealthIssue(
                        "WALL_JUNCTION_NATIVE_PLAN_UNAVAILABLE",
                        HealthSeverity.Error,
                        "Wall Junction current project-wide plan could not be recomputed safely from semantic wall sources.",
                        string.Empty));
                }

                var currentByOwner = currentPlans
                    .GroupBy(x => x.OwnerToken, StringComparer.Ordinal)
                    .ToDictionary(x => x.Key, x => x.Single(), StringComparer.Ordinal);

                foreach (var duplicate in records.GroupBy(x => x.OwnerToken, StringComparer.Ordinal).Where(x => x.Count() > 1))
                {
                    issues.Add(new ModelHealthIssue(
                        "WALL_JUNCTION_NATIVE_OWNER_DUPLICATE",
                        HealthSeverity.Error,
                        "Multiple live Solid3d objects claim one Wall Junction owner token.",
                        string.Empty));
                }

                foreach (var group in records.GroupBy(x => x.GroupToken, StringComparer.Ordinal))
                {
                    var groupRecords = group.OrderBy(x => x.OccurrenceIndex).ToList();
                    var first = groupRecords[0];
                    if (groupRecords.Any(x => !x.OwnerIdentities.SequenceEqual(first.OwnerIdentities)))
                    {
                        issues.Add(new ModelHealthIssue(
                            "WALL_JUNCTION_NATIVE_GROUP_OWNER_MISMATCH",
                            HealthSeverity.Error,
                            "Wall Junction group records do not preserve one complete owner set.",
                            string.Empty));
                        continue;
                    }

                    if (groupRecords.Any(x => !string.Equals(x.ProjectIdentity, currentProjectIdentity, StringComparison.Ordinal) ||
                                              !string.Equals(x.DrawingIdentity, currentDrawingIdentity, StringComparison.Ordinal)))
                    {
                        issues.Add(new ModelHealthIssue(
                            "WALL_JUNCTION_NATIVE_PROJECT_MISMATCH",
                            HealthSeverity.Error,
                            "Wall Junction output belongs to another project or drawing and is not eligible for automatic replacement.",
                            string.Empty));
                        continue;
                    }

                    var owners = new List<ProjectElement>();
                    foreach (var identity in first.OwnerIdentities)
                    {
                        if (!ownerByIdentity.TryGetValue(identity, out var owner))
                        {
                            issues.Add(new ModelHealthIssue(
                                "WALL_JUNCTION_NATIVE_OWNER_MISSING",
                                HealthSeverity.Error,
                                "Wall Junction output references a semantic wall owner that is no longer present.",
                                string.Empty));
                            continue;
                        }
                        owners.Add(owner);
                    }
                    if (owners.Count != first.OwnerIdentities.Count) continue;
                    if (!currentPlanAvailable) continue;

                    foreach (var record in groupRecords)
                    {
                        var elementId = owners.Count > 0 ? owners[0].Id : string.Empty;
                        if (!currentByOwner.TryGetValue(record.OwnerToken, out var plan))
                        {
                            issues.Add(new ModelHealthIssue(
                                "WALL_JUNCTION_NATIVE_STALE_EXTRA",
                                HealthSeverity.Error,
                                "Wall Junction output no longer has a current ownership plan and requires whole-group rebuild/removal: " + record.Handle + ".",
                                elementId));
                            continue;
                        }
                        if (!GeneratedWallJunctionNativeOwnershipService.MatchesPlan(record, plan))
                            issues.Add(new ModelHealthIssue(
                                "WALL_JUNCTION_NATIVE_FINGERPRINT_STALE",
                                HealthSeverity.Error,
                                "Wall Junction output does not match its current WJF1 plan and requires whole-group rebuild: " + record.Handle + ".",
                                elementId));
                    }
                }

                if (currentPlanAvailable)
                {
                    var persistedOwnerTokens = new HashSet<string>(records.Select(x => x.OwnerToken), StringComparer.Ordinal);
                    foreach (var plan in currentPlans.Where(x => !persistedOwnerTokens.Contains(x.OwnerToken)))
                    {
                        issues.Add(new ModelHealthIssue(
                            "WALL_JUNCTION_NATIVE_OUTPUT_MISSING",
                            HealthSeverity.Error,
                            "Current Wall Junction ownership plan has no live dedicated Solid3d output.",
                            plan.OwnerWallIds.FirstOrDefault() ?? string.Empty));
                    }

                    foreach (var group in currentPlans.GroupBy(x => x.GroupToken, StringComparer.Ordinal))
                    {
                        var persistedCount = records.Count(x =>
                            string.Equals(x.GroupToken, group.Key, StringComparison.Ordinal) &&
                            string.Equals(x.ProjectIdentity, currentProjectIdentity, StringComparison.Ordinal) &&
                            string.Equals(x.DrawingIdentity, currentDrawingIdentity, StringComparison.Ordinal));
                        if (persistedCount != group.Count())
                            issues.Add(new ModelHealthIssue(
                                "WALL_JUNCTION_NATIVE_OUTPUT_SET_INCOMPLETE",
                                HealthSeverity.Error,
                                "Wall Junction group output count does not match the current complete occurrence plan.",
                                group.First().OwnerWallIds.FirstOrDefault() ?? string.Empty));
                    }
                }

                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        public static IReadOnlyList<string> Handles(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var handles = new List<string>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    using (var marker = entity.GetXDataForApplication(GeneratedWallJunctionNativeOwnershipService.RegAppName))
                        if (marker != null) handles.Add(entity.Handle.ToString());
                }
                transaction.Commit();
            }
            return handles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static List<WallJunctionNativeRecord> ReadRecords(
            Document document,
            Transaction transaction,
            ICollection<ModelHealthIssue> issues)
        {
            var result = new List<WallJunctionNativeRecord>();
            var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in modelSpace)
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                using (var marker = entity.GetXDataForApplication(GeneratedWallJunctionNativeOwnershipService.RegAppName))
                {
                    if (marker == null) continue;
                }
                if (!(entity is Solid3d))
                {
                    issues.Add(new ModelHealthIssue(
                        "WALL_JUNCTION_NATIVE_ENTITY_TYPE_MISMATCH",
                        HealthSeverity.Error,
                        "Wall Junction ownership marker is attached to a non-Solid3d CAD object: " + entity.Handle + ".",
                        string.Empty));
                    continue;
                }
                if (!GeneratedWallJunctionNativeOwnershipService.TryRead(entity, out var record, out var errorCode))
                {
                    issues.Add(new ModelHealthIssue(
                        "WALL_JUNCTION_NATIVE_MARKER_INVALID",
                        HealthSeverity.Error,
                        "Wall Junction ownership marker is malformed (" + errorCode + "): " + entity.Handle + ".",
                        string.Empty));
                    continue;
                }
                result.Add(record);
            }
            return result;
        }

        private static Dictionary<string, ProjectElement> BuildOwnerIndex(
            ProjectState project,
            ICollection<ModelHealthIssue> issues)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.Ordinal);
            foreach (var owner in project.Elements.Where(x => IsWall(x.Category)))
            {
                var identity = GeneratedWallJunctionNativeOwnershipService.OwnerIdentity(owner.Id);
                if (result.ContainsKey(identity))
                {
                    issues.Add(new ModelHealthIssue(
                        "WALL_JUNCTION_NATIVE_OWNER_IDENTITY_COLLISION",
                        HealthSeverity.Error,
                        "Two semantic walls resolve to the same native Wall Junction owner identity.",
                        owner.Id));
                    continue;
                }
                result[identity] = owner;
            }
            return result;
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        private static double MetadataNumber(ProjectState project, string key, double fallback)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new InvalidOperationException(key + " is invalid for Wall Junction health.");
            return value;
        }
    }
}
