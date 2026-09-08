$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$source=Get-Content (Join-Path $PSScriptRoot 'Local022BrepVerifier.cs') -Raw
# Compile the actual independent topology oracle. Only the proprietary native
# adapter is replaced by a throwing boundary; invoking it cannot fabricate PASS.
$start=$source.IndexOf('        private static Snapshot Read(Solid3d solid)')
$end=$source.IndexOf('        private static bool Same(Point3d a,Point3d b)')
if($start -lt 0 -or $end -le $start){throw 'FAIL: native adapter boundary missing.'}
$source=$source.Substring(0,$start)+"        private static Snapshot Read(Solid3d solid) { throw new NotSupportedException(); }`n"+$source.Substring($end)
# Give these boundary doubles private names so this test composes with the
# section replay in one PowerShell process; the oracle logic remains unchanged.
Add-Type -TypeDefinition (($source+@'
#nullable disable
namespace Teigha.BoundaryRepresentation { }
namespace Teigha.DatabaseServices { public sealed class Solid3d {} }
namespace Teigha.Geometry {
 public struct Point3d {
  public double X,Y,Z;public Point3d(double x,double y,double z){X=x;Y=y;Z=z;}
  public static Vector3d operator-(Point3d a,Point3d b)=>new Vector3d(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
 }
 public struct Vector3d {
  public double X,Y,Z;public Vector3d(double x,double y,double z){X=x;Y=y;Z=z;}
  public double Length=>System.Math.Sqrt(X*X+Y*Y+Z*Z);
  public Vector3d GetNormal(){double l=Length;return new Vector3d(X/l,Y/l,Z/l);}
  public double DotProduct(Vector3d b)=>X*b.X+Y*b.Y+Z*b.Z;
  public Vector3d CrossProduct(Vector3d b)=>new Vector3d(Y*b.Z-Z*b.Y,Z*b.X-X*b.Z,X*b.Y-Y*b.X);
 }
}
public static class Local022BrepReplay {
 static Teigha.Geometry.Point3d P(double x,double y,double z)=>new Teigha.Geometry.Point3d(x+10,y-20,z+4);
 static QS3D.LocalQualification.BrepVerifier.Snapshot Fixture(bool taper){
  var points=taper?new[]{P(-1.5,-1,0),P(1.5,-1,0),P(1.5,1,0),P(-1.5,1,0),
   P(-1.5,-1,1),P(1.5,-1,1),P(1.5,1,1),P(-1.5,1,1),
   P(-.5,-.5,2),P(.5,-.5,2),P(.5,.5,2),P(-.5,.5,2)}:
   new[]{P(-1,-1,0),P(1,-1,0),P(1,1,0),P(-1,1,0),P(-1,-1,1),P(1,-1,1),P(1,1,1),P(-1,1,1)};
  var quads=taper?new[]{new[]{0,3,2,1},new[]{8,9,10,11},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7},
   new[]{4,5,9,8},new[]{5,6,10,9},new[]{6,7,11,10},new[]{7,4,8,11}}:
   new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7}};
  double[] areas=taper?new[]{6d,1d,3d,2d,3d,2d,System.Math.Sqrt(5),1.5*System.Math.Sqrt(2),System.Math.Sqrt(5),1.5*System.Math.Sqrt(2)}:new[]{4d,4d,2d,2d,2d,2d};
  var s=new QS3D.LocalQualification.BrepVerifier.Snapshot{Complexes=1,Shells=1,ShellFaces=quads.Length};
  s.Vertices.AddRange(points);
  var seen=new System.Collections.Generic.HashSet<string>();
  for(int f=0;f<quads.Length;f++){
   var q=quads[f];var face=new QS3D.LocalQualification.BrepVerifier.FaceData{Planar=true,Area=areas[f],PlanePoint=points[q[0]],
    OutwardNormal=(points[q[1]]-points[q[0]]).CrossProduct(points[q[3]]-points[q[0]]).GetNormal()};
   var loop=new System.Collections.Generic.List<QS3D.LocalQualification.BrepVerifier.EdgeData>();
   for(int i=0;i<4;i++){
    int a=q[i],b=q[(i+1)%4];var edge=new QS3D.LocalQualification.BrepVerifier.EdgeData{A=points[a],B=points[b],Linear=true};
    loop.Add(edge);string key=System.Math.Min(a,b)+":"+System.Math.Max(a,b);if(seen.Add(key))s.Edges.Add(edge);
   }
   loop.Reverse();face.Loops.Add(loop);s.Faces.Add(face);
  }
  s.Vertices.Reverse();s.Edges.Reverse();s.Faces.Reverse();return s;
 }
 static System.Collections.Generic.List<Teigha.Geometry.Point3d[]> Expected(bool t)=>
  QS3D.LocalQualification.BrepVerifier.Expected(t?3:2,2,1,1,1,t?1:0,P(0,0,0));
 static void Reject(string code,System.Action<QS3D.LocalQualification.BrepVerifier.Snapshot> mutation){
  var s=Fixture(true);mutation(s);string error=null;
  try{QS3D.LocalQualification.BrepVerifier.VerifySnapshot(s,Expected(true));}catch(System.Exception e){error=e.Message;}
  if(error!=code)throw new System.Exception("Missing/wrong rejection: "+code+" actual="+error);
 }
 public static void Run(){
  foreach(bool taper in new[]{false,true}){
   var s=Fixture(taper);var expected=Expected(taper);
   if(expected.Count!=(taper?10:6)||s.Edges.Count!=(taper?20:12)||s.Vertices.Count!=(taper?12:8))throw new System.Exception("Fixture cardinality drift");
   QS3D.LocalQualification.BrepVerifier.VerifySnapshot(s,expected);
  }
  Reject("brep_single_exterior_shell",s=>s.Complexes=2);
  Reject("brep_single_exterior_shell",s=>s.Shells=2);
  Reject("brep_single_exterior_shell",s=>s.ExteriorShells=false);
  Reject("brep_vertex_inventory",s=>s.Vertices.RemoveAt(0));
  Reject("brep_vertex_inventory",s=>s.Vertices[0]=s.Vertices[1]);
  Reject("brep_vertex_position",s=>s.Vertices[0]=P(99,0,0));
  Reject("brep_vertex_position",s=>s.Vertices[0]=P(double.NaN,0,0));
  Reject("brep_curved_edge",s=>s.Edges[0].Linear=false);
  Reject("brep_edge_inventory",s=>s.Edges.RemoveAt(0));
  Reject("brep_edge_inventory",s=>s.Edges[0]=s.Edges[1]);
  Reject("brep_face_inventory",s=>s.Faces.RemoveAt(0));
  Reject("brep_face_inventory",s=>s.ShellFaces=1);
  Reject("brep_face_loop",s=>s.Faces[0].Planar=false);
  Reject("brep_face_loop",s=>s.Faces[0].ExteriorLoops=false);
  Reject("brep_face_loop",s=>s.Faces[0].Loops.Add(s.Faces[0].Loops[0]));
  Reject("brep_face_loop",s=>s.Faces[0].Loops[0].RemoveAt(0));
  Reject("brep_face_edges",s=>s.Faces[0].Loops[0][0]=s.Faces[0].Loops[0][1]);
  Reject("brep_face_identity",s=>s.Faces[0]=s.Faces[1]);
  Reject("brep_face_orientation",s=>s.Faces[0].OutwardNormal=new Teigha.Geometry.Vector3d(0,0,1));
  Reject("brep_face_orientation",s=>s.Faces[0].OutwardNormal=new Teigha.Geometry.Vector3d(double.NaN,0,0));
  Reject("brep_face_plane",s=>s.Faces[0].PlanePoint=P(99,99,99));
  Reject("brep_face_area",s=>s.Faces[0].Area+=0.1);
  Reject("brep_face_area",s=>s.Faces[0].Area=double.NaN);
  bool unsupported=false;try{QS3D.LocalQualification.BrepVerifier.Expected(3,2,1,2,1,1,P(0,0,0));}catch(System.InvalidOperationException e){unsupported=e.Message=="brep_fixture_one_axis_unsupported";}
  if(!unsupported)throw new System.Exception("Unqualified one-axis topology admitted");
 }
}
'@).Replace('Teigha.','Local022BrepDoubles.Teigha.'))
[Local022BrepReplay]::Run()
Write-Output 'PASS: actual topology oracle validates independent box/taper fixtures regardless of order; extra shells, malformed vertex/edge/face inventories, loops, planes, orientation and areas fail closed. No CAD/native runtime claimed.'
