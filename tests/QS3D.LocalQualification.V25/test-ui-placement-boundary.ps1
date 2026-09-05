$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Replay the actual pre-click target latch, with no CAD assembly or native input.
$source = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$latch = [regex]::Match($source, '(?ms)^        private static Point3d\? CapturePlacementCentre\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $latch) { throw 'FAIL: missing physical placement target latch.' }
$name = 'Local022PlacementReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
public static class $name {
    private struct Point3d {
        public double X;
        public Point3d(double x) { X = x; }
    }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
$latch
    public static void Run() {
        int reads = 0;
        double viewPoint = 12;
        Func<Point3d> read = () => { reads++; return new Point3d(viewPoint); };
        var pending = CapturePlacementCentre(null, false, false, read);
        if (pending.HasValue || reads != 0) throw new Exception("Captured before hover acknowledgement");
        pending = CapturePlacementCentre(pending, false, true, read);
        if (!pending.HasValue || pending.Value.X != 12 || reads != 1) throw new Exception("Did not capture before physical click");
        viewPoint = 90;
        pending = CapturePlacementCentre(pending, true, true, read);
        if (!pending.HasValue || pending.Value.X != 12 || reads != 1) throw new Exception("View change moved the clicked target");
        pending = CapturePlacementCentre(pending, false, true, read);
        if (!pending.HasValue || pending.Value.X != 12 || reads != 1) throw new Exception("Repeated tick overwrote target");
        bool rejected = false;
        try { CapturePlacementCentre(null, true, true, read); }
        catch (ProbeException error) { if (error.Message != "ui_placement_target_not_captured") throw; rejected = true; }
        if (!rejected || reads != 1) throw new Exception("Missing pre-click target accepted");
        pending = CapturePlacementCentre(null, false, true, read);
        if (!pending.HasValue || pending.Value.X != 90 || reads != 2) throw new Exception("Next placement did not capture a fresh target");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual placement latch waits for hover, captures before click, survives view drift, rejects missing targets and resets per placement.'

# Actual mapping method with native/document doubles. The local SDK-backed HWND
# lookup itself is verified by licensed execution, never loaded in this replay.
$mapping = [regex]::Match($source, '(?ms)^        private static DrawingPoint\? ScreenToDrawingClient\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $mapping) { throw 'FAIL: missing drawing-client mapping.' }
$mapping = $mapping.Replace('Bricscad.ApplicationServices.Document', 'Document')
$name = 'Local022ClientReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
public static class $name {
    private struct DrawingPoint { public int X, Y; public DrawingPoint(int x, int y) { X=x; Y=y; } }
    private struct UiNativePoint { public int X, Y; }
    private struct UiNativeRect { public int Left, Top, Right, Bottom; }
    private sealed class Document {}
    private sealed class Manager { public Document MdiActiveDocument = new Document(); }
    private static class Application { public static Manager DocumentManager = new Manager(); }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private static IntPtr View = new IntPtr(123);
    private static bool ValidOwner = true, ValidRect = true, ValidMap = true;
    private static IntPtr GetDrawingViewWindow() { return View; }
    private static uint GetWindowThreadProcessId(IntPtr window, out uint owner) {
        using (var process = Process.GetCurrentProcess()) owner = ValidOwner ? (uint)process.Id : 0;
        return 1;
    }
    private static bool GetClientRect(IntPtr window, out UiNativeRect bounds) {
        bounds = new UiNativeRect { Left=0, Top=0, Right=370, Bottom=475 }; return ValidRect;
    }
    private static bool ScreenToClient(IntPtr window, ref UiNativePoint point) {
        point.X -= 644; point.Y -= 215; return ValidMap;
    }
$mapping
    private static void Reject(Action action, string code) {
        try { action(); } catch (ProbeException error) { if (error.Message != code) throw; return; }
        throw new Exception("Accepted invalid mapping: " + code);
    }
    public static void Run() {
        var doc = Application.DocumentManager.MdiActiveDocument;
        var point = new DrawingPoint(751,385);
        var mapped = ScreenToDrawingClient(doc,point,true);
        if (!mapped.HasValue || mapped.Value.X != 107 || mapped.Value.Y != 170) throw new Exception("Desktop origin was not removed");
        Reject(() => ScreenToDrawingClient(new Document(),point,true), "ui_drawing_document_changed");
        View = IntPtr.Zero; Reject(() => ScreenToDrawingClient(doc,point,true), "ui_drawing_window_identity"); View = new IntPtr(123);
        ValidOwner = false; Reject(() => ScreenToDrawingClient(doc,point,true), "ui_drawing_window_identity"); ValidOwner = true;
        ValidRect = false; Reject(() => ScreenToDrawingClient(doc,point,true), "ui_drawing_window_identity"); ValidRect = true;
        ValidMap = false; Reject(() => ScreenToDrawingClient(doc,point,true), "ui_drawing_client_mapping"); ValidMap = true;
        foreach (var outside in new[] { new DrawingPoint(643,385), new DrawingPoint(751,214), new DrawingPoint(1014,385), new DrawingPoint(751,690) }) {
            if (ScreenToDrawingClient(doc,outside,false).HasValue) throw new Exception("Out-of-view candidate accepted");
            Reject(() => ScreenToDrawingClient(doc,outside,true), "ui_point_outside_drawing_client");
        }
        foreach (var inside in new[] { new DrawingPoint(644,215), new DrawingPoint(1013,689) })
            if (!ScreenToDrawingClient(doc,inside,true).HasValue) throw new Exception("Inside boundary rejected");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual mapping subtracts drawing origin and rejects document/PID/HWND/API failures and every outside edge; no native library loaded.'

$rowMethod = [regex]::Match($source, '(?ms)^        private static bool IsPropertyRow\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $rowMethod) { throw 'FAIL: missing dimension row identity guard.' }
$name = 'Local022PropertyReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
public static class $name {
    private sealed class Row { public string Name {get;set;} = "H2"; public string Unit {get;set;} = "mm"; public bool IsReadOnly {get;set;} }
$rowMethod
    public static void Run() {
        var row = new Row();
        if (!IsPropertyRow(row,"H2","mm")) throw new Exception("Valid H2/mm editor rejected");
        row.Name = "H1"; if (IsPropertyRow(row,"H2","mm")) throw new Exception("Wrong dimension accepted");
        row.Name = "H2"; row.Unit = "m"; if (IsPropertyRow(row,"H2","mm")) throw new Exception("Wrong unit accepted");
        row.Unit = "mm"; row.IsReadOnly = true; if (IsPropertyRow(row,"H2","mm")) throw new Exception("Read-only row accepted");
        if (IsPropertyRow(new object(),"H2","mm")) throw new Exception("Unknown row accepted");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual row selector requires exact editable H2/mm identity, rejecting wrong dimension/unit/read-only/unknown rows.'

$prepare = [regex]::Match($source, '(?ms)^        private static bool PrepareOwnedUiWindow\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $prepare) { throw 'FAIL: missing one-shot pre-input window preparation.' }
$name = 'Local022WindowReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
public static class $name {
    private sealed class Process : IDisposable {
        public int Id = 123;
        public IntPtr MainWindowHandle = CurrentWindow;
        public static Process GetCurrentProcess() { return new Process(); }
        public void Dispose() {}
    }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private static IntPtr CurrentWindow = new IntPtr(7);
    private static uint Owner = 123;
    private static bool Zoomed;
    private static int Shows;
    private static uint GetWindowThreadProcessId(IntPtr window,out uint owner) { owner=Owner; return 1; }
    private static bool IsZoomed(IntPtr window) { return Zoomed; }
    private static bool ShowWindow(IntPtr window,int command) { if (window != CurrentWindow || command != 3) throw new Exception("Wrong maximize target"); Shows++; return false; }
$prepare
    public static void Run() {
        var window=IntPtr.Zero; DateTime? ready=null; var now=new DateTime(2026,9,5);
        if (PrepareOwnedUiWindow(ref window,ref ready,0,now) || Shows!=1) throw new Exception("Did not issue one pre-input maximize");
        if (PrepareOwnedUiWindow(ref window,ref ready,0,now.AddSeconds(1)) || Shows!=1) throw new Exception("Repeated maximize or admitted unmaximized window");
        Zoomed=true;
        if (PrepareOwnedUiWindow(ref window,ref ready,0,now.AddSeconds(2))) throw new Exception("Skipped layout settling");
        if (!PrepareOwnedUiWindow(ref window,ref ready,0,now.AddSeconds(4))) throw new Exception("Stable window not admitted");
        bool refused=false;
        try { PrepareOwnedUiWindow(ref window,ref ready,1,now.AddSeconds(4)); } catch (ProbeException) { refused=true; }
        if (!refused || Shows!=1) throw new Exception("Allowed resize after input");
        CurrentWindow=new IntPtr(8); refused=false;
        try { PrepareOwnedUiWindow(ref window,ref ready,0,now); } catch (ProbeException) { refused=true; }
        if (!refused) throw new Exception("Window replacement accepted");
        window=IntPtr.Zero; Owner=456; refused=false;
        try { PrepareOwnedUiWindow(ref window,ref ready,0,now); } catch (ProbeException) { refused=true; }
        if (!refused || Shows!=1) throw new Exception("Foreign HWND accepted");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual pre-input preparation maximizes once, waits for verified stable state, rejects HWND drift/foreign ownership and any post-input resize.'

$hit = [regex]::Match($source, '(?ms)^        private static bool TreeLabelHitMatches\([^\r\n]*\)\r?\n        \{.*?^        \}').Value
if (-not $hit) { throw 'FAIL: missing intended-row hit verification.' }
$name = 'Local022HitReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
public static class $name {
    private class DependencyObject { public DependencyObject Parent; }
    private class FrameworkElement : DependencyObject {
        public WpfPoint PointFromScreen(WpfPoint p) { return p; }
        public DependencyObject InputHitTest(WpfPoint p) { return Hit; }
    }
    private class TreeViewItem : DependencyObject {}
    private struct WpfPoint { public double X,Y; public WpfPoint(double x,double y) {X=x;Y=y;} }
    private static class VisualTreeHelper { public static DependencyObject GetParent(DependencyObject obj) { return obj.Parent; } }
    private static DependencyObject Hit;
    private static WpfPoint ElementCenter(FrameworkElement label) { return new WpfPoint(90,605); }
$hit
    public static void Run() {
        var root=new FrameworkElement(); var target=new TreeViewItem {Parent=root};
        var label=new FrameworkElement {Parent=target};
        Hit=label; if (!TreeLabelHitMatches(root,target,label)) throw new Exception("Intended row rejected");
        Hit=new FrameworkElement {Parent=new TreeViewItem {Parent=root}};
        if (TreeLabelHitMatches(root,target,label)) throw new Exception("Overlapping other row accepted");
        Hit=new FrameworkElement {Parent=new TreeViewItem {Parent=target}};
        if (TreeLabelHitMatches(root,target,label)) throw new Exception("Nested different row accepted");
        Hit=root; if (TreeLabelHitMatches(root,target,label)) throw new Exception("Clipped label accepted");
        Hit=null; if (TreeLabelHitMatches(root,target,label)) throw new Exception("Missing hit accepted");
    }
}
"@
([type]$name)::Run()
Write-Output 'PASS: actual hit ancestry accepts only intended nearest tree row, refusing overlapping/nested other rows and clipped/missing hits.'
