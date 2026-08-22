using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.GraphicsInterface;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// LOCAL-008 P02 source-prep probe for BricsCAD V25 DrawJig/editor lifecycle.
    ///
    /// This deliberately does not create, erase, capture, persist, or regenerate any CAD/QS3D
    /// object. It exists to prove the transient primitive (profile preview + repeated clicks +
    /// Enter/ESC termination) on licensed V25 before that primitive is wired into production
    /// Direct Draw. Keeping the probe database-free makes preview-residue failures observable and
    /// prevents a second authoring/ownership model from being introduced by qualification code.
    /// </summary>
    public sealed class DirectDrawJigRuntimeProbeCommands
    {
        private const string Schema = "QS3D_DIRECT_DRAW_JIG_RUNTIME_V1";
        private const int MinimumQualifiedSegments = 3;

        [CommandMethod("QS3DPROBEDIRECTDRAWJIG", CommandFlags.Modal)]
        public void ProbeDirectDrawJig()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var editor = document.Editor;
            try
            {
                var widthOptions = new PromptDoubleOptions("\nLOCAL-008 P02 preview width in metres <0.3>: ")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = true,
                    DefaultValue = 0.3d,
                    UseDefaultValue = true
                };
                var widthResult = editor.GetDouble(widthOptions);
                if (widthResult.Status != PromptStatus.OK && widthResult.Status != PromptStatus.None)
                {
                    WriteResult(editor, 0, "CANCEL_BEFORE_START");
                    return;
                }

                var widthM = widthResult.Status == PromptStatus.OK ? widthResult.Value : 0.3d;
                if (double.IsNaN(widthM) || double.IsInfinity(widthM) || widthM <= 0d)
                    throw new InvalidOperationException("Preview width must be a positive finite metre value.");

                var widthDrawingUnits = Cad.CadGeometryGuard.ToDrawingUnits(document, widthM, "LOCAL-008 P02 preview width");
                if (double.IsNaN(widthDrawingUnits) || double.IsInfinity(widthDrawingUnits) || widthDrawingUnits <= 0d)
                    throw new InvalidOperationException("Preview width could not be converted to positive drawing units.");

                var startOptions = new PromptPointOptions("\nLOCAL-008 P02 first point (Enter/ESC exits): ")
                {
                    AllowNone = true
                };
                var first = editor.GetPoint(startOptions);
                if (first.Status != PromptStatus.OK)
                {
                    WriteResult(editor, 0, first.Status == PromptStatus.None ? "ENTER_BEFORE_START" : "CANCEL_BEFORE_START");
                    return;
                }

                RequireSameDocument(document);
                var ucs = editor.CurrentUserCoordinateSystem;
                var start = first.Value;
                var accepted = 0;
                var termination = "UNKNOWN";

                while (true)
                {
                    RequireSameDocument(document);
                    var jig = new ProfileStripJig(start, widthDrawingUnits, ucs);
                    var drag = editor.Drag(jig);
                    RequireSameDocument(document);

                    if (drag.Status != PromptStatus.OK)
                    {
                        termination = jig.LastPromptStatus == PromptStatus.None ? "ENTER" : "ESC_OR_CANCEL";
                        break;
                    }

                    if (!jig.HasUsableEndPoint)
                    {
                        termination = "DEGENERATE_REJECTED";
                        break;
                    }

                    accepted++;
                    start = jig.EndPoint;
                    editor.WriteMessage(
                        "\nLOCAL-008 P02 accepted transient segment #" + accepted +
                        ". Click next endpoint to continue; Enter/ESC exits. No entity has been committed.");
                }

                WriteResult(editor, accepted, termination);
            }
            catch (Exception ex)
            {
                // Sanitized marker intentionally excludes paths, handles, project IDs and stack traces.
                editor.WriteMessage("\n" + Schema + "|qualified=false|error_class=" + ex.GetType().Name + "|persistent_writes=0");
            }
        }

        private static void RequireSameDocument(Document expected)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, expected))
                throw new InvalidOperationException("ActiveDocumentChanged");
        }

        private static void WriteResult(Editor editor, int acceptedSegments, string termination)
        {
            var qualifiedCandidate =
                acceptedSegments >= MinimumQualifiedSegments &&
                (termination == "ENTER" || termination == "ESC_OR_CANCEL");

            editor.WriteMessage(
                "\n" + Schema +
                "|qualified_candidate=" + (qualifiedCandidate ? "true" : "false") +
                "|accepted_segments=" + acceptedSegments +
                "|minimum_segments=" + MinimumQualifiedSegments +
                "|termination=" + termination +
                "|preview_model=DrawJigProfileStrip" +
                "|persistent_writes=0" +
                "|ownership_writes=0");
        }

        private sealed class ProfileStripJig : DrawJig
        {
            private readonly Point3d _start;
            private readonly double _widthDrawingUnits;
            private readonly Matrix3d _ucs;
            private Point3d _end;
            private bool _hasSample;

            internal ProfileStripJig(Point3d start, double widthDrawingUnits, Matrix3d ucs)
            {
                _start = start;
                _end = start;
                _widthDrawingUnits = widthDrawingUnits;
                _ucs = ucs;
                LastPromptStatus = PromptStatus.None;
            }

            internal Point3d EndPoint => _end;
            internal PromptStatus LastPromptStatus { get; private set; }
            internal bool HasUsableEndPoint => _hasSample && _start.DistanceTo(_end) > 1e-9d;

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var options = new JigPromptPointOptions("\nNext endpoint (Enter/ESC exits): ")
                {
                    BasePoint = _start,
                    UseBasePoint = true,
                    UserInputControls =
                        UserInputControls.Accept3dCoordinates |
                        UserInputControls.GovernedByOrthoMode |
                        UserInputControls.GovernedByUCSDetect |
                        UserInputControls.NullResponseAccepted
                };

                var result = prompts.AcquirePoint(options);
                LastPromptStatus = result.Status;
                if (result.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                if (_hasSample && result.Value.DistanceTo(_end) <= 1e-9d)
                    return SamplerStatus.NoChange;

                _end = result.Value;
                _hasSample = true;
                return SamplerStatus.OK;
            }

            protected override bool WorldDraw(WorldDraw worldDraw)
            {
                if (!_hasSample || _start.DistanceTo(_end) <= 1e-9d)
                    return true;

                var dx = _end.X - _start.X;
                var dy = _end.Y - _start.Y;
                var planLength = Math.Sqrt((dx * dx) + (dy * dy));
                if (planLength <= 1e-12d)
                    return true;

                var half = _widthDrawingUnits / 2d;
                var offset = new Vector3d(-dy / planLength * half, dx / planLength * half, 0d);

                var a = (_start + offset).TransformBy(_ucs);
                var b = (_end + offset).TransformBy(_ucs);
                var c = (_end - offset).TransformBy(_ucs);
                var d = (_start - offset).TransformBy(_ucs);
                var centerStart = _start.TransformBy(_ucs);
                var centerEnd = _end.TransformBy(_ucs);

                worldDraw.Geometry.WorldLine(a, b);
                worldDraw.Geometry.WorldLine(b, c);
                worldDraw.Geometry.WorldLine(c, d);
                worldDraw.Geometry.WorldLine(d, a);
                worldDraw.Geometry.WorldLine(centerStart, centerEnd);
                return true;
            }
        }
    }
}
