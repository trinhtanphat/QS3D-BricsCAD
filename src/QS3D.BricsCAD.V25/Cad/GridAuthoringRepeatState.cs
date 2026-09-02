using System;
using System.Runtime.CompilerServices;
using Bricscad.ApplicationServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GridAuthoringRepeatState
    {
        private sealed class State
        {
            internal readonly object Sync = new object();
            internal RectangularTemplate Rectangular;
            internal RadialTemplate Radial;
        }

        private sealed class RectangularTemplate
        {
            internal int UCount;
            internal int VCount;
            internal double USpacingM;
            internal double VSpacingM;
        }

        private sealed class RadialTemplate
        {
            internal int RayCount;
            internal double RayStepDegrees;
            internal double InnerRadiusM;
            internal double FirstRingRadiusM;
            internal int RingCount;
            internal double RingSpacingM;
        }

        // Weak document keys prevent repeat state from extending the lifetime of a closed DWG.
        private static readonly ConditionalWeakTable<Document, State> States = new ConditionalWeakTable<Document, State>();

        private static State GetState(Document document)
        {
            return States.GetValue(document, _ => new State());
        }

        internal static void RememberRectangular(Document document, RectangularGridNativeRequest request)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (request == null) throw new ArgumentNullException(nameof(request));
            var state = GetState(document);
            lock (state.Sync)
            {
                state.Rectangular = new RectangularTemplate
                {
                    UCount = request.UCount,
                    VCount = request.VCount,
                    USpacingM = request.USpacingM,
                    VSpacingM = request.VSpacingM
                };
            }
        }

        internal static void RememberRadial(Document document, RadialGridNativeRequest request)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (request == null) throw new ArgumentNullException(nameof(request));
            var state = GetState(document);
            lock (state.Sync)
            {
                state.Radial = new RadialTemplate
                {
                    RayCount = request.RayCount,
                    RayStepDegrees = request.RayStepDegrees,
                    InnerRadiusM = request.InnerRadiusM,
                    FirstRingRadiusM = request.FirstRingRadiusM,
                    RingCount = request.RingCount,
                    RingSpacingM = request.RingSpacingM
                };
            }
        }

        internal static bool TryCreateRectangularRequest(
            Document document,
            string systemKey,
            Point3d origin,
            Point3d uDirectionPoint,
            out RectangularGridNativeRequest request)
        {
            request = null;
            if (document == null || !States.TryGetValue(document, out var state)) return false;
            lock (state.Sync)
            {
                var template = state.Rectangular;
                if (template == null) return false;
                request = new RectangularGridNativeRequest
                {
                    SystemKey = systemKey ?? string.Empty,
                    OriginDrawing = origin,
                    UDirectionPointDrawing = uDirectionPoint,
                    UCount = template.UCount,
                    VCount = template.VCount,
                    USpacingM = template.USpacingM,
                    VSpacingM = template.VSpacingM
                };
                return true;
            }
        }

        internal static bool TryCreateRadialRequest(
            Document document,
            string systemKey,
            Point3d center,
            Point3d firstRayDirectionPoint,
            out RadialGridNativeRequest request)
        {
            request = null;
            if (document == null || !States.TryGetValue(document, out var state)) return false;
            lock (state.Sync)
            {
                var template = state.Radial;
                if (template == null) return false;
                request = new RadialGridNativeRequest
                {
                    SystemKey = systemKey ?? string.Empty,
                    CenterDrawing = center,
                    FirstRayDirectionPointDrawing = firstRayDirectionPoint,
                    RayCount = template.RayCount,
                    RayStepDegrees = template.RayStepDegrees,
                    InnerRadiusM = template.InnerRadiusM,
                    FirstRingRadiusM = template.FirstRingRadiusM,
                    RingCount = template.RingCount,
                    RingSpacingM = template.RingSpacingM
                };
                return true;
            }
        }
    }
}
