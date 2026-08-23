using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Read-only LOCAL-008 P03 verifier for production QS3DDRAWWALLREPEAT and
    /// QS3DDRAWBEAMREPEAT. It never creates,
    /// edits, captures, rebuilds, saves or selects CAD. The production command and BricsCAD's
    /// native Undo/Redo own every mutation; this probe only checks canonical ownership/geometry
    /// and emits sanitized markers under the runner-owned ignored artifact directory.
    /// </summary>
    public sealed class DirectDrawRepeatedRuntimeProbeCommands
    {
        private const string EvidenceDirectoryVariable = "QS3D_REPEAT_EVIDENCE_DIR";
        private const string DrawingVariable = "QS3D_REPEAT_DWG";
        private const string SecondDrawingVariable = "QS3D_REPEAT_SECOND_DWG";
        private const string NonceVariable = "QS3D_REPEAT_NONCE";
        private const string Schema = "QS3D_DIRECT_DRAW_REPEAT_RUNTIME_V1";
        private const double ToleranceM = 1e-8d;
        private static readonly object Sync = new object();
        private static SequenceState? _state;
        private static SequenceArmState? _sequenceArm;
        private static EscapeArmState? _escapeArm;
        private static SwitchArmState? _switchArm;

        [CommandMethod("QS3DREPEATARMSEQUENCE", CommandFlags.Modal)]
        public void ArmSequenceQualification() => ArmSequenceQualification(ElementCategory.Beam);

        [CommandMethod("QS3DREPEATARMWALLSEQUENCE", CommandFlags.Modal)]
        public void ArmWallSequenceQualification() =>
            ArmSequenceQualification(ElementCategory.ArchitecturalWall);

        private static void ArmSequenceQualification(ElementCategory category)
        {
            var control = ControlContext();
            var arm = new SequenceArmState(
                control.Document,
                control.Nonce,
                control.EvidenceDirectory,
                category);
            lock (Sync)
            {
                if (_sequenceArm != null)
                    throw new ProbeFailure("SEQUENCE_ARM_ALREADY_ACTIVE");
                _sequenceArm = arm;
            }

            try
            {
                DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification +=
                    OnSequenceQualificationCompleted;
                arm.SequenceSubscribed = true;
                DirectDrawProfileStripJig.ProfileRenderedForRuntimeQualification += OnProfileRendered;
                arm.ProfileRenderedSubscribed = true;
            }
            catch
            {
                DisarmSequence(arm);
                throw;
            }
        }

        [CommandMethod("QS3DREPEATARMESC", CommandFlags.Modal)]
        public void ArmEscapeQualification()
        {
            var control = ControlContext();
            EscapeArmState? arm = null;
            try
            {
                arm = new EscapeArmState(control.Document, control.Nonce, control.EvidenceDirectory);
                lock (Sync)
                {
                    if (_escapeArm != null)
                        throw new ProbeFailure("ESC_ARM_ALREADY_ACTIVE");
                    _escapeArm = arm;
                }

                DirectDrawRepeatedCommands.SegmentCommittedForRuntimeQualification += OnEscapeSegmentCommitted;
                DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification += OnEscapeSequenceCompleted;
                control.Document.CommandEnded += OnEscapeCommandTerminal;
                control.Document.CommandCancelled += OnEscapeCommandTerminal;
                control.Document.CommandFailed += OnEscapeCommandTerminal;
                arm.Subscribed = true;

                // Submit exactly one production segment, then leave Editor.Drag waiting for the
                // next endpoint. The runner sends an exact-process Windows ESC after the ready marker.
                control.Document.SendStringToExecute(
                    "QS3DDRAWBEAMREPEAT 0,2000 5000,2000 ",
                    true,
                    false,
                    false);
            }
            catch (Exception ex)
            {
                if (arm != null) DisarmEscape(arm);
                var code = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                WriteControlEvidence(control, "esc", "FAIL", -1, code);
                throw;
            }
        }

        [CommandMethod("QS3DREPEATARMSWITCH", CommandFlags.Modal)]
        public void ArmDocumentSwitchQualification()
        {
            var environment = EnvironmentContext();
            var drawingAPath = ExpectedDrawingPath(DrawingVariable);
            var drawingBPath = ExpectedDrawingPath(SecondDrawingVariable);
            var documentA = FindOpenDocument(drawingAPath);
            var documentB = FindOpenDocument(drawingBPath);
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, documentB))
                throw new ProbeFailure("SWITCH_ARM_ACTIVE_DOCUMENT_REJECTED");
            if (ProjectContextCoordinator.TryGetReadOnly(documentB, out _))
                throw new ProbeFailure("SWITCH_SECOND_PROJECT_REJECTED");

            SwitchArmState? arm = null;
            try
            {
                arm = new SwitchArmState(
                    documentA,
                    documentB,
                    environment.Nonce,
                    environment.EvidenceDirectory,
                    NativeFingerprint(documentB));
                lock (Sync)
                {
                    if (_switchArm != null)
                        throw new ProbeFailure("SWITCH_ARM_ALREADY_ACTIVE");
                    _switchArm = arm;
                }

                DirectDrawRepeatedCommands.SegmentCommittedForRuntimeQualification += OnSwitchSegmentCommitted;
                DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification += OnSwitchSequenceCompleted;
                documentA.CommandEnded += OnSwitchCommandTerminal;
                documentA.CommandCancelled += OnSwitchCommandTerminal;
                documentA.CommandFailed += OnSwitchCommandTerminal;
                arm.Subscribed = true;

                Application.DocumentManager.MdiActiveDocument = documentA;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, documentA))
                    throw new ProbeFailure("SWITCH_ACTIVATE_FIRST_REJECTED");
                documentA.SendStringToExecute(
                    "QS3DDRAWBEAMREPEAT 0,4000 5000,4000 ",
                    true,
                    false,
                    false);
            }
            catch (Exception ex)
            {
                if (arm != null) DisarmSwitch(arm);
                var code = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                WriteEvidenceCore(
                    environment.Nonce,
                    environment.EvidenceDirectory,
                    "document_switch",
                    "FAIL",
                    -1,
                    code);
                throw;
            }
        }

        [CommandMethod("QS3DREPEATVERIFYAFTER", CommandFlags.Modal)]
        public void VerifyAfter() => Execute("after", () =>
        {
            var context = Context();
            var arm = SequenceArm(context);
            CaptureAfter(context, arm.Category, arm.WorldDrawCount);
        });

        private static void CaptureAfter(
            ProbeContext context,
            ElementCategory category,
            int worldDrawCount)
        {
            if (worldDrawCount <= 0) throw new ProbeFailure("WORLD_DRAW_NOT_OBSERVED");
            var segments = RequireTwoSegments(context.Document, context.Project, category);
            var undoState = SourceReconcileUndoCoordinator.CaptureSanitizedState(
                context.Document,
                context.Project);
            lock (Sync)
            {
                _state = new SequenceState(
                    context.Document,
                    context.Nonce,
                    segments.Select(x => x.SourceHandle).ToArray(),
                    segments.Select(x => x.GeneratedHandle).ToArray(),
                    undoState,
                    category,
                    worldDrawCount);
            }
            WriteEvidence(
                context,
                PhaseFor(category, "after"),
                "PASS",
                2,
                "NONE",
                category,
                worldDrawCount);
        }

        [CommandMethod("QS3DREPEATVERIFYUNDO", CommandFlags.Modal)]
        public void VerifyUndo() => Execute("undo", () => CaptureUndo(Context()));

        private static void CaptureUndo(ProbeContext context)
        {
            var state = State(context);
            if (context.Project.Elements.Any(x => x.Category == state.Category))
            {
                var undoState = SourceReconcileUndoCoordinator.CaptureSanitizedState(
                    context.Document,
                    context.Project);
                throw new ProbeFailure(
                    "SEMANTIC_UNDO_REJECTED_" + undoState.HistoryState + "_" +
                    undoState.CompareMarkerTo(state.AfterCommandUndoState));
            }
            RequireNoLiveHandles(context.Document, state.SourceHandles, "SOURCE_UNDO_REJECTED");
            RequireNoLiveHandles(context.Document, state.GeneratedHandles, "GENERATED_UNDO_REJECTED");
            state.UndoBoundaryObserved = true;
            WriteEvidence(
                context,
                PhaseFor(state.Category, "undo"),
                "PASS",
                0,
                "NONE",
                state.Category,
                state.WorldDrawCount);
        }

        [CommandMethod("QS3DREPEATVERIFYREDO", CommandFlags.Modal)]
        public void VerifyRedo() => Execute("redo", () =>
        {
            var context = Context();
            var state = State(context);
            if (!state.UndoBoundaryObserved)
                throw new ProbeFailure("UNDO_BOUNDARY_NOT_OBSERVED");
            RequireTwoSegments(context.Document, context.Project, state.Category);
            WriteEvidence(
                context,
                PhaseFor(state.Category, "redo"),
                "PASS",
                2,
                "NONE",
                state.Category,
                state.WorldDrawCount);
        });

        [CommandMethod("QS3DREPEATVERIFYCOLD", CommandFlags.Modal)]
        public void VerifyColdReopen() => Execute("cold_reopen", () =>
        {
            var context = Context();
            RequireTwoBeamSegments(context.Document, context.Project);
            WriteEvidence(context, "cold_reopen", "PASS", 2, "NONE");
        });

        [CommandMethod("QS3DREPEATVERIFYWALLCOLD", CommandFlags.Modal)]
        public void VerifyWallColdReopen() => Execute("wall_cold_reopen", () =>
        {
            var context = Context();
            RequireTwoWallSegments(context.Document, context.Project);
            WriteEvidence(
                context,
                "wall_cold_reopen",
                "PASS",
                2,
                "NONE",
                ElementCategory.ArchitecturalWall);
        });

        [CommandMethod("QS3DREPEATVERIFYUCS", CommandFlags.Modal)]
        public void VerifyPlanarUcs() => Execute("planar_ucs", () =>
        {
            var context = Context();
            RequireTwoPlanarUcsBeamSegments(context.Document, context.Project);
            WriteEvidence(context, "planar_ucs", "PASS", 2, "NONE");
        });

        [CommandMethod("QS3DREPEATVERIFYESC", CommandFlags.Modal)]
        public void VerifyEscape()
        {
            EscapeArmState? arm;
            lock (Sync) arm = _escapeArm;
            try
            {
                Execute("esc", () =>
                {
                    var context = Context();
                    var escape = EscapeResult(context);
                    if (escape.AcceptedSegments != 1 ||
                        !string.Equals(escape.Termination, "ESC_OR_CANCEL", StringComparison.Ordinal))
                        throw new ProbeFailure("ESC_TERMINATION_REJECTED");
                    RequireOneEscBeamSegment(context.Document, context.Project);
                    WriteEvidence(context, "esc", "PASS", 1, "NONE");
                });
            }
            finally
            {
                if (arm != null) DisarmEscape(arm);
            }
        }

        [CommandMethod("QS3DREPEATVERIFYSWITCH", CommandFlags.Modal)]
        public void VerifyDocumentSwitch()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                SwitchArmState arm;
                lock (Sync)
                {
                    arm = _switchArm ?? throw new ProbeFailure("SWITCH_SEQUENCE_STATE_REJECTED");
                }
                if (!ReferenceEquals(document, arm.DocumentB) ||
                    !arm.CommandTerminalObserved ||
                    !arm.RoundTripObserved ||
                    arm.AcceptedSegments != 1 ||
                    !string.Equals(arm.Termination, "DOCUMENT_SWITCH", StringComparison.Ordinal))
                    throw new ProbeFailure("DOCUMENT_SWITCH_TERMINATION_REJECTED");
                if (!ProjectContextCoordinator.TryGetReadOnly(arm.DocumentA, out var projectA))
                    throw new ProbeFailure("SWITCH_FIRST_PROJECT_UNAVAILABLE");
                RequireOneBeamSegment(arm.DocumentA, projectA, 4d, "SWITCH_FIRST_SEGMENT_REJECTED");
                if (ProjectContextCoordinator.TryGetReadOnly(arm.DocumentB, out _))
                    throw new ProbeFailure("SWITCH_SECOND_PROJECT_MUTATED");
                if (!string.Equals(
                    NativeFingerprint(arm.DocumentB),
                    arm.DocumentBFingerprint,
                    StringComparison.Ordinal))
                    throw new ProbeFailure("SWITCH_SECOND_NATIVE_MUTATED");
                WriteEvidenceCore(
                    arm.Nonce,
                    arm.EvidenceDirectory,
                    "document_switch",
                    "PASS",
                    1,
                    "NONE");
                document.Editor.WriteMessage(
                    "\n" + Schema + "|phase=document_switch|status=PASS|error_code=NONE");
                DisarmSwitch(arm);
            }
            catch (Exception ex)
            {
                var errorCode = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                try
                {
                    SwitchArmState? arm;
                    lock (Sync) { arm = _switchArm; }
                    if (arm != null)
                        WriteEvidenceCore(
                            arm.Nonce,
                            arm.EvidenceDirectory,
                            "document_switch",
                            "FAIL",
                            -1,
                            errorCode);
                }
                catch { }
                document.Editor.WriteMessage(
                    "\n" + Schema + "|phase=document_switch|status=FAIL|error_code=" + errorCode);
            }
        }

        private static void Execute(string phase, Action action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                action();
                document.Editor.WriteMessage(
                    "\n" + Schema + "|phase=" + phase + "|status=PASS|error_code=NONE");
            }
            catch (Exception ex)
            {
                var errorCode = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                try
                {
                    var context = Context();
                    WriteEvidence(context, phase, "FAIL", -1, errorCode);
                }
                catch { }
                document.Editor.WriteMessage(
                    "\n" + Schema + "|phase=" + phase + "|status=FAIL|error_code=" + errorCode);
            }
        }

        private static ProbeContext Context()
        {
            var control = ControlContext();
            var document = control.Document;
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_UNAVAILABLE");
            return new ProbeContext(document, project, control.Nonce, control.EvidenceDirectory);
        }

        private static ControlProbeContext ControlContext()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new ProbeFailure("NO_ACTIVE_DOCUMENT");
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (nonce.Length != 32 || nonce.Any(x => !Uri.IsHexDigit(x)))
                throw new ProbeFailure("NONCE_REJECTED");
            var drawingRaw = (Environment.GetEnvironmentVariable(DrawingVariable) ?? string.Empty).Trim();
            if (drawingRaw.Length == 0) throw new ProbeFailure("DRAWING_AFFINITY_REJECTED");
            var expectedDrawing = Path.GetFullPath(drawingRaw);
            var actualDrawing = Path.GetFullPath(document.Name ?? string.Empty);
            if (!string.Equals(actualDrawing, expectedDrawing, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("DRAWING_AFFINITY_REJECTED");
            return new ControlProbeContext(document, nonce, EvidenceDirectory());
        }

        private static string EvidenceDirectory()
        {
            var raw = (Environment.GetEnvironmentVariable(EvidenceDirectoryVariable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("EVIDENCE_DIRECTORY_REJECTED");
            var path = Path.GetFullPath(raw);
            if (!Directory.Exists(path)) throw new ProbeFailure("EVIDENCE_DIRECTORY_REJECTED");
            return path;
        }

        private static SequenceState State(ProbeContext context)
        {
            lock (Sync)
            {
                if (_state == null ||
                    !ReferenceEquals(_state.Document, context.Document) ||
                    !string.Equals(_state.Nonce, context.Nonce, StringComparison.Ordinal))
                    throw new ProbeFailure("SEQUENCE_STATE_REJECTED");
                return _state;
            }
        }

        private static SequenceArmState SequenceArm(ProbeContext context)
        {
            lock (Sync)
            {
                if (_sequenceArm == null ||
                    !ReferenceEquals(_sequenceArm.Document, context.Document) ||
                    !string.Equals(_sequenceArm.Nonce, context.Nonce, StringComparison.Ordinal))
                    throw new ProbeFailure("SEQUENCE_ARM_STATE_REJECTED");
                return _sequenceArm;
            }
        }

        private static void OnProfileRendered()
        {
            lock (Sync)
            {
                if (_sequenceArm == null || !_sequenceArm.ProfileRenderedSubscribed) return;
                _sequenceArm.WorldDrawCount = checked(_sequenceArm.WorldDrawCount + 1);
            }
        }

        private static void OnSequenceQualificationCompleted(
            Document document,
            int acceptedSegments,
            string termination)
        {
            SequenceArmState? arm;
            lock (Sync)
            {
                arm = _sequenceArm;
                if (arm == null || !ReferenceEquals(arm.Document, document)) return;
            }

            var transitionArmed = false;
            try
            {
                if (acceptedSegments != 2 ||
                    !string.Equals(termination, "ENTER", StringComparison.Ordinal))
                    throw new ProbeFailure("SEQUENCE_COMPLETION_REJECTED");
                var context = Context();
                if (!string.Equals(context.Nonce, arm.Nonce, StringComparison.Ordinal) ||
                    !string.Equals(
                        context.EvidenceDirectory,
                        arm.EvidenceDirectory,
                        StringComparison.OrdinalIgnoreCase))
                    throw new ProbeFailure("SEQUENCE_ARM_AFFINITY_REJECTED");
                if (arm.WorldDrawCount <= 0)
                    throw new ProbeFailure("WORLD_DRAW_NOT_OBSERVED");
                CaptureAfter(context, arm.Category, arm.WorldDrawCount);
                ArmUndoBoundary(arm);
                transitionArmed = true;
            }
            catch (Exception ex)
            {
                var errorCode = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                try
                {
                    WriteEvidenceCore(
                        arm.Nonce,
                        arm.EvidenceDirectory,
                        PhaseFor(arm.Category, "after"),
                        "FAIL",
                        -1,
                        errorCode,
                        arm.Category,
                        arm.WorldDrawCount);
                }
                catch { }
            }
            finally { if (!transitionArmed) DisarmSequence(arm); }
        }

        private static void ArmUndoBoundary(SequenceArmState arm)
        {
            if (arm.SequenceSubscribed)
            {
                arm.SequenceSubscribed = false;
                DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification -=
                    OnSequenceQualificationCompleted;
            }
            arm.Document.CommandWillStart += OnSequenceTransitionCommandWillStart;
            arm.TransitionSubscribed = true;
        }

        private static void OnSequenceTransitionCommandWillStart(object sender, CommandEventArgs args)
        {
            if (!IsNativeRedoCommand(args?.GlobalCommandName)) return;

            SequenceArmState? arm;
            lock (Sync) arm = _sequenceArm;
            if (arm == null) return;
            try
            {
                CaptureUndo(Context());
            }
            catch (Exception ex)
            {
                var errorCode = ex is ProbeFailure failure
                    ? failure.Code
                    : "UNEXPECTED_" + ex.GetType().Name.ToUpperInvariant();
                try
                {
                    WriteEvidenceCore(
                        arm.Nonce,
                        arm.EvidenceDirectory,
                        PhaseFor(arm.Category, "undo"),
                        "FAIL",
                        -1,
                        errorCode,
                        arm.Category,
                        arm.WorldDrawCount);
                }
                catch { }
            }
            finally
            {
                DisarmSequence(arm);
            }
        }

        private static bool IsNativeRedoCommand(string? commandName)
        {
            var normalized = (commandName ?? string.Empty).Trim().TrimStart('_', '.');
            return string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase);
        }

        private static void DisarmSequence(SequenceArmState arm)
        {
            if (arm.SequenceSubscribed)
            {
                arm.SequenceSubscribed = false;
                DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification -=
                    OnSequenceQualificationCompleted;
            }
            if (arm.ProfileRenderedSubscribed)
            {
                arm.ProfileRenderedSubscribed = false;
                DirectDrawProfileStripJig.ProfileRenderedForRuntimeQualification -= OnProfileRendered;
            }
            if (arm.TransitionSubscribed)
            {
                arm.TransitionSubscribed = false;
                arm.Document.CommandWillStart -= OnSequenceTransitionCommandWillStart;
            }
            lock (Sync)
            {
                if (ReferenceEquals(_sequenceArm, arm)) _sequenceArm = null;
            }
        }

        private static EscapeResultState EscapeResult(ProbeContext context)
        {
            lock (Sync)
            {
                if (_escapeArm == null ||
                    !ReferenceEquals(_escapeArm.Document, context.Document) ||
                    !string.Equals(_escapeArm.Nonce, context.Nonce, StringComparison.Ordinal) ||
                    !_escapeArm.CommandTerminalObserved)
                    throw new ProbeFailure("ESC_SEQUENCE_STATE_REJECTED");
                return new EscapeResultState(
                    _escapeArm.AcceptedSegments,
                    _escapeArm.Termination ?? string.Empty);
            }
        }

        private static void OnEscapeSegmentCommitted(Document document, int acceptedSegments)
        {
            EscapeArmState? arm;
            lock (Sync)
            {
                arm = _escapeArm;
                if (arm == null || !ReferenceEquals(arm.Document, document)) return;
                arm.AcceptedSegments = acceptedSegments;
                if (acceptedSegments != 1 || arm.ReadyWritten) return;
                arm.ReadyWritten = true;
            }
            WriteControlEvidence(
                new ControlProbeContext(document, arm.Nonce, arm.EvidenceDirectory),
                "esc_ready",
                "READY",
                1,
                "NONE");
        }

        private static void OnEscapeSequenceCompleted(
            Document document,
            int acceptedSegments,
            string termination)
        {
            lock (Sync)
            {
                var arm = _escapeArm;
                if (arm == null || !ReferenceEquals(arm.Document, document)) return;
                arm.AcceptedSegments = acceptedSegments;
                arm.Termination = termination;
            }
        }

        private static void OnEscapeCommandTerminal(object sender, CommandEventArgs args)
        {
            if (!IsRepeatedBeamCommand(args?.GlobalCommandName)) return;

            EscapeArmState? arm;
            lock (Sync)
            {
                arm = _escapeArm;
                if (arm == null) return;
                arm.CommandTerminalObserved = true;
            }
            DisarmEscapeSubscriptions(arm);
            arm.Document.SendStringToExecute("QS3DREPEATVERIFYESC ", true, false, false);
        }

        private static bool IsRepeatedBeamCommand(string? commandName)
        {
            var normalized = (commandName ?? string.Empty).Trim().TrimStart('_', '.');
            return string.Equals(
                normalized,
                "QS3DDRAWBEAMREPEAT",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void DisarmEscape(EscapeArmState arm)
        {
            DisarmEscapeSubscriptions(arm);
            lock (Sync)
            {
                if (ReferenceEquals(_escapeArm, arm)) _escapeArm = null;
            }
        }

        private static void DisarmEscapeSubscriptions(EscapeArmState arm)
        {
            if (!arm.Subscribed) return;
            arm.Subscribed = false;
            DirectDrawRepeatedCommands.SegmentCommittedForRuntimeQualification -= OnEscapeSegmentCommitted;
            DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification -= OnEscapeSequenceCompleted;
            arm.Document.CommandEnded -= OnEscapeCommandTerminal;
            arm.Document.CommandCancelled -= OnEscapeCommandTerminal;
            arm.Document.CommandFailed -= OnEscapeCommandTerminal;
        }

        private static void OnSwitchSegmentCommitted(Document document, int acceptedSegments)
        {
            SwitchArmState? arm;
            lock (Sync)
            {
                arm = _switchArm;
                if (arm == null || !ReferenceEquals(arm.DocumentA, document)) return;
                arm.AcceptedSegments = acceptedSegments;
                if (acceptedSegments != 1 || arm.ReadyWritten) return;
                arm.ReadyWritten = true;
            }
            WriteEvidenceCore(
                arm.Nonce,
                arm.EvidenceDirectory,
                "document_switch_ready",
                "READY",
                1,
                "NONE");
            try
            {
                Application.DocumentManager.MdiActiveDocument = arm.DocumentB;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, arm.DocumentB))
                    throw new ProbeFailure("NATIVE_DOCUMENT_SWITCH_REJECTED");
                Application.DocumentManager.MdiActiveDocument = arm.DocumentA;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, arm.DocumentA))
                    throw new ProbeFailure("NATIVE_DOCUMENT_SWITCH_REJECTED");
                lock (Sync)
                {
                    if (ReferenceEquals(_switchArm, arm)) arm.RoundTripObserved = true;
                }
            }
            catch
            {
                try
                {
                    WriteEvidenceCore(
                        arm.Nonce,
                        arm.EvidenceDirectory,
                        "document_switch",
                        "FAIL",
                        -1,
                        "NATIVE_DOCUMENT_SWITCH_REJECTED");
                }
                catch { }
            }
        }

        private static void OnSwitchSequenceCompleted(
            Document document,
            int acceptedSegments,
            string termination)
        {
            lock (Sync)
            {
                var arm = _switchArm;
                if (arm == null || !ReferenceEquals(arm.DocumentA, document)) return;
                arm.AcceptedSegments = acceptedSegments;
                arm.Termination = termination;
            }
        }

        private static void OnSwitchCommandTerminal(object sender, CommandEventArgs args)
        {
            if (!IsRepeatedBeamCommand(args?.GlobalCommandName)) return;

            SwitchArmState? arm;
            lock (Sync)
            {
                arm = _switchArm;
                if (arm == null) return;
                arm.CommandTerminalObserved = true;
            }
            DisarmSwitchSubscriptions(arm);
            try
            {
                Application.DocumentManager.MdiActiveDocument = arm.DocumentB;
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, arm.DocumentB))
                    throw new ProbeFailure("NATIVE_DOCUMENT_SWITCH_REJECTED");
                arm.DocumentB.SendStringToExecute("QS3DREPEATVERIFYSWITCH ", true, false, false);
            }
            catch
            {
                try
                {
                    WriteEvidenceCore(
                        arm.Nonce,
                        arm.EvidenceDirectory,
                        "document_switch",
                        "FAIL",
                        -1,
                        "NATIVE_DOCUMENT_SWITCH_REJECTED");
                }
                catch { }
            }
        }

        private static void DisarmSwitch(SwitchArmState arm)
        {
            DisarmSwitchSubscriptions(arm);
            lock (Sync)
            {
                if (ReferenceEquals(_switchArm, arm)) _switchArm = null;
            }
        }

        private static void DisarmSwitchSubscriptions(SwitchArmState arm)
        {
            if (!arm.Subscribed) return;
            arm.Subscribed = false;
            DirectDrawRepeatedCommands.SegmentCommittedForRuntimeQualification -= OnSwitchSegmentCommitted;
            DirectDrawRepeatedCommands.SequenceCompletedForRuntimeQualification -= OnSwitchSequenceCompleted;
            arm.DocumentA.CommandEnded -= OnSwitchCommandTerminal;
            arm.DocumentA.CommandCancelled -= OnSwitchCommandTerminal;
            arm.DocumentA.CommandFailed -= OnSwitchCommandTerminal;
        }

        private static EnvironmentProbeContext EnvironmentContext()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (nonce.Length != 32 || nonce.Any(x => !Uri.IsHexDigit(x)))
                throw new ProbeFailure("NONCE_REJECTED");
            return new EnvironmentProbeContext(nonce, EvidenceDirectory());
        }

        private static string ExpectedDrawingPath(string variable)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("DRAWING_AFFINITY_REJECTED");
            return Path.GetFullPath(raw);
        }

        private static Document FindOpenDocument(string expectedPath)
        {
            foreach (Document candidate in Application.DocumentManager)
            {
                string actualPath;
                try { actualPath = Path.GetFullPath(candidate.Name ?? string.Empty); }
                catch { continue; }
                if (string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
            throw new ProbeFailure("OPEN_DOCUMENT_AFFINITY_REJECTED");
        }

        private static string NativeFingerprint(Document document)
        {
            var records = new List<string>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                foreach (ObjectId objectId in modelSpace)
                {
                    var entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var record = entity.GetType().FullName + "|" + objectId.Handle;
                    try
                    {
                        var extents = entity.GeometricExtents;
                        record += "|" + PointToken(extents.MinPoint) + "|" + PointToken(extents.MaxPoint);
                    }
                    catch
                    {
                        record += "|NO_EXTENTS";
                    }
                    records.Add(record);
                }
                transaction.Commit();
            }
            records.Sort(StringComparer.Ordinal);
            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", records)));
                return BitConverter.ToString(digest).Replace("-", string.Empty);
            }
        }

        private static string PointToken(Point3d point) =>
            point.X.ToString("R", CultureInfo.InvariantCulture) + "," +
            point.Y.ToString("R", CultureInfo.InvariantCulture) + "," +
            point.Z.ToString("R", CultureInfo.InvariantCulture);

        private static IReadOnlyList<SegmentState> RequireTwoSegments(
            Document document,
            ProjectState project,
            ElementCategory category)
        {
            if (category == ElementCategory.Beam)
                return RequireTwoBeamSegments(document, project);
            if (category == ElementCategory.ArchitecturalWall)
                return RequireTwoWallSegments(document, project);
            throw new ProbeFailure("SEQUENCE_CATEGORY_REJECTED");
        }

        private static IReadOnlyList<SegmentState> RequireTwoBeamSegments(
            Document document,
            ProjectState project)
        {
            var beams = project.Elements
                .Where(x => x.Category == ElementCategory.Beam)
                .OrderBy(x => x.Id, StringComparer.Ordinal)
                .ToList();
            if (beams.Count != 2) throw new ProbeFailure("SEMANTIC_SEGMENT_COUNT_REJECTED");

            var segments = beams.Select(x => ReadSegment(document, project, x))
                .OrderBy(x => x.MinimumXM)
                .ToList();
            RequireSegment(segments[0], 0d, 5d, 0d, "FIRST_SEGMENT_GEOMETRY_REJECTED");
            RequireSegment(segments[1], 5d, 10d, 0d, "SECOND_SEGMENT_GEOMETRY_REJECTED");
            if (segments.Select(x => x.SourceHandle).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2 ||
                segments.Select(x => x.GeneratedHandle).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
                throw new ProbeFailure("OWNERSHIP_ALIAS_REJECTED");
            return segments;
        }

        private static IReadOnlyList<SegmentState> RequireTwoWallSegments(
            Document document,
            ProjectState project)
        {
            var walls = project.Elements
                .Where(x => x.Category == ElementCategory.ArchitecturalWall)
                .OrderBy(x => x.Id, StringComparer.Ordinal)
                .ToList();
            if (walls.Count != 2) throw new ProbeFailure("WALL_SEMANTIC_SEGMENT_COUNT_REJECTED");

            var segments = walls.Select(x => ReadSegment(document, project, x))
                .OrderBy(x => x.MinimumXM)
                .ToList();
            RequireSegment(segments[0], 0d, 5d, 6d, "WALL_FIRST_SEGMENT_GEOMETRY_REJECTED");
            RequireSegment(segments[1], 5d, 10d, 6d, "WALL_SECOND_SEGMENT_GEOMETRY_REJECTED");
            if (segments.Select(x => x.SourceHandle).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2 ||
                segments.Select(x => x.GeneratedHandle).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
                throw new ProbeFailure("WALL_OWNERSHIP_ALIAS_REJECTED");
            return segments;
        }

        private static void RequireOneEscBeamSegment(Document document, ProjectState project)
        {
            RequireOneBeamSegment(document, project, 2d, "ESC_SEGMENT_GEOMETRY_REJECTED");
        }

        private static void RequireOneBeamSegment(
            Document document,
            ProjectState project,
            double expectedYM,
            string errorCode)
        {
            var beams = project.Elements.Where(x => x.Category == ElementCategory.Beam).ToList();
            if (beams.Count != 1) throw new ProbeFailure(errorCode);
            RequireSegment(ReadSegment(document, project, beams[0]), 0d, 5d, expectedYM, errorCode);
        }

        private static void RequireTwoPlanarUcsBeamSegments(Document document, ProjectState project)
        {
            var beams = project.Elements
                .Where(x => x.Category == ElementCategory.Beam)
                .Select(x => ReadSegment(document, project, x))
                .OrderBy(x => x.MinimumXM)
                .ToList();
            if (beams.Count != 2) throw new ProbeFailure("UCS_SEMANTIC_SEGMENT_COUNT_REJECTED");

            var cosine = Math.Cos(Math.PI / 6d);
            var sine = Math.Sin(Math.PI / 6d);
            RequireSegmentBounds(
                beams[0],
                0d,
                5d * cosine,
                0d,
                5d * sine,
                "UCS_FIRST_SEGMENT_GEOMETRY_REJECTED");
            RequireSegmentBounds(
                beams[1],
                5d * cosine,
                10d * cosine,
                5d * sine,
                10d * sine,
                "UCS_SECOND_SEGMENT_GEOMETRY_REJECTED");
        }

        private static SegmentState ReadSegment(
            Document document,
            ProjectState project,
            ProjectElement element)
        {
            if (element.SourceHandles.Count != 1)
                throw new ProbeFailure("SOURCE_OWNERSHIP_REJECTED");
            var sourceHandle = CadHandleService.NormalizeHexHandle(element.SourceHandles[0])
                ?? throw new ProbeFailure("SOURCE_HANDLE_REJECTED");
            var sourceIds = CadHandleService.Resolve(document, new[] { sourceHandle });
            if (sourceIds.Count != 1) throw new ProbeFailure("SOURCE_LIVENESS_REJECTED");

            Point3d start;
            Point3d end;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Line
                    ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                start = line.StartPoint;
                end = line.EndPoint;
                transaction.Commit();
            }
            if (Math.Abs(CadUnitService.DrawingUnitsToMeters(document, start.Z)) > ToleranceM ||
                Math.Abs(CadUnitService.DrawingUnitsToMeters(document, end.Z)) > ToleranceM)
                throw new ProbeFailure("SOURCE_PLANE_REJECTED");

            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var generatedRaw) ||
                string.IsNullOrWhiteSpace(generatedRaw))
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var generatedHandles = generatedRaw
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CadHandleService.NormalizeHexHandle(x))
                .Where(x => x != null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (generatedHandles.Count != 1 ||
                CadHandleService.GetLiveSolidHandles(document, generatedHandles).Count != 1)
                throw new ProbeFailure("GENERATED_LIVENESS_REJECTED");

            return new SegmentState(
                sourceHandle,
                generatedHandles[0],
                CadUnitService.DrawingUnitsToMeters(document, Math.Min(start.X, end.X)),
                CadUnitService.DrawingUnitsToMeters(document, Math.Max(start.X, end.X)),
                CadUnitService.DrawingUnitsToMeters(document, Math.Min(start.Y, end.Y)),
                CadUnitService.DrawingUnitsToMeters(document, Math.Max(start.Y, end.Y)));
        }

        private static void RequireSegment(
            SegmentState segment,
            double expectedMinimumXM,
            double expectedMaximumXM,
            double expectedYM,
            string errorCode)
        {
            if (Math.Abs(segment.MinimumXM - expectedMinimumXM) > ToleranceM ||
                Math.Abs(segment.MaximumXM - expectedMaximumXM) > ToleranceM ||
                Math.Abs(segment.MinimumYM - expectedYM) > ToleranceM ||
                Math.Abs(segment.MaximumYM - expectedYM) > ToleranceM)
                throw new ProbeFailure(errorCode);
        }

        private static void RequireSegmentBounds(
            SegmentState segment,
            double expectedMinimumXM,
            double expectedMaximumXM,
            double expectedMinimumYM,
            double expectedMaximumYM,
            string errorCode)
        {
            if (Math.Abs(segment.MinimumXM - expectedMinimumXM) > ToleranceM ||
                Math.Abs(segment.MaximumXM - expectedMaximumXM) > ToleranceM ||
                Math.Abs(segment.MinimumYM - expectedMinimumYM) > ToleranceM ||
                Math.Abs(segment.MaximumYM - expectedMaximumYM) > ToleranceM)
                throw new ProbeFailure(errorCode);
        }

        private static void RequireNoLiveHandles(
            Document document,
            IReadOnlyList<string> handles,
            string errorCode)
        {
            if (CadHandleService.GetLiveHandles(document, handles).Count != 0)
                throw new ProbeFailure(errorCode);
        }

        private static void WriteEvidence(
            ProbeContext context,
            string phase,
            string status,
            int semanticSegments,
            string errorCode,
            ElementCategory category = ElementCategory.Beam,
            int worldDrawCount = -1) =>
            WriteEvidenceCore(
                context.Nonce,
                context.EvidenceDirectory,
                phase,
                status,
                semanticSegments,
                errorCode,
                category,
                worldDrawCount);

        private static void WriteEvidenceCore(
            string nonce,
            string evidenceDirectory,
            string phase,
            string status,
            int semanticSegments,
            string errorCode,
            ElementCategory category = ElementCategory.Beam,
            int worldDrawCount = -1)
        {
            var fileName = "repeat-" + phase.Replace('_', '-') + ".txt";
            var path = Path.GetFullPath(Path.Combine(evidenceDirectory, fileName));
            var prefix = evidenceDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("EVIDENCE_PATH_REJECTED");

#if BRICSCAD_V26
            const string hostMajor = "26";
            const string adapter = "QS3D.BricsCAD.V26";
#else
            const string hostMajor = "25";
            const string adapter = "QS3D.BricsCAD.V25";
#endif
            File.WriteAllLines(path, new[]
            {
                "status=" + status,
                "schema=" + Schema,
                "phase=" + phase,
                "nonce=" + nonce,
                "host_major=" + hostMajor,
                "adapter=" + adapter,
                "production_command=" + CommandFor(category),
                "production_category=" + category,
                "semantic_segments=" + semanticSegments.ToString(CultureInfo.InvariantCulture),
                "source_type=LINE",
                "native_type=Solid3d",
                "preview_type=DrawJigProfileStrip",
                "drawjig_worlddraw_count=" + worldDrawCount.ToString(CultureInfo.InvariantCulture),
                "undo_scope=WholeCommand",
                "error_code=" + errorCode
            });
        }

        private static void WriteControlEvidence(
            ControlProbeContext context,
            string phase,
            string status,
            int semanticSegments,
            string errorCode)
        {
            WriteEvidenceCore(
                context.Nonce,
                context.EvidenceDirectory,
                phase,
                status,
                semanticSegments,
                errorCode);
        }

        private static string CommandFor(ElementCategory category)
        {
            if (category == ElementCategory.Beam) return "QS3DDRAWBEAMREPEAT";
            if (category == ElementCategory.ArchitecturalWall) return "QS3DDRAWWALLREPEAT";
            throw new ProbeFailure("SEQUENCE_CATEGORY_REJECTED");
        }

        private static string PhaseFor(ElementCategory category, string phase) =>
            category == ElementCategory.ArchitecturalWall ? "wall_" + phase : phase;

        private sealed class ControlProbeContext
        {
            public ControlProbeContext(Document document, string nonce, string evidenceDirectory)
            {
                Document = document;
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public string EvidenceDirectory { get; }
        }

        private sealed class EnvironmentProbeContext
        {
            public EnvironmentProbeContext(string nonce, string evidenceDirectory)
            {
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
            }

            public string Nonce { get; }
            public string EvidenceDirectory { get; }
        }

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce, string evidenceDirectory)
            {
                Document = document;
                Project = project;
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
            }

            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
            public string EvidenceDirectory { get; }
        }

        private sealed class SegmentState
        {
            public SegmentState(
                string sourceHandle,
                string generatedHandle,
                double minimumXM,
                double maximumXM,
                double minimumYM,
                double maximumYM)
            {
                SourceHandle = sourceHandle;
                GeneratedHandle = generatedHandle;
                MinimumXM = minimumXM;
                MaximumXM = maximumXM;
                MinimumYM = minimumYM;
                MaximumYM = maximumYM;
            }

            public string SourceHandle { get; }
            public string GeneratedHandle { get; }
            public double MinimumXM { get; }
            public double MaximumXM { get; }
            public double MinimumYM { get; }
            public double MaximumYM { get; }
        }

        private sealed class SequenceArmState
        {
            public SequenceArmState(
                Document document,
                string nonce,
                string evidenceDirectory,
                ElementCategory category)
            {
                Document = document;
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
                Category = category;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public string EvidenceDirectory { get; }
            public ElementCategory Category { get; }
            public bool SequenceSubscribed { get; set; }
            public bool TransitionSubscribed { get; set; }
            public bool ProfileRenderedSubscribed { get; set; }
            public int WorldDrawCount { get; set; }
        }

        private sealed class EscapeArmState
        {
            public EscapeArmState(Document document, string nonce, string evidenceDirectory)
            {
                Document = document;
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public string EvidenceDirectory { get; }
            public bool Subscribed { get; set; }
            public bool ReadyWritten { get; set; }
            public bool CommandTerminalObserved { get; set; }
            public int AcceptedSegments { get; set; }
            public string? Termination { get; set; }
        }

        private sealed class EscapeResultState
        {
            public EscapeResultState(int acceptedSegments, string termination)
            {
                AcceptedSegments = acceptedSegments;
                Termination = termination;
            }

            public int AcceptedSegments { get; }
            public string Termination { get; }
        }

        private sealed class SwitchArmState
        {
            public SwitchArmState(
                Document documentA,
                Document documentB,
                string nonce,
                string evidenceDirectory,
                string documentBFingerprint)
            {
                DocumentA = documentA;
                DocumentB = documentB;
                Nonce = nonce;
                EvidenceDirectory = evidenceDirectory;
                DocumentBFingerprint = documentBFingerprint;
            }

            public Document DocumentA { get; }
            public Document DocumentB { get; }
            public string Nonce { get; }
            public string EvidenceDirectory { get; }
            public string DocumentBFingerprint { get; }
            public bool Subscribed { get; set; }
            public bool ReadyWritten { get; set; }
            public bool RoundTripObserved { get; set; }
            public bool CommandTerminalObserved { get; set; }
            public int AcceptedSegments { get; set; }
            public string? Termination { get; set; }
        }

        private sealed class SequenceState
        {
            public SequenceState(
                Document document,
                string nonce,
                IReadOnlyList<string> sourceHandles,
                IReadOnlyList<string> generatedHandles,
                SourceReconcileUndoCoordinator.SanitizedDiagnosticSnapshot afterCommandUndoState,
                ElementCategory category,
                int worldDrawCount)
            {
                Document = document;
                Nonce = nonce;
                SourceHandles = sourceHandles;
                GeneratedHandles = generatedHandles;
                AfterCommandUndoState = afterCommandUndoState ??
                    throw new ArgumentNullException(nameof(afterCommandUndoState));
                Category = category;
                WorldDrawCount = worldDrawCount;
            }

            public Document Document { get; }
            public string Nonce { get; }
            public IReadOnlyList<string> SourceHandles { get; }
            public IReadOnlyList<string> GeneratedHandles { get; }
            public SourceReconcileUndoCoordinator.SanitizedDiagnosticSnapshot AfterCommandUndoState { get; }
            public ElementCategory Category { get; }
            public int WorldDrawCount { get; }
            public bool UndoBoundaryObserved { get; set; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base(code) { Code = code; }
            public string Code { get; }
        }
    }
}
