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
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P05 state machine. It proves bounded stale,
    /// Health and replacement transitions on synthetic legacy/no-Level LINE
    /// owners while keeping all production mutation/build services unchanged.
    /// </summary>
    public sealed class CurtainPanelStaleRebuildRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_STALE_REBUILD_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_STALE_REBUILD_NONCE";
        private const string ResultFileName = "curtain-panel-stale-rebuild-runtime-result.txt";
        private const double GeometryToleranceM = 1e-6d;

        private enum SequenceStage
        {
            None,
            Prepared,
            Baseline,
            GridStale,
            GridBuilt,
            DepthStale,
            DepthBuilt,
            HeightStale,
            HeightBuilt,
            OpeningStale,
            OpeningBuilt,
            SourceEdited,
            SourceSynced,
            Complete
        }

        private enum ChangeKind
        {
            Grid,
            Depth,
            Height,
            Opening,
            Source
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

        private sealed class OwnerSample
        {
            public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> HostHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> FrameHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> PanelHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<NativeBounds> NativeBounds { get; set; } = Array.Empty<NativeBounds>();
            public string ConfigFingerprint { get; set; } = string.Empty;
            public string LiveFingerprint { get; set; } = string.Empty;
            public int Columns { get; set; }
            public int Rows { get; set; }
            public int BasePanelCount { get; set; }
            public int OpeningCount { get; set; }
            public int PanelCount { get; set; }
            public double DepthM { get; set; }
            public double HeightM { get; set; }
            public double SourceLengthM { get; set; }
            public double AreaM2 { get; set; }
        }

        private sealed class SequenceState
        {
            public string Nonce { get; set; } = string.Empty;
            public string TargetId { get; set; } = string.Empty;
            public string ControlId { get; set; } = string.Empty;
            public string OpeningId { get; set; } = string.Empty;
            public SequenceStage Stage { get; set; }
            public OwnerSample Target { get; set; } = null!;
            public OwnerSample Control { get; set; } = null!;
            public int ReplacementCount { get; set; }
            public int StaleTransitionCount { get; set; }
            public int NativeMatchCount { get; set; }
        }

        private static readonly object StateSync = new object();
        private static SequenceState? State;

        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH",
            "SEED_HOSTS",
            "SEED_OPENING",
            "PREPARE_BASELINE",
            "VERIFY_BASELINE",
            "MUTATE_GRID",
            "VERIFY_GRID_REBUILD",
            "MUTATE_DEPTH",
            "VERIFY_DEPTH_REBUILD",
            "MUTATE_HEIGHT",
            "VERIFY_HEIGHT_REBUILD",
            "MUTATE_OPENING_RELATION",
            "VERIFY_OPENING_REBUILD",
            "MUTATE_SOURCE_GEOMETRY",
            "VERIFY_SOURCE_SYNC",
            "VERIFY_SOURCE_REBUILD",
            "OUTPUT_OWNERSHIP",
            "METADATA",
            "PLANNED_GEOMETRY",
            "NATIVE_GEOMETRY",
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

        [CommandMethod("QS3DCURTAINSTALESEEDHOSTS", CommandFlags.Modal)]
        public void SeedHosts() => ExecuteStage("SEED_HOSTS", (document, _, nonce) =>
        {
            var ids = new List<ObjectId>(2);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ids.Add(AppendLine(document, transaction, modelSpace, 0d, 0d, 5d, 0d, "P05 target host"));
                ids.Add(AppendLine(document, transaction, modelSpace, 0d, 10d, 5d, 10d, "P05 control host"));
                transaction.Commit();
            }
            lock (StateSync) State = new SequenceState { Nonce = nonce, Stage = SequenceStage.None };
            document.Editor.SetImpliedSelection(ids.ToArray());
        });

        [CommandMethod("QS3DCURTAINSTALESEEDOPENING", CommandFlags.Modal)]
        public void SeedOpening() => ExecuteStage("SEED_OPENING", (document, _, nonce) =>
        {
            RequireState(nonce, SequenceStage.None);
            ObjectId id;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                id = AppendLine(document, transaction, modelSpace, 2d, 0d, 3d, 0d, "P05 opening");
                transaction.Commit();
            }
            document.Editor.SetImpliedSelection(new[] { id });
        });

        [CommandMethod("QS3DCURTAINSTALEPREPARE", CommandFlags.Modal)]
        public void PrepareBaseline() => ExecuteStage("PREPARE_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.None);
            var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            var openings = project.Elements.Where(x => x.Category == ElementCategory.Door).ToList();
            if (hosts.Count != 2 || openings.Count != 1)
                throw new InvalidOperationException("P05 prepare requires two GlassWalls and one Door.");
            foreach (var element in hosts.Concat(openings)) RequireLegacyNoLevel(element);
            if (openings[0].Properties.ContainsKey("HostWallId") || openings[0].DependsOn.Count != 0)
                throw new InvalidOperationException("P05 Door must begin unlinked.");

            ProjectElement? target = null;
            ProjectElement? control = null;
            var sourceIds = new List<ObjectId>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in hosts)
                {
                    var pair = RequireSingleSourceLine(document, transaction, host);
                    RequireHorizontalLine(document, pair.Value, 5d);
                    var yM = CadGeometryGuard.ToMeters(document, pair.Value.StartPoint.Y, "P05 host Y");
                    if (Near(yM, 0d)) target = host;
                    else if (Near(yM, 10d)) control = host;
                    else throw new InvalidOperationException("P05 host source is outside the synthetic lanes.");
                    sourceIds.Add(pair.Key);
                }
                var openingPair = RequireSingleSourceLine(document, transaction, openings[0]);
                RequireLineCoordinates(document, openingPair.Value, 2d, 0d, 3d, 0d, "P05 opening");
                transaction.Commit();
            }
            if (target == null || control == null || ReferenceEquals(target, control))
                throw new InvalidOperationException("P05 target/control classification failed.");
            state.TargetId = target.Id;
            state.ControlId = control.Id;
            state.OpeningId = openings[0].Id;
            state.Stage = SequenceStage.Prepared;
            document.Editor.SetImpliedSelection(sourceIds.ToArray());
        });

        [CommandMethod("QS3DCURTAINSTALEBASELINE", CommandFlags.Modal)]
        public void VerifyBaseline() => ExecuteStage("VERIFY_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Prepared);
            var target = RequiredElement(project, state.TargetId, ElementCategory.GlassWall);
            var control = RequiredElement(project, state.ControlId, ElementCategory.GlassWall);
            state.Target = InspectOwner(document, project, target, 0);
            state.Control = InspectOwner(document, project, control, 0);
            RequireNear(state.Target.SourceLengthM, 5d, "P05 baseline target length");
            RequireNear(state.Control.SourceLengthM, 5d, "P05 baseline control length");
            RequireNear(state.Target.DepthM, 0.012d, "P05 baseline target depth");
            RequireNear(state.Target.HeightM, 3.6d, "P05 baseline target height");
            RequireDisjoint(project, target, control, RequiredElement(project, state.OpeningId, ElementCategory.Door));
            state.NativeMatchCount = checked(state.Target.PanelCount + state.Control.PanelCount);
            state.Stage = SequenceStage.Baseline;
        });

        [CommandMethod("QS3DCURTAINSTALEMUTATEGRID", CommandFlags.Modal)]
        public void MutateGrid() => ExecuteStage("MUTATE_GRID", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Baseline);
            MutateSemantic(project, state.TargetId, target =>
            {
                target.SetProperty("CurtainMaxPanelWidthM", "0.8");
                target.SetProperty("CurtainMaxPanelHeightM", "1");
            });
            VerifySemanticStale(document, project, state, expectedLiveGeometryStale: false);
            SelectTarget(document, project, state);
            state.StaleTransitionCount++;
            state.Stage = SequenceStage.GridStale;
        });

        [CommandMethod("QS3DCURTAINSTALEVERIFYGRID", CommandFlags.Modal)]
        public void VerifyGridRebuild() => ExecuteStage("VERIFY_GRID_REBUILD", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.GridStale);
            var next = VerifyReplacement(document, project, state, ChangeKind.Grid);
            if (next.Columns == state.Target.Columns || next.Rows == state.Target.Rows || next.PanelCount == state.Target.PanelCount || Near(next.AreaM2, state.Target.AreaM2))
                throw new InvalidOperationException("P05 grid rebuild did not change grid/count/area coherently.");
            state.Target = next;
            state.ReplacementCount++;
            state.Stage = SequenceStage.GridBuilt;
        });

        [CommandMethod("QS3DCURTAINSTALEMUTATEDEPTH", CommandFlags.Modal)]
        public void MutateDepth() => ExecuteStage("MUTATE_DEPTH", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.GridBuilt);
            MutateSemantic(project, state.TargetId, target => target.SetProperty("ThicknessM", "0.02"));
            VerifySemanticStale(document, project, state, expectedLiveGeometryStale: false);
            SelectTarget(document, project, state);
            state.StaleTransitionCount++;
            state.Stage = SequenceStage.DepthStale;
        });

        [CommandMethod("QS3DCURTAINSTALEVERIFYDEPTH", CommandFlags.Modal)]
        public void VerifyDepthRebuild() => ExecuteStage("VERIFY_DEPTH_REBUILD", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.DepthStale);
            var next = VerifyReplacement(document, project, state, ChangeKind.Depth);
            RequireNear(next.DepthM, 0.02d, "P05 rebuilt depth");
            if (next.PanelCount != state.Target.PanelCount || !Near(next.AreaM2, state.Target.AreaM2))
                throw new InvalidOperationException("P05 depth-only rebuild changed panel count/area.");
            state.Target = next;
            state.ReplacementCount++;
            state.Stage = SequenceStage.DepthBuilt;
        });

        [CommandMethod("QS3DCURTAINSTALEMUTATEHEIGHT", CommandFlags.Modal)]
        public void MutateHeight() => ExecuteStage("MUTATE_HEIGHT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.DepthBuilt);
            MutateSemantic(project, state.TargetId, target => target.SetProperty("HeightM", "4.2"));
            VerifySemanticStale(document, project, state, expectedLiveGeometryStale: false);
            SelectTarget(document, project, state);
            state.StaleTransitionCount++;
            state.Stage = SequenceStage.HeightStale;
        });

        [CommandMethod("QS3DCURTAINSTALEVERIFYHEIGHT", CommandFlags.Modal)]
        public void VerifyHeightRebuild() => ExecuteStage("VERIFY_HEIGHT_REBUILD", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.HeightStale);
            var next = VerifyReplacement(document, project, state, ChangeKind.Height);
            RequireNear(next.HeightM, 4.2d, "P05 rebuilt height");
            if (next.Rows == state.Target.Rows || next.PanelCount == state.Target.PanelCount || Near(next.AreaM2, state.Target.AreaM2))
                throw new InvalidOperationException("P05 height rebuild did not change rows/count/area coherently.");
            state.Target = next;
            state.ReplacementCount++;
            state.Stage = SequenceStage.HeightBuilt;
        });

        [CommandMethod("QS3DCURTAINSTALEMUTATEOPENING", CommandFlags.Modal)]
        public void MutateOpeningRelation() => ExecuteStage("MUTATE_OPENING_RELATION", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.HeightBuilt);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                new HostLinkService().LinkOpening(project, state.OpeningId, state.TargetId);
                project.Touch();
                VerifySemanticStale(document, project, state, expectedLiveGeometryStale: true);
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            SelectTarget(document, project, state);
            state.StaleTransitionCount++;
            state.Stage = SequenceStage.OpeningStale;
        });

        [CommandMethod("QS3DCURTAINSTALEVERIFYOPENING", CommandFlags.Modal)]
        public void VerifyOpeningRebuild() => ExecuteStage("VERIFY_OPENING_REBUILD", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.OpeningStale);
            var opening = RequiredElement(project, state.OpeningId, ElementCategory.Door);
            if (!opening.Properties.TryGetValue("HostWallId", out var hostId) || !string.Equals(hostId, state.TargetId, StringComparison.Ordinal))
                throw new InvalidOperationException("P05 opening relationship is not canonical after rebuild.");
            var next = VerifyReplacement(document, project, state, ChangeKind.Opening);
            if (next.OpeningCount != 1 || !(next.AreaM2 < state.Target.AreaM2))
                throw new InvalidOperationException("P05 opening rebuild did not reduce clear panel area.");
            state.Target = next;
            state.ReplacementCount++;
            state.Stage = SequenceStage.OpeningBuilt;
        });

        [CommandMethod("QS3DCURTAINSTALEMUTATESOURCE", CommandFlags.Modal)]
        public void MutateSourceGeometry() => ExecuteStage("MUTATE_SOURCE_GEOMETRY", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.OpeningBuilt);
            var target = RequiredElement(project, state.TargetId, ElementCategory.GlassWall);
            var sourceId = ResolveSingleSource(document, target);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(sourceId, OpenMode.ForWrite, false) as Line
                    ?? throw new InvalidOperationException("P05 target source is not a LINE.");
                RequireLineCoordinates(document, line, 0d, 0d, 5d, 0d, "P05 pre-edit target");
                var endX = CadGeometryGuard.ToDrawingUnits(document, 6d, "P05 source edit length");
                line.EndPoint = new Point3d(endX, line.EndPoint.Y, line.EndPoint.Z);
                transaction.Commit();
            }
            if (target.IsGeneratedCurtainPanelStale() || target.Properties.ContainsKey(ProjectElement.GeneratedCurtainPanelStateKey) || target.Properties.ContainsKey(ProjectElement.GeneratedCurtainPanelStaleSnapshotKey))
                throw new InvalidOperationException("Direct CAD edit must not fabricate semantic owner stale state before Source Sync.");
            RequireNear(RequiredDouble(target, "LengthM", false), 5d, "P05 pre-sync semantic length sample");
            RequireLiveIssue(document, project, target.Id, "CURTAIN_PANEL_LIVE_GEOMETRY_STALE");
            RequireLiveIssue(document, project, target.Id, "CURTAIN_PANEL_CONFIG_STALE");
            AssertControlUnchanged(document, project, state);
            document.Editor.SetImpliedSelection(new[] { sourceId });
            state.Stage = SequenceStage.SourceEdited;
        });

        [CommandMethod("QS3DCURTAINSTALEAFTERSYNC", CommandFlags.Modal)]
        public void VerifySourceSync() => ExecuteStage("VERIFY_SOURCE_SYNC", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.SourceEdited);
            var target = RequiredElement(project, state.TargetId, ElementCategory.GlassWall);
            RequireNear(RequiredDouble(target, "LengthM", false), 6d, "P05 post-sync semantic length");
            if (target.Properties.Keys.Any(x => x.StartsWith("GeneratedCurtainPanel", StringComparison.OrdinalIgnoreCase)) ||
                target.Properties.Keys.Any(x => x.StartsWith("GeneratedCurtainFrame", StringComparison.OrdinalIgnoreCase)) ||
                target.Properties.ContainsKey("GeneratedSolidHandle"))
                throw new InvalidOperationException("P05 Source Sync did not remove invalidated target native metadata.");
            if (CadHandleService.Resolve(document, state.Target.HostHandles.Concat(state.Target.FrameHandles).Concat(state.Target.PanelHandles)).Count != 0)
                throw new InvalidOperationException("P05 Source Sync left old target native output live.");
            AssertControlUnchanged(document, project, state);
            SelectTarget(document, project, state);
            state.Stage = SequenceStage.SourceSynced;
        });

        [CommandMethod("QS3DCURTAINSTALEPROBE", CommandFlags.Modal)]
        public void RunFinalProbe() => ExecuteStage("VERIFY_SOURCE_REBUILD", (document, project, nonce) =>
        {
            var resultPath = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable)!);
            if (File.Exists(resultPath)) throw new IOException("P05 result already exists.");
            var state = RequireState(nonce, SequenceStage.SourceSynced);
            var next = VerifyReplacement(document, project, state, ChangeKind.Source);
            RequireNear(next.SourceLengthM, 6d, "P05 final source length");
            if (next.Columns == state.Target.Columns || next.PanelCount == state.Target.PanelCount || !(next.AreaM2 > state.Target.AreaM2))
                throw new InvalidOperationException("P05 source rebuild did not change columns/count/area coherently.");
            if (state.StaleTransitionCount != 4 || state.ReplacementCount != 4)
                throw new InvalidOperationException("P05 transition/replacement sequence is incomplete.");
            state.Target = next;
            state.ReplacementCount++;
            state.Stage = SequenceStage.Complete;

            WriteMarkerAtomic(resultPath, new[]
            {
                "status=PASS",
                "command=QS3DCURTAINSTALEPROBE",
                "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + nonce,
                "schema=QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1",
                "qualification_boundary=LOCAL_002_P05_ONLY",
                "production_local002_qualified=false",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                "legacy_no_level=true",
                "semantic_owner_stale_transitions=4",
                "source_live_drift_transition_count=1",
                "target_replacement_count=5",
                "unrelated_owner_unchanged=true",
                "old_target_sets_removed=true",
                "target_handle_sets_disjoint=true",
                "native_plan_match_complete=true",
                "health_transitions_verified=true",
                "source_sync_invalidation_verified=true",
                "source_owner_sample_distinguished=true",
                "glass_wall_count=2",
                "linked_opening_count=1",
                "baseline_source_length_m=5",
                "final_source_length_m=6",
                "final_panel_count=" + next.PanelCount.ToString(CultureInfo.InvariantCulture),
                "final_panel_area_m2=" + next.AreaM2.ToString("R", CultureInfo.InvariantCulture),
                "final_native_match_count=" + next.PanelCount.ToString(CultureInfo.InvariantCulture),
                "health_issue_count=0",
                "located_panel_count=1",
                "canonical_owner_count=1"
            });
            document.Editor.WriteMessage("\nQS3D Curtain panel P05 stale/rebuild probe PASS.");
        });

        private static void ExecuteStage(string phase, Action<Document, ProjectState, string> action)
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireAutomation(requestedPath, nonce);
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                var project = ProjectContextCoordinator.TryGetReadOnly(document, out var existing)
                    ? existing
                    : ProjectContextCoordinator.GetOrCreate(document);
                action(document, project, nonce);
            }
            catch (System.Exception error)
            {
                TryWriteFailure(requestedPath, nonce, phase, FailureCode(error));
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Curtain panel P05 probe stage failed. See the sanitized local result.");
                throw;
            }
        }

        private static ObjectId AppendLine(
            Document document,
            Transaction transaction,
            BlockTableRecord modelSpace,
            double startX_M,
            double startY_M,
            double endX_M,
            double endY_M,
            string label)
        {
            var line = new Line(
                new Point3d(CadGeometryGuard.ToDrawingUnits(document, startX_M, label + " start X"), CadGeometryGuard.ToDrawingUnits(document, startY_M, label + " start Y"), 0d),
                new Point3d(CadGeometryGuard.ToDrawingUnits(document, endX_M, label + " end X"), CadGeometryGuard.ToDrawingUnits(document, endY_M, label + " end Y"), 0d));
            try
            {
                line.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                line = null!;
                return id;
            }
            finally { line?.Dispose(); }
        }

        private static SequenceState RequireState(string nonce, SequenceStage stage)
        {
            lock (StateSync)
            {
                if (State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) || State.Stage != stage)
                    throw new InvalidOperationException("P05 runtime command sequence is invalid.");
                return State;
            }
        }

        private static void MutateSemantic(ProjectState project, string targetId, Action<ProjectElement> mutation)
        {
            var target = RequiredElement(project, targetId, ElementCategory.GlassWall);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                mutation(target);
                project.Touch();
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
        }

        private static void VerifySemanticStale(
            Document document,
            ProjectState project,
            SequenceState state,
            bool expectedLiveGeometryStale)
        {
            var target = RequiredElement(project, state.TargetId, ElementCategory.GlassWall);
            if (!target.IsGeneratedCurtainPanelStale())
                throw new InvalidOperationException("P05 semantic mutation did not mark target panel output stale.");
            var expectedSignature = string.Join(";", state.Target.PanelHandles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            if (!target.Properties.TryGetValue(ProjectElement.GeneratedCurtainPanelStateKey, out var staleState) || staleState != "stale" ||
                !target.Properties.TryGetValue(ProjectElement.GeneratedCurtainPanelStaleSnapshotKey, out var staleSnapshot) ||
                !string.Equals(staleSnapshot, expectedSignature, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P05 semantic stale snapshot does not pin the exact old panel set.");

            var live = AllLivePanelHandles(document, project);
            var core = new GeneratedCurtainPanelHealthService().Inspect(project, live);
            RequireIssue(core, state.TargetId, "CURTAIN_PANEL_GENERATED_STALE");
            var liveIssues = CurtainWallPanelLiveStateService.Inspect(document, project);
            RequireIssue(liveIssues, state.TargetId, "CURTAIN_PANEL_CONFIG_STALE");
            if (expectedLiveGeometryStale) RequireIssue(liveIssues, state.TargetId, "CURTAIN_PANEL_LIVE_GEOMETRY_STALE");
            else if (HasIssue(liveIssues, state.TargetId, "CURTAIN_PANEL_LIVE_GEOMETRY_STALE"))
                throw new InvalidOperationException("P05 grid/depth mutation unexpectedly changed the live geometry fingerprint.");
            if (GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project).Any(x => string.Equals(x.ElementId, state.TargetId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("P05 semantic stale transition corrupted native ownership health.");
            AssertControlUnchanged(document, project, state);
        }

        private static OwnerSample VerifyReplacement(
            Document document,
            ProjectState project,
            SequenceState state,
            ChangeKind kind)
        {
            var target = RequiredElement(project, state.TargetId, ElementCategory.GlassWall);
            if (target.IsGeneratedCurtainPanelStale() || target.Properties.ContainsKey(ProjectElement.GeneratedCurtainPanelStateKey) || target.Properties.ContainsKey(ProjectElement.GeneratedCurtainPanelStaleSnapshotKey))
                throw new InvalidOperationException("P05 target panel stale state was not cleared by replacement.");
            var next = InspectOwner(document, project, target, kind == ChangeKind.Opening || kind == ChangeKind.Source ? 1 : 0);
            if (string.Equals(next.ConfigFingerprint, state.Target.ConfigFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("P05 replacement did not refresh its config fingerprint.");
            var liveFingerprintChanged = !string.Equals(next.LiveFingerprint, state.Target.LiveFingerprint, StringComparison.Ordinal);
            var expectedLiveFingerprintChange = kind == ChangeKind.Opening || kind == ChangeKind.Source;
            if (liveFingerprintChanged != expectedLiveFingerprintChange)
                throw new InvalidOperationException("P05 replacement live fingerprint transition does not match the change kind.");
            if (next.PanelHandles.Intersect(state.Target.PanelHandles, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P05 replacement reused an old target panel Handle.");
            if (CadHandleService.Resolve(document, state.Target.PanelHandles).Count != 0)
                throw new InvalidOperationException("P05 replacement left an old target panel live.");
            AssertControlUnchanged(document, project, state);
            RequireDisjoint(project, target, RequiredElement(project, state.ControlId, ElementCategory.GlassWall), RequiredElement(project, state.OpeningId, ElementCategory.Door));
            RequireLocate(document, project, target, next.PanelHandles[0]);
            state.NativeMatchCount = checked(state.NativeMatchCount + next.PanelCount);
            return next;
        }

        private static OwnerSample InspectOwner(Document document, ProjectState project, ProjectElement host, int expectedOpeningCount)
        {
            RequireLegacyNoLevel(host);
            if (host.IsGeneratedCurtainPanelStale()) throw new InvalidOperationException("P05 owner is stale during clean inspection.");
            OwnerSample sample;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var sourcePair = RequireSingleSourceLine(document, transaction, host);
                var line = sourcePair.Value;
                var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, "P05 owner dx");
                var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, "P05 owner dy");
                var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, "P05 owner length");
                var lengthM = CadGeometryGuard.ToMeters(document, lengthDrawing, "P05 owner length");
                var ux = dx / lengthDrawing;
                var uy = dy / lengthDrawing;
                var family = project.FindFamily(host.FamilyId);
                var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), "P05 owner height");
                var depthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.012d), "P05 owner depth");
                var bottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
                var placement = CadVerticalPlacementResolver.Resolve(document, project, host, line.StartPoint.Z, heightM, bottomOffsetM);
                var detail = CurtainWallDetailPlanner.Plan(LayoutInput(host, family, lengthM, placement.HeightM));
                var openings = CurtainWallPanelBuilderSupport.ReadLineOpenings(document, transaction, project, host, line, ux, uy, lengthM, heightM, bottomOffsetM, depthM);
                if (openings.Count != expectedOpeningCount)
                    throw new InvalidOperationException("P05 owner opening count is unexpected.");
                var plan = CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d);
                var expectedFingerprint = CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
                {
                    SourceLengthM = lengthM,
                    HeightM = placement.HeightM,
                    BottomOffsetM = bottomOffsetM,
                    PanelDepthM = depthM,
                    SourceKind = "Line",
                    PathSegmentCount = 0,
                    Pieces = plan.Pieces
                });

                var sourceHandles = CanonicalHandles(host.SourceHandles, "source");
                var hostHandles = CanonicalHandles(PropertyValues(host, "GeneratedSolidHandle"), "host");
                var frameHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainFrameHandles"), "frame");
                var panelHandles = CanonicalHandles(PropertyValues(host, "GeneratedCurtainPanelHandles"), "panel");
                if (sourceHandles.Count != 1 || hostHandles.Count != 1 || frameHandles.Count == 0 || panelHandles.Count == 0)
                    throw new InvalidOperationException("P05 owner output sets are incomplete.");
                if (CadHandleService.GetLiveSolidHandles(document, hostHandles).Count != hostHandles.Count ||
                    CadHandleService.GetLiveSolidHandles(document, frameHandles).Count != frameHandles.Count ||
                    CadHandleService.GetLiveSolidHandles(document, panelHandles).Count != panelHandles.Count)
                    throw new InvalidOperationException("P05 owner output contains a non-live Solid3d.");

                RequireExactProperty(host, "GeneratedCurtainPanelBuildState", "Complete");
                RequireExactProperty(host, "GeneratedCurtainPanelMode", expectedOpeningCount == 0 ? "LinePanelSolids" : "LinePanelSolids.OpeningAware");
                RequireExactInteger(host, "GeneratedCurtainPanelCount", plan.Pieces.Count);
                RequireExactInteger(host, "GeneratedCurtainPanelBaseCount", detail.Panels.Count);
                RequireExactInteger(host, "GeneratedCurtainPanelOpeningCount", expectedOpeningCount);
                RequireExactInteger(host, "GeneratedCurtainPanelColumns", detail.Layout.Columns);
                RequireExactInteger(host, "GeneratedCurtainPanelRows", detail.Layout.Rows);
                RequireNear(RequiredDouble(host, "GeneratedCurtainPanelDepthM", false), depthM, "P05 depth metadata");
                RequireNear(RequiredDouble(host, "GeneratedCurtainPanelSourceLengthM", false), lengthM, "P05 length metadata");
                RequireNear(RequiredDouble(host, "GeneratedCurtainPanelHeightM", false), placement.HeightM, "P05 height metadata");
                RequireNear(RequiredDouble(host, "GeneratedCurtainPanelAreaM2", true), plan.RemainingPanelAreaM2, "P05 area metadata");
                var config = RequiredFingerprint(host, "GeneratedCurtainPanelConfigFingerprint");
                var live = RequiredFingerprint(host, "GeneratedCurtainPanelLiveFingerprint");
                if (!string.Equals(config, expectedFingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("P05 config fingerprint differs from the authoritative plan.");
                var expectedLive = CurtainWallFrameLiveFingerprint.Compute(document, transaction, project, host, line);
                if (!string.Equals(live, expectedLive, StringComparison.Ordinal))
                    throw new InvalidOperationException("P05 live fingerprint differs from current source/opening geometry.");
                if (panelHandles.Count != plan.Pieces.Count)
                    throw new InvalidOperationException("P05 panel Handle count differs from the plan.");

                var native = ReadNativeBounds(document, transaction, panelHandles);
                MatchNativePieces(document, native, plan.Pieces, line, depthM, CadGeometryGuard.ToMeters(document, placement.BottomDrawingUnits, "P05 base Z"));
                RequireNoOpeningIntersection(plan.Pieces, openings);

                sample = new OwnerSample
                {
                    SourceHandles = sourceHandles,
                    HostHandles = hostHandles,
                    FrameHandles = frameHandles,
                    PanelHandles = panelHandles,
                    NativeBounds = native,
                    ConfigFingerprint = config,
                    LiveFingerprint = live,
                    Columns = detail.Layout.Columns,
                    Rows = detail.Layout.Rows,
                    BasePanelCount = detail.Panels.Count,
                    OpeningCount = openings.Count,
                    PanelCount = plan.Pieces.Count,
                    DepthM = depthM,
                    HeightM = placement.HeightM,
                    SourceLengthM = lengthM,
                    AreaM2 = plan.RemainingPanelAreaM2
                };
                transaction.Commit();
            }
            RequireCleanHealth(document, project, host.Id);
            return sample;
        }

        private static void AssertControlUnchanged(Document document, ProjectState project, SequenceState state)
        {
            var current = InspectOwner(document, project, RequiredElement(project, state.ControlId, ElementCategory.GlassWall), 0);
            var expected = state.Control;
            if (!current.SourceHandles.SequenceEqual(expected.SourceHandles, StringComparer.OrdinalIgnoreCase) ||
                !current.HostHandles.SequenceEqual(expected.HostHandles, StringComparer.OrdinalIgnoreCase) ||
                !current.FrameHandles.SequenceEqual(expected.FrameHandles, StringComparer.OrdinalIgnoreCase) ||
                !current.PanelHandles.SequenceEqual(expected.PanelHandles, StringComparer.OrdinalIgnoreCase) ||
                !string.Equals(current.ConfigFingerprint, expected.ConfigFingerprint, StringComparison.Ordinal) ||
                !string.Equals(current.LiveFingerprint, expected.LiveFingerprint, StringComparison.Ordinal) ||
                current.Columns != expected.Columns || current.Rows != expected.Rows || current.PanelCount != expected.PanelCount ||
                !Near(current.DepthM, expected.DepthM) || !Near(current.HeightM, expected.HeightM) ||
                !Near(current.SourceLengthM, expected.SourceLengthM) || !Near(current.AreaM2, expected.AreaM2) ||
                !BoundsEqual(current.NativeBounds, expected.NativeBounds))
                throw new InvalidOperationException("P05 unrelated control owner changed during target mutation/rebuild.");
        }

        private static void RequireCleanHealth(Document document, ProjectState project, string elementId)
        {
            var live = AllLivePanelHandles(document, project);
            var issues = new GeneratedCurtainPanelHealthService().Inspect(project, live)
                .Concat(CurtainWallPanelLiveStateService.Inspect(document, project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project))
                .Where(x => string.Equals(x.ElementId, elementId, StringComparison.OrdinalIgnoreCase) && x.Severity != HealthSeverity.Info)
                .ToList();
            if (issues.Count != 0) throw new InvalidOperationException("P05 owner panel Health is not clean.");
        }

        private static HashSet<string> AllLivePanelHandles(Document document, ProjectState project)
        {
            var handles = project.Elements
                .SelectMany(x => PropertyValues(x, "GeneratedCurtainPanelHandles"))
                .Select(x => CadHandleService.NormalizeHexHandle(x))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
            return new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, handles), StringComparer.OrdinalIgnoreCase);
        }

        private static void RequireLiveIssue(Document document, ProjectState project, string elementId, string code) =>
            RequireIssue(CurtainWallPanelLiveStateService.Inspect(document, project), elementId, code);

        private static void RequireIssue(IEnumerable<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!HasIssue(issues, elementId, code))
                throw new InvalidOperationException("P05 expected Health transition is missing.");
        }

        private static bool HasIssue(IEnumerable<ModelHealthIssue> issues, string elementId, string code) =>
            issues.Any(x => string.Equals(x.ElementId, elementId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Code, code, StringComparison.Ordinal));

        private static IReadOnlyList<NativeBounds> ReadNativeBounds(Document document, Transaction transaction, IReadOnlyList<string> handles)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count) throw new InvalidOperationException("P05 native panel resolution is incomplete.");
            var result = new List<NativeBounds>(ids.Count);
            foreach (var id in ids)
            {
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException("P05 generated panel is not a Solid3d.");
                var extents = solid.GeometricExtents;
                result.Add(new NativeBounds
                {
                    MinX_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.X, "P05 native min X"),
                    MaxX_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.X, "P05 native max X"),
                    MinY_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.Y, "P05 native min Y"),
                    MaxY_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Y, "P05 native max Y"),
                    MinZ_M = CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, "P05 native min Z"),
                    MaxZ_M = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, "P05 native max Z")
                });
            }
            return result.AsReadOnly();
        }

        private static void MatchNativePieces(
            Document document,
            IReadOnlyList<NativeBounds> native,
            IReadOnlyList<CurtainWallPanelPiece> planned,
            Line line,
            double depthM,
            double baseM)
        {
            if (native.Count != planned.Count) throw new InvalidOperationException("P05 native/planned counts differ.");
            var dx = line.EndPoint.X - line.StartPoint.X;
            var dy = line.EndPoint.Y - line.StartPoint.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            var ux = dx / length;
            var uy = dy / length;
            var lineStartX_M = CadGeometryGuard.ToMeters(document, line.StartPoint.X, "P05 line start X");
            var lineStartY_M = CadGeometryGuard.ToMeters(document, line.StartPoint.Y, "P05 line start Y");
            var used = new bool[planned.Count];
            foreach (var actual in native)
            {
                var matches = new List<int>();
                for (var index = 0; index < planned.Count; index++)
                {
                    if (used[index]) continue;
                    var panel = planned[index];
                    var centerStation = panel.X_M + panel.WidthM / 2d;
                    var centerX_M = lineStartX_M + ux * centerStation;
                    var centerY_M = lineStartY_M + uy * centerStation;
                    var angle = Math.Atan2(uy, ux);
                    var halfX = Math.Abs(Math.Cos(angle)) * panel.WidthM / 2d + Math.Abs(Math.Sin(angle)) * depthM / 2d;
                    var halfY = Math.Abs(Math.Sin(angle)) * panel.WidthM / 2d + Math.Abs(Math.Cos(angle)) * depthM / 2d;
                    if (Near(actual.MinX_M, centerX_M - halfX) && Near(actual.MaxX_M, centerX_M + halfX) &&
                        Near(actual.MinY_M, centerY_M - halfY) && Near(actual.MaxY_M, centerY_M + halfY) &&
                        Near(actual.MinZ_M, baseM + panel.Z_M) && Near(actual.MaxZ_M, baseM + panel.Z_M + panel.HeightM))
                        matches.Add(index);
                }
                if (matches.Count != 1) throw new InvalidOperationException("P05 native panel does not uniquely match one planned AABB.");
                used[matches[0]] = true;
            }
            if (used.Any(x => !x)) throw new InvalidOperationException("P05 planned piece has no native match.");
        }

        private static void RequireNoOpeningIntersection(IReadOnlyList<CurtainWallPanelPiece> pieces, IReadOnlyList<CurtainWallOpeningRect> openings)
        {
            foreach (var panel in pieces)
                foreach (var opening in openings)
                {
                    var overlapX = Math.Min(panel.X_M + panel.WidthM, opening.X_M + opening.WidthM) - Math.Max(panel.X_M, opening.X_M);
                    var overlapZ = Math.Min(panel.Z_M + panel.HeightM, opening.Z_M + opening.HeightM) - Math.Max(panel.Z_M, opening.Z_M);
                    if (overlapX > GeometryToleranceM && overlapZ > GeometryToleranceM)
                        throw new InvalidOperationException("P05 planned panel has positive-area opening intersection.");
                }
        }

        private static bool BoundsEqual(IReadOnlyList<NativeBounds> left, IReadOnlyList<NativeBounds> right)
        {
            if (left.Count != right.Count) return false;
            var used = new bool[right.Count];
            foreach (var item in left)
            {
                var match = -1;
                for (var index = 0; index < right.Count; index++)
                {
                    if (used[index]) continue;
                    var candidate = right[index];
                    if (Near(item.MinX_M, candidate.MinX_M) && Near(item.MaxX_M, candidate.MaxX_M) &&
                        Near(item.MinY_M, candidate.MinY_M) && Near(item.MaxY_M, candidate.MaxY_M) &&
                        Near(item.MinZ_M, candidate.MinZ_M) && Near(item.MaxZ_M, candidate.MaxZ_M))
                    {
                        if (match >= 0) return false;
                        match = index;
                    }
                }
                if (match < 0) return false;
                used[match] = true;
            }
            return true;
        }

        private static void RequireLocate(Document document, ProjectState project, ProjectElement owner, string panelHandle)
        {
            var ids = CadHandleService.Resolve(document, new[] { panelHandle });
            if (ids.Count != 1) throw new InvalidOperationException("P05 cannot resolve one panel for Locate.");
            document.Editor.SetImpliedSelection(ids.ToArray());
            var owners = SemanticSelectionResolver.ResolveImplied(document, project);
            if (owners.Count != 1 || !ReferenceEquals(owners[0], owner))
                throw new InvalidOperationException("P05 Locate did not resolve the canonical target owner.");
        }

        private static void RequireDisjoint(ProjectState project, ProjectElement target, ProjectElement control, ProjectElement opening)
        {
            var groups = new[]
            {
                CanonicalHandles(target.SourceHandles, "target source"),
                CanonicalHandles(control.SourceHandles, "control source"),
                CanonicalHandles(opening.SourceHandles, "opening source"),
                CanonicalHandles(PropertyValues(target, "GeneratedSolidHandle"), "target host"),
                CanonicalHandles(PropertyValues(target, "GeneratedCurtainFrameHandles"), "target frame"),
                CanonicalHandles(PropertyValues(target, "GeneratedCurtainPanelHandles"), "target panel"),
                CanonicalHandles(PropertyValues(control, "GeneratedSolidHandle"), "control host"),
                CanonicalHandles(PropertyValues(control, "GeneratedCurtainFrameHandles"), "control frame"),
                CanonicalHandles(PropertyValues(control, "GeneratedCurtainPanelHandles"), "control panel")
            };
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
                foreach (var handle in group)
                    if (!all.Add(handle)) throw new InvalidOperationException("P05 source/generated ownership sets overlap.");
        }

        private static void SelectTarget(Document document, ProjectState project, SequenceState state) =>
            document.Editor.SetImpliedSelection(new[] { ResolveSingleSource(document, RequiredElement(project, state.TargetId, ElementCategory.GlassWall)) });

        private static ObjectId ResolveSingleSource(Document document, ProjectElement element)
        {
            var handles = CanonicalHandles(element.SourceHandles, "source");
            var ids = CadHandleService.Resolve(document, handles);
            if (handles.Count != 1 || ids.Count != 1) throw new InvalidOperationException("P05 owner requires one live source.");
            return ids[0];
        }

        private static KeyValuePair<ObjectId, Line> RequireSingleSourceLine(Document document, Transaction transaction, ProjectElement element)
        {
            var id = ResolveSingleSource(document, element);
            var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                ?? throw new InvalidOperationException("P05 source is not a LINE.");
            return new KeyValuePair<ObjectId, Line>(id, line);
        }

        private static void RequireHorizontalLine(Document document, Line line, double lengthM)
        {
            var dx = line.EndPoint.X - line.StartPoint.X;
            var dy = line.EndPoint.Y - line.StartPoint.Y;
            var dz = line.EndPoint.Z - line.StartPoint.Z;
            RequireNear(CadGeometryGuard.ToMeters(document, Math.Sqrt(dx * dx + dy * dy), "P05 source length"), lengthM, "P05 source length");
            RequireNear(CadGeometryGuard.ToMeters(document, dz, "P05 source delta Z"), 0d, "P05 source delta Z");
        }

        private static void RequireLineCoordinates(Document document, Line line, double x1, double y1, double x2, double y2, string label)
        {
            RequireNear(CadGeometryGuard.ToMeters(document, line.StartPoint.X, label + " X1"), x1, label + " X1");
            RequireNear(CadGeometryGuard.ToMeters(document, line.StartPoint.Y, label + " Y1"), y1, label + " Y1");
            RequireNear(CadGeometryGuard.ToMeters(document, line.EndPoint.X, label + " X2"), x2, label + " X2");
            RequireNear(CadGeometryGuard.ToMeters(document, line.EndPoint.Y, label + " Y2"), y2, label + " Y2");
        }

        private static CurtainWallLayoutInput LayoutInput(ProjectElement element, ProjectFamily? family, double lengthM, double heightM) => new CurtainWallLayoutInput
        {
            LengthM = lengthM,
            HeightM = heightM,
            MaxPanelWidthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelWidthM", 1.2d), "P05 max panel width"),
            MaxPanelHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelHeightM", 1.5d), "P05 max panel height"),
            PerimeterFrameWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d), "P05 perimeter"),
            MullionWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainMullionWidthM", 0.05d), "P05 mullion"),
            TransomWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainTransomWidthM", 0.05d), "P05 transom")
        };

        private static ProjectElement RequiredElement(ProjectState project, string id, ElementCategory category)
        {
            var element = project.FindElement(id) ?? throw new InvalidOperationException("P05 semantic element is missing.");
            if (element.Category != category) throw new InvalidOperationException("P05 semantic category changed.");
            return element;
        }

        private static void RequireLegacyNoLevel(ProjectElement element)
        {
            if (CadVerticalPlacementResolver.HasConfiguredLevel(element))
                throw new InvalidOperationException("P05 probe requires legacy/no-Level placement.");
        }

        private static IReadOnlyList<string> PropertyValues(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.None);
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> values, string label)
        {
            var result = values.Select(x => CadHandleService.NormalizeHexHandle(x)
                    ?? throw new InvalidDataException("P05 " + label + " contains an invalid Handle token."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return result.AsReadOnly();
        }

        private static string RequiredFingerprint(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || raw == null || raw.Length != 64 ||
                raw.Any(x => !((x >= '0' && x <= '9') || (x >= 'a' && x <= 'f'))))
                throw new InvalidDataException("P05 fingerprint is missing or non-canonical.");
            return raw;
        }

        private static void RequireExactInteger(ProjectElement element, string key, int expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value != expected)
                throw new InvalidDataException("P05 integer metadata differs from the plan.");
        }

        private static void RequireExactProperty(ProjectElement element, string key, string expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !string.Equals(raw, expected, StringComparison.Ordinal))
                throw new InvalidDataException("P05 metadata token is missing or non-canonical.");
        }

        private static double RequiredDouble(ProjectElement element, string key, bool allowZero)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
                throw new InvalidDataException("P05 numeric metadata is missing or invalid.");
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " must be >= 0.");
            return value;
        }

        private static bool Near(double actual, double expected)
        {
            var scale = Math.Max(1d, Math.Max(Math.Abs(actual), Math.Abs(expected)));
            return Math.Abs(actual - expected) <= GeometryToleranceM * scale;
        }

        private static void RequireNear(double actual, double expected, string label)
        {
            if (!Near(actual, expected)) throw new InvalidOperationException(label + " differs from the expected synthetic/plan value.");
        }

        private static void RequireAutomation(string? requestedPath, string nonce)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("P05 runtime commands are automation-only.");
            RequiredResultPath(requestedPath!);
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P05 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("P05 result directory must already exist.");
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
                if (normalized.Length > 0 && !File.Exists(normalized) && Guid.TryParseExact(nonce, "N", out _) &&
                    FailurePhases.Contains(phase) && FailureCodes.Contains(failureCode))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DCURTAINSTALEPROBE",
                        "nonce=" + nonce,
                        "schema=QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1",
                        "qualification_boundary=LOCAL_002_P05_ONLY",
                        "production_local002_qualified=false",
                        "error_code=CURTAIN_PANEL_STALE_REBUILD_RUNTIME_FAILED",
                        "failure_phase=" + phase,
                        "failure_code=" + failureCode
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("P05 result already exists.");
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

        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
