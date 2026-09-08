param(
    [Parameter(Mandatory=$true)][ValidateSet(25,26)][int]$HostMajor,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$HarnessSha,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]{4,50}$')][string]$AllocationName,
    [Parameter(Mandatory=$true)][string]$PackageRoot,
    [string]$V26ProvenancePath,
    [string]$PrecedingV25Receipt,
    [string]$SourceProfile,
    [switch]$NativeApi,
    [switch]$RenderExperiment,
    [ValidateSet('NATIVE_V1','OBSERVED_CLICK_V2')][string]$UiDriver = 'NATIVE_V1',
    [switch]$PauseForOperator,
    [Parameter(Mandatory=$true)][switch]$ConfirmTemporaryAutostartPause
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Get-Local022SourceProfile([string]$Name, [bool]$Specified) {
    if (-not $Specified) { return $null }
    if ([string]::IsNullOrWhiteSpace($Name) -or $Name -cne $Name.Trim() -or
        $Name.Length -gt 128 -or $Name -match '[\\/"\x00-\x1f\x7f]') {
        throw 'Source profile must be a canonical nonblank profile name without paths, quotes or controls.'
    }
    return $Name
}
$selectedProfile = Get-Local022SourceProfile $SourceProfile $PSBoundParameters.ContainsKey('SourceProfile')
if (-not $ConfirmTemporaryAutostartPause) { throw 'Explicit temporary-autostart authorization required.' }
if ($NativeApi -and ($PSBoundParameters.ContainsKey('UiDriver') -or $PSBoundParameters.ContainsKey('PauseForOperator'))) {
    throw 'NativeApi cannot be combined with UiDriver or PauseForOperator.'
}
if ($PauseForOperator -and $UiDriver -cne 'OBSERVED_CLICK_V2') { throw 'Operator pause requires OBSERVED_CLICK_V2.' }
if ($RenderExperiment -and ($NativeApi -or $UiDriver -cne 'OBSERVED_CLICK_V2')) {
    throw 'RenderExperiment requires observed UI and cannot qualify native or UI acceptance.'
}
$operatorWaitPolicy = if ($PauseForOperator) { 'PAUSE_FOR_OPERATOR_V1' } else { 'WALL_CLOCK_V1' }
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = '87aff7fec452f9a8dd9f641ef84d143edc73514d'
$v25PackageSha256 = '6da38fcb3bc5fdb1989e9397fad45da712bf4af6b7690298a2fe83657bcb10ac'
$base = Join-Path $taskRepo 'artifacts\issue-5718-local022'
$runRoot = Join-Path $base $AllocationName
$restoreRoot = Join-Path $base ($AllocationName + '-autostart-recovery')
$appData = [Environment]::GetFolderPath('ApplicationData')
$openAiFlag = Join-Path $appData 'QS3D\MCP\OpenAiSecureTunnel\autostart.txt'
$cloudflareFlag = Join-Path $appData 'QS3D\MCP\CloudflareAccount\autostart.txt'
function Get-Local022Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Assert-NoLocal022Hosts {
    if (@(Get-Process bricscad -ErrorAction SilentlyContinue).Count) { throw 'Existing host; no mutation.' }
    if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(cloudflared|tunnel-client)' }).Count) {
        throw 'Existing tunnel; no mutation.'
    }
}
function Assert-Local022NativeV25Predecessor($Receipt, $Allocation, $Restoration, [string]$Source, [string]$PackageHash) {
    # Current runners record explicit mode fields in both artifacts. Missing
    # fields on historical receipts must never be interpreted as native proof.
    if ($Receipt.schema -cne 'QS3D_LOCAL022_RECEIPT_V1' -or $Allocation.schema -cne 'QS3D_LOCAL022_ALLOCATION_V1' -or
        $Receipt.run_id -isnot [string] -or $Receipt.run_id -cnotmatch '\A[a-f0-9]{32}\z' -or
        $Receipt.run_id -cne $Allocation.run_id -or
        $Receipt.product_source_sha -cne $Source -or $Allocation.product_source_sha -cne $Source -or
        $Allocation.package_sha256 -cne $PackageHash -or
        $Allocation.host_version -isnot [string] -or $Allocation.host_version -cnotmatch '\A25\.[0-9]+(?:\.[0-9]+)*\z' -or
        $Receipt.status -cne 'LOCAL_PASS_BOUNDED' -or
        ($Receipt.phases_verified -isnot [int] -and $Receipt.phases_verified -isnot [long]) -or $Receipt.phases_verified -ne 3) {
        throw 'Preceding V25 native result must qualify three phases of the same frozen source/package on V25.'
    }
    foreach ($mode in @($Receipt.interactive_ui_executed, $Allocation.interactive_ui)) {
        if ($mode -isnot [bool] -or $mode) { throw 'Preceding V25 result must explicitly qualify native API execution.' }
    }
    if ($Receipt.ui_driver -cne 'NATIVE_V1' -or $Allocation.ui_driver -cne 'NATIVE_V1' -or
        $Receipt.operator_wait_policy -cne 'WALL_CLOCK_V1' -or $Allocation.operator_wait_policy -cne 'WALL_CLOCK_V1') {
        throw 'Preceding V25 native result has a conflicting UI driver or operator wait policy.'
    }
    foreach ($clean in @($Receipt.private_cleanup_verified, $Receipt.protected_state_unchanged,
        $Receipt.profile_cleanup.zero_bricscad_processes, $Receipt.profile_cleanup.cur_profile_restored,
        $Receipt.profile_cleanup.profile_inventory_restored, $Receipt.profile_cleanup.nonce_profile_removed,
        $Restoration.restored)) {
        if ($clean -isnot [bool] -or -not $clean) { throw 'Preceding V25 native cleanup is not qualified.' }
    }
    if ($Receipt.profile_cleanup.profile_inventory_before_sha256 -isnot [string] -or
        $Receipt.profile_cleanup.profile_inventory_before_sha256 -cnotmatch '\A[a-f0-9]{64}\z' -or
        $Receipt.profile_cleanup.profile_inventory_before_sha256 -cne $Receipt.profile_cleanup.profile_inventory_after_sha256) {
        throw 'Preceding V25 native profile inventory was not restored exactly.'
    }
}
function Assert-Local022NativeV25Phases([string]$RunnerPath, [string]$EvidenceRoot, [string]$ExpectedRunId) {
    # Reuse the existing runner's exact assertion coverage, not a duplicate list
    # or the summary receipt alone. Loading this one function never starts CAD.
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($RunnerPath, [ref]$null, [ref]$errors)
    $functions = @($ast.FindAll({ param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-Phase'
    }, $true))
    if ($errors.Count -or $functions.Count -ne 1) { throw 'Native V25 phase validator is unavailable.' }
    $ArtifactDir = $EvidenceRoot
    $runId = $ExpectedRunId
    . ([scriptblock]::Create($functions[0].Extent.Text))
    foreach ($phase in @('run','saved','reopen')) { [void](Read-Phase $phase) }
}
if ((& git -C $taskRepo rev-parse HEAD).Trim() -cne $HarnessSha -or $LASTEXITCODE -ne 0) { throw 'Harness SHA differs.' }
$dirty = @(& git -C $taskRepo status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $dirty.Count) { throw 'Harness must be committed and clean.' }
$remoteHead = @(& git -C $taskRepo ls-remote origin ('refs/heads/' + (& git -C $taskRepo branch --show-current).Trim()))
if ($LASTEXITCODE -ne 0 -or $remoteHead.Count -ne 1 -or -not $remoteHead[0].StartsWith($HarnessSha + "`t")) {
    throw 'Exact harness must be pushed before licensed execution.'
}
Assert-NoLocal022Hosts
foreach ($path in @($runRoot,$restoreRoot)) { if (Test-Path -LiteralPath $path) { throw 'Consumed allocation.' } }
if ($HostMajor -eq 26) {
    if ([string]::IsNullOrWhiteSpace($PrecedingV25Receipt) -or [string]::IsNullOrWhiteSpace($V26ProvenancePath)) {
        throw 'V26 requires its frozen provenance and preceding cleaned V25 result for the selected mode.'
    }
    $v25 = Get-Content -LiteralPath $PrecedingV25Receipt -Raw | ConvertFrom-Json
    $v25Root = Split-Path ([IO.Path]::GetFullPath($PrecedingV25Receipt))
    $allocation = Get-Content (Join-Path $v25Root 'allocation.json') -Raw | ConvertFrom-Json
    $restore = Get-Content ($v25Root + '-autostart-recovery\tunnel-restoration.json') -Raw | ConvertFrom-Json
    if ($NativeApi) {
        Assert-Local022NativeV25Predecessor $v25 $allocation $restore $source $v25PackageSha256
        Assert-Local022NativeV25Phases (Join-Path $PSScriptRoot 'test-bricscad-v25-single-footing.ps1') $v25Root $v25.run_id
    } else {
        if ($v25.ui_driver -cne $UiDriver -or $allocation.ui_driver -cne $UiDriver -or
            $v25.operator_wait_policy -cne $operatorWaitPolicy -or $allocation.operator_wait_policy -cne $operatorWaitPolicy) {
            throw 'Preceding V25 result must qualify the same observed driver and operator wait policy.'
        }
        if ($v25.status -cne 'LOCAL_PASS_BOUNDED' -or $v25.product_source_sha -cne $source -or
            -not $v25.interactive_ui_executed -or -not $v25.private_cleanup_verified -or
            -not $v25.protected_state_unchanged -or -not $v25.profile_cleanup.cur_profile_restored -or
            -not $v25.profile_cleanup.profile_inventory_restored -or -not $restore.restored -or
            $allocation.host_version -notmatch '^25\.') { throw 'Preceding V25 UI result/cleanup is not qualified.' }
    }
}
$flagInfo = Get-Item -LiteralPath $openAiFlag -Force
if (($flagInfo.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $flagInfo.Length -ne 1 -or
    [IO.File]::ReadAllText($openAiFlag) -cne '1') { throw 'Unexpected OpenAI preference; no mutation.' }
if ([IO.File]::ReadAllText($cloudflareFlag).Trim() -cne '0') { throw 'Cloudflare is not paused.' }
$originalHash = Get-Local022Hash $openAiFlag
$originalWriteUtc = $flagInfo.LastWriteTimeUtc
$cloudflareHash = Get-Local022Hash $cloudflareFlag
New-Item -ItemType Directory -Path $restoreRoot | Out-Null
$backup = Join-Path $restoreRoot 'openai-autostart.original.bin'
Copy-Item -LiteralPath $openAiFlag -Destination $backup
if ((Get-Local022Hash $backup) -cne $originalHash) { throw 'Autostart backup mismatch.' }
$restoreInfo = [ordered]@{ original_sha256=$originalHash; original_last_write_utc=$originalWriteUtc.ToString('o'); user_approved_temporary_pause=$true; restored=$false }
$receiptPath = Join-Path $restoreRoot 'tunnel-restoration.json'
[IO.File]::WriteAllText($receiptPath,($restoreInfo | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
$paused = $false
$runFailure = $null
try {
    Assert-NoLocal022Hosts
    if ((Get-Local022Hash $openAiFlag) -cne $originalHash -or (Get-Item $openAiFlag).LastWriteTimeUtc -ne $originalWriteUtc) {
        throw 'Preference changed before pause.'
    }
    [IO.File]::WriteAllText($openAiFlag,'0',[Text.UTF8Encoding]::new($false))
    $paused = $true
    $pausedHash = Get-Local022Hash $openAiFlag
    $pausedWriteUtc = (Get-Item $openAiFlag).LastWriteTimeUtc
    $framework = if ($HostMajor -eq 25) { 'net48' } else { 'net8.0-windows' }
    $parameters = @{
        ProductDir = Join-Path $PackageRoot "QS3D-BricsCAD-V$HostMajor"
        PackageZip = Join-Path $PackageRoot "QS3D-BricsCAD-V$HostMajor.zip"
        PackageSha256 = if ($HostMajor -eq 25) { $v25PackageSha256 } else { '59498948341f36d408f8bf838e177170c19d99ffc64acaa2070b0524e6b99a81' }
        ProductSourceSha = $source
        ProbeDll = Join-Path $taskRepo "tests\QS3D.LocalQualification.V$HostMajor\bin\Release\$framework\QS3D.LocalQualification.V$HostMajor.dll"
        ArtifactDir = $runRoot
        PhaseTimeoutSeconds = if ($UiDriver -ceq 'OBSERVED_CLICK_V2') { 3600 } else { 600 }
        ConfirmDisposableCopy = $true
        InteractiveUi = -not [bool]$NativeApi
        UiDriver = $UiDriver
        PauseForOperator = [bool]$PauseForOperator
        RenderExperiment = [bool]$RenderExperiment
    }
    if ($null -ne $selectedProfile) { $parameters.Profile = $selectedProfile }
    if ($HostMajor -eq 26) { $parameters.ProvenancePath = $V26ProvenancePath }
    & (Join-Path $PSScriptRoot "test-bricscad-v$HostMajor-single-footing.ps1") @parameters
} catch { $runFailure = $_ }
finally {
    if ($paused) {
        Assert-NoLocal022Hosts
        if ((Get-Local022Hash $openAiFlag) -cne $pausedHash -or (Get-Item $openAiFlag).LastWriteTimeUtc -ne $pausedWriteUtc) {
            throw 'External autostart change; retained backup, no overwrite.'
        }
        Copy-Item -LiteralPath $backup -Destination $openAiFlag -Force
        (Get-Item $openAiFlag).LastWriteTimeUtc = $originalWriteUtc
        if ((Get-Local022Hash $openAiFlag) -cne $originalHash -or (Get-Local022Hash $cloudflareFlag) -cne $cloudflareHash) {
            throw 'Exact autostart restoration failed.'
        }
        $restoreInfo.restored = $true
        [IO.File]::WriteAllText($receiptPath,($restoreInfo | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
        Write-Output 'LOCAL022_ORIGINAL_AUTOSTART_RESTORED'
    }
}
if ($null -ne $runFailure) { throw $runFailure }
