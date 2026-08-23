using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 P01 probe for a production Direct Draw LINE wall edited by
    /// BricsCAD's native MOVE, ROTATE and STRETCH commands. The probe never writes CAD directly;
    /// production QS3DSYNCSOURCE/QS3DBUILD3D/QS3DSAVE own semantic, native and persistence changes.
    /// </summary>
    public sealed class SourceReconcileNativeLineEditRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RESULT";
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_NONCE";
        private const string DrawingVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_DWG";
        private const string DrawingBVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_DWG_B";
        private const string ModeVariable = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_MODE";
        private const string ResultFileName = "source-reconcile-native-line-result.txt";
        private const string PhaseFileName = "source-reconcile-native-line-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RUNTIME_V1";
        private const string EnhancedSchema = "QS3D_SOURCE_RECONCILE_NATIVE_LINE_V26_RUNTIME_V2";
        private const string EnhancedMode = "V26_P03";
        private const double ToleranceM = 1e-8d;
        private static readonly object Sync = new object();
        private static SequenceState? _state;

        [CommandMethod("QS3DSRNATIVEPREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("prepare", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Initial);
                RequireSemanticLength(owner, 5d, "initial");
                var initialGenerated = RequireGenerated(context.Document, owner, "initial");
                lock (Sync)
                {
                    _state = new SequenceState(
                        context.Document,
                        context.Project.ProjectId,
                        owner.Id,
                        owner.SourceHandles.Single(),
                        context.Nonce,
                        initialGenerated,
                        IsEnhancedMode());
                }
            });
        }

        [CommandMethod("QS3DSRNATIVEMOVE", CommandFlags.Modal)]
        public void Move()
        {
            Execute("native_move", () =>
            {
                var context = Context();
                var state = State(context, "PREPARED");
                var owner = Owner(context, state);
                var selection = ExactSourceSelection(context.Document, state.SourceHandle);
                context.Document.Editor.Command(
                    "_.MOVE",
                    selection,
                    string.Empty,
                    "_Displacement",
                    new Point3d(0d, Drawing(context.Document, 2d), 0d));
                RequireGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticLength(owner, 5d, "native MOVE before reconcile");
                RequireSameGenerated(context.Document, owner, state.InitialGeneratedHandle, "native MOVE before reconcile");
                state.NativeMoveVerified = true;
                state.Phase = "MOVED";
            });
        }

        [CommandMethod("QS3DSRNATIVESELECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SelectSource()
        {
            Execute("select_source", () =>
            {
                var context = Context();
                var state = State(context);
                var id = ResolveSource(context.Document, state.SourceHandle);
                context.Document.Editor.SetImpliedSelection(new[] { id });
            });
        }

        [CommandMethod("QS3DSRNATIVECHECKMOVE", CommandFlags.Modal)]
        public void CheckMoveReconcile()
        {
            Execute("check_move_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "MOVED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticLength(owner, 5d, "MOVE reconcile");
                RequireNoGenerated(context.Document, owner, state.InitialGeneratedHandle, "MOVE reconcile");
                state.MoveReconcileVerified = true;
                state.Phase = "MOVE_SYNCED";
            });
        }

        [CommandMethod("QS3DSRNATIVECHECKMOVEBUILD", CommandFlags.Modal)]
        public void CheckMoveBuild()
        {
            Execute("check_move_build", () =>
            {
                var context = Context();
                var state = State(context, "MOVE_SYNCED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticLength(owner, 5d, "MOVE rebuild");
                var handle = RequireGenerated(context.Document, owner, "MOVE rebuild");
                if (string.Equals(handle, state.InitialGeneratedHandle, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
                state.MoveGeneratedHandle = handle;
                state.MoveRebuildVerified = true;
                state.Phase = "MOVE_REBUILT";
            });
        }

        [CommandMethod("QS3DSRNATIVEROTATE", CommandFlags.Modal)]
        public void Rotate()
        {
            Execute("native_rotate", () =>
            {
                var context = Context();
                var state = State(context, "MOVE_REBUILT");
                var owner = Owner(context, state);
                var selection = ExactSourceSelection(context.Document, state.SourceHandle);
                context.Document.Editor.Command(
                    "_.ROTATE",
                    selection,
                    string.Empty,
                    new Point3d(0d, Drawing(context.Document, 2d), 0d),
                    "90");
                RequireGeometry(context.Document, owner, ExpectedStage.Rotated);
                RequireSemanticLength(owner, 5d, "native ROTATE before reconcile");
                RequireSameGenerated(context.Document, owner, state.MoveGeneratedHandle, "native ROTATE before reconcile");
                state.NativeRotateVerified = true;
                state.Phase = "ROTATED";
            });
        }

        [CommandMethod("QS3DSRNATIVECHECKROTATE", CommandFlags.Modal)]
        public void CheckRotateReconcile()
        {
            Execute("check_rotate_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "ROTATED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Rotated);
                RequireSemanticLength(owner, 5d, "ROTATE reconcile");
                RequireNoGenerated(context.Document, owner, state.MoveGeneratedHandle, "ROTATE reconcile");
                state.RotateReconcileVerified = true;
                state.Phase = "ROTATE_SYNCED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKROTATEBUILD", CommandFlags.Modal)]
        public void CheckEnhancedRotateBuild()
        {
            Execute("check_v26_rotate_build", () =>
            {
                var context = Context();
                var state = State(context, "ROTATE_SYNCED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Rotated);
                RequireSemanticLength(owner, 5d, "V26 ROTATE rebuild");
                var handle = RequireGenerated(context.Document, owner, "V26 ROTATE rebuild");
                if (string.Equals(handle, state.InitialGeneratedHandle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(handle, state.MoveGeneratedHandle, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
                state.RotateGeneratedHandle = handle;
                state.RotateRebuildVerified = true;
                state.Phase = "ROTATE_REBUILT";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26STRETCHPREPARE", CommandFlags.Modal)]
        public void PrepareEnhancedStretch()
        {
            Execute("prepare_v26_native_stretch", () =>
            {
                var context = Context();
                var state = State(context, "ROTATE_REBUILT");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Rotated);
                RequireSemanticLength(owner, 5d, "V26 native STRETCH preparation");
                RequireSameGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 native STRETCH preparation");
                context.Document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                state.Phase = "STRETCH_READY_ENHANCED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26STRETCH", CommandFlags.Modal)]
        public void CheckEnhancedNativeStretch()
        {
            Execute("v26_native_stretch", () =>
            {
                var context = Context();
                var state = State(context, "STRETCH_READY_ENHANCED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_GEOMETRY_" + ClassifyStretchGeometry(context.Document, owner)); }
                try { RequireSemanticLength(owner, 5d, "V26 native STRETCH before reconcile"); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_SEMANTIC_REJECTED"); }
                try { RequireSameGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 native STRETCH before reconcile"); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_GENERATED_REJECTED"); }
                state.NativeStretchVerified = true;
                state.Phase = "STRETCHED_ENHANCED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKSTRETCH", CommandFlags.Modal)]
        public void CheckEnhancedStretchReconcile()
        {
            Execute("check_v26_stretch_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "STRETCHED_ENHANCED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "V26 STRETCH reconcile");
                RequireNoGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 STRETCH reconcile");
                state.StretchReconcileVerified = true;
                state.Phase = "STRETCH_SYNCED_ENHANCED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKUNDO", CommandFlags.Modal)]
        public void CheckEnhancedUndo()
        {
            Execute("check_v26_undo", () =>
            {
                var context = Context();
                var state = State(context, "STRETCH_SYNCED_ENHANCED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_UNDO_GEOMETRY_REJECTED"); }
                try { RequireSemanticLength(owner, 5d, "V26 native Undo"); }
                catch { throw new ProbeFailure("NATIVE_UNDO_SEMANTIC_REJECTED"); }
                try { RequireSameGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 native Undo"); }
                catch { throw new ProbeFailure("NATIVE_UNDO_GENERATED_REJECTED"); }
                state.NativeUndoVerified = true;
                state.Phase = "UNDO_VERIFIED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26REARMREDO", CommandFlags.Modal)]
        public void RearmEnhancedRedo()
        {
            Execute("rearm_v26_redo", () =>
            {
                var context = Context();
                var state = State(context, "UNDO_VERIFIED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_REDO_REARM_GEOMETRY_REJECTED"); }
                try { RequireSemanticLength(owner, 8d, "V26 native Redo rearm"); }
                catch { throw new ProbeFailure("NATIVE_REDO_REARM_SEMANTIC_REJECTED"); }
                try { RequireNoGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 native Redo rearm"); }
                catch { throw new ProbeFailure("NATIVE_REDO_REARM_GENERATED_REJECTED"); }
                state.Phase = "REDO_ARMED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKREDO", CommandFlags.Modal)]
        public void CheckEnhancedRedo()
        {
            Execute("check_v26_redo", () =>
            {
                var context = Context();
                var state = State(context, "REDO_ARMED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_REDO_GEOMETRY_REJECTED"); }
                try { RequireSemanticLength(owner, 8d, "V26 native Redo"); }
                catch { throw new ProbeFailure("NATIVE_REDO_SEMANTIC_REJECTED"); }
                try { RequireNoGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 native Redo"); }
                catch { throw new ProbeFailure("NATIVE_REDO_GENERATED_REJECTED"); }
                state.NativeRedoVerified = true;
                state.Phase = "REDO_VERIFIED";
            });
        }

        [CommandMethod("QS3DSRNATIVESTRETCHPREPARE", CommandFlags.Modal)]
        public void PrepareStretch()
        {
            Execute("prepare_native_stretch", () =>
            {
                var context = Context();
                var state = State(context, "ROTATE_SYNCED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Rotated);
                RequireSemanticLength(owner, 5d, "native STRETCH preparation");
                RequireNoGeneratedProperty(owner, "native STRETCH preparation");
                // STRETCH must derive vertex ownership from the runner's explicit top-level
                // crossing window, never a retained PICKFIRST selection for the whole LINE.
                context.Document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                state.Phase = "STRETCH_READY";
            });
        }

        [CommandMethod("QS3DSRNATIVESTRETCH", CommandFlags.Modal)]
        public void CheckNativeStretch()
        {
            Execute("native_stretch", () =>
            {
                var context = Context();
                var state = State(context, "STRETCH_READY");
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_GEOMETRY_" + ClassifyStretchGeometry(context.Document, owner)); }
                try { RequireSemanticLength(owner, 5d, "native STRETCH before reconcile"); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_SEMANTIC_REJECTED"); }
                try { RequireNoGeneratedProperty(owner, "native STRETCH before reconcile"); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_GENERATED_REJECTED"); }
                state.NativeStretchVerified = true;
                state.Phase = "STRETCHED";
            });
        }

        [CommandMethod("QS3DSRNATIVECHECKSTRETCH", CommandFlags.Modal)]
        public void CheckStretchReconcile()
        {
            Execute("check_stretch_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "STRETCHED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "STRETCH reconcile");
                RequireNoGeneratedProperty(owner, "STRETCH reconcile");
                state.StretchReconcileVerified = true;
                state.Phase = "STRETCH_SYNCED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26SELECTB", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SelectEnhancedWrongDrawingSource()
        {
            Execute("select_v26_wrong_dwg_source", () =>
            {
                var multi = MultiDwgContext("REDO_VERIFIED", requireDrawingBActive: true);
                var documentB = multi.DocumentB;
                if (ProjectContextCoordinator.TryGetReadOnly(documentB, out _))
                    throw new ProbeFailure("WRONG_DWG_PROJECT_CREATED");
                var lineIds = CurrentSpaceLineIds(documentB);
                if (lineIds.Count == 0) throw new ProbeFailure("WRONG_DWG_SOURCE_MISSING");
                documentB.Editor.SetImpliedSelection(new[] { lineIds[0] });
                var selected = documentB.Editor.SelectImplied();
                if (selected.Status != PromptStatus.OK || selected.Value == null ||
                    selected.Value.GetObjectIds().Length != 1 || selected.Value.GetObjectIds()[0] != lineIds[0])
                    throw new ProbeFailure("WRONG_DWG_SELECTION_REJECTED");
                multi.State.DrawingBEntityCount = CurrentSpaceEntityCount(documentB);
                multi.State.TwoDocumentsObserved = Application.DocumentManager.Count >= 2;
                if (!multi.State.TwoDocumentsObserved) throw new ProbeFailure("DOCUMENT_COUNT_REJECTED");
                multi.State.Phase = "B_SELECTED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKB", CommandFlags.Modal)]
        public void CheckEnhancedWrongDrawingRefusal()
        {
            Execute("check_v26_wrong_dwg_refusal", () =>
            {
                var multi = MultiDwgContext("B_SELECTED", requireDrawingBActive: true);
                if (ProjectContextCoordinator.TryGetReadOnly(multi.DocumentB, out _))
                    throw new ProbeFailure("WRONG_DWG_PROJECT_CREATED");
                if (File.Exists(Path.ChangeExtension(multi.DrawingB, ".qsdb")))
                    throw new ProbeFailure("WRONG_DWG_SIDECAR_CREATED");
                if (CurrentSpaceEntityCount(multi.DocumentB) != multi.State.DrawingBEntityCount)
                    throw new ProbeFailure("WRONG_DWG_ENTITY_MUTATED");

                var projectA = ProjectForDocument(multi.DocumentA);
                var contextA = new ProbeContext(multi.DocumentA, projectA, multi.State.Nonce);
                var ownerA = Owner(contextA, multi.State);
                RequireGeometry(multi.DocumentA, ownerA, ExpectedStage.Stretched);
                RequireSemanticLength(ownerA, 8d, "V26 wrong-DWG refusal / drawing A");
                RequireNoGenerated(contextA.Document, ownerA, multi.State.RotateGeneratedHandle, "V26 wrong-DWG refusal / drawing A");
                multi.State.WrongDwgReconcileRefused = true;
                multi.State.DrawingAUnchangedWhileBActive = true;
                multi.State.DrawingBProjectNotCreated = true;
                multi.State.Phase = "B_VERIFIED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26ACTIVATEA", CommandFlags.Modal)]
        public void ActivateEnhancedDrawingA()
        {
            Execute("activate_v26_drawing_a", () =>
            {
                var multi = MultiDwgContext("B_VERIFIED", requireDrawingBActive: true);
                Application.DocumentManager.MdiActiveDocument = multi.DocumentA;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, multi.DocumentA))
                    throw new ProbeFailure("DOCUMENT_ACTIVATION_REJECTED");
                multi.State.DrawingAReactivated = true;
                multi.State.Phase = "A_REACTIVATED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CHECKA", CommandFlags.Modal)]
        public void CheckEnhancedDrawingAAfterSwitch()
        {
            Execute("check_v26_drawing_a", () =>
            {
                var context = Context();
                var state = State(context, "A_REACTIVATED");
                RequireEnhanced(state);
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "V26 drawing A reactivation");
                RequireNoGenerated(context.Document, owner, state.RotateGeneratedHandle, "V26 drawing A reactivation");

                var documentB = FindDocument(RequiredDrawingPath(DrawingBVariable));
                if (ProjectContextCoordinator.TryGetReadOnly(documentB, out _) ||
                    File.Exists(Path.ChangeExtension(documentB.Name, ".qsdb")) ||
                    CurrentSpaceEntityCount(documentB) != state.DrawingBEntityCount)
                    throw new ProbeFailure("WRONG_DWG_STATE_MUTATED");
                state.DrawingBUnchanged = true;
                state.MultiDwgIsolationVerified = true;
                state.Phase = "A_VERIFIED";
            });
        }

        [CommandMethod("QS3DSRNATIVEV26CLOSEB", CommandFlags.Modal)]
        public void CloseEnhancedDrawingB()
        {
            Execute("close_v26_drawing_b", () =>
            {
                var context = Context();
                var state = State(context, "A_VERIFIED");
                RequireEnhanced(state);
                var drawingB = RequiredDrawingPath(DrawingBVariable);
                var documentB = FindDocument(drawingB);
                documentB.CloseAndDiscard();
                if (TryFindDocument(drawingB, out _)) throw new ProbeFailure("WRONG_DWG_CLOSE_REJECTED");
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, context.Document))
                    Application.DocumentManager.MdiActiveDocument = context.Document;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, context.Document))
                    throw new ProbeFailure("DOCUMENT_ACTIVATION_REJECTED");
                state.DrawingBClosed = true;
                state.Phase = "MULTIDWG_CLOSED";
            });
        }

        [CommandMethod("QS3DSRNATIVEFINAL", CommandFlags.Modal)]
        public void FinalizeSessionOne()
        {
            Execute("final_rebuild", () =>
            {
                var context = Context();
                var state = State(context);
                var expectedPhase = state.Enhanced ? "MULTIDWG_CLOSED" : "STRETCH_SYNCED";
                if (!string.Equals(state.Phase, expectedPhase, StringComparison.Ordinal))
                    throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "final rebuild");
                var finalGenerated = RequireGenerated(context.Document, owner, "final rebuild");
                if (string.Equals(finalGenerated, state.InitialGeneratedHandle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(finalGenerated, state.MoveGeneratedHandle, StringComparison.OrdinalIgnoreCase) ||
                    (state.Enhanced && string.Equals(finalGenerated, state.RotateGeneratedHandle, StringComparison.OrdinalIgnoreCase)))
                    throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
                state.FinalRebuildVerified = true;
                state.Phase = "FINAL_REBUILT";
                WriteMarkerAtomic(
                    RequiredPath(PhaseVariable, PhaseFileName),
                    state.Enhanced
                        ? EnhancedPhaseEvidenceLines(context.Nonce, state)
                        : EvidenceLines("PASS", context.Nonce, coldReopenVerified: false, state));
            });
        }

        [CommandMethod("QS3DSRNATIVEREOPEN", CommandFlags.Modal)]
        public void Reopen()
        {
            Execute("cold_reopen", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Stretched);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "cold reopen");
                RequireGenerated(context.Document, owner, "cold reopen");
                var phase = ReadPhaseEvidence(context.Nonce);
                WriteMarkerAtomic(RequiredPath(ResultVariable, ResultFileName), new[]
                {
                    "status=PASS",
                    "command=QS3DSRNATIVEREOPEN",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_004_P01_LINE_ONLY",
                    "production_local004_p01_qualified=true",
                    "native_move_verified=" + phase["native_move_verified"],
                    "move_reconcile_verified=" + phase["move_reconcile_verified"],
                    "move_rebuild_verified=" + phase["move_rebuild_verified"],
                    "native_rotate_verified=" + phase["native_rotate_verified"],
                    "rotate_reconcile_verified=" + phase["rotate_reconcile_verified"],
                    "native_stretch_verified=" + phase["native_stretch_verified"],
                    "stretch_reconcile_verified=" + phase["stretch_reconcile_verified"],
                    "final_rebuild_verified=" + phase["final_rebuild_verified"],
                    "cold_reopen_verified=true",
                    "source_type=LINE",
                    "edit_commands=MOVE_ROTATE_STRETCH",
                    "final_length_class=EIGHT_METERS",
                    "error_code=NONE"
                });
            });
        }

        [CommandMethod("QS3DSRNATIVEV26REOPEN", CommandFlags.Modal)]
        public void ReopenEnhanced()
        {
            Execute("v26_cold_reopen", () =>
            {
                if (!IsEnhancedMode()) throw new ProbeFailure("ENHANCED_MODE_REJECTED");
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Stretched);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticLength(owner, 8d, "V26 cold reopen");
                RequireGenerated(context.Document, owner, "V26 cold reopen");
                WriteMarkerAtomic(RequiredPath(ResultVariable, ResultFileName), new[]
                {
                    "status=PASS",
                    "command=QS3DSRNATIVEV26REOPEN",
                    "nonce=" + context.Nonce,
                    "schema=" + EnhancedSchema,
                    "qualification_boundary=LOCAL_018_P03_V26_LINE_LIFECYCLE",
                    "local018_p03_reopen_candidate=true",
                    "prior_session_phases_replayed=false",
                    "cold_reopen_verified=true",
                    "source_type=LINE",
                    "final_length_class=EIGHT_METERS",
                    "error_code=NONE"
                });
            });
        }

        private static void Execute(string phase, Action action)
        {
            try { action(); }
            catch (ProbeFailure failure) { TryWriteFailure(phase, failure.Code); }
            catch { TryWriteFailure(phase, "STATE_REJECTED"); }
        }

        private static ProbeContext Context(bool requireState = true)
        {
            var nonce = RequiredNonce();
            RequiredPath(ResultVariable, ResultFileName);
            RequiredPath(PhaseVariable, PhaseFileName);
            var document = Application.DocumentManager.MdiActiveDocument ?? throw new ProbeFailure("ACTIVE_DOCUMENT_MISSING");
            RequireExactDocument(document);
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_MISSING");
            var context = new ProbeContext(document, project, nonce);
            if (requireState) State(context);
            return context;
        }

        private static SequenceState State(ProbeContext context, string? expectedPhase = null)
        {
            SequenceState state;
            lock (Sync) state = _state ?? throw new ProbeFailure("SEQUENCE_NOT_INITIALIZED");
            if (!ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_CONTEXT_CHANGED");
            if (expectedPhase != null && !string.Equals(state.Phase, expectedPhase, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
            return state;
        }

        private static MultiDwgProbeContext MultiDwgContext(string expectedPhase, bool requireDrawingBActive)
        {
            var nonce = RequiredNonce();
            RequiredPath(ResultVariable, ResultFileName);
            RequiredPath(PhaseVariable, PhaseFileName);
            var drawingA = RequiredDrawingPath(DrawingVariable);
            var drawingB = RequiredDrawingPath(DrawingBVariable);
            var documentA = FindDocument(drawingA);
            var documentB = FindDocument(drawingB);
            var active = Application.DocumentManager.MdiActiveDocument ?? throw new ProbeFailure("ACTIVE_DOCUMENT_MISSING");
            if (requireDrawingBActive && !ReferenceEquals(active, documentB))
                throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
            var projectA = ProjectForDocument(documentA);
            var contextA = new ProbeContext(documentA, projectA, nonce);
            var state = State(contextA, expectedPhase);
            RequireEnhanced(state);
            return new MultiDwgProbeContext(state, documentA, documentB, drawingA, drawingB);
        }

        private static ProjectState ProjectForDocument(Document document)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_MISSING");
            return project;
        }

        private static List<ObjectId> CurrentSpaceLineIds(Document document)
        {
            var result = new List<ObjectId>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space == null) throw new ProbeFailure("WRONG_DWG_SOURCE_MISSING");
                foreach (ObjectId id in space)
                {
                    if (id.IsNull || id.IsErased) continue;
                    if (transaction.GetObject(id, OpenMode.ForRead, false) is Line) result.Add(id);
                }
            }
            return result;
        }

        private static int CurrentSpaceEntityCount(Document document)
        {
            var count = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space == null) throw new ProbeFailure("WRONG_DWG_STATE_MUTATED");
                foreach (ObjectId id in space)
                {
                    if (!id.IsNull && !id.IsErased && transaction.GetObject(id, OpenMode.ForRead, false) is Entity) count++;
                }
            }
            return count;
        }

        private static Document FindDocument(string path)
        {
            if (TryFindDocument(path, out var document)) return document;
            throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
        }

        private static bool TryFindDocument(string path, out Document document)
        {
            foreach (Document candidate in Application.DocumentManager)
            {
                if (SamePath(candidate.Name, path))
                {
                    document = candidate;
                    return true;
                }
            }
            document = null!;
            return false;
        }

        private static bool SamePath(string? left, string? right)
        {
            try
            {
                return string.Equals(Path.GetFullPath(left ?? string.Empty), Path.GetFullPath(right ?? string.Empty), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string RequiredDrawingPath(string variable)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("AUTOMATION_CONTEXT_REJECTED");
            var path = Path.GetFullPath(raw);
            if (!File.Exists(path)) throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
            return path;
        }

        private static bool IsEnhancedMode() =>
            string.Equals(Environment.GetEnvironmentVariable(ModeVariable), EnhancedMode, StringComparison.Ordinal);

        private static void RequireEnhanced(SequenceState state)
        {
            if (!state.Enhanced || !IsEnhancedMode()) throw new ProbeFailure("ENHANCED_MODE_REJECTED");
        }

        private static ProjectElement Owner(ProbeContext context, SequenceState state)
        {
            var owner = context.Project.FindElement(state.OwnerId);
            if (owner == null || owner.Category != ElementCategory.ArchitecturalWall ||
                owner.SourceHandles.Count != 1 ||
                !string.Equals(owner.SourceHandles[0], state.SourceHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            ResolveSource(context.Document, state.SourceHandle);
            return owner;
        }

        private static ProjectElement FindUniqueOwner(Document document, ProjectState project, ExpectedStage stage)
        {
            var matches = new List<ProjectElement>();
            foreach (var candidate in project.Elements.Where(x => x.Category == ElementCategory.ArchitecturalWall && x.SourceHandles.Count == 1))
            {
                try
                {
                    RequireGeometry(document, candidate, stage);
                    matches.Add(candidate);
                }
                catch { }
            }
            if (matches.Count != 1) throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            return matches[0];
        }

        private static SelectionSet ExactSourceSelection(Document document, string sourceHandle)
        {
            var id = ResolveSource(document, sourceHandle);
            document.Editor.SetImpliedSelection(new[] { id });
            var selected = document.Editor.SelectImplied();
            if (selected.Status != PromptStatus.OK || selected.Value == null)
                throw new ProbeFailure("SELECTION_REJECTED");
            var ids = selected.Value.GetObjectIds();
            if (ids.Length != 1 || ids[0] != id) throw new ProbeFailure("SELECTION_REJECTED");
            return selected.Value;
        }

        private static ObjectId ResolveSource(Document document, string sourceHandle)
        {
            var ids = CadHandleService.Resolve(document, new[] { sourceHandle });
            if (ids.Count != 1) throw new ProbeFailure("SOURCE_MISSING");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                if (!(transaction.GetObject(ids[0], OpenMode.ForRead, false) is Line))
                    throw new ProbeFailure("SOURCE_TYPE_REJECTED");
            }
            return ids[0];
        }

        private static void RequireGeometry(Document document, ProjectElement owner, ExpectedStage stage)
        {
            var id = ResolveSource(document, owner.SourceHandles.Single());
            Point3d start;
            Point3d end;
            double length;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                start = line.StartPoint;
                end = line.EndPoint;
                length = line.Length;
            }
            var expected = Coordinates(stage);
            RequireNear(Meters(document, start.X), expected.StartX, "start X");
            RequireNear(Meters(document, start.Y), expected.StartY, "start Y");
            RequireNear(Meters(document, start.Z), 0d, "start Z");
            RequireNear(Meters(document, end.X), expected.EndX, "end X");
            RequireNear(Meters(document, end.Y), expected.EndY, "end Y");
            RequireNear(Meters(document, end.Z), 0d, "end Z");
            RequireNear(Meters(document, length), expected.Length, "length");
        }

        private static LineCoordinates Coordinates(ExpectedStage stage)
        {
            switch (stage)
            {
                case ExpectedStage.Initial: return new LineCoordinates(0d, 0d, 5d, 0d, 5d);
                case ExpectedStage.Moved: return new LineCoordinates(0d, 2d, 5d, 2d, 5d);
                case ExpectedStage.Rotated: return new LineCoordinates(0d, 2d, 0d, 7d, 5d);
                case ExpectedStage.Stretched: return new LineCoordinates(0d, 2d, 0d, 10d, 8d);
                default: throw new ProbeFailure("EXPECTED_GEOMETRY_REJECTED");
            }
        }

        private static string ClassifyStretchGeometry(Document document, ProjectElement owner)
        {
            try
            {
                var id = ResolveSource(document, owner.SourceHandles.Single());
                Point3d start;
                Point3d end;
                double length;
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                    if (line == null) return "OTHER";
                    start = line.StartPoint;
                    end = line.EndPoint;
                    length = line.Length;
                }
                var actual = new LineCoordinates(
                    Meters(document, start.X), Meters(document, start.Y),
                    Meters(document, end.X), Meters(document, end.Y),
                    Meters(document, length));
                if (Matches(actual, Coordinates(ExpectedStage.Stretched))) return "EXPECTED";
                if (Matches(actual, Coordinates(ExpectedStage.Rotated))) return "UNCHANGED";
                if (Matches(actual, new LineCoordinates(0d, 5d, 0d, 10d, 5d))) return "WHOLE_LINE_MOVED";
                if (Matches(actual, new LineCoordinates(0d, 2d, 0d, 3d, 1d))) return "ENDPOINT_SET_ABSOLUTE";
                if (Matches(actual, new LineCoordinates(0d, 5d, 0d, 7d, 2d))) return "STARTPOINT_MOVED";
                if (Matches(actual, new LineCoordinates(0d, -1d, 0d, 7d, 8d))) return "STARTPOINT_STRETCHED";
                return "OTHER";
            }
            catch { return "OTHER"; }
        }

        private static bool Matches(LineCoordinates actual, LineCoordinates expected) =>
            Near(actual.StartX, expected.StartX) && Near(actual.StartY, expected.StartY) &&
            Near(actual.EndX, expected.EndX) && Near(actual.EndY, expected.EndY) &&
            Near(actual.Length, expected.Length);

        private static bool Near(double actual, double expected) =>
            !double.IsNaN(actual) && !double.IsInfinity(actual) && Math.Abs(actual - expected) <= ToleranceM;

        private static void RequireSemanticLength(ProjectElement owner, double expectedM, string label)
        {
            if (!owner.Properties.TryGetValue("LengthM", out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Native LINE probe semantic length is unavailable at " + label + ".");
            RequireNear(value, expectedM, label + " semantic length");
        }

        private static string RequireGenerated(Document document, ProjectElement owner, string label)
        {
            if (!owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Native LINE probe generated ownership is missing at " + label + ".");
            var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CadHandleService.NormalizeHexHandle(x) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (handles.Count != 1 || CadHandleService.GetLiveSolidHandles(document, handles).Count != 1)
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            return handles[0];
        }

        private static void RequireSameGenerated(Document document, ProjectElement owner, string expected, string label)
        {
            var actual = RequireGenerated(document, owner, label);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
        }

        private static void RequireNoGenerated(Document document, ProjectElement owner, string previous, string label)
        {
            RequireNoGeneratedProperty(owner, label);
            if (CadHandleService.GetLiveHandles(document, new[] { previous }).Count != 0)
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
        }

        private static void RequireNoGeneratedProperty(ProjectElement owner, string label)
        {
            if (owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) && !string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Native LINE probe retained generated ownership at " + label + ".");
        }

        private static double Drawing(Document document, double meters) =>
            CadUnitService.MetersToDrawingUnits(document, meters);

        private static double Meters(Document document, double drawingUnits) =>
            CadUnitService.DrawingUnitsToMeters(document, drawingUnits);

        private static void RequireNear(double actual, double expected, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > ToleranceM)
                throw new InvalidOperationException("Native LINE probe geometry mismatch at " + label + ".");
        }

        private static IReadOnlyList<string> EvidenceLines(string status, string nonce, bool coldReopenVerified, SequenceState state) =>
            new[]
            {
                "status=" + status,
                "command=QS3DSRNATIVEFINAL",
                "nonce=" + nonce,
                "schema=" + Schema,
                "qualification_boundary=LOCAL_004_P01_LINE_ONLY",
                "production_local004_p01_qualified=false",
                "native_move_verified=" + Boolean(state.NativeMoveVerified),
                "move_reconcile_verified=" + Boolean(state.MoveReconcileVerified),
                "move_rebuild_verified=" + Boolean(state.MoveRebuildVerified),
                "native_rotate_verified=" + Boolean(state.NativeRotateVerified),
                "rotate_reconcile_verified=" + Boolean(state.RotateReconcileVerified),
                "native_stretch_verified=" + Boolean(state.NativeStretchVerified),
                "stretch_reconcile_verified=" + Boolean(state.StretchReconcileVerified),
                "final_rebuild_verified=" + Boolean(state.FinalRebuildVerified),
                "cold_reopen_verified=" + Boolean(coldReopenVerified),
                "source_type=LINE",
                "edit_commands=MOVE_ROTATE_STRETCH",
                "final_length_class=EIGHT_METERS",
                "error_code=NONE"
            };

        private static IReadOnlyList<string> EnhancedPhaseEvidenceLines(string nonce, SequenceState state) =>
            new[]
            {
                "status=PASS",
                "command=QS3DSRNATIVEFINAL",
                "nonce=" + nonce,
                "schema=" + EnhancedSchema,
                "qualification_boundary=LOCAL_018_P03_V26_LINE_LIFECYCLE",
                "local018_p03_phase_candidate=true",
                "native_move_verified=" + Boolean(state.NativeMoveVerified),
                "move_reconcile_verified=" + Boolean(state.MoveReconcileVerified),
                "move_rebuild_verified=" + Boolean(state.MoveRebuildVerified),
                "native_rotate_verified=" + Boolean(state.NativeRotateVerified),
                "rotate_reconcile_verified=" + Boolean(state.RotateReconcileVerified),
                "rotate_rebuild_verified=" + Boolean(state.RotateRebuildVerified),
                "native_stretch_verified=" + Boolean(state.NativeStretchVerified),
                "stretch_reconcile_verified=" + Boolean(state.StretchReconcileVerified),
                "native_undo_verified=" + Boolean(state.NativeUndoVerified),
                "native_redo_verified=" + Boolean(state.NativeRedoVerified),
                "two_documents_observed=" + Boolean(state.TwoDocumentsObserved),
                "wrong_dwg_reconcile_refused=" + Boolean(state.WrongDwgReconcileRefused),
                "drawing_a_unchanged_while_b_active=" + Boolean(state.DrawingAUnchangedWhileBActive),
                "drawing_b_project_not_created=" + Boolean(state.DrawingBProjectNotCreated),
                "drawing_a_reactivated=" + Boolean(state.DrawingAReactivated),
                "drawing_b_unchanged=" + Boolean(state.DrawingBUnchanged),
                "drawing_b_closed=" + Boolean(state.DrawingBClosed),
                "multi_dwg_isolation_verified=" + Boolean(state.MultiDwgIsolationVerified),
                "final_rebuild_verified=" + Boolean(state.FinalRebuildVerified),
                "cold_reopen_verified=false",
                "source_type=LINE",
                "edit_commands=MOVE_ROTATE_STRETCH_UNDO_REDO",
                "final_length_class=EIGHT_METERS",
                "error_code=NONE"
            };

        private static Dictionary<string, string> ReadPhaseEvidence(string nonce)
        {
            var path = RequiredPath(PhaseVariable, PhaseFileName);
            if (!File.Exists(path)) throw new ProbeFailure("PHASE_EVIDENCE_MISSING");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path, new UTF8Encoding(false, true)))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0 || separator == line.Length - 1)
                    throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
                var key = line.Substring(0, separator);
                if (result.ContainsKey(key)) throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
                result.Add(key, line.Substring(separator + 1));
            }
            foreach (var pair in new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["status"] = "PASS",
                ["schema"] = Schema,
                ["qualification_boundary"] = "LOCAL_004_P01_LINE_ONLY",
                ["nonce"] = nonce,
                ["native_move_verified"] = "true",
                ["move_reconcile_verified"] = "true",
                ["move_rebuild_verified"] = "true",
                ["native_rotate_verified"] = "true",
                ["rotate_reconcile_verified"] = "true",
                ["native_stretch_verified"] = "true",
                ["stretch_reconcile_verified"] = "true",
                ["final_rebuild_verified"] = "true",
                ["source_type"] = "LINE",
                ["edit_commands"] = "MOVE_ROTATE_STRETCH",
                ["final_length_class"] = "EIGHT_METERS",
                ["error_code"] = "NONE"
            })
            {
                if (!result.TryGetValue(pair.Key, out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal))
                    throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
            }
            return result;
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

        private static void RequireExactDocument(Document document)
        {
            var expected = Path.GetFullPath(Environment.GetEnvironmentVariable(DrawingVariable) ?? string.Empty);
            var actual = Path.GetFullPath(document.Name ?? string.Empty);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
        }

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _)) return;
                var path = RequiredPath(ResultVariable, ResultFileName);
                if (File.Exists(path)) return;
                if (IsEnhancedMode())
                {
                    WriteMarkerAtomic(path, new[]
                    {
                        "status=FAIL",
                        "command=QS3DSRNATIVEV26REOPEN",
                        "nonce=" + nonce,
                        "schema=" + EnhancedSchema,
                        "qualification_boundary=LOCAL_018_P03_V26_LINE_LIFECYCLE",
                        "local018_p03_qualified=false",
                        "error_code=SOURCE_RECONCILE_NATIVE_LINE_V26_RUNTIME_FAILED",
                        "failure_phase=" + OneLine(phase),
                        "failure_code=" + OneLine(code)
                    });
                }
                else
                {
                    WriteMarkerAtomic(path, new[]
                    {
                        "status=FAIL",
                        "command=QS3DSRNATIVEREOPEN",
                        "nonce=" + nonce,
                        "schema=" + Schema,
                        "qualification_boundary=LOCAL_004_P01_LINE_ONLY",
                        "production_local004_p01_qualified=false",
                        "error_code=SOURCE_RECONCILE_NATIVE_LINE_RUNTIME_FAILED",
                        "failure_phase=" + OneLine(phase),
                        "failure_code=" + OneLine(code)
                    });
                }
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Native LINE probe marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
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

        private static string OneLine(string? value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        private static string Boolean(bool value) => value ? "true" : "false";

        private enum ExpectedStage { Initial, Moved, Rotated, Stretched }

        private sealed class LineCoordinates
        {
            public LineCoordinates(double startX, double startY, double endX, double endY, double length)
            { StartX = startX; StartY = startY; EndX = endX; EndY = endY; Length = length; }
            public double StartX { get; }
            public double StartY { get; }
            public double EndX { get; }
            public double EndY { get; }
            public double Length { get; }
        }

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce)
            { Document = document; Project = project; Nonce = nonce; }
            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
        }

        private sealed class MultiDwgProbeContext
        {
            public MultiDwgProbeContext(SequenceState state, Document documentA, Document documentB, string drawingA, string drawingB)
            {
                State = state;
                DocumentA = documentA;
                DocumentB = documentB;
                DrawingA = drawingA;
                DrawingB = drawingB;
            }

            public SequenceState State { get; }
            public Document DocumentA { get; }
            public Document DocumentB { get; }
            public string DrawingA { get; }
            public string DrawingB { get; }
        }

        private sealed class SequenceState
        {
            public SequenceState(Document document, string projectId, string ownerId, string sourceHandle, string nonce, string initialGeneratedHandle, bool enhanced)
            {
                Document = document;
                ProjectId = projectId;
                OwnerId = ownerId;
                SourceHandle = sourceHandle;
                Nonce = nonce;
                InitialGeneratedHandle = initialGeneratedHandle;
                Enhanced = enhanced;
            }
            public Document Document { get; }
            public string ProjectId { get; }
            public string OwnerId { get; }
            public string SourceHandle { get; }
            public string Nonce { get; }
            public string InitialGeneratedHandle { get; }
            public bool Enhanced { get; }
            public string MoveGeneratedHandle { get; set; } = string.Empty;
            public string RotateGeneratedHandle { get; set; } = string.Empty;
            public string Phase { get; set; } = "PREPARED";
            public int DrawingBEntityCount { get; set; }
            public bool NativeMoveVerified { get; set; }
            public bool MoveReconcileVerified { get; set; }
            public bool MoveRebuildVerified { get; set; }
            public bool NativeRotateVerified { get; set; }
            public bool RotateReconcileVerified { get; set; }
            public bool RotateRebuildVerified { get; set; }
            public bool NativeStretchVerified { get; set; }
            public bool StretchReconcileVerified { get; set; }
            public bool NativeUndoVerified { get; set; }
            public bool NativeRedoVerified { get; set; }
            public bool TwoDocumentsObserved { get; set; }
            public bool WrongDwgReconcileRefused { get; set; }
            public bool DrawingAUnchangedWhileBActive { get; set; }
            public bool DrawingBProjectNotCreated { get; set; }
            public bool DrawingAReactivated { get; set; }
            public bool DrawingBUnchanged { get; set; }
            public bool DrawingBClosed { get; set; }
            public bool MultiDwgIsolationVerified { get; set; }
            public bool FinalRebuildVerified { get; set; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base("Native LINE source-reconcile probe state rejected.") { Code = code; }
            public string Code { get; }
        }
    }
}
