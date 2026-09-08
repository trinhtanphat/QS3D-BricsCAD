$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$path = Join-Path $PSScriptRoot 'Local022UiRenderControls.cs'
$source = Get-Content -LiteralPath $path -Raw
# The compiled subject is the real helper. Only external WPF/Bricsys types are
# redirected: this test never creates a desktop window or loads the CAD host.
$forbidden = 'QS3D\.BricsCAD|(?:Find|TryFind)Resource\s*\(|SetResourceReference\s*\(|Application\.Current|UpdateLayout\s*\(|Invalidate(?:Visual|Measure|Arrange)\s*\(|CompositionTarget\.Rendering|ProcessRenderMode\s*=|RenderTargetBitmap|Dispatcher\.(?:PushFrame|Run)|DoEvents\s*\(|SendInput\s*\(|mouse_event\s*\(|keybd_event\s*\(|ShowDialog\s*\(|DllImport'
if ($source -match $forbidden) {
    throw "Diagnostic controls acquired a product resource, forced render, native input or modal dependency: $($Matches[0])"
}
$source = $source.Replace('System.Windows', 'Local022ControlDoubles.Windows').Replace('Bricscad.Windows', 'Local022ControlDoubles.Bricscad')
Add-Type -TypeDefinition ($source + @'
#nullable disable
namespace Local022ControlDoubles {
 public static class Faults {
  static readonly System.Collections.Generic.Dictionary<string,int> remaining = new System.Collections.Generic.Dictionary<string,int>();
  public static void Next(string name) { remaining[name] = 1; }
  public static void Hit(string name) {
   if(remaining.TryGetValue(name,out var count) && count > 0) {
    remaining[name] = count - 1;
    throw new System.InvalidOperationException("injected_" + name);
   }
  }
  public static void Reset() { remaining.Clear(); Windows.Window.Created.Clear(); Bricscad.PaletteSet.Created.Clear(); }
 }
}
namespace Local022ControlDoubles.Windows.Media {
 public class Visual { internal Windows.PresentationSource Source; }
 public sealed class Brush { internal Brush(string name) { Name = name; } public string Name { get; } }
 public static class Brushes {
  public static readonly Brush White = new Brush("white"), Black = new Brush("black"),
   LightYellow = new Brush("yellow"), LightCyan = new Brush("cyan");
 }
 public sealed class FontFamily { public FontFamily(string name) { Name = name; } public string Name { get; } }
}
namespace Local022ControlDoubles.Windows {
 public struct Size { public Size(double w,double h) { Width=w; Height=h; } public double Width,Height; }
 public struct Point { public Point(double x,double y) { X=x; Y=y; } public double X,Y; }
 public struct Thickness { public Thickness(double value) { Value=value; } public double Value; }
 public enum WindowStartupLocation { Manual }
 public enum ResizeMode { NoResize }
 public enum TextWrapping { NoWrap, Wrap }
 public class FrameworkElement : Media.Visual {
  public object Style {get;set;} = new object();
  public bool IsLoaded {get;internal set;}
  public bool IsVisible {get;internal set;}
 }
 public class PresentationSource {
  public static PresentationSource FromVisual(Media.Visual visual) => visual.Source;
 }
 public sealed class TestPresentationSource : PresentationSource { }
 public static class Mount {
  public static void Set(object value,bool visible) {
   if(value is FrameworkElement element) {
    element.IsLoaded=visible; element.IsVisible=visible;
    element.Source=visible ? new TestPresentationSource() : null;
   }
   if(value is Controls.Border border) Set(border.Child,visible);
  }
 }
 public sealed class Window : FrameworkElement {
  public static readonly System.Collections.Generic.List<Window> Created = new System.Collections.Generic.List<Window>();
  string title;
  System.EventHandler closed;
  public Window() { Faults.Hit("window_construct"); Created.Add(this); }
  public string Title {get => title; set { TitleWrites++; title=value; }}
  public int TitleWrites,ShowAttempts,CloseAttempts;
  public bool ClosedState;
  public System.IntPtr Owner;
  public double Width {get;set;} public double Height {get;set;}
  public double Left {get;set;} public double Top {get;set;}
  public WindowStartupLocation WindowStartupLocation {get;set;}
  public bool ShowInTaskbar {get;set;} = true;
  public bool ShowActivated {get;set;} = true;
  public bool AllowsTransparency {get;set;} = true;
  public bool Topmost {get;set;} = true;
  public ResizeMode ResizeMode {get;set;}
  public Media.Brush Background {get;set;}
  public object Content {get;set;}
  public event System.EventHandler Closed {add {closed+=value;} remove {closed-=value;}}
  public int Subscribers => closed?.GetInvocationList().Length ?? 0;
  public void Show() { ShowAttempts++; Faults.Hit("window_show"); Mount.Set(Content,true); }
  public void Close() {
   CloseAttempts++; Faults.Hit("window_close"); ClosedState=true;
   Mount.Set(Content,false); closed?.Invoke(this,System.EventArgs.Empty);
  }
 }
}
namespace Local022ControlDoubles.Windows.Controls {
 public sealed class TextBlock : Windows.FrameworkElement {
  string text;
  public int TextWrites;
  public string Text {get => text; set {TextWrites++; text=value;}}
  public Windows.Media.FontFamily FontFamily {get;set;}
  public double FontSize {get;set;}
  public Windows.Media.Brush Foreground {get;set;}
  public Windows.TextWrapping TextWrapping {get;set;}
  public Windows.Thickness Margin {get;set;}
 }
 public sealed class Border : Windows.FrameworkElement {
  public Windows.Media.Brush Background {get;set;}
  public Windows.Media.Brush BorderBrush {get;set;}
  public Windows.Thickness BorderThickness {get;set;}
  public object Child {get;set;}
 }
}
namespace Local022ControlDoubles.Windows.Interop {
 public sealed class WindowInteropHelper {
  readonly Windows.Window window;
  public WindowInteropHelper(Windows.Window value) {window=value;}
  public System.IntPtr Owner {get => window.Owner; set {Faults.Hit("window_owner"); window.Owner=value;}}
 }
}
namespace Local022ControlDoubles.Bricscad {
 public enum DockSides { None, Left, Right }
 public sealed class PaletteSet : System.IDisposable {
  public static readonly System.Collections.Generic.List<PaletteSet> Created = new System.Collections.Generic.List<PaletteSet>();
  bool visible;
  public PaletteSet(string name,System.Guid identity) {
   Faults.Hit("palette_construct"); Name=name; Identity=identity; Created.Add(this);
  }
  public string Name {get;} public System.Guid Identity {get;}
  public DockSides DockEnabled {get;set;} public DockSides Dock {get;set;}
  public bool KeepFocus {get;set;} = true;
  public System.Drawing.Size MinimumSize {get;set;}
  public Windows.Size DeviceIndependentSize {get;set;}
  public Windows.Point DeviceIndependentLocation {get;set;}
  public Windows.Media.Visual Visual;
  public bool AutoSize,Disposed;
  public int AddAttempts,DisposeAttempts;
  public bool Visible {
   get => visible;
   set {Faults.Hit("palette_visible"); visible=value; Windows.Mount.Set(Visual,value);}
  }
  public void AddVisual(string title,Windows.Media.Visual visual,bool autoSize) {
   AddAttempts++; Faults.Hit("palette_add"); Visual=visual; AutoSize=autoSize;
  }
  public void Dispose() {
   DisposeAttempts++; Faults.Hit("palette_dispose"); Disposed=true;
   Windows.Mount.Set(Visual,false);
  }
 }
}
public static class Local022RenderControlsReplay {
 static readonly System.IntPtr Owner = new System.IntPtr(1234);
 static readonly System.Guid RunId = new System.Guid("1b37bba8-ab53-49f9-ab22-5580d347048f");
 static void Check(bool value,string message) {if(!value) throw new System.Exception(message);}
 static void Eq(object actual,object expected) {Check(object.Equals(actual,expected),"Mismatch: " + actual + " != " + expected);}
 static void Has(string text,string token) {Check(System.Array.IndexOf(text.Split(' '),token)>=0,"Missing diagnostic token " + token + ": " + text);}
 static System.Exception Reject(System.Action action,string expected) {
  try {action();} catch(System.Exception error) {
   if(expected!=null) Eq(error.Message,expected);
   return error;
  }
  throw new System.Exception("Expected failure: " + expected);
 }
 static void AllReleased() {
  foreach(var palette in Local022ControlDoubles.Bricscad.PaletteSet.Created) Check(palette.Disposed,"Palette leaked after cleanup");
  foreach(var window in Local022ControlDoubles.Windows.Window.Created) {
   Check(window.ClosedState,"Window leaked after cleanup"); Eq(window.Subscribers,0);
  }
 }
 static void Identity() {
  Local022ControlDoubles.Faults.Reset();
  Reject(()=>new QS3D.LocalQualification.UiRenderControls(System.IntPtr.Zero,RunId),"render_controls_identity_missing");
  Reject(()=>new QS3D.LocalQualification.UiRenderControls(Owner,System.Guid.Empty),"render_controls_identity_missing");
  Eq(Local022ControlDoubles.Bricscad.PaletteSet.Created.Count,0);
  Eq(Local022ControlDoubles.Windows.Window.Created.Count,0);
 }
 static void Updates() {
  Local022ControlDoubles.Faults.Reset();
  var subject=new QS3D.LocalQualification.UiRenderControls(Owner,RunId);
  Eq(Local022ControlDoubles.Bricscad.PaletteSet.Created.Count,1);
  Eq(Local022ControlDoubles.Windows.Window.Created.Count,1);
  var palette=Local022ControlDoubles.Bricscad.PaletteSet.Created[0];
  var window=Local022ControlDoubles.Windows.Window.Created[0];
  Eq(palette.Identity,RunId); Eq(window.Owner,Owner);
  Check(palette.Visible && palette.AutoSize,"Palette must use visible AddVisual auto-size hosting"); Eq(palette.AddAttempts,1);
  Eq(palette.DockEnabled,Local022ControlDoubles.Bricscad.DockSides.None);
  Eq(palette.Dock,Local022ControlDoubles.Bricscad.DockSides.None);
  Check(!palette.KeepFocus && !window.ShowActivated && !window.ShowInTaskbar && !window.Topmost && !window.AllowsTransparency,
   "Witnesses changed focus, transparency or shell behavior");
  Eq(window.ShowAttempts,1); Eq(window.Subscribers,1); Eq(window.ResizeMode,Local022ControlDoubles.Windows.ResizeMode.NoResize);
  var paletteBorder=(Local022ControlDoubles.Windows.Controls.Border)palette.Visual;
  var windowBorder=(Local022ControlDoubles.Windows.Controls.Border)window.Content;
  var paletteText=(Local022ControlDoubles.Windows.Controls.TextBlock)paletteBorder.Child;
  var windowText=(Local022ControlDoubles.Windows.Controls.TextBlock)windowBorder.Child;
  Check(!object.ReferenceEquals(paletteBorder,windowBorder) && !object.ReferenceEquals(paletteText,windowText),"Witnesses share a visual parent");
  foreach(var element in new Local022ControlDoubles.Windows.FrameworkElement[]{window,paletteBorder,windowBorder,paletteText,windowText})
   Eq(element.Style,null);
  Eq(window.Background,Local022ControlDoubles.Windows.Media.Brushes.White);
  Eq(paletteText.Foreground,Local022ControlDoubles.Windows.Media.Brushes.Black);
  Eq(windowText.Foreground,Local022ControlDoubles.Windows.Media.Brushes.Black);
  Eq(paletteText.Text,"PALETTE 000\nbaseline_default"); Eq(windowText.Text,"WINDOW 000\nbaseline_default");
  var titleWrites=window.TitleWrites; var textWrites=paletteText.TextWrites+windowText.TextWrites;
  subject.Update("baseline_default",0);
  Eq(window.TitleWrites,titleWrites); Eq(paletteText.TextWrites+windowText.TextWrites,textWrites);
  subject.Update("software_only",1);
  Eq(paletteText.Text,"PALETTE 001\nsoftware_only"); Eq(windowText.Text,"WINDOW 001\nsoftware_only");
  Eq(window.Title,"LOCAL022 window witness 001 software_only");
  Eq(paletteBorder.Background,Local022ControlDoubles.Windows.Media.Brushes.LightCyan); Eq(windowBorder.Background,paletteBorder.Background);
  foreach(var token in new[]{"diagnostic_controls_only=true","control_sequence=1","control_stage=software_only","palette_loaded=true","palette_visible=true",
   "window_loaded=true","window_visible=true","palette_source=TestPresentationSource","window_source=TestPresentationSource"}) Has(subject.Snapshot(),token);
  var before=subject.Snapshot(); titleWrites=window.TitleWrites;
  Reject(()=>subject.Update("baseline_default",-1),"render_controls_sequence_or_stage");
  Reject(()=>subject.Update("baseline_default",0),"render_controls_sequence_or_stage");
  Reject(()=>subject.Update("PASS",2),"render_controls_sequence_or_stage");
  Reject(()=>subject.Update(null,2),"render_controls_sequence_or_stage");
  Eq(subject.Snapshot(),before); Eq(window.TitleWrites,titleWrites);
  subject.Update("restored_default",1); Eq(windowText.Text,"WINDOW 001\nrestored_default");
  subject.Update("restored_default",2);
  Eq(paletteBorder.Background,Local022ControlDoubles.Windows.Media.Brushes.LightYellow); Eq(windowBorder.Background,paletteBorder.Background);
  subject.Update("complete",3); Has(subject.Snapshot(),"control_stage=complete");
  subject.Dispose(); subject.Dispose(); AllReleased(); Eq(window.CloseAttempts,1); Eq(palette.DisposeAttempts,1);
  Reject(()=>subject.Update("complete",4),"render_controls_closed");
  foreach(var token in new[]{"palette_loaded=false","window_visible=false","palette_source=none","window_source=none"}) Has(subject.Snapshot(),token);
 }
 static void ClosedWindow() {
  Local022ControlDoubles.Faults.Reset();
  var subject=new QS3D.LocalQualification.UiRenderControls(Owner,RunId);
  var window=Local022ControlDoubles.Windows.Window.Created[0]; window.Close();
  Reject(()=>subject.Update("baseline_default",1),"render_controls_closed");
  subject.Dispose(); AllReleased(); Eq(window.CloseAttempts,1);
 }
 static void ConstructionFailures() {
  foreach(var fault in new[]{"palette_construct","palette_add","palette_visible","window_construct","window_owner","window_show"}) {
   Local022ControlDoubles.Faults.Reset(); Local022ControlDoubles.Faults.Next(fault);
   Reject(()=>new QS3D.LocalQualification.UiRenderControls(Owner,RunId),"injected_"+fault); AllReleased();
  }
  Local022ControlDoubles.Faults.Reset();
  Local022ControlDoubles.Faults.Next("window_show"); Local022ControlDoubles.Faults.Next("palette_dispose");
  var failure=Reject(()=>new QS3D.LocalQualification.UiRenderControls(Owner,RunId),null) as System.AggregateException;
  Check(failure!=null,"Construction and cleanup errors must both survive");
  var flattened=failure.Flatten(); Eq(flattened.InnerExceptions.Count,2);
  Check(System.Linq.Enumerable.Any(flattened.InnerExceptions,e=>e.Message=="injected_window_show"),"Lost construction failure");
  Check(System.Linq.Enumerable.Any(flattened.InnerExceptions,e=>e.Message=="injected_palette_dispose"),"Lost cleanup failure");
  // The unreachable partial object reports its cleanup debt. Release only the fake
  // resource here so the next test begins clean; this does not claim auto-recovery.
  var palette=Local022ControlDoubles.Bricscad.PaletteSet.Created[0];
  Check(!palette.Disposed,"Fault injection did not reach cleanup");
  Eq(Local022ControlDoubles.Windows.Window.Created[0].Subscribers,0);
  palette.Dispose(); AllReleased();
 }
 static void CleanupRetry() {
  foreach(var failWindow in new[]{false,true}) foreach(var failPalette in new[]{false,true}) {
   if(!failWindow && !failPalette) continue;
   Local022ControlDoubles.Faults.Reset();
   var subject=new QS3D.LocalQualification.UiRenderControls(Owner,RunId);
   var window=Local022ControlDoubles.Windows.Window.Created[0];
   var palette=Local022ControlDoubles.Bricscad.PaletteSet.Created[0];
   if(failWindow) Local022ControlDoubles.Faults.Next("window_close");
   if(failPalette) Local022ControlDoubles.Faults.Next("palette_dispose");
   var failure=Reject(()=>subject.Dispose(),null) as System.AggregateException;
   Check(failure!=null,"Cleanup failure was hidden");
   Eq(failure.Flatten().InnerExceptions.Count,(failWindow?1:0)+(failPalette?1:0));
   Eq(window.CloseAttempts,1); Eq(palette.DisposeAttempts,1);
   Eq(window.ClosedState,!failWindow); Eq(palette.Disposed,!failPalette);
   subject.Dispose(); subject.Dispose(); AllReleased();
   Eq(window.CloseAttempts,failWindow?2:1); Eq(palette.DisposeAttempts,failPalette?2:1);
   Reject(()=>subject.Update("baseline_default",1),"render_controls_closed");
  }
 }
 public static void Run() { Identity(); Updates(); ClosedWindow(); ConstructionFailures(); CleanupRetry(); }
}
'@)
[Local022RenderControlsReplay]::Run()
Write-Output 'PASS: actual render controls preserve independent diagnostic updates, owner/opacity, invalid-input refusal and partial/failed cleanup behavior; host doubles only, no WPF/CAD/native/input execution.'
