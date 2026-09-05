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
foreach ($value in @('0','2000','1000.5','-25')) {
    $action = New-Action; $action.action='text'; $action.text=$value
    $null = Assert-Local022UiAction $action $nonce 1 12345
}
foreach ($value in @('ENTER','ESC')) {
    $action = New-Action; $action.action='key'; $action.text=$value
    $null = Assert-Local022UiAction $action $nonce 1 12345
}
# This invokes the real input boundary with a non-host process. It must reject
# before initializing native input or sending any event to the desktop.
$rejected = $false
try { Invoke-Local022UiPhysicalAction (New-Action) (Get-Process -Id $PID) 'C:\not-the-owned-host\bricscad.exe' } catch { $rejected = $true }
if (-not $rejected) { throw 'FAIL: native input accepted a non-host target.' }
Write-Output 'PASS: LOCAL022 UI action validation and non-host refusal (23 cases); no desktop input sent.'

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
