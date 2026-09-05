$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$library = Join-Path $PSScriptRoot 'local022-ui-input.ps1'
if (-not (Test-Path -LiteralPath $library)) { throw 'FAIL: the guarded UI input consumer is not implemented.' }
. $library
$nonce = '0123456789abcdef0123456789abcdef'
function New-Action {
    [pscustomobject]@{ schema='QS3D_LOCAL022_UI_ACTION_V1'; run_id=$nonce; sequence=1; action='click'; x=100; y=200; text=''; target_pid=12345 }
}
function Expect-Rejected([string]$Name, [scriptblock]$Change) {
    $action = New-Action
    & $Change $action
    $rejected = $false
    try { $null = Assert-Local022UiAction $action $nonce 1 12345 } catch { $rejected = $true }
    if (-not $rejected) { throw "FAIL: accepted $Name" }
}
$accepted = Assert-Local022UiAction (New-Action) $nonce 1 12345
if ($accepted.action -cne 'click') { throw 'FAIL: valid click was not retained.' }
$hover = New-Action; $hover.action='move'
$null = Assert-Local022UiAction $hover $nonce 1 12345
Expect-Rejected 'another process' { param($a) $a.target_pid = 54321 }
Expect-Rejected 'another allocation' { param($a) $a.run_id = 'fedcba9876543210fedcba9876543210' }
Expect-Rejected 'replayed sequence' { param($a) $a.sequence = 0 }
Expect-Rejected 'string sequence' { param($a) $a.sequence = '1' }
Expect-Rejected 'non-finite coordinate' { param($a) $a.x = [double]::NaN }
Expect-Rejected 'fractional coordinate' { param($a) $a.x = 1.5 }
Expect-Rejected 'unknown command' { param($a) $a.action = 'shell' }
Expect-Rejected 'unexpected field' { param($a) $a | Add-Member NoteProperty extra 'untrusted' }
Expect-Rejected 'command text' { param($a) $a.action='text'; $a.text='_.QUIT' }
Expect-Rejected 'newline in text' { param($a) $a.action='text'; $a.text="100`nQUIT" }
Expect-Rejected 'unsupported key' { param($a) $a.action='key'; $a.text='ALT+F4' }
Expect-Rejected 'click carrying text' { param($a) $a.text='100' }
Expect-Rejected 'non-ASCII digits' { param($a) $a.action='text'; $a.text=[string][char]0x0661 }
Expect-Rejected 'array allocation identity' { param($a) $a.run_id=@($a.run_id) }
Expect-Rejected 'array schema identity' { param($a) $a.schema=@($a.schema) }
Expect-Rejected 'hidden schema character' { param($a) $a.schema += [char]0x00ad }
Expect-Rejected 'hidden nonce character' { param($a) $a.run_id += [char]0x00ad }
Expect-Rejected 'NUL schema character' { param($a) $a.schema += [char]0 }
Expect-Rejected 'NUL nonce character' { param($a) $a.run_id += [char]0 }
Expect-Rejected 'hidden action character' { param($a) $a.action += [char]0x00ad }
Expect-Rejected 'hidden key character' { param($a) $a.action='key'; $a.text='ENTER' + [char]0x00ad }
Expect-Rejected 'trailing numeric newline' { param($a) $a.action='text'; $a.text="100`n" }
foreach ($value in @('0','2000','1000.5','-25')) {
    $action = New-Action; $action.action='text'; $action.text=$value
    $null = Assert-Local022UiAction $action $nonce 1 12345
}
foreach ($value in @('ENTER','ESC')) {
    $action = New-Action; $action.action='key'; $action.text=$value
    $null = Assert-Local022UiAction $action $nonce 1 12345
}
# Exercise the actual file-consumer decoder, before normalized JSON can hide
# duplicate/escaped property identifiers. No native input is initialized here.
$canonical = '{"schema":"QS3D_LOCAL022_UI_ACTION_V1","run_id":"0123456789abcdef0123456789abcdef","sequence":1,"action":"click","x":100,"y":200,"text":"","target_pid":12345}'
$decoded = ConvertFrom-Local022UiActionJson $canonical $nonce 1 12345
if ($decoded.x -ne 100) { throw 'FAIL: canonical action did not retain its coordinate.' }
foreach ($raw in @(
    $canonical.Replace('"click"','"move"'),
    $canonical.Replace('"click"','"text"').Replace('"text":""','"text":"-25.5"'),
    $canonical.Replace('"click"','"key"').Replace('"text":""','"text":"ENTER"'),
    $canonical.Replace('"click"','"key"').Replace('"text":""','"text":"ESC"'),
    $canonical.Replace('"x":100','"x":-32768').Replace('"y":200','"y":32767')
)) {
    $null = ConvertFrom-Local022UiActionJson $raw $nonce 1 12345
}
$invalidJson = [ordered]@{
    'escaped duplicate coordinate' = $canonical.TrimEnd('}') + ',"\u0078":101}'
    'escaped duplicate schema' = $canonical.TrimEnd('}') + ',"\u0073chema":"QS3D_LOCAL022_UI_ACTION_V1"}'
    'plain duplicate coordinate' = $canonical.TrimEnd('}') + ',"x":101}'
    'unexpected raw field' = $canonical.TrimEnd('}') + ',"extra":1}'
    'missing raw field' = $canonical.Replace(',"y":200','')
    'renamed raw field' = $canonical.Replace('"x":100','"wrong":100')
    'escaped sole coordinate name' = $canonical.Replace('"x":100','"\u0078":100')
    'escaped action value' = $canonical.Replace('"click"','"cl\u0069ck"')
    'reordered fields' = $canonical.Replace('"x":100,"y":200','"y":200,"x":100')
    'alternate integer representation' = $canonical.Replace('"x":100','"x":1e2')
    'leading whitespace' = ' ' + $canonical
    'trailing newline' = $canonical + "`n"
    'comment before field' = $canonical.Replace('"x":100','/* ambiguous */"x":100')
    'array wrapper' = '[' + $canonical + ']'
    'malformed JSON' = $canonical.TrimEnd('}')
    'literal soft-hyphen schema' = $canonical.Replace('QS3D_LOCAL022_UI_ACTION_V1', ('QS3D_LOCAL022_UI_ACTION_V1' + [char]0x00ad))
    'escaped soft-hyphen schema' = $canonical.Replace('QS3D_LOCAL022_UI_ACTION_V1', 'QS3D_LOCAL022_UI_ACTION_V1\u00ad')
    'literal soft-hyphen nonce' = $canonical.Replace($nonce, ($nonce + [char]0x00ad))
    'escaped soft-hyphen nonce' = $canonical.Replace($nonce, ($nonce + '\u00ad'))
    'escaped NUL schema' = $canonical.Replace('QS3D_LOCAL022_UI_ACTION_V1', 'QS3D_LOCAL022_UI_ACTION_V1\u0000')
    'escaped NUL nonce' = $canonical.Replace($nonce, ($nonce + '\u0000'))
    'escaped trailing numeric newline' = $canonical.Replace('"click"','"text"').Replace('"text":""','"text":"100\n"')
}
foreach ($case in $invalidJson.GetEnumerator()) {
    $rejected = $false
    try { $null = ConvertFrom-Local022UiActionJson $case.Value $nonce 1 12345 } catch { $rejected = $true }
    if (-not $rejected) { throw ('FAIL: raw UI decoder accepted ' + $case.Key) }
}
Write-Output 'PASS: raw UI decoder rejects escaped/plain duplicate keys and wrong fields.'
# This invokes the real input boundary with a non-host process. It must reject
# before initializing native input or sending any event to the desktop.
$rejected = $false
try { Invoke-Local022UiPhysicalAction (New-Action) (Get-Process -Id $PID) 'C:\not-the-owned-host\bricscad.exe' } catch { $rejected = $true }
if (-not $rejected) { throw 'FAIL: native input accepted a non-host target.' }
Write-Output 'PASS: LOCAL022 UI action validation, hidden-character rejection and non-host refusal; no desktop input sent.'

$captureRejected = $false
& {
    $ErrorActionPreference = 'Continue'
    try { Save-Local022OwnedWindow (Get-Process -Id $PID) 'unused-must-not-capture.png' } catch { $script:captureRejected = $true }
}
if (-not $captureRejected -or (Test-Path -LiteralPath 'unused-must-not-capture.png')) { throw 'FAIL: non-host screenshot did not fail closed.' }
Write-Output 'PASS: capture rejects non-hosts even when caller error policy is Continue.'

# Exercise the actual final decision, isolated from all host/file operations.
# A FAIL_OR_NO_RESULT receipt must never finish with a successful exit simply
# because neither exception slot was populated.
foreach ($major in @(25,26)) {
    $path = Join-Path $PSScriptRoot "test-bricscad-v$major-single-footing.ps1"
    $tokens = $null; $parseErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$parseErrors)
    if ($parseErrors.Count) { throw 'Runner parse failed.' }
    $decision = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.IfStatementAst] })[-1]
    $failure = $null; $cleanupFailure = $null; $status = 'FAIL_OR_NO_RESULT'; $ArtifactDir = 'unused'
    function Write-Json { param($Path,$Value) }
    $rejected = $false
    try { & ([scriptblock]::Create($decision.Extent.Text)) } catch { $rejected = $true }
    if (-not $rejected) { throw "FAIL: V$major failure receipt can exit successfully." }
}
Write-Output 'PASS: both runners reject non-PASS final receipts.'

if (-not (Get-Command Assert-Local022UiPhase -ErrorAction SilentlyContinue)) { throw 'FAIL: UI phase coverage validator is not implemented.' }
$phaseCases = @{
    ui = @('active_disposable_drawing','mcp_mutation_boundary_paused','workspace_visible','single_footing_tree_clicked','cancel_nonmutation','six_field_dialog_layout','six_field_physical_input','active_family_h2_zero','two_physical_centres','enter_command_termination','family_h2_physical_edit','existing_geometry_regenerated','former_generated_handles_erased','repeat_physical_centre','escape_command_termination','geometry_ownership_extents','exact_semantic_native_cardinality','physical_receipts_complete','saved_exact_artifact_digest')
    uisaved = @('active_disposable_drawing','mcp_mutation_boundary_paused','same_process_ui_state','sidecar_exists_after_qs3dsave','qsave_command_completed','saved_semantic_native_state','saved_exact_artifact_digest','saved_exact_cardinality')
    uireopen = @('active_disposable_drawing','mcp_mutation_boundary_paused','cold_project_bind','reopened_family_identity','reopened_semantic_identity','reopened_generated_solids_live','reopened_dimensions_volume_extents','reopened_exact_artifact_digest','reopened_exact_cardinality')
}
foreach ($phase in $phaseCases.Keys) {
    $checks = [ordered]@{}
    foreach ($check in $phaseCases[$phase]) { $checks[$check]=$true }
    $marker = [pscustomobject]@{schema='QS3D_LOCAL022_NATIVE_UI_V1';run_id=$nonce;phase=$phase;status='PASS';stage=$phase;error_code='NONE';checks=[pscustomobject]$checks}
    $null = Assert-Local022UiPhase $marker $nonce $phase
    foreach ($field in @('schema','run_id','phase','status','stage','error_code')) {
        $original = $marker.$field
        foreach ($hidden in @([char]0x00ad,[char]0,[char]10)) {
            $marker.$field = $original + $hidden
            $rejected = $false
            try { $null=Assert-Local022UiPhase $marker $nonce $phase } catch { $rejected=$true }
            if (-not $rejected) { throw "FAIL: hidden character accepted in $phase marker/$field" }
        }
        $marker.$field = $original
    }
    $checkName = $phaseCases[$phase][0]
    $marker.checks.PSObject.Properties.Remove($checkName)
    $marker.checks | Add-Member NoteProperty ($checkName + [char]0x00ad) $true
    $rejected = $false
    try { $null=Assert-Local022UiPhase $marker $nonce $phase } catch { $rejected=$true }
    if (-not $rejected) { throw 'FAIL: hidden character accepted in assertion name.' }
    $marker.checks.PSObject.Properties.Remove($checkName + [char]0x00ad)
    $marker.checks | Add-Member NoteProperty $checkName $true
    foreach ($check in $phaseCases[$phase]) {
        $marker.checks.$check=$false
        $rejected=$false
        try { $null=Assert-Local022UiPhase $marker $nonce $phase } catch { $rejected=$true }
        if (-not $rejected) { throw "FAIL: accepted unproved UI check $phase/$check" }
        $marker.checks.$check=$true
    }
    $marker.checks.PSObject.Properties.Remove($phaseCases[$phase][0])
    $rejected=$false
    try { $null=Assert-Local022UiPhase $marker $nonce $phase } catch { $rejected=$true }
    if (-not $rejected) { throw 'FAIL: accepted missing UI coverage.' }
}
Write-Output 'PASS: complete UI/save/reopen assertion sets required; every false/missing check rejected.'

$rejected=$false
try {
    & (Join-Path $PSScriptRoot 'run-local022-ui-qualification.ps1') -HostMajor 25 `
        -HarnessSha ('a' * 40) -AllocationName 'test-no-consent' -PackageRoot 'unused' `
        -ConfirmTemporaryAutostartPause:$false
} catch {
    if ($_.Exception.Message -cne 'Explicit temporary-autostart authorization required.') { throw }
    $rejected=$true
}
if (-not $rejected) { throw 'FAIL: orchestration ignored missing autostart authorization.' }
Write-Output 'PASS: orchestrator refuses missing consent before inspecting or modifying machine state.'

# Required regression: replay cleanup against in-memory host doubles, never CAD.
& (Join-Path $PSScriptRoot '../tests/QS3D.LocalQualification.V25/test-ui-quit-boundary.ps1')
& (Join-Path $PSScriptRoot '../tests/QS3D.LocalQualification.V25/test-ui-placement-boundary.ps1')

# Replay the actual new loop admission before any possible UI input.
foreach ($major in @(25,26)) {
    $runner = Get-Content (Join-Path $PSScriptRoot "test-bricscad-v$major-single-footing.ps1") -Raw
    $guard = [regex]::Match($runner, '(?m)^        if \(\$InteractiveUi -and \(Test-Path -LiteralPath \$markerPath\)\) \{ \[void\]\(Read-Phase \$Phase\) \}').Value
    if (-not $guard -or $runner.IndexOf($guard) -gt $runner.IndexOf('if (Invoke-Local022UiPendingAction')) { throw 'FAIL: marker admission must precede input.' }
    & {
        $InteractiveUi = $true; $markerPath = $PSCommandPath; $Phase = 'ui'; $testStatus = 'FAIL'
        function Read-Phase([string]$phase) { if ($testStatus -ceq 'FAIL') { throw 'TEST_PHASE_FAILED' }; return $true }
        $rejected = $false
        try { & ([scriptblock]::Create($guard)) } catch { if ($_.Exception.Message -cne 'TEST_PHASE_FAILED') { throw }; $rejected = $true }
        if (-not $rejected) { throw 'FAIL: failed phase allowed input loop continuation.' }
        $testStatus = 'PASS'
        & ([scriptblock]::Create($guard))
    }
}
Write-Output 'PASS: both native loops reject failed atomic UI markers before further input and retain PASS wait behavior.'

# Replay the actual activation guard with an in-memory native/COM double.
$inputSource = Get-Content $library -Raw
$activation = [regex]::Match($inputSource, '(?ms)^    if \(-not \[Qs3dLocal022Input\]::IsForegroundOwned\(\$Process.Id\)\) \{.*?^    \}\r?\n    \[Qs3dLocal022Input\]::RequireForeground\(\$Process.Id\)').Value
if (-not $activation) { throw 'FAIL: already-owned focus is not preserved before UI input.' }
$activationType = 'Local022ActivationReplay_' + [Guid]::NewGuid().ToString('N')
Add-Type -TypeDefinition @"
using System;
public static class $activationType {
    public static bool Owned, AllowActivation = true;
    public static int Activations;
    public static bool IsForegroundOwned(int process) { return Owned; }
    public static bool ActivateOwned(IntPtr window,int process) { Activations++; Owned=AllowActivation; return Owned; }
    public static void RequireForeground(int process) { if (!Owned) throw new Exception("TEST_FOREIGN_FOCUS"); }
}
"@
$activation = $activation.Replace('Qs3dLocal022Input',$activationType).Replace('AddSeconds(5)','AddSeconds(-1)')
& {
    $Process = [pscustomobject]@{ Id=12345 }; $window=[IntPtr]123
    function New-Object { param($ComObject) return [pscustomobject]@{} | Add-Member ScriptMethod AppActivate { param($Id) return $true } -PassThru }
    function Start-Sleep { param($Milliseconds) }
    ([type]$activationType)::Owned = $true
    & ([scriptblock]::Create($activation))
    if (([type]$activationType)::Activations -ne 0) { throw 'FAIL: existing owned foreground was reactivated.' }
    ([type]$activationType)::Owned = $false
    & ([scriptblock]::Create($activation))
    if (([type]$activationType)::Activations -ne 1) { throw 'FAIL: foreign foreground did not require guarded activation.' }
    ([type]$activationType)::Owned = $false
    ([type]$activationType)::AllowActivation = $false
    $rejected=$false
    try { & ([scriptblock]::Create($activation)) } catch { if ($_.Exception.Message -notmatch 'TEST_FOREIGN_FOCUS') { throw }; $rejected=$true }
    if (-not $rejected) { throw 'FAIL: failed activation allowed input.' }
}
Write-Output 'PASS: actual activation guard preserves owned focus, activates only when necessary and refuses failed activation; no native input.'

# Inspect and replay the actual V26 UI startup command array without launching CAD.
$v26Source = Get-Content (Join-Path $PSScriptRoot 'test-bricscad-v26-single-footing.ps1') -Raw
$startup = [regex]::Match($v26Source, "(?m)^        \`$markers \+= Invoke-NativePhase 'ui' @\([^\r\n]+\)").Value
if (-not $startup) { throw 'FAIL: missing V26 UI startup sequence.' }
& {
    $markers = @()
    function Invoke-NativePhase([string]$phase,[string[]]$commands) {
        $expected = @('OSMODE','0','SNAPMODE','0','DYNMODE','0','QS3D','_.-TOOLPANEL','Tips','_Hide','_.PROPERTIESCLOSE','QL22UI')
        if ($phase -cne 'ui' -or [string]::Join('|',$commands) -cne [string]::Join('|',$expected)) {
            throw 'FAIL: V26 UI must hide only native Tips/Properties in its disposable profile before qualification.'
        }
    }
    & ([scriptblock]::Create($startup))
}
Write-Output 'PASS: actual V26 startup hides only native Tips/Properties before UI qualification; no CAD or input executed.'

& (Join-Path $PSScriptRoot '..\tests\QS3D.LocalQualification.V25\test-ui-pick-witness.ps1')
& (Join-Path $PSScriptRoot '..\tests\QS3D.LocalQualification.V25\test-ui-observed-protocol.ps1')
