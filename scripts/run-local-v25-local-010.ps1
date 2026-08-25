[CmdletBinding()]
param(
    [string]$BricsCadDir = "",
    [string]$Profile = "QS3D-V25-LOCAL010",
    [string]$ArtifactDir = ""
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) { $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-local-010" }
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

$caseIds = @(
    "performance.dependency_graph", "performance.regeneration", "performance.rooms", "performance.wall_junctions",
    "performance.auto_host", "performance.curtain", "performance.bq_bbs_ed2_interchange", "performance.ownership_health", "performance.rebar_limits",
    "ui.start_center_100", "ui.start_center_125", "ui.start_center_150", "ui.start_center_200",
    "ui.ribbon_100", "ui.ribbon_125", "ui.ribbon_150", "ui.ribbon_200",
    "ui.workspace_narrow", "ui.workspace_normal", "ui.workspace_wide", "ui.document_switch_cleanup"
)

function Resolve-BricsCadV25Dir([string]$Requested) {
    $candidates = @($Requested, $env:BRICSCAD_V25_DIR, "C:\Program Files\Bricsys\BricsCAD V25 en_US", "C:\Program Files\Bricsys\BricsCAD V25") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($candidate in $candidates) {
        $full=[IO.Path]::GetFullPath($candidate); $exe=Join-Path $full "bricscad.exe"
        if ((Test-Path $exe) -and (Test-Path (Join-Path $full "BrxMgd.dll")) -and (Test-Path (Join-Path $full "TD_Mgd.dll"))) {
            $v=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe); $text=if($v.ProductVersion){$v.ProductVersion}else{$v.FileVersion}; if ($text -match '^25\.') { return $full }
        }
    }
    throw "BLOCKED: licensed BricsCAD V25 runtime was not found."
}

function Read-Case([string]$Id) {
    Write-Host ""; Write-Host ("=== {0} ===" -f $Id)
    while ($true) {
        $raw=(Read-Host ("Type PASS {0}, FAIL {0}, or BLOCKED {0}" -f $Id)).Trim()
        $status=@("PASS","FAIL","BLOCKED") | Where-Object { $raw -ceq ("{0} {1}" -f $_,$Id) } | Select-Object -First 1
        if (-not $status) { Write-Warning "Exact case confirmation required."; continue }
        $note=(Read-Host "Sanitized evidence/timing note (no private paths/raw IDs)").Trim()
        if ($note.Length -lt 12) { Write-Warning "Evidence note is too short."; continue }
        return [pscustomobject]@{id=$Id;status=$status;evidence=$note;completedUtc=[DateTime]::UtcNow.ToString("O")}
    }
}

$started=[DateTime]::UtcNow; $head=""; $version=""; $status="NO_RESULT"; $errorMessage=""; $results=New-Object System.Collections.Generic.List[object]; $proc=$null
try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) { throw "BLOCKED: LOCAL-010 requires an interactive Windows desktop." }
    foreach($cmd in @("git","dotnet")){if(-not(Get-Command $cmd -ErrorAction SilentlyContinue)){throw "BLOCKED: required command missing: $cmd"}}
    if (@(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0) { throw "BLOCKED: close existing BricsCAD processes first; this runner never kills unrelated sessions." }
    $BricsCadDir=Resolve-BricsCadV25Dir $BricsCadDir; $exe=Join-Path $BricsCadDir "bricscad.exe"; $vi=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe); $version=if($vi.ProductVersion){$vi.ProductVersion}else{$vi.FileVersion}
    Push-Location $repoRoot
    try {
        $head=(& git rev-parse HEAD).Trim(); if($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-fA-F]{40}$'){throw "NO_RESULT: exact Git SHA could not be resolved."}
        if(@(& git status --porcelain).Count -gt 0){throw "NO_RESULT: working tree must be clean; local workers only pull/sync and run."}
        & (Join-Path $PSScriptRoot "run-local-v25-qualification.ps1") -BricsCadDir $BricsCadDir -Profile $Profile -ArtifactDir (Join-Path $ArtifactDir "baseline") -SkipScreenshot
        $baselinePath=Join-Path $ArtifactDir "baseline\qualification.json"; if(-not(Test-Path $baselinePath)){throw "FAIL: baseline qualification report missing."}
        $baseline=Get-Content $baselinePath -Raw | ConvertFrom-Json; if([string]$baseline.status -ne "PASS" -or [string]$baseline.runtimeSmokeStatus -ne "PASS" -or [string]$baseline.exactSha -ne $head){throw "FAIL: baseline is not licensed runtime PASS for exact SHA."}
        $plugin=Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"; if(-not(Test-Path $plugin)){throw "FAIL: V25 plugin output missing."}
        $scr=Join-Path $ArtifactDir "manual-session.scr"; @('_.NETLOAD',('"'+$plugin.Replace('"','')+'"')) | Set-Content $scr -Encoding ASCII
        Write-Host "Follow docs/LOCAL-010-PERFORMANCE-UI-QUALIFICATION.md. Use representative sanitized projects only."
        $proc=Start-Process -FilePath $exe -ArgumentList @('/nologo','/p',('"'+$Profile+'"'),'/b',('"'+$scr+'"')) -PassThru
        foreach($id in $caseIds){if($proc.HasExited){throw "BLOCKED: BricsCAD exited before matrix completion."};$results.Add((Read-Case $id))}
        while(-not $proc.HasExited){if((Read-Host "Close the dedicated BricsCAD session normally, then type CLOSED").Trim() -cne "CLOSED"){continue};$proc.Refresh()}
        if(@(Get-Process -Name bricscad -ErrorAction SilentlyContinue).Count -gt 0){throw "BLOCKED: BricsCAD process residue remains."}
        $s=@($results|ForEach-Object{$_.status}); if($s -contains "FAIL"){$status="FAIL"}elseif($s -contains "BLOCKED"){$status="BLOCKED"}elseif($s.Count -eq $caseIds.Count -and @($s|Where-Object{$_ -ne "PASS"}).Count -eq 0){$status="PASS"}else{$status="NO_RESULT"}
    } finally { Pop-Location }
} catch {
    $errorMessage=$_.Exception.Message; if($errorMessage.StartsWith("FAIL:")){$status="FAIL"}elseif($errorMessage.StartsWith("BLOCKED:")){$status="BLOCKED"}else{$status="NO_RESULT"}
} finally {
    $report=Join-Path $ArtifactDir "qualification.json"
    [ordered]@{schema=1;localItem="LOCAL-010";status=$status;localPassClaimedByRunner=$false;exactSha=$head;windowsVersion=[Environment]::OSVersion.VersionString;bricsCadV25=$version;baselineReport=(Join-Path $ArtifactDir "baseline\qualification.json");manualRunbook="docs/LOCAL-010-PERFORMANCE-UI-QUALIFICATION.md";startedUtc=$started.ToString("O");completedUtc=[DateTime]::UtcNow.ToString("O");cases=$results.ToArray();error=$errorMessage} | ConvertTo-Json -Depth 8 | Set-Content $report -Encoding UTF8
    Write-Host "LOCAL-010 evidence report: $report"
}
if($status -eq "PASS"){Write-Host "LOCAL-010 MATRIX PASS RECORDED for exact SHA $head. Runner does not itself claim LOCAL_PASS.";exit 0}
if($status -eq "FAIL"){exit 1};if($status -eq "BLOCKED"){exit 2};exit 3
