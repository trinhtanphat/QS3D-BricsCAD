using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class WallJunctionBuildResult
    {
        public int PlannedOutputs { get; set; }
        public int CreatedOutputs { get; set; }
        public int RemovedOutputs { get; set; }
        public int KeptOutputs { get; set; }
        public int RebuiltGroups { get; set; }
        public int RemovedGroups { get; set; }
    }

    internal static class WallJunctionSolidBuilder
    {
        public static WallJunctionBuildResult BuildSelected(
            Document document,
            ProjectState project,
            IReadOnlyList<ObjectId> selectedIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));
            if (selectedIds.Count == 0) return new WallJunctionBuildResult();
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Wall Junction 3D is supported only in Model Space.");

            var toleranceM = MetadataNumber(project, "WallJunctionToleranceM", 0.005d);
            var sagittaM = MetadataNumber(project, "WallArcSagittaM", 0.002d);
            var planarityToleranceM = MetadataNumber(project, "WallJunctionPlanarityToleranceM", toleranceM);
            var expectedProjectId = project.ProjectId;
            var expectedChangeVersion = project.ChangeVersion;
            var result = new WallJunctionBuildResult();

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var selected = WallJunctionSelectionReader.Read(
                    document,
                    transaction,
                    project,
                    selectedIds,
                    sagittaM,
                    planarityToleranceM);
                if (selected.Segments.Count == 0) return result;

                var selectedIdSet = new HashSet<ObjectId>(selectedIds);
                var planeScopes = WallJunctionSelectionReader.ResolveProjectPlaneScopes(
                    document,
                    transaction,
                    project,
                    planarityToleranceM,
                    rejectUnsupportedSources: false);
                var matchingScopes = planeScopes
                    .Where(scope => selectedIdSet.All(scope.Contains))
                    .Take(2)
                    .ToList();
                if (matchingScopes.Count != 1)
                    throw new InvalidOperationException("Wall Junction 3D could not resolve one complete semantic wall plane scope for the selected sources.");

                var selection = WallJunctionSelectionReader.Read(
                    document,
                    transaction,
                    project,
                    matchingScopes[0],
                    sagittaM,
                    planarityToleranceM);
                var selectedOwnerIds = new HashSet<string>(selected.Owners.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var junctions = new WallJunctionPlanner().Plan(selection.Axes, toleranceM);
                var allPlans = WallJunctionOwnershipPlanner.Plan(junctions, selection.OwnerMappings);
                var directlyScopedPlans = allPlans
                    .Where(x => x.OwnerWallIds.Any(selectedOwnerIds.Contains))
                    .ToList();

                var existing = GeneratedWallJunctionNativeOwnershipService.ReadAllStrict(document, transaction);
                foreach (var record in existing)
                    GeneratedWallJunctionNativeOwnershipService.RequireCurrentProject(record, project);
                foreach (var group in existing.GroupBy(x => x.GroupToken, StringComparer.Ordinal))
                    GeneratedWallJunctionNativeOwnershipService.ValidateGroupOwnerSet(group);

                var selectedOwnerIdentities = new HashSet<string>(
                    selected.Owners.Select(x => GeneratedWallJunctionNativeOwnershipService.OwnerIdentity(x.Id)),
                    StringComparer.Ordinal);
                var currentGroups = allPlans
                    .GroupBy(x => x.GroupToken, StringComparer.Ordinal)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.OccurrenceIndex).ToList(), StringComparer.Ordinal);
                var existingGroups = existing
                    .GroupBy(x => x.GroupToken, StringComparer.Ordinal)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.OccurrenceIndex).ToList(), StringComparer.Ordinal);

                var groupTokens = new HashSet<string>(directlyScopedPlans.Select(x => x.GroupToken), StringComparer.Ordinal);
                foreach (var pair in existingGroups)
                {
                    var ownerSet = pair.Value[0].OwnerIdentities;
                    var intersectsSelection = ownerSet.Any(selectedOwnerIdentities.Contains);
                    if (intersectsSelection || IsSupersededByCurrentTopology(pair.Value, directlyScopedPlans, toleranceM))
                        groupTokens.Add(pair.Key);
                }
                result.PlannedOutputs = groupTokens
                    .Where(currentGroups.ContainsKey)
                    .Sum(groupToken => currentGroups[groupToken].Count);

                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var segmentById = selection.Segments.ToDictionary(x => x.Axis.Id, StringComparer.OrdinalIgnoreCase);
                foreach (var groupToken in groupTokens.OrderBy(x => x, StringComparer.Ordinal))
                {
                    currentGroups.TryGetValue(groupToken, out var groupPlans);
                    existingGroups.TryGetValue(groupToken, out var groupRecords);
                    groupPlans = groupPlans ?? new List<WallJunctionOwnershipPlan>();
                    groupRecords = groupRecords ?? new List<WallJunctionNativeRecord>();

                    if (groupPlans.Count == 0)
                    {
                        foreach (var record in groupRecords) Erase(transaction, record);
                        result.RemovedOutputs += groupRecords.Count;
                        if (groupRecords.Count > 0) result.RemovedGroups++;
                        continue;
                    }

                    var expectedOwners = groupPlans[0].OwnerWallIds
                        .Select(GeneratedWallJunctionNativeOwnershipService.OwnerIdentity)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToArray();
                    if (groupPlans.Any(x => !x.OwnerWallIds.Select(GeneratedWallJunctionNativeOwnershipService.OwnerIdentity).OrderBy(y => y, StringComparer.Ordinal).SequenceEqual(expectedOwners)))
                        throw new InvalidOperationException("Wall Junction current group " + groupToken + " has inconsistent semantic owner membership.");
                    if (groupRecords.Any(x => !x.OwnerIdentities.SequenceEqual(expectedOwners)))
                        throw new InvalidOperationException("Wall Junction persisted group " + groupToken + " does not match the current complete owner set. Refusing destructive replacement.");

                    var recordByOwner = groupRecords.ToDictionary(x => x.OwnerToken, StringComparer.Ordinal);
                    var allCurrent = groupRecords.Count == groupPlans.Count &&
                                     groupPlans.All(plan => recordByOwner.TryGetValue(plan.OwnerToken, out var record) && GeneratedWallJunctionNativeOwnershipService.MatchesPlan(record, plan));
                    if (allCurrent)
                    {
                        result.KeptOutputs += groupRecords.Count;
                        continue;
                    }

                    foreach (var record in groupRecords) Erase(transaction, record);
                    result.RemovedOutputs += groupRecords.Count;
                    foreach (var plan in groupPlans)
                    {
                        var layerId = ResolveLayer(plan, segmentById);
                        var solid = CreateJunctionCore(document, plan, layerId);
                        try
                        {
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedWallJunctionNativeOwnershipService.MarkGenerated(document, transaction, solid, plan);
                            result.CreatedOutputs++;
                            solid = null!;
                        }
                        finally { solid?.Dispose(); }
                    }
                    result.RebuiltGroups++;
                }

                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) || project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Wall Junction 3D project changed during native planning; no junction output was committed.");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Wall Junction 3D");
                transaction.Commit();
            }

            if (result.CreatedOutputs > 0 || result.RemovedOutputs > 0)
                CadPostCommitUi.TryRegen(document, "Wall Junction 3D");
            return result;
        }

        private static Solid3d CreateJunctionCore(Document document, WallJunctionOwnershipPlan plan, ObjectId layerId)
        {
            // A rotationally symmetric dedicated core uses only the explicit Core plan fields.
            // Its diameter is the minimum participating wall thickness, so it remains bounded by
            // every owner profile. It never booleans, consumes, or reassigns a semantic wall host.
            var radius = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, plan.MinThicknessM / 2d, plan.OwnerToken + "/radius"),
                plan.OwnerToken + "/drawing radius");
            var height = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, plan.TopM - plan.BottomM, plan.OwnerToken + "/height"),
                plan.OwnerToken + "/drawing height");
            var x = CadGeometryGuard.Finite(CadGeometryGuard.ToDrawingUnits(document, plan.JunctionPoint.X, plan.OwnerToken + "/X"), plan.OwnerToken + "/drawing X");
            var y = CadGeometryGuard.Finite(CadGeometryGuard.ToDrawingUnits(document, plan.JunctionPoint.Y, plan.OwnerToken + "/Y"), plan.OwnerToken + "/drawing Y");
            var bottom = CadGeometryGuard.Finite(CadGeometryGuard.ToDrawingUnits(document, plan.BottomM, plan.OwnerToken + "/bottom"), plan.OwnerToken + "/drawing bottom");

            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(height, radius, radius, radius);
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, bottom)));
                if (!layerId.IsNull && layerId.IsValid) solid.LayerId = layerId;
                var completed = solid;
                solid = null!;
                return completed;
            }
            finally { solid?.Dispose(); }
        }

        private static ObjectId ResolveLayer(
            WallJunctionOwnershipPlan plan,
            IReadOnlyDictionary<string, WallJunctionSelectedSegment> segmentById)
        {
            foreach (var segmentId in plan.SourceSegmentIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                if (segmentById.TryGetValue(segmentId, out var segment)) return segment.LayerId;
            throw new InvalidOperationException("Wall Junction native layer source is missing for " + plan.OwnerToken + ".");
        }

        private static void Erase(Transaction transaction, WallJunctionNativeRecord record)
        {
            var solid = transaction.GetObject(record.ObjectId, OpenMode.ForWrite, false) as Solid3d;
            if (solid == null || solid.IsErased)
                throw new InvalidOperationException("Wall Junction output disappeared before whole-group replacement: " + record.Handle + ".");
            solid.Erase();
        }

        private static bool IsSupersededByCurrentTopology(
            IEnumerable<WallJunctionNativeRecord> persistedGroup,
            IEnumerable<WallJunctionOwnershipPlan> directlyScopedPlans,
            double toleranceM)
        {
            foreach (var record in persistedGroup)
            {
                foreach (var plan in directlyScopedPlans)
                {
                    var currentOwners = new HashSet<string>(
                        plan.OwnerWallIds.Select(GeneratedWallJunctionNativeOwnershipService.OwnerIdentity),
                        StringComparer.Ordinal);
                    if (!record.OwnerIdentities.Any(currentOwners.Contains)) continue;
                    if (record.JunctionPoint.DistanceTo(plan.JunctionPoint) <= toleranceM) return true;
                }
            }
            return false;
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
                throw new InvalidOperationException(key + " is invalid for Wall Junction 3D.");
            return value;
        }
    }
}
