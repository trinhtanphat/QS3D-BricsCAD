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
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P08 probe for the semantic and six native
    /// pre-commit Curtain boundaries. Production QS3DCURTAIN3D performs all work.
    /// </summary>
    public sealed class CurtainPanelAtomicFailureRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_P08_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_P08_NONCE";
        private const string ResultFileName = "curtain-panel-atomic-failure-runtime-result.txt";

        private static readonly string[] Phases =
        {
            CurtainWallBuildFailureInjection.SemanticRegeneration,
            CurtainWallBuildFailureInjection.LineHost,
            CurtainWallBuildFailureInjection.PathHost,
            CurtainWallBuildFailureInjection.LineFrame,
            CurtainWallBuildFailureInjection.PathFrame,
            CurtainWallBuildFailureInjection.LinePanel,
            CurtainWallBuildFailureInjection.PathPanel
        };

        private enum SequenceStage { None, Seeded, Prepared, Baseline, Armed, Verified, ValidReady, Complete }

        private sealed class OwnerOutput
        {
            public string ElementId { get; set; } = string.Empty;
            public string SourceKind { get; set; } = string.Empty;
            public IReadOnlyList<string> SourceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> GeneratedHandles { get; set; } = Array.Empty<string>();
        }

        private sealed class AttemptState
        {
            public string Phase { get; set; } = string.Empty;
            public string ProjectDigest { get; set; } = string.Empty;
            public string NativeDigest { get; set; } = string.Empty;
        }

        private sealed class SequenceState
        {
            public string Nonce { get; set; } = string.Empty;
            public SequenceStage Stage { get; set; }
            public string LineId { get; set; } = string.Empty;
            public string PathId { get; set; } = string.Empty;
            public OwnerOutput? LineBaseline { get; set; }
            public OwnerOutput? PathBaseline { get; set; }
            public ProjectStateSnapshot? BaselineSnapshot { get; set; }
            public string BaselineProjectDigest { get; set; } = string.Empty;
            public string BaselineNativeDigest { get; set; } = string.Empty;
            public AttemptState? Attempt { get; set; }
            public int NextPhase { get; set; }
            public int VerifiedPhases { get; set; }
        }

        private static readonly object StateSync = new object();
        private static SequenceState? State;
        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH", "SEED_LINE", "PREPARE_BASELINE", "VERIFY_BASELINE", "ARM_FAILURE",
            "VERIFY_FAILURE_ROLLBACK", "PREPARE_VALID_REPLACEMENT", "VERIFY_VALID_REPLACEMENT", "RESULT_PUBLISH"
        };
        private static readonly HashSet<string> FailureCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "STATE_REJECTED", "DATA_REJECTED", "IO_REJECTED", "OVERFLOW_REJECTED", "UNEXPECTED_REJECTED"
        };

        [CommandMethod("QS3DCURTAINP08SEEDLINE", CommandFlags.Modal)]
        public void SeedLine() => ExecuteStage("SEED_LINE", (document, _, nonce) =>
        {
            ObjectId id;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(CadGeometryGuard.ToDrawingUnits(document, 5d, "P08 line length"), 0d, 0d));
                try
                {
                    line.SetDatabaseDefaults(document.Database);
                    id = modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    transaction.Commit();
                    line = null!;
                }
                finally { line?.Dispose(); }
            }
            CurtainWallBuildFailureInjection.RequireIdle();
            lock (StateSync) State = new SequenceState { Nonce = nonce, Stage = SequenceStage.Seeded };
            document.Editor.SetImpliedSelection(new[] { id });
        });

        [CommandMethod("QS3DCURTAINP08PREPARE", CommandFlags.Modal)]
        public void PrepareBaseline() => ExecuteStage("PREPARE_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Seeded);
            var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            if (hosts.Count != 2) throw new InvalidOperationException("P08 requires exactly two synthetic GlassWalls.");
            var selection = new List<ObjectId>(2);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in hosts)
                {
                    RequireLegacyNoLevel(host);
                    var id = ResolveSingleSource(document, host);
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                        ?? throw new InvalidOperationException("P08 source is not live.");
                    if (entity is Line line)
                    {
                        if (!Near(ToMeters(document, line.StartPoint.Y), 0d)) throw new InvalidOperationException("P08 LINE is outside its lane.");
                        state.LineId = host.Id;
                    }
                    else if (entity is Polyline path)
                    {
                        if (path.Closed || path.NumberOfVertices != 3 || !Near(ToMeters(document, path.GetPoint2dAt(0).Y), 10d))
                            throw new InvalidOperationException("P08 path is not the expected open three-vertex POLYLINE.");
                        state.PathId = host.Id;
                    }
                    else throw new InvalidOperationException("P08 requires one LINE and one open POLYLINE source.");
                    selection.Add(id);
                }
                transaction.Commit();
            }
            if (string.IsNullOrWhiteSpace(state.LineId) || string.IsNullOrWhiteSpace(state.PathId))
                throw new InvalidOperationException("P08 mixed-source classification is incomplete.");
            document.Editor.SetImpliedSelection(selection.ToArray());
            state.Stage = SequenceStage.Prepared;
        });

        [CommandMethod("QS3DCURTAINP08BASELINE", CommandFlags.Modal)]
        public void VerifyBaseline() => ExecuteStage("VERIFY_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Prepared);
            state.LineBaseline = CaptureCleanOwner(document, project, RequiredOwner(project, state.LineId), "Line");
            state.PathBaseline = CaptureCleanOwner(document, project, RequiredOwner(project, state.PathId), "OpenPolyline");
            RequireDisjoint(state.LineBaseline, state.PathBaseline);
            state.BaselineSnapshot = ProjectStateSnapshot.Capture(project);
            state.BaselineProjectDigest = CaptureProjectDigest(project);
            state.BaselineNativeDigest = CaptureNativeDigest(document);
            state.Stage = SequenceStage.Baseline;
        });

        [CommandMethod("QS3DCURTAINP08ARM", CommandFlags.Modal)]
        public void ArmNextFailure() => ExecuteStage("ARM_FAILURE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, stateForArm: true);
            if (state.NextPhase < 0 || state.NextPhase >= Phases.Length) throw new InvalidOperationException("P08 phase index is complete.");
            var phase = Phases[state.NextPhase];
            var width = (0.88d - state.NextPhase * 0.01d).ToString("R", CultureInfo.InvariantCulture);
            RequiredOwner(project, state.LineId).SetProperty("CurtainMaxPanelWidthM", width);
            RequiredOwner(project, state.PathId).SetProperty("CurtainMaxPanelWidthM", width);
            project.Touch();
            state.Attempt = new AttemptState { Phase = phase, ProjectDigest = CaptureProjectDigest(project), NativeDigest = CaptureNativeDigest(document) };
            CurtainWallBuildFailureInjection.Arm(nonce, phase);
            SelectSources(document, project, state);
            state.Stage = SequenceStage.Armed;
        });

        [CommandMethod("QS3DCURTAINP08VERIFY", CommandFlags.Modal)]
        public void VerifyFailureRollback() => ExecuteStage("VERIFY_FAILURE_ROLLBACK", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Armed);
            var attempt = state.Attempt ?? throw new InvalidOperationException("P08 attempt snapshot is missing.");
            CurtainWallBuildFailureInjection.RequireConsumed(nonce, attempt.Phase);
            if (!string.Equals(CaptureProjectDigest(project), attempt.ProjectDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P08 failure changed semantic state instead of restoring the command snapshot.");
            if (!string.Equals(CaptureNativeDigest(document), attempt.NativeDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P08 failure left partial native phase output.");
            RequireBaselineOutputsLive(document, state);
            RestoreBaseline(project, state);
            state.Attempt = null;
            state.NextPhase++;
            state.VerifiedPhases++;
            state.Stage = SequenceStage.Verified;
        });

        [CommandMethod("QS3DCURTAINP08VALID", CommandFlags.Modal)]
        public void PrepareValidReplacement() => ExecuteStage("PREPARE_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Verified);
            if (state.VerifiedPhases != Phases.Length || state.NextPhase != Phases.Length)
                throw new InvalidOperationException("P08 seven-phase matrix is incomplete.");
            CurtainWallBuildFailureInjection.RequireIdle();
            RequiredOwner(project, state.LineId).SetProperty("CurtainMaxPanelWidthM", "0.73");
            RequiredOwner(project, state.PathId).SetProperty("CurtainMaxPanelWidthM", "0.71");
            project.Touch();
            SelectSources(document, project, state);
            state.Stage = SequenceStage.ValidReady;
        });

        [CommandMethod("QS3DCURTAINP08PROBE", CommandFlags.Modal)]
        public void VerifyValidReplacement() => ExecuteStage("VERIFY_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var resultPath = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable)!);
            if (File.Exists(resultPath)) throw new IOException("P08 result already exists.");
            var state = RequireState(nonce, SequenceStage.ValidReady);
            CurtainWallBuildFailureInjection.RequireIdle();
            var line = CaptureCleanOwner(document, project, RequiredOwner(project, state.LineId), "Line");
            var path = CaptureCleanOwner(document, project, RequiredOwner(project, state.PathId), "OpenPolyline");
            RequireDisjoint(line, path);
            var oldGenerated = RequiredBaseline(state.LineBaseline).GeneratedHandles.Concat(RequiredBaseline(state.PathBaseline).GeneratedHandles).ToList();
            if (CadHandleService.Resolve(document, oldGenerated).Count != 0) throw new InvalidOperationException("P08 valid control left old generated output live.");
            if (line.GeneratedHandles.Intersect(oldGenerated, StringComparer.OrdinalIgnoreCase).Any() || path.GeneratedHandles.Intersect(oldGenerated, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P08 valid control reused an old generated handle.");
            if (string.Equals(CaptureNativeDigest(document), state.BaselineNativeDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P08 valid control did not replace native output.");
            state.Stage = SequenceStage.Complete;
            WriteMarkerAtomic(resultPath, new[]
            {
                "status=PASS", "command=QS3DCURTAINP08PROBE", "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + nonce, "schema=QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1",
                "qualification_boundary=LOCAL_002_P08_ONLY", "production_local002_qualified=false",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"), "legacy_no_level=true",
                "mixed_line_path=true", "injected_phase_count=7", "semantic_regeneration_rollback=true",
                "line_host_rollback=true", "path_host_rollback=true", "line_frame_rollback=true",
                "path_frame_rollback=true", "line_panel_rollback=true", "path_panel_rollback=true",
                "whole_batch_native_preserved=true", "whole_batch_semantic_preserved=true",
                "source_geometry_preserved=true", "valid_replacement_succeeded=true",
                "valid_old_sets_removed=true", "valid_new_sets_complete=true",
                "baseline_generated_count=" + oldGenerated.Count.ToString(CultureInfo.InvariantCulture),
                "valid_generated_count=" + checked(line.GeneratedHandles.Count + path.GeneratedHandles.Count).ToString(CultureInfo.InvariantCulture),
                "health_issue_count=0"
            });
            document.Editor.WriteMessage("\nQS3D Curtain panel P08 seven-boundary atomic-failure probe PASS.");
        });

        private static SequenceState RequireState(string nonce, SequenceStage expected)
        {
            lock (StateSync)
            {
                if (State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) || State.Stage != expected)
                    throw new InvalidOperationException("P08 runtime command sequence is invalid.");
                return State;
            }
        }

        private static SequenceState RequireState(string nonce, bool stateForArm)
        {
            lock (StateSync)
            {
                if (!stateForArm || State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) ||
                    (State.Stage != SequenceStage.Baseline && State.Stage != SequenceStage.Verified))
                    throw new InvalidOperationException("P08 arm sequence is invalid.");
                return State;
            }
        }

        private static void ExecuteStage(string phase, Action<Document, ProjectState, string> action)
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireAutomation(requestedPath, nonce);
                var document = Application.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                var project = ProjectContextCoordinator.TryGetReadOnly(document, out var existing) ? existing : ProjectContextCoordinator.GetOrCreate(document);
                action(document, project, nonce);
            }
            catch (Exception error)
            {
                CurtainWallBuildFailureInjection.Clear(nonce);
                TryWriteFailure(requestedPath, nonce, phase, FailureCode(error));
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D Curtain panel P08 probe stage failed. See the sanitized local result.");
                throw;
            }
        }

        private static OwnerOutput CaptureCleanOwner(Document document, ProjectState project, ProjectElement owner, string sourceKind)
        {
            RequireLegacyNoLevel(owner);
            if (owner.IsGeneratedCurtainPanelStale()) throw new InvalidOperationException("P08 owner is stale.");
            var source = CanonicalHandles(owner.SourceHandles, "P08 source");
            var generated = new List<string>();
            generated.Add(CanonicalHandle(RequiredProperty(owner, "GeneratedSolidHandle"), "P08 host"));
            generated.AddRange(CanonicalHandles(SplitProperty(owner, "GeneratedCurtainFrameHandles"), "P08 frames"));
            generated.AddRange(CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P08 panels"));
            if (source.Count != 1 || generated.Count < 3 || generated.Distinct(StringComparer.OrdinalIgnoreCase).Count() != generated.Count)
                throw new InvalidOperationException("P08 owner output is incomplete or ambiguous.");
            if (CadHandleService.Resolve(document, generated).Count != generated.Count) throw new InvalidOperationException("P08 generated output is not completely live.");
            var livePanels = new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P08 panels")), StringComparer.OrdinalIgnoreCase);
            var issues = new GeneratedCurtainPanelHealthService().Inspect(project, livePanels)
                .Concat(CurtainWallPanelLiveStateService.Inspect(document, project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project))
                .Where(x => string.Equals(x.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase) && x.Severity != HealthSeverity.Info).ToList();
            if (issues.Count != 0) throw new InvalidOperationException("P08 owner has blocking panel Health.");
            return new OwnerOutput { ElementId = owner.Id, SourceKind = sourceKind, SourceHandles = source, GeneratedHandles = generated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly() };
        }

        private static void RequireDisjoint(OwnerOutput left, OwnerOutput right)
        {
            var sets = new[] { left.SourceHandles, left.GeneratedHandles, right.SourceHandles, right.GeneratedHandles };
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in sets) foreach (var handle in set) if (!all.Add(handle)) throw new InvalidOperationException("P08 mixed ownership sets overlap.");
        }

        private static void RequireBaselineOutputsLive(Document document, SequenceState state)
        {
            var all = RequiredBaseline(state.LineBaseline).GeneratedHandles.Concat(RequiredBaseline(state.PathBaseline).GeneratedHandles).ToList();
            if (CadHandleService.Resolve(document, all).Count != all.Count) throw new InvalidOperationException("P08 failure changed baseline generated ownership.");
        }

        private static OwnerOutput RequiredBaseline(OwnerOutput? value) => value ?? throw new InvalidOperationException("P08 baseline output is missing.");

        private static void RestoreBaseline(ProjectState project, SequenceState state)
        {
            (state.BaselineSnapshot ?? throw new InvalidOperationException("P08 semantic baseline snapshot is missing.")).Restore(project);
            if (!string.Equals(CaptureProjectDigest(project), state.BaselineProjectDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P08 semantic baseline restoration is incomplete.");
        }

        private static void SelectSources(Document document, ProjectState project, SequenceState state) => document.Editor.SetImpliedSelection(new[]
        {
            ResolveSingleSource(document, RequiredOwner(project, state.LineId)),
            ResolveSingleSource(document, RequiredOwner(project, state.PathId))
        });

        private static ProjectElement RequiredOwner(ProjectState project, string id)
        {
            var owner = project.FindElement(id) ?? throw new InvalidOperationException("P08 GlassWall owner is missing.");
            if (owner.Category != ElementCategory.GlassWall) throw new InvalidOperationException("P08 owner category changed.");
            return owner;
        }

        private static ObjectId ResolveSingleSource(Document document, ProjectElement owner)
        {
            var handles = CanonicalHandles(owner.SourceHandles, "P08 source");
            var ids = CadHandleService.Resolve(document, handles);
            if (handles.Count != 1 || ids.Count != 1) throw new InvalidOperationException("P08 owner requires one live source.");
            return ids[0];
        }

        private static string CaptureNativeDigest(Document document)
        {
            var records = new List<string>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = (BlockTableRecord)transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead);
                foreach (ObjectId id in space)
                {
                    if (id.IsNull || id.IsErased) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var builder = new StringBuilder();
                    Append(builder, CanonicalHandle(entity.Handle.ToString(), "P08 native handle")); Append(builder, entity.GetType().FullName);
                    try { var extents = entity.GeometricExtents; AppendPoint(builder, extents.MinPoint); AppendPoint(builder, extents.MaxPoint); }
                    catch { Append(builder, "NO_EXTENTS"); }
                    records.Add(builder.ToString());
                }
                transaction.Commit();
            }
            return Sha256(string.Join("", records.OrderBy(x => x, StringComparer.Ordinal)));
        }

        private static string CaptureProjectDigest(ProjectState project)
        {
            var builder = new StringBuilder();
            Append(builder, project.ProjectId); Append(builder, project.Name); Append(builder, project.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.ChangeVersion.ToString(CultureInfo.InvariantCulture)); Append(builder, project.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            Append(builder, project.DrawingPath); Append(builder, project.DrawingFingerprint); Append(builder, project.ActiveFloorId); Append(builder, project.ActiveZoneId); AppendPairs(builder, project.Metadata);
            foreach (var zone in project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "zone"); Append(builder, zone.Id); Append(builder, zone.Name); }
            foreach (var floor in project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "floor"); Append(builder, floor.Id); Append(builder, floor.Name); Append(builder, floor.ElevationM.ToString("R", CultureInfo.InvariantCulture)); }
            foreach (var family in project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "family"); Append(builder, family.Id); Append(builder, family.Name); Append(builder, family.Category.ToString()); AppendPairs(builder, family.Properties); }
            foreach (var element in project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "element"); Append(builder, element.Id); Append(builder, element.Category.ToString()); Append(builder, element.FamilyId); Append(builder, element.FloorId); Append(builder, element.ZoneId); Append(builder, element.DrawingFingerprint); Append(builder, element.Dirty.ToString()); Append(builder, element.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
                foreach (var handle in element.SourceHandles) { Append(builder, "source"); Append(builder, handle); }
                foreach (var dependency in element.DependsOn) { Append(builder, "depends"); Append(builder, dependency); }
                AppendPairs(builder, element.Properties); foreach (var pair in element.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) { Append(builder, pair.Key); Append(builder, pair.Value.ToString("R", CultureInfo.InvariantCulture)); }
            }
            foreach (var audit in project.AuditEvents) { Append(builder, "audit"); Append(builder, audit.Utc.ToString("O", CultureInfo.InvariantCulture)); Append(builder, audit.Action); Append(builder, audit.ElementId); Append(builder, audit.Detail); Append(builder, audit.Actor); Append(builder, audit.CorrelationId); }
            return Sha256(builder.ToString());
        }

        private static void AppendPairs(StringBuilder builder, IEnumerable<KeyValuePair<string, string>> pairs) { foreach (var pair in pairs.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) { Append(builder, pair.Key); Append(builder, pair.Value); } }
        private static void AppendPoint(StringBuilder builder, Point3d point) { Append(builder, point.X.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Y.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Z.ToString("R", CultureInfo.InvariantCulture)); }
        private static void Append(StringBuilder builder, string? value) { var normalized = value ?? string.Empty; builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized).Append('|'); }
        private static string Sha256(string value) { using (var algorithm = SHA256.Create()) return string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
        private static IReadOnlyList<string> SplitProperty(ProjectElement owner, string key) => RequiredProperty(owner, key).Split(new[] { ';' }, StringSplitOptions.None);
        private static string RequiredProperty(ProjectElement owner, string key) => owner.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException("P08 required metadata is missing.");
        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles, string label) => handles.Select(x => CanonicalHandle(x, label)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        private static string CanonicalHandle(string? handle, string label) => CadHandleService.NormalizeHexHandle(handle) ?? throw new InvalidOperationException(label + " is invalid.");
        private static void RequireLegacyNoLevel(ProjectElement element) { if (CadVerticalPlacementResolver.HasConfiguredLevel(element)) throw new InvalidOperationException("P08 requires legacy/no-Level placement."); }
        private static double ToMeters(Document document, double value) => CadGeometryGuard.ToMeters(document, value, "P08 drawing conversion");
        private static bool Near(double left, double right) => Math.Abs(left - right) <= 1e-6d;

        private static void RequireAutomation(string? requestedPath, string nonce) { if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("P08 runtime commands are automation-only."); RequiredResultPath(requestedPath!); }
        private static string RequiredResultPath(string value) { var fullPath = Path.GetFullPath(value); var directory = Path.GetDirectoryName(fullPath); if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("P08 result filename is invalid."); if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("P08 result directory must already exist."); return fullPath; }
        private static string FailureCode(Exception error) { if (error is InvalidDataException) return "DATA_REJECTED"; if (error is OverflowException) return "OVERFLOW_REJECTED"; if (error is IOException) return "IO_REJECTED"; if (error is InvalidOperationException) return "STATE_REJECTED"; return "UNEXPECTED_REJECTED"; }
        private static void TryWriteFailure(string? requestedPath, string nonce, string phase, string failureCode)
        {
            try { var normalized = (requestedPath ?? string.Empty).Trim(); if (normalized.Length > 0 && !File.Exists(normalized) && Guid.TryParseExact(nonce, "N", out _) && FailurePhases.Contains(phase) && FailureCodes.Contains(failureCode)) WriteMarkerAtomic(normalized, new[] { "status=FAIL", "command=QS3DCURTAINP08PROBE", "nonce=" + nonce, "schema=QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1", "qualification_boundary=LOCAL_002_P08_ONLY", "production_local002_qualified=false", "error_code=CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_FAILED", "failure_phase=" + phase, "failure_code=" + failureCode }); } catch { }
        }
        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath); if (File.Exists(fullPath)) throw new IOException("P08 result already exists."); var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) { foreach (var line in lines) writer.WriteLine(OneLine(line)); writer.Flush(); stream.Flush(true); } File.Move(tempPath, fullPath); }
            finally { try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
