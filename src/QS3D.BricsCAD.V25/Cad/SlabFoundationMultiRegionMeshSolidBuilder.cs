using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class MultiRegionMeshBuildResult
    {
        public int Elements { get; set; }
        public int Regions { get; set; }
        public int Bars { get; set; }
    }

    /// <summary>
    /// Native BricsCAD materializer for one semantic Slab/Foundation whose complete
    /// selected source is a set of disconnected closed polygon loops and/or holes.
    /// Existing rectangle and single-polygon builders remain unchanged.
    /// </summary>
    internal static class SlabFoundationMultiRegionMeshSolidBuilder
    {
        internal const int MaxBarsPerBatch = 12000;
        internal const string RegionOwnershipMarker = "QS3D_REBAR_REGION";
        private const double MaximumSagittaM = .002d;
        private const double ElevationToleranceDrawing = 1e-8d;

        private const string SlabHandlesKey = "GeneratedSlabMeshHandles";
        private const string FoundationHandlesKey = "GeneratedFoundationMeshHandles";
        private const string GeneratedManifestSuffix = "MultiRegionGeneratedManifest";
        private const string SourceManifestSuffix = "MultiRegionSourceManifest";
        private const string TopologyFingerprintSuffix = "MultiRegionTopologyFingerprint";
        private const string ModeSuffix = "MultiRegionMode";
        private const string Mode = "PolygonMultiRegionGlobalXY";

        private sealed class BuildConfiguration
        {
            public string HandlesKey { get; set; } = string.Empty;
            public string CountKey { get; set; } = string.Empty;
            public string PropertyPrefix { get; set; } = string.Empty;
            public string XNotationKey { get; set; } = string.Empty;
            public string YNotationKey { get; set; } = string.Empty;
            public string CoverKey { get; set; } = string.Empty;
            public string FacesKey { get; set; } = string.Empty;
            public string XClosestKey { get; set; } = string.Empty;
            public bool NotationFallsBackToFamily { get; set; }
            public double DefaultCoverM { get; set; }
            public double DefaultThicknessM { get; set; }
            public string AuditAction { get; set; } = string.Empty;
        }

        private sealed class SourceLoop
        {
            public ObjectId Id { get; set; }
            public Polyline Polyline { get; set; } = null!;
            public ClosedPolygonSourceLoopReadResult Read { get; set; } = null!;
        }

        public static MultiRegionMeshBuildResult BuildSlab(Document document, ProjectState project) =>
            BuildSelected(document, project, ElementCategory.Slab, SlabConfiguration());

        public static MultiRegionMeshBuildResult BuildFoundation(Document document, ProjectState project) =>
            BuildSelected(document, project, ElementCategory.Foundation, FoundationConfiguration());

        private static MultiRegionMeshBuildResult BuildSelected(
            Document document,
            ProjectState project,
            ElementCategory category,
            BuildConfiguration configuration)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null)
                    return new MultiRegionMeshBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var selectedIds = selection.Value.GetObjectIds();
            if (selectedIds == null || selectedIds.Length == 0) return new MultiRegionMeshBuildResult();
            var selectedHandles = selectedIds
                .Select(id => CanonicalHandle(id.Handle.ToString(), "selected multi-region source handle"))
                .ToList();
            if (selectedHandles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selectedHandles.Count)
                throw new InvalidOperationException("Multi-region source selection contains a duplicate CAD handle.");
            var selectedHandleSet = new HashSet<string>(selectedHandles, StringComparer.OrdinalIgnoreCase);

            var element = ResolveTargetElement(project, category, selectedHandleSet);
            EnsureAggregateMetadataConsistency(element, configuration);
            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var sources = ReadSources(document, transaction, selectedIds, element.Id);
                    EnsureCommonElevation(sources, element.Id);
                    var assembly = PolygonSourceLoopRegionAssembler.Assemble(
                        sources.Select(source => new PolygonSourceLoop2(source.Read.SourceHandle, source.Read.Loop)));
                    var topologyFingerprint = ComputeTopologyFingerprint(assembly, sources);

                    var family = project.FindFamily(element.FamilyId);
                    var xGroup = ParseDirection(element, family, configuration.XNotationKey, configuration.NotationFallsBackToFamily);
                    var yGroup = ParseDirection(element, family, configuration.YNotationKey, configuration.NotationFallsBackToFamily);
                    var verticalPlacement = CadElementVerticalPlacement.Resolve(
                        document,
                        project,
                        element,
                        family,
                        sources[0].Read.DrawingElevation,
                        "ThicknessM",
                        configuration.DefaultThicknessM);
                    var coverM = CadGeometryGuard.Number(
                        element,
                        family,
                        configuration.CoverKey,
                        CadGeometryGuard.Number(element, family, "RebarCoverM", configuration.DefaultCoverM));
                    if (coverM < 0d) throw new InvalidOperationException(element.Id + "/" + configuration.CoverKey + " must be >= 0.");
                    var faces = ReadText(element, family, configuration.FacesKey, "Bottom");
                    var includeBottom = string.Equals(faces, "Bottom", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                    var includeTop = string.Equals(faces, "Top", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                    if (!includeBottom && !includeTop)
                        throw new InvalidOperationException(element.Id + "/" + configuration.FacesKey + " must be Bottom, Top or Both.");
                    var xClosest = ReadBoolean(element, family, configuration.XClosestKey, true);

                    // This is deliberately the one and only multi-region planning call.
                    var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(new PolygonalSlabMultiRegionMeshInput
                    {
                        Regions = assembly.Regions.Select(region => new PolygonalSlabMeshRegionInput
                        {
                            RegionId = region.RegionId,
                            FootprintM = region.Region.Outer,
                            HoleFootprintsM = region.Region.Holes
                        }).ToList().AsReadOnly(),
                        ThicknessM = verticalPlacement.HeightM,
                        CoverM = coverM,
                        XDiameterMm = xGroup.DiameterMm,
                        YDiameterMm = yGroup.DiameterMm,
                        XSpacingMm = xGroup.SpacingMm,
                        XCount = xGroup.Quantity,
                        YSpacingMm = yGroup.SpacingMm,
                        YCount = yGroup.Quantity,
                        IncludeBottom = includeBottom,
                        IncludeTop = includeTop,
                        XClosestToFace = xClosest
                    });
                    if (layout.TotalBarCount > MaxBarsPerBatch)
                        throw new InvalidOperationException("Multi-region reinforcement exceeds native cap " + MaxBarsPerBatch + " generated bars; no CAD objects were erased.");

                    var previous = ValidateCompletePreviousOwnership(
                        document,
                        transaction,
                        project,
                        element,
                        configuration,
                        ownership,
                        out var legacyMigration);

                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var sourceByHandle = sources.ToDictionary(
                        x => CanonicalHandle(x.Read.SourceHandle, element.Id + "/source handle"),
                        x => x.Polyline,
                        StringComparer.OrdinalIgnoreCase);
                    var generatedByRegion = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var regionLayout in layout.Regions)
                    {
                        var sourceRegion = assembly.Regions.Single(x => string.Equals(x.RegionId, regionLayout.RegionId, StringComparison.OrdinalIgnoreCase));
                        var sourcePolyline = sourceByHandle[CanonicalHandle(sourceRegion.OuterSourceId, element.Id + "/outer source handle")];
                        var handles = new List<string>(regionLayout.Count);
                        foreach (var placement in regionLayout.Layout.Bars)
                        {
                            var startX = CadGeometryGuard.ToDrawingUnits(document, placement.StartM.X, element.Id + "/multi-region start X");
                            var startY = CadGeometryGuard.ToDrawingUnits(document, placement.StartM.Y, element.Id + "/multi-region start Y");
                            var endX = CadGeometryGuard.ToDrawingUnits(document, placement.EndM.X, element.Id + "/multi-region end X");
                            var endY = CadGeometryGuard.ToDrawingUnits(document, placement.EndM.Y, element.Id + "/multi-region end Y");
                            var elevationOffset = CadGeometryGuard.ToDrawingUnits(document, placement.ElevationOffsetM, element.Id + "/multi-region elevation");
                            var startZ = CadGeometryGuard.Add(verticalPlacement.CenterDrawing, elevationOffset, element.Id + "/multi-region start Z");
                            var run = new Vector3d(
                                CadGeometryGuard.Subtract(endX, startX, element.Id + "/multi-region run X"),
                                CadGeometryGuard.Subtract(endY, startY, element.Id + "/multi-region run Y"),
                                0d);
                            var length = CadGeometryGuard.Positive(
                                CadGeometryGuard.ToDrawingUnits(document, placement.LengthM, element.Id + "/multi-region bar length"),
                                element.Id + "/multi-region bar length drawing");
                            var radius = CadGeometryGuard.Positive(
                                CadGeometryGuard.ToDrawingUnits(document, placement.DiameterMm / 2000d, element.Id + "/multi-region bar radius"),
                                element.Id + "/multi-region bar radius drawing");
                            var bar = CreateCylinder(document, new Point3d(startX, startY, startZ), run, length, radius, element.Id + "/multi-region bar");
                            try
                            {
                                bar.Layer = sourcePolyline.Layer;
                                modelSpace.AppendEntity(bar);
                                transaction.AddNewlyCreatedDBObject(bar, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, bar, project, element, configuration.HandlesKey);
                                GeneratedRebarRegionOwnershipService.MarkGenerated(document, transaction, bar, project, element, configuration.HandlesKey, regionLayout.RegionId);
                                handles.Add(bar.Handle.ToString());
                                bar = null;
                            }
                            finally { bar?.Dispose(); }
                        }
                        generatedByRegion.Add(regionLayout.RegionId, handles);
                    }

                    // Ownership of the COMPLETE old aggregate was validated above. Only now may erase begin.
                    foreach (var old in previous)
                    {
                        var ids = CadHandleService.Resolve(document, new[] { old });
                        if (ids.Count != 1) throw new InvalidOperationException("Validated old generated handle " + old + " no longer resolves uniquely before erase.");
                        var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                        if (entity == null || entity.IsErased) throw new InvalidOperationException("Validated old generated handle " + old + " is not a live Entity before erase.");
                        entity.Erase();
                    }

                    var allHandles = generatedByRegion
                        .OrderBy(x => x.Key, StringComparer.Ordinal)
                        .SelectMany(x => x.Value.OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    element.Properties[configuration.HandlesKey] = string.Join(";", allHandles);
                    element.Properties[configuration.CountKey] = allHandles.Count.ToString(CultureInfo.InvariantCulture);
                    element.Properties[configuration.PropertyPrefix + GeneratedManifestSuffix] = MultiRegionRebarManifest.SerializeGenerated(
                        generatedByRegion.OrderBy(x => x.Key, StringComparer.Ordinal)
                            .Select(x => new GeneratedManifestEntry(x.Key, x.Value.AsReadOnly())));
                    element.Properties[configuration.PropertyPrefix + SourceManifestSuffix] = MultiRegionRebarManifest.SerializeSources(
                        assembly.Regions.Select(region => new SourceManifestEntry(region.RegionId, region.OuterSourceId, region.HoleSourceIds)));
                    element.Properties[configuration.PropertyPrefix + TopologyFingerprintSuffix] = topologyFingerprint;
                    element.Properties[configuration.PropertyPrefix + ModeSuffix] = Mode;
                    element.Properties[configuration.PropertyPrefix + "MultiRegionCount"] = assembly.Regions.Count.ToString(CultureInfo.InvariantCulture);
                    element.Properties[configuration.PropertyPrefix + "MultiRegionBarCount"] = allHandles.Count.ToString(CultureInfo.InvariantCulture);
                    element.Properties[configuration.PropertyPrefix + "MultiRegionLegacyMigration"] = legacyMigration ? "1" : "0";
                    CadElementVerticalPlacement.CommitSnapshot(element, configuration.PropertyPrefix + "MultiRegion", verticalPlacement);
                    if (element.Category == ElementCategory.Foundation) element.ClearGeneratedFoundationMeshStale();
                    AuditTrail.ForProject(project).Record(configuration.AuditAction, element.Id,
                        allHandles.Count.ToString(CultureInfo.InvariantCulture) + " bars / " + assembly.Regions.Count.ToString(CultureInfo.InvariantCulture) + " regions");

                    transaction.Commit();
                    cadCommitted = true;
                    return new MultiRegionMeshBuildResult { Elements = 1, Regions = assembly.Regions.Count, Bars = allHandles.Count };
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Multi-region reinforcement replacement failed before CAD commit and project Rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        private static ProjectElement ResolveTargetElement(ProjectState project, ElementCategory category, ISet<string> selectedHandles)
        {
            var candidates = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in project.Elements)
            {
                var semanticAnchor = candidate.SourceHandles.Any(source =>
                {
                    var normalized = CadHandleService.NormalizeHexHandle(source);
                    return normalized != null && selectedHandles.Contains(normalized);
                });
                if (semanticAnchor || PreviousSourceManifestContainsAny(candidate, selectedHandles))
                    candidates[candidate.Id] = candidate;
            }

            if (candidates.Count != 1)
                throw new InvalidOperationException("Multi-region source selection must resolve to exactly one semantic QS3D owner from current source ownership or its previous source manifest before any CAD write.");
            var element = candidates.Values.Single();
            if (element.Category != category)
                throw new InvalidOperationException("Selected source belongs to " + element.Category + ", not requested " + category + ".");
            return element;
        }

        private static bool PreviousSourceManifestContainsAny(ProjectElement element, ISet<string> selectedHandles)
        {
            string prefix;
            if (element.Category == ElementCategory.Slab) prefix = "GeneratedSlabMesh";
            else if (element.Category == ElementCategory.Foundation) prefix = "GeneratedFoundationMesh";
            else return false;

            string raw;
            if (!element.Properties.TryGetValue(prefix + SourceManifestSuffix, out raw) || string.IsNullOrWhiteSpace(raw)) return false;
            if (!selectedHandles.Any(handle => raw.IndexOf(handle, StringComparison.OrdinalIgnoreCase) >= 0)) return false;

            foreach (var entry in MultiRegionRebarManifest.ParseSources(raw))
            {
                if (selectedHandles.Contains(CanonicalHandle(entry.OuterSourceHandle, element.Id + "/previous outer source"))) return true;
                if (entry.HoleSourceHandles.Any(handle => selectedHandles.Contains(CanonicalHandle(handle, element.Id + "/previous hole source")))) return true;
            }
            return false;
        }

        private static void EnsureAggregateMetadataConsistency(ProjectElement element, BuildConfiguration configuration)
        {
            string rawHandles;
            var hasAggregate = element.Properties.TryGetValue(configuration.HandlesKey, out rawHandles) && !string.IsNullOrWhiteSpace(rawHandles);
            if (hasAggregate) return;

            var multiRegionKeys = new[]
            {
                configuration.PropertyPrefix + GeneratedManifestSuffix,
                configuration.PropertyPrefix + SourceManifestSuffix,
                configuration.PropertyPrefix + TopologyFingerprintSuffix,
                configuration.PropertyPrefix + "MultiRegionCount",
                configuration.PropertyPrefix + "MultiRegionBarCount"
            };
            if (multiRegionKeys.Any(key => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)))
                throw new InvalidOperationException(
                    element.Id + " has persisted multi-region metadata but its aggregate generated-handle slot " +
                    configuration.HandlesKey + " is missing. Refusing replacement before any CAD write.");
        }

        private static string ComputeTopologyFingerprint(PolygonSourceRegionAssembly2 assembly, IReadOnlyList<SourceLoop> sources)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            var fingerprintByHandle = sources.ToDictionary(
                source => CanonicalHandle(source.Read.SourceHandle, "multi-region source fingerprint handle"),
                source => source.Read.Fingerprint,
                StringComparer.OrdinalIgnoreCase);
            var canonical = new StringBuilder();
            foreach (var region in assembly.Regions.OrderBy(x => x.RegionId, StringComparer.Ordinal))
            {
                if (canonical.Length > 0) canonical.Append(';');
                canonical.Append("R=").Append(CanonicalHandle(region.RegionId, "multi-region RegionId"));
                AppendSourceFingerprint(canonical, "O", region.OuterSourceId, fingerprintByHandle);
                foreach (var hole in region.HoleSourceIds
                    .Select(handle => CanonicalHandle(handle, "multi-region hole source handle"))
                    .OrderBy(x => x, StringComparer.Ordinal))
                    AppendSourceFingerprint(canonical, "H", hole, fingerprintByHandle);
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static void AppendSourceFingerprint(
            StringBuilder target,
            string role,
            string sourceHandle,
            IReadOnlyDictionary<string, string> fingerprintByHandle)
        {
            var handle = CanonicalHandle(sourceHandle, "multi-region topology source handle");
            string fingerprint;
            if (!fingerprintByHandle.TryGetValue(handle, out fingerprint) || string.IsNullOrWhiteSpace(fingerprint))
                throw new InvalidOperationException("Multi-region topology source " + sourceHandle + " has no deterministic geometry fingerprint.");
            target.Append('|').Append(role).Append('=').Append(handle).Append(':').Append(fingerprint);
        }

        private static IReadOnlyList<string> ValidateCompletePreviousOwnership(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            BuildConfiguration configuration,
            GeneratedRebarOwnershipGuard.OwnershipIndex ownership,
            out bool LegacyAggregateMigration)
        {
            LegacyAggregateMigration = false;
            string rawHandles;
            if (!element.Properties.TryGetValue(configuration.HandlesKey, out rawHandles) || string.IsNullOrWhiteSpace(rawHandles))
                return Array.Empty<string>();

            var aggregate = rawHandles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(handle => CanonicalHandle(handle.Trim(), element.Id + "/" + configuration.HandlesKey))
                .ToList();
            if (aggregate.Count == 0 || aggregate.Distinct(StringComparer.OrdinalIgnoreCase).Count() != aggregate.Count)
                throw new InvalidOperationException("Generated rebar aggregate contains no handles or duplicate canonical handles; refusing erase.");

            string rawCount;
            if (element.Properties.TryGetValue(configuration.CountKey, out rawCount) && !string.IsNullOrWhiteSpace(rawCount))
            {
                int expectedCount;
                if (!int.TryParse(rawCount.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out expectedCount) || expectedCount != aggregate.Count)
                    throw new InvalidOperationException("Generated rebar aggregate count does not match its handle slot; refusing erase.");
            }

            var generatedManifestKey = configuration.PropertyPrefix + GeneratedManifestSuffix;
            string rawManifest;
            if (!element.Properties.TryGetValue(generatedManifestKey, out rawManifest) || string.IsNullOrWhiteSpace(rawManifest))
            {
                LegacyAggregateMigration = true;
                foreach (var handle in aggregate)
                {
                    ownership.EnsureOwned(handle, element, configuration.HandlesKey);
                    var entity = ResolveLiveEntity(document, transaction, handle, OpenMode.ForRead);
                    GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(entity, project, element, configuration.HandlesKey, "migrate legacy aggregate " + handle);
                }
                return aggregate.AsReadOnly();
            }

            var manifest = MultiRegionRebarManifest.ParseGenerated(rawManifest);
            var manifestHandles = manifest.SelectMany(x => x.Handles)
                .Select(handle => CanonicalHandle(handle, element.Id + "/multi-region generated manifest"))
                .ToList();
            var aggregateSet = new HashSet<string>(aggregate, StringComparer.OrdinalIgnoreCase);
            var manifestSet = new HashSet<string>(manifestHandles, StringComparer.OrdinalIgnoreCase);
            if (manifestSet.Count != manifestHandles.Count || !aggregateSet.SetEquals(manifestSet))
                throw new InvalidOperationException("Multi-region generated manifest does not exactly match the aggregate generated-handle slot; refusing erase.");
            var regionByHandle = manifest.SelectMany(entry => entry.Handles.Select(handle => new
                {
                    Handle = CanonicalHandle(handle, element.Id + "/multi-region generated handle"),
                    entry.RegionId
                }))
                .ToDictionary(x => x.Handle, x => x.RegionId, StringComparer.OrdinalIgnoreCase);

            foreach (var handle in aggregate)
            {
                ownership.EnsureOwned(handle, element, configuration.HandlesKey);
                var entity = ResolveLiveEntity(document, transaction, handle, OpenMode.ForRead);
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(entity, project, element, configuration.HandlesKey, "refresh multi-region aggregate " + handle);
                GeneratedRebarRegionOwnershipService.RequireMatchingOwnership(entity, project, element, configuration.HandlesKey, regionByHandle[handle], "refresh multi-region region " + handle);
            }
            return aggregate.AsReadOnly();
        }

        private static string CanonicalHandle(string? handle, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(handle);
            if (canonical == null)
                throw new InvalidOperationException(label + " is not a valid positive CAD handle: " + (handle ?? "<null>") + ".");
            return canonical;
        }

        private static Entity ResolveLiveEntity(Document document, Transaction transaction, string handle, OpenMode mode)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException("Generated handle " + handle + " must resolve to exactly one CAD object.");
            var entity = transaction.GetObject(ids[0], mode, false) as Entity;
            if (entity == null || entity.IsErased) throw new InvalidOperationException("Generated handle " + handle + " is not a live Entity.");
            return entity;
        }

        private static List<SourceLoop> ReadSources(Document document, Transaction transaction, ObjectId[] ids, string elementId)
        {
            var sources = new List<SourceLoop>(ids.Length);
            foreach (var id in ids)
            {
                var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                if (polyline == null) throw new InvalidOperationException("Multi-region source for " + elementId + " must contain only closed POLYLINE objects.");
                sources.Add(new SourceLoop
                {
                    Id = id,
                    Polyline = polyline,
                    Read = ClosedPolygonSourceLoopReader.Read(document, polyline, MaximumSagittaM, elementId + "/multi-region source")
                });
            }
            return sources;
        }

        private static void EnsureCommonElevation(IReadOnlyList<SourceLoop> sources, string elementId)
        {
            if (sources.Count == 0) throw new InvalidOperationException("No multi-region source loops were selected for " + elementId + ".");
            var baseline = sources[0].Read.DrawingElevation;
            foreach (var source in sources.Skip(1))
            {
                var scale = Math.Max(1d, Math.Max(Math.Abs(baseline), Math.Abs(source.Read.DrawingElevation)));
                if (Math.Abs(source.Read.DrawingElevation - baseline) > ElevationToleranceDrawing * scale)
                    throw new InvalidOperationException("All multi-region source loops for " + elementId + " must share one horizontal elevation.");
            }
        }

        private static RebarGroup ParseDirection(ProjectElement element, ProjectFamily? family, string key, bool useFamilyFallback)
        {
            string notation;
            if (element.Properties.TryGetValue(key, out notation) && !string.IsNullOrWhiteSpace(notation))
                notation = notation.Trim();
            else if (useFamilyFallback && family != null && family.Properties.TryGetValue(key, out notation) && !string.IsNullOrWhiteSpace(notation))
                notation = notation.Trim();
            else
                throw new InvalidOperationException(element.Id + " is missing " + key + ".");

            var groups = RebarNotationParser.Parse(notation);
            if (groups.Count != 1) throw new InvalidOperationException(element.Id + "/" + key + " supports exactly one rebar group.");
            var group = groups[0];
            if (!group.Quantity.HasValue && !group.SpacingMm.HasValue) throw new InvalidOperationException(element.Id + "/" + key + " requires count or spacing.");
            if (group.Quantity.HasValue && group.SpacingMm.HasValue) throw new InvalidOperationException(element.Id + "/" + key + " cannot specify count and spacing together.");
            return group;
        }

        private static string ReadText(ProjectElement element, ProjectFamily? family, string key, string fallback)
        {
            string value;
            if (element.Properties.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            if (family != null && family.Properties.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            return fallback;
        }

        private static bool ReadBoolean(ProjectElement element, ProjectFamily? family, string key, bool fallback)
        {
            var raw = ReadText(element, family, key, fallback ? "true" : "false");
            bool value;
            if (bool.TryParse(raw, out value)) return value;
            if (raw == "1") return true;
            if (raw == "0") return false;
            throw new InvalidOperationException(element.Id + "/" + key + " must be true/false or 1/0.");
        }

        private static Solid3d CreateCylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = CadGeometryGuard.Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Multi-region bar axis is invalid: " + label);
            var unit = new Vector3d(direction.X / magnitude, direction.Y / magnitude, direction.Z / magnitude);
            var safeStart = new Point3d(
                CadGeometryGuard.Finite(start.X, label + "/start X"),
                CadGeometryGuard.Finite(start.Y, label + "/start Y"),
                CadGeometryGuard.Finite(start.Z, label + "/start Z"));
            Solid3d solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(length, radius, radius, radius);
                var dot = Math.Max(-1d, Math.Min(1d, unit.Z));
                var angle = Math.Acos(dot);
                var rotationAxis = Vector3d.ZAxis.CrossProduct(unit);
                if (CadGeometryGuard.Hypot3(rotationAxis.X, rotationAxis.Y, rotationAxis.Z, label + "/rotation axis") > 1e-12d)
                    solid.TransformBy(Matrix3d.Rotation(angle, rotationAxis, Point3d.Origin));
                else if (dot < 0d)
                    solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(safeStart - Point3d.Origin));
                var result = solid;
                solid = null!;
                return result;
            }
            finally { solid?.Dispose(); }
        }

        private static BuildConfiguration SlabConfiguration() => new BuildConfiguration
        {
            HandlesKey = SlabHandlesKey,
            CountKey = "GeneratedSlabMeshCount",
            PropertyPrefix = "GeneratedSlabMesh",
            XNotationKey = "RebarSlabXNotation",
            YNotationKey = "RebarSlabYNotation",
            CoverKey = "RebarSlabCoverM",
            FacesKey = "RebarSlabFaces",
            XClosestKey = "RebarSlabXClosestToFace",
            NotationFallsBackToFamily = false,
            DefaultCoverM = .02d,
            DefaultThicknessM = .12d,
            AuditAction = "geometry.rebar.slab.mesh.multiregion"
        };

        private static BuildConfiguration FoundationConfiguration() => new BuildConfiguration
        {
            HandlesKey = FoundationHandlesKey,
            CountKey = "GeneratedFoundationMeshCount",
            PropertyPrefix = "GeneratedFoundationMesh",
            XNotationKey = "RebarFoundationXNotation",
            YNotationKey = "RebarFoundationYNotation",
            CoverKey = "RebarFoundationCoverM",
            FacesKey = "RebarFoundationFaces",
            XClosestKey = "RebarFoundationXClosestToFace",
            NotationFallsBackToFamily = true,
            DefaultCoverM = .05d,
            DefaultThicknessM = .5d,
            AuditAction = "geometry.rebar.foundation.mesh.multiregion"
        };
    }
}