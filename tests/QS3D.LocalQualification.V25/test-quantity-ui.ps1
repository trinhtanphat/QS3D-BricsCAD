$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Compile the actual portable oracle with an isolated namespace. No host SDK,
# production reporting implementation, native input or machine-state access.
$quantityNamespace = 'Local022QuantityReplay_' + [Guid]::NewGuid().ToString('N')
$quantitySource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Local022QuantityOracle.cs') -Raw
$quantitySource = $quantitySource.Replace('namespace QS3D.LocalQualification', "namespace $quantityNamespace")
Add-Type -TypeDefinition ($quantitySource + @"

namespace $quantityNamespace {
    public static class OracleCases {
        private static readonly System.Collections.Generic.Dictionary<string,string> Sources =
            new System.Collections.Generic.Dictionary<string,string>(System.StringComparer.OrdinalIgnoreCase) {
                { "FOUNDATION-A1", "A1" }, { "FOUNDATION-B2", "B2" }
            };
        private static Local022QuantityOracle.Row Row(params string[] ids) {
            return new Local022QuantityOracle.Row {
                FamilyId="fixture-family", Category="Foundation", Fingerprint="fixture-drawing",
                ElementIds=ids, SourceHandles=ids.Select(id=>Sources[id]).ToArray(), Count=ids.Length,
                Gross=ids.Length*(38d/3d), Net=ids.Length*(38d/3d), Deduction=0d,
                GrossEvidence=true, NetEvidence=true, DeductionEvidence=true
            };
        }
        private static void Verify(Local022QuantityOracle.Row[] rows, bool detail) {
            Local022QuantityOracle.Verify(rows,detail,"fixture-family","fixture-drawing",Sources);
        }
        private static void Reject(string name, System.Action action) {
            try { action(); } catch(System.InvalidOperationException) { return; }
            throw new System.Exception("FAIL: quantity oracle accepted "+name);
        }
        private static void RejectSummary(string name, System.Action<Local022QuantityOracle.Row> mutate) {
            var row=Row("FOUNDATION-A1","FOUNDATION-B2"); mutate(row);
            Reject(name,()=>Verify(new[]{row},false));
        }
        public static void Run() {
            Verify(new[]{Row("FOUNDATION-A1","FOUNDATION-B2")},false);
            Verify(new[]{Row("FOUNDATION-A1"),Row("FOUNDATION-B2")},true);
            Verify(new[]{Row("FOUNDATION-B2"),Row("FOUNDATION-A1")},true);
            foreach(double wrong in new[]{32d,16d,24d,double.NaN,double.PositiveInfinity,double.NegativeInfinity}) {
                RejectSummary("gross "+wrong,row=>row.Gross=wrong);
                RejectSummary("net "+wrong,row=>row.Net=wrong);
            }
            foreach(double wrong in new[]{1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
                RejectSummary("deduction "+wrong,row=>row.Deduction=wrong);
            RejectSummary("false gross evidence",row=>row.GrossEvidence=false);
            RejectSummary("false net evidence",row=>row.NetEvidence=false);
            RejectSummary("false deduction evidence",row=>row.DeductionEvidence=false);
            RejectSummary("duplicate ID",row=>row.ElementIds=new[]{"FOUNDATION-A1","FOUNDATION-A1"});
            RejectSummary("case-alias duplicate ID",row=>row.ElementIds=new[]{"FOUNDATION-A1","foundation-a1"});
            RejectSummary("missing ID",row=>row.ElementIds=new[]{"FOUNDATION-A1"});
            RejectSummary("foreign ID",row=>row.ElementIds=new[]{"FOUNDATION-A1","FOUNDATION-C3"});
            RejectSummary("wrong source",row=>row.SourceHandles=new[]{"A1","C3"});
            RejectSummary("generated source substitution",row=>row.SourceHandles=new[]{"D4","E5"});
            RejectSummary("duplicate source",row=>row.SourceHandles=new[]{"A1","A1"});
            RejectSummary("missing source",row=>row.SourceHandles=new[]{"A1"});
            RejectSummary("wrong count",row=>row.Count=1);
            RejectSummary("wrong Family",row=>row.FamilyId="other-family");
            RejectSummary("wrong category",row=>row.Category="Slab");
            RejectSummary("wrong drawing",row=>row.Fingerprint="other-drawing");
            Reject("missing row",()=>Verify(System.Array.Empty<Local022QuantityOracle.Row>(),false));
            Reject("extra summary row",()=>Verify(new[]{Row("FOUNDATION-A1"),Row("FOUNDATION-B2")},false));
            Reject("missing detail row",()=>Verify(new[]{Row("FOUNDATION-A1")},true));
            Reject("duplicate detail element",()=>Verify(new[]{Row("FOUNDATION-A1"),Row("FOUNDATION-A1")},true));
            foreach(double wrong in new[]{16d,24d,32d,4d,12d,double.NaN,double.PositiveInfinity}) {
                var first=Row("FOUNDATION-A1"); first.Gross=wrong; first.Net=wrong;
                Reject("wrong detail volume "+wrong,()=>Verify(new[]{first,Row("FOUNDATION-B2")},true));
            }
        }
    }
}
"@)
([type]($quantityNamespace + '.OracleCases'))::Run()
Write-Output 'PASS: actual quantity oracle requires analytic 38/3 per footing and 76/3 total, complete unique semantic/source identity and true concrete evidence.'

# The real DWG is still held open by CAD. Replay the actual read-only hash
# method with a live write handle; File.OpenRead's FileShare.Read would fail.
$observerSource = Get-Content (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.QuantityUi.cs') -Raw
$openWaitMethod = [regex]::Match($observerSource, '(?ms)^            private static bool QuantityWindowOpenTimedOut\(.*?^            \}').Value
if (-not $openWaitMethod) { throw 'FAIL: actual quantity window-open wait policy missing.' }
$openWaitTypeName = 'Local022QuantityOpenWait_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition (@"
using System;
public static class $openWaitTypeName {
$openWaitMethod
public static void Run() {
 var start=new DateTime(2026,9,8,0,0,0,DateTimeKind.Utc);
 if(QuantityWindowOpenTimedOut(start,start,false)) throw new Exception("immediate opening rejected");
 if(QuantityWindowOpenTimedOut(start,start.AddSeconds(59.999),false)) throw new Exception("opening grace shortened");
 if(!QuantityWindowOpenTimedOut(start,start.AddSeconds(60),false)) throw new Exception("missing window still waits for operator deadline");
 if(!QuantityWindowOpenTimedOut(start,start.AddMinutes(2),false)) throw new Exception("late missing window accepted");
 if(QuantityWindowOpenTimedOut(start,start.AddMinutes(54),true)) throw new Exception("physical operator wait shortened");
}
}
"@)
([type]$openWaitTypeName)::Run()
# Execute the real Tick opening prefix with only host window enumeration and
# the clock replaced. This catches a timeout that runs before current discovery.
$tickPrefix = [regex]::Match($observerSource, '(?s)RequireUiContextStable\(_context\);\s*if \(DateTime.UtcNow >= _deadline\).*?(?=\s*_window = windows\[0\];)').Value
if (-not $tickPrefix) { throw 'FAIL: actual observer opening prefix missing.' }
$tickPrefix = [regex]::Replace($tickPrefix, '(?s)var windows = PresentationSource.CurrentSources.*?\.ToArray\(\);', 'var windows = Enumerable.Range(0, windowCount).Select(x => new object()).ToArray();')
$tickPrefix = $tickPrefix.Replace('DateTime.UtcNow','_now')
$tickTypeName='Local022QuantityOpening_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition (@"
using System;
using System.Linq;
public sealed class $tickTypeName {
 object _window; readonly object _context = new object();
 DateTime _startedUtc, _deadline, _now;
 static void RequireUiContextStable(object context) { }
 sealed class ProbeException : Exception { public ProbeException(string code):base(code){} }
$openWaitMethod
 void Tick(int windowCount) {
$tickPrefix
 _window = windows[0];
 }
 }
 public static void Run() {
  var start=new DateTime(2026,9,8,0,0,0,DateTimeKind.Utc);
  var test=new $tickTypeName { _startedUtc=start, _deadline=start.AddMinutes(55), _now=start.AddSeconds(60.1) };
  test.Tick(1);
  if(test._window==null) throw new Exception("visible near-boundary window was not observed");
  test._window=null;
  try { test.Tick(0); } catch(ProbeException e) {
   if(e.Message=="quantity_product_window_not_opened") return;
   throw;
  }
  throw new Exception("absent window after boundary did not fail");
 }
}
"@)
([type]$tickTypeName)::Run()
Write-Output 'PASS: actual missing-window policy stops at 60 seconds without shortening the physical operator window.'
$hashMethod = [regex]::Match($observerSource, '(?ms)^            private static string HashFile\(string path\).*?^            \}').Value
if (-not $hashMethod) { throw 'FAIL: actual quantity file hash method missing.' }
$hashTypeName = 'Local022SharedHash_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition (@"
using System;
using System.IO;
using System.Linq;
using System.Globalization;
public static class $hashTypeName {
$hashMethod
public static void Run() {
 var path=Path.GetTempFileName();
 try {
  using(var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.ReadWrite)) {
   held.Write(new byte[]{1,2,3,4},0,4); held.Flush(true);
   var actual=HashFile(path);
   if(actual!="9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a") throw new Exception("shared read hash differs");
   if(held.Length!=4) throw new Exception("hash changed fixture");
  }
 } finally { File.Delete(path); }
}
}
"@)
([type]$hashTypeName)::Run()
Write-Output 'PASS: actual quantity hash reads a write-open fixture without changing it; private temporary fixture removed.'

. (Join-Path $PSScriptRoot '../../scripts/local022-ui-input.ps1')
$quantityRunId = '0123456789abcdef0123456789abcdef'
$quantityCommon = @('actual_product_bq_window','exact_displayed_rows_and_totals','analytic_footing_volume','exact_element_source_identity','reporting_readonly','observed_window_closed')
$quantityChecks = @{
    quantity = $quantityCommon + @('recalculate_click_fresh_rows','detail_summary_clicks','locate_click_native_selection')
    quantityreopen = $quantityCommon + @('cold_bq_same_quantities')
}
function New-QuantityTestMarker([string]$Phase) {
    $checks = [ordered]@{}
    foreach ($check in $quantityChecks[$Phase]) { $checks[$check] = $true }
    [pscustomobject]@{schema='QS3D_LOCAL022_NATIVE_UI_V1';run_id=$quantityRunId;phase=$Phase;status='PASS';stage=$Phase;error_code='NONE';checks=[pscustomobject]$checks}
}
function Assert-QuantityTestRejected([string]$Name, [scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw ('FAIL: quantity guard accepted ' + $Name) }
}
function Test-QuantityMarkerMutation([string]$Name, [string]$Phase, [scriptblock]$Mutation) {
    $marker = New-QuantityTestMarker $Phase
    & $Mutation $marker
    Assert-QuantityTestRejected $Name { Assert-Local022QuantityPhase $marker $quantityRunId $Phase }
}
foreach ($phase in @('quantity','quantityreopen')) {
    $null = Assert-Local022QuantityPhase (New-QuantityTestMarker $phase) $quantityRunId $phase
    foreach ($field in @('schema','run_id','phase','status','stage','error_code','checks')) {
        Test-QuantityMarkerMutation "missing $phase/$field" $phase { param($m) $m.PSObject.Properties.Remove($field) }
        foreach ($hidden in @([char]0,[char]10,[char]0x00ad)) {
            Test-QuantityMarkerMutation "hidden key $phase/$field" $phase {
                param($m) $value=$m.$field; $m.PSObject.Properties.Remove($field); $m | Add-Member NoteProperty ($field + $hidden) $value
            }
        }
    }
    foreach ($field in @('schema','run_id','phase','status','stage','error_code')) {
        foreach ($hidden in @([char]0,[char]10,[char]0x00ad)) {
            Test-QuantityMarkerMutation "hidden value $phase/$field" $phase { param($m) $m.$field += $hidden }
        }
        Test-QuantityMarkerMutation "array value $phase/$field" $phase { param($m) $m.$field = @($m.$field) }
        Test-QuantityMarkerMutation "null value $phase/$field" $phase { param($m) $m.$field = $null }
    }
    Test-QuantityMarkerMutation "extra $phase field" $phase { param($m) $m | Add-Member NoteProperty extra $true }
    Test-QuantityMarkerMutation "wrong $phase nonce" $phase { param($m) $m.run_id = 'f' * 32 }
    Test-QuantityMarkerMutation "wrong $phase schema" $phase { param($m) $m.schema = 'QS3D_LOCAL022_NATIVE_V3' }
    Test-QuantityMarkerMutation "failed $phase" $phase { param($m) $m.status = 'FAIL' }
    Test-QuantityMarkerMutation "wrong $phase stage" $phase { param($m) $m.stage = 'quantity_failure' }
    Test-QuantityMarkerMutation "wrong $phase error" $phase { param($m) $m.error_code = 'QUANTITY_FAILED' }
    Test-QuantityMarkerMutation "null $phase checks" $phase { param($m) $m.checks = $null }
    Test-QuantityMarkerMutation "array $phase checks" $phase { param($m) $m.checks = @($m.checks) }
    Test-QuantityMarkerMutation "extra $phase check" $phase { param($m) $m.checks | Add-Member NoteProperty extra $true }
    foreach ($check in $quantityChecks[$phase]) {
        Test-QuantityMarkerMutation "missing $phase/$check" $phase { param($m) $m.checks.PSObject.Properties.Remove($check) }
        foreach ($badValue in @($false,'true',1,$null)) {
            Test-QuantityMarkerMutation "non-true-Boolean $phase/$check" $phase { param($m) $m.checks.$check = $badValue }
        }
        Test-QuantityMarkerMutation "hidden $phase/$check" $phase {
            param($m) $m.checks.PSObject.Properties.Remove($check); $m.checks | Add-Member NoteProperty ($check + [char]0x00ad) $true
        }
    }
    foreach ($badRunId in @('',('a'*31),($quantityRunId+[char]0),($quantityRunId+[char]0x00ad))) {
        Assert-QuantityTestRejected 'invalid expected nonce' { Assert-Local022QuantityPhase (New-QuantityTestMarker $phase) $badRunId $phase }
    }
    foreach ($badPhase in @('ui','run','Quantity',($phase+[char]0x00ad))) {
        Assert-QuantityTestRejected 'invalid expected phase' { Assert-Local022QuantityPhase (New-QuantityTestMarker $phase) $quantityRunId $badPhase }
    }
}
Write-Output 'PASS: both quantity phase gates reject missing/extra/hidden fields, foreign identity, failure metadata and every absent/false/non-Boolean assertion.'

function New-QuantityTestPredecessor {
    [pscustomobject]@{
        receipt=[pscustomobject]@{run_id=$quantityRunId;quantity_ui_executed=$true;quantity_phases_verified=2;render_experiment=$false}
        allocation=[pscustomobject]@{run_id=$quantityRunId;quantity_ui=$true;render_experiment=$false}
    }
}
function Test-QuantityPredecessorMutation([string]$Name, [scriptblock]$Mutation) {
    $pair = New-QuantityTestPredecessor
    & $Mutation $pair
    Assert-QuantityTestRejected $Name { Assert-Local022QuantityPredecessor $pair.receipt $pair.allocation }
}
$quantityPair = New-QuantityTestPredecessor
Assert-Local022QuantityPredecessor $quantityPair.receipt $quantityPair.allocation
$quantityPair.receipt.quantity_phases_verified = [long]2
Assert-Local022QuantityPredecessor $quantityPair.receipt $quantityPair.allocation
foreach ($side in @('receipt','allocation')) {
    $fields = if ($side -ceq 'receipt') { @('run_id','quantity_ui_executed','quantity_phases_verified','render_experiment') } else { @('run_id','quantity_ui','render_experiment') }
    foreach ($field in $fields) {
        Test-QuantityPredecessorMutation "missing predecessor $side/$field" { param($p) $p.$side.PSObject.Properties.Remove($field) }
        Test-QuantityPredecessorMutation "hidden predecessor $side/$field" {
            param($p) $value=$p.$side.$field; $p.$side.PSObject.Properties.Remove($field); $p.$side | Add-Member NoteProperty ($field+[char]0x00ad) $value
        }
    }
    foreach ($badRunId in @('',('a'*31),('f'*32),($quantityRunId+[char]0x00ad),($quantityRunId+[char]0))) {
        Test-QuantityPredecessorMutation "wrong predecessor $side nonce" { param($p) $p.$side.run_id = $badRunId }
    }
    Test-QuantityPredecessorMutation "array predecessor $side nonce" { param($p) $p.$side.run_id = @($quantityRunId) }
    foreach ($badValue in @($true,'false',0,$null)) {
        Test-QuantityPredecessorMutation "diagnostic predecessor $side" { param($p) $p.$side.render_experiment = $badValue }
    }
    $modeField = if ($side -ceq 'receipt') { 'quantity_ui_executed' } else { 'quantity_ui' }
    foreach ($badValue in @($false,'true',1,$null)) {
        Test-QuantityPredecessorMutation "unproved predecessor $side mode" { param($p) $p.$side.$modeField = $badValue }
    }
}
foreach ($badCount in @(0,1,3,'2',[double]2,$true,$null)) {
    Test-QuantityPredecessorMutation 'incomplete/non-integer quantity phase count' { param($p) $p.receipt.quantity_phases_verified = $badCount }
}
Write-Output 'PASS: quantity predecessor requires two actual quantity phases, explicit mode, matching canonical allocation and non-diagnostic receipts.'

# Replay only each actual final status expression, isolated from runner effects.
foreach ($major in @(25,26)) {
    $runnerPath = Join-Path $PSScriptRoot "../../scripts/test-bricscad-v$major-single-footing.ps1"
    $tokens=$null; $parseErrors=$null
    $runnerAst = [Management.Automation.Language.Parser]::ParseFile($runnerPath,[ref]$tokens,[ref]$parseErrors)
    if ($parseErrors.Count) { throw "FAIL: V$major runner parse errors." }
    $quantityPoll = $runnerAst.Find({ param($node)
        $node -is [Management.Automation.Language.IfStatementAst] -and
        $node.Clauses[0].Item1.Extent.Text -ceq '$QuantityUi' -and
        $node.Extent.Text.Contains('$quantityPhase') -and $node.Extent.Text.Contains('Read-Phase')
    },$true)
    if ($null -eq $quantityPoll) { throw "FAIL: V$major runner does not observe early quantity failure." }
    & {
        # Isolate the actual polling statement; no process, registry or file access.
        $ArtifactDir='fixture-only'; $runId=$quantityRunId; $QuantityUi=$true
        $script:quantityPollExists=$true; $script:quantityPollReads=@()
        function Test-Path { param([string]$LiteralPath) return $script:quantityPollExists }
        function Read-Phase([string]$Phase) {
            $script:quantityPollReads += $Phase
            return Assert-Local022QuantityPhase $script:quantityPollMarker $runId $Phase
        }
        foreach ($Phase in @('run','reopen')) {
            $expectedPhase=if($Phase -ceq 'run'){'quantity'}else{'quantityreopen'}
            $script:quantityPollMarker=New-QuantityTestMarker $expectedPhase
            & ([scriptblock]::Create($quantityPoll.Extent.Text))
            if ($script:quantityPollReads[-1] -cne $expectedPhase) { throw 'FAIL: wrong live quantity phase polled.' }
            $script:quantityPollMarker.status='FAIL'
            Assert-QuantityTestRejected 'early quantity failure while host remains live' { & ([scriptblock]::Create($quantityPoll.Extent.Text)) }
            $script:quantityPollMarker=New-QuantityTestMarker $expectedPhase
            $script:quantityPollMarker.run_id='f'*32
            Assert-QuantityTestRejected 'foreign live quantity marker' { & ([scriptblock]::Create($quantityPoll.Extent.Text)) }
        }
        $script:quantityPollReads=@(); $script:quantityPollExists=$false
        & ([scriptblock]::Create($quantityPoll.Extent.Text))
        if($script:quantityPollReads.Count) { throw 'FAIL: absent quantity marker read.' }
        $QuantityUi=$false; $script:quantityPollExists=$true
        & ([scriptblock]::Create($quantityPoll.Extent.Text))
        if($script:quantityPollReads.Count) { throw 'FAIL: non-quantity mode polled quantity marker.' }
    }
    $statusNode = $runnerAst.Find({ param($node)
        $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -ceq '$status'
    },$true)
    if ($null -eq $statusNode) { throw "FAIL: V$major final status expression missing." }
    & {
        $failure=$null; $cleanupFailure=$null; $cleanupOk=$true; $RenderExperiment=$false
        $QuantityUi=$true; $markers=@(1,2,3)
        foreach ($count in @(0,1,2,3)) {
            $quantityMarkers = @(for ($index=0; $index -lt $count; $index++) { $index })
            & ([scriptblock]::Create($statusNode.Extent.Text))
            # The assignment executes in its own scope; evaluate the RHS directly.
            $actualStatus = & ([scriptblock]::Create($statusNode.Right.Extent.Text))
            $expectedStatus = if ($count -eq 2) { 'LOCAL_PASS_BOUNDED' } else { 'FAIL_OR_NO_RESULT' }
            if ($actualStatus -cne $expectedStatus) { throw "FAIL: V$major quantity status admitted $count markers." }
        }
        $quantityMarkers=@(1,2); $markers=@(1,2)
        if ((& ([scriptblock]::Create($statusNode.Right.Extent.Text))) -cne 'FAIL_OR_NO_RESULT') { throw 'FAIL: quantity mode bypassed native phases.' }
        $markers=@(1,2,3); $RenderExperiment=$true
        if ((& ([scriptblock]::Create($statusNode.Right.Extent.Text))) -cne 'DIAGNOSTIC_ONLY') { throw 'FAIL: render experiment became acceptance.' }
        $RenderExperiment=$false; $QuantityUi=$false; $quantityMarkers=@()
        if ((& ([scriptblock]::Create($statusNode.Right.Extent.Text))) -cne 'LOCAL_PASS_BOUNDED') { throw 'FAIL: native-only status contract changed.' }
    }
}
Write-Output 'PASS: both actual runner status expressions require all native and quantity markers; diagnostics never become acceptance, native-only mode remains unchanged.'
Write-Output 'PASS: both actual live polling statements reject failed or foreign quantity markers before host exit; absent markers and native-only mode do not trigger reads.'
