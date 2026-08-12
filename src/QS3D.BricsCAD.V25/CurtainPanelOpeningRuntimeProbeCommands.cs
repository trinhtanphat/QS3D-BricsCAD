using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P02 probe for existing LINE curtain-panel opening
    /// clipping. The runner creates only synthetic legacy/no-Level geometry. Result
    /// markers contain aggregate counts and booleans, never CAD Handles or semantic IDs.
    /// </summary>
    public sealed class CurtainPanelOpeningRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_OPENING_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_OPENING_NONCE";
        private const string ResultFileName = "curtain-panel-opening-runtime-result.txt";
        private const double GeometryToleranceM = 1e-6d;

        private sealed class ScenarioEvidence
        {
            public ProjectElement Host { get; set; } = null!;
            public ElementCategory OpeningCategory { get; set; }
            public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> OpeningSourceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> HostHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> FrameHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> PanelHandles { get; set; } = Array.Empty<string>();
            public int SourcePanelCount { get; set; }
            public int OutputPieceCount { get; set; }
            public int FullyRemovedPanelCount { get; set; }
            public int PartiallyClippedPanelCount { get; set; }
            public int NativePlanMatchCount { get; set; }
            public int NativeOpeningIntersectionCount { get; set; }
            public bool CompleteEmpty { get; set; }
        }

        private sealed class NativePiece
        {
            public double X_M { get; set; }
            public double Z_M { get; set; }
            public double WidthM { get; set; }
            public double HeightM { get; set; }
        }

        [CommandMethod("QS3DCURTAINOPENINGPREPARE", CommandFlags.Modal)]
        public void PrepareSourceSelection()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain opening prepare is automation-only.");
            RequiredResultPath(requestedPath);

            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Curtain opening prepare requires an existing QS3D project.");
            var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            if (hosts.Count != 2)
                throw new InvalidOperationException("Curtain opening prepare requires exactly two GlassWalls.");

            var sourceHandles = new List<string>();
            foreach (var host in hosts)
            {
                RequireLegacyNoLevel(host, "GlassWall");
                var handles = CanonicalHandles(host.SourceHandles, "GlassWall source");
                if (handles.Count != 1)
                    throw new InvalidOperationException("Each Curtain opening GlassWall requires exactly one source.");
                var linked = LinkedOpenings(project, host);
                if (linked.Count != 1)
                    throw new InvalidOperationException("Each Curtain opening GlassWall requires exactly one linked opening.");
                RequireLegacyNoLevel(linked[0], "opening");
                sourceHandles.Add(handles[0]);
            }
            if (sourceHandles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
                throw new InvalidOperationException("Curtain opening GlassWall sources are not distinct.");
            var sourceIds = CadHandleService.Resolve(document, sourceHandles);
            if (sourceIds.Count != 2)
                throw new InvalidOperationException("Curtain opening prepare could not resolve both GlassWall sources.");
            document.Editor.SetImpliedSelection(sourceIds.ToArray());
        }

        [CommandMethod("QS3DCURTAINOPENINGPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain opening probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Curtain opening runtime nonce is invalid.");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Curtain opening result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Curtain opening probe requires an existing QS3D project.");

                var hosts = project.Elements
                    .Where(x => x.Category == ElementCategory.GlassWall &&
                                x.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state) &&
                                string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (hosts.Count != 2)
                    throw new InvalidOperationException("Curtain opening probe requires exactly two completed GlassWall owners.");

                var evidence = new List<ScenarioEvidence>();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (var host in hosts) evidence.Add(InspectScenario(document, transaction, project, host));
                    transaction.Commit();
                }

                var partial = evidence.SingleOrDefault(x => x.OpeningCategory == ElementCategory.Door)
                    ?? throw new InvalidOperationException("Curtain opening probe requires one Door partial scenario.");
                var completeEmpty = evidence.SingleOrDefault(x => x.OpeningCategory == ElementCategory.WallOpening)
                    ?? throw new InvalidOperationException("Curtain opening probe requires one WallOpening complete-empty scenario.");
                if (evidence.Count(x => x.OpeningCategory == ElementCategory.Door) != 1 ||
                    evidence.Count(x => x.OpeningCategory == ElementCategory.WallOpening) != 1)
                    throw new InvalidOperationException("Curtain opening scenario categories are ambiguous.");

                if (partial.CompleteEmpty || partial.OutputPieceCount <= 0 || partial.FullyRemovedPanelCount <= 0 ||
                    partial.PartiallyClippedPanelCount <= 0 || partial.NativePlanMatchCount != partial.OutputPieceCount ||
                    partial.NativeOpeningIntersectionCount != 0)
                    throw new InvalidOperationException("Door partial/full-cell clipping evidence is incomplete.");
                if (!completeEmpty.CompleteEmpty || completeEmpty.OutputPieceCount != 0 ||
                    completeEmpty.FullyRemovedPanelCount != completeEmpty.SourcePanelCount ||
                    completeEmpty.PartiallyClippedPanelCount != 0 || completeEmpty.PanelHandles.Count != 0 ||
                    completeEmpty.NativePlanMatchCount != 0 || completeEmpty.NativeOpeningIntersectionCount != 0)
                    throw new InvalidOperationException("WallOpening complete-empty panel evidence is incomplete.");

                RequireDisjoint(evidence.SelectMany(x => new[]
                {
                    x.SourceHandles,
                    x.OpeningSourceHandles,
                    x.HostHandles,
                    x.FrameHandles,
                    x.PanelHandles
                }).ToArray());

                var livePanels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var handle in evidence.SelectMany(x => x.PanelHandles)) livePanels.Add(handle);
                var coreIssues = new GeneratedCurtainPanelHealthService().Inspect(project, livePanels);
                var liveIssues = CurtainWallPanelLiveStateService.Inspect(document, project);
                var runtimeIssues = GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project);
                var blockingIssues = coreIssues.Concat(liveIssues).Concat(runtimeIssues)
                    .Count(x => x.Severity != HealthSeverity.Info);
                if (blockingIssues != 0)
                    throw new InvalidOperationException("Curtain opening panel Health is not clean.");

                var locatedIds = CadHandleService.Resolve(document, new[] { partial.PanelHandles[0] });
                if (locatedIds.Count != 1)
                    throw new InvalidOperationException("Curtain opening probe cannot resolve one partial panel for Locate.");
                document.Editor.SetImpliedSelection(locatedIds.ToArray());
                var owners = SemanticSelectionResolver.ResolveImplied(document, project);
                if (owners.Count != 1 || !ReferenceEquals(owners[0], partial.Host))
                    throw new InvalidOperationException("Curtain opening panel Locate did not resolve one canonical GlassWall.");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DCURTAINOPENINGPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V1",
                    "qualification_boundary=LOCAL_002_P02_ONLY",
                    "production_local002_qualified=false",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "legacy_no_level=true",
                    "glass_wall_count=2",
                    "linked_opening_count=2",
                    "partial_source_panel_count=" + partial.SourcePanelCount.ToString(CultureInfo.InvariantCulture),
                    "partial_output_piece_count=" + partial.OutputPieceCount.ToString(CultureInfo.InvariantCulture),
                    "partial_fully_removed_panel_count=" + partial.FullyRemovedPanelCount.ToString(CultureInfo.InvariantCulture),
                    "partial_clipped_panel_count=" + partial.PartiallyClippedPanelCount.ToString(CultureInfo.InvariantCulture),
                    "partial_native_plan_match_count=" + partial.NativePlanMatchCount.ToString(CultureInfo.InvariantCulture),
                    "partial_native_opening_intersection_count=0",
                    "complete_empty_source_panel_count=" + completeEmpty.SourcePanelCount.ToString(CultureInfo.InvariantCulture),
                    "complete_empty_output_piece_count=0",
                    "complete_empty_fully_removed_panel_count=" + completeEmpty.FullyRemovedPanelCount.ToString(CultureInfo.InvariantCulture),
                    "complete_empty_handle_count=0",
                    "complete_empty_build_state=true",
                    "opening_aware_metadata=true",
                    "source_geometry_preserved=true",
                    "ownership_sets_disjoint=true",
                    "health_issue_count=0",
                    "located_panel_count=1",
                    "canonical_owner_count=1"
                });
                document.Editor.WriteMessage("\nQS3D curtain opening probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(requestedPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain opening probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static ScenarioEvidence InspectScenario(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host)
        {
            RequireLegacyNoLevel(host, "GlassWall");
            var linked = LinkedOpenings(project, host);
            if (linked.Count != 1)
                throw new InvalidOperationException("Curtain opening scenario requires exactly one linked opening.");
            var opening = linked[0];
            RequireLegacyNoLevel(opening, "opening");

            var sourceHandles = CanonicalHandles(host.SourceHandles, "GlassWall source");
            var openingSourceHandles = CanonicalHandles(opening.SourceHandles, "opening source");
            if (sourceHandles.Count != 1 || openingSourceHandles.Count != 1)
                throw new InvalidOperationException("Curtain opening scenario source ownership is incomplete.");
            var sourceIds = CadHandleService.Resolve(document, sourceHandles);
            var openingIds = CadHandleService.Resolve(document, openingSourceHandles);
            if (sourceIds.Count != 1 || openingIds.Count != 1)
                throw new InvalidOperationException("Curtain opening scenario sources are not live.");
            var line = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Line
                ?? throw new InvalidOperationException("Curtain opening host source is not a LINE.");
            var openingLine = transaction.GetObject(openingIds[0], OpenMode.ForRead, false) as Line
                ?? throw new InvalidOperationException("Curtain opening source is not a LINE.");
            RequireSyntheticLine(document, line, openingLine, opening.Category);

            var family = project.FindFamily(host.FamilyId);
            var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, "curtain opening probe/dx");
            var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, "curtain opening probe/dy");
            var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, "curtain opening probe/dz");
            var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, "curtain opening probe/length");
            if (dx <= 0d || Math.Abs(dy) > GeometryToleranceM || Math.Abs(dz) > GeometryToleranceM)
                throw new InvalidOperationException("Curtain opening probe requires positive-X horizontal LINE hosts.");
            var lengthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, lengthDrawing, "curtain opening probe/length"), "curtain opening probe/length");
            var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), "curtain opening probe/height");
            var depthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.012d), "curtain opening probe/depth");
            var bottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
            var placement = CadVerticalPlacementResolver.Resolve(document, project, host, line.StartPoint.Z, heightM, bottomOffsetM);
            var detail = CurtainWallDetailPlanner.Plan(LayoutInput(host, family, lengthM, placement.HeightM));
            var openings = CurtainWallPanelBuilderSupport.ReadLineOpenings(
                document, transaction, project, host, line, 1d, 0d, lengthM, heightM, bottomOffsetM, depthM);
            if (openings.Count != 1)
                throw new InvalidOperationException("Curtain opening planner did not resolve exactly one opening rectangle.");
            var plan = CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d);

            var hostHandles = CanonicalHandles(PropertyValues(host, "GeneratedSolidHandle"), "host solid");
            var frameHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainFrameHandles"), "frame solid");
            var panelHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainPanelHandles"), "panel solid");
            if (hostHandles.Count != 1 || frameHandles.Count == 0)
                throw new InvalidOperationException("Curtain opening host/frame output is incomplete.");
            if (CadHandleService.GetLiveSolidHandles(document, hostHandles).Count != hostHandles.Count ||
                CadHandleService.GetLiveSolidHandles(document, frameHandles).Count != frameHandles.Count ||
                CadHandleService.GetLiveSolidHandles(document, panelHandles).Count != panelHandles.Count)
                throw new InvalidOperationException("Curtain opening output contains a non-live Solid3d.");

            RequireExactInteger(host, "GeneratedCurtainPanelCount", plan.Pieces.Count);
            RequireExactInteger(host, "GeneratedCurtainPanelBaseCount", detail.Panels.Count);
            RequireExactInteger(host, "GeneratedCurtainPanelOpeningCount", 1);
            RequireExactInteger(host, "GeneratedCurtainPanelColumns", detail.Layout.Columns);
            RequireExactInteger(host, "GeneratedCurtainPanelRows", detail.Layout.Rows);
            RequireExactProperty(host, "GeneratedCurtainPanelMode", "LinePanelSolids.OpeningAware");
            RequireExactProperty(host, "GeneratedCurtainPanelBuildState", "Complete");
            var recordedArea = RequiredDouble(host, "GeneratedCurtainPanelAreaM2", allowZero: true);
            RequireNear(recordedArea, plan.RemainingPanelAreaM2, "Curtain opening remaining-area metadata");
            if (panelHandles.Count != plan.Pieces.Count)
                throw new InvalidOperationException("Curtain opening handle count does not match authoritative plan pieces.");

            var remainingBySource = new double[detail.Panels.Count];
            foreach (var piece in plan.Pieces)
            {
                if (piece.SourcePanelIndex < 0 || piece.SourcePanelIndex >= remainingBySource.Length)
                    throw new InvalidOperationException("Curtain opening plan contains an invalid source-panel index.");
                if (!(piece.WidthM > 0d) || !(piece.HeightM > 0d) || !(piece.AreaM2 > 0d) ||
                    double.IsNaN(piece.AreaM2) || double.IsInfinity(piece.AreaM2))
                    throw new InvalidOperationException("Curtain opening plan emitted a non-positive/non-finite piece.");
                remainingBySource[piece.SourcePanelIndex] += piece.AreaM2;
                if (PositiveIntersectionArea(piece.X_M, piece.Z_M, piece.WidthM, piece.HeightM, openings[0]) > GeometryToleranceM)
                    throw new InvalidOperationException("Authoritative curtain panel piece intersects the linked opening.");
            }
            var fullyRemoved = 0;
            var partiallyClipped = 0;
            for (var index = 0; index < detail.Panels.Count; index++)
            {
                var original = detail.Panels[index].AreaM2;
                var remaining = remainingBySource[index];
                if (remaining <= GeometryToleranceM) fullyRemoved++;
                else if (remaining < original - GeometryToleranceM) partiallyClipped++;
                else RequireNear(remaining, original, "Uninterrupted curtain panel area");
            }

            var native = ReadNativePieces(document, transaction, panelHandles, line, placement.BottomDrawingUnits);
            var matched = MatchNativePieces(native, plan.Pieces);
            var intersections = native.Count(x =>
                PositiveIntersectionArea(x.X_M, x.Z_M, x.WidthM, x.HeightM, openings[0]) > GeometryToleranceM);
            var completeEmpty = plan.Pieces.Count == 0;
            if (completeEmpty &&
                (host.Properties.TryGetValue("GeneratedCurtainPanelHandles", out var emptyRaw) && !string.IsNullOrWhiteSpace(emptyRaw)))
                throw new InvalidOperationException("Complete-empty curtain panel output retained handle metadata.");

            return new ScenarioEvidence
            {
                Host = host,
                OpeningCategory = opening.Category,
                SourceHandles = sourceHandles,
                OpeningSourceHandles = openingSourceHandles,
                HostHandles = hostHandles,
                FrameHandles = frameHandles,
                PanelHandles = panelHandles,
                SourcePanelCount = detail.Panels.Count,
                OutputPieceCount = plan.Pieces.Count,
                FullyRemovedPanelCount = fullyRemoved,
                PartiallyClippedPanelCount = partiallyClipped,
                NativePlanMatchCount = matched,
                NativeOpeningIntersectionCount = intersections,
                CompleteEmpty = completeEmpty
            };
        }

        private static IReadOnlyList<NativePiece> ReadNativePieces(
            Document document,
            Transaction transaction,
            IReadOnlyList<string> handles,
            Line hostLine,
            double baseDrawing)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count)
                throw new InvalidOperationException("Curtain opening native panel resolution is incomplete.");
            var result = new List<NativePiece>(ids.Count);
            foreach (var id in ids)
            {
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException("Curtain opening generated panel is not a Solid3d.");
                Extents3d extents;
                try { extents = solid.GeometricExtents; }
                catch (Exception ex) { throw new InvalidOperationException("Cannot read curtain opening panel extents.", ex); }
                var minX = CadGeometryGuard.ToMeters(document, extents.MinPoint.X - hostLine.StartPoint.X, "curtain opening native min X");
                var minZ = CadGeometryGuard.ToMeters(document, extents.MinPoint.Z - baseDrawing, "curtain opening native min Z");
                var width = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, extents.MaxPoint.X - extents.MinPoint.X, "curtain opening native width"), "curtain opening native width");
                var height = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z - extents.MinPoint.Z, "curtain opening native height"), "curtain opening native height");
                result.Add(new NativePiece { X_M = minX, Z_M = minZ, WidthM = width, HeightM = height });
            }
            return result.AsReadOnly();
        }

        private static int MatchNativePieces(IReadOnlyList<NativePiece> native, IReadOnlyList<CurtainWallPanelPiece> planned)
        {
            if (native.Count != planned.Count)
                throw new InvalidOperationException("Curtain opening native/planned piece counts differ.");
            var used = new bool[planned.Count];
            foreach (var actual in native)
            {
                var matches = new List<int>();
                for (var index = 0; index < planned.Count; index++)
                {
                    if (used[index]) continue;
                    var expected = planned[index];
                    if (Near(actual.X_M, expected.X_M) && Near(actual.Z_M, expected.Z_M) &&
                        Near(actual.WidthM, expected.WidthM) && Near(actual.HeightM, expected.HeightM))
                        matches.Add(index);
                }
                if (matches.Count != 1)
                    throw new InvalidOperationException("Curtain opening native panel did not uniquely match one planned piece.");
                used[matches[0]] = true;
            }
            if (used.Any(x => !x))
                throw new InvalidOperationException("Curtain opening planned piece has no native Solid3d match.");
            return used.Count(x => x);
        }

        private static double PositiveIntersectionArea(
            double x,
            double z,
            double width,
            double height,
            CurtainWallOpeningRect opening)
        {
            var overlapX = Math.Min(x + width, opening.X_M + opening.WidthM) - Math.Max(x, opening.X_M);
            var overlapZ = Math.Min(z + height, opening.Z_M + opening.HeightM) - Math.Max(z, opening.Z_M);
            if (overlapX <= GeometryToleranceM || overlapZ <= GeometryToleranceM) return 0d;
            var area = overlapX * overlapZ;
            if (double.IsNaN(area) || double.IsInfinity(area))
                throw new OverflowException("Curtain opening intersection area is not finite.");
            return area;
        }

        private static CurtainWallLayoutInput LayoutInput(
            ProjectElement element,
            ProjectFamily? family,
            double lengthM,
            double heightM) => new CurtainWallLayoutInput
        {
            LengthM = lengthM,
            HeightM = heightM,
            MaxPanelWidthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelWidthM", 1.2d), "curtain opening/max panel width"),
            MaxPanelHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelHeightM", 1.5d), "curtain opening/max panel height"),
            PerimeterFrameWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d), "curtain opening/perimeter frame"),
            MullionWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainMullionWidthM", 0.05d), "curtain opening/mullion"),
            TransomWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainTransomWidthM", 0.05d), "curtain opening/transom")
        };

        private static IReadOnlyList<ProjectElement> LinkedOpenings(ProjectState project, ProjectElement host) =>
            project.Elements
                .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) &&
                            x.Properties.TryGetValue("HostWallId", out var hostId) &&
                            string.Equals(hostId, host.Id, StringComparison.Ordinal))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        private static void RequireSyntheticLine(
            Document document,
            Line host,
            Line opening,
            ElementCategory category)
        {
            var expectedHostY = category == ElementCategory.Door ? 0d : 10d;
            var expectedOpeningStartX = category == ElementCategory.Door ? 0.8d : 0.05d;
            var expectedOpeningEndX = category == ElementCategory.Door ? 2.2d : 4.95d;
            RequireNear(CadGeometryGuard.ToMeters(document, host.StartPoint.X, "curtain opening synthetic host start X"), 0d, "Synthetic GlassWall start X");
            RequireNear(CadGeometryGuard.ToMeters(document, host.StartPoint.Y, "curtain opening synthetic host start Y"), expectedHostY, "Synthetic GlassWall start Y");
            RequireNear(CadGeometryGuard.ToMeters(document, host.StartPoint.Z, "curtain opening synthetic host start Z"), 0d, "Synthetic GlassWall start Z");
            var hostLengthM = CadGeometryGuard.ToMeters(document, host.EndPoint.X - host.StartPoint.X, "curtain opening synthetic host length");
            RequireNear(hostLengthM, 5d, "Synthetic GlassWall length");
            RequireNear(CadGeometryGuard.ToMeters(document, host.EndPoint.Y - host.StartPoint.Y, "curtain opening synthetic host Y"), 0d, "Synthetic GlassWall Y delta");
            RequireNear(CadGeometryGuard.ToMeters(document, host.EndPoint.Z - host.StartPoint.Z, "curtain opening synthetic host Z"), 0d, "Synthetic GlassWall Z delta");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.StartPoint.X, "curtain opening synthetic opening start X"), expectedOpeningStartX, "Synthetic opening start X");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.EndPoint.X, "curtain opening synthetic opening end X"), expectedOpeningEndX, "Synthetic opening end X");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.StartPoint.Y, "curtain opening synthetic opening start Y"), expectedHostY, "Synthetic opening start Y");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.StartPoint.Z, "curtain opening synthetic opening start Z"), 0d, "Synthetic opening start Z");
            var openingWidthM = CadGeometryGuard.ToMeters(document, opening.EndPoint.X - opening.StartPoint.X, "curtain opening synthetic opening width");
            RequireNear(openingWidthM, category == ElementCategory.Door ? 1.4d : 4.9d, "Synthetic opening width");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.EndPoint.Y - opening.StartPoint.Y, "curtain opening synthetic opening Y"), 0d, "Synthetic opening Y delta");
            RequireNear(CadGeometryGuard.ToMeters(document, opening.EndPoint.Z - opening.StartPoint.Z, "curtain opening synthetic opening Z"), 0d, "Synthetic opening Z delta");
        }

        private static void RequireLegacyNoLevel(ProjectElement element, string label)
        {
            if (CadVerticalPlacementResolver.HasConfiguredLevel(element))
                throw new InvalidOperationException("Curtain opening " + label + " must use legacy/no-Level placement.");
        }

        private static IReadOnlyList<string> PropertyValues(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.None);
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> values, string label)
        {
            var result = values
                .Select(x => CadHandleService.NormalizeHexHandle(x)
                    ?? throw new InvalidDataException("Curtain opening " + label + " contains an invalid handle."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return result.AsReadOnly();
        }

        private static void RequireDisjoint(params IReadOnlyList<string>[] groups)
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
                foreach (var handle in group)
                    if (!all.Add(handle))
                        throw new InvalidOperationException("Curtain opening source/generated ownership sets overlap.");
        }

        private static void RequireExactInteger(ProjectElement element, string key, int expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value != expected)
                throw new InvalidDataException(key + " does not match the authoritative Curtain opening plan.");
        }

        private static void RequireExactProperty(ProjectElement element, string key, string expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !string.Equals(raw, expected, StringComparison.Ordinal))
                throw new InvalidDataException(key + " is missing or non-canonical.");
        }

        private static double RequiredDouble(ProjectElement element, string key, bool allowZero)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
                throw new InvalidDataException(key + " is missing or invalid.");
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " must be >= 0.");
            return value;
        }

        private static bool Near(double actual, double expected) => Math.Abs(actual - expected) <= GeometryToleranceM;

        private static void RequireNear(double actual, double expected, string label)
        {
            if (!Near(actual, expected))
                throw new InvalidOperationException(label + " does not match the expected synthetic/plan value.");
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curtain opening result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain opening result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DCURTAINOPENINGPROBE",
                        "error_code=CURTAIN_PANEL_OPENING_RUNTIME_FAILED"
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Curtain opening result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
