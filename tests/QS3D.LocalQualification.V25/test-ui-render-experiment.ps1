$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$source=Get-Content (Join-Path $PSScriptRoot 'Local022UiRenderExperiment.cs') -Raw
# Compile the actual state machine against a controlled rendering property, not
# the live WPF renderer. Namespace isolation allows the complete suite to share a process.
$source=$source.Replace('System.Windows','Local022ExperimentDoubles.Windows')
Add-Type -TypeDefinition ($source + @'
#nullable disable
namespace Local022ExperimentDoubles.Windows.Interop { public enum RenderMode { Default, SoftwareOnly } }
namespace Local022ExperimentDoubles.Windows.Media {
 public static class RenderOptions {
  static Interop.RenderMode mode;
  public static bool FailNextSet;
  public static int Writes;
  public static Interop.RenderMode ProcessRenderMode {
   get => mode;
   set { Writes++; if(FailNextSet){ FailNextSet=false; throw new System.Exception("setter_failed"); } mode=value; }
  }
 }
}
public static class Local022RenderExperimentReplay {
 static readonly System.DateTime Start=new System.DateTime(2026,9,8,0,0,0,System.DateTimeKind.Utc);
 static void Eq(object actual,object expected) { if(!object.Equals(actual,expected)) throw new System.Exception("Mismatch: "+actual+" != "+expected); }
 static void Reject(System.Action action,string code) {
  try { action(); } catch(System.Exception e) { if(e.Message==code)return; throw; }
  throw new System.Exception("Expected rejection: "+code);
 }
 static string Advance(QS3D.LocalQualification.UiRenderExperiment test,int from,int to) {
  string last=""; for(int i=from;i<=to;i++) last=test.Advance(Start.AddSeconds(i)); return last;
 }
 static void Reset() { Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode=Local022ExperimentDoubles.Windows.Interop.RenderMode.Default; Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes=0; }
 public static void Run() {
  Reset(); var test=new QS3D.LocalQualification.UiRenderExperiment(Start);
  Eq(Advance(test,0,119),"baseline_default"); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,0);
  Eq(Advance(test,120,239),"software_only"); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,1);
  Eq(Advance(test,240,299),"restored_default"); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,2);
  Eq(Advance(test,300,300),"complete"); test.Dispose(); test.Dispose(); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,2);
  Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode,Local022ExperimentDoubles.Windows.Interop.RenderMode.Default);
  Reject(()=>test.Advance(Start.AddSeconds(301)),"render_experiment_clock_or_lifetime");
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); test.Dispose(); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,0);
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); Advance(test,0,120); test.Dispose(); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,2);
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); Reject(()=>test.Advance(Start.AddSeconds(-1)),"render_experiment_clock_or_lifetime"); test.Dispose();
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); Reject(()=>test.Advance(Start.AddSeconds(300)),"render_experiment_tick_gap"); test.Dispose(); Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.Writes,0);
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); Advance(test,0,119);
  Local022ExperimentDoubles.Windows.Media.RenderOptions.FailNextSet=true;
  Reject(()=>test.Advance(Start.AddSeconds(120)),"setter_failed"); test.Dispose();
  Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode,Local022ExperimentDoubles.Windows.Interop.RenderMode.Default);
  Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode=Local022ExperimentDoubles.Windows.Interop.RenderMode.SoftwareOnly;
  Reject(()=>new QS3D.LocalQualification.UiRenderExperiment(Start),"render_experiment_baseline_not_default");
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start);
  Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode=Local022ExperimentDoubles.Windows.Interop.RenderMode.SoftwareOnly;
  Reject(()=>test.Advance(Start),"render_experiment_mode_not_confirmed"); test.Dispose();
  Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode,Local022ExperimentDoubles.Windows.Interop.RenderMode.SoftwareOnly);
  Reset(); test=new QS3D.LocalQualification.UiRenderExperiment(Start); Advance(test,0,120);
  Local022ExperimentDoubles.Windows.Media.RenderOptions.FailNextSet=true;
  Reject(()=>test.Dispose(),"setter_failed"); test.Dispose();
  Eq(Local022ExperimentDoubles.Windows.Media.RenderOptions.ProcessRenderMode,Local022ExperimentDoubles.Windows.Interop.RenderMode.Default);
 }
}
'@)
[Local022RenderExperimentReplay]::Run()

foreach($major in 25,26) {
 $path=Join-Path $PSScriptRoot "../../scripts/test-bricscad-v$major-single-footing.ps1"
 $runner=Get-Content $path -Raw
 $verdict=[regex]::Match($runner,'(?m)^\$status = if .*').Value
 if(-not $verdict) { throw 'Missing actual final verdict expression.' }
 foreach($diagnostic in $false,$true) {
  & {
   $RenderExperiment=$diagnostic; $failure=$null; $cleanupFailure=$null; $cleanupOk=$true; $markers=@(1,2,3)
   . ([scriptblock]::Create($verdict))
   $expected=if($diagnostic){'DIAGNOSTIC_ONLY'}else{'LOCAL_PASS_BOUNDED'}
   if($status -cne $expected){throw 'Diagnostic contaminated qualification verdict.'}
  }
 }
 foreach($needle in @('render_experiment = [bool]$RenderExperiment', "'QS3D_LOCAL022_RENDER_EXPERIMENT'", '$env:QS3D_LOCAL022_RENDER_EXPERIMENT = if ($RenderExperiment)')) {
  if(-not $runner.Contains($needle)){throw "Missing frozen/restored diagnostic identity in V$major"}
 }
 $guard=[regex]::Match($runner,'(?ms)^if \(\$RenderExperiment -and .*?^\}').Value
 foreach($interactive in $false,$true){foreach($driver in 'NATIVE_V1','OBSERVED_CLICK_V2'){
  & { $RenderExperiment=$true; $InteractiveUi=$interactive; $UiDriver=$driver; $rejected=$false
   try{ . ([scriptblock]::Create($guard)) } catch { $rejected=$true }
   if($rejected -eq ($interactive -and $driver -ceq 'OBSERVED_CLICK_V2')){throw 'Diagnostic mode gate changed.'}
  }
 }}
}
$controller=Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$experiment=[regex]::Match($controller,'(?ms)^        private static void StartRenderExperiment\(Context context\).*?^        \}').Value
if(-not $experiment -or $experiment.Contains('"PASS"') -or $experiment -match 'AwaitAction|new UiController|WriteUiAction') {
 throw 'Diagnostic must not implement acceptance or authoring.'
}
foreach($token in @('RequireUiContextStable(context);','experiment.Dispose();','"DIAGNOSTIC_ONLY"','QueueOwnedQuit(context, true);')) {
 if(-not $experiment.Contains($token)){throw 'Diagnostic cleanup/non-qualification boundary missing.'}
}
Write-Output 'PASS: actual render experiment timing, mode restoration/failures and both runner verdict/entry gates; no WPF rendering, CAD or input performed.'
