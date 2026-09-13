using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class IfcViewportBounds
    {
        public IfcViewportBounds(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            MinX = QsModelElementSnapshot.Finite(minX, "minX");
            MinY = QsModelElementSnapshot.Finite(minY, "minY");
            MinZ = QsModelElementSnapshot.Finite(minZ, "minZ");
            MaxX = QsModelElementSnapshot.Finite(maxX, "maxX");
            MaxY = QsModelElementSnapshot.Finite(maxY, "maxY");
            MaxZ = QsModelElementSnapshot.Finite(maxZ, "maxZ");
            if (MaxX < MinX || MaxY < MinY || MaxZ < MinZ)
                throw new ArgumentException("Viewport bounds maxima must not be smaller than minima.");
        }

        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MinZ { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public double MaxZ { get; private set; }
        public double CenterX { get { return StableMidpoint(MinX, MaxX); } }
        public double CenterY { get { return StableMidpoint(MinY, MaxY); } }
        public double CenterZ { get { return StableMidpoint(MinZ, MaxZ); } }
        public double SizeX { get { return MaxX - MinX; } }
        public double SizeY { get { return MaxY - MinY; } }
        public double SizeZ { get { return MaxZ - MinZ; } }

        internal double HalfExtentX { get { return StableHalfExtent(MinX, MaxX); } }
        internal double HalfExtentY { get { return StableHalfExtent(MinY, MaxY); } }
        internal double HalfExtentZ { get { return StableHalfExtent(MinZ, MaxZ); } }

        private static double StableMidpoint(double minimum, double maximum)
        {
            // Same-sign subtraction cannot overflow. Opposite-sign addition is performed
            // after halving so valid finite coordinates never overflow just to find center.
            if ((minimum >= 0.0 && maximum >= 0.0) || (minimum <= 0.0 && maximum <= 0.0))
                return minimum + ((maximum - minimum) / 2.0);
            return (minimum / 2.0) + (maximum / 2.0);
        }

        private static double StableHalfExtent(double minimum, double maximum)
        {
            // For opposite signs, maximum - minimum may overflow even though the half-span
            // is finite. Halve each endpoint before subtraction to preserve that result.
            if (minimum < 0.0 && maximum > 0.0)
                return (maximum / 2.0) - (minimum / 2.0);
            return (maximum - minimum) / 2.0;
        }
    }

    public sealed class IfcViewportFrame
    {
        public IfcViewportFrame(IfcViewportBounds bounds, double radius, double distance, double nearPlane, double farPlane)
        {
            Bounds = bounds ?? throw new ArgumentNullException("bounds");
            Radius = QsModelElementSnapshot.Finite(radius, "radius");
            Distance = QsModelElementSnapshot.Finite(distance, "distance");
            NearPlane = QsModelElementSnapshot.Finite(nearPlane, "nearPlane");
            FarPlane = QsModelElementSnapshot.Finite(farPlane, "farPlane");
            if (Radius <= 0.0) throw new ArgumentOutOfRangeException("radius", "Viewport radius must be positive.");
            if (Distance <= Radius) throw new ArgumentOutOfRangeException("distance", "Viewport distance must exceed radius.");
            if (NearPlane <= 0.0 || FarPlane <= NearPlane) throw new ArgumentException("Viewport clipping planes are invalid.");
        }

        public IfcViewportBounds Bounds { get; private set; }
        public double Radius { get; private set; }
        public double Distance { get; private set; }
        public double NearPlane { get; private set; }
        public double FarPlane { get; private set; }
    }

    /// <summary>
    /// Host-neutral projection for deterministic standalone IFC viewer framing.
    /// Geometry is used only for navigation/visualization; it is never quantity evidence.
    /// </summary>
    public sealed class QuantBimStandaloneViewport
    {
        private const double MinimumRadius = 1e-6;
        private const double DefaultFieldOfViewDegrees = 45.0;
        private const double FitMargin = 1.15;

        public IfcViewportFrame FitAll(IfcStandaloneScene scene)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            return Fit(scene.Nodes, DefaultFieldOfViewDegrees);
        }

        public IfcViewportFrame FitSelection(IfcStandaloneScene scene)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            var selected = scene.Nodes.Where(x => x != null && x.Selected).ToList();
            if (selected.Count == 0) throw new InvalidOperationException("Viewport selection is empty.");
            return Fit(selected, DefaultFieldOfViewDegrees);
        }

        public IfcViewportFrame Fit(IEnumerable<IfcSceneNode> nodes, double fieldOfViewDegrees)
        {
            var list = (nodes ?? throw new ArgumentNullException("nodes")).ToList();
            if (list.Count == 0) throw new InvalidOperationException("Viewport scene is empty.");
            if (list.Any(x => x == null)) throw new InvalidOperationException("Viewport scene contains a null node.");
            if (!IsFinite(fieldOfViewDegrees) || fieldOfViewDegrees <= 1.0 || fieldOfViewDegrees >= 179.0)
                throw new ArgumentOutOfRangeException("fieldOfViewDegrees", "Viewport field of view must be finite and between 1 and 179 degrees.");

            var vertices = list.SelectMany(x => x.Mesh == null
                ? throw new InvalidOperationException("Viewport node has no mesh: " + x.Guid + ".")
                : x.Mesh.Vertices).ToList();
            if (vertices.Count == 0) throw new InvalidOperationException("Viewport geometry is empty.");

            var minX = vertices.Min(x => x.X);
            var minY = vertices.Min(x => x.Y);
            var minZ = vertices.Min(x => x.Z);
            var maxX = vertices.Max(x => x.X);
            var maxY = vertices.Max(x => x.Y);
            var maxZ = vertices.Max(x => x.Z);
            var bounds = new IfcViewportBounds(minX, minY, minZ, maxX, maxY, maxZ);

            var halfX = bounds.HalfExtentX;
            var halfY = bounds.HalfExtentY;
            var halfZ = bounds.HalfExtentZ;
            var radius = ScaledHypot(halfX, halfY, halfZ);
            if (!IsFinite(radius)) throw new InvalidOperationException("Viewport radius is not finite.");
            if (radius < MinimumRadius) radius = MinimumRadius;

            var halfFovRadians = fieldOfViewDegrees * Math.PI / 360.0;
            var distanceFactor = FitMargin / Math.Sin(halfFovRadians);
            var distance = radius * distanceFactor;
            if (!IsFinite(distance) || distance <= radius) throw new InvalidOperationException("Viewport camera distance is invalid.");

            var radiusPadding = radius * 2.0;
            if (!IsFinite(radiusPadding)) throw new InvalidOperationException("Viewport clipping range is invalid.");
            var nearPlane = Math.Max(MinimumRadius, distance - radiusPadding);
            var farPlane = distance + radiusPadding;
            if (!IsFinite(nearPlane) || !IsFinite(farPlane) || farPlane <= nearPlane)
                throw new InvalidOperationException("Viewport clipping range is invalid.");

            return new IfcViewportFrame(bounds, radius, distance, nearPlane, farPlane);
        }

        private static double ScaledHypot(double x, double y, double z)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            z = Math.Abs(z);
            var scale = Math.Max(x, Math.Max(y, z));
            if (scale == 0.0) return 0.0;
            if (!IsFinite(scale)) return double.PositiveInfinity;

            var scaledX = x / scale;
            var scaledY = y / scale;
            var scaledZ = z / scale;
            return scale * Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
