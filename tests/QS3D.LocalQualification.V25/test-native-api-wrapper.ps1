$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$wrapperPath = Join-Path $PSScriptRoot '../../scripts/run-local022-ui-qualification.ps1'
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($wrapperPath, [ref]$null, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'FAIL: qualification wrapper parse errors.' }
function Get-WrapperAssignment([string]$Name) {
    $nodes = @($ast.FindAll({ param($node)
        $node -is [Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left -is [Management.Automation.Language.VariableExpressionAst] -and
        $node.Left.VariablePath.UserPath -ceq $Name
    }, $true))
    if ($nodes.Count -ne 1) { throw "FAIL: ambiguous wrapper assignment $Name." }
    return $nodes[0]
}
$firstSetup = Get-WrapperAssignment 'taskRepo'
$entryStatements = @($ast.EndBlock.Statements | Where-Object { $_.Extent.EndOffset -le $firstSetup.Extent.StartOffset })
$entry = [scriptblock]::Create($ast.ParamBlock.Extent.Text + "`n" +
    (($entryStatements | ForEach-Object { $_.Extent.Text }) -join "`n") +
    "`n[pscustomobject]@{ NativeApi = [bool]`$NativeApi; UiDriver = `$UiDriver; Pause = [bool]`$PauseForOperator; Policy = `$operatorWaitPolicy }")
$entryArguments = @{
    HostMajor=25; HarnessSha=('a' * 40); AllocationName='host-free-native';
    PackageRoot='unused-no-machine-access'; ConfirmTemporaryAutostartPause=$true
}
$default = & $entry @entryArguments
if ($default.NativeApi -or $default.UiDriver -cne 'NATIVE_V1' -or $default.Pause -or $default.Policy -cne 'WALL_CLOCK_V1') {
    throw 'FAIL: existing default UI mode changed.'
}
$native = & $entry @entryArguments -NativeApi
if (-not $native.NativeApi -or $native.UiDriver -cne 'NATIVE_V1' -or $native.Pause -or $native.Policy -cne 'WALL_CLOCK_V1') {
    throw 'FAIL: explicit native API mode was not selected.'
}
$observed = & $entry @entryArguments -UiDriver OBSERVED_CLICK_V2 -PauseForOperator
if ($observed.NativeApi -or $observed.Policy -cne 'PAUSE_FOR_OPERATOR_V1') { throw 'FAIL: observed UI policy changed.' }
foreach ($conflict in @(@{UiDriver='NATIVE_V1'}, @{UiDriver='OBSERVED_CLICK_V2'}, @{PauseForOperator=$true}, @{PauseForOperator=$false})) {
    $rejected = $false
    try { $null = & $entry @entryArguments -NativeApi @conflict } catch {
        if ($_.Exception.Message -cne 'NativeApi cannot be combined with UiDriver or PauseForOperator.') { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'FAIL: native/UI conflict reached repository or machine setup.' }
}
$withoutConsent = $entryArguments.Clone(); $withoutConsent.ConfirmTemporaryAutostartPause = $false
$rejected = $false
try { $null = & $entry @withoutConsent -NativeApi } catch {
    if ($_.Exception.Message -cne 'Explicit temporary-autostart authorization required.') { throw }
    $rejected = $true
}
if (-not $rejected) { throw 'FAIL: native API bypassed temporary-autostart consent.' }

# Replay the actual forwarding statements and invocation with a runner double.
# Only the runner path is redirected; no host, registry, preference or file write
# operation from the wrapper's surrounding try/finally is evaluated.
$framework = Get-WrapperAssignment 'framework'
$invocations = @($ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.CommandAst] -and
    $node.InvocationOperator -eq [Management.Automation.Language.TokenKind]::Ampersand -and
    $node.Extent.Text.Contains('test-bricscad-v$HostMajor-single-footing.ps1')
}, $true))
if ($invocations.Count -ne 1) { throw 'FAIL: wrapper native runner invocation is ambiguous.' }
$forward = [scriptblock]::Create(($framework.Parent.Statements | Where-Object {
    $_.Extent.StartOffset -ge $framework.Extent.StartOffset -and $_.Extent.EndOffset -le $invocations[0].Extent.EndOffset
} | ForEach-Object { $_.Extent.Text }) -join "`n")
foreach ($HostMajor in @(25,26)) {
    foreach ($mode in @($default, $native, $observed)) {
        & {
            $NativeApi = $mode.NativeApi; $UiDriver = $mode.UiDriver; $PauseForOperator = $mode.Pause; $RenderExperiment = $false; $QuantityUi = $false
            $PackageRoot = 'C:\host-free-package'; $taskRepo = 'C:\host-free-harness'; $runRoot = 'C:\host-free-allocation'
            $source = & ([scriptblock]::Create((Get-WrapperAssignment 'source').Right.Extent.Text))
            $v25PackageSha256 = & ([scriptblock]::Create((Get-WrapperAssignment 'v25PackageSha256').Right.Extent.Text))
            $selectedProfile = 'Default'; $V26ProvenancePath = 'C:\host-free-provenance.json'
            function Join-Path { param($Path, $ChildPath)
                if ($ChildPath -ceq "test-bricscad-v$HostMajor-single-footing.ps1") { return 'Invoke-HostFreeNativeRunner' }
                Microsoft.PowerShell.Management\Join-Path $Path $ChildPath
            }
            function Invoke-HostFreeNativeRunner {
                param($ProductDir, $PackageZip, $PackageSha256, $ProductSourceSha, $ProbeDll, $ArtifactDir,
                    $PhaseTimeoutSeconds, $ConfirmDisposableCopy, $InteractiveUi, $UiDriver, $PauseForOperator, $Profile, $ProvenancePath)
                if ($InteractiveUi -isnot [bool] -or $InteractiveUi -eq $mode.NativeApi -or
                    $UiDriver -cne $mode.UiDriver -or $PauseForOperator -ne $mode.Pause -or -not $ConfirmDisposableCopy) {
                    throw 'FAIL: wrapper changed runner mode or disposable authorization.'
                }
                $expectedHash = if ($HostMajor -eq 25) { '6da38fcb3bc5fdb1989e9397fad45da712bf4af6b7690298a2fe83657bcb10ac' }
                    else { '59498948341f36d408f8bf838e177170c19d99ffc64acaa2070b0524e6b99a81' }
                $expectedTimeout = if ($mode.UiDriver -ceq 'OBSERVED_CLICK_V2') { 3600 } else { 600 }
                $expectedFramework = if ($HostMajor -eq 25) { 'net48' } else { 'net8.0-windows' }
                if ($PackageSha256 -cne $expectedHash -or $ProductSourceSha -cne '87aff7fec452f9a8dd9f641ef84d143edc73514d' -or
                    $ProductDir -cne "C:\host-free-package\QS3D-BricsCAD-V$HostMajor" -or
                    $PackageZip -cne "C:\host-free-package\QS3D-BricsCAD-V$HostMajor.zip" -or
                    $ProbeDll -cne "C:\host-free-harness\tests\QS3D.LocalQualification.V$HostMajor\bin\Release\$expectedFramework\QS3D.LocalQualification.V$HostMajor.dll" -or
                    $ArtifactDir -cne $runRoot -or $PhaseTimeoutSeconds -ne $expectedTimeout -or $Profile -cne 'Default' -or
                    ($HostMajor -eq 26 -and $ProvenancePath -cne $V26ProvenancePath) -or
                    ($HostMajor -eq 25 -and $PSBoundParameters.ContainsKey('ProvenancePath'))) {
                    throw 'FAIL: wrapper changed frozen source/package/probe, timing, profile or V26 provenance.'
                }
                return 'HOST_FREE_RUNNER_INVOKED'
            }
            if ((& $forward) -cne 'HOST_FREE_RUNNER_INVOKED') { throw 'FAIL: wrapper did not invoke the existing native runner.' }
        }
    }
}

$helper = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-Local022NativeV25Predecessor'
}, $true)
if ($null -eq $helper) { throw 'FAIL: native V25 predecessor assertion missing.' }
. ([scriptblock]::Create($helper.Extent.Text))
$v26Gate = @($ast.EndBlock.Statements | Where-Object {
    $_ -is [Management.Automation.Language.IfStatementAst] -and $_.Extent.Text.StartsWith('if ($HostMajor -eq 26)')
})
if ($v26Gate.Count -ne 1) { throw 'FAIL: V26 predecessor admission is ambiguous.' }
$gate = [scriptblock]::Create($v26Gate[0].Extent.Text.Replace('$PSScriptRoot', "'C:\host-free-harness\scripts'"))
function New-NativePredecessor {
    [pscustomobject]@{
        Receipt = [pscustomobject]@{
            schema='QS3D_LOCAL022_RECEIPT_V1'; run_id=('a' * 32); status='LOCAL_PASS_BOUNDED'
            product_source_sha='87aff7fec452f9a8dd9f641ef84d143edc73514d'; phases_verified=3
            interactive_ui_executed=$false; ui_driver='NATIVE_V1'; operator_wait_policy='WALL_CLOCK_V1'
            private_cleanup_verified=$true; protected_state_unchanged=$true
            profile_cleanup=[pscustomobject]@{
                zero_bricscad_processes=$true; cur_profile_restored=$true; profile_inventory_restored=$true; nonce_profile_removed=$true
                profile_inventory_before_sha256=('b' * 64); profile_inventory_after_sha256=('b' * 64)
            }
        }
        Allocation = [pscustomobject]@{
            schema='QS3D_LOCAL022_ALLOCATION_V1'; run_id=('a' * 32); host_version='25.2.10'
            product_source_sha='87aff7fec452f9a8dd9f641ef84d143edc73514d'
            package_sha256='6da38fcb3bc5fdb1989e9397fad45da712bf4af6b7690298a2fe83657bcb10ac'
            interactive_ui=$false; ui_driver='NATIVE_V1'; operator_wait_policy='WALL_CLOCK_V1'
        }
        Restoration = [pscustomobject]@{ restored=$true }
    }
}
function Invoke-PredecessorGate($Fixture, [bool]$Native=$true) {
    $HostMajor=26; $NativeApi=$Native; $UiDriver='NATIVE_V1'; $operatorWaitPolicy='WALL_CLOCK_V1'; $QuantityUi=$false
    $PrecedingV25Receipt='C:\host-free-receipts\native-v25\receipt.json'; $V26ProvenancePath='C:\host-free-provenance.json'
    $source='87aff7fec452f9a8dd9f641ef84d143edc73514d'
    $v25PackageSha256='6da38fcb3bc5fdb1989e9397fad45da712bf4af6b7690298a2fe83657bcb10ac'
    $script:phaseAdmissionCalled=$false
    function Assert-Local022NativeV25Phases($RunnerPath,$EvidenceRoot,$ExpectedRunId) {
        if ((Split-Path $RunnerPath -Leaf) -cne 'test-bricscad-v25-single-footing.ps1' -or
            $EvidenceRoot -cne 'C:\host-free-receipts\native-v25' -or $ExpectedRunId -cne $Fixture.Receipt.run_id) {
            throw 'FAIL: native phase admission received another validator/root/run.'
        }
        $script:phaseAdmissionCalled=$true
    }
    function Get-Content { param($Path, $LiteralPath, [switch]$Raw)
        $requested = if ($LiteralPath) { $LiteralPath } else { $Path }
        switch -Exact ($requested) {
            'C:\host-free-receipts\native-v25\receipt.json' { return ($Fixture.Receipt | ConvertTo-Json -Depth 8) }
            'C:\host-free-receipts\native-v25\allocation.json' { return ($Fixture.Allocation | ConvertTo-Json -Depth 8) }
            'C:\host-free-receipts\native-v25-autostart-recovery\tunnel-restoration.json' { return ($Fixture.Restoration | ConvertTo-Json -Depth 8) }
            default { throw 'FAIL: predecessor gate accessed an unexpected path.' }
        }
    }
    & $gate
    if ($Native -and -not $script:phaseAdmissionCalled) { throw 'FAIL: native phase admission was bypassed.' }
}
Invoke-PredecessorGate (New-NativePredecessor)
$invalid = [ordered]@{
    'UI receipt' = { param($f) $f.Receipt.interactive_ui_executed=$true }
    'UI allocation' = { param($f) $f.Allocation.interactive_ui=$true }
    'legacy receipt without mode' = { param($f) $f.Receipt.PSObject.Properties.Remove('interactive_ui_executed') }
    'legacy allocation without mode' = { param($f) $f.Allocation.PSObject.Properties.Remove('interactive_ui') }
    'string false mode' = { param($f) $f.Receipt.interactive_ui_executed='false' }
    'null allocation mode' = { param($f) $f.Allocation.interactive_ui=$null }
    'stale 43130 source' = { param($f) $f.Receipt.product_source_sha='43130a49f49676299b865f094a9a6ded482f67ad' }
    'stale allocation source' = { param($f) $f.Allocation.product_source_sha='43130a49f49676299b865f094a9a6ded482f67ad' }
    'different package' = { param($f) $f.Allocation.package_sha256=('c' * 64) }
    'V26 receipt schema' = { param($f) $f.Receipt.schema='QS3D_LOCAL022_V26_RECEIPT_V1' }
    'wrong allocation schema' = { param($f) $f.Allocation.schema='QS3D_LOCAL022_V26_ALLOCATION_V1' }
    'V26 host' = { param($f) $f.Allocation.host_version='26.2.07' }
    'malformed V25 host' = { param($f) $f.Allocation.host_version="25.2.10`n" }
    'different run' = { param($f) $f.Allocation.run_id=('d' * 32) }
    'missing run' = { param($f) $f.Receipt.run_id=''; $f.Allocation.run_id='' }
    'failed receipt' = { param($f) $f.Receipt.status='FAIL_OR_NO_RESULT' }
    'partial phases' = { param($f) $f.Receipt.phases_verified=2 }
    'string phase count' = { param($f) $f.Receipt.phases_verified='3' }
    'observed receipt driver' = { param($f) $f.Receipt.ui_driver='OBSERVED_CLICK_V2' }
    'observed allocation driver' = { param($f) $f.Allocation.ui_driver='OBSERVED_CLICK_V2' }
    'paused receipt policy' = { param($f) $f.Receipt.operator_wait_policy='PAUSE_FOR_OPERATOR_V1' }
    'paused allocation policy' = { param($f) $f.Allocation.operator_wait_policy='PAUSE_FOR_OPERATOR_V1' }
    'private cleanup failure' = { param($f) $f.Receipt.private_cleanup_verified=$false }
    'protected state changed' = { param($f) $f.Receipt.protected_state_unchanged=$false }
    'host remains' = { param($f) $f.Receipt.profile_cleanup.zero_bricscad_processes=$false }
    'current profile not restored' = { param($f) $f.Receipt.profile_cleanup.cur_profile_restored=$false }
    'inventory not restored' = { param($f) $f.Receipt.profile_cleanup.profile_inventory_restored=$false }
    'nonce profile remains' = { param($f) $f.Receipt.profile_cleanup.nonce_profile_removed=$false }
    'different inventory hash' = { param($f) $f.Receipt.profile_cleanup.profile_inventory_after_sha256=('e' * 64) }
    'empty inventory hashes' = { param($f) $f.Receipt.profile_cleanup.profile_inventory_before_sha256=''; $f.Receipt.profile_cleanup.profile_inventory_after_sha256='' }
    'autostart not restored' = { param($f) $f.Restoration.restored=$false }
    'string cleanup true' = { param($f) $f.Restoration.restored='true' }
}
foreach ($case in $invalid.GetEnumerator()) {
    $fixture = New-NativePredecessor
    & $case.Value $fixture
    $rejected=$false
    try { Invoke-PredecessorGate $fixture } catch { $rejected=$true }
    if (-not $rejected) { throw ('FAIL: actual V26 native gate accepted ' + $case.Key) }
}
$ui = New-NativePredecessor; $ui.Receipt.interactive_ui_executed=$true; $ui.Allocation.interactive_ui=$true
Invoke-PredecessorGate $ui $false
$rejected=$false
try { Invoke-PredecessorGate (New-NativePredecessor) $false } catch { $rejected=$true }
if (-not $rejected) { throw 'FAIL: default UI predecessor gate accepted native API proof.' }

# Run the actual extracted phase validator against in-memory markers. No CAD,
# registry, real evidence or preferences are modified by this negative coverage.
$phaseHelper=$ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-Local022NativeV25Phases'
},$true)
. ([scriptblock]::Create($phaseHelper.Extent.Text))
$runnerPath=Join-Path $PSScriptRoot '../../scripts/test-bricscad-v25-single-footing.ps1'
$runnerAst=[Management.Automation.Language.Parser]::ParseFile($runnerPath,[ref]$null,[ref]$null)
$coverage=$runnerAst.Find({ param($node)
    $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -ceq '$requiredByPhase'
},$true)
$requiredByPhase=& ([scriptblock]::Create($coverage.Right.Extent.Text))
function Test-ActualPhases([string]$Mutation) {
    function Get-Content { param($LiteralPath,[switch]$Raw)
        $phase=[IO.Path]::GetFileNameWithoutExtension($LiteralPath).Substring(6)
        $checks=[ordered]@{}
        foreach($key in $requiredByPhase[$phase]) { $checks[$key]=$true }
        $marker=[ordered]@{schema='QS3D_LOCAL022_NATIVE_V3';run_id=('a'*32);phase=$phase;status='PASS';stage=$phase;error_code='NONE';checks=$checks}
        if($phase -ceq 'run') {
            switch($Mutation) {
                'missing' { throw 'test_missing_marker' }
                'false' { $checks.generic_foundation_rejected_before_mutation=$false }
                'string' { $checks.generic_foundation_rejected_before_mutation='true' }
                'coverage' { $checks.Remove('generic_foundation_rejected_before_mutation') }
                'runid' { $marker.run_id=('b'*32) }
                'v1' { $marker.schema='QS3D_LOCAL022_NATIVE_V1' }
                'section' { $checks.Remove('native_rectangular_sections') }
                'failed' { $marker.status='FAIL' }
            }
        }
        $marker | ConvertTo-Json -Depth 6 -Compress
    }
    Assert-Local022NativeV25Phases $runnerPath 'C:\host-free-receipts\native-v25' ('a'*32)
}
Test-ActualPhases ''
foreach($mutation in @('missing','false','string','coverage','runid','failed','v1','section')) {
    $rejected=$false
    try { Test-ActualPhases $mutation } catch { $rejected=$true }
    if(-not $rejected) { throw "FAIL: native phase validator accepted $mutation" }
}
Write-Output 'PASS: actual wrapper native mode/forwarding and V26 predecessor admission reject UI, stale source/package, incomplete phases and cleanup; defaults preserved, no host/registry/preferences changed.'
