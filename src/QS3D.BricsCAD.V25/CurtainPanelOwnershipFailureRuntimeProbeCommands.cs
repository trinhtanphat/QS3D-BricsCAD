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
    /// Automation-only LOCAL-002/P06 probe. It corrupts only synthetic panel
    /// ownership state, invokes production QS3DCURTAIN3D for every attempt,
    /// and proves that destructive replacement is fail-closed.
    /// </summary>
    public sealed class CurtainPanelOwnershipFailureRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_OWNERSHIP_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_OWNERSHIP_NONCE";
        private const string ResultFileName = "curtain-panel-ownership-failure-runtime-result.txt";
        private const string CrossOwnerSlot = "GeneratedRebarHandles";
        private const int OwnerCount = 6;

        private enum SequenceStage
        {
            None,
            Prepared,
            Baseline,
            MissingReady,
            MissingVerified,
            DuplicateReady,
            DuplicateVerified,
            ForeignReady,
            ForeignVerified,
            CrossReady,
            CrossVerified,
            CrossCleared,
            ValidReady,
            Complete
        }

        private sealed class OwnerState
        {
            public string ElementId { get; set; } = string.Empty;
            public string SourceHandle { get; set; } = string.Empty;
            public IReadOnlyList<string> PanelHandles { get; set; } = Array.Empty<string>();
            public string Digest { get; set; } = string.Empty;
        }

        private sealed class AttemptState
        {
            public int Lane { get; set; }
            public string ProjectDigest { get; set; } = string.Empty;
            public string OwnerDigest { get; set; } = string.Empty;
            public IReadOnlyList<string> CurrentSpaceHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> OriginalPanelHandles { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> LiveOriginalPanelHandles { get; set; } = Array.Empty<string>();
            public string SpecialHandle { get; set; } = string.Empty;
        }

        private sealed class SequenceState
        {
            public string Nonce { get; set; } = string.Empty;
            public SequenceStage Stage { get; set; }
            public Dictionary<int, string> ElementIds { get; } = new Dictionary<int, string>();
            public Dictionary<int, OwnerState> BaselineOwners { get; } = new Dictionary<int, OwnerState>();
            public Dictionary<int, AttemptState> VerifiedAttempts { get; } = new Dictionary<int, AttemptState>();
            public AttemptState? PendingAttempt { get; set; }
            public int BaselinePanelCount { get; set; }
            public int RefusalCount { get; set; }
            public IReadOnlyList<string> ValidOldPanels { get; set; } = Array.Empty<string>();
        }

        private static readonly object StateSync = new object();
        private static SequenceState? State;

        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH",
            "SEED_HOSTS",
            "PREPARE_BASELINE",
            "VERIFY_BASELINE",
            "CORRUPT_MISSING",
            "VERIFY_MISSING_REFUSAL",
            "CORRUPT_DUPLICATE",
            "VERIFY_DUPLICATE_REFUSAL",
            "CORRUPT_FOREIGN",
            "VERIFY_FOREIGN_REFUSAL",
            "CORRUPT_CROSS_OWNER",
            "VERIFY_CROSS_OWNER_REFUSAL",
            "CLEAR_CROSS_OWNER",
            "PREPARE_VALID_REPLACEMENT",
            "VERIFY_VALID_REPLACEMENT",
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

        [CommandMethod("QS3DCURTAINP06SEED", CommandFlags.Modal)]
        public void SeedHosts() => ExecuteStage("SEED_HOSTS", (document, _, nonce) =>
        {
            var ids = new List<ObjectId>(OwnerCount);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                for (var lane = 0; lane < OwnerCount; lane++)
                    ids.Add(AppendLine(document, transaction, modelSpace, 0d, lane * 10d, 5d, lane * 10d, "P06 host"));
                transaction.Commit();
            }
            lock (StateSync) State = new SequenceState { Nonce = nonce, Stage = SequenceStage.None };
            document.Editor.SetImpliedSelection(ids.ToArray());
        });

        [CommandMethod("QS3DCURTAINP06PREPARE", CommandFlags.Modal)]
        public void PrepareBaseline() => ExecuteStage("PREPARE_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.None);
            var hosts = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            if (hosts.Count != OwnerCount)
                throw new InvalidOperationException("P06 requires exactly six synthetic GlassWall owners.");

            var sourceIds = new List<ObjectId>(OwnerCount);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in hosts)
                {
                    RequireLegacyNoLevel(host);
                    var sourceId = ResolveSingleSource(document, host);
                    var line = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Line
                        ?? throw new InvalidOperationException("P06 source is not a LINE.");
                    RequireNear(ToMeters(document, line.StartPoint.X), 0d, "P06 start X");
                    RequireNear(ToMeters(document, line.EndPoint.X), 5d, "P06 end X");
                    RequireNear(ToMeters(document, line.EndPoint.Y), ToMeters(document, line.StartPoint.Y), "P06 horizontal Y");
                    RequireNear(ToMeters(document, line.EndPoint.Z - line.StartPoint.Z), 0d, "P06 horizontal Z");
                    var laneValue = ToMeters(document, line.StartPoint.Y) / 10d;
                    var lane = checked((int)Math.Round(laneValue, MidpointRounding.AwayFromZero));
                    if (lane < 0 || lane >= OwnerCount || Math.Abs(laneValue - lane) > 1e-6d || state.ElementIds.ContainsKey(lane))
                        throw new InvalidOperationException("P06 synthetic owner lane classification failed.");
                    state.ElementIds.Add(lane, host.Id);
                    sourceIds.Add(sourceId);
                }
                transaction.Commit();
            }
            if (state.ElementIds.Count != OwnerCount) throw new InvalidOperationException("P06 owner map is incomplete.");
            state.Stage = SequenceStage.Prepared;
            document.Editor.SetImpliedSelection(sourceIds.ToArray());
        });

        [CommandMethod("QS3DCURTAINP06BASELINE", CommandFlags.Modal)]
        public void VerifyBaseline() => ExecuteStage("VERIFY_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Prepared);
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var lane = 0; lane < OwnerCount; lane++)
            {
                var owner = CaptureCleanOwner(document, project, RequiredOwner(project, state, lane));
                state.BaselineOwners.Add(lane, owner);
                state.BaselinePanelCount = checked(state.BaselinePanelCount + owner.PanelHandles.Count);
                foreach (var handle in owner.PanelHandles)
                    if (!all.Add(handle)) throw new InvalidOperationException("P06 baseline panel ownership overlaps.");
            }
            if (state.BaselinePanelCount <= OwnerCount)
                throw new InvalidOperationException("P06 baseline panel output is unexpectedly small.");
            state.Stage = SequenceStage.Baseline;
        });

        [CommandMethod("QS3DCURTAINP06MISSING", CommandFlags.Modal)]
        public void CorruptMissingHandle() => ExecuteStage("CORRUPT_MISSING", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Baseline);
            var owner = RequiredOwner(project, state, 0);
            var missing = state.BaselineOwners[0].PanelHandles[0];
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                MarkForDifferentReplacement(owner, "0.83");
                project.Touch();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var id = RequireSingleResolved(document, missing, "P06 missing panel");
                    var solid = transaction.GetObject(id, OpenMode.ForWrite, false) as Solid3d
                        ?? throw new InvalidOperationException("P06 missing-case target is not a Solid3d.");
                    GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, owner, "prepare P06 missing-handle case");
                    solid.Erase();
                    transaction.Commit();
                }
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            state.PendingAttempt = CaptureAttempt(document, project, state, 0, missing);
            if (state.PendingAttempt.LiveOriginalPanelHandles.Count != state.BaselineOwners[0].PanelHandles.Count - 1)
                throw new InvalidOperationException("P06 missing-handle precondition is not exact.");
            SelectOwner(document, project, state, 0);
            state.Stage = SequenceStage.MissingReady;
        });

        [CommandMethod("QS3DCURTAINP06CHECKMISSING", CommandFlags.Modal)]
        public void VerifyMissingRefusal() => ExecuteStage("VERIFY_MISSING_REFUSAL", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.MissingReady);
            var attempt = VerifyAttemptUnchanged(document, project, state, 0);
            if (IsLiveSolid(document, attempt.SpecialHandle))
                throw new InvalidOperationException("P06 missing panel unexpectedly became live.");
            CompleteAttempt(state, attempt, SequenceStage.MissingVerified);
        });

        [CommandMethod("QS3DCURTAINP06DUPLICATE", CommandFlags.Modal)]
        public void CorruptDuplicateHandle() => ExecuteStage("CORRUPT_DUPLICATE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.MissingVerified);
            var owner = RequiredOwner(project, state, 1);
            var canonical = state.BaselineOwners[1].PanelHandles[0];
            var alias = "0" + canonical;
            if (!string.Equals(CadHandleService.NormalizeHexHandle(alias), canonical, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P06 duplicate alias is not numerically equivalent.");
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                MarkForDifferentReplacement(owner, "0.82");
                owner.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = RequiredProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey) + ";" + alias;
                project.Touch();
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            state.PendingAttempt = CaptureAttempt(document, project, state, 1, alias);
            SelectOwner(document, project, state, 1);
            state.Stage = SequenceStage.DuplicateReady;
        });

        [CommandMethod("QS3DCURTAINP06CHECKDUPLICATE", CommandFlags.Modal)]
        public void VerifyDuplicateRefusal() => ExecuteStage("VERIFY_DUPLICATE_REFUSAL", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.DuplicateReady);
            var attempt = VerifyAttemptUnchanged(document, project, state, 1);
            var raw = RequiredProperty(RequiredOwner(project, state, 1), GeneratedCurtainPanelHealthService.HandlesKey);
            if (!raw.EndsWith(";" + attempt.SpecialHandle, StringComparison.Ordinal))
                throw new InvalidOperationException("P06 duplicate alias metadata was not preserved.");
            RequireAllLive(document, attempt.OriginalPanelHandles, "P06 duplicate originals");
            CompleteAttempt(state, attempt, SequenceStage.DuplicateVerified);
        });

        [CommandMethod("QS3DCURTAINP06FOREIGN", CommandFlags.Modal)]
        public void CorruptForeignHandle() => ExecuteStage("CORRUPT_FOREIGN", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.DuplicateVerified);
            var owner = RequiredOwner(project, state, 2);
            var rollback = ProjectStateSnapshot.Capture(project);
            string foreignHandle;
            try
            {
                MarkForDifferentReplacement(owner, "0.81");
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var foreign = new Solid3d();
                    try
                    {
                        foreign.SetDatabaseDefaults(document.Database);
                        foreign.CreateBox(
                            CadGeometryGuard.ToDrawingUnits(document, 0.25d, "P06 foreign width"),
                            CadGeometryGuard.ToDrawingUnits(document, 0.25d, "P06 foreign depth"),
                            CadGeometryGuard.ToDrawingUnits(document, 0.25d, "P06 foreign height"));
                        foreign.TransformBy(Matrix3d.Displacement(new Vector3d(
                            CadGeometryGuard.ToDrawingUnits(document, 20d, "P06 foreign X"),
                            CadGeometryGuard.ToDrawingUnits(document, 80d, "P06 foreign Y"),
                            CadGeometryGuard.ToDrawingUnits(document, 1d, "P06 foreign Z"))));
                        modelSpace.AppendEntity(foreign);
                        transaction.AddNewlyCreatedDBObject(foreign, true);
                        foreignHandle = CanonicalHandle(foreign.Handle.ToString(), "P06 foreign handle");
                        foreign = null!;
                    }
                    finally { foreign?.Dispose(); }

                    var handles = state.BaselineOwners[2].PanelHandles.ToList();
                    handles[0] = foreignHandle;
                    owner.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = string.Join(";", handles);
                    project.Touch();
                    transaction.Commit();
                }
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            state.PendingAttempt = CaptureAttempt(document, project, state, 2, foreignHandle);
            RequireForeignUnmarked(document, project, owner, foreignHandle);
            SelectOwner(document, project, state, 2);
            state.Stage = SequenceStage.ForeignReady;
        });

        [CommandMethod("QS3DCURTAINP06CHECKFOREIGN", CommandFlags.Modal)]
        public void VerifyForeignRefusal() => ExecuteStage("VERIFY_FOREIGN_REFUSAL", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.ForeignReady);
            var attempt = VerifyAttemptUnchanged(document, project, state, 2);
            RequireForeignUnmarked(document, project, RequiredOwner(project, state, 2), attempt.SpecialHandle);
            RequireAllLive(document, attempt.OriginalPanelHandles, "P06 orphaned original panels");
            CompleteAttempt(state, attempt, SequenceStage.ForeignVerified);
        });

        [CommandMethod("QS3DCURTAINP06CROSS", CommandFlags.Modal)]
        public void CorruptCrossOwner() => ExecuteStage("CORRUPT_CROSS_OWNER", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.ForeignVerified);
            var owner = RequiredOwner(project, state, 3);
            var claimant = RequiredOwner(project, state, 4);
            var claimedHandle = state.BaselineOwners[3].PanelHandles[0];
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                MarkForDifferentReplacement(owner, "0.79");
                if (claimant.Properties.ContainsKey(CrossOwnerSlot))
                    throw new InvalidOperationException("P06 cross-owner claimant is not clean.");
                claimant.Properties[CrossOwnerSlot] = claimedHandle;
                project.Touch();
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            state.PendingAttempt = CaptureAttempt(document, project, state, 3, claimedHandle);
            SelectOwner(document, project, state, 3);
            state.Stage = SequenceStage.CrossReady;
        });

        [CommandMethod("QS3DCURTAINP06CHECKCROSS", CommandFlags.Modal)]
        public void VerifyCrossOwnerRefusal() => ExecuteStage("VERIFY_CROSS_OWNER_REFUSAL", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.CrossReady);
            var attempt = VerifyAttemptUnchanged(document, project, state, 3);
            var claimant = RequiredOwner(project, state, 4);
            if (!claimant.Properties.TryGetValue(CrossOwnerSlot, out var raw) ||
                !string.Equals(CanonicalHandle(raw, "P06 cross-owner claim"), attempt.SpecialHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P06 cross-owner claim was not preserved.");
            RequireAllLive(document, attempt.OriginalPanelHandles, "P06 cross-owner originals");
            CompleteAttempt(state, attempt, SequenceStage.CrossVerified);
        });

        [CommandMethod("QS3DCURTAINP06CLEARCROSS", CommandFlags.Modal)]
        public void ClearCrossOwner() => ExecuteStage("CLEAR_CROSS_OWNER", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.CrossVerified);
            var claimant = RequiredOwner(project, state, 4);
            if (!claimant.Properties.Remove(CrossOwnerSlot))
                throw new InvalidOperationException("P06 cross-owner claim is already absent.");
            project.Touch();
            if (!string.Equals(CaptureOwnerDigest(claimant), state.BaselineOwners[4].Digest, StringComparison.Ordinal))
                throw new InvalidOperationException("P06 claimant did not return to its baseline semantic state.");
            RequireOwnerPanelsLive(document, project, claimant, state.BaselineOwners[4].PanelHandles);
            state.Stage = SequenceStage.CrossCleared;
        });

        [CommandMethod("QS3DCURTAINP06VALID", CommandFlags.Modal)]
        public void PrepareValidReplacement() => ExecuteStage("PREPARE_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.CrossCleared);
            var owner = RequiredOwner(project, state, 5);
            state.ValidOldPanels = state.BaselineOwners[5].PanelHandles;
            MarkForDifferentReplacement(owner, "0.77");
            project.Touch();
            SelectOwner(document, project, state, 5);
            state.Stage = SequenceStage.ValidReady;
        });

        [CommandMethod("QS3DCURTAINP06PROBE", CommandFlags.Modal)]
        public void VerifyValidReplacement() => ExecuteStage("VERIFY_VALID_REPLACEMENT", (document, project, nonce) =>
        {
            var resultPath = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable)!);
            if (File.Exists(resultPath)) throw new IOException("P06 result already exists.");
            var state = RequireState(nonce, SequenceStage.ValidReady);
            if (state.RefusalCount != 4 || state.VerifiedAttempts.Count != 4)
                throw new InvalidOperationException("P06 refusal matrix is incomplete.");

            var validOwner = RequiredOwner(project, state, 5);
            var next = CaptureCleanOwner(document, project, validOwner);
            if (next.PanelHandles.Intersect(state.ValidOldPanels, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P06 valid replacement reused an old panel handle.");
            if (CadHandleService.Resolve(document, state.ValidOldPanels).Count != 0)
                throw new InvalidOperationException("P06 valid replacement left an old panel live.");

            for (var lane = 0; lane < 4; lane++)
            {
                var attempt = state.VerifiedAttempts[lane];
                var owner = RequiredOwner(project, state, lane);
                if (!string.Equals(CaptureOwnerDigest(owner), attempt.OwnerDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("P06 refused owner semantic metadata changed later in the matrix.");
            }
            var missing = state.VerifiedAttempts[0];
            if (IsLiveSolid(document, missing.SpecialHandle)) throw new InvalidOperationException("P06 missing panel reappeared.");
            RequireAllLive(document, missing.LiveOriginalPanelHandles, "P06 missing-case survivors");
            RequireAllLive(document, state.VerifiedAttempts[1].OriginalPanelHandles, "P06 duplicate-case originals");
            RequireAllLive(document, state.VerifiedAttempts[2].OriginalPanelHandles, "P06 foreign-case originals");
            RequireAllLive(document, state.VerifiedAttempts[3].OriginalPanelHandles, "P06 cross-owner originals");
            RequireForeignUnmarked(document, project, RequiredOwner(project, state, 2), state.VerifiedAttempts[2].SpecialHandle);
            RequireOwnerPanelsLive(document, project, RequiredOwner(project, state, 4), state.BaselineOwners[4].PanelHandles);

            state.Stage = SequenceStage.Complete;
            WriteMarkerAtomic(resultPath, new[]
            {
                "status=PASS",
                "command=QS3DCURTAINP06PROBE",
                "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + nonce,
                "schema=QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1",
                "qualification_boundary=LOCAL_002_P06_ONLY",
                "production_local002_qualified=false",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                "legacy_no_level=true",
                "glass_wall_count=6",
                "refusal_count=4",
                "missing_handle_refused=true",
                "duplicate_canonical_refused=true",
                "foreign_unmarked_refused=true",
                "cross_owner_refused=true",
                "no_erase_append_verified=true",
                "surviving_old_sets_preserved=true",
                "semantic_metadata_preserved=true",
                "unrelated_owners_preserved=true",
                "foreign_object_preserved=true",
                "valid_replacement_succeeded=true",
                "valid_old_set_removed=true",
                "valid_new_set_complete=true",
                "baseline_panel_count=" + state.BaselinePanelCount.ToString(CultureInfo.InvariantCulture),
                "valid_replacement_panel_count=" + next.PanelHandles.Count.ToString(CultureInfo.InvariantCulture),
                "health_issue_count=0"
            });
            document.Editor.WriteMessage("\nQS3D Curtain panel P06 ownership-failure probe PASS.");
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
                    "\nQS3D Curtain panel P06 probe stage failed. See the sanitized local result.");
                throw;
            }
        }

        private static SequenceState RequireState(string nonce, SequenceStage expected)
        {
            lock (StateSync)
            {
                if (State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) || State.Stage != expected)
                    throw new InvalidOperationException("P06 runtime command sequence is invalid.");
                return State;
            }
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

        private static ProjectElement RequiredOwner(ProjectState project, SequenceState state, int lane)
        {
            if (!state.ElementIds.TryGetValue(lane, out var id)) throw new InvalidOperationException("P06 owner lane is missing.");
            var owner = project.FindElement(id) ?? throw new InvalidOperationException("P06 semantic owner is missing.");
            if (owner.Category != ElementCategory.GlassWall) throw new InvalidOperationException("P06 owner category changed.");
            return owner;
        }

        private static void RequireLegacyNoLevel(ProjectElement element)
        {
            if (CadVerticalPlacementResolver.HasConfiguredLevel(element))
                throw new InvalidOperationException("P06 requires legacy/no-Level placement.");
        }

        private static OwnerState CaptureCleanOwner(Document document, ProjectState project, ProjectElement owner)
        {
            RequireLegacyNoLevel(owner);
            if (owner.IsGeneratedCurtainPanelStale()) throw new InvalidOperationException("P06 clean owner is stale.");
            if (!string.Equals(RequiredProperty(owner, "GeneratedCurtainPanelBuildState"), "Complete", StringComparison.Ordinal))
                throw new InvalidOperationException("P06 clean owner build state is not Complete.");
            var source = CanonicalHandles(owner.SourceHandles, "P06 source");
            var panels = CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P06 panels");
            if (source.Count != 1 || panels.Count == 0) throw new InvalidOperationException("P06 clean owner output is incomplete.");
            if (!int.TryParse(RequiredProperty(owner, "GeneratedCurtainPanelCount"), NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count != panels.Count)
                throw new InvalidOperationException("P06 panel count metadata is inconsistent.");
            RequireOwnerPanelsLive(document, project, owner, panels);
            var live = new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, panels), StringComparer.OrdinalIgnoreCase);
            var issues = new GeneratedCurtainPanelHealthService().Inspect(project, live)
                .Concat(CurtainWallPanelLiveStateService.Inspect(document, project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project))
                .Where(x => string.Equals(x.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase) && x.Severity != HealthSeverity.Info)
                .ToList();
            if (issues.Count != 0) throw new InvalidOperationException("P06 clean owner has blocking panel Health.");
            return new OwnerState
            {
                ElementId = owner.Id,
                SourceHandle = source[0],
                PanelHandles = panels,
                Digest = CaptureOwnerDigest(owner)
            };
        }

        private static AttemptState CaptureAttempt(Document document, ProjectState project, SequenceState state, int lane, string specialHandle)
        {
            var owner = RequiredOwner(project, state, lane);
            return new AttemptState
            {
                Lane = lane,
                ProjectDigest = CaptureProjectDigest(project),
                OwnerDigest = CaptureOwnerDigest(owner),
                CurrentSpaceHandles = CurrentSpaceHandles(document),
                OriginalPanelHandles = state.BaselineOwners[lane].PanelHandles,
                LiveOriginalPanelHandles = LiveSolidHandles(document, state.BaselineOwners[lane].PanelHandles),
                SpecialHandle = specialHandle
            };
        }

        private static AttemptState VerifyAttemptUnchanged(Document document, ProjectState project, SequenceState state, int lane)
        {
            var attempt = state.PendingAttempt ?? throw new InvalidOperationException("P06 attempt snapshot is missing.");
            if (attempt.Lane != lane) throw new InvalidOperationException("P06 attempt lane changed.");
            if (!string.Equals(CaptureProjectDigest(project), attempt.ProjectDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P06 production refusal changed semantic project state.");
            if (!CurrentSpaceHandles(document).SequenceEqual(attempt.CurrentSpaceHandles, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("P06 production refusal erased or appended a native entity.");
            if (!string.Equals(CaptureOwnerDigest(RequiredOwner(project, state, lane)), attempt.OwnerDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P06 production refusal changed owner metadata.");
            if (!LiveSolidHandles(document, attempt.OriginalPanelHandles).SequenceEqual(attempt.LiveOriginalPanelHandles, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("P06 production refusal changed the surviving old panel set.");
            return attempt;
        }

        private static void CompleteAttempt(SequenceState state, AttemptState attempt, SequenceStage next)
        {
            state.VerifiedAttempts.Add(attempt.Lane, attempt);
            state.PendingAttempt = null;
            state.RefusalCount++;
            state.Stage = next;
        }

        private static void MarkForDifferentReplacement(ProjectElement owner, string maxWidthM)
        {
            owner.SetProperty("CurtainMaxPanelWidthM", maxWidthM);
            if (!owner.IsGeneratedCurtainPanelStale())
                throw new InvalidOperationException("P06 replacement precondition did not mark panel output stale.");
        }

        private static void SelectOwner(Document document, ProjectState project, SequenceState state, int lane) =>
            document.Editor.SetImpliedSelection(new[] { ResolveSingleSource(document, RequiredOwner(project, state, lane)) });

        private static ObjectId ResolveSingleSource(Document document, ProjectElement owner)
        {
            var handles = CanonicalHandles(owner.SourceHandles, "P06 source");
            var ids = CadHandleService.Resolve(document, handles);
            if (handles.Count != 1 || ids.Count != 1) throw new InvalidOperationException("P06 owner requires one live source.");
            return ids[0];
        }

        private static ObjectId RequireSingleResolved(Document document, string handle, string label)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException(label + " did not resolve exactly once.");
            return ids[0];
        }

        private static void RequireOwnerPanelsLive(Document document, ProjectState project, ProjectElement owner, IReadOnlyList<string> handles)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count) throw new InvalidOperationException("P06 owned panel resolution is incomplete.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d
                        ?? throw new InvalidOperationException("P06 owned panel is not a Solid3d.");
                    GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, owner, "inspect P06 owned panel");
                }
                transaction.Commit();
            }
        }

        private static void RequireForeignUnmarked(Document document, ProjectState project, ProjectElement owner, string handle)
        {
            var id = RequireSingleResolved(document, handle, "P06 foreign solid");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException("P06 foreign object is not a Solid3d.");
                if (GeneratedCurtainPanelNativeOwnershipService.HasMatchingOwnership(solid, project, owner))
                    throw new InvalidOperationException("P06 foreign object unexpectedly has matching panel ownership.");
                transaction.Commit();
            }
        }

        private static bool IsLiveSolid(Document document, string handle) =>
            CadHandleService.GetLiveSolidHandles(document, new[] { handle }).Count == 1;

        private static void RequireAllLive(Document document, IReadOnlyList<string> handles, string label)
        {
            if (CadHandleService.GetLiveSolidHandles(document, handles).Count != handles.Count)
                throw new InvalidOperationException(label + " are not all live.");
        }

        private static IReadOnlyList<string> LiveSolidHandles(Document document, IReadOnlyList<string> handles) =>
            CadHandleService.GetLiveSolidHandles(document, handles)
                .Select(x => CanonicalHandle(x, "P06 live panel"))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        private static IReadOnlyList<string> CurrentSpaceHandles(Document document)
        {
            var result = new List<string>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = (BlockTableRecord)transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead);
                foreach (var id in space)
                {
                    if (id.IsNull || id.IsErased) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity != null && !entity.IsErased) result.Add(CanonicalHandle(entity.Handle.ToString(), "P06 current-space entity"));
                }
                transaction.Commit();
            }
            return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static string CaptureProjectDigest(ProjectState project)
        {
            var builder = new StringBuilder();
            Append(builder, project.ProjectId);
            Append(builder, project.Name);
            Append(builder, project.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.ChangeVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            Append(builder, project.DrawingPath);
            Append(builder, project.DrawingFingerprint);
            Append(builder, project.ActiveFloorId);
            Append(builder, project.ActiveZoneId);
            AppendPairs(builder, project.Metadata);
            foreach (var zone in project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "zone"); Append(builder, zone.Id); Append(builder, zone.Name);
            }
            foreach (var floor in project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "floor"); Append(builder, floor.Id); Append(builder, floor.Name); Append(builder, floor.ElevationM.ToString("R", CultureInfo.InvariantCulture));
            }
            foreach (var family in project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "family"); Append(builder, family.Id); Append(builder, family.Name); Append(builder, family.Category.ToString()); AppendPairs(builder, family.Properties);
            }
            foreach (var element in project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, "element"); Append(builder, CaptureOwnerPayload(element));
            }
            foreach (var audit in project.AuditEvents)
            {
                Append(builder, "audit"); Append(builder, audit.Utc.ToString("O", CultureInfo.InvariantCulture)); Append(builder, audit.Action);
                Append(builder, audit.ElementId); Append(builder, audit.Detail); Append(builder, audit.Actor); Append(builder, audit.CorrelationId);
            }
            return Sha256(builder.ToString());
        }

        private static string CaptureOwnerDigest(ProjectElement owner) => Sha256(CaptureOwnerPayload(owner));

        private static string CaptureOwnerPayload(ProjectElement owner)
        {
            var builder = new StringBuilder();
            Append(builder, owner.Id); Append(builder, owner.Category.ToString()); Append(builder, owner.FamilyId); Append(builder, owner.FloorId);
            Append(builder, owner.ZoneId); Append(builder, owner.DrawingFingerprint); Append(builder, owner.Dirty.ToString());
            Append(builder, owner.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            foreach (var handle in owner.SourceHandles) { Append(builder, "source"); Append(builder, handle); }
            foreach (var dependency in owner.DependsOn) { Append(builder, "depends"); Append(builder, dependency); }
            AppendPairs(builder, owner.Properties);
            foreach (var pair in owner.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, pair.Key); Append(builder, pair.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static void AppendPairs(StringBuilder builder, IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var pair in pairs.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, pair.Key); Append(builder, pair.Value);
            }
        }

        private static void Append(StringBuilder builder, string? value)
        {
            var normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized).Append('|');
        }

        private static string Sha256(string value)
        {
            using (var algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
                return string.Concat(hash.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static IReadOnlyList<string> SplitProperty(ProjectElement owner, string key) =>
            RequiredProperty(owner, key).Split(new[] { ';' }, StringSplitOptions.None);

        private static string RequiredProperty(ProjectElement owner, string key)
        {
            if (!owner.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("P06 required semantic property is missing.");
            return value;
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles, string label) =>
            handles.Select(x => CanonicalHandle(x, label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        private static string CanonicalHandle(string? handle, string label) =>
            CadHandleService.NormalizeHexHandle(handle) ?? throw new InvalidDataException(label + " is invalid.");

        private static double ToMeters(Document document, double value) =>
            CadGeometryGuard.ToMeters(document, value, "P06 measurement");

        private static void RequireNear(double actual, double expected, string label)
        {
            var scale = Math.Max(1d, Math.Max(Math.Abs(actual), Math.Abs(expected)));
            if (Math.Abs(actual - expected) > 1e-6d * scale)
                throw new InvalidOperationException(label + " differs from the synthetic fixture.");
        }

        private static void RequireAutomation(string? requestedPath, string nonce)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("P06 runtime commands are automation-only.");
            RequiredResultPath(requestedPath!);
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P06 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("P06 result directory must already exist.");
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
                        "command=QS3DCURTAINP06PROBE",
                        "nonce=" + nonce,
                        "schema=QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1",
                        "qualification_boundary=LOCAL_002_P06_ONLY",
                        "production_local002_qualified=false",
                        "error_code=CURTAIN_PANEL_OWNERSHIP_RUNTIME_FAILED",
                        "failure_phase=" + phase,
                        "failure_code=" + failureCode
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("P06 result already exists.");
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
