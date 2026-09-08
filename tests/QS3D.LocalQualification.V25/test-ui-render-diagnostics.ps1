$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$path=Join-Path $PSScriptRoot 'Local022UiRenderDiagnostics.cs'
if(-not (Test-Path -LiteralPath $path)) { throw 'FAIL: passive render diagnostic missing.' }
$source=Get-Content -LiteralPath $path -Raw
# Dispatcher hooks are an external event source; replay their real subscription
# boundary without loading CAD/WPF or scheduling work on the shared desktop.
Add-Type -TypeDefinition ($source + @'
#nullable disable
namespace System.Windows.Threading {
 public enum DispatcherPriority { Background=4, Render=7, Normal=9 }
 public sealed class DispatcherOperation { public DispatcherPriority Priority {get;set;} }
 public sealed class DispatcherHookEventArgs : System.EventArgs {
  public DispatcherOperation Operation {get;set;} = new DispatcherOperation();
 }
 public delegate void DispatcherHookEventHandler(object sender,DispatcherHookEventArgs e);
 public sealed class DispatcherHooks {
  public event DispatcherHookEventHandler OperationPosted;
  public event DispatcherHookEventHandler OperationCompleted;
  public event DispatcherHookEventHandler OperationAborted;
  public int Subscribers => (OperationPosted?.GetInvocationList().Length??0)+(OperationCompleted?.GetInvocationList().Length??0)+(OperationAborted?.GetInvocationList().Length??0);
  public void Raise(int kind,DispatcherPriority priority) {
   var e=new DispatcherHookEventArgs {Operation=new DispatcherOperation {Priority=priority}};
   if(kind==0) OperationPosted?.Invoke(this,e);
   if(kind==1) OperationCompleted?.Invoke(this,e);
   if(kind==2) OperationAborted?.Invoke(this,e);
  }
 }
 public sealed class Dispatcher {
  public DispatcherHooks Hooks {get;}=new DispatcherHooks();
  public System.Threading.Thread Thread => System.Threading.Thread.CurrentThread;
 }
}
public static class Local022RenderDiagnosticReplay {
 static void Has(string text,string token) {
  if(System.Array.IndexOf(text.Split(' '),token)<0) throw new System.Exception("Diagnostic missing exact token "+token+": "+text);
 }
 public static void Run() {
  var dispatcher=new System.Windows.Threading.Dispatcher();
  var subject=new QS3D.LocalQualification.UiRenderDiagnostics(dispatcher);
  if(dispatcher.Hooks.Subscribers!=3) throw new System.Exception("Hook subscription boundary changed");
  var first=subject.Snapshot(null,null);
  foreach(var token in new[]{"diagnostic_only=true","command_active=unavailable","same_dispatcher=unbound","render_posted=0","render_completed=0","render_aborted=0","disposed=false"}) Has(first,token);
  foreach(var priority in new[]{System.Windows.Threading.DispatcherPriority.Background,System.Windows.Threading.DispatcherPriority.Normal})
   for(int kind=0;kind<3;kind++) dispatcher.Hooks.Raise(kind,priority);
  Has(subject.Snapshot(4,dispatcher),"render_posted=0");
  dispatcher.Hooks.Raise(0,System.Windows.Threading.DispatcherPriority.Render);
  dispatcher.Hooks.Raise(0,System.Windows.Threading.DispatcherPriority.Render);
  dispatcher.Hooks.Raise(1,System.Windows.Threading.DispatcherPriority.Render);
  dispatcher.Hooks.Raise(2,System.Windows.Threading.DispatcherPriority.Render);
  var active=subject.Snapshot(4,dispatcher);
  foreach(var token in new[]{"command_active=4","same_dispatcher=true","render_posted=2","render_completed=1","render_aborted=1"}) Has(active,token);
  Has(subject.Snapshot(0,new System.Windows.Threading.Dispatcher()),"same_dispatcher=false");
  subject.Dispose(); subject.Dispose();
  if(dispatcher.Hooks.Subscribers!=0) throw new System.Exception("Diagnostic hooks leaked");
  for(int kind=0;kind<3;kind++) dispatcher.Hooks.Raise(kind,System.Windows.Threading.DispatcherPriority.Render);
  var last=subject.Snapshot(0,dispatcher);
  foreach(var token in new[]{"disposed=true","render_posted=2","render_completed=1","render_aborted=1"}) Has(last,token);
 }
}
'@)
[Local022RenderDiagnosticReplay]::Run()
Write-Output 'PASS: actual passive diagnostics count only Render-priority notifications, preserve unavailable/foreign-dispatcher identity and detach idempotently; no CAD/WPF/input.'

$controller=Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$trace=[regex]::Match($controller,'(?ms)^            private void TraceRenderProgress\(\)\r?\n            \{.*?^            \}').Value
$stop=[regex]::Match($controller,'(?ms)^            private void StopRenderDiagnostics\(\)\r?\n            \{.*?^            \}').Value
if(-not $trace -or -not $stop) { throw 'FAIL: missing bounded diagnostic integration.' }
Add-Type -TypeDefinition (@'
using System;
using System.Globalization;
public sealed class Local022RenderTraceReplay {
 bool _observedClickDriver=true;
 DateTime _nextRenderTraceUtc;
 string _stage="EditH2";
 UiRenderDiagnostics _renderDiagnostics=new UiRenderDiagnostics();
 Workspace _workspace=new Workspace();
 int lines; string last; bool failLog;
 sealed class Workspace { public object Dispatcher=new object(); }
 sealed class UiRenderDiagnostics {
  public bool Disposed;
  public string Snapshot(int? active,object dispatcher) => "active="+(active?.ToString(CultureInfo.InvariantCulture)??"unavailable");
  public void Dispose() { Disposed=true; }
 }
 static class Application {
  public static int Reads; public static bool Fail;
  public static object GetSystemVariable(string name) {
   if(name!="CMDACTIVE") throw new Exception("Unexpected host query");
   Reads++; if(Fail) throw new Exception("Host unavailable"); return 4;
  }
 }
 void UiTrace(string text) { if(failLog) throw new Exception("Disk unavailable"); lines++; last=text; }
'@ + $trace + "`n" + $stop + @'
 public static void Run() {
  var test=new Local022RenderTraceReplay();
  test.TraceRenderProgress();
  if(test.lines!=1 || test.last!="render_progress stage=EditH2 active=4") throw new Exception("Missing diagnostic trace");
  test.TraceRenderProgress();
  if(test.lines!=1 || Application.Reads!=1) throw new Exception("Repeated trace bypassed throttle");
  test._nextRenderTraceUtc=DateTime.MinValue; Application.Fail=true;
  test.TraceRenderProgress();
  if(test.last!="render_progress stage=EditH2 active=unavailable") throw new Exception("Missing unavailable state");
  test._nextRenderTraceUtc=DateTime.MinValue; test.failLog=true;
  test.TraceRenderProgress(); // logging failure must not escape into product verdict.
  test._observedClickDriver=false; test._nextRenderTraceUtc=DateTime.MinValue;
  int reads=Application.Reads; test.TraceRenderProgress();
  if(Application.Reads!=reads) throw new Exception("Legacy driver performed diagnostic host query");
  var observer=test._renderDiagnostics; test.StopRenderDiagnostics(); test.StopRenderDiagnostics();
  if(!observer.Disposed || test._renderDiagnostics!=null) throw new Exception("Controller retained observer");
 }
}
'@)
[Local022RenderTraceReplay]::Run()
Write-Output 'PASS: actual controller trace is throttled, observed-only, records unavailable host state, isolates log/query errors and releases hooks; no CAD/input.'
