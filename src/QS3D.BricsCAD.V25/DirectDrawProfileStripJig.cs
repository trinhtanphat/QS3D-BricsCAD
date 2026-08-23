using System;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.GraphicsInterface;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Database-free transient used by both the production repeated Direct Draw commands and
    /// the licensed lifecycle probe. Editor.GetPoint values are normalized to WCS before this
    /// jig is created; AcquirePoint/base-point values are then kept in WCS. Only the strip-offset
    /// calculation is performed in the snapshotted UCS-local XY plane.
    /// </summary>
    internal sealed class DirectDrawProfileStripJig : DrawJig
    {
        // Optional, read-only qualification hook. It observes the real hosted WorldDraw callback
        // and is exception-isolated so test evidence cannot own preview behavior.
        internal static event Action? ProfileRenderedForRuntimeQualification;

        private readonly Point3d _startWcs;
        private readonly double _widthDrawingUnits;
        private readonly Matrix3d _ucsToWcs;
        private readonly Matrix3d _wcsToUcs;
        private readonly string _prompt;
        private Point3d _endWcs;
        private bool _hasSample;

        internal DirectDrawProfileStripJig(
            Point3d startWcs,
            double widthDrawingUnits,
            Matrix3d ucsToWcs,
            string prompt = "\nNext endpoint (Enter/ESC exits): ")
        {
            _startWcs = startWcs;
            _endWcs = startWcs;
            _widthDrawingUnits = widthDrawingUnits;
            _ucsToWcs = ucsToWcs;
            _wcsToUcs = ucsToWcs.Inverse();
            _prompt = prompt;
            LastPromptStatus = PromptStatus.None;
        }

        internal Point3d EndPoint => _endWcs;
        internal PromptStatus LastPromptStatus { get; private set; }
        internal bool HasUsableEndPoint => _hasSample && _startWcs.DistanceTo(_endWcs) > 1e-9d;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions(_prompt)
            {
                BasePoint = _startWcs,
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

            if (_hasSample && result.Value.DistanceTo(_endWcs) <= 1e-9d)
                return SamplerStatus.NoChange;

            _endWcs = result.Value;
            _hasSample = true;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw worldDraw)
        {
            if (!_hasSample || _startWcs.DistanceTo(_endWcs) <= 1e-9d)
                return true;

            var localStart = _startWcs.TransformBy(_wcsToUcs);
            var localEnd = _endWcs.TransformBy(_wcsToUcs);
            var dx = localEnd.X - localStart.X;
            var dy = localEnd.Y - localStart.Y;
            var planLength = System.Math.Sqrt((dx * dx) + (dy * dy));
            if (planLength <= 1e-12d)
                return true;

            var half = _widthDrawingUnits / 2d;
            var offset = new Vector3d(-dy / planLength * half, dx / planLength * half, 0d);

            var a = (localStart + offset).TransformBy(_ucsToWcs);
            var b = (localEnd + offset).TransformBy(_ucsToWcs);
            var c = (localEnd - offset).TransformBy(_ucsToWcs);
            var d = (localStart - offset).TransformBy(_ucsToWcs);

            worldDraw.Geometry.WorldLine(a, b);
            worldDraw.Geometry.WorldLine(b, c);
            worldDraw.Geometry.WorldLine(c, d);
            worldDraw.Geometry.WorldLine(d, a);
            worldDraw.Geometry.WorldLine(_startWcs, _endWcs);
            NotifyProfileRendered();
            return true;
        }

        private static void NotifyProfileRendered()
        {
            var observer = ProfileRenderedForRuntimeQualification;
            if (observer == null) return;
            try { observer(); }
            catch { }
        }
    }
}
