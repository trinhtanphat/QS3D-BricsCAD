[CmdletBinding()]
param(
    [ValidateSet('Plan', 'Run')]
    [string]$Action = 'Plan',
    [ValidatePattern('^(LOCAL-[0-9]{3}|#3681)$')]
    [string]$Lane = '',
    [string]$BricsCadDir = '',
    [string]$Profile = '',
    [string]$ArtifactDir = '',
    [string]$PythonPath = '',
    [string]$PluginDll = '',
    [string]$DrawingCopy = '',
    [string]$FixtureDwg = '',
    [string]$ReferenceCopy = '',
    [string]$VersionKey = '',
    [string]$LanguageKey = '',
    [switch]$ConfirmDisposableCopy,
    [switch]$ConfirmDisposableCopies,
    [switch]$ConfirmReferenceCopy,
    [switch]$ConfirmDisposableInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$inboxPath = Join-Path $repoRoot 'docs\LOCAL-AGENT-INBOX.md'
if (-not (Test-Path -LiteralPath $inboxPath -PathType Leaf)) {
    throw "Canonical local queue is missing: $inboxPath"
}

function Require-Value {
    param([string]$Value, [string]$Name)
    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Name is required for this lane." }
}

function Get-ExactSourceIdentity {
    $sha = (& git -C $repoRoot rev-parse HEAD).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $sha -notmatch '^[0-9a-f]{40}$') {
        throw 'Could not resolve an exact Git HEAD SHA.'
    }
    $dirty = @(& git -C $repoRoot status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw 'git status failed.' }
    if ($dirty.Count -ne 0) {
        throw 'Local qualification requires a completely clean exact-SHA working tree.'
    }
    return $sha
}

function Get-CanonicalLocalQueue {
    $text = Get-Content -LiteralPath $inboxPath -Raw -Encoding UTF8
    $pattern = '(?ms)^## (?<heading>(?:LOCAL-[0-9]{3}|P0 — #3681)[^\r\n]*)\r?\n(?<body>.*?)(?=^## |\z)'
    $matches = [regex]::Matches($text, $pattern)
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($match in $matches) {
        $heading = [string]$match.Groups['heading'].Value
        $body = [string]$match.Groups['body'].Value
        $id = if ($heading.StartsWith('LOCAL-', [StringComparison]::Ordinal)) {
            ([regex]::Match($heading, '^LOCAL-[0-9]{3}')).Value
        }
        else { '#3681' }
        $priorityMatch = [regex]::Match($body, '(?m)^- Priority: (?<value>P[0-2])\s*$')
        $statusMatch = [regex]::Match($body, '(?m)^- Status: (?<value>OPEN|IN_PROGRESS|PASS|BLOCKED)\s*$')
        if (-not $priorityMatch.Success -or -not $statusMatch.Success) {
            throw "Canonical inbox item '$id' is missing a valid Priority or Status field."
        }
        $rows.Add([pscustomobject]@{
            id = $id
            priority = [string]$priorityMatch.Groups['value'].Value
            status = [string]$statusMatch.Groups['value'].Value
            heading = $heading
            noRerun = ($body -match '(?i)NO_RERUN')
            body = $body
        })
    }
    if ($rows.Count -eq 0) { throw 'Canonical local queue contains no parseable items.' }
    return $rows.ToArray()
}

# This registry is execution metadata only. Priority/status always come from
# docs/LOCAL-AGENT-INBOX.md at run time; do not turn this into a second queue.
$contracts = [ordered]@{
    '#3681' = [ordered]@{ host = 'V25'; mode = 'COMPLETED_NO_RERUN'; runners = @('run-local-v25-wall-contact-3681.ps1'); note = 'Completed licensed qualification; regression reference only.' }
    'LOCAL-001' = [ordered]@{ host = 'V25'; mode = 'AUTOMATED_BASELINE'; runners = @('run-local-v25-qualification.ps1'); note = 'Runs exact-SHA source/build/offline-WPF/licensed NETLOAD baseline. Broader interactive/private-DWG rows remain governed by the inbox.' }
    'LOCAL-002' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Curtain P01-P12, Family editor and H.1 P07 are already bounded evidence; do not rerun superseded H.1 P01-P06 blockers. Execute only a still-explicit broader current inbox row.' }
    'LOCAL-003' = [ordered]@{ host = 'V25'; mode = 'AUTOMATED_REGRESSION'; runners = @('run-local-v25-qualification.ps1','test-bricscad-v25-level-z.ps1','test-bricscad-v25-level-z-lifecycle.ps1'); note = 'Fresh exact-SHA Millimeter + Meter representative Level Z and lifecycle regression. Complete-family/private-DWG breadth remains local.' }
    'LOCAL-004' = [ordered]@{ host = 'V25'; mode = 'AUTOMATED_REGRESSION'; runners = @('run-local-v25-qualification.ps1','test-bricscad-v25-source-reconcile.ps1'); note = 'Exact-SHA production Source Reconcile base matrix. Broader topology/category/manual-grip breadth remains local.' }
    'LOCAL-005' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Polygon reinforcement native topology requires the exact local runbook/native matrix.' }
    'LOCAL-006' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Source is complete; native documentation/UI/Unicode/HiDPI matrix is local-only.' }
    'LOCAL-007' = [ordered]@{ host = 'V25'; mode = 'COMPLETED_BOUNDED'; runners = @(); note = 'P01/P02/P03 physical wall-junction slice is merged and licensed-qualified; parent #73 remains open only for broader advanced geometry.' }
    'LOCAL-008' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Remaining quick/ADV prompt drift, Auto Host/reference and UI matrix require interactive editor input.' }
    'LOCAL-009' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Signing/trust/clean-machine install requires approved local certificate and workstation.' }
    'LOCAL-010' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Performance/HiDPI matrix requires representative local hardware.' }
    'LOCAL-011' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Fault-injection/modeless generated-replacement matrix requires licensed native runtime.' }
    'LOCAL-012' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Workspace/Project Browser modeless selection and DPI matrix requires interactive V25.' }
    'LOCAL-013' = [ordered]@{ host = 'V25'; mode = 'AUTOMATED_WITH_AUTHORIZED_INPUT'; runners = @('run-local-v25-qualification.ps1','test-bricscad-v25-brc-probe.ps1','test-bricscad-v25-brc-quantity-roundtrip.ps1'); note = 'Requires an explicitly authorized disposable BRC/reference copy; never reconstruct a missing private reference.' }
    'LOCAL-014' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Bounded Plan-to-3D probes already exist; remaining prompt drift/cancel/rollback breadth is interactive.' }
    'LOCAL-015' = [ordered]@{ host = 'V25'; mode = 'MANUAL_OR_EXTERNAL'; runners = @(); note = 'Default-browser/modeless lifecycle is local desktop behavior.' }
    'LOCAL-016' = [ordered]@{ host = 'V26'; mode = 'AUTOMATED_SOURCE_READY'; runners = @('test-v26-package-install-lifecycle.ps1'); note = 'Post-#3878 exact-SHA V26 package install/uninstall lifecycle only; no signing/customer-release inference.' }
    'LOCAL-017' = [ordered]@{ host = 'V26'; mode = 'COMPLETED_BOUNDED'; runners = @(); note = 'Bounded V26 Slab POLYLINE row already PASS.' }
    'LOCAL-018' = [ordered]@{ host = 'V26'; mode = 'COMPLETED_BOUNDED'; runners = @(); note = 'Bounded V26 LINE/repeated Direct Draw lifecycle already PASS.' }
    'LOCAL-019' = [ordered]@{ host = 'V25+V26'; mode = 'COMPLETED_BOUNDED'; runners = @(); note = 'Six-sheet Review export/Locate row already PASS.' }
}

foreach ($entry in $contracts.GetEnumerator()) {
    foreach ($runner in @($entry.Value.runners)) {
        $runnerPath = Join-Path $PSScriptRoot $runner
        if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
            throw "Execution contract '$($entry.Key)' references a missing runner: $runner"
        }
    }
}

$queue = @(Get-CanonicalLocalQueue)
$plan = foreach ($row in $queue) {
    $contract = if ($contracts.Contains($row.id)) { $contracts[$row.id] } else { $null }
    [pscustomobject]@{
        lane = $row.id
        priority = $row.priority
        status = $row.status
        noRerun = [bool]$row.noRerun
        host = if ($null -eq $contract) { 'UNMAPPED' } else { $contract.host }
        mode = if ($null -eq $contract) { 'UNMAPPED_FAIL_CLOSED' } else { $contract.mode }
        runners = if ($null -eq $contract) { @() } else { @($contract.runners) }
        note = if ($null -eq $contract) { 'No source-ready execution contract exists; do not improvise local scope.' } else { $contract.note }
    }
}

if ($Action -eq 'Plan') {
    $selected = if ([string]::IsNullOrWhiteSpace($Lane)) { @($plan) } else { @($plan | Where-Object lane -eq $Lane) }
    if ($selected.Count -eq 0) { throw "Lane '$Lane' is not present in the canonical inbox." }
    $selected | Sort-Object priority, lane | Format-Table lane, priority, status, host, mode, noRerun -AutoSize
    Write-Host ''
    Write-Host 'Status/priority above came from docs/LOCAL-AGENT-INBOX.md at runtime.'
    Write-Host 'PASS/NO_RERUN rows are never executable through this entrypoint.'
    return
}

Require-Value $Lane '-Lane'
$row = $queue | Where-Object id -eq $Lane | Select-Object -First 1
if ($null -eq $row) { throw "Lane '$Lane' is not present in the canonical inbox." }
if ($row.status -eq 'PASS' -or $row.noRerun) {
    throw "Lane '$Lane' is completed or NO_RERUN in the canonical inbox; refusing duplicate local execution."
}
if (-not $contracts.Contains($Lane)) {
    throw "Lane '$Lane' has no source-ready execution contract. Do not improvise a local runner."
}
$contract = $contracts[$Lane]
if ($contract.mode -like 'MANUAL_OR_EXTERNAL*' -or $contract.mode -like 'COMPLETED_*') {
    throw "Lane '$Lane' is not safely automatable by this entrypoint. Follow its current inbox/runbook boundary only. $($contract.note)"
}

$exactSha = Get-ExactSourceIdentity
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot ("artifacts\local-source-ready\{0}\{1}" -f $exactSha, $Lane.Replace('#','issue-'))
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

function Invoke-V25Baseline {
    Require-Value $BricsCadDir '-BricsCadDir'
    $args = @{
        BricsCadDir = $BricsCadDir
        Profile = $Profile
        ArtifactDir = (Join-Path $ArtifactDir 'baseline')
        SkipScreenshot = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($PythonPath)) { $args.PythonPath = $PythonPath }
    & (Join-Path $PSScriptRoot 'run-local-v25-qualification.ps1') @args
}

function Resolve-V25Plugin {
    if (-not [string]::IsNullOrWhiteSpace($PluginDll)) {
        $resolved = [IO.Path]::GetFullPath($PluginDll)
    }
    else {
        $resolved = Join-Path $repoRoot 'src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll'
    }
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Exact V25 plugin output is missing: $resolved"
    }
    return $resolved
}

switch ($Lane) {
    'LOCAL-001' {
        Invoke-V25Baseline
    }
    'LOCAL-003' {
        Require-Value $Profile '-Profile'
        Require-Value $DrawingCopy '-DrawingCopy'
        if (-not $ConfirmDisposableCopy) { throw 'LOCAL-003 requires -ConfirmDisposableCopy.' }
        Invoke-V25Baseline
        $dll = Resolve-V25Plugin
        foreach ($unit in @('Millimeter','Meter')) {
            & (Join-Path $PSScriptRoot 'test-bricscad-v25-level-z.ps1') `
                -BricsCadDir $BricsCadDir -PluginDll $dll -DrawingCopy $DrawingCopy -Profile $Profile `
                -ArtifactDir (Join-Path $ArtifactDir ("level-z-{0}" -f $unit.ToLowerInvariant())) `
                -ExpectedSourceSha $exactSha -ConfirmDisposableCopy -NativeDrawingUnit $unit
        }
        & (Join-Path $PSScriptRoot 'test-bricscad-v25-level-z-lifecycle.ps1') `
            -BricsCadDir $BricsCadDir -PluginDll $dll -DrawingCopy $DrawingCopy -Profile $Profile `
            -ArtifactDir (Join-Path $ArtifactDir 'level-z-lifecycle') -ExpectedSourceSha $exactSha -ConfirmDisposableCopy
    }
    'LOCAL-004' {
        Require-Value $Profile '-Profile'
        Require-Value $FixtureDwg '-FixtureDwg'
        if (-not $ConfirmDisposableCopies) { throw 'LOCAL-004 requires -ConfirmDisposableCopies.' }
        Invoke-V25Baseline
        $dll = Resolve-V25Plugin
        & (Join-Path $PSScriptRoot 'test-bricscad-v25-source-reconcile.ps1') `
            -BricsCadDir $BricsCadDir -PluginDll $dll -FixtureDwg $FixtureDwg -Profile $Profile `
            -ArtifactDir (Join-Path $ArtifactDir 'source-reconcile') -ConfirmDisposableCopies
    }
    'LOCAL-013' {
        Require-Value $Profile '-Profile'
        Require-Value $ReferenceCopy '-ReferenceCopy'
        if (-not $ConfirmReferenceCopy) { throw 'LOCAL-013 requires -ConfirmReferenceCopy for an explicitly authorized disposable reference copy.' }
        Invoke-V25Baseline
        $dll = Resolve-V25Plugin
        & (Join-Path $PSScriptRoot 'test-bricscad-v25-brc-probe.ps1') `
            -BricsCadDir $BricsCadDir -PluginDll $dll -DrawingCopy $ReferenceCopy -Profile $Profile `
            -ArtifactDir (Join-Path $ArtifactDir 'brc-public-probe') -ConfirmReferenceCopy
        & (Join-Path $PSScriptRoot 'test-bricscad-v25-brc-quantity-roundtrip.ps1') `
            -BricsCadDir $BricsCadDir -PluginDll $dll -DrawingCopy $ReferenceCopy -Profile $Profile `
            -ArtifactDir (Join-Path $ArtifactDir 'brc-roundtrip') -ConfirmReferenceCopy
    }
    'LOCAL-016' {
        Require-Value $BricsCadDir '-BricsCadDir'
        Require-Value $VersionKey '-VersionKey'
        Require-Value $LanguageKey '-LanguageKey'
        if (-not $ConfirmDisposableInstall) { throw 'LOCAL-016 requires -ConfirmDisposableInstall.' }
        & (Join-Path $PSScriptRoot 'test-v26-package-install-lifecycle.ps1') `
            -BricsCadDir $BricsCadDir -VersionKey $VersionKey -LanguageKey $LanguageKey `
            -ExpectedSourceSha $exactSha -ArtifactDir (Join-Path $ArtifactDir 'v26-package-install') -ConfirmDisposableInstall
    }
    default {
        throw "Lane '$Lane' is mapped but has no safe automated dispatcher. $($contract.note)"
    }
}

$summary = [ordered]@{
    schema = 1
    lane = $Lane
    exactSha = $exactSha
    executionMode = $contract.mode
    boundedRunnerStatus = 'PASS'
    fullLocalPass = $false
    customerReleaseQualified = $false
    note = $contract.note
}
$summaryPath = Join-Path $ArtifactDir 'source-ready-run-summary.json'
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-Host ''
Write-Host "BOUNDED SOURCE-READY RUNNERS PASS for $Lane on exact SHA $exactSha"
Write-Host "Summary: $summaryPath"
Write-Host 'This is not broad LOCAL_PASS and not customer-release qualification; close only the exact rows proven by the current inbox/runbook evidence.'
