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
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P04 probe for native panels mapped across one
    /// synthetic open bulged WCS-XY POLYLINE. The result qualifies bounded
    /// tessellated fragments only, never exact swept-curve geometry.
    /// </summary>
    public sealed class CurtainPanelBulgedPathRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_BULGED_PATH_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_BULGED_PATH_NONCE";
        private const string ResultFileName = "curtain-panel-bulged-path-runtime-result.txt";
        private const double ConfiguredSagittaM = 0.001d;
        private const double ArcRadiusM = 2d;
        private const int MaximumTessellatedSegments = 4096;
        private const int MaximumPathPoints = 8192;
        private const int MaximumPanelPieces = 4096;
        private const double GeometryToleranceM = 1e-6d;

        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH",
            "SEED_SOURCE",
            "CONFIGURE_SOURCE",
            "PROJECT_DISCOVERY",
            "OUTPUT_DISCOVERY",
            "SOURCE_SHAPE",
            "TESSELLATION_BOUND",
            "PLAN_RECONSTRUCTION",
            "OUTPUT_OWNERSHIP",
            "METADATA",
            "PLANNED_GEOMETRY",
            "NATIVE_GEOMETRY",
            "OWNERSHIP_DISJOINT",
            "HEALTH",
            "LOCATE",
            "RESULT_PUBLISH"
        };

        private static readonly HashSet<string> FailureCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "STATE_REJECTED",
            "DATA_REJECTED",
            "IO_REJECTED",
            "OVERFLOW_REJECTED",
            "UNEXPECTED_REJECTED"
        };

        private sealed class ProbePhaseTracker
        {
            public string Value { get; private set; } = "PROBE_AUTH";

            public void Set(string phase)
            {
                if (!FailurePhases.Contains(phase))
                    throw new InvalidOperationException("Curtain bulged-path probe phase is not allowlisted.");
                Value = phase;
            }
        }

        private sealed class NativeBounds
        {
            public double MinX_M { get; set; }
            public double MaxX_M { get; set; }
            public double MinY_M { get; set; }
            public double MaxY_M { get; set; }
            public double MinZ_M { get; set; }
            public double MaxZ_M { get; set; }
        }

        private sealed class ProbeEvidence
        {
            public ProjectElement Host { get; set; } = null!;
            public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> HostHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> FrameHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> PanelHandles { get; set; } = Array.Empty<string>();
            public int SourceVertexCount { get; set; }
            public int RawSegmentCount { get; set; }
            public int CurvedTessellationSegmentCount { get; set; }
            public int TessellatedPathSegmentCount { get; set; }
            public int SourcePanelCount { get; set; }
            public int PathPieceCount { get; set; }
            public int CurvedPieceCount { get; set; }
            public int CurvedSegmentsWithPanels { get; set; }
            public int StraightPieceCount { get; set; }
            public int NativeMatchCount { get; set; }
        }

        [CommandMethod("QS3DCURTAINBULGEDSEED", CommandFlags.Modal)]
        public void SeedSource()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireAutomation(requestedPath, nonce, "Curtain bulged-path seed");
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                ObjectId sourceId;
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var x4 = CadGeometryGuard.ToDrawingUnits(document, 4d, "Curtain bulged-path seed X4");
                    var x7 = CadGeometryGuard.ToDrawingUnits(document, 7d, "Curtain bulged-path seed X7");
                    var polyline = new Polyline();
                    try
                    {
                        polyline.SetDatabaseDefaults(document.Database);
                        polyline.AddVertexAt(0, new Point2d(0d, 0d), 1d, 0d, 0d);
                        polyline.AddVertexAt(1, new Point2d(x4, 0d), 0d, 0d, 0d);
                        polyline.AddVertexAt(2, new Point2d(x7, 0d), 0d, 0d, 0d);
                        polyline.Closed = false;
                        polyline.Elevation = 0d;
                        polyline.Normal = Vector3d.ZAxis;
                        sourceId = modelSpace.AppendEntity(polyline);
                        transaction.AddNewlyCreatedDBObject(polyline, true);
                        transaction.Commit();
                        polyline = null!;
                    }
                    finally { polyline?.Dispose(); }
                }
                document.Editor.SetImpliedSelection(new[] { sourceId });
            }
            catch (System.Exception error)
            {
                TryWriteFailure(requestedPath, nonce, "SEED_SOURCE", FailureCode(error));
                throw;
            }
        }

        [CommandMethod("QS3DCURTAINBULGEDPREPARE", CommandFlags.Modal)]
        public void ConfigureAndPrepareSource()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireAutomation(requestedPath, nonce, "Curtain bulged-path prepare");
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                var project = ExistingProjectMutationContext.Require(document, "Curtain P04 runtime sagitta configuration");
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    project.Metadata["WallArcSagittaM"] = ConfiguredSagittaM.ToString("R", CultureInfo.InvariantCulture);
                    project.Touch();

                    var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
                    if (hosts.Count != 1)
                        throw new InvalidOperationException("Curtain bulged-path prepare requires exactly one GlassWall.");
                    RequireLegacyNoLevel(hosts[0]);
                    var sourceHandles = CanonicalHandles(hosts[0].SourceHandles, "source");
                    if (sourceHandles.Count != 1)
                        throw new InvalidOperationException("Curtain bulged-path prepare requires exactly one canonical source.");
                    var sourceIds = CadHandleService.Resolve(document, sourceHandles);
                    if (sourceIds.Count != 1)
                        throw new InvalidOperationException("Curtain bulged-path prepare could not resolve the canonical source.");
                    document.Editor.SetImpliedSelection(sourceIds.ToArray());
                }
                catch
                {
                    rollback.Restore(project);
                    throw;
                }
            }
            catch (System.Exception error)
            {
                TryWriteFailure(requestedPath, nonce, "CONFIGURE_SOURCE", FailureCode(error));
                throw;
            }
        }

        [CommandMethod("QS3DCURTAINBULGEDPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain bulged-path probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            var phase = new ProbePhaseTracker();
            try
            {
                RequireAutomation(requestedPath, nonce, "Curtain bulged-path probe");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Curtain bulged-path result already exists.");

                phase.Set("PROJECT_DISCOVERY");
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Curtain bulged-path probe requires an existing QS3D project.");
                RequireNear(ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d), ConfiguredSagittaM, "Configured sagitta");

                phase.Set("OUTPUT_DISCOVERY");
                var hosts = project.Elements
                    .Where(x => x.Category == ElementCategory.GlassWall &&
                                x.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state) &&
                                string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (hosts.Count != 1)
                    throw new InvalidOperationException("Curtain bulged-path probe requires exactly one completed GlassWall panel owner.");
                RequireLegacyNoLevel(hosts[0]);

                ProbeEvidence evidence;
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    evidence = Inspect(document, transaction, project, hosts[0], phase);
                    transaction.Commit();
                }

                phase.Set("OWNERSHIP_DISJOINT");
                RequireDisjoint(evidence.SourceHandles, evidence.HostHandles, evidence.FrameHandles, evidence.PanelHandles);

                phase.Set("HEALTH");
                var livePanels = new HashSet<string>(evidence.PanelHandles, StringComparer.OrdinalIgnoreCase);
                var coreIssues = new GeneratedCurtainPanelHealthService().Inspect(project, livePanels);
                var liveIssues = CurtainWallPanelLiveStateService.Inspect(document, project);
                var runtimeIssues = GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project);
                var blockingIssues = coreIssues.Concat(liveIssues).Concat(runtimeIssues)
                    .Count(x => x.Severity != HealthSeverity.Info);
                if (blockingIssues != 0)
                    throw new InvalidOperationException("Curtain bulged-path panel Health is not clean.");

                phase.Set("LOCATE");
                var locatedIds = CadHandleService.Resolve(document, new[] { evidence.PanelHandles[0] });
                if (locatedIds.Count != 1)
                    throw new InvalidOperationException("Curtain bulged-path probe cannot resolve one panel for Locate.");
                document.Editor.SetImpliedSelection(locatedIds.ToArray());
                var owners = SemanticSelectionResolver.ResolveImplied(document, project);
                if (owners.Count != 1 || !ReferenceEquals(owners[0], evidence.Host))
                    throw new InvalidOperationException("Curtain bulged-path panel Locate did not resolve one canonical GlassWall.");

                phase.Set("RESULT_PUBLISH");
                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DCURTAINBULGEDPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1",
                    "qualification_boundary=LOCAL_002_P04_ONLY",
                    "production_local002_qualified=false",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "legacy_no_level=true",
                    "open_bulged_polyline=true",
                    "tessellated_fragments_only=true",
                    "exact_swept_curve_qualified=false",
                    "source_geometry_preserved=true",
                    "ownership_sets_disjoint=true",
                    "panel_build_state_complete=true",
                    "budget_within_limits=true",
                    "maximum_chord_sagitta_within_limit=true",
                    "glass_wall_count=1",
                    "source_vertex_count=" + evidence.SourceVertexCount.ToString(CultureInfo.InvariantCulture),
                    "source_raw_segment_count=" + evidence.RawSegmentCount.ToString(CultureInfo.InvariantCulture),
                    "configured_sagitta_mm=1",
                    "arc_radius_m=2",
                    "curved_tessellation_segment_count=" + evidence.CurvedTessellationSegmentCount.ToString(CultureInfo.InvariantCulture),
                    "tessellated_path_segment_count=" + evidence.TessellatedPathSegmentCount.ToString(CultureInfo.InvariantCulture),
                    "source_panel_count=" + evidence.SourcePanelCount.ToString(CultureInfo.InvariantCulture),
                    "authoritative_path_piece_count=" + evidence.PathPieceCount.ToString(CultureInfo.InvariantCulture),
                    "curved_piece_count=" + evidence.CurvedPieceCount.ToString(CultureInfo.InvariantCulture),
                    "curved_segments_with_panels=" + evidence.CurvedSegmentsWithPanels.ToString(CultureInfo.InvariantCulture),
                    "straight_piece_count=" + evidence.StraightPieceCount.ToString(CultureInfo.InvariantCulture),
                    "native_plan_match_count=" + evidence.NativeMatchCount.ToString(CultureInfo.InvariantCulture),
                    "host_solid_count=" + evidence.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "frame_solid_count=" + evidence.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_solid_count=" + evidence.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_metadata_count=" + evidence.PathPieceCount.ToString(CultureInfo.InvariantCulture),
                    "health_issue_count=0",
                    "located_panel_count=1",
                    "canonical_owner_count=1"
                });
                document.Editor.WriteMessage("\nQS3D curtain bulged-path probe PASS.");
            }
            catch (System.Exception error)
            {
                TryWriteFailure(requestedPath, nonce, phase.Value, FailureCode(error));
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain bulged-path probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static ProbeEvidence Inspect(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            ProbePhaseTracker phase)
        {
            phase.Set("SOURCE_SHAPE");
            var sourceHandles = CanonicalHandles(host.SourceHandles, "source");
            if (sourceHandles.Count != 1)
                throw new InvalidOperationException("Curtain bulged-path source ownership is incomplete.");
            var sourceIds = CadHandleService.Resolve(document, sourceHandles);
            if (sourceIds.Count != 1)
                throw new InvalidOperationException("Curtain bulged-path source is not live.");
            var polyline = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Polyline
                ?? throw new InvalidOperationException("Curtain bulged-path source is not a POLYLINE.");
            RequireSyntheticPolyline(document, polyline);

            phase.Set("TESSELLATION_BOUND");
            var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
            RequireNear(sagittaM, ConfiguredSagittaM, "Curtain bulged-path sagitta");
            var curvedSegmentCount = IndependentExpectedSegmentCount(Math.PI, ArcRadiusM, sagittaM);
            if (curvedSegmentCount != 50 || curvedSegmentCount > MaximumTessellatedSegments)
                throw new InvalidOperationException("Curtain bulged-path independent segment bound is unexpected.");
            var centerline = CadPolylinePathReader.ReadOpenWcsXy(document, polyline, sagittaM, "curtain bulged-path probe/source");
            if (centerline.Count != curvedSegmentCount + 2 || centerline.Count > MaximumPathPoints)
                throw new InvalidOperationException("Curtain bulged-path tessellation count is outside the independent bound.");
            RequireTessellatedArc(centerline, curvedSegmentCount, sagittaM);

            phase.Set("PLAN_RECONSTRUCTION");
            var lengthM = CurtainPathFramePlanner.Length(centerline);
            var family = project.FindFamily(host.FamilyId);
            var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), "curtain bulged-path probe/height");
            var depthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.012d), "curtain bulged-path probe/depth");
            var bottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
            var placement = CadVerticalPlacementResolver.Resolve(document, project, host, polyline.Elevation, heightM, bottomOffsetM);
            var detail = CurtainWallDetailPlanner.Plan(LayoutInput(host, family, lengthM, placement.HeightM));
            var rectangles = detail.Panels
                .Select(x => new CurtainWallRect(x.X_M, x.Z_M, x.WidthM, x.HeightM))
                .ToList();
            var pathPlan = CurtainPathFramePlanner.Plan(centerline, rectangles);
            if (pathPlan.PathSegmentCount != curvedSegmentCount + 1 || pathPlan.SourceFrameCount != rectangles.Count || pathPlan.Pieces.Count == 0)
                throw new InvalidOperationException("Curtain bulged-path authoritative plan is incomplete.");
            if (detail.Panels.Count > MaximumPanelPieces || pathPlan.Pieces.Count > MaximumPanelPieces)
                throw new InvalidOperationException("Curtain bulged-path authoritative plan exceeds the published panel budget.");

            phase.Set("OUTPUT_OWNERSHIP");
            var hostHandles = CanonicalHandles(PropertyValues(host, "GeneratedSolidHandle"), "host solid");
            var frameHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainFrameHandles"), "frame solid");
            var panelHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainPanelHandles"), "panel solid");
            if (hostHandles.Count != 1 || frameHandles.Count == 0 || panelHandles.Count == 0)
                throw new InvalidOperationException("Curtain bulged-path host/frame/panel output is incomplete.");
            if (CadHandleService.GetLiveSolidHandles(document, hostHandles).Count != hostHandles.Count ||
                CadHandleService.GetLiveSolidHandles(document, frameHandles).Count != frameHandles.Count ||
                CadHandleService.GetLiveSolidHandles(document, panelHandles).Count != panelHandles.Count)
                throw new InvalidOperationException("Curtain bulged-path output contains a non-live Solid3d.");

            phase.Set("METADATA");
            RequireExactInteger(host, "GeneratedCurtainPanelCount", pathPlan.Pieces.Count);
            RequireExactInteger(host, "GeneratedCurtainPanelBaseCount", detail.Panels.Count);
            RequireExactInteger(host, "GeneratedCurtainPanelOpeningCount", 0);
            RequireExactInteger(host, "GeneratedCurtainPanelColumns", detail.Layout.Columns);
            RequireExactInteger(host, "GeneratedCurtainPanelRows", detail.Layout.Rows);
            RequireExactInteger(host, "GeneratedCurtainPanelPathSegmentCount", pathPlan.PathSegmentCount);
            RequireExactInteger(host, "GeneratedCurtainPanelMappedCount", rectangles.Count);
            RequireExactProperty(host, "GeneratedCurtainPanelMode", "PathPanelSolids");
            RequireExactProperty(host, "GeneratedCurtainPanelSourceKind", "OpenPolyline");
            RequireExactProperty(host, "GeneratedCurtainPanelBuildState", "Complete");
            RequireNear(RequiredDouble(host, "GeneratedCurtainPanelSourceLengthM", false), lengthM, "Curtain bulged-path length metadata");
            RequireNear(RequiredDouble(host, "GeneratedCurtainPanelDepthM", false), depthM, "Curtain bulged-path depth metadata");
            RequireNear(RequiredDouble(host, "GeneratedCurtainPanelHeightM", false), placement.HeightM, "Curtain bulged-path height metadata");
            RequireNear(RequiredDouble(host, "GeneratedCurtainPanelAreaM2", false), detail.Panels.Sum(x => x.AreaM2), "Curtain bulged-path area metadata");
            if (panelHandles.Count != pathPlan.Pieces.Count)
                throw new InvalidOperationException("Curtain bulged-path Handle count does not match authoritative path pieces.");

            phase.Set("PLANNED_GEOMETRY");
            var curvedPieceCount = 0;
            var straightPieceCount = 0;
            var curvedSegmentsWithPanels = new HashSet<int>();
            foreach (var piece in pathPlan.Pieces)
            {
                if (!(piece.WidthM > 0d) || !(piece.HeightM > 0d) ||
                    double.IsNaN(piece.WidthM) || double.IsInfinity(piece.WidthM) ||
                    double.IsNaN(piece.HeightM) || double.IsInfinity(piece.HeightM))
                    throw new InvalidOperationException("Curtain bulged-path plan emitted a non-positive/non-finite piece.");
                if (piece.PathSegmentIndex >= 0 && piece.PathSegmentIndex < curvedSegmentCount)
                {
                    curvedPieceCount++;
                    curvedSegmentsWithPanels.Add(piece.PathSegmentIndex);
                }
                else if (piece.PathSegmentIndex == curvedSegmentCount) straightPieceCount++;
                else throw new InvalidOperationException("Curtain bulged-path plan emitted an unexpected path segment.");
            }
            if (curvedPieceCount == 0 || curvedSegmentsWithPanels.Count <= 1 || straightPieceCount == 0)
                throw new InvalidOperationException("Curtain bulged-path plan did not map panels across curved and straight segments.");

            phase.Set("NATIVE_GEOMETRY");
            var native = ReadNativeBounds(document, transaction, panelHandles);
            var baseM = CadGeometryGuard.ToMeters(document, placement.BottomDrawingUnits, "curtain bulged-path probe/base Z");
            var nativeMatchCount = MatchNativePieces(native, pathPlan.Pieces, depthM, baseM);

            return new ProbeEvidence
            {
                Host = host,
                SourceHandles = sourceHandles,
                HostHandles = hostHandles,
                FrameHandles = frameHandles,
                PanelHandles = panelHandles,
                SourceVertexCount = polyline.NumberOfVertices,
                RawSegmentCount = polyline.NumberOfVertices - 1,
                CurvedTessellationSegmentCount = curvedSegmentCount,
                TessellatedPathSegmentCount = pathPlan.PathSegmentCount,
                SourcePanelCount = detail.Panels.Count,
                PathPieceCount = pathPlan.Pieces.Count,
                CurvedPieceCount = curvedPieceCount,
                CurvedSegmentsWithPanels = curvedSegmentsWithPanels.Count,
                StraightPieceCount = straightPieceCount,
                NativeMatchCount = nativeMatchCount
            };
        }

        private static int IndependentExpectedSegmentCount(double includedAngle, double radiusM, double sagittaM)
        {
            if (!(includedAngle > 0d) || !(radiusM > 0d) || !(sagittaM > 0d))
                throw new InvalidOperationException("Curtain bulged-path independent tessellation inputs must be positive.");
            var maximumAngle = Math.PI / 18d;
            var sagittaRatio = sagittaM / radiusM;
            var segmentAngle = maximumAngle;
            if (sagittaRatio < 2d)
            {
                var quarterSine = Math.Sqrt(Math.Min(1d, sagittaRatio * 0.5d));
                var bySagitta = 4d * Math.Asin(quarterSine);
                if (!(bySagitta > 0d) || double.IsNaN(bySagitta) || double.IsInfinity(bySagitta))
                    throw new InvalidOperationException("Curtain bulged-path independent sagitta angle is invalid.");
                segmentAngle = Math.Min(segmentAngle, bySagitta);
            }
            var required = Math.Ceiling(includedAngle / segmentAngle);
            if (double.IsNaN(required) || double.IsInfinity(required) || required > MaximumTessellatedSegments)
                throw new InvalidOperationException("Curtain bulged-path independent tessellation exceeds its bound.");
            return Math.Max(1, (int)required);
        }

        private static void RequireTessellatedArc(IReadOnlyList<Point2> centerline, int curvedSegmentCount, double sagittaM)
        {
            var center = new Point2(2d, 0d);
            RequireNear(centerline[0].X, 0d, "Curtain bulged-path first X");
            RequireNear(centerline[0].Y, 0d, "Curtain bulged-path first Y");
            RequireNear(centerline[curvedSegmentCount].X, 4d, "Curtain bulged-path arc end X");
            RequireNear(centerline[curvedSegmentCount].Y, 0d, "Curtain bulged-path arc end Y");
            RequireNear(centerline[curvedSegmentCount + 1].X, 7d, "Curtain bulged-path straight end X");
            RequireNear(centerline[curvedSegmentCount + 1].Y, 0d, "Curtain bulged-path straight end Y");

            for (var index = 0; index <= curvedSegmentCount; index++)
                RequireNear(centerline[index].DistanceTo(center), ArcRadiusM, "Curtain bulged-path tessellated radius");
            for (var index = 0; index < curvedSegmentCount; index++)
            {
                var chordM = centerline[index].DistanceTo(centerline[index + 1]);
                if (!(chordM > 0d) || chordM > ArcRadiusM * 2d)
                    throw new InvalidOperationException("Curtain bulged-path tessellated chord is invalid.");
                var halfChord = chordM / 2d;
                var chordSagitta = ArcRadiusM - Math.Sqrt(Math.Max(0d, ArcRadiusM * ArcRadiusM - halfChord * halfChord));
                if (double.IsNaN(chordSagitta) || double.IsInfinity(chordSagitta) || chordSagitta > sagittaM + GeometryToleranceM)
                    throw new InvalidOperationException("Curtain bulged-path chord exceeds configured sagitta.");
            }
        }

        private static IReadOnlyList<NativeBounds> ReadNativeBounds(
            Document document,
            Transaction transaction,
            IReadOnlyList<string> handles)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count)
                throw new InvalidOperationException("Curtain bulged-path native panel resolution is incomplete.");
            var result = new List<NativeBounds>(ids.Count);
            foreach (var id in ids)
            {
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException("Curtain bulged-path generated panel is not a Solid3d.");
                Extents3d extents;
                try { extents = solid.GeometricExtents; }
                catch (Exception ex) { throw new InvalidOperationException("Cannot read Curtain bulged-path panel extents.", ex); }
                result.Add(new NativeBounds
                {
                    MinX_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.X, "curtain bulged-path native min X"),
                    MaxX_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.X, "curtain bulged-path native max X"),
                    MinY_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.Y, "curtain bulged-path native min Y"),
                    MaxY_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Y, "curtain bulged-path native max Y"),
                    MinZ_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, "curtain bulged-path native min Z"),
                    MaxZ_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, "curtain bulged-path native max Z")
                });
            }
            return result.AsReadOnly();
        }

        private static int MatchNativePieces(
            IReadOnlyList<NativeBounds> native,
            IReadOnlyList<CurtainPathFramePiece> planned,
            double depthM,
            double baseM)
        {
            if (native.Count != planned.Count)
                throw new InvalidOperationException("Curtain bulged-path native/planned piece counts differ.");
            var used = new bool[planned.Count];
            foreach (var actual in native)
            {
                var matches = new List<int>();
                for (var index = 0; index < planned.Count; index++)
                {
                    if (used[index]) continue;
                    var expected = planned[index];
                    var halfX = Math.Abs(Math.Cos(expected.AngleRadians)) * expected.WidthM / 2d +
                                Math.Abs(Math.Sin(expected.AngleRadians)) * depthM / 2d;
                    var halfY = Math.Abs(Math.Sin(expected.AngleRadians)) * expected.WidthM / 2d +
                                Math.Abs(Math.Cos(expected.AngleRadians)) * depthM / 2d;
                    if (Near(actual.MinX_M, expected.CenterX_M - halfX) && Near(actual.MaxX_M, expected.CenterX_M + halfX) &&
                        Near(actual.MinY_M, expected.CenterY_M - halfY) && Near(actual.MaxY_M, expected.CenterY_M + halfY) &&
                        Near(actual.MinZ_M, baseM + expected.Z_M) && Near(actual.MaxZ_M, baseM + expected.Z_M + expected.HeightM))
                        matches.Add(index);
                }
                if (matches.Count != 1)
                    throw new InvalidOperationException("Curtain bulged-path native panel does not uniquely match one authoritative piece.");
                used[matches[0]] = true;
            }
            if (used.Any(x => !x))
                throw new InvalidOperationException("Curtain bulged-path authoritative piece has no native match.");
            return used.Count(x => x);
        }

        private static CurtainWallLayoutInput LayoutInput(
            ProjectElement element,
            ProjectFamily? family,
            double lengthM,
            double heightM) => new CurtainWallLayoutInput
        {
            LengthM = lengthM,
            HeightM = heightM,
            MaxPanelWidthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelWidthM", 1.2d), "curtain bulged-path probe/max width"),
            MaxPanelHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelHeightM", 1.5d), "curtain bulged-path probe/max height"),
            PerimeterFrameWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d), "curtain bulged-path probe/perimeter"),
            MullionWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainMullionWidthM", 0.05d), "curtain bulged-path probe/mullion"),
            TransomWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainTransomWidthM", 0.05d), "curtain bulged-path probe/transom")
        };

        private static void RequireSyntheticPolyline(Document document, Polyline polyline)
        {
            if (polyline.Closed || polyline.NumberOfVertices != 3)
                throw new InvalidOperationException("Curtain bulged-path probe requires one open three-vertex POLYLINE.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > GeometryToleranceM || Math.Abs(normal.Y) > GeometryToleranceM || Math.Abs(normal.Z - 1d) > GeometryToleranceM)
                throw new InvalidOperationException("Curtain bulged-path probe requires +Z WCS-XY source geometry.");
            RequireNear(CadGeometryGuard.ToMeters(document, polyline.Elevation, "curtain bulged-path source elevation"), 0d, "Curtain bulged-path source elevation");
            RequireNear(polyline.GetBulgeAt(0), 1d, "Curtain bulged-path source arc bulge");
            RequireNear(polyline.GetBulgeAt(1), 0d, "Curtain bulged-path source straight bulge");
            RequireNear(polyline.GetBulgeAt(2), 0d, "Curtain bulged-path source terminal bulge");
            var expected = new[] { new Point2(0d, 0d), new Point2(4d, 0d), new Point2(7d, 0d) };
            for (var index = 0; index < expected.Length; index++)
            {
                var point = polyline.GetPoint2dAt(index);
                RequireNear(CadGeometryGuard.ToMeters(document, point.X, "curtain bulged-path source X"), expected[index].X, "Curtain bulged-path source X");
                RequireNear(CadGeometryGuard.ToMeters(document, point.Y, "curtain bulged-path source Y"), expected[index].Y, "Curtain bulged-path source Y");
            }
        }

        private static void RequireAutomation(string? requestedPath, string nonce, string label)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException(label + " is automation-only.");
            RequiredResultPath(requestedPath!);
        }

        private static void RequireLegacyNoLevel(ProjectElement element)
        {
            if (CadVerticalPlacementResolver.HasConfiguredLevel(element))
                throw new InvalidOperationException("Curtain bulged-path probe requires legacy/no-Level placement.");
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
                    ?? throw new InvalidDataException("Curtain bulged-path " + label + " contains an invalid Handle token."))
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
                        throw new InvalidOperationException("Curtain bulged-path source/generated ownership sets overlap.");
        }

        private static void RequireExactInteger(ProjectElement element, string key, int expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value != expected)
                throw new InvalidDataException(key + " does not match the authoritative Curtain bulged-path plan.");
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

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException("Curtain bulged-path project metadata is invalid.");
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
                throw new InvalidOperationException("Curtain bulged-path result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain bulged-path result directory must already exist.");
            return fullPath;
        }

        private static string FailureCode(Exception error)
        {
            if (error is InvalidDataException) return "DATA_REJECTED";
            if (error is OverflowException) return "OVERFLOW_REJECTED";
            if (error is IOException) return "IO_REJECTED";
            if (error is InvalidOperationException) return "STATE_REJECTED";
            return "UNEXPECTED_REJECTED";
        }

        private static void TryWriteFailure(string? requestedPath, string nonce, string phase, string failureCode)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized) &&
                    Guid.TryParseExact(nonce, "N", out _) && FailurePhases.Contains(phase) &&
                    FailureCodes.Contains(failureCode))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DCURTAINBULGEDPROBE",
                        "nonce=" + nonce,
                        "schema=QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1",
                        "qualification_boundary=LOCAL_002_P04_ONLY",
                        "production_local002_qualified=false",
                        "error_code=CURTAIN_PANEL_BULGED_PATH_RUNTIME_FAILED",
                        "failure_phase=" + phase,
                        "failure_code=" + failureCode
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Curtain bulged-path result already exists.");
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
