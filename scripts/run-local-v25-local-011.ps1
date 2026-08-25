[CmdletBinding()]
param(
    [string]$BricsCadDir = "",
    [string]$Profile = "QS3D-V25-LOCAL011",
    [string]$ArtifactDir = ""
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) { $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-local-011" }
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

$sourceReadyShas = @(
    "761b9b92f5dd3638b18d281c273a406e41069511",
    "ffd26294f3f27d03de1050643aa0aeb894dcb0f2",
    "1850f02382c8ccf71f04e3ea9daa28455aaae08f",
    "b22eacd681230f231e0f970fb670e8f89769c35e"
)
$caseIds = @(
    "native.before_commit_abort", "native.during_commit_abort", "native.after_commit_ui_failure",
    "native.document_lock_multi_dwg", "recognition.stale_apply_no_project", "modeless.door_detached",
    "modeless.room_detached", "modeless.bbs_detached", "modeless.bq_canonical_write",
    "modeless.rebar_mesh_stale_save", "palette.unavailable_project_teardown_rebind",
    "generated.grid_stale_handle", "generated.curtain_line_stale_handle", "generated.curtain_path_stale_handle",
    "generated.rebar_stale_handle", "generated.rebar_malformed_metadata", "generated.rebar_duplicate_canonical",
    "generated.full_live_exact_replacement", "generated.foreign_object_protection",
    "generated.undo_save_reopen", "isolation.other_dwg_untouched"
)

function Resolve-BricsCadV25Dir([string]$Requested) {
    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in @($Requested, $env:BRICSCAD_V25_DIR, "C:\Program Files\Bricsys\BricsCAD V25 en_US", "C:\Program Files\Bricsys\BricsCAD V25")) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { $candidates.Add([IO.Path]::GetFullPath($candidate)) }
    }
    $root = "C:\Program Files\Bricsys"
    if (Test-Path -LiteralPath $root) { Get-ChildItem -LiteralPath $root -Directory -Filter "BricsCAD V25*" -ErrorAction SilentlyContinue | ForEach-Object { $candidates.Add($_.FullName) } }
    foreach ($candidate in $candidates | Select-Object -Unique) {
        $exe = Join-Path $candidate "bricscad.exe"
        if ((Test-Path $exe) -and (Test-Path (Join-Path $candidate "BrxMgd.dll")) -and (Test-Path (Join-Path $candidate "TD_Mgd.dll"))) {
            $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
            $text = if ($version.ProductVersion) { [string]$version.ProductVersion } else { [string]$version.FileVersion }
            if ($text -match '^25\.') { return [IO.Path]::GetFullPath($candidate) }
        }
    }
    throw "BLOCKED: BricsCAD V25 runtime directory was not found. Install licensed V25, set BRICSCAD_V25_DIR, or pass -BricsCadDir."
}

function Read-CaseResult([string]$CaseId) {
    Write-Host ""
    Write-Host ("=== {0} ===" -f $CaseId)
    Write-Host "Execute this exact row from docs/LOCAL-011-NATIVE-QUALIFICATION.md."
    while ($true) {
        $raw = (Read-Host ("Type PASS {0}, FAIL {0}, or BLOCKED {0}" -f $CaseId)).Trim()
        $status = @("PASS", "FAIL", "BLOCKED") | Where-Object { $raw -ceq ("{0} {1}" -f $_, $CaseId) } | Select-Object -First 1
        if (-not $status) { Write-Warning "Exact case confirmation required; no result recorded."; continue }
        $note = (Read-Host "Sanitized evidence note (no private paths/raw IDs)").Trim()
        if ($note.Length -lt 12) { Write-Warning "Evidence note is too short."; continue }
        return [pscustomobject]@{ id=$CaseId; status=$status; evidence=$note; completedUtc=[DateTime]::UtcNow.ToString("O") }
    }
}

function Write-Report([string]$Status,[string]$HeadSha,[string]$BricsVersion,[object[]]$Cases,[string]$ErrorMessage,[datetime]$StartedUtc) {
    $path = Join-Path $ArtifactDir "qualification.json"
    [ordered]@{
        schema=1; localItem="LOCAL-011"; status=$Status; localPassClaimedByRunner=$false; exactSha=$HeadSha
        sourceReadyAncestors=$sourceReadyShas; windowsVersion=[Environment]::OSVersion.VersionString; bricsCadV25=$BricsVersion
        baselineReport=(Join-Path $ArtifactDir "baseline\qualification.json"); manualRunbook="docs/LOCAL-011-NATIVE-QUALIFICATION.md"
        startedUtc=$StartedUtc.ToString("O"); completedUtc=[DateTime]::UtcNow.ToString("O"); cases=$Cases; error=$ErrorMessage
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

$startedUtc=[DateTime]::UtcNow; $headSha=""; $bricsVersion=""; $results=New-Object System.Collections.Generic.List[object]
$manualProcess=$null; $finalStatus="NO_RESULT"; $errorMessage=""
try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "BLOCKED: LOCAL-011 requires Windows." }
    if (-not [Environment]::UserInteractive) { throw "BLOCKED: LOCAL-011 requires an interactive licensed desktop session." }
    foreach ($command in @("git","dotnet")) { if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "BLOCKED: required command is missing: $command" } }
    if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) { throw "BLOCKED: close existing BricsCAD processes first; this runner never kills unrelated sessions." }

    $BricsCadDir=Resolve-BricsCadV25Dir $BricsCadDir; $bricsExe=Join-Path $BricsCadDir "bricscad.exe"
    $info=[Diagnostics.FileVersionInfo]::GetVersionInfo($bricsExe); $bricsVersion=if ($info.ProductVersion) { [string]$info.ProductVersion } else { [string]$info.FileVersion }
    Push-Location $repoRoot
    try {
        $headSha=(& git rev-parse HEAD).Trim(); if ($LASTEXITCODE -ne 0 -or $headSha -notmatch '^[0-9a-fA-F]{40}$') { throw "NO_RESULT: exact Git SHA could not be resolved." }
        $dirty=@(& git status --porcelain); if ($LASTEXITCODE -ne 0) { throw "NO_RESULT: git status failed." }
        if ($dirty.Count -gt 0) { throw "NO_RESULT: working tree must be clean; local agents only pull/sync and run this test." }
        foreach ($sha in $sourceReadyShas) { & git merge-base --is-ancestor $sha HEAD *> $null; if ($LASTEXITCODE -ne 0) { throw "NO_RESULT: HEAD does not contain required LOCAL-011 source-ready ancestor $sha." } }

        & (Join-Path $PSScriptRoot "run-local-v25-qualification.ps1") -BricsCadDir $BricsCadDir -Profile $Profile -ArtifactDir (Join-Path $ArtifactDir "baseline") -SkipScreenshot
        $baselinePath=Join-Path $ArtifactDir "baseline\qualification.json"
        if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { throw "FAIL: baseline qualification report was not created." }
        $baseline=Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
        if ([string]$baseline.status -ne "PASS" -or [string]$baseline.runtimeSmokeStatus -ne "PASS" -or [string]$baseline.exactSha -ne $headSha) { throw "FAIL: baseline evidence is not a licensed runtime PASS for the exact current SHA." }

        $pluginDll=Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
        if (-not (Test-Path -LiteralPath $pluginDll -PathType Leaf)) { throw "FAIL: V25 plugin output is missing after baseline build." }
        $bootstrap=Join-Path $ArtifactDir "manual-session.scr"; @('_.NETLOAD', ('"'+$pluginDll.Replace('"','')+'"')) | Set-Content -LiteralPath $bootstrap -Encoding ASCII
        Write-Host "Open docs/LOCAL-011-NATIVE-QUALIFICATION.md beside this console; one dedicated BricsCAD V25 session will now start."
        $args=@('/nologo','/p',('"'+$Profile.Replace('"','')+'"'),'/b',('"'+$bootstrap.Replace('"','')+'"'))
        $manualProcess=Start-Process -FilePath $bricsExe -ArgumentList $args -PassThru
        foreach ($caseId in $caseIds) { if ($manualProcess.HasExited) { throw "BLOCKED: BricsCAD session exited before all cases were recorded." }; $results.Add((Read-CaseResult $caseId)) }
        while (-not $manualProcess.HasExited) { if ((Read-Host "Close the dedicated BricsCAD session normally, then type CLOSED").Trim() -cne "CLOSED") { continue }; $manualProcess.Refresh() }
        if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -gt 0) { throw "BLOCKED: a BricsCAD process remains; cleanup evidence is incomplete." }
        $statuses=@($results | ForEach-Object {$_.status})
        if ($statuses -contains "FAIL") {$finalStatus="FAIL"} elseif ($statuses -contains "BLOCKED") {$finalStatus="BLOCKED"} elseif ($statuses.Count -eq $caseIds.Count -and @($statuses|Where-Object{$_ -ne "PASS"}).Count -eq 0) {$finalStatus="PASS"} else {$finalStatus="NO_RESULT"}
    } finally { Pop-Location }
} catch {
    $errorMessage=$_.Exception.Message
    if ($errorMessage.StartsWith("FAIL:",[StringComparison]::Ordinal)) {$finalStatus="FAIL"} elseif ($errorMessage.StartsWith("BLOCKED:",[StringComparison]::Ordinal)) {$finalStatus="BLOCKED"} else {$finalStatus="NO_RESULT"}
} finally {
    if ($null -ne $manualProcess) { try { if (-not $manualProcess.HasExited) { Write-Warning "Dedicated LOCAL-011 BricsCAD session is still open; close it normally before rerunning. The runner never kills it." } } catch {} }
    $reportPath=Write-Report $finalStatus $headSha $bricsVersion $results.ToArray() $errorMessage $startedUtc
    Write-Host "LOCAL-011 evidence report: $reportPath"
}
if ($finalStatus -eq "PASS") { Write-Host "LOCAL-011 NATIVE MATRIX PASS RECORDED for exact SHA $headSha. The runner does not itself claim LOCAL_PASS."; exit 0 }
if ($finalStatus -eq "FAIL") {[Console]::Error.WriteLine("LOCAL-011 FAIL: $errorMessage"); exit 1}
if ($finalStatus -eq "BLOCKED") {[Console]::Error.WriteLine("LOCAL-011 BLOCKED: $errorMessage"); exit 2}
[Console]::Error.WriteLine("LOCAL-011 NO_RESULT: $errorMessage"); exit 3