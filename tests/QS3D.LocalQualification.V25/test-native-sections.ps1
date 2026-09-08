$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$source=Get-Content (Join-Path $PSScriptRoot 'Local022SectionVerifier.cs') -Raw
# Compile the actual adapter/oracle against controlled CAD boundary doubles.
# No host, drawing, native DLL, registry, input or historical receipt is used.
Add-Type -TypeDefinition ($source + @'
#nullable disable
namespace Teigha.Geometry {
 public struct Point3d { public double X,Y,Z; public Point3d(double x,double y,double z){X=x;Y=y;Z=z;} }
 public struct Vector3d { public double Z; public static Vector3d ZAxis => new Vector3d{Z=1}; }
 public sealed class Plane:System.IDisposable {
  public Point3d Origin; public static int Created,Disposed;
  public Plane(Point3d p,Vector3d v){if(v.Z!=1)throw new System.Exception("wrong normal");Origin=p;Created++;}
  public void Dispose(){Disposed++;}
 }
}
namespace Teigha.DatabaseServices {
 public class DBObject:System.IDisposable { public bool Disposed; public void Dispose(){if(Disposed)throw new System.Exception("double dispose");Disposed=true;} }
 public sealed class Line:DBObject {public Teigha.Geometry.Point3d StartPoint,EndPoint;}
 public struct Extents3d {public Teigha.Geometry.Point3d MinPoint,MaxPoint;}
 public sealed class DBObjectCollection:System.Collections.Generic.List<DBObject>,System.IDisposable {public void Dispose(){}}
 public sealed class Region:DBObject {
  public bool IsNull; public double Area,Perimeter; public Extents3d GeometricExtents;
  public DBObject[] Edges; public bool ExplodeThrows;
  public void Explode(DBObjectCollection target){target.AddRange(Edges);if(ExplodeThrows)throw new System.Exception("test_explode");}
 }
 public sealed class Solid3d {
  public System.Collections.Generic.List<Region> Regions=new System.Collections.Generic.List<Region>();
  public double[] Z; public int Calls; public System.Func<int,Region,Region> Mutation;
  public Region GetSection(Teigha.Geometry.Plane plane){
   if(Calls>=Z.Length || System.Math.Abs(plane.Origin.Z-Z[Calls])>1e-12 || plane.Origin.X!=10 || plane.Origin.Y!=-20)throw new System.Exception("wrong sample plane");
   int i=Calls++;return Mutation==null?Regions[i]:Mutation(i,Regions[i]);
  }
 }
}
public static class Local022SectionsReplay {
 static Teigha.Geometry.Point3d P(double x,double y,double z)=>new Teigha.Geometry.Point3d(x,y,z);
 static Teigha.DatabaseServices.Region R(double l,double w,double z){
  var p=new[]{P(10-l/2,-20-w/2,z),P(10+l/2,-20-w/2,z),P(10+l/2,-20+w/2,z),P(10-l/2,-20+w/2,z)};
  // Deliberately reversed/reordered edges: acceptance cannot depend on traversal order.
  return new Teigha.DatabaseServices.Region {Area=l*w,Perimeter=2*(l+w),
   GeometricExtents=new Teigha.DatabaseServices.Extents3d{MinPoint=p[0],MaxPoint=p[2]},
   Edges=new Teigha.DatabaseServices.DBObject[]{
    new Teigha.DatabaseServices.Line{StartPoint=p[2],EndPoint=p[1]},
    new Teigha.DatabaseServices.Line{StartPoint=p[0],EndPoint=p[3]},
    new Teigha.DatabaseServices.Line{StartPoint=p[1],EndPoint=p[0]},
    new Teigha.DatabaseServices.Line{StartPoint=p[3],EndPoint=p[2]}}};
 }
 static Teigha.DatabaseServices.Solid3d Fixture(bool tapered){
  var s=new Teigha.DatabaseServices.Solid3d();
  s.Z=tapered?new[]{4.25,4.5,4.75,5.25,5.5,5.75}:new[]{4.25,4.5,4.75};
  if(tapered)s.Regions.AddRange(new[]{R(3,2,4.25),R(3,2,4.5),R(3,2,4.75),R(2.5,1.75,5.25),R(2,1.5,5.5),R(1.5,1.25,5.75)});
  else s.Regions.AddRange(new[]{R(2,2,4.25),R(2,2,4.5),R(2,2,4.75)});
  return s;
 }
 static void RunSubject(Teigha.DatabaseServices.Solid3d s,bool tapered){
  QS3D.LocalQualification.SectionVerifier.Verify(s,tapered?3:2,2,1,1,1,tapered?1:0,P(10,-20,4));
 }
 static void Clean(Teigha.DatabaseServices.Solid3d s,int exploded){
  for(int i=0;i<s.Calls;i++){
   if(!s.Regions[i].Disposed)throw new System.Exception("region leaked");
   if(i<exploded)foreach(var e in s.Regions[i].Edges)if(!e.Disposed)throw new System.Exception("edge leaked");
  }
  if(Teigha.Geometry.Plane.Created!=Teigha.Geometry.Plane.Disposed)throw new System.Exception("plane leaked");
 }
 static void Reject(string code,System.Action<Teigha.DatabaseServices.Region> mutate,bool exploded=false){
  var s=Fixture(true); mutate(s.Regions[0]); string error=null;
  try{RunSubject(s,true);}catch(System.Exception e){error=e.Message;}
  if(error!=code || s.Calls!=1)throw new System.Exception("Wrong/missing rejection: "+code+" actual="+error);
  Clean(s,exploded?1:0);
 }
 public static void Run(){
  foreach(bool tapered in new[]{false,true}){
   var s=Fixture(tapered);RunSubject(s,tapered);
   if(s.Calls!=(tapered?6:3))throw new System.Exception("sample coverage skipped");Clean(s,s.Calls);
  }
  Reject("section_missing",r=>r.IsNull=true);
  Reject("section_area",r=>r.Area+=0.1);
  Reject("section_area",r=>r.Area=double.NaN);
  Reject("section_perimeter",r=>r.Perimeter+=0.1);
  Reject("section_perimeter",r=>r.Perimeter=double.PositiveInfinity);
  Reject("section_bounds",r=>r.GeometricExtents.MinPoint.X+=0.01);
  Reject("section_bounds",r=>r.GeometricExtents.MaxPoint.Z+=0.01);
  Reject("section_bounds",r=>r.GeometricExtents.MaxPoint.Y=double.NaN);
  Reject("section_edge_count",r=>r.Edges=new[]{r.Edges[0],r.Edges[1],r.Edges[2]},true);
  Reject("section_edge_not_line",r=>r.Edges[0]=new Teigha.DatabaseServices.DBObject(),true);
  Reject("section_edge_boundary",r=>((Teigha.DatabaseServices.Line)r.Edges[0]).StartPoint.X+=0.1,true);
  Reject("section_edge_boundary",r=>{var a=(Teigha.DatabaseServices.Line)r.Edges[0];var b=(Teigha.DatabaseServices.Line)r.Edges[1];a.StartPoint=b.StartPoint;a.EndPoint=b.EndPoint;},true);
  Reject("test_explode",r=>r.ExplodeThrows=true,true);
  var late=Fixture(true);late.Regions[4].Area+=0.1;string lateError=null;
  try{RunSubject(late,true);}catch(System.Exception e){lateError=e.Message;}
  if(lateError!="section_area" || late.Calls!=5)throw new System.Exception("upper midpoint not validated");Clean(late,4);
  var missing=Fixture(false);missing.Mutation=(i,r)=>null;string missingError=null;
  try{RunSubject(missing,false);}catch(System.Exception e){missingError=e.Message;}
  if(missingError!="section_missing" || missing.Calls!=1 || Teigha.Geometry.Plane.Created!=Teigha.Geometry.Plane.Disposed)throw new System.Exception("null section not rejected/plane leaked");
  foreach(var bad in new[]{double.NaN,double.PositiveInfinity,-1d,0d}){
   var s=Fixture(false);string error=null;
   try{QS3D.LocalQualification.SectionVerifier.Verify(s,bad,2,1,1,1,0,P(10,-20,4));}catch(System.Exception e){error=e.Message;}
   if(error!="section_input" || s.Calls!=0)throw new System.Exception("invalid inputs reached native API");
  }
 }
}
'@)
[Local022SectionsReplay]::Run()
Write-Output 'PASS: actual section verifier checks all lower/taper quarter planes, area/perimeter/bounds and unique rectangular boundary; malformed native responses fail closed and transient resources dispose.'

foreach($major in @(25,26)) {
    $probe=Get-Content (Join-Path $PSScriptRoot "../QS3D.LocalQualification.V$major/Local022NativeFootingProbeCommands.cs") -Raw
    if(([regex]::Matches($probe,'VerifySolid\([^;]+verifySections: true\);')).Count -ne 5 -or
        $probe -notmatch 'bool verifySections = false' -or
        $probe -notmatch 'SectionVerifier.Verify\(solid' -or
        $probe -notmatch 'BrepVerifier.Verify\(solid' -or
        $probe -notmatch 'catch \(InvalidOperationException error\)') { throw "FAIL: V$major section integration bypassed." }
}
Write-Output 'PASS: both native probes cover initial/regenerated/saved/cold sections; existing physical UI callers remain opt-out.'

foreach($major in @(25,26)) {
    $runner=Join-Path $PSScriptRoot "../../scripts/test-bricscad-v$major-single-footing.ps1"
    $ast=[Management.Automation.Language.Parser]::ParseFile($runner,[ref]$null,[ref]$null)
    $read=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Read-Phase'},$true)
    $coverage=$ast.Find({param($n) $n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -ceq '$requiredByPhase'},$true)
    $requiredByPhase=& ([scriptblock]::Create($coverage.Right.Extent.Text))
    . ([scriptblock]::Create($read.Extent.Text))
    $ArtifactDir='C:\host-free-sections'; $runId='a'*32
    $schema=if($major -eq 25){'QS3D_LOCAL022_NATIVE_V3'}else{'QS3D_LOCAL022_V26_NATIVE_V3'}
    foreach($phase in @('run','saved','reopen')) {
        $key=switch($phase){run{'native_rectangular_sections'} saved{'saved_native_rectangular_sections'} reopen{'reopened_native_rectangular_sections'}}
        if($requiredByPhase[$phase] -cnotcontains $key){throw "FAIL: V$major/$phase section assertion is optional."}
        foreach($mutation in @('','missing','false','string','old_schema','section_only','missing_brep','false_brep','string_brep')) {
            function Get-Content {param($LiteralPath,[switch]$Raw)
                if($LiteralPath -cne "C:\host-free-sections\phase-$phase.json"){throw 'Unexpected evidence path.'}
                $checks=[ordered]@{};foreach($name in $requiredByPhase[$phase]){$checks[$name]=$true}
                $marker=[ordered]@{schema=$schema;run_id=$runId;phase=$phase;status='PASS';stage=$phase;error_code='NONE';checks=$checks}
                switch($mutation){
                    missing{$checks.Remove($key)} false{$checks[$key]=$false} string{$checks[$key]='true'}
                    old_schema{$marker.schema=$schema.Replace('_V3','_V1')}
                    section_only{$marker.schema=$schema.Replace('_V3','_V2')}
                    missing_brep{$checks.Remove($key.Replace('rectangular_sections','brep_topology'))}
                    false_brep{$checks[$key.Replace('rectangular_sections','brep_topology')]=$false}
                    string_brep{$checks[$key.Replace('rectangular_sections','brep_topology')]='true'}
                }
                $marker | ConvertTo-Json -Depth 5 -Compress
            }
            $rejected=$false
            try{$null=Read-Phase $phase}catch{$rejected=$true}
            finally{Remove-Item Function:\Get-Content}
            if($rejected -ne [bool]$mutation){throw "FAIL: V$major/$phase admitted malformed sections or rejected valid marker: $mutation"}
        }
    }
}
Write-Output 'PASS: both actual V3 native validators require section and topology evidence in every phase; V1/V2/missing/false/string checks cannot qualify it.'
