using System;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class IfcCameraNavigationState
    {
        public IfcCameraNavigationState(
            double positionX, double positionY, double positionZ,
            double targetX, double targetY, double targetZ,
            double upX, double upY, double upZ,
            double sceneRadius, double nearPlane, double farPlane)
        {
            PositionX = Finite(positionX, "positionX");
            PositionY = Finite(positionY, "positionY");
            PositionZ = Finite(positionZ, "positionZ");
            TargetX = Finite(targetX, "targetX");
            TargetY = Finite(targetY, "targetY");
            TargetZ = Finite(targetZ, "targetZ");
            UpX = Finite(upX, "upX");
            UpY = Finite(upY, "upY");
            UpZ = Finite(upZ, "upZ");
            SceneRadius = Finite(sceneRadius, "sceneRadius");
            NearPlane = Finite(nearPlane, "nearPlane");
            FarPlane = Finite(farPlane, "farPlane");

            if (SceneRadius <= 0.0) throw new ArgumentOutOfRangeException("sceneRadius");
            if (NearPlane <= 0.0 || FarPlane <= NearPlane) throw new ArgumentException("Camera clipping planes are invalid.");

            var viewX = TargetX - PositionX;
            var viewY = TargetY - PositionY;
            var viewZ = TargetZ - PositionZ;
            if (Norm(viewX, viewY, viewZ) <= 0.0) throw new ArgumentException("Camera position and target must differ.");
            if (Norm(UpX, UpY, UpZ) <= 0.0) throw new ArgumentException("Camera up vector must be non-zero.");
            var crossX = (viewY * UpZ) - (viewZ * UpY);
            var crossY = (viewZ * UpX) - (viewX * UpZ);
            var crossZ = (viewX * UpY) - (viewY * UpX);
            if (Norm(crossX, crossY, crossZ) <= 1e-12) throw new ArgumentException("Camera up vector must not be parallel to view direction.");
        }

        public double PositionX { get; private set; }
        public double PositionY { get; private set; }
        public double PositionZ { get; private set; }
        public double TargetX { get; private set; }
        public double TargetY { get; private set; }
        public double TargetZ { get; private set; }
        public double UpX { get; private set; }
        public double UpY { get; private set; }
        public double UpZ { get; private set; }
        public double SceneRadius { get; private set; }
        public double NearPlane { get; private set; }
        public double FarPlane { get; private set; }

        public double Distance
        {
            get { return Norm(PositionX - TargetX, PositionY - TargetY, PositionZ - TargetZ); }
        }

        internal static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("Camera " + name + " must be finite.", name);
            return value;
        }

        internal static double Norm(double x, double y, double z)
        {
            var scale = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
            if (scale == 0.0) return 0.0;
            if (double.IsNaN(scale) || double.IsInfinity(scale)) return double.PositiveInfinity;
            x /= scale;
            y /= scale;
            z /= scale;
            return scale * Math.Sqrt((x * x) + (y * y) + (z * z));
        }
    }

    /// <summary>
    /// Renderer-neutral camera navigation for the standalone IFC workbench.
    /// Scene geometry participates in visualization/navigation only and never becomes authoritative quantity evidence.
    /// </summary>
    public sealed class QuantBimStandaloneNavigation
    {
        private const double PoleLimitDegrees = 89.0;
        private const double MinimumDistanceFactor = 1.05;
        private const double MaximumDistanceFactor = 1000000.0;
        private const double ClipPaddingFactor = 2.0;
        private const double MinimumNearPlane = 1e-6;

        public IfcCameraNavigationState Focus(IfcViewportFrame frame)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            var invSqrt3 = 1.0 / Math.Sqrt(3.0);
            var targetX = frame.Bounds.CenterX;
            var targetY = frame.Bounds.CenterY;
            var targetZ = frame.Bounds.CenterZ;
            var positionX = targetX + (frame.Distance * invSqrt3);
            var positionY = targetY - (frame.Distance * invSqrt3);
            var positionZ = targetZ + (frame.Distance * invSqrt3);
            return Build(positionX, positionY, positionZ, targetX, targetY, targetZ, frame.Radius);
        }

        public IfcCameraNavigationState Orbit(IfcCameraNavigationState state, double yawDegrees, double pitchDegrees)
        {
            if (state == null) throw new ArgumentNullException("state");
            yawDegrees = IfcCameraNavigationState.Finite(yawDegrees, "yawDegrees");
            pitchDegrees = IfcCameraNavigationState.Finite(pitchDegrees, "pitchDegrees");

            var ox = state.PositionX - state.TargetX;
            var oy = state.PositionY - state.TargetY;
            var oz = state.PositionZ - state.TargetZ;
            var distance = IfcCameraNavigationState.Norm(ox, oy, oz);
            if (!IsFinite(distance) || distance <= 0.0) throw new InvalidOperationException("Camera orbit distance is invalid.");

            var yaw = Math.Atan2(oy, ox) + DegreesToRadians(yawDegrees);
            var ratio = Clamp(oz / distance, -1.0, 1.0);
            var pitch = Math.Asin(ratio) + DegreesToRadians(pitchDegrees);
            var pitchLimit = DegreesToRadians(PoleLimitDegrees);
            pitch = Clamp(pitch, -pitchLimit, pitchLimit);

            var horizontal = distance * Math.Cos(pitch);
            var px = state.TargetX + (horizontal * Math.Cos(yaw));
            var py = state.TargetY + (horizontal * Math.Sin(yaw));
            var pz = state.TargetZ + (distance * Math.Sin(pitch));
            return Build(px, py, pz, state.TargetX, state.TargetY, state.TargetZ, state.SceneRadius);
        }

        public IfcCameraNavigationState Pan(IfcCameraNavigationState state, double rightDistance, double upDistance)
        {
            if (state == null) throw new ArgumentNullException("state");
            rightDistance = IfcCameraNavigationState.Finite(rightDistance, "rightDistance");
            upDistance = IfcCameraNavigationState.Finite(upDistance, "upDistance");

            double fx, fy, fz, rx, ry, rz, ux, uy, uz;
            Basis(state.PositionX, state.PositionY, state.PositionZ, state.TargetX, state.TargetY, state.TargetZ,
                out fx, out fy, out fz, out rx, out ry, out rz, out ux, out uy, out uz);

            var dx = (rx * rightDistance) + (ux * upDistance);
            var dy = (ry * rightDistance) + (uy * upDistance);
            var dz = (rz * rightDistance) + (uz * upDistance);
            return Build(
                state.PositionX + dx, state.PositionY + dy, state.PositionZ + dz,
                state.TargetX + dx, state.TargetY + dy, state.TargetZ + dz,
                state.SceneRadius);
        }

        public IfcCameraNavigationState Dolly(IfcCameraNavigationState state, double distanceScale)
        {
            if (state == null) throw new ArgumentNullException("state");
            distanceScale = IfcCameraNavigationState.Finite(distanceScale, "distanceScale");
            if (distanceScale <= 0.0) throw new ArgumentOutOfRangeException("distanceScale", "Camera dolly scale must be positive.");

            var ox = state.PositionX - state.TargetX;
            var oy = state.PositionY - state.TargetY;
            var oz = state.PositionZ - state.TargetZ;
            var distance = IfcCameraNavigationState.Norm(ox, oy, oz);
            if (!IsFinite(distance) || distance <= 0.0) throw new InvalidOperationException("Camera dolly distance is invalid.");

            var minimum = state.SceneRadius * MinimumDistanceFactor;
            var maximum = state.SceneRadius * MaximumDistanceFactor;
            if (!IsFinite(minimum) || !IsFinite(maximum) || maximum <= minimum)
                throw new InvalidOperationException("Camera dolly limits are invalid.");
            var nextDistance = Clamp(distance * distanceScale, minimum, maximum);
            if (!IsFinite(nextDistance)) throw new InvalidOperationException("Camera dolly result is invalid.");

            var ratio = nextDistance / distance;
            return Build(
                state.TargetX + (ox * ratio),
                state.TargetY + (oy * ratio),
                state.TargetZ + (oz * ratio),
                state.TargetX, state.TargetY, state.TargetZ,
                state.SceneRadius);
        }

        private static IfcCameraNavigationState Build(
            double positionX, double positionY, double positionZ,
            double targetX, double targetY, double targetZ,
            double sceneRadius)
        {
            double fx, fy, fz, rx, ry, rz, ux, uy, uz;
            Basis(positionX, positionY, positionZ, targetX, targetY, targetZ,
                out fx, out fy, out fz, out rx, out ry, out rz, out ux, out uy, out uz);

            var distance = IfcCameraNavigationState.Norm(positionX - targetX, positionY - targetY, positionZ - targetZ);
            var padding = sceneRadius * ClipPaddingFactor;
            if (!IsFinite(padding)) throw new InvalidOperationException("Camera clipping padding is invalid.");
            var nearPlane = Math.Max(MinimumNearPlane, distance - padding);
            var farPlane = distance + padding;
            if (!IsFinite(nearPlane) || !IsFinite(farPlane) || farPlane <= nearPlane)
                throw new InvalidOperationException("Camera clipping range is invalid.");

            return new IfcCameraNavigationState(
                positionX, positionY, positionZ,
                targetX, targetY, targetZ,
                ux, uy, uz,
                sceneRadius, nearPlane, farPlane);
        }

        private static void Basis(
            double positionX, double positionY, double positionZ,
            double targetX, double targetY, double targetZ,
            out double fx, out double fy, out double fz,
            out double rx, out double ry, out double rz,
            out double ux, out double uy, out double uz)
        {
            fx = targetX - positionX;
            fy = targetY - positionY;
            fz = targetZ - positionZ;
            Normalize(ref fx, ref fy, ref fz, "view direction");

            rx = fy;
            ry = -fx;
            rz = 0.0;
            Normalize(ref rx, ref ry, ref rz, "camera right axis");

            ux = (ry * fz) - (rz * fy);
            uy = (rz * fx) - (rx * fz);
            uz = (rx * fy) - (ry * fx);
            Normalize(ref ux, ref uy, ref uz, "camera up axis");
        }

        private static void Normalize(ref double x, ref double y, ref double z, string name)
        {
            var length = IfcCameraNavigationState.Norm(x, y, z);
            if (!IsFinite(length) || length <= 1e-12) throw new InvalidOperationException("Degenerate " + name + ".");
            x /= length;
            y /= length;
            z /= length;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
