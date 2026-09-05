param(
    [Parameter(Mandatory=$true)][ValidateSet(25,26)][int]$HostMajor,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$HarnessSha,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]{4,50}$')][string]$AllocationName,
    [Parameter(Mandatory=$true)][string]$PackageRoot,
    [string]$V26ProvenancePath,
    [string]$PrecedingV25Receipt,
    [Parameter(Mandatory=$true)][switch]$ConfirmTemporaryAutostartPause
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $ConfirmTemporaryAutostartPause) { throw 'Explicit temporary-autostart authorization required.' }
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = '0db6e659510809a6781221204a32409605c851ba'
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
        throw 'V26 requires its frozen provenance and preceding cleaned V25 UI result.'
    }
    $v25 = Get-Content -LiteralPath $PrecedingV25Receipt -Raw | ConvertFrom-Json
    $v25Root = Split-Path ([IO.Path]::GetFullPath($PrecedingV25Receipt))
    $allocation = Get-Content (Join-Path $v25Root 'allocation.json') -Raw | ConvertFrom-Json
    $restore = Get-Content ($v25Root + '-autostart-recovery\tunnel-restoration.json') -Raw | ConvertFrom-Json
    if ($v25.status -cne 'LOCAL_PASS_BOUNDED' -or $v25.product_source_sha -cne $source -or
        -not $v25.interactive_ui_executed -or -not $v25.private_cleanup_verified -or
        -not $v25.protected_state_unchanged -or -not $v25.profile_cleanup.cur_profile_restored -or
        -not $v25.profile_cleanup.profile_inventory_restored -or -not $restore.restored -or
        $allocation.host_version -notmatch '^25\.') { throw 'Preceding V25 UI result/cleanup is not qualified.' }
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
        PackageSha256 = if ($HostMajor -eq 25) { '6b6d00de4d391e772b58780be96afab9e4b31c0d8e0246dee3d7b79a8c1c5f70' } else { '4259a2c9850e2e18dd82a8496a8b70c4c68d80abe290c51fb660e1e9d12e946d' }
        ProductSourceSha = $source
        ProbeDll = Join-Path $taskRepo "tests\QS3D.LocalQualification.V$HostMajor\bin\Release\$framework\QS3D.LocalQualification.V$HostMajor.dll"
        ArtifactDir = $runRoot
        PhaseTimeoutSeconds = 600
        ConfirmDisposableCopy = $true
        InteractiveUi = $true
    }
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
