using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 discriminator for issue #1005. The native
    /// revision token remains private inside SanitizedDiagnosticSnapshot; this
    /// helper publishes only bounded marker comparisons after native Undo/Redo.
    /// Production Source Reconcile behavior is never changed here.
    /// </summary>
    public sealed class SourceReconcilePostUndoMarkerProbeCommands
    {
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NONCE";
        private const string PhaseFileName = "source-reconcile-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_RUNTIME_V1";
        private static readonly object Gate = new object();
        private static MarkerState? _state;

        [CommandMethod("QS3DSRTMARKERBEFOREFINAL", CommandFlags.Modal)]
        public void CaptureBeforeFinal()
        {
            var context = Context();
            var snapshot = SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project);
            lock (Gate) _state = new MarkerState(snapshot);
        }

        [CommandMethod("QS3DSRTMARKERAFTERFINAL", CommandFlags.Modal)]
        public void CaptureAfterFinal()
        {
            var context = Context();
            var snapshot = SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project);
            lock (Gate)
            {
                var state = RequireState();
                state.PostFinal = snapshot;
            }
        }

        [CommandMethod("QS3DSRTMARKERAFTERUNDO", CommandFlags.Modal)]
        public void CaptureAfterUndo()
        {
            var context = Context();
            var current = SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project);
            lock (Gate)
            {
                var state = RequireState();
                var postFinal = state.PostFinal ?? throw new InvalidOperationException("LOCAL-004 post-final marker baseline is missing.");
                state.PostUndoVsPreFinal = current.CompareMarkerTo(state.PreFinal);
                state.PostUndoVsPostFinal = current.CompareMarkerTo(postFinal);
                state.UndoCaptured = true;
            }
        }

        [CommandMethod("QS3DSRTMARKERAFTERREDO", CommandFlags.Modal)]
        public void CaptureAfterRedo()
        {
            var context = Context();
            var current = SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project);
            lock (Gate)
            {
                var state = RequireState();
                var postFinal = state.PostFinal ?? throw new InvalidOperationException("LOCAL-004 post-final marker baseline is missing.");
                state.PostRedoVsPreFinal = current.CompareMarkerTo(state.PreFinal);
                state.PostRedoVsPostFinal = current.CompareMarkerTo(postFinal);
                state.RedoCaptured = true;
            }
        }

        [CommandMethod("QS3DSRTMARKERPUBLISH", CommandFlags.Modal)]
        public void Publish()
        {
            MarkerState state;
            lock (Gate)
            {
                state = RequireState();
                if (!state.UndoCaptured || !state.RedoCaptured)
                    throw new InvalidOperationException("LOCAL-004 post-Undo marker discriminator sequence is incomplete.");
                RequireClassification(state.PostUndoVsPreFinal);
                RequireClassification(state.PostUndoVsPostFinal);
                RequireClassification(state.PostRedoVsPreFinal);
                RequireClassification(state.PostRedoVsPostFinal);
            }

            var path = RequiredPhasePath();
            var nonce = RequiredNonce();
            var lines = File.ReadAllLines(path).ToList();
            RequireExactLine(lines, "status=PASS");
            RequireExactLine(lines, "schema=" + Schema);
            RequireExactLine(lines, "qualification_boundary=LOCAL_004_ONLY");
            RequireExactLine(lines, "nonce=" + nonce);
            foreach (var prefix in new[]
            {
                "post_undo_marker_vs_pre_final_state=",
                "post_undo_marker_vs_post_final_state=",
                "post_redo_marker_vs_pre_final_state=",
                "post_redo_marker_vs_post_final_state="
            })
            {
                if (lines.Any(line => line.StartsWith(prefix, StringComparison.Ordinal)))
                    throw new InvalidOperationException("LOCAL-004 post-Undo marker discriminator was already published.");
            }

            lines.Add("post_undo_marker_vs_pre_final_state=" + state.PostUndoVsPreFinal);
            lines.Add("post_undo_marker_vs_post_final_state=" + state.PostUndoVsPostFinal);
            lines.Add("post_redo_marker_vs_pre_final_state=" + state.PostRedoVsPreFinal);
            lines.Add("post_redo_marker_vs_post_final_state=" + state.PostRedoVsPostFinal);
            ReplaceAtomic(path, lines);
        }

        private static ProbeContext Context()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("LOCAL-004 active document is unavailable.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("LOCAL-004 canonical project is unavailable.");
            return new ProbeContext(document, project);
        }

        private static MarkerState RequireState() =>
            _state ?? throw new InvalidOperationException("LOCAL-004 post-Undo marker discriminator is not initialized.");

        private static void RequireClassification(string value)
        {
            if (!string.Equals(value, "ADVANCED", StringComparison.Ordinal) &&
                !string.Equals(value, "UNCHANGED", StringComparison.Ordinal) &&
                !string.Equals(value, "MISSING_OR_INVALID", StringComparison.Ordinal))
                throw new InvalidOperationException("LOCAL-004 post-Undo marker discriminator classification is invalid.");
        }

        private static string RequiredNonce()
        {
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("LOCAL-004 automation nonce is invalid.");
            return nonce;
        }

        private static string RequiredPhasePath()
        {
            var raw = (Environment.GetEnvironmentVariable(PhaseVariable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new InvalidOperationException("LOCAL-004 phase marker path is missing.");
            var path = Path.GetFullPath(raw);
            var directory = Path.GetDirectoryName(path);
            if (!string.Equals(Path.GetFileName(path), PhaseFileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory) || !File.Exists(path))
                throw new InvalidOperationException("LOCAL-004 phase marker path is invalid.");
            return path;
        }

        private static void RequireExactLine(IReadOnlyCollection<string> lines, string expected)
        {
            if (!lines.Contains(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("LOCAL-004 phase marker contract is invalid.");
        }

        private static void ReplaceAtomic(string path, IReadOnlyCollection<string> lines)
        {
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(line);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Replace(temp, path, null);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project)
            {
                Document = document;
                Project = project;
            }

            public Document Document { get; }
            public ProjectState Project { get; }
        }

        private sealed class MarkerState
        {
            public MarkerState(SourceReconcileUndoCoordinator.SanitizedDiagnosticSnapshot preFinal)
            {
                PreFinal = preFinal ?? throw new ArgumentNullException(nameof(preFinal));
            }

            public SourceReconcileUndoCoordinator.SanitizedDiagnosticSnapshot PreFinal { get; }
            public SourceReconcileUndoCoordinator.SanitizedDiagnosticSnapshot? PostFinal { get; set; }
            public string PostUndoVsPreFinal { get; set; } = "MISSING_OR_INVALID";
            public string PostUndoVsPostFinal { get; set; } = "MISSING_OR_INVALID";
            public string PostRedoVsPreFinal { get; set; } = "MISSING_OR_INVALID";
            public string PostRedoVsPostFinal { get; set; } = "MISSING_OR_INVALID";
            public bool UndoCaptured { get; set; }
            public bool RedoCaptured { get; set; }
        }
    }
}
