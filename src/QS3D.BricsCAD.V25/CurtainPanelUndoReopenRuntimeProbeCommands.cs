using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P11 probe. It intentionally stores native handles only
    /// in process memory; the persisted phase/final markers contain aggregate counts and
    /// booleans only. The caller must restore the disposable drawing and remove its QSDB
    /// sidecar after the two isolated BricsCAD processes finish.
    /// </summary>
    public sealed class CurtainPanelUndoReopenRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_P11_RESULT";
        private const string PhaseResultVariable = "QS3D_CURTAIN_P11_PHASE_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_P11_NONCE";
        private const string ExpectedHostVariable = "QS3D_CURTAIN_P11_EXPECTED_HOSTS";
        private const string ExpectedFrameVariable = "QS3D_CURTAIN_P11_EXPECTED_FRAMES";
        private const string ExpectedPanelVariable = "QS3D_CURTAIN_P11_EXPECTED_PANELS";
        private const string UndoCoherentVariable = "QS3D_CURTAIN_P11_UNDO_COHERENT";
        private const string RedoCoherentVariable = "QS3D_CURTAIN_P11_REDO_COHERENT";
        private const string ResultFileName = "curtain-panel-undo-reopen-result.txt";
        private const string PhaseResultFileName = "curtain-panel-undo-reopen-session1.txt";
        private const string SentinelRegApp = "QS3D_CURTAIN_P11_SENTINEL";
        private const string SentinelVersion = "1";
        private const string Schema = "QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1";
        private static readonly object Sync = new object();
        private static SessionOneState? _sessionOne;
        private static SessionTwoState? _sessionTwo;

        [CommandMethod("QS3DCURTAINP11PREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("prepare", () =>
            {
                var context = Context();
                var owner = RequireSingleOwner(context.Project);
                var sentinel = CreateSentinel(context.Document, context.Nonce);
                var before = Capture(context.Document, context.Project, owner, sentinel, false, true);
                lock (Sync)
                {
                    _sessionOne = new SessionOneState(context.Document, context.Project.ProjectId, owner.Id, context.Nonce, before);
                    _sessionTwo = null;
                }
                SelectSingleSource(context.Document, owner);
            });
        }

        [CommandMethod("QS3DCURTAINP11SELECT", CommandFlags.Modal)]
        public void SelectSource()
        {
            Execute("select_source", () =>
            {
                var context = Context();
                ProjectElement owner;
                lock (Sync)
                {
                    if (_sessionOne != null)
                    {
                        _sessionOne.Require(context);
                        owner = RequireOwner(context.Project, _sessionOne.OwnerId);
                    }
                    else if (_sessionTwo != null)
                    {
                        _sessionTwo.Require(context);
                        owner = RequireOwner(context.Project, _sessionTwo.OwnerId);
                    }
                    else
                    {
                        throw new InvalidOperationException("Curtain P11 sequence is not initialized.");
                    }
                }
                SelectSingleSource(context.Document, owner);
            });
        }

        [CommandMethod("QS3DCURTAINP11BASELINE", CommandFlags.Modal)]
        public void CaptureBaseline()
        {
            Execute("baseline", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var owner = RequireOwner(context.Project, state.OwnerId);
                var after = Capture(context.Document, context.Project, owner, state.Before.SentinelHandle, true, true);
                RequireHealthy(after, "baseline");
                if (after.HostHandles.Count == 0 || after.FrameHandles.Count == 0 || after.PanelHandles.Count == 0)
                    throw new InvalidOperationException("Curtain P11 baseline output is incomplete.");
                state.After = after;
            });
        }

        [CommandMethod("QS3DCURTAINP11CHECKUNDO", CommandFlags.Modal)]
        public void CheckUndo()
        {
            Execute("native_undo", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var after = state.After ?? throw new InvalidOperationException("Curtain P11 baseline was not captured.");
                var owner = RequireOwner(context.Project, state.OwnerId);
                var current = Capture(context.Document, context.Project, owner, state.Before.SentinelHandle, false, false);
                state.UndoCoherent = SameSemanticAndNative(state.Before, current) &&
                                     AllPresent(context.Document, GeneratedHandles(state.Before)) &&
                                     AllAbsent(context.Document, GeneratedHandles(after)) &&
                                     SameSourceAndSentinel(state.Before, current);
                state.UndoChecked = true;
            });
        }

        [CommandMethod("QS3DCURTAINP11CHECKREDO", CommandFlags.Modal)]
        public void CheckRedo()
        {
            Execute("native_redo", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var after = state.After ?? throw new InvalidOperationException("Curtain P11 baseline was not captured.");
                var owner = RequireOwner(context.Project, state.OwnerId);
                var current = Capture(context.Document, context.Project, owner, state.Before.SentinelHandle, true, true);
                state.RedoCoherent = SameSemanticAndNative(after, current) &&
                                     SameSourceAndSentinel(state.Before, current) &&
                                     current.HealthIssueCount == 0;
                state.RedoChecked = true;
            });
        }

        [CommandMethod("QS3DCURTAINP11SESSION1", CommandFlags.Modal)]
        public void CompleteSessionOne()
        {
            Execute("session1_publish", () =>
            {
                var context = Context();
                var state = SessionOne(context);
                var after = state.After ?? throw new InvalidOperationException("Curtain P11 baseline was not captured.");
                if (!state.UndoChecked || !state.RedoChecked)
                    throw new InvalidOperationException("Curtain P11 Undo/Redo checks are incomplete.");
                var phasePath = RequiredPath(
                    Environment.GetEnvironmentVariable(PhaseResultVariable),
                    PhaseResultFileName);
                WriteMarkerAtomic(phasePath, new[]
                {
                    "status=PASS",
                    "command=QS3DCURTAINP11SESSION1",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_002_P11_ONLY",
                    "undo_coherent=" + Boolean(state.UndoCoherent),
                    "redo_coherent=" + Boolean(state.RedoCoherent),
                    "host_solid_count=" + after.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "frame_solid_count=" + after.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_solid_count=" + after.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "change_version=" + after.ChangeVersion.ToString(CultureInfo.InvariantCulture),
                    "health_issue_count=0",
                    "source_preserved=true",
                    "sentinel_preserved=true"
                });
            });
        }

        [CommandMethod("QS3DCURTAINP11REOPEN", CommandFlags.Modal)]
        public void CaptureReopenedState()
        {
            Execute("cold_reopen", () =>
            {
                var context = Context();
                var owner = RequireSingleOwner(context.Project);
                var sentinel = RequireSingleSentinel(context.Document, context.Nonce);
                var reopened = Capture(context.Document, context.Project, owner, sentinel, true, true);
                RequireHealthy(reopened, "cold reopen");
                RequireExpectedCount(ExpectedHostVariable, reopened.HostHandles.Count);
                RequireExpectedCount(ExpectedFrameVariable, reopened.FrameHandles.Count);
                RequireExpectedCount(ExpectedPanelVariable, reopened.PanelHandles.Count);
                lock (Sync)
                {
                    _sessionTwo = new SessionTwoState(context.Document, context.Project.ProjectId, owner.Id, context.Nonce, reopened);
                    _sessionOne = null;
                }
                SelectSingleSource(context.Document, owner);
            });
        }

        [CommandMethod("QS3DCURTAINP11AFTERREBUILD", CommandFlags.Modal)]
        public void CaptureRebuiltState()
        {
            Execute("rebuild", () =>
            {
                var context = Context();
                var state = SessionTwo(context);
                var owner = RequireOwner(context.Project, state.OwnerId);
                var rebuilt = Capture(context.Document, context.Project, owner, state.Reopened.SentinelHandle, true, true);
                RequireHealthy(rebuilt, "rebuild");
                var previous = GeneratedHandles(state.Reopened);
                var current = GeneratedHandles(rebuilt);
                state.OldGeneratedRemoved = AllAbsent(context.Document, previous);
                state.NewGeneratedDisjoint = previous.All(handle => !current.Contains(handle));
                state.CountsStable = rebuilt.HostHandles.Count == state.Reopened.HostHandles.Count &&
                                     rebuilt.FrameHandles.Count == state.Reopened.FrameHandles.Count &&
                                     rebuilt.PanelHandles.Count == state.Reopened.PanelHandles.Count;
                state.SourcePreserved = string.Equals(state.Reopened.SourceSignature, rebuilt.SourceSignature, StringComparison.Ordinal);
                state.SentinelPreserved = string.Equals(state.Reopened.SentinelSignature, rebuilt.SentinelSignature, StringComparison.Ordinal);
                state.Rebuilt = rebuilt;
            });
        }

        [CommandMethod("QS3DCURTAINP11COMPLETE", CommandFlags.Modal)]
        public void Complete()
        {
            Execute("final_publish", () =>
            {
                var context = Context();
                var state = SessionTwo(context);
                var rebuilt = state.Rebuilt ?? throw new InvalidOperationException("Curtain P11 rebuild was not captured.");
                var undoCoherent = RequiredBooleanEnvironment(UndoCoherentVariable);
                var redoCoherent = RequiredBooleanEnvironment(RedoCoherentVariable);
                var pass = undoCoherent && redoCoherent && state.OldGeneratedRemoved && state.NewGeneratedDisjoint &&
                           state.CountsStable && state.SourcePreserved && state.SentinelPreserved && rebuilt.HealthIssueCount == 0;
                var resultPath = RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
                var lines = new List<string>
                {
                    "status=" + (pass ? "PASS" : "FAIL"),
                    "command=QS3DCURTAINP11COMPLETE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_002_P11_ONLY",
                    "production_local002_qualified=false",
                    "is_64bit=" + Boolean(Environment.Is64BitProcess),
                    "undo_coherent=" + Boolean(undoCoherent),
                    "redo_coherent=" + Boolean(redoCoherent),
                    "reopen_coherent=true",
                    "rebuild_coherent=true",
                    "source_preserved=" + Boolean(state.SourcePreserved),
                    "sentinel_preserved=" + Boolean(state.SentinelPreserved),
                    "old_generated_removed=" + Boolean(state.OldGeneratedRemoved),
                    "new_generated_disjoint=" + Boolean(state.NewGeneratedDisjoint),
                    "rebuild_counts_stable=" + Boolean(state.CountsStable),
                    "reopened_host_count=" + state.Reopened.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "reopened_frame_count=" + state.Reopened.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "reopened_panel_count=" + state.Reopened.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_host_count=" + rebuilt.HostHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_frame_count=" + rebuilt.FrameHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_panel_count=" + rebuilt.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "reopened_change_version=" + state.Reopened.ChangeVersion.ToString(CultureInfo.InvariantCulture),
                    "rebuilt_change_version=" + rebuilt.ChangeVersion.ToString(CultureInfo.InvariantCulture),
                    "health_issue_count=0",
                    "p11_qualified=" + Boolean(pass)
                };
                if (!pass)
                {
                    lines.Add("error_code=CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_FAILED");
                    lines.Add("failure_phase=" + (!undoCoherent ? "native_undo" : "rebuild"));
                    lines.Add("failure_code=" + (!undoCoherent ? "SEMANTIC_NATIVE_DIVERGENCE" : "STATE_REJECTED"));
                }
                WriteMarkerAtomic(resultPath, lines);
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
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain P11 probe is automation-only.");
            RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
            RequiredPath(Environment.GetEnvironmentVariable(PhaseResultVariable), PhaseResultFileName);
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Curtain P11 probe requires an existing project.");
            return new ProbeContext(document, project, nonce);
        }

        private static SessionOneState SessionOne(ProbeContext context)
        {
            lock (Sync)
            {
                var state = _sessionOne ?? throw new InvalidOperationException("Curtain P11 session one is not initialized.");
                state.Require(context);
                return state;
            }
        }

        private static SessionTwoState SessionTwo(ProbeContext context)
        {
            lock (Sync)
            {
                var state = _sessionTwo ?? throw new InvalidOperationException("Curtain P11 session two is not initialized.");
                state.Require(context);
                return state;
            }
        }

        private static ProjectElement RequireSingleOwner(ProjectState project)
        {
            var owners = project.Elements.Where(element => element.Category == ElementCategory.GlassWall).ToList();
            if (owners.Count != 1) throw new InvalidOperationException("Curtain P11 requires exactly one GlassWall owner.");
            return owners[0];
        }

        private static ProjectElement RequireOwner(ProjectState project, string ownerId)
        {
            var owner = project.FindElement(ownerId);
            if (owner == null || owner.Category != ElementCategory.GlassWall)
                throw new InvalidOperationException("Curtain P11 owner changed during the sequence.");
            return owner;
        }

        private static void SelectSingleSource(Document document, ProjectElement owner)
        {
            var sources = Canonical(owner.SourceHandles, "source");
            if (sources.Count != 1) throw new InvalidOperationException("Curtain P11 requires one canonical source.");
            var ids = CadHandleService.Resolve(document, sources);
            if (ids.Count != 1) throw new InvalidOperationException("Curtain P11 source is not live.");
            document.Editor.SetImpliedSelection(ids.ToArray());
        }

        private static Snapshot Capture(
            Document document,
            ProjectState project,
            ProjectElement owner,
            string sentinelHandle,
            bool inspectHealth,
            bool requireGeneratedLive)
        {
            var source = Canonical(owner.SourceHandles, "source");
            var hosts = Canonical(PropertyValues(owner, "GeneratedSolidHandle"), "host");
            var frames = Canonical(PropertyValues(owner, "GeneratedCurtainFrameHandles"), "frame");
            var panels = Canonical(PropertyValues(owner, "GeneratedCurtainPanelHandles"), "panel");
            RequireDisjoint(source, hosts, frames, panels);
            RequireAllLive(document, source, false, "source");
            if (requireGeneratedLive)
            {
                RequireAllLive(document, hosts, true, "host");
                RequireAllLive(document, frames, true, "frame");
                RequireAllLive(document, panels, true, "panel");
            }
            var sentinel = CadHandleService.NormalizeHexHandle(sentinelHandle)
                ?? throw new InvalidOperationException("Curtain P11 sentinel handle is invalid.");
            var sentinelSignature = RequireSentinelSignature(document, sentinel);
            var healthIssues = inspectHealth ? InspectHealth(document, project, panels, frames) : 0;
            return new Snapshot(
                SemanticSignature(project, owner),
                source,
                hosts,
                frames,
                panels,
                SourceSignature(document, source),
                sentinel,
                sentinelSignature,
                healthIssues,
                project.ChangeVersion,
                project.UpdatedUtc);
        }

        private static int InspectHealth(
            Document document,
            ProjectState project,
            IReadOnlyList<string> panels,
            IReadOnlyList<string> frames)
        {
            var issues = new List<ModelHealthIssue>();
            var liveFrames = new HashSet<string>(frames, StringComparer.OrdinalIgnoreCase);
            var livePanels = new HashSet<string>(panels, StringComparer.OrdinalIgnoreCase);
            issues.AddRange(new GeneratedCurtainFrameHealthService().Inspect(project, liveFrames));
            issues.AddRange(CurtainWallFrameLiveStateService.Inspect(document, project));
            issues.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, livePanels));
            issues.AddRange(CurtainWallPanelLiveStateService.Inspect(document, project));
            issues.AddRange(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project));
            return issues.Count(issue => issue.Severity != HealthSeverity.Info);
        }

        private static void RequireHealthy(Snapshot snapshot, string phase)
        {
            if (snapshot.HealthIssueCount != 0)
                throw new InvalidOperationException("Curtain P11 " + phase + " health is not clean.");
        }

        private static bool SameSemanticAndNative(Snapshot expected, Snapshot current)
        {
            return string.Equals(expected.SemanticSignature, current.SemanticSignature, StringComparison.Ordinal) &&
                   Same(expected.SourceHandles, current.SourceHandles) &&
                   Same(expected.HostHandles, current.HostHandles) &&
                   Same(expected.FrameHandles, current.FrameHandles) &&
                   Same(expected.PanelHandles, current.PanelHandles);
        }

        private static bool SameSourceAndSentinel(Snapshot expected, Snapshot current)
        {
            return string.Equals(expected.SourceSignature, current.SourceSignature, StringComparison.Ordinal) &&
                   string.Equals(expected.SentinelHandle, current.SentinelHandle, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(expected.SentinelSignature, current.SentinelSignature, StringComparison.Ordinal);
        }

        private static HashSet<string> GeneratedHandles(Snapshot snapshot)
        {
            return new HashSet<string>(snapshot.HostHandles.Concat(snapshot.FrameHandles).Concat(snapshot.PanelHandles), StringComparer.OrdinalIgnoreCase);
        }

        private static bool AllAbsent(Document document, IEnumerable<string> handles)
        {
            return CadHandleService.Resolve(document, handles.ToArray()).Count == 0;
        }

        private static bool AllPresent(Document document, IReadOnlyCollection<string> handles)
        {
            return CadHandleService.GetLiveSolidHandles(document, handles).Count == handles.Count;
        }

        private static bool Same(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return left.Count == right.Count && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
        }

        private static string SemanticSignature(ProjectState project, ProjectElement owner)
        {
            var builder = new StringBuilder();
            Append(builder, project.ChangeVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            Append(builder, owner.Category.ToString());
            Append(builder, owner.FamilyId);
            Append(builder, owner.FloorId);
            Append(builder, owner.ZoneId);
            Append(builder, owner.Dirty.ToString());
            foreach (var source in owner.SourceHandles) Append(builder, source);
            foreach (var property in owner.Properties
                         .Where(pair => string.Equals(pair.Key, "GeneratedSolidHandle", StringComparison.OrdinalIgnoreCase) ||
                                        pair.Key.StartsWith("GeneratedCurtain", StringComparison.OrdinalIgnoreCase) ||
                                        pair.Key.StartsWith("QS3D.GeneratedCurtain", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Append(builder, property.Key);
                Append(builder, property.Value);
            }
            return Sha256(builder.ToString());
        }

        private static string SourceSignature(Document document, IReadOnlyList<string> handles)
        {
            var builder = new StringBuilder();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var handle in handles)
                {
                    var ids = CadHandleService.Resolve(document, new[] { handle });
                    if (ids.Count != 1) throw new InvalidOperationException("Curtain P11 source disappeared.");
                    var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity
                        ?? throw new InvalidOperationException("Curtain P11 source is not an entity.");
                    Append(builder, entity.GetType().Name);
                    AppendBounds(builder, entity.GeometricExtents);
                    if (entity is Line line)
                    {
                        AppendPoint(builder, line.StartPoint);
                        AppendPoint(builder, line.EndPoint);
                    }
                }
            }
            return Sha256(builder.ToString());
        }

        private static string CreateSentinel(Document document, string nonce)
        {
            if (FindSentinels(document, nonce).Count != 0)
                throw new InvalidOperationException("Curtain P11 sentinel already exists.");
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                EnsureRegApp(document.Database, transaction, SentinelRegApp);
                var solid = new Solid3d();
                solid.CreateBox(250d, 250d, 250d);
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(10000d, 10000d, 1000d)));
                using (var marker = new ResultBuffer(
                           new TypedValue((int)DxfCode.ExtendedDataRegAppName, SentinelRegApp),
                           new TypedValue((int)DxfCode.ExtendedDataAsciiString, SentinelVersion),
                           new TypedValue((int)DxfCode.ExtendedDataAsciiString, nonce)))
                    solid.XData = marker;
                modelSpace.AppendEntity(solid);
                transaction.AddNewlyCreatedDBObject(solid, true);
                var handle = solid.Handle.ToString();
                transaction.Commit();
                return CadHandleService.NormalizeHexHandle(handle)
                    ?? throw new InvalidOperationException("Curtain P11 sentinel Handle is invalid.");
            }
        }

        private static string RequireSingleSentinel(Document document, string nonce)
        {
            var sentinels = FindSentinels(document, nonce);
            if (sentinels.Count != 1) throw new InvalidOperationException("Curtain P11 sentinel count is not one.");
            return sentinels[0];
        }

        private static IReadOnlyList<string> FindSentinels(Document document, string nonce)
        {
            var result = new List<string>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = (BlockTableRecord)transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead);
                foreach (ObjectId id in space)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (!(entity is Solid3d) || entity.IsErased) continue;
                    using (var marker = entity.GetXDataForApplication(SentinelRegApp))
                    {
                        if (marker == null) continue;
                        var values = marker.AsArray();
                        if (values.Length < 3 ||
                            !string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), SentinelVersion, StringComparison.Ordinal) ||
                            !string.Equals(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), nonce, StringComparison.Ordinal)) continue;
                        var handle = CadHandleService.NormalizeHexHandle(entity.Handle.ToString());
                        if (handle != null) result.Add(handle);
                    }
                }
            }
            return result.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static string RequireSentinelSignature(Document document, string handle)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException("Curtain P11 sentinel is missing.");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException("Curtain P11 sentinel is not a Solid3d.");
                var builder = new StringBuilder();
                AppendBounds(builder, solid.GeometricExtents);
                return Sha256(builder.ToString());
            }
        }

        private static void EnsureRegApp(Database database, Transaction transaction, string name)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(name)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = name };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
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
                    ?? throw new InvalidDataException("Curtain P11 " + label + " ownership contains an invalid Handle."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return result.AsReadOnly();
        }

        private static void RequireDisjoint(params IReadOnlyList<string>[] groups)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
                foreach (var handle in group)
                    if (!seen.Add(handle)) throw new InvalidOperationException("Curtain P11 ownership sets overlap.");
        }

        private static void RequireAllLive(Document document, IReadOnlyList<string> handles, bool solids, string label)
        {
            if (handles.Count == 0) return;
            var live = solids
                ? CadHandleService.GetLiveSolidHandles(document, handles)
                : CadHandleService.GetLiveHandles(document, handles);
            if (live.Count != handles.Count)
                throw new InvalidOperationException("Curtain P11 " + label + " ownership is not fully live.");
        }

        private static void RequireExpectedCount(string variable, int actual)
        {
            var raw = Environment.GetEnvironmentVariable(variable) ?? string.Empty;
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var expected) || expected <= 0 || expected != actual)
                throw new InvalidOperationException("Curtain P11 reopened aggregate count changed.");
        }

        private static bool RequiredBooleanEnvironment(string variable)
        {
            var raw = Environment.GetEnvironmentVariable(variable) ?? string.Empty;
            if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new InvalidOperationException("Curtain P11 boolean environment state is invalid.");
        }

        private static void Append(StringBuilder builder, string? value)
        {
            var text = value ?? string.Empty;
            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('|');
        }

        private static void AppendPoint(StringBuilder builder, Point3d point)
        {
            Append(builder, point.X.ToString("R", CultureInfo.InvariantCulture));
            Append(builder, point.Y.ToString("R", CultureInfo.InvariantCulture));
            Append(builder, point.Z.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AppendBounds(StringBuilder builder, Extents3d extents)
        {
            AppendPoint(builder, extents.MinPoint);
            AppendPoint(builder, extents.MaxPoint);
        }

        private static string Sha256(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty))).Replace("-", string.Empty);
        }

        private static string RequiredPath(string? value, string fileName)
        {
            var raw = (value ?? string.Empty).Trim();
            if (raw.Length == 0) throw new InvalidOperationException("Curtain P11 result path is missing.");
            var fullPath = Path.GetFullPath(raw);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), fileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curtain P11 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain P11 result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _)) return;
                var resultPath = RequiredPath(Environment.GetEnvironmentVariable(ResultVariable), ResultFileName);
                if (File.Exists(resultPath)) return;
                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=FAIL",
                    "command=QS3DCURTAINP11COMPLETE",
                    "nonce=" + nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_002_P11_ONLY",
                    "production_local002_qualified=false",
                    "p11_qualified=false",
                    "error_code=CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Curtain P11 marker already exists.");
            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, path);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string? value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private static string Boolean(bool value) => value ? "true" : "false";

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce)
            {
                Document = document; Project = project; Nonce = nonce;
            }
            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
        }

        private abstract class SequenceState
        {
            protected SequenceState(Document document, string projectId, string ownerId, string nonce)
            {
                Document = document; ProjectId = projectId; OwnerId = ownerId; Nonce = nonce;
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
                    throw new InvalidOperationException("Curtain P11 sequence context changed.");
            }
        }

        private sealed class SessionOneState : SequenceState
        {
            public SessionOneState(Document document, string projectId, string ownerId, string nonce, Snapshot before)
                : base(document, projectId, ownerId, nonce) { Before = before; }
            public Snapshot Before { get; }
            public Snapshot? After { get; set; }
            public bool UndoChecked { get; set; }
            public bool UndoCoherent { get; set; }
            public bool RedoChecked { get; set; }
            public bool RedoCoherent { get; set; }
        }

        private sealed class SessionTwoState : SequenceState
        {
            public SessionTwoState(Document document, string projectId, string ownerId, string nonce, Snapshot reopened)
                : base(document, projectId, ownerId, nonce) { Reopened = reopened; }
            public Snapshot Reopened { get; }
            public Snapshot? Rebuilt { get; set; }
            public bool OldGeneratedRemoved { get; set; }
            public bool NewGeneratedDisjoint { get; set; }
            public bool CountsStable { get; set; }
            public bool SourcePreserved { get; set; }
            public bool SentinelPreserved { get; set; }
        }

        private sealed class Snapshot
        {
            public Snapshot(
                string semanticSignature,
                IReadOnlyList<string> sourceHandles,
                IReadOnlyList<string> hostHandles,
                IReadOnlyList<string> frameHandles,
                IReadOnlyList<string> panelHandles,
                string sourceSignature,
                string sentinelHandle,
                string sentinelSignature,
                int healthIssueCount,
                long changeVersion,
                DateTime updatedUtc)
            {
                SemanticSignature = semanticSignature;
                SourceHandles = sourceHandles;
                HostHandles = hostHandles;
                FrameHandles = frameHandles;
                PanelHandles = panelHandles;
                SourceSignature = sourceSignature;
                SentinelHandle = sentinelHandle;
                SentinelSignature = sentinelSignature;
                HealthIssueCount = healthIssueCount;
                ChangeVersion = changeVersion;
                UpdatedUtc = updatedUtc;
            }
            public string SemanticSignature { get; }
            public IReadOnlyList<string> SourceHandles { get; }
            public IReadOnlyList<string> HostHandles { get; }
            public IReadOnlyList<string> FrameHandles { get; }
            public IReadOnlyList<string> PanelHandles { get; }
            public string SourceSignature { get; }
            public string SentinelHandle { get; }
            public string SentinelSignature { get; }
            public int HealthIssueCount { get; }
            public long ChangeVersion { get; }
            public DateTime UpdatedUtc { get; }
        }
    }
}
