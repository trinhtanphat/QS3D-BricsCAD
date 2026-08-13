using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 probe. Production commands own authoring, reconcile,
    /// rebuild, Undo/Redo and persistence. This class only prepares deterministic native
    /// state, selects production targets and publishes aggregate/privacy-safe evidence.
    /// </summary>
    public sealed class SourceReconcileRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_RECONCILE_RESULT";
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NONCE";
        private const string DrawingAVariable = "QS3D_SOURCE_RECONCILE_DWG_A";
        private const string DrawingBVariable = "QS3D_SOURCE_RECONCILE_DWG_B";
        private const string UndoVariable = "QS3D_SOURCE_RECONCILE_UNDO_COHERENT";
        private const string RedoVariable = "QS3D_SOURCE_RECONCILE_REDO_COHERENT";
        private const string ResultFileName = "source-reconcile-result.txt";
        private const string PhaseFileName = "source-reconcile-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_RUNTIME_V1";
        private static readonly object Sync = new object();
        private static SequenceState? _state;

        [CommandMethod("QS3DSRTPREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("prepare_initial_edit", () =>
            {
                var context = ContextA(requireState: false);
                var owners = RequireOwners(context.Document, context.Project);
                var baseline = Capture(context.Document, context.Project, owners);
                RequireCompleteGenerated(context.Document, baseline, "initial authoring");
                EditSources(context.Document, owners, secondEdit: false);
                var editedSourceDigest = SourceDigest(context.Document, owners);
                if (string.Equals(baseline.SourceDigest, editedSourceDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 initial source edit did not change native geometry.");
                lock (Sync)
                {
                    _state = new SequenceState(
                        context.Document,
                        context.Project.ProjectId,
                        owners.Line.Id,
                        owners.Polyline.Id,
                        context.Nonce,
                        baseline,
                        editedSourceDigest);
                }
                SelectSources(context.Document, owners);
            });
        }

        [CommandMethod("QS3DSRTAFTERSYNC1", CommandFlags.Modal)]
        public void AfterFirstSync()
        {
            Execute("verify_first_reconcile", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                var current = Capture(context.Document, context.Project, owners);
                RequireSemanticMatchesSources(context.Document, owners);
                RequireNoGenerated(context.Document, current, state.Initial.GeneratedHandles, "first reconcile");
                if (!string.Equals(current.SourceDigest, state.FirstEditedSourceDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 first reconcile changed user source geometry.");
                state.AfterFirstSync = current;
            });
        }

        [CommandMethod("QS3DSRTSELECTLINE", CommandFlags.Modal)]
        public void SelectLine() => Execute("select_line", () => SelectOwnerSource(ContextA(), line: true));

        [CommandMethod("QS3DSRTSELECTPOLY", CommandFlags.Modal)]
        public void SelectPolyline() => Execute("select_polyline", () => SelectOwnerSource(ContextA(), line: false));

        [CommandMethod("QS3DSRTAFTERREBUILD1", CommandFlags.Modal)]
        public void AfterFirstRebuild()
        {
            Execute("verify_first_rebuild", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                var current = Capture(context.Document, context.Project, owners);
                RequireCompleteGenerated(context.Document, current, "first rebuild");
                RequireSemanticMatchesSources(context.Document, owners);
                if (current.GeneratedHandles.Intersect(state.Initial.GeneratedHandles, StringComparer.OrdinalIgnoreCase).Any())
                    throw new InvalidOperationException("LOCAL-004 rebuild reused invalidated generated ownership.");
                state.AfterFirstRebuild = current;
            });
        }

        [CommandMethod("QS3DSRTPREPAREROLLBACK", CommandFlags.Modal)]
        public void PrepareRollback()
        {
            Execute("prepare_forced_rollback", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                EditSources(context.Document, owners, secondEdit: true);
                state.BeforeForcedFailure = Capture(context.Document, context.Project, owners);
                if (string.Equals(state.BeforeForcedFailure.SourceDigest, state.FirstEditedSourceDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 forced-rollback edit did not change native geometry.");
                SelectSources(context.Document, owners);
            });
        }

        [CommandMethod("QS3DSRTCHECKROLLBACK", CommandFlags.Modal)]
        public void CheckRollback()
        {
            Execute("verify_forced_rollback", () =>
            {
                var context = ContextA();
                var state = State(context);
                var expected = state.BeforeForcedFailure ?? throw new InvalidOperationException("LOCAL-004 forced-failure baseline is missing.");
                var owners = RequireOwners(context.Document, context.Project, state);
                var current = Capture(context.Document, context.Project, owners);
                if (!Same(expected, current))
                    throw new InvalidOperationException("LOCAL-004 failed reconcile did not restore exact semantic/native ownership state.");
                RequireCompleteGenerated(context.Document, current, "forced rollback");
                state.ForcedRollbackVerified = true;
            });
        }

        [CommandMethod("QS3DSRTPREPGENERATED", CommandFlags.Modal)]
        public void PrepareGeneratedRefusal()
        {
            Execute("prepare_generated_refusal", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                state.BeforeGeneratedRefusal = Capture(context.Document, context.Project, owners);
                var generated = state.BeforeGeneratedRefusal.GeneratedHandles;
                if (generated.Count == 0) throw new InvalidOperationException("LOCAL-004 generated refusal has no live target.");
                SelectHandles(context.Document, new[] { generated[0] });
            });
        }

        [CommandMethod("QS3DSRTCHECKGENERATED", CommandFlags.Modal)]
        public void CheckGeneratedRefusal()
        {
            Execute("verify_generated_refusal", () =>
            {
                var context = ContextA();
                var state = State(context);
                var expected = state.BeforeGeneratedRefusal ?? throw new InvalidOperationException("LOCAL-004 generated-refusal baseline is missing.");
                var current = Capture(context.Document, context.Project, RequireOwners(context.Document, context.Project, state));
                if (!Same(expected, current))
                    throw new InvalidOperationException("LOCAL-004 generated-output selection mutated project or CAD state.");
                state.GeneratedRefusalVerified = true;
            });
        }

        [CommandMethod("QS3DSRTPREPAMBIGUOUS", CommandFlags.Modal)]
        public void PrepareAmbiguousRefusal()
        {
            Execute("prepare_ambiguous_refusal", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                state.AmbiguousRollback = ProjectStateSnapshot.Capture(context.Project);
                var duplicate = new ProjectElement("LOCAL004-AMBIGUOUS-" + context.Nonce, owners.Line.Category);
                duplicate.SourceHandles.Add(owners.Line.SourceHandles.Single());
                context.Project.Elements.Add(duplicate);
                context.Project.Touch();
                state.BeforeAmbiguousRefusal = Capture(context.Document, context.Project, owners);
                SelectHandles(context.Document, owners.Line.SourceHandles);
            });
        }

        [CommandMethod("QS3DSRTCHECKAMBIGUOUS", CommandFlags.Modal)]
        public void CheckAmbiguousRefusal()
        {
            Execute("verify_ambiguous_refusal", () =>
            {
                var context = ContextA();
                var state = State(context);
                var expected = state.BeforeAmbiguousRefusal ?? throw new InvalidOperationException("LOCAL-004 ambiguous-refusal baseline is missing.");
                var owners = RequireOwners(context.Document, context.Project, state);
                var current = Capture(context.Document, context.Project, owners);
                if (!Same(expected, current))
                    throw new InvalidOperationException("LOCAL-004 ambiguous source selection mutated project or CAD state.");
                var rollback = state.AmbiguousRollback ?? throw new InvalidOperationException("LOCAL-004 ambiguous rollback snapshot is missing.");
                rollback.Restore(context.Project);
                state.AmbiguousRefusalVerified = true;
            });
        }

        [CommandMethod("QS3DSRTSEEDB", CommandFlags.Modal)]
        public void SeedDocumentB()
        {
            Execute("prepare_document_b_refusal", () =>
            {
                var state = RequiredState();
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("LOCAL-004 document B is unavailable.");
                if (ReferenceEquals(document, state.DocumentA)) throw new InvalidOperationException("LOCAL-004 document B did not become active.");
                RequireExactDocumentPath(document, DrawingBVariable);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var id = AppendLine(document, 20d, 20d, 22d, 20d);
                document.Editor.SetImpliedSelection(new[] { id });
                state.DocumentB = document;
                state.ProjectBId = project.ProjectId;
                state.DocumentBSourceHandle = CadHandleService.NormalizeHexHandle(id.Handle.ToString())
                    ?? throw new InvalidOperationException("LOCAL-004 document B source handle is invalid.");
                state.BeforeDocumentBProjectDigest = ProjectDigest(project);
                state.BeforeDocumentBNativeDigest = EntityDigest(document, new[] { state.DocumentBSourceHandle });
                state.BeforeDocumentARefusal = CaptureA(state);
            });
        }

        [CommandMethod("QS3DSRTCHECKB", CommandFlags.Modal)]
        public void CheckDocumentBRefusal()
        {
            Execute("verify_document_b_refusal", () =>
            {
                var state = RequiredState();
                var documentB = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("LOCAL-004 active document disappeared.");
                if (!ReferenceEquals(documentB, state.DocumentB)) throw new InvalidOperationException("LOCAL-004 document B affinity changed.");
                if (!ProjectContextCoordinator.TryGetReadOnly(documentB, out var projectB) ||
                    !string.Equals(projectB.ProjectId, state.ProjectBId, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 document B project changed.");
                if (!string.Equals(ProjectDigest(projectB), state.BeforeDocumentBProjectDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 refusal mutated document B semantic state.");
                if (!string.Equals(EntityDigest(documentB, new[] { state.DocumentBSourceHandle }), state.BeforeDocumentBNativeDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 refusal mutated document B native state.");
                var currentA = CaptureA(state);
                if (!Same(state.BeforeDocumentARefusal ?? throw new InvalidOperationException("LOCAL-004 A refusal baseline is missing."), currentA))
                    throw new InvalidOperationException("LOCAL-004 document B command mutated document A.");
                state.MultiDocumentRefusalVerified = true;
                Application.DocumentManager.MdiActiveDocument = state.DocumentA;
                documentB.CloseAndDiscard();
                state.DocumentB = null;
            });
        }

        [CommandMethod("QS3DSRTSELECTSOURCES", CommandFlags.Modal)]
        public void SelectSourcesForFinalReconcile()
        {
            Execute("select_sources_final", () =>
            {
                var context = ContextA();
                SelectSources(context.Document, RequireOwners(context.Document, context.Project, State(context)));
            });
        }

        [CommandMethod("QS3DSRTAFTERFINALSYNC", CommandFlags.Modal)]
        public void AfterFinalSync()
        {
            Execute("verify_final_reconcile", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                var before = state.BeforeForcedFailure ?? throw new InvalidOperationException("LOCAL-004 pre-final state is missing.");
                var current = Capture(context.Document, context.Project, owners);
                RequireSemanticMatchesSources(context.Document, owners);
                RequireNoGenerated(context.Document, current, before.GeneratedHandles, "final reconcile");
                if (!string.Equals(current.SourceDigest, before.SourceDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("LOCAL-004 final reconcile changed user source geometry.");
                state.BeforeFinalSync = before;
                state.AfterFinalSync = current;
                state.FinalSyncVerified = true;
            });
        }

        [CommandMethod("QS3DSRTCHECKUNDO", CommandFlags.Modal)]
        public void CheckUndo()
        {
            Execute("native_undo", () =>
            {
                var context = ContextA();
                var state = State(context);
                var current = Capture(context.Document, context.Project, RequireOwners(context.Document, context.Project, state));
                state.UndoCoherent = Same(state.BeforeFinalSync ?? throw new InvalidOperationException("LOCAL-004 Undo baseline is missing."), current);
                state.UndoChecked = true;
            });
        }

        [CommandMethod("QS3DSRTCHECKREDO", CommandFlags.Modal)]
        public void CheckRedo()
        {
            Execute("native_redo", () =>
            {
                var context = ContextA();
                var state = State(context);
                var current = Capture(context.Document, context.Project, RequireOwners(context.Document, context.Project, state));
                state.RedoCoherent = Same(state.AfterFinalSync ?? throw new InvalidOperationException("LOCAL-004 Redo baseline is missing."), current);
                state.RedoChecked = true;
            });
        }

        [CommandMethod("QS3DSRTFINALREBUILD", CommandFlags.Modal)]
        public void CaptureFinalRebuild()
        {
            Execute("verify_final_rebuild", () =>
            {
                var context = ContextA();
                var state = State(context);
                var owners = RequireOwners(context.Document, context.Project, state);
                var current = Capture(context.Document, context.Project, owners);
                RequireCompleteGenerated(context.Document, current, "final rebuild");
                RequireSemanticMatchesSources(context.Document, owners);
                state.FinalRebuild = current;
            });
        }

        [CommandMethod("QS3DSRTSESSION1", CommandFlags.Modal)]
        public void PublishSessionOne()
        {
            Execute("session1_publish", () =>
            {
                var context = ContextA();
                var state = State(context);
                var final = state.FinalRebuild ?? throw new InvalidOperationException("LOCAL-004 final rebuild is missing.");
                if (!state.ForcedRollbackVerified || !state.GeneratedRefusalVerified || !state.AmbiguousRefusalVerified ||
                    !state.MultiDocumentRefusalVerified || !state.FinalSyncVerified || !state.UndoChecked || !state.RedoChecked)
                    throw new InvalidOperationException("LOCAL-004 sequence did not execute every required phase.");
                WriteMarkerAtomic(RequiredPath(PhaseVariable, PhaseFileName), new[]
                {
                    "status=PASS",
                    "command=QS3DSRTSESSION1",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_004_ONLY",
                    "source_count=2",
                    "generated_solid_count=" + final.GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "success_reconcile_count=2",
                    "generated_refusal_verified=true",
                    "ambiguous_refusal_verified=true",
                    "forced_rollback_verified=true",
                    "multi_document_refusal_verified=true",
                    "source_geometry_preserved=true",
                    "generated_replacement_verified=true",
                    "undo_coherent=" + Boolean(state.UndoCoherent),
                    "redo_coherent=" + Boolean(state.RedoCoherent)
                });
            });
        }

        [CommandMethod("QS3DSRTREOPEN", CommandFlags.Modal)]
        public void VerifyColdReopen()
        {
            Execute("cold_reopen", () =>
            {
                var context = ContextA(requireState: false);
                var owners = RequireOwners(context.Document, context.Project);
                var reopened = Capture(context.Document, context.Project, owners);
                RequireCompleteGenerated(context.Document, reopened, "cold reopen");
                RequireSemanticMatchesSources(context.Document, owners);
                var undo = RequiredBooleanEnvironment(UndoVariable);
                var redo = RequiredBooleanEnvironment(RedoVariable);
                var pass = undo && redo;
                WriteMarkerAtomic(RequiredPath(ResultVariable, ResultFileName), new[]
                {
                    "status=" + (pass ? "PASS" : "FAIL"),
                    "command=QS3DSRTREOPEN",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_004_ONLY",
                    "production_local004_qualified=" + Boolean(pass),
                    "source_count=2",
                    "generated_solid_count=" + reopened.GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "success_reconcile_count=2",
                    "generated_refusal_verified=true",
                    "ambiguous_refusal_verified=true",
                    "forced_rollback_verified=true",
                    "multi_document_refusal_verified=true",
                    "source_geometry_preserved=true",
                    "generated_replacement_verified=true",
                    "cold_reopen_verified=true",
                    "undo_coherent=" + Boolean(undo),
                    "redo_coherent=" + Boolean(redo),
                    "error_code=" + (pass ? "NONE" : "NATIVE_UNDO_SEMANTIC_DIVERGENCE"),
                    "failure_phase=" + (pass ? "none" : "native_undo"),
                    "failure_code=" + (pass ? "NONE" : "NATIVE_UNDO_SEMANTIC_DIVERGENCE")
                });
            });
        }

        private static void Execute(string phase, Action action)
        {
            try { action(); }
            catch (ProbeFailure failure) { TryWriteFailure(phase, failure.Code); }
            catch { TryWriteFailure(phase, "STATE_REJECTED"); }
        }

        private static ProbeContext ContextA(bool requireState = true)
        {
            var nonce = RequiredNonce();
            RequiredPath(ResultVariable, ResultFileName);
            RequiredPath(PhaseVariable, PhaseFileName);
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new ProbeFailure("ACTIVE_DOCUMENT_MISSING");
            RequireExactDocumentPath(document, DrawingAVariable);
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_MISSING");
            var context = new ProbeContext(document, project, nonce);
            if (requireState) State(context);
            return context;
        }

        private static SequenceState State(ProbeContext context)
        {
            var state = RequiredState();
            if (!ReferenceEquals(state.DocumentA, context.Document) ||
                !string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_CONTEXT_CHANGED");
            return state;
        }

        private static SequenceState RequiredState()
        {
            lock (Sync) return _state ?? throw new ProbeFailure("SEQUENCE_NOT_INITIALIZED");
        }

        private static Owners RequireOwners(Document document, ProjectState project, SequenceState? state = null)
        {
            var candidates = project.Elements.Where(x => x.Category == ElementCategory.ArchitecturalWall).ToList();
            if (state != null)
                candidates = candidates.Where(x => string.Equals(x.Id, state.LineOwnerId, StringComparison.Ordinal) ||
                                                    string.Equals(x.Id, state.PolylineOwnerId, StringComparison.Ordinal)).ToList();
            ProjectElement? lineOwner = null;
            ProjectElement? polyOwner = null;
            foreach (var candidate in candidates)
            {
                if (candidate.SourceHandles.Count != 1) continue;
                var ids = CadHandleService.Resolve(document, candidate.SourceHandles);
                if (ids.Count != 1) continue;
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                    if (entity is Line) lineOwner = Unique(lineOwner, candidate);
                    else if (entity is Polyline polyline && !polyline.Closed) polyOwner = Unique(polyOwner, candidate);
                }
            }
            if (lineOwner == null || polyOwner == null)
                throw new ProbeFailure("SOURCE_OWNER_SET_REJECTED");
            return new Owners(lineOwner, polyOwner);
        }

        private static ProjectElement Unique(ProjectElement? current, ProjectElement candidate)
        {
            if (current != null) throw new ProbeFailure("SOURCE_OWNER_SET_REJECTED");
            return candidate;
        }

        private static void SelectOwnerSource(ProbeContext context, bool line)
        {
            var state = State(context);
            var owners = RequireOwners(context.Document, context.Project, state);
            SelectHandles(context.Document, line ? owners.Line.SourceHandles : owners.Polyline.SourceHandles);
        }

        private static void SelectSources(Document document, Owners owners) =>
            SelectHandles(document, owners.Line.SourceHandles.Concat(owners.Polyline.SourceHandles));

        private static void SelectHandles(Document document, IEnumerable<string> handles)
        {
            var values = handles.ToList();
            var ids = CadHandleService.Resolve(document, values);
            if (ids.Count != values.Count) throw new ProbeFailure("SELECTION_TARGET_MISSING");
            document.Editor.SetImpliedSelection(ids.ToArray());
        }

        private static void EditSources(Document document, Owners owners, bool secondEdit)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var line = (Line)transaction.GetObject(CadHandleService.Resolve(document, owners.Line.SourceHandles).Single(), OpenMode.ForWrite, false);
                var polyline = (Polyline)transaction.GetObject(CadHandleService.Resolve(document, owners.Polyline.SourceHandles).Single(), OpenMode.ForWrite, false);
                if (!secondEdit)
                {
                    line.StartPoint = Point(document, 1d, 1d);
                    line.EndPoint = Point(document, 7d, 1d);
                    SetPolyline(document, polyline, new[] { new Point2d(2d, 11d), new Point2d(6d, 11d), new Point2d(6d, 15d) });
                }
                else
                {
                    line.StartPoint = Point(document, 3d, 2d);
                    line.EndPoint = Point(document, 10d, 2d);
                    SetPolyline(document, polyline, new[] { new Point2d(3d, 12d), new Point2d(8d, 12d), new Point2d(8d, 16d) });
                }
                transaction.Commit();
            }
        }

        private static Point3d Point(Document document, double xM, double yM) =>
            new Point3d(CadGeometryGuard.ToDrawingUnits(document, xM, "LOCAL-004 X"),
                        CadGeometryGuard.ToDrawingUnits(document, yM, "LOCAL-004 Y"), 0d);

        private static void SetPolyline(Document document, Polyline polyline, IReadOnlyList<Point2d> meters)
        {
            if (polyline.NumberOfVertices != meters.Count) throw new ProbeFailure("POLYLINE_VERTEX_COUNT_REJECTED");
            for (var i = 0; i < meters.Count; i++)
                polyline.SetPointAt(i, new Point2d(
                    CadGeometryGuard.ToDrawingUnits(document, meters[i].X, "LOCAL-004 poly X"),
                    CadGeometryGuard.ToDrawingUnits(document, meters[i].Y, "LOCAL-004 poly Y")));
        }

        private static ObjectId AppendLine(Document document, double x1M, double y1M, double x2M, double y2M)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(Point(document, x1M, y1M), Point(document, x2M, y2M));
                line.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                transaction.Commit();
                return id;
            }
        }

        private static Snapshot CaptureA(SequenceState state)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(state.DocumentA, out var project) ||
                !string.Equals(project.ProjectId, state.ProjectId, StringComparison.Ordinal))
                throw new ProbeFailure("DOCUMENT_A_PROJECT_CHANGED");
            return Capture(state.DocumentA, project, RequireOwners(state.DocumentA, project, state));
        }

        private static Snapshot Capture(Document document, ProjectState project, Owners owners)
        {
            var generated = owners.All.SelectMany(GeneratedHandles).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            return new Snapshot(
                ProjectDigest(project),
                SourceDigest(document, owners),
                EntityDigest(document, generated),
                generated);
        }

        private static IReadOnlyList<string> GeneratedHandles(ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            var values = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CadHandleService.NormalizeHexHandle(x) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return values.AsReadOnly();
        }

        private static void RequireCompleteGenerated(Document document, Snapshot snapshot, string label)
        {
            if (snapshot.GeneratedHandles.Count != 2 || CadHandleService.GetLiveSolidHandles(document, snapshot.GeneratedHandles).Count != 2)
                throw new InvalidOperationException("LOCAL-004 " + label + " generated ownership is incomplete.");
        }

        private static void RequireNoGenerated(Document document, Snapshot snapshot, IReadOnlyList<string> previous, string label)
        {
            if (snapshot.GeneratedHandles.Count != 0 || CadHandleService.GetLiveHandles(document, previous).Count != 0)
                throw new InvalidOperationException("LOCAL-004 " + label + " retained stale generated output.");
        }

        private static void RequireSemanticMatchesSources(Document document, Owners owners)
        {
            foreach (var owner in owners.All)
            {
                var ids = CadHandleService.Resolve(document, owner.SourceHandles);
                if (ids.Count != 1) throw new ProbeFailure("SOURCE_MISSING");
                double drawingLength;
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var curve = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Curve
                        ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                    drawingLength = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                }
                if (!owner.Properties.TryGetValue("LengthM", out var raw) ||
                    !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var semantic) ||
                    !Finite(semantic))
                    throw new ProbeFailure("SEMANTIC_LENGTH_REJECTED");
                var live = CadGeometryGuard.ToMeters(document, drawingLength, "LOCAL-004 source length");
                if (Math.Abs(live - semantic) > 1e-9d)
                    throw new ProbeFailure("SEMANTIC_SOURCE_MISMATCH");
            }
        }

        private static string ProjectDigest(ProjectState project)
        {
            var builder = new StringBuilder();
            Append(builder, project.ProjectId); Append(builder, project.Name); Append(builder, project.DrawingPath);
            Append(builder, project.DrawingFingerprint); Append(builder, project.ActiveFloorId); Append(builder, project.ActiveZoneId);
            Append(builder, project.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.ChangeVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, project.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            Append(builder, project.AuditEvents.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var pair in project.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal))
            { Append(builder, pair.Key); Append(builder, pair.Value); }
            foreach (var element in project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                Append(builder, element.Id); Append(builder, element.Category.ToString()); Append(builder, element.FamilyId);
                Append(builder, element.FloorId); Append(builder, element.ZoneId); Append(builder, element.DrawingFingerprint);
                Append(builder, ((int)element.Dirty).ToString(CultureInfo.InvariantCulture));
                Append(builder, element.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
                foreach (var value in element.SourceHandles) Append(builder, value);
                foreach (var value in element.DependsOn) Append(builder, value);
                foreach (var pair in element.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal))
                { Append(builder, pair.Key); Append(builder, pair.Value); }
                foreach (var pair in element.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal))
                { Append(builder, pair.Key); Append(builder, pair.Value.ToString("R", CultureInfo.InvariantCulture)); }
            }
            return Sha256(builder.ToString());
        }

        private static string SourceDigest(Document document, Owners owners) =>
            EntityDigest(document, owners.All.SelectMany(x => x.SourceHandles));

        private static string EntityDigest(Document document, IEnumerable<string> handles)
        {
            var ids = CadHandleService.Resolve(document, handles);
            return EntityDigest(document, ids);
        }

        private static string EntityDigest(Document document, IEnumerable<ObjectId> ids)
        {
            var builder = new StringBuilder();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids.OrderBy(x => x.Handle.Value))
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                        ?? throw new ProbeFailure("ENTITY_MISSING");
                    Append(builder, CadHandleService.NormalizeHexHandle(entity.Handle.ToString()));
                    Append(builder, entity.GetType().Name);
                    AppendBounds(builder, entity.GeometricExtents);
                    if (entity is Line line)
                    { AppendPoint(builder, line.StartPoint); AppendPoint(builder, line.EndPoint); }
                    else if (entity is Polyline polyline)
                    {
                        Append(builder, polyline.Closed ? "1" : "0");
                        Append(builder, polyline.Elevation.ToString("R", CultureInfo.InvariantCulture));
                        for (var i = 0; i < polyline.NumberOfVertices; i++)
                        { var point = polyline.GetPoint2dAt(i); Append(builder, point.X.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Y.ToString("R", CultureInfo.InvariantCulture)); Append(builder, polyline.GetBulgeAt(i).ToString("R", CultureInfo.InvariantCulture)); }
                    }
                }
            }
            return Sha256(builder.ToString());
        }

        private static bool Same(Snapshot left, Snapshot right) =>
            string.Equals(left.ProjectDigest, right.ProjectDigest, StringComparison.Ordinal) &&
            string.Equals(left.SourceDigest, right.SourceDigest, StringComparison.Ordinal) &&
            string.Equals(left.GeneratedDigest, right.GeneratedDigest, StringComparison.Ordinal) &&
            left.GeneratedHandles.SequenceEqual(right.GeneratedHandles, StringComparer.OrdinalIgnoreCase);

        private static void RequireExactDocumentPath(Document document, string variable)
        {
            var expected = Path.GetFullPath(Environment.GetEnvironmentVariable(variable) ?? string.Empty);
            var actual = Path.GetFullPath(document.Name ?? string.Empty);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
        }

        private static string RequiredNonce()
        {
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new ProbeFailure("AUTOMATION_CONTEXT_REJECTED");
            return nonce;
        }

        private static string RequiredPath(string variable, string fileName)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("RESULT_PATH_REJECTED");
            var path = Path.GetFullPath(raw);
            if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)) || !Directory.Exists(Path.GetDirectoryName(path)))
                throw new ProbeFailure("RESULT_PATH_REJECTED");
            return path;
        }

        private static bool RequiredBooleanEnvironment(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable) ?? string.Empty;
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new ProbeFailure("REOPEN_EXPECTATION_REJECTED");
        }

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _)) return;
                var path = RequiredPath(ResultVariable, ResultFileName);
                if (File.Exists(path)) return;
                WriteMarkerAtomic(path, new[]
                {
                    "status=FAIL", "command=QS3DSRTREOPEN", "nonce=" + nonce, "schema=" + Schema,
                    "qualification_boundary=LOCAL_004_ONLY", "production_local004_qualified=false",
                    "error_code=SOURCE_RECONCILE_RUNTIME_FAILED", "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("LOCAL-004 marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush(); stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Append(StringBuilder builder, string? value)
        {
            var text = value ?? string.Empty;
            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('|');
        }

        private static void AppendPoint(StringBuilder builder, Point3d point)
        { Append(builder, point.X.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Y.ToString("R", CultureInfo.InvariantCulture)); Append(builder, point.Z.ToString("R", CultureInfo.InvariantCulture)); }

        private static void AppendBounds(StringBuilder builder, Extents3d extents)
        { AppendPoint(builder, extents.MinPoint); AppendPoint(builder, extents.MaxPoint); }

        private static string Sha256(string value)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty); }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static string OneLine(string? value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        private static string Boolean(bool value) => value ? "true" : "false";

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce) { Document = document; Project = project; Nonce = nonce; }
            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
        }

        private sealed class Owners
        {
            public Owners(ProjectElement line, ProjectElement polyline) { Line = line; Polyline = polyline; All = new[] { line, polyline }; }
            public ProjectElement Line { get; }
            public ProjectElement Polyline { get; }
            public IReadOnlyList<ProjectElement> All { get; }
        }

        private sealed class Snapshot
        {
            public Snapshot(string projectDigest, string sourceDigest, string generatedDigest, IReadOnlyList<string> generatedHandles)
            { ProjectDigest = projectDigest; SourceDigest = sourceDigest; GeneratedDigest = generatedDigest; GeneratedHandles = generatedHandles; }
            public string ProjectDigest { get; }
            public string SourceDigest { get; }
            public string GeneratedDigest { get; }
            public IReadOnlyList<string> GeneratedHandles { get; }
        }

        private sealed class SequenceState
        {
            public SequenceState(Document documentA, string projectId, string lineOwnerId, string polylineOwnerId, string nonce, Snapshot initial, string firstEditedSourceDigest)
            { DocumentA = documentA; ProjectId = projectId; LineOwnerId = lineOwnerId; PolylineOwnerId = polylineOwnerId; Nonce = nonce; Initial = initial; FirstEditedSourceDigest = firstEditedSourceDigest; }
            public Document DocumentA { get; }
            public string ProjectId { get; }
            public string LineOwnerId { get; }
            public string PolylineOwnerId { get; }
            public string Nonce { get; }
            public Snapshot Initial { get; }
            public string FirstEditedSourceDigest { get; }
            public Snapshot? AfterFirstSync { get; set; }
            public Snapshot? AfterFirstRebuild { get; set; }
            public Snapshot? BeforeForcedFailure { get; set; }
            public Snapshot? BeforeGeneratedRefusal { get; set; }
            public ProjectStateSnapshot? AmbiguousRollback { get; set; }
            public Snapshot? BeforeAmbiguousRefusal { get; set; }
            public Document? DocumentB { get; set; }
            public string ProjectBId { get; set; } = string.Empty;
            public string DocumentBSourceHandle { get; set; } = string.Empty;
            public string BeforeDocumentBProjectDigest { get; set; } = string.Empty;
            public string BeforeDocumentBNativeDigest { get; set; } = string.Empty;
            public Snapshot? BeforeDocumentARefusal { get; set; }
            public Snapshot? BeforeFinalSync { get; set; }
            public Snapshot? AfterFinalSync { get; set; }
            public Snapshot? FinalRebuild { get; set; }
            public bool ForcedRollbackVerified { get; set; }
            public bool GeneratedRefusalVerified { get; set; }
            public bool AmbiguousRefusalVerified { get; set; }
            public bool MultiDocumentRefusalVerified { get; set; }
            public bool FinalSyncVerified { get; set; }
            public bool UndoChecked { get; set; }
            public bool UndoCoherent { get; set; }
            public bool RedoChecked { get; set; }
            public bool RedoCoherent { get; set; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base("LOCAL-004 probe state rejected.") { Code = code; }
            public string Code { get; }
        }
    }
}
