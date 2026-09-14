using System;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Renderer-neutral perspective projection used to turn standalone viewport pointer coordinates
    /// into world-space scene rays. Reconstructed scene geometry remains visualization/navigation only
    /// and never becomes authoritative quantity evidence.
    /// </summary>
    public sealed class QuantBimStandaloneScreenRayProjector
    {
        private const double MinimumFieldOfViewDegrees = 1.0;
        private const double MaximumFieldOfViewDegrees = 179.0;
        private const double BasisTolerance = 1e-12;

        public IfcSceneRay ProjectPerspective(
            IfcCameraNavigationState camera,
            double viewportWidth,
            double viewportHeight,
            double pointerX,
            double pointerY,
            double verticalFieldOfViewDegrees)
        {
            if (camera == null) throw new ArgumentNullException("camera");
            viewportWidth = Finite(viewportWidth, "viewportWidth");
            viewportHeight = Finite(viewportHeight, "viewportHeight");
            pointerX = Finite(pointerX, "pointerX");
            pointerY = Finite(pointerY, "pointerY");
            verticalFieldOfViewDegrees = Finite(verticalFieldOfViewDegrees, "verticalFieldOfViewDegrees");

            if (viewportWidth <= 0.0 || viewportHeight <= 0.0)
                throw new ArgumentOutOfRangeException("viewportWidth", "Viewport dimensions must be positive.");
            if (pointerX < 0.0 || pointerX > viewportWidth || pointerY < 0.0 || pointerY > viewportHeight)
                throw new ArgumentOutOfRangeException("pointerX", "Viewport pointer must lie inside the admitted viewport rectangle.");
            if (verticalFieldOfViewDegrees < MinimumFieldOfViewDegrees || verticalFieldOfViewDegrees > MaximumFieldOfViewDegrees)
                throw new ArgumentOutOfRangeException("verticalFieldOfViewDegrees", "Perspective field of view must be between 1 and 179 degrees.");

            var aspect = viewportWidth / viewportHeight;
            if (!IsFinite(aspect) || aspect <= 0.0)
                throw new InvalidOperationException("Viewport aspect ratio is invalid.");

            var forwardX = camera.TargetX - camera.PositionX;
            var forwardY = camera.TargetY - camera.PositionY;
            var forwardZ = camera.TargetZ - camera.PositionZ;
            Normalize(ref forwardX, ref forwardY, ref forwardZ, "camera forward axis");

            var upX = camera.UpX;
            var upY = camera.UpY;
            var upZ = camera.UpZ;
            Normalize(ref upX, ref upY, ref upZ, "camera up axis");

            var rightX = (forwardY * upZ) - (forwardZ * upY);
            var rightY = (forwardZ * upX) - (forwardX * upZ);
            var rightZ = (forwardX * upY) - (forwardY * upX);
            Normalize(ref rightX, ref rightY, ref rightZ, "camera right axis");

            // Re-orthogonalize up so renderer hosts cannot accumulate skew from a merely approximate up vector.
            upX = (rightY * forwardZ) - (rightZ * forwardY);
            upY = (rightZ * forwardX) - (rightX * forwardZ);
            upZ = (rightX * forwardY) - (rightY * forwardX);
            Normalize(ref upX, ref upY, ref upZ, "orthogonal camera up axis");

            // Continuous viewport convention: origin is the top-left edge; (width/2,height/2) is the optical axis.
            var ndcX = ((2.0 * pointerX) / viewportWidth) - 1.0;
            var ndcY = 1.0 - ((2.0 * pointerY) / viewportHeight);
            if (!IsFinite(ndcX) || !IsFinite(ndcY))
                throw new InvalidOperationException("Viewport normalized coordinates are invalid.");

            var halfFovRadians = verticalFieldOfViewDegrees * Math.PI / 360.0;
            var tangent = Math.Tan(halfFovRadians);
            if (!IsFinite(tangent) || tangent <= 0.0)
                throw new InvalidOperationException("Perspective field of view produced an invalid projection scale.");

            var horizontal = ndcX * tangent * aspect;
            var vertical = ndcY * tangent;
            if (!IsFinite(horizontal) || !IsFinite(vertical))
                throw new InvalidOperationException("Perspective pointer projection is invalid.");

            var directionX = forwardX + (rightX * horizontal) + (upX * vertical);
            var directionY = forwardY + (rightY * horizontal) + (upY * vertical);
            var directionZ = forwardZ + (rightZ * horizontal) + (upZ * vertical);

            return new IfcSceneRay(
                camera.PositionX,
                camera.PositionY,
                camera.PositionZ,
                directionX,
                directionY,
                directionZ);
        }

        private static double Finite(double value, string name)
        {
            if (!IsFinite(value)) throw new ArgumentException("Screen-ray " + name + " must be finite.", name);
            return value;
        }

        private static void Normalize(ref double x, ref double y, ref double z, string name)
        {
            var length = IfcCameraNavigationState.Norm(x, y, z);
            if (!IsFinite(length) || length <= BasisTolerance)
                throw new InvalidOperationException("Screen-ray projection encountered a degenerate " + name + ".");
            x /= length;
            y /= length;
            z /= length;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
