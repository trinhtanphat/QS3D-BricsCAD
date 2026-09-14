using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class IfcSceneRay
    {
        public IfcSceneRay(double originX, double originY, double originZ, double directionX, double directionY, double directionZ)
        {
            OriginX = QsModelElementSnapshot.Finite(originX, "originX");
            OriginY = QsModelElementSnapshot.Finite(originY, "originY");
            OriginZ = QsModelElementSnapshot.Finite(originZ, "originZ");
            directionX = QsModelElementSnapshot.Finite(directionX, "directionX");
            directionY = QsModelElementSnapshot.Finite(directionY, "directionY");
            directionZ = QsModelElementSnapshot.Finite(directionZ, "directionZ");

            var length = ScaledNorm(directionX, directionY, directionZ);
            if (!Finite(length) || length <= 0d) throw new ArgumentException("Scene pick ray direction must be finite and non-zero.");
            DirectionX = directionX / length;
            DirectionY = directionY / length;
            DirectionZ = directionZ / length;
        }

        public double OriginX { get; private set; }
        public double OriginY { get; private set; }
        public double OriginZ { get; private set; }
        public double DirectionX { get; private set; }
        public double DirectionY { get; private set; }
        public double DirectionZ { get; private set; }

        private static double ScaledNorm(double x, double y, double z)
        {
            var scale = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
            if (scale == 0d) return 0d;
            if (!Finite(scale)) return double.PositiveInfinity;
            x /= scale;
            y /= scale;
            z /= scale;
            return scale * Math.Sqrt((x * x) + (y * y) + (z * z));
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class IfcScenePickHit
    {
        public IfcScenePickHit(string guid, string geometryReference, double distance, double x, double y, double z, int triangleIndex)
        {
            Guid = QsModelElementSnapshot.Require(guid, "guid");
            GeometryReference = QsModelElementSnapshot.Require(geometryReference, "geometryReference");
            Distance = QsModelElementSnapshot.Finite(distance, "distance");
            X = QsModelElementSnapshot.Finite(x, "x");
            Y = QsModelElementSnapshot.Finite(y, "y");
            Z = QsModelElementSnapshot.Finite(z, "z");
            if (Distance < 0d) throw new ArgumentOutOfRangeException("distance");
            if (triangleIndex < 0) throw new ArgumentOutOfRangeException("triangleIndex");
            TriangleIndex = triangleIndex;
        }

        public string Guid { get; private set; }
        public string GeometryReference { get; private set; }
        public double Distance { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public int TriangleIndex { get; private set; }
    }

    /// <summary>
    /// Renderer-neutral IFC scene picking. Meshes are visualization/navigation evidence only;
    /// a hit identifies an IFC element but never becomes authoritative quantity evidence.
    /// </summary>
    public sealed class QuantBimStandaloneScenePicker
    {
        private const double ParallelTolerance = 1e-12;
        private const double AmbiguityTolerance = 1e-9;

        public IfcScenePickHit? PickNearest(IfcStandaloneScene scene, IfcSceneRay ray)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            if (ray == null) throw new ArgumentNullException("ray");
            if (scene.Nodes.Count == 0) return null;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hits = new List<IfcScenePickHit>();
            foreach (var node in scene.Nodes)
            {
                if (node == null) throw new InvalidOperationException("Scene picking encountered a null node.");
                if (!seen.Add(node.Guid)) throw new InvalidOperationException("Scene picking requires unique IFC guid: " + node.Guid + ".");
                if (node.Mesh == null) throw new InvalidOperationException("Scene picking node has no mesh: " + node.Guid + ".");

                IfcScenePickHit? nearestForNode = null;
                for (var i = 0; i < node.Mesh.TriangleIndices.Count; i += 3)
                {
                    var a = node.Mesh.Vertices[node.Mesh.TriangleIndices[i]];
                    var b = node.Mesh.Vertices[node.Mesh.TriangleIndices[i + 1]];
                    var c = node.Mesh.Vertices[node.Mesh.TriangleIndices[i + 2]];
                    var distance = Intersect(ray, a, b, c, node.Guid, i / 3);
                    if (!distance.HasValue) continue;
                    if (nearestForNode == null || distance.Value < nearestForNode.Distance)
                    {
                        nearestForNode = new IfcScenePickHit(
                            node.Guid,
                            node.GeometryReference,
                            distance.Value,
                            ray.OriginX + (ray.DirectionX * distance.Value),
                            ray.OriginY + (ray.DirectionY * distance.Value),
                            ray.OriginZ + (ray.DirectionZ * distance.Value),
                            i / 3);
                    }
                }
                if (nearestForNode != null) hits.Add(nearestForNode);
            }

            if (hits.Count == 0) return null;
            var ordered = hits.OrderBy(x => x.Distance).ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Guid, StringComparer.Ordinal).ToList();
            if (ordered.Count > 1 && NearlyEqual(ordered[0].Distance, ordered[1].Distance))
                throw new InvalidOperationException("Scene pick is ambiguous between IFC elements " + ordered[0].Guid + " and " + ordered[1].Guid + ".");
            return ordered[0];
        }

        private static double? Intersect(IfcSceneRay ray, IfcSceneVertex a, IfcSceneVertex b, IfcSceneVertex c, string guid, int triangleIndex)
        {
            var e1x = b.X - a.X;
            var e1y = b.Y - a.Y;
            var e1z = b.Z - a.Z;
            var e2x = c.X - a.X;
            var e2y = c.Y - a.Y;
            var e2z = c.Z - a.Z;

            var nx = (e1y * e2z) - (e1z * e2y);
            var ny = (e1z * e2x) - (e1x * e2z);
            var nz = (e1x * e2y) - (e1y * e2x);
            var areaScale = Math.Max(Math.Abs(nx), Math.Max(Math.Abs(ny), Math.Abs(nz)));
            if (!Finite(areaScale)) throw new InvalidOperationException("Scene triangle contains non-finite geometry for " + guid + ".");
            if (areaScale <= ParallelTolerance) throw new InvalidOperationException("Scene triangle is degenerate for " + guid + " at triangle " + triangleIndex + ".");

            var px = (ray.DirectionY * e2z) - (ray.DirectionZ * e2y);
            var py = (ray.DirectionZ * e2x) - (ray.DirectionX * e2z);
            var pz = (ray.DirectionX * e2y) - (ray.DirectionY * e2x);
            var determinant = (e1x * px) + (e1y * py) + (e1z * pz);
            if (!Finite(determinant)) throw new InvalidOperationException("Scene triangle intersection is non-finite for " + guid + ".");
            if (Math.Abs(determinant) <= ParallelTolerance) return null;

            var inverse = 1d / determinant;
            var tx = ray.OriginX - a.X;
            var ty = ray.OriginY - a.Y;
            var tz = ray.OriginZ - a.Z;
            var u = ((tx * px) + (ty * py) + (tz * pz)) * inverse;
            if (!Finite(u) || u < 0d || u > 1d) return null;

            var qx = (ty * e1z) - (tz * e1y);
            var qy = (tz * e1x) - (tx * e1z);
            var qz = (tx * e1y) - (ty * e1x);
            var v = ((ray.DirectionX * qx) + (ray.DirectionY * qy) + (ray.DirectionZ * qz)) * inverse;
            if (!Finite(v) || v < 0d || u + v > 1d) return null;

            var distance = ((e2x * qx) + (e2y * qy) + (e2z * qz)) * inverse;
            if (!Finite(distance)) throw new InvalidOperationException("Scene triangle hit distance is non-finite for " + guid + ".");
            return distance >= 0d ? (double?)distance : null;
        }

        private static bool NearlyEqual(double a, double b)
        {
            var scale = Math.Max(1d, Math.Max(Math.Abs(a), Math.Abs(b)));
            return Math.Abs(a - b) <= AmbiguityTolerance * scale;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
