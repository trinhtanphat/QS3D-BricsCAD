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
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P07 probe. Production QS3DCURTAIN3D is the
    /// only native builder; this class prepares invalid later-batch inputs and
    /// verifies whole-command semantic/native rollback.
    /// </summary>
    public sealed class CurtainPanelBudgetProvenanceRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_P07_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_P07_NONCE";
        private const string ResultFileName = "curtain-panel-budget-provenance-runtime-result.txt";
        private const int ExpectedOwners = 2;
        private const int ExpectedOpenings = 2;

        private enum SequenceStage
        {
            None,
            HostsSeeded,
            OpeningsSeeded,
            Prepared,
            Baseline,
            BudgetReady,
            BudgetVerified,
            MissingReady,
            MissingVerified,
            OffHostReady,
            OffHostVerified,
            ValidReady,
            Complete
        }

        private sealed class OwnerState
        {
            public string ElementId { get; set; } = string.Empty;
            public string SourceHandle { get; set; } = string.Empty;
            public IReadOnlyList<string> PanelHandles { get; set; } = Array.Empty<string>();
        }

        private sealed class AttemptState
        {
            public string ProjectDigest { get; set; } = string.Empty;
            public string NativeDigest { get; set; } = string.Empty;
            public IReadOnlyList<string> FirstPanels { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> LaterPanels { get; set; } = Array.Empty<string>();
        }

        private sealed class SequenceState
        {
            public string Nonce { get; set; } = string.Empty;
            public SequenceStage Stage { get; set; }
            public string FirstId { get; set; } = string.Empty;
            public string LaterId { get; set; } = string.Empty;
            public string OnHostOpeningId { get; set; } = string.Empty;
            public string OffHostOpeningId { get; set; } = string.Empty;
            public OwnerState? FirstBaseline { get; set; }
            public OwnerState? LaterBaseline { get; set; }
            public ProjectStateSnapshot? BaselineSnapshot { get; set; }
            public string BaselineProjectDigest { get; set; } = string.Empty;
            public string BaselineNativeDigest { get; set; } = string.Empty;
            public AttemptState? Attempt { get; set; }
            public int RefusalCount { get; set; }
        }

        private static readonly object StateSync = new object();
        private static SequenceState? State;

        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH", "SEED_HOSTS", "SEED_OPENINGS", "PREPARE_BASELINE", "VERIFY_BASELINE",
            "PREPARE_BUDGET", "VERIFY_BUDGET_ROLLBACK", "PREPARE_MISSING_SOURCE", "VERIFY_MISSING_SOURCE_ROLLBACK",
            "PREPARE_OFF_HOST", "VERIFY_OFF_HOST_ROLLBACK", "PREPARE_VALID_REPLACEMENT", "VERIFY_VALID_REPLACEMENT",
            "RESULT_PUBLISH"
        };

        private static readonly HashSet<string> FailureCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "STATE_REJECTED", "DATA_REJECTED", "IO_REJECTED", "OVERFLOW_REJECTED", "UNEXPECTED_REJECTED"
        };

        [CommandMethod("QS3DCURTAINP07SEEDHOSTS", CommandFlags.Modal)]
        public void SeedHosts() => ExecuteStage("SEED_HOSTS", (document, _, nonce) =>
        {
            var ids = new List<ObjectId>(ExpectedOwners);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ids.Add(AppendLine(document, transaction, modelSpace, 0d, 0d, 5d, 0d, "P07 first host"));
                ids.Add(AppendLine(document, transaction, modelSpace, 0d, 10d, 5d, 10d, "P07 later host"));
                transaction.Commit();
            }
            lock (StateSync) State = new SequenceState { Nonce = nonce, Stage = SequenceStage.HostsSeeded };
            document.Editor.SetImpliedSelection(ids.ToArray());
        });

        [CommandMethod("QS3DCURTAINP07SEEDOPENINGS", CommandFlags.Modal)]
        public void SeedOpenings() => ExecuteStage("SEED_OPENINGS", (document, _, nonce) =>
        {
            RequireState(nonce, SequenceStage.HostsSeeded);
            var ids = new List<ObjectId>(ExpectedOpenings);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ids.Add(AppendLine(document, transaction, modelSpace, 2d, 10d, 3d, 10d, "P07 on-host opening"));
                ids.Add(AppendLine(document, transaction, modelSpace, 2d, 30d, 3d, 30d, "P07 off-host opening"));
                transaction.Commit();
            }
            document.Editor.SetImpliedSelection(ids.ToArray());
            lock (StateSync) State!.Stage = SequenceStage.OpeningsSeeded;
        });

        [CommandMethod("QS3DCURTAINP07PREPARE", CommandFlags.Modal)]
        public void PrepareBaseline() => ExecuteStage("PREPARE_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.OpeningsSeeded);
            var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            var openings = project.Elements.Where(x => x.Category == ElementCategory.Door).ToList();
            if (hosts.Count != ExpectedOwners || openings.Count != ExpectedOpenings)
                throw new InvalidOperationException("P07 requires exactly two GlassWalls and two Doors.");

            var selectedHosts = new List<ObjectId>(ExpectedOwners);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in hosts)
                {
                    RequireLegacyNoLevel(host);
                    var id = ResolveSingleSource(document, host);
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                        ?? throw new InvalidOperationException("P07 GlassWall source is not a LINE.");
                    var yM = ToMeters(document, line.StartPoint.Y);
                    if (Near(yM, 0d)) state.FirstId = host.Id;
                    else if (Near(yM, 10d)) state.LaterId = host.Id;
                    else throw new InvalidOperationException("P07 GlassWall source is outside the synthetic lanes.");
                    selectedHosts.Add(id);
                }
                foreach (var opening in openings)
                {
                    RequireLegacyNoLevel(opening);
                    if (opening.Properties.ContainsKey("HostWallId") || opening.DependsOn.Count != 0)
                        throw new InvalidOperationException("P07 Door must begin unlinked.");
                    var id = ResolveSingleSource(document, opening);
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                        ?? throw new InvalidOperationException("P07 Door source is not a LINE.");
                    var yM = ToMeters(document, line.StartPoint.Y);
                    if (Near(yM, 10d)) state.OnHostOpeningId = opening.Id;
                    else if (Near(yM, 30d)) state.OffHostOpeningId = opening.Id;
                    else throw new InvalidOperationException("P07 Door source is outside the synthetic lanes.");
                }
                transaction.Commit();
            }
            if (string.IsNullOrWhiteSpace(state.FirstId) || string.IsNullOrWhiteSpace(state.LaterId) ||
                string.IsNullOrWhiteSpace(state.OnHostOpeningId) || string.IsNullOrWhiteSpace(state.OffHostOpeningId))
                throw new InvalidOperationException("P07 semantic lane classification is incomplete.");
            document.Editor.SetImpliedSelection(selectedHosts.ToArray());
            state.Stage = SequenceStage.Prepared;
        });

        [CommandMethod("QS3DCURTAINP07BASELINE", CommandFlags.Modal)]
        public void VerifyBaseline() => ExecuteStage("VERIFY_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Prepared);
            state.FirstBaseline = CaptureCleanOwner(document, project, RequiredOwner(project, state.FirstId));
            state.LaterBaseline = CaptureCleanOwner(document, project, RequiredOwner(project, state.LaterId));
            if (state.FirstBaseline.PanelHandles.Intersect(state.LaterBaseline.PanelHandles, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P07 baseline panel owners overlap.");
            state.BaselineSnapshot = ProjectStateSnapshot.Capture(project);
            state.BaselineProjectDigest = CaptureProjectDigest(project);
            state.BaselineNativeDigest = CaptureNativeDigest(document);
            state.Stage = SequenceStage.Baseline;
        });

        [CommandMethod("QS3DCURTAINP07BUDGET", CommandFlags.Modal)]
        public void PrepareBudgetFailure() => ExecuteStage("PREPARE_BUDGET", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Baseline);
            var later = RequiredOwner(project, state.LaterId);
            later.SetProperty("CurtainMaxPanelWidthM", "0.07");
            later.SetProperty("CurtainMaxPanelHeightM", "0.06");
            project.Touch();
            state.Attempt = CaptureAttempt(document, project, state);
            SelectHosts(document, project, state);
            state.Stage = SequenceStage.BudgetReady;
        });

        [CommandMethod("QS3DCURTAINP07CHECKBUDGET", CommandFlags.Modal)]
        public void VerifyBudgetRollback() => ExecuteStage("VERIFY_BUDGET_ROLLBACK", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.BudgetReady);
            VerifyAttemptUnchanged(document, project, state);
            RestoreBaseline(project, state);
            state.RefusalCount++;
            state.Stage = SequenceStage.BudgetVerified;
        });

        [CommandMethod("QS3DCURTAINP07MISSING", CommandFlags.Modal)]
        public void PrepareMissingSourceFailure() => ExecuteStage("PREPARE_MISSING_SOURCE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.BudgetVerified);
            new HostLinkService().LinkOpening(project, state.OnHostOpeningId, state.LaterId);
            var opening = RequiredOpening(project, state.OnHostOpeningId);
            opening.SourceHandles.Clear();
            project.Touch();
            state.Attempt = CaptureAttempt(document, project, state);
            SelectHosts(document, project, state);
            state.Stage = SequenceStage.MissingReady;
        });

        [CommandMethod("QS3DCURTAINP07CHECKMISSING", CommandFlags.Modal)]
        public void VerifyMissingSourceRollback() => ExecuteStage("VERIFY_MISSING_SOURCE_ROLLBACK", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.MissingReady);
            VerifyAttemptUnchanged(document, project, state);
            RestoreBaseline(project, state);
            state.RefusalCount++;
            state.Stage = SequenceStage.MissingVerified;
        });

        [CommandMethod("QS3DCURTAINP07OFFHOST", CommandFlags.Modal)]
        public void PrepareOffHostFailure() => ExecuteStage("PREPARE_OFF_HOST", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.MissingVerified);
            new HostLinkService().LinkOpening(project, state.OffHostOpeningId, state.LaterId);
            project.Touch();
            state.Attempt = CaptureAttempt(document, project, state);
            SelectHosts(document, project, state);
            state.Stage = SequenceStage.OffHostReady;
        });

        [CommandMethod("QS3DCURTAINP07CHECKOFFHOST", CommandFlags.Modal)]
        public void VerifyOffHostRollback() => ExecuteStage("VERIFY_OFF_HOST_ROLLBACK", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.OffHostReady);
            VerifyAttemptUnchanged(document, project, state);
            RestoreBaseline(project, state);
            state.RefusalCount++;
            state.Stage = SequenceStage.OffHostVerified;
        });

        [CommandMethod("QS3DCURTAINP07VALID", CommandFlags.Modal)]
        public void PrepareValidReplacement() => ExecuteStage("PREPARE_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.OffHostVerified);
            RequiredOwner(project, state.FirstId).SetProperty("CurtainMaxPanelWidthM", "0.91");
            RequiredOwner(project, state.LaterId).SetProperty("CurtainMaxPanelWidthM", "0.93");
            project.Touch();
            SelectHosts(document, project, state);
            state.Stage = SequenceStage.ValidReady;
        });

        [CommandMethod("QS3DCURTAINP07PROBE", CommandFlags.Modal)]
        public void VerifyValidReplacement() => ExecuteStage("VERIFY_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var resultPath = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable)!);
            if (File.Exists(resultPath)) throw new IOException("P07 result already exists.");
            var state = RequireState(nonce, SequenceStage.ValidReady);
            if (state.RefusalCount != 3) throw new InvalidOperationException("P07 refusal matrix is incomplete.");
            var first = CaptureCleanOwner(document, project, RequiredOwner(project, state.FirstId));
            var later = CaptureCleanOwner(document, project, RequiredOwner(project, state.LaterId));
            var firstOld = state.FirstBaseline?.PanelHandles ?? throw new InvalidOperationException("P07 first baseline is missing.");
            var laterOld = state.LaterBaseline?.PanelHandles ?? throw new InvalidOperationException("P07 later baseline is missing.");
            if (first.PanelHandles.Intersect(firstOld, StringComparer.OrdinalIgnoreCase).Any() ||
                later.PanelHandles.Intersect(laterOld, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P07 valid replacement reused an old panel handle.");
            if (CadHandleService.Resolve(document, firstOld.Concat(laterOld)).Count != 0)
                throw new InvalidOperationException("P07 valid replacement left an old panel live.");
            if (string.Equals(CaptureNativeDigest(document), state.BaselineNativeDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P07 valid control did not change native output.");
            state.Stage = SequenceStage.Complete;
            WriteMarkerAtomic(resultPath, new[]
            {
                "status=PASS", "command=QS3DCURTAINP07PROBE", "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + nonce, "schema=QS3D_CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_V1",
                "qualification_boundary=LOCAL_002_P07_ONLY", "production_local002_qualified=false",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"), "legacy_no_level=true",
                "glass_wall_count=2", "opening_count=2", "refusal_count=3",
                "panel_budget_refused=true", "missing_source_refused=true", "off_host_refused=true",
                "later_element_failure_verified=true", "whole_batch_native_preserved=true",
                "whole_batch_semantic_preserved=true", "valid_replacement_succeeded=true",
                "valid_old_sets_removed=true", "valid_new_sets_complete=true",
                "baseline_panel_count=" + checked(firstOld.Count + laterOld.Count).ToString(CultureInfo.InvariantCulture),
                "valid_replacement_panel_count=" + checked(first.PanelHandles.Count + later.PanelHandles.Count).ToString(CultureInfo.InvariantCulture),
                "health_issue_count=0"
            });
            document.Editor.WriteMessage("\nQS3D Curtain panel P07 budget/provenance rollback probe PASS.");
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
            catch (Exception error)
            {
                TryWriteFailure(requestedPath, nonce, phase, FailureCode(error));
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Curtain panel P07 probe stage failed. See the sanitized local result.");
                throw;
            }
        }

        private static SequenceState RequireState(string nonce, SequenceStage expected)
        {
            lock (StateSync)
            {
                if (State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) || State.Stage != expected)
                    throw new InvalidOperationException("P07 runtime command sequence is invalid.");
                return State;
            }
        }

        private static ProjectElement RequiredOwner(ProjectState project, string id)
        {
            var owner = project.FindElement(id) ?? throw new InvalidOperationException("P07 GlassWall owner is missing.");
            if (owner.Category != ElementCategory.GlassWall) throw new InvalidOperationException("P07 owner category changed.");
            return owner;
        }

        private static ProjectElement RequiredOpening(ProjectState project, string id)
        {
            var opening = project.FindElement(id) ?? throw new InvalidOperationException("P07 Door is missing.");
            if (opening.Category != ElementCategory.Door) throw new InvalidOperationException("P07 opening category changed.");
            return opening;
        }

        private static OwnerState CaptureCleanOwner(Document document, ProjectState project, ProjectElement owner)
        {
            RequireLegacyNoLevel(owner);
            if (owner.IsGeneratedCurtainPanelStale()) throw new InvalidOperationException("P07 clean owner is stale.");
            if (!string.Equals(RequiredProperty(owner, "GeneratedCurtainPanelBuildState"), "Complete", StringComparison.Ordinal))
                throw new InvalidOperationException("P07 panel build state is not Complete.");
            var source = CanonicalHandles(owner.SourceHandles, "P07 source");
            var panels = CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P07 panels");
            if (source.Count != 1 || panels.Count == 0) throw new InvalidOperationException("P07 clean owner output is incomplete.");
            if (!int.TryParse(RequiredProperty(owner, "GeneratedCurtainPanelCount"), NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count != panels.Count)
                throw new InvalidOperationException("P07 panel count metadata is inconsistent.");
            if (CadHandleService.GetLiveSolidHandles(document, panels).Count != panels.Count)
                throw new InvalidOperationException("P07 panel set is not completely live.");
            var live = new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, panels), StringComparer.OrdinalIgnoreCase);
            var issues = new GeneratedCurtainPanelHealthService().Inspect(project, live)
                .Concat(CurtainWallPanelLiveStateService.Inspect(document, project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project))
                .Where(x => string.Equals(x.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase) && x.Severity != HealthSeverity.Info)
                .ToList();
            if (issues.Count != 0) throw new InvalidOperationException("P07 clean owner has blocking panel Health.");
            return new OwnerState { ElementId = owner.Id, SourceHandle = source[0], PanelHandles = panels };
        }

        private static AttemptState CaptureAttempt(Document document, ProjectState project, SequenceState state) => new AttemptState
        {
            ProjectDigest = CaptureProjectDigest(project), NativeDigest = CaptureNativeDigest(document),
            FirstPanels = state.FirstBaseline?.PanelHandles ?? throw new InvalidOperationException("P07 first baseline is missing."),
            LaterPanels = state.LaterBaseline?.PanelHandles ?? throw new InvalidOperationException("P07 later baseline is missing.")
        };

        private static void VerifyAttemptUnchanged(Document document, ProjectState project, SequenceState state)
        {
            var attempt = state.Attempt ?? throw new InvalidOperationException("P07 attempt snapshot is missing.");
            if (!string.Equals(CaptureProjectDigest(project), attempt.ProjectDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P07 production refusal changed semantic project state.");
            if (!string.Equals(CaptureNativeDigest(document), attempt.NativeDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P07 production refusal changed native objects or bounds.");
            if (CadHandleService.GetLiveSolidHandles(document, attempt.FirstPanels).Count != attempt.FirstPanels.Count ||
                CadHandleService.GetLiveSolidHandles(document, attempt.LaterPanels).Count != attempt.LaterPanels.Count)
                throw new InvalidOperationException("P07 production refusal changed an old panel set.");
            state.Attempt = null;
        }

        private static void RestoreBaseline(ProjectState project, SequenceState state)
        {
            var snapshot = state.BaselineSnapshot ?? throw new InvalidOperationException("P07 baseline snapshot is missing.");
            snapshot.Restore(project);
            if (!string.Equals(CaptureProjectDigest(project), state.BaselineProjectDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P07 semantic baseline restoration is incomplete.");
        }

        private static void SelectHosts(Document document, ProjectState project, SequenceState state) =>
            document.Editor.SetImpliedSelection(new[]
            {
                ResolveSingleSource(document, RequiredOwner(project, state.FirstId)),
                ResolveSingleSource(document, RequiredOwner(project, state.LaterId))
            });

        private static ObjectId ResolveSingleSource(Document document, ProjectElement element)
        {
            var handles = CanonicalHandles(element.SourceHandles, "P07 source");
            var ids = CadHandleService.Resolve(document, handles);
            if (handles.Count != 1 || ids.Count != 1) throw new InvalidOperationException("P07 element requires one live source.");
            return ids[0];
        }

        private static ObjectId AppendLine(Document document, Transaction transaction, BlockTableRecord modelSpace,
            double x1M, double y1M, double x2M, double y2M, string label)
        {
            var line = new Line(
                new Point3d(CadGeometryGuard.ToDrawingUnits(document, x1M, label + " X1"), CadGeometryGuard.ToDrawingUnits(document, y1M, label + " Y1"), 0d),
                new Point3d(CadGeometryGuard.ToDrawingUnits(document, x2M, label + " X2"), CadGeometryGuard.ToDrawingUnits(document, y2M, label + " Y2"), 0d));
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
                    Append(builder, CanonicalHandle(entity.Handle.ToString(), "P07 native handle"));
                    Append(builder, entity.GetType().FullName);
                    try
                    {
                        var extents = entity.GeometricExtents;
                        AppendPoint(builder, extents.MinPoint);
                        AppendPoint(builder, extents.MaxPoint);
                    }
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
            Append(builder, project.DrawingPath); Append(builder, project.DrawingFingerprint); Append(builder, project.ActiveFloorId); Append(builder, project.ActiveZoneId);
            AppendPairs(builder, project.Metadata);
            foreach (var zone in project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "zone"); Append(builder, zone.Id); Append(builder, zone.Name); }
            foreach (var floor in project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "floor"); Append(builder, floor.Id); Append(builder, floor.Name); Append(builder, floor.ElevationM.ToString("R", CultureInfo.InvariantCulture)); }
            foreach (var family in project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { Append(builder, "family"); Append(builder, family.Id); Append(builder, family.Name); Append(builder, family.Category.ToString()); AppendPairs(builder, family.Properties); }
            foreach (var element in project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "element"); Append(builder, element.Id); Append(builder, element.Category.ToString()); Append(builder, element.FamilyId); Append(builder, element.FloorId);
                Append(builder, element.ZoneId); Append(builder, element.DrawingFingerprint); Append(builder, element.Dirty.ToString()); Append(builder, element.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
                foreach (var handle in element.SourceHandles) { Append(builder, "source"); Append(builder, handle); }
                foreach (var dependency in element.DependsOn) { Append(builder, "depends"); Append(builder, dependency); }
                AppendPairs(builder, element.Properties);
                foreach (var pair in element.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) { Append(builder, pair.Key); Append(builder, pair.Value.ToString("R", CultureInfo.InvariantCulture)); }
            }
            foreach (var audit in project.AuditEvents) { Append(builder, "audit"); Append(builder, audit.Utc.ToString("O", CultureInfo.InvariantCulture)); Append(builder, audit.Action); Append(builder, audit.ElementId); Append(builder, audit.Detail); Append(builder, audit.Actor); Append(builder, audit.CorrelationId); }
            return Sha256(builder.ToString());
        }

        private static void AppendPairs(StringBuilder builder, IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var pair in pairs.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) { Append(builder, pair.Key); Append(builder, pair.Value); }
        }

        private static void AppendPoint(StringBuilder builder, Point3d point)
        {
            Append(builder, point.X.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Y.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Z.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void Append(StringBuilder builder, string? value)
        {
            var normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized).Append('|');
        }

        private static string Sha256(string value)
        {
            using (var algorithm = SHA256.Create())
                return string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static IReadOnlyList<string> SplitProperty(ProjectElement owner, string key) => RequiredProperty(owner, key).Split(new[] { ';' }, StringSplitOptions.None);
        private static string RequiredProperty(ProjectElement owner, string key) => owner.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException("P07 required metadata is missing.");
        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles, string label) => handles.Select(x => CanonicalHandle(x, label)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        private static string CanonicalHandle(string? handle, string label) => CadHandleService.NormalizeHexHandle(handle) ?? throw new InvalidOperationException(label + " is invalid.");
        private static void RequireLegacyNoLevel(ProjectElement element) { if (CadVerticalPlacementResolver.HasConfiguredLevel(element)) throw new InvalidOperationException("P07 requires legacy/no-Level placement."); }
        private static double ToMeters(Document document, double value) => CadGeometryGuard.ToMeters(document, value, "P07 drawing conversion");
        private static bool Near(double left, double right) => Math.Abs(left - right) <= 1e-6d;

        private static void RequireAutomation(string? requestedPath, string nonce)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("P07 runtime commands are automation-only.");
            RequiredResultPath(requestedPath!);
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("P07 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("P07 result directory must already exist.");
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
                if (normalized.Length > 0 && !File.Exists(normalized) && Guid.TryParseExact(nonce, "N", out _) && FailurePhases.Contains(phase) && FailureCodes.Contains(failureCode))
                    WriteMarkerAtomic(normalized, new[] { "status=FAIL", "command=QS3DCURTAINP07PROBE", "nonce=" + nonce, "schema=QS3D_CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_V1", "qualification_boundary=LOCAL_002_P07_ONLY", "production_local002_qualified=false", "error_code=CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_FAILED", "failure_phase=" + phase, "failure_code=" + failureCode });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("P07 result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush(); stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally { try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }

        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
