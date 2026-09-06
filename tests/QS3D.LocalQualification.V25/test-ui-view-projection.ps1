$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# Execute the actual pre-click mapping against independent camera/viewport doubles.
# A projected 3D point is on the view plane, not necessarily on the WCS Z=0 pick plane.
$source = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$mapping = [regex]::Match($source, '(?ms)^        private static Point3d ScreenWorldPoint\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $mapping) { throw 'FAIL: missing pre-click world mapper.' }
$mapping = $mapping.Replace('Bricscad.ApplicationServices.Document', 'Document')
$name = 'Local022ProjectionReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
using System.Globalization;
public static class $name {
    private struct Point3d { public double X,Y,Z; public Point3d(double x,double y,double z) { X=x;Y=y;Z=z; } }
    private struct Vector3d { public double X,Y,Z; public Vector3d(double x,double y,double z) { X=x;Y=y;Z=z; } }
    private struct DrawingPoint { public double X,Y; public DrawingPoint(double x,double y) { X=x;Y=y; } }
    private struct Matrix3d { public bool World; public static Matrix3d Identity => new Matrix3d { World=true }; }
    private sealed class View : IDisposable {
        public Vector3d ViewDirection = new Vector3d(0,0,1);
        public bool PerspectiveEnabled;
        public void Dispose() {}
    }
    private sealed class Editor {
        public View Camera = new View();
        public Matrix3d CurrentUserCoordinateSystem = Matrix3d.Identity;
        public Point3d Mapped = new Point3d(3,5,7);
        public bool BadRoundTrip;
        public View GetCurrentView() => Camera;
        public Point3d PointToWorld(DrawingPoint point,int viewport) => Mapped;
        public DrawingPoint PointToScreen(Point3d point,int viewport) {
            // An orthographic ray is invariant under displacement along view direction.
            var d=Camera.ViewDirection;
            double sx=point.X-point.Z*d.X/d.Z, sy=point.Y-point.Z*d.Y/d.Z;
            return new DrawingPoint(100+sx-(Mapped.X-Mapped.Z*d.X/d.Z)+(BadRoundTrip?3:0),200+sy-(Mapped.Y-Mapped.Z*d.Y/d.Z));
        }
    }
    private sealed class Document { public Editor Editor=new Editor(); }
    private static class Application {
        public static double Elevation;
        public static int Viewport=2;
        public static object GetSystemVariable(string key) => key=="CVPORT" ? Viewport : Elevation;
    }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private static DrawingPoint? ScreenToDrawingClient(Document doc,DrawingPoint point,bool inside) => new DrawingPoint(100,200);
$mapping
    private static void Near(Point3d actual,double x,double y,double z) {
        if(Math.Abs(actual.X-x)>1e-10 || Math.Abs(actual.Y-y)>1e-10 || Math.Abs(actual.Z-z)>1e-10)
            throw new Exception("Projection changed the click ray: actual="+actual.X+","+actual.Y+","+actual.Z);
    }
    private static void Reject(Action action,string code) {
        try { action(); } catch(ProbeException ex) { if(ex.Message!=code) throw; return; }
        throw new Exception("Accepted unsupported mapping: "+code);
    }
    public static void Run() {
        var doc=new Document(); var p=new DrawingPoint(820,483);
        Near(ScreenWorldPoint(doc,p),3,5,0);
        doc.Editor.Camera.ViewDirection=new Vector3d(1,-1,1);
        Near(ScreenWorldPoint(doc,p),-4,12,0); // Flattening only Z incorrectly returns (3,5,0).
        doc.Editor.Camera.ViewDirection=new Vector3d(2,-2,2);
        Near(ScreenWorldPoint(doc,p),-4,12,0); // Direction need not be unit length.
        doc.Editor.Camera.ViewDirection=new Vector3d(-1,1,-1);
        Near(ScreenWorldPoint(doc,p),-4,12,0);
        doc.Editor.Mapped=new Point3d(6.536382877543484,0.1535076787139963,3.7146304185727745);
        Near(ScreenWorldPoint(doc,p),2.8217524589707095,3.8681380972867708,0); // Allocation42 ray regression.
        doc.Editor.Camera.PerspectiveEnabled=true;
        Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_projection_unsupported");
        doc.Editor.Camera.PerspectiveEnabled=false;
        doc.Editor.CurrentUserCoordinateSystem=new Matrix3d();
        Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_plane_changed");
        doc.Editor.CurrentUserCoordinateSystem=Matrix3d.Identity;
        Application.Elevation=5; Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_plane_changed"); Application.Elevation=0;
        Application.Viewport=1; Reject(()=>ScreenWorldPoint(doc,p),"ui_model_viewport_missing"); Application.Viewport=2;
        doc.Editor.Camera.ViewDirection=new Vector3d(1,0,0);
        Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_projection_unsupported");
        doc.Editor.Camera.ViewDirection=new Vector3d(double.NaN,0,1);
        Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_projection_nonfinite");
        doc.Editor.Camera.ViewDirection=new Vector3d(0,0,1);
        doc.Editor.Mapped=new Point3d(double.PositiveInfinity,0,0);
        Reject(()=>ScreenWorldPoint(doc,p),"ui_pick_projection_nonfinite");
        doc.Editor.Mapped=new Point3d(3,5,7); doc.Editor.BadRoundTrip=true;
        Reject(()=>ScreenWorldPoint(doc,p),"ui_viewport_roundtrip_changed");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual pre-click mapper intersects the orthographic ray with WCS Z=0, keeps top/oblique/scaled directions and refuses unsupported/nonfinite/drifted mappings; no CAD or input.'
