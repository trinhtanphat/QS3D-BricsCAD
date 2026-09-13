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
        public double CenterX { get { return (MinX + MaxX) / 2.0; } }
        public double CenterY { get { return (MinY + MaxY) / 2.0; } }
        public double CenterZ { get { return (MinZ + MaxZ) / 2.0; } }
        public double SizeX { get { return MaxX - MinX; } }
        public double SizeY { get { return MaxY - MinY; } }
        public double SizeZ { get { return MaxZ - MinZ; } }
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

            var halfX = bounds.SizeX / 2.0;
            var halfY = bounds.SizeY / 2.0;
            var halfZ = bounds.SizeZ / 2.0;
            var radius = Math.Sqrt((halfX * halfX) + (halfY * halfY) + (halfZ * halfZ));
            if (!IsFinite(radius)) throw new InvalidOperationException("Viewport radius is not finite.");
            if (radius < MinimumRadius) radius = MinimumRadius;

            var halfFovRadians = fieldOfViewDegrees * Math.PI / 360.0;
            var distance = (radius / Math.Sin(halfFovRadians)) * FitMargin;
            if (!IsFinite(distance) || distance <= radius) throw new InvalidOperationException("Viewport camera distance is invalid.");

            var nearPlane = Math.Max(MinimumRadius, distance - (radius * 2.0));
            var farPlane = distance + (radius * 2.0);
            if (!IsFinite(nearPlane) || !IsFinite(farPlane) || farPlane <= nearPlane)
                throw new InvalidOperationException("Viewport clipping range is invalid.");

            return new IfcViewportFrame(bounds, radius, distance, nearPlane, farPlane);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
