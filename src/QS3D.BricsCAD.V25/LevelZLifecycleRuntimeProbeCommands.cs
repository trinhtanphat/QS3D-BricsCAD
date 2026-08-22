using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Additive LOCAL-003 lifecycle proof layered over the already-qualified Curtain P11
    /// production flow. It verifies that a Bottom+Top Level GlassWall keeps its exact
    /// vertical configuration and native output through Undo, Redo, save, cold reopen
    /// and ownership-scoped rebuild without introducing another persistence/Undo path.
    /// </summary>
    public sealed class LevelZLifecycleRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_LEVEL_Z_LIFECYCLE_RESULT";
        private const string PhaseResultVariable = "QS3D_LEVEL_Z_LIFECYCLE_PHASE_RESULT";
        private const string NonceVariable = "QS3D_LEVEL_Z_LIFECYCLE_NONCE";
        private const string SourceShaVariable = "QS3D_LEVEL_Z_LIFECYCLE_SOURCE_SHA";
        private const string ExpectedHostVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_HOSTS";
        private const string ExpectedFrameVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_FRAMES";
        private const string ExpectedPanelVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_PANELS";
        private const string ResultFileName = "level-z-lifecycle-result.txt";
        private const string PhaseResultFileName = "level-z-lifecycle-session1.txt";
        private const string Schema = "QS3D_LEVEL_Z_LIFECYCLE_RUNTIME_V1";
        private const string BottomLevelId = "level-z-lifecycle-bottom";
        private const string TopLevelId = "level-z-lifecycle-top";
        private const double LegacyBottomM = 0d;
        private const double LegacyTopM = 3.6d;
        private const double BoundedBottomM = 3.1d;
        private const double BoundedTopM = 6.8d;
        private static readonly object Sync = new object();
        private static SessionOneState? _sessionOne;
        private static SessionTwoState? _sessionTwo;

        [CommandMethod("QS3DLEVELZLIFECYCLECONFIGURE", CommandFlags.Modal)]
        public void Configure()
        {
            Execute("configure", () =>
            {
                var context = Context();
                var owner = RequireSingleOwner(context.Project);
                if (context.Project.FindFloor(BottomLevelId) != null || context.Project.FindFloor(TopLevelId) != null)
                    throw new InvalidOperationException("Level lifecycle floors already exist.");

                var beforeHosts = Canonical(PropertyValues(owner, "GeneratedSolidHandle"), "pre-build host");
                if (beforeHosts.Count != 1 || PropertyValues(owner, "GeneratedCurtainFrameHandles").Count != 0 ||
                    PropertyValues(owner, "GeneratedCurtainPanelHandles").Count != 0)
                    throw new InvalidOperationException("Level lifecycle configure requires one legacy host and no Curtain output.");
                RequireAllLive(context.Document, beforeHosts, "pre-build host");
                var beforeHostRange = ReadZRange(context.Document, beforeHosts, "pre-build host");
                RequireNear(LegacyBottomM, beforeHostRange.MinimumM, "pre-build host bottom");
                RequireNear(LegacyTopM, beforeHostRange.MaximumM, "pre-build host top");
                RequireSnapshot(owner, "GeneratedSolid", LegacyBottomM, LegacyTopM, "LegacySourceRelative");

                ProjectFloorService.Create(context.Project, BottomLevelId, "Lifecycle Bottom", 3d);
                ProjectFloorService.Create(context.Project, TopLevelId, "Lifecycle Top", 7d);
                Require(ProjectFloorService.AssignBottomLevel(context.Project, BottomLevelId, new[] { owner }) == 1,
                    "Bottom Level assignment");
                owner.SetProperty(ProjectFloorService.BottomLevelOffsetKey, Number(0.1d));
                Require(ProjectFloorService.AssignTopLevel(context.Project, TopLevelId, new[] { owner }) == 1,
                    "Top Level assignment");
                owner.SetProperty(ProjectFloorService.TopLevelOffsetKey, Number(-0.2d));
                var configSignature = RequireLevelConfiguration(context.Project, owner);
                Require(owner.IsGeneratedSolidStale(), "Level assignment must stale the legacy host");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(
                    context.Document,
                    context.Project,
                    "Level lifecycle configure");

                lock (Sync)
                {
                    _sessionOne = new SessionOneState(
                        context.Document,
                        context.Project.ProjectId,
                        owner.Id,
                        context.Nonce,
                        configSignature,
                        beforeHosts,
                        beforeHostRange);
                    _sessionTwo = null;
                }
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLEBASELINE", CommandFlags.Modal)]
        public void CaptureBaseline()
        {
            Execute("baseline", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var owner = RequireOwner(context.Project, state.OwnerId);
                var snapshot = CaptureBounded(context.Document, context.Project, owner);
                Require(string.Equals(snapshot.ConfigSignature, state.ConfigSignature, StringComparison.Ordinal),
                    "Level configuration changed before baseline");
                state.After = snapshot;
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLECHECKUNDO", CommandFlags.Modal)]
        public void CheckUndo()
        {
            Execute("native_undo", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var after = state.After ?? throw new InvalidOperationException("Level lifecycle baseline is missing.");
                var owner = RequireOwner(context.Project, state.OwnerId);
                var configSignature = RequireLevelConfiguration(context.Project, owner);
                var currentHosts = Canonical(PropertyValues(owner, "GeneratedSolidHandle"), "Undo host");
                Require(Same(state.BeforeHostHandles, currentHosts), "Undo must restore the pre-build host ownership");
                RequireAllLive(context.Document, currentHosts, "Undo host");
                var currentRange = ReadZRange(context.Document, currentHosts, "Undo host");
                RequireNear(state.BeforeHostRange.MinimumM, currentRange.MinimumM, "Undo host bottom");
                RequireNear(state.BeforeHostRange.MaximumM, currentRange.MaximumM, "Undo host top");
                RequireSnapshot(owner, "GeneratedSolid", LegacyBottomM, LegacyTopM, "LegacySourceRelative");
                Require(PropertyValues(owner, "GeneratedCurtainFrameHandles").Count == 0,
                    "Undo must restore empty Curtain frame ownership");
                Require(PropertyValues(owner, "GeneratedCurtainPanelHandles").Count == 0,
                    "Undo must restore empty Curtain panel ownership");
                Require(owner.IsGeneratedSolidStale(), "Undo-restored legacy host must remain stale for the Level configuration");
                state.UndoLevelConfigurationPreserved = string.Equals(configSignature, state.ConfigSignature, StringComparison.Ordinal);
                state.UndoPreBuildHostRestored = Same(state.BeforeHostHandles, currentHosts);
                state.UndoGeneratedAfterAbsent = AllAbsent(context.Document, GeneratedHandles(after));
                Require(state.UndoLevelConfigurationPreserved, "Undo Level configuration preservation");
                Require(state.UndoPreBuildHostRestored, "Undo pre-build host restoration");
                Require(state.UndoGeneratedAfterAbsent, "Undo generated-after removal");
                state.UndoChecked = true;
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLECHECKREDO", CommandFlags.Modal)]
        public void CheckRedo()
        {
            Execute("native_redo", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var expected = state.After ?? throw new InvalidOperationException("Level lifecycle Redo baseline is missing.");
                var owner = RequireOwner(context.Project, state.OwnerId);
                var current = CaptureBounded(context.Document, context.Project, owner);
                state.RedoLevelOutputCoherent = SameOutput(expected, current) &&
                                                string.Equals(current.ConfigSignature, state.ConfigSignature, StringComparison.Ordinal);
                Require(state.RedoLevelOutputCoherent, "Redo Level output coherence");
                state.RedoChecked = true;
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLESESSION1", CommandFlags.Modal)]
        public void CompleteSessionOne()
        {
            Execute("session1_publish", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var after = state.After ?? throw new InvalidOperationException("Level lifecycle session-one output is missing.");
                if (!state.UndoChecked || !state.RedoChecked)
                    throw new InvalidOperationException("Level lifecycle Undo/Redo checks are incomplete.");
                var path = RequiredPath(Environment.GetEnvironmentVariable(PhaseResultVariable), PhaseResultFileName);
                WriteMarkerAtomic(path, new[]
                {
                    "status=PASS",
                    "command=QS3DLEVELZLIFECYCLESESSION1",
                    "nonce=" + context.Nonce,
                    "source_sha=" + context.SourceSha,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_003_LEVEL_LIFECYCLE_ONLY",
                    "production_local003_qualified=false",
                    "native_drawing_unit=" + after.NativeUnit,
                    "undo_level_config_preserved=" + Boolean(state.UndoLevelConfigurationPreserved),
                    "undo_prebuild_host_restored=" + Boolean(state.UndoPreBuildHostRestored),
                    "undo_generated_after_absent=" + Boolean(state.UndoGeneratedAfterAbsent),
                    "redo_level_output_coherent=" + Boolean(state.RedoLevelOutputCoherent),
                    "host_solid_count=" + after.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "frame_solid_count=" + after.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_solid_count=" + after.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "bounded_host_bottom_m=" + Number(after.HostRange.MinimumM),
                    "bounded_host_top_m=" + Number(after.HostRange.MaximumM),
                    "level_health_issue_count=0"
                });
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLEREOPEN", CommandFlags.Modal)]
        public void CaptureReopened()
        {
            Execute("cold_reopen", () =>
            {
                var context = Context();
                var owner = RequireSingleOwner(context.Project);
                var reopened = CaptureBounded(context.Document, context.Project, owner);
                RequireExpectedCount(ExpectedHostVariable, reopened.HostHandles.Count);
                RequireExpectedCount(ExpectedFrameVariable, reopened.FrameHandles.Count);
                RequireExpectedCount(ExpectedPanelVariable, reopened.PanelHandles.Count);
                lock (Sync)
                {
                    _sessionTwo = new SessionTwoState(
                        context.Document,
                        context.Project.ProjectId,
                        owner.Id,
                        context.Nonce,
                        reopened);
                    _sessionOne = null;
                }
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLEAFTERREBUILD", CommandFlags.Modal)]
        public void CaptureAfterRebuild()
        {
            Execute("rebuild", () =>
            {
                var context = Context();
                var state = SessionTwo(context);
                var owner = RequireOwner(context.Project, state.OwnerId);
                var rebuilt = CaptureBounded(context.Document, context.Project, owner);
                var reopenedHandles = GeneratedHandles(state.Reopened);
                var rebuiltHandles = GeneratedHandles(rebuilt);
                state.OldGeneratedRemoved = AllAbsent(context.Document, reopenedHandles);
                state.NewGeneratedDisjoint = reopenedHandles.All(handle => !rebuiltHandles.Contains(handle));
                state.CountsStable = state.Reopened.HostHandles.Count == rebuilt.HostHandles.Count &&
                                     state.Reopened.FrameHandles.Count == rebuilt.FrameHandles.Count &&
                                     state.Reopened.PanelHandles.Count == rebuilt.PanelHandles.Count;
                state.RebuildLevelOutputCoherent = SameGeometry(state.Reopened, rebuilt) &&
                                                   string.Equals(state.Reopened.ConfigSignature, rebuilt.ConfigSignature, StringComparison.Ordinal);
                Require(state.OldGeneratedRemoved, "rebuild old generated removal");
                Require(state.NewGeneratedDisjoint, "rebuild generated identity replacement");
                Require(state.CountsStable, "rebuild count stability");
                Require(state.RebuildLevelOutputCoherent, "rebuild Level output coherence");
                state.Rebuilt = rebuilt;
            });
        }

        [CommandMethod("QS3DLEVELZLIFECYCLECOMPLETE", CommandFlags.Modal)]
        public void Complete()
        {
            Execute("final_publish", () =>
            {
                var context = Context();
                var state = SessionTwo(context);
                var rebuilt = state.Rebuilt ?? throw new InvalidOperationException("Level lifecycle rebuilt state is missing.");
                var pass = state.OldGeneratedRemoved && state.NewGeneratedDisjoint && state.CountsStable &&
                           state.RebuildLevelOutputCoherent && rebuilt.HealthIssueCount == 0;
                var path = RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
                var lines = new List<string>
                {
                    "status=" + (pass ? "PASS" : "FAIL"),
                    "command=QS3DLEVELZLIFECYCLECOMPLETE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + context.Nonce,
                    "source_sha=" + context.SourceSha,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_003_LEVEL_LIFECYCLE_ONLY",
                    "production_local003_qualified=false",
                    "is_64bit=" + Boolean(Environment.Is64BitProcess),
                    "native_drawing_unit=" + rebuilt.NativeUnit,
                    "reopen_level_config_coherent=true",
                    "reopen_level_output_coherent=true",
                    "rebuild_level_output_coherent=" + Boolean(state.RebuildLevelOutputCoherent),
                    "old_generated_removed=" + Boolean(state.OldGeneratedRemoved),
                    "new_generated_disjoint=" + Boolean(state.NewGeneratedDisjoint),
                    "rebuild_counts_stable=" + Boolean(state.CountsStable),
                    "reopened_host_count=" + state.Reopened.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "reopened_frame_count=" + state.Reopened.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "reopened_panel_count=" + state.Reopened.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_host_count=" + rebuilt.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_frame_count=" + rebuilt.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_panel_count=" + rebuilt.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "bounded_host_bottom_m=" + Number(rebuilt.HostRange.MinimumM),
                    "bounded_host_top_m=" + Number(rebuilt.HostRange.MaximumM),
                    "level_health_issue_count=0",
                    "level_lifecycle_qualified=" + Boolean(pass)
                };
                if (!pass)
                {
                    lines.Add("error_code=LEVEL_Z_LIFECYCLE_RUNTIME_FAILED");
                    lines.Add("failure_phase=rebuild");
                    lines.Add("failure_code=STATE_REJECTED");
                }
                WriteMarkerAtomic(path, lines);
            });
        }

        private static void Execute(string phase, Action action)
        {
            try { action(); }
            catch (Exception)
            {
                TryWriteFailure(phase, "STATE_REJECTED");
                throw;
            }
        }

        private static ProbeContext Context()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Level lifecycle probe is automation-only.");
            var sourceSha = (Environment.GetEnvironmentVariable(SourceShaVariable) ?? string.Empty).Trim().ToLowerInvariant();
            if (sourceSha.Length != 40 || sourceSha.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidOperationException("Level lifecycle source SHA is invalid.");
            RequireAssemblyRevision(typeof(LevelZLifecycleRuntimeProbeCommands).Assembly, sourceSha, "QS3D.BricsCAD.V25");
            RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha, "QS3D.Core");
            RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
            RequiredPath(Environment.GetEnvironmentVariable(PhaseResultVariable), PhaseResultFileName);
            if (!Environment.Is64BitProcess) throw new InvalidOperationException("Level lifecycle requires 64-bit BricsCAD.");
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!document.Name.EndsWith(".level-z-lifecycle-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Level lifecycle requires the guarded disposable drawing suffix.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Level lifecycle requires an existing project.");
            return new ProbeContext(document, project, nonce, sourceSha);
        }

        private static SessionOneState SessionOne(ProbeContext context)
        {
            lock (Sync)
            {
                var state = _sessionOne ?? throw new InvalidOperationException("Level lifecycle session one is not initialized.");
                state.Require(context);
                return state;
            }
        }

        private static SessionTwoState SessionTwo(ProbeContext context)
        {
            lock (Sync)
            {
                var state = _sessionTwo ?? throw new InvalidOperationException("Level lifecycle session two is not initialized.");
                state.Require(context);
                return state;
            }
        }

        private static ProjectElement RequireSingleOwner(ProjectState project)
        {
            var owners = project.Elements.Where(element => element.Category == ElementCategory.GlassWall).ToList();
            if (owners.Count != 1) throw new InvalidOperationException("Level lifecycle requires exactly one GlassWall owner.");
            return owners[0];
        }

        private static ProjectElement RequireOwner(ProjectState project, string ownerId)
        {
            var owner = project.FindElement(ownerId);
            if (owner == null || owner.Category != ElementCategory.GlassWall)
                throw new InvalidOperationException("Level lifecycle owner changed during the sequence.");
            return owner;
        }

        private static string RequireLevelConfiguration(ProjectState project, ProjectElement owner)
        {
            var bottom = project.FindFloor(BottomLevelId)
                ?? throw new InvalidOperationException("Level lifecycle Bottom Level is missing.");
            var top = project.FindFloor(TopLevelId)
                ?? throw new InvalidOperationException("Level lifecycle Top Level is missing.");
            RequireNear(3d, bottom.ElevationM, "Bottom Level elevation");
            RequireNear(7d, top.ElevationM, "Top Level elevation");
            Require(string.Equals(Property(owner, ProjectFloorService.BottomLevelIdKey), BottomLevelId, StringComparison.Ordinal),
                "Bottom Level identity");
            Require(string.Equals(Property(owner, ProjectFloorService.TopLevelIdKey), TopLevelId, StringComparison.Ordinal),
                "Top Level identity");
            RequireNear(0.1d, NumberProperty(owner, ProjectFloorService.BottomLevelOffsetKey), "Bottom Level offset");
            RequireNear(-0.2d, NumberProperty(owner, ProjectFloorService.TopLevelOffsetKey), "Top Level offset");
            var placement = ElementVerticalPlacementService.Resolve(project, owner, 0d, 3.6d, 0d);
            Require(placement.UsesBottomLevel && placement.UsesTopLevel, "Bottom+Top placement mode");
            RequireNear(BoundedBottomM, placement.BottomElevationM, "resolved Level bottom");
            RequireNear(BoundedTopM, placement.TopElevationM, "resolved Level top");
            return BottomLevelId + "|3|0.1|" + TopLevelId + "|7|-0.2";
        }

        private static LevelOutputSnapshot CaptureBounded(Document document, ProjectState project, ProjectElement owner)
        {
            var config = RequireLevelConfiguration(project, owner);
            var hosts = Canonical(PropertyValues(owner, "GeneratedSolidHandle"), "host");
            var frames = Canonical(PropertyValues(owner, "GeneratedCurtainFrameHandles"), "frame");
            var panels = Canonical(PropertyValues(owner, "GeneratedCurtainPanelHandles"), "panel");
            if (hosts.Count != 1 || frames.Count == 0 || panels.Count == 0)
                throw new InvalidOperationException("Level lifecycle generated output is incomplete.");
            RequireDisjoint(hosts, frames, panels);
            RequireAllLive(document, hosts, "host");
            RequireAllLive(document, frames, "frame");
            RequireAllLive(document, panels, "panel");
            var hostRange = ReadZRange(document, hosts, "host");
            var frameRange = ReadZRange(document, frames, "frame");
            var panelRange = ReadZRange(document, panels, "panel");
            RequireNear(BoundedBottomM, hostRange.MinimumM, "bounded host bottom");
            RequireNear(BoundedTopM, hostRange.MaximumM, "bounded host top");
            RequireContained(frameRange, hostRange, "Curtain frame Z");
            RequireContained(panelRange, hostRange, "Curtain panel Z");
            RequireSnapshot(owner, "GeneratedSolid", BoundedBottomM, BoundedTopM, "BottomTopLevels");
            RequireSnapshot(owner, "GeneratedCurtainFrame", BoundedBottomM, BoundedTopM, "BottomTopLevels");
            RequireSnapshot(owner, "GeneratedCurtainPanel", BoundedBottomM, BoundedTopM, "BottomTopLevels");
            Require(!owner.IsGeneratedSolidStale(), "bounded host freshness");
            Require(!owner.IsGeneratedCurtainFrameStale(), "Curtain frame freshness");
            Require(!owner.IsGeneratedCurtainPanelStale(), "Curtain panel freshness");
            var healthIssues = new LevelReferenceHealthService().Inspect(project)
                .Count(issue => issue.Severity != HealthSeverity.Info);
            Require(healthIssues == 0, "Level health");
            if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                throw new InvalidOperationException("Level lifecycle drawing unit is unsupported.");
            return new LevelOutputSnapshot(config, hosts, frames, panels, hostRange, frameRange, panelRange, healthIssues, nativeUnit.ToString());
        }

        private static bool SameOutput(LevelOutputSnapshot expected, LevelOutputSnapshot current)
        {
            return string.Equals(expected.ConfigSignature, current.ConfigSignature, StringComparison.Ordinal) &&
                   Same(expected.HostHandles, current.HostHandles) &&
                   Same(expected.FrameHandles, current.FrameHandles) &&
                   Same(expected.PanelHandles, current.PanelHandles) &&
                   SameGeometry(expected, current) &&
                   expected.HealthIssueCount == current.HealthIssueCount &&
                   string.Equals(expected.NativeUnit, current.NativeUnit, StringComparison.Ordinal);
        }

        private static bool SameGeometry(LevelOutputSnapshot expected, LevelOutputSnapshot current)
        {
            return Near(expected.HostRange.MinimumM, current.HostRange.MinimumM) &&
                   Near(expected.HostRange.MaximumM, current.HostRange.MaximumM) &&
                   Near(expected.FrameRange.MinimumM, current.FrameRange.MinimumM) &&
                   Near(expected.FrameRange.MaximumM, current.FrameRange.MaximumM) &&
                   Near(expected.PanelRange.MinimumM, current.PanelRange.MinimumM) &&
                   Near(expected.PanelRange.MaximumM, current.PanelRange.MaximumM);
        }

        private static IReadOnlyList<string> PropertyValues(ProjectElement owner, string key)
        {
            if (!owner.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static IReadOnlyList<string> Canonical(IEnumerable<string> values, string label)
        {
            var result = values.Select(value => CadHandleService.NormalizeHexHandle(value)
                    ?? throw new InvalidDataException("Level lifecycle " + label + " ownership is invalid."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return result.AsReadOnly();
        }

        private static void RequireAllLive(Document document, IReadOnlyList<string> handles, string label)
        {
            if (CadHandleService.GetLiveSolidHandles(document, handles).Count != handles.Count)
                throw new InvalidOperationException("Level lifecycle " + label + " ownership is not fully live.");
        }

        private static void RequireDisjoint(params IReadOnlyList<string>[] groups)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
                foreach (var handle in group)
                    if (!seen.Add(handle)) throw new InvalidOperationException("Level lifecycle generated ownership overlaps.");
        }

        private static HashSet<string> GeneratedHandles(LevelOutputSnapshot snapshot)
        {
            return new HashSet<string>(
                snapshot.HostHandles.Concat(snapshot.FrameHandles).Concat(snapshot.PanelHandles),
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool AllAbsent(Document document, IEnumerable<string> handles)
        {
            return CadHandleService.Resolve(document, handles.ToArray()).Count == 0;
        }

        private static bool Same(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return left.Count == right.Count && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
        }

        private static ZRange ReadZRange(Document document, IReadOnlyList<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count) throw new InvalidOperationException("Level lifecycle " + label + " count changed.");
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased)
                        throw new InvalidOperationException("Level lifecycle " + label + " is not a live Solid3d.");
                    var extents = solid.GeometricExtents;
                    minimum = Math.Min(minimum, CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, label + " minimum Z"));
                    maximum = Math.Max(maximum, CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, label + " maximum Z"));
                }
                transaction.Commit();
            }
            if (double.IsInfinity(minimum) || double.IsInfinity(maximum) || maximum <= minimum)
                throw new InvalidOperationException("Level lifecycle " + label + " range is invalid.");
            return new ZRange(minimum, maximum);
        }

        private static void RequireSnapshot(ProjectElement owner, string prefix, double bottomM, double topM, string mode)
        {
            RequireNear(bottomM, NumberProperty(owner, prefix + "VerticalBottomM"), prefix + " snapshot bottom");
            RequireNear(topM, NumberProperty(owner, prefix + "VerticalTopM"), prefix + " snapshot top");
            RequireNear(topM - bottomM, NumberProperty(owner, prefix + "VerticalHeightM"), prefix + " snapshot height");
            Require(string.Equals(Property(owner, prefix + "VerticalMode"), mode, StringComparison.Ordinal),
                prefix + " snapshot mode");
        }

        private static void RequireContained(ZRange inner, ZRange outer, string label)
        {
            const double toleranceM = 1e-6d;
            Require(inner.MinimumM >= outer.MinimumM - toleranceM, label + " minimum");
            Require(inner.MaximumM <= outer.MaximumM + toleranceM, label + " maximum");
        }

        private static string Property(ProjectElement owner, string key)
        {
            return owner.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
        }

        private static double NumberProperty(ProjectElement owner, string key)
        {
            var raw = Property(owner, key);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Level lifecycle numeric property is invalid: " + key + ".");
            return value;
        }

        private static void RequireExpectedCount(string variable, int actual)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var expected) || expected <= 0 || expected != actual)
                throw new InvalidOperationException("Level lifecycle expected count changed.");
        }

        private static void RequireAssemblyRevision(Assembly assembly, string sourceSha, string label)
        {
            var version = assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .Select(attribute => attribute.InformationalVersion ?? string.Empty)
                .FirstOrDefault() ?? string.Empty;
            if (!version.EndsWith("+" + sourceSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " assembly revision does not match the source SHA.");
        }

        private static string RequiredPath(string? value, string fileName)
        {
            var fullPath = Path.GetFullPath((value ?? string.Empty).Trim());
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), fileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException("Level lifecycle result path is invalid.");
            return fullPath;
        }

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var path = RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
                if (File.Exists(path)) return;
                WriteMarkerAtomic(path, new[]
                {
                    "status=FAIL",
                    "command=QS3DLEVELZLIFECYCLECOMPLETE",
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_003_LEVEL_LIFECYCLE_ONLY",
                    "production_local003_qualified=false",
                    "error_code=LEVEL_Z_LIFECYCLE_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code),
                    "level_lifecycle_qualified=false"
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath)) throw new IOException("Level lifecycle result already exists.");
            var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temporaryPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void RequireNear(double expected, double actual, string label)
        {
            if (!Near(expected, actual))
                throw new InvalidOperationException("Level lifecycle " + label + " did not match.");
        }

        private static bool Near(double expected, double actual)
        {
            var tolerance = Math.Max(1e-7d, Math.Max(Math.Abs(expected), Math.Abs(actual)) * 1e-7d);
            return Math.Abs(expected - actual) <= tolerance;
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Level lifecycle assertion failed: " + label + ".");
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Boolean(bool value) => value ? "true" : "false";
        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce, string sourceSha)
            {
                Document = document;
                Project = project;
                Nonce = nonce;
                SourceSha = sourceSha;
            }

            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
            public string SourceSha { get; }
        }

        private abstract class SessionState
        {
            protected SessionState(Document document, string projectId, string ownerId, string nonce)
            {
                Document = document;
                ProjectId = projectId;
                OwnerId = ownerId;
                Nonce = nonce;
            }

            protected Document Document { get; }
            protected string ProjectId { get; }
            public string OwnerId { get; }
            protected string Nonce { get; }

            public void Require(ProbeContext context)
            {
                if (!ReferenceEquals(Document, context.Document) ||
                    !string.Equals(ProjectId, context.Project.ProjectId, StringComparison.Ordinal) ||
                    !string.Equals(Nonce, context.Nonce, StringComparison.Ordinal))
                    throw new InvalidOperationException("Level lifecycle session affinity changed.");
            }
        }

        private sealed class SessionOneState : SessionState
        {
            public SessionOneState(
                Document document,
                string projectId,
                string ownerId,
                string nonce,
                string configSignature,
                IReadOnlyList<string> beforeHostHandles,
                ZRange beforeHostRange)
                : base(document, projectId, ownerId, nonce)
            {
                ConfigSignature = configSignature;
                BeforeHostHandles = beforeHostHandles;
                BeforeHostRange = beforeHostRange;
            }

            public string ConfigSignature { get; }
            public IReadOnlyList<string> BeforeHostHandles { get; }
            public ZRange BeforeHostRange { get; }
            public LevelOutputSnapshot? After { get; set; }
            public bool UndoChecked { get; set; }
            public bool UndoLevelConfigurationPreserved { get; set; }
            public bool UndoPreBuildHostRestored { get; set; }
            public bool UndoGeneratedAfterAbsent { get; set; }
            public bool RedoChecked { get; set; }
            public bool RedoLevelOutputCoherent { get; set; }
        }

        private sealed class SessionTwoState : SessionState
        {
            public SessionTwoState(Document document, string projectId, string ownerId, string nonce, LevelOutputSnapshot reopened)
                : base(document, projectId, ownerId, nonce)
            {
                Reopened = reopened;
            }

            public LevelOutputSnapshot Reopened { get; }
            public LevelOutputSnapshot? Rebuilt { get; set; }
            public bool OldGeneratedRemoved { get; set; }
            public bool NewGeneratedDisjoint { get; set; }
            public bool CountsStable { get; set; }
            public bool RebuildLevelOutputCoherent { get; set; }
        }

        private sealed class LevelOutputSnapshot
        {
            public LevelOutputSnapshot(
                string configSignature,
                IReadOnlyList<string> hostHandles,
                IReadOnlyList<string> frameHandles,
                IReadOnlyList<string> panelHandles,
                ZRange hostRange,
                ZRange frameRange,
                ZRange panelRange,
                int healthIssueCount,
                string nativeUnit)
            {
                ConfigSignature = configSignature;
                HostHandles = hostHandles;
                FrameHandles = frameHandles;
                PanelHandles = panelHandles;
                HostRange = hostRange;
                FrameRange = frameRange;
                PanelRange = panelRange;
                HealthIssueCount = healthIssueCount;
                NativeUnit = nativeUnit;
            }

            public string ConfigSignature { get; }
            public IReadOnlyList<string> HostHandles { get; }
            public IReadOnlyList<string> FrameHandles { get; }
            public IReadOnlyList<string> PanelHandles { get; }
            public ZRange HostRange { get; }
            public ZRange FrameRange { get; }
            public ZRange PanelRange { get; }
            public int HealthIssueCount { get; }
            public string NativeUnit { get; }
        }

        private sealed class ZRange
        {
            public ZRange(double minimumM, double maximumM)
            {
                MinimumM = minimumM;
                MaximumM = maximumM;
            }

            public double MinimumM { get; }
            public double MaximumM { get; }
        }
    }
}
