#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.LocalQualification
{
    // Qualification fixture oracle only; never builds or repairs product geometry.
    internal static class BrepVerifier
    {
        internal sealed class EdgeData
        {
            internal Point3d A, B;
            internal bool Linear;
        }
        internal sealed class FaceData
        {
            internal Point3d PlanePoint;
            internal Vector3d OutwardNormal;
            internal bool Planar;
            internal double Area;
            internal List<List<EdgeData>> Loops = new List<List<EdgeData>>();
            internal bool ExteriorLoops = true;
        }
        internal sealed class Snapshot
        {
            internal int Complexes, Shells, ShellFaces;
            internal bool ExteriorShells = true;
            internal List<Point3d> Vertices = new List<Point3d>();
            internal List<EdgeData> Edges = new List<EdgeData>();
            internal List<FaceData> Faces = new List<FaceData>();
        }

        internal static void Verify(Solid3d solid, double l1, double w1, double l2,
            double w2, double h1, double h2, Point3d center) =>
            VerifySnapshot(Read(solid), Expected(l1, w1, l2, w2, h1, h2, center));

        // Canonical outward face winding for a box or a prism plus a two-axis
        // tapered top. One-axis taper is outside this qualification fixture set.
        internal static List<Point3d[]> Expected(double l1, double w1, double l2,
            double w2, double h1, double h2, Point3d center)
        {
            Require(new[] {l1,w1,l2,w2,h1,h2,center.X,center.Y,center.Z}.All(Finite) &&
                l1 > 0 && w1 > 0 && l2 > 0 && w2 > 0 && h1 > 0 && h2 >= 0 &&
                l2 <= l1 && w2 <= w1, "brep_fixture_input");
            bool taper = h2 > 0 && l2 < l1 && w2 < w1;
            Require(h2 == 0 || taper || (l2 == l1 && w2 == w1), "brep_fixture_one_axis_unsupported");
            Point3d[] Ring(double l,double w,double z) => new[] {
                new Point3d(center.X-l/2,center.Y-w/2,z),new Point3d(center.X+l/2,center.Y-w/2,z),
                new Point3d(center.X+l/2,center.Y+w/2,z),new Point3d(center.X-l/2,center.Y+w/2,z)};
            var rings = new List<Point3d[]> { Ring(l1,w1,center.Z) };
            if(taper) rings.Add(Ring(l1,w1,center.Z+h1));
            rings.Add(Ring(taper?l2:l1,taper?w2:w1,center.Z+h1+h2));
            var faces = new List<Point3d[]> {new[]{rings[0][3],rings[0][2],rings[0][1],rings[0][0]},rings[rings.Count-1]};
            for(int r=0;r<rings.Count-1;r++)
                for(int i=0;i<4;i++)
                    faces.Add(new[]{rings[r][i],rings[r][(i+1)%4],rings[r+1][(i+1)%4],rings[r+1][i]});
            return faces;
        }

        internal static void VerifySnapshot(Snapshot actual, List<Point3d[]> faces)
        {
            Require(actual.Complexes == 1 && actual.Shells == 1 && actual.ExteriorShells,
                "brep_single_exterior_shell");
            var vertices = new List<Point3d>();
            foreach(var p in faces.SelectMany(x=>x)) if(!vertices.Any(x=>Same(x,p))) vertices.Add(p);
            int Index(Point3d p) { var found=vertices.FindAll(x=>Same(x,p));
                Require(found.Count==1,"brep_vertex_position");return vertices.FindIndex(x=>Same(x,p)); }
            string Key(Point3d a,Point3d b) {int i=Index(a),j=Index(b);Require(i!=j,"brep_degenerate_edge");return Math.Min(i,j)+":"+Math.Max(i,j);}
            HashSet<string> FaceKeys(Point3d[] face) => new HashSet<string>(Enumerable.Range(0,4).Select(i=>Key(face[i],face[(i+1)%4])));
            var expectedFaces=faces.Select(FaceKeys).ToList();
            var expectedEdges=new HashSet<string>(expectedFaces.SelectMany(x=>x));
            Require(actual.Vertices.Count==vertices.Count && actual.Vertices.Select(Index).Distinct().Count()==vertices.Count,
                "brep_vertex_inventory");
            var globalEdges=new HashSet<string>();
            foreach(var edge in actual.Edges) {
                Require(edge.Linear,"brep_curved_edge");
                var key=Key(edge.A,edge.B);Require(expectedEdges.Contains(key)&&globalEdges.Add(key),"brep_edge_inventory");
            }
            Require(globalEdges.SetEquals(expectedEdges),"brep_edge_inventory");
            Require(actual.Faces.Count==faces.Count && actual.ShellFaces==faces.Count,"brep_face_inventory");
            var matched=new HashSet<int>(); var incidences=expectedEdges.ToDictionary(x=>x,x=>0);
            foreach(var face in actual.Faces) {
                Require(face.Planar && face.ExteriorLoops && face.Loops.Count==1 && face.Loops[0].Count==4,
                    "brep_face_loop");
                var keys=new HashSet<string>();
                foreach(var edge in face.Loops[0]) {
                    Require(edge.Linear,"brep_curved_edge");var key=Key(edge.A,edge.B);
                    Require(globalEdges.Contains(key)&&keys.Add(key),"brep_face_edges");incidences[key]++;
                }
                int index=expectedFaces.FindIndex(x=>x.SetEquals(keys));
                Require(index>=0 && matched.Add(index),"brep_face_identity");
                var expected=faces[index];
                var cross=(expected[1]-expected[0]).CrossProduct(expected[3]-expected[0]);
                var normal=cross.GetNormal();
                Require(Finite(face.OutwardNormal.Length) && face.OutwardNormal.Length>0 &&
                    Near(face.OutwardNormal.GetNormal().DotProduct(normal),1),"brep_face_orientation");
                Require(Finite(face.PlanePoint.X)&&Finite(face.PlanePoint.Y)&&Finite(face.PlanePoint.Z)&&
                    Math.Abs((face.PlanePoint-expected[0]).DotProduct(normal))<=1e-7,"brep_face_plane");
                // Two triangles: independent rectangular/trapezoidal planar area.
                double area=((expected[1]-expected[0]).CrossProduct(expected[2]-expected[0]).Length+
                    (expected[2]-expected[0]).CrossProduct(expected[3]-expected[0]).Length)/2;
                Require(Near(face.Area,area),"brep_face_area");
            }
            Require(incidences.Values.All(x=>x==2) && vertices.Count-globalEdges.Count+actual.Faces.Count==2,
                "brep_closed_manifold_incidence");
        }

        private static Snapshot Read(Solid3d solid)
        {
            var result=new Snapshot();
            using(var brep=new Brep(solid)) {
                Require(!brep.IsNull,"brep_missing");
                foreach(var complex in brep.Complexes) using(complex) { result.Complexes++; }
                foreach(var shell in brep.Shells) using(shell) {
                    result.Shells++;result.ExteriorShells &= shell.ShellType==ShellType.ShellExterior;
                    foreach(var face in shell.Faces) using(face) {result.ShellFaces++;}
                }
                foreach(var vertex in brep.Vertices) using(vertex) { result.Vertices.Add(vertex.Point); }
                foreach(var edge in brep.Edges) using(edge) { result.Edges.Add(ReadEdge(edge)); }
                foreach(var face in brep.Faces) using(face) {
                    var data=new FaceData {Area=face.GetArea()};
                    using(var surface=face.Surface) {
                        if(surface is PlanarEntity plane) ReadPlane(plane,face.IsOrientToSurface,data);
                        else if(surface is ExternalBoundedSurface external && external.IsPlane)
                            using(var basis=external.BaseSurface) {
                                if(basis is PlanarEntity basePlane) ReadPlane(basePlane,face.IsOrientToSurface,data);
                            }
                    }
                    foreach(var loop in face.Loops) using(loop) {
                        data.ExteriorLoops &= loop.LoopType==LoopType.LoopExterior;
                        var edges=new List<EdgeData>();
                        foreach(var edge in loop.Edges) using(edge) {edges.Add(ReadEdge(edge));}
                        data.Loops.Add(edges);
                    }
                    result.Faces.Add(data);
                }
            }
            return result;
        }

        private static void ReadPlane(PlanarEntity plane,bool same,FaceData data)
        {data.Planar=true;data.PlanePoint=plane.PointOnPlane;data.OutwardNormal=same?plane.Normal:-plane.Normal;}
        private static EdgeData ReadEdge(Edge edge)
        {
            using(var a=edge.Vertex1) using(var b=edge.Vertex2) using(var curve=edge.Curve) {
                Line3d? line=null;
                try {
                    var tolerance=new Tolerance(1e-7,1e-7);
                    bool linear=curve.IsLinear(out line,tolerance) && line!=null &&
                        line.IsOn(a.Point,tolerance) && line.IsOn(b.Point,tolerance);
                    return new EdgeData {A=a.Point,B=b.Point,Linear=linear};
                } finally {line?.Dispose();}
            }
        }
        private static bool Same(Point3d a,Point3d b)=>Finite(a.X)&&Finite(a.Y)&&Finite(a.Z)&&
            Math.Abs(a.X-b.X)<=1e-7&&Math.Abs(a.Y-b.Y)<=1e-7&&Math.Abs(a.Z-b.Z)<=1e-7;
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static bool Near(double a,double b)=>Finite(a)&&Finite(b)&&Math.Abs(a-b)<=1e-7*Math.Max(1,Math.Abs(b));
        private static void Require(bool value,string code){if(!value)throw new InvalidOperationException(code);}
    }
}
