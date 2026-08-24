[CmdletBinding()]
param(
    [string]$BricsCadDir = "",
    [string]$Profile = "QS3D-V25-3681",
    [string]$ArtifactDir = "",
    [ValidateRange(30, 900)][int]$TimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceFixSha = "4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0"
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-wall-contact-3681"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

function Resolve-BricsCadV25Dir {
    param([string]$Requested)
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($Requested)) { $candidates.Add([IO.Path]::GetFullPath($Requested)) }
    if (-not [string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_DIR)) { $candidates.Add([IO.Path]::GetFullPath($env:BRICSCAD_V25_DIR)) }
    foreach ($candidate in @(
        "C:\Program Files\Bricsys\BricsCAD V25 en_US",
        "C:\Program Files\Bricsys\BricsCAD V25"
    )) { $candidates.Add($candidate) }
    $bricsysRoot = "C:\Program Files\Bricsys"
    if (Test-Path -LiteralPath $bricsysRoot -PathType Container) {
        foreach ($dir in Get-ChildItem -LiteralPath $bricsysRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "BricsCAD V25*" }) {
            $candidates.Add($dir.FullName)
        }
    }
    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if ((Test-Path -LiteralPath (Join-Path $candidate "bricscad.exe") -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $candidate "BrxMgd.dll") -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $candidate "TD_Mgd.dll") -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $candidate "TD_MgdBrep.dll") -PathType Leaf)) {
            return [IO.Path]::GetFullPath($candidate)
        }
    }
    throw "NO_RESULT: BricsCAD V25 runtime directory was not found. Install V25 or pass -BricsCadDir."
}

function Read-Marker {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "NO_RESULT: runtime marker was not created: $Path" }
    $marker = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "FAIL: malformed runtime marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($marker.ContainsKey($key)) { throw "FAIL: duplicate runtime marker key '$key'." }
        $marker[$key] = $value
    }
    if (-not $marker.ContainsKey("status")) { throw "FAIL: runtime marker has no status." }
    return $marker
}

function Require-MarkerPass {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Phase)
    if (-not [string]::Equals([string]$Marker["status"], "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        $errorType = if ($Marker.ContainsKey("error_type")) { [string]$Marker["error_type"] } else { "unknown" }
        $errorCode = if ($Marker.ContainsKey("error_code")) { [string]$Marker["error_code"] } else { "unknown" }
        throw "FAIL: #3681 $Phase runtime phase failed ($errorType/$errorCode)."
    }
}

function New-ScrLinePath {
    param([Parameter(Mandatory = $true)][string]$Path)
    return '"' + ([IO.Path]::GetFullPath($Path).Replace('"', '')) + '"'
}

function Quote-ProcessArgument {
    param([Parameter(Mandatory = $true)][string]$Value)
    return '"' + $Value.Replace('"', '') + '"'
}

function Invoke-BricsCadScript {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Lines,
        [Parameter(Mandatory = $true)][string]$MarkerPath,
        [string]$DrawingPath = ""
    )

    $existing = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        throw "NO_RESULT: close existing BricsCAD processes before #3681 qualification; the runner never kills unrelated sessions."
    }

    $scriptPath = Join-Path $ArtifactDir ($Name + ".scr")
    Remove-Item -LiteralPath $scriptPath, $MarkerPath -Force -ErrorAction SilentlyContinue
    Set-Content -LiteralPath $scriptPath -Value $Lines -Encoding ASCII

    $oldMarker = $env:QS3D_3681_RESULT
    $process = $null
    try {
        $env:QS3D_3681_RESULT = [IO.Path]::GetFullPath($MarkerPath)
        $arguments = New-Object System.Collections.Generic.List[string]
        $arguments.Add('/nologo')
        if (-not [string]::IsNullOrWhiteSpace($DrawingPath)) { $arguments.Add((Quote-ProcessArgument ([IO.Path]::GetFullPath($DrawingPath)))) }
        $arguments.Add('/p')
        $arguments.Add((Quote-ProcessArgument $Profile))
        $arguments.Add('/b')
        $arguments.Add((Quote-ProcessArgument ([IO.Path]::GetFullPath($scriptPath))))
        $process = Start-Process -FilePath (Join-Path $BricsCadDir "bricscad.exe") -ArgumentList $arguments -PassThru
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch { }
            throw "NO_RESULT: BricsCAD timed out in #3681 phase '$Name'."
        }
    }
    finally {
        $env:QS3D_3681_RESULT = $oldMarker
        if ($null -ne $process -and -not $process.HasExited) {
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch { }
        }
    }

    $marker = Read-Marker $MarkerPath
    Require-MarkerPass $marker $Name
    return $marker
}

function Require-Case {
    param([Parameter(Mandatory = $true)]$Marker, [Parameter(Mandatory = $true)][string]$Name)
    $key = "case.$Name"
    if (-not $Marker.ContainsKey($key)) { throw "FAIL: #3681 marker is missing '$key'." }
    $value = [string]$Marker[$key]
    if (-not ($value -eq "PASS" -or $value -eq "PASS_NOT_APPLICABLE_READ_ONLY_MEASUREMENT")) {
        throw "FAIL: #3681 case '$Name' was '$value'."
    }
}

function Near {
    param([double]$Left, [double]$Right, [double]$Tolerance = 0.000001)
    return [Math]::Abs($Left - $Right) -le $Tolerance
}

$startedUtc = [DateTime]::UtcNow
$overall = "NO_RESULT"
$summaryPath = Join-Path $ArtifactDir "qualification.json"
$scratchDir = Join-Path $ArtifactDir "scratch"
$scratchDwg = Join-Path $scratchDir "wall-contact-3681.dwg"
$scratchQsdb = [IO.Path]::ChangeExtension($scratchDwg, ".qsdb")
$productDll = Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
$coreDll = Join-Path (Split-Path -Parent $productDll) "QS3D.Core.dll"
$harnessProject = Join-Path $repoRoot "tests\QS3D.BricsCAD.V25.LocalQualification\QS3D.BricsCAD.V25.LocalQualification.csproj"
$harnessDll = Join-Path $repoRoot "tests\QS3D.BricsCAD.V25.LocalQualification\bin\x64\Release\net48\QS3D.BricsCAD.V25.LocalQualification.dll"
$gatePath = Join-Path $ArtifactDir "source-fix-gate.txt"
$geometry1Path = Join-Path $ArtifactDir "geometry-1.txt"
$geometry2Path = Join-Path $ArtifactDir "geometry-2.txt"
$persistPath = Join-Path $ArtifactDir "persist.txt"
$reopenPath = Join-Path $ArtifactDir "reopen.txt"

try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw "NO_RESULT: #3681 licensed qualification requires Windows." }
    if (-not [Environment]::UserInteractive) { throw "NO_RESULT: #3681 requires an interactive licensed BricsCAD session." }
    foreach ($command in @("git", "dotnet")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "NO_RESULT: required command is missing: $command" }
    }
    $BricsCadDir = Resolve-BricsCadV25Dir $BricsCadDir

    Push-Location $repoRoot
    try {
        $headSha = (& git rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0 -or $headSha -notmatch '^[0-9a-fA-F]{40}$') { throw "NO_RESULT: exact Git SHA could not be resolved." }
        $dirty = @(& git status --porcelain)
        if ($LASTEXITCODE -ne 0) { throw "NO_RESULT: git status failed." }
        if ($dirty.Count -gt 0) { throw "NO_RESULT: working tree must be clean; local agents must not patch source." }
        & git merge-base --is-ancestor $sourceFixSha HEAD *> $null
        if ($LASTEXITCODE -ne 0) { throw "NO_RESULT: HEAD does not contain the merged #3711/#3729 wall-contact source correction $sourceFixSha." }

        Write-Host "#3681 exact HEAD: $headSha"
        Write-Host "BricsCAD V25: $BricsCadDir"

        & (Join-Path $PSScriptRoot "run-local-v25-qualification.ps1") `
            -BricsCadDir $BricsCadDir `
            -Profile $Profile `
            -ArtifactDir (Join-Path $ArtifactDir "baseline") `
            -SkipRuntime `
            -SkipScreenshot
        if ($LASTEXITCODE -ne 0) { throw "FAIL: repository-safe V25 qualification baseline failed." }

        $oldBrics = $env:BRICSCAD_V25_DIR
        try {
            $env:BRICSCAD_V25_DIR = $BricsCadDir
            & dotnet build $harnessProject -c Release -p:Platform=x64
            if ($LASTEXITCODE -ne 0) { throw "FAIL: #3681 local qualification harness build failed." }
        }
        finally { $env:BRICSCAD_V25_DIR = $oldBrics }

        foreach ($required in @($productDll, $coreDll, $harnessDll)) {
            if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "FAIL: required built qualification payload is missing: $required" }
        }

        $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($productDll).ProductVersion
        $coreVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($coreDll).ProductVersion
        $productHash = (Get-FileHash -LiteralPath $productDll -Algorithm SHA256).Hash.ToLowerInvariant()
        $coreHash = (Get-FileHash -LiteralPath $coreDll -Algorithm SHA256).Hash.ToLowerInvariant()
        $harnessHash = (Get-FileHash -LiteralPath $harnessDll -Algorithm SHA256).Hash.ToLowerInvariant()

        $baseScript = @(
            '(setvar "SECURELOAD" 0)',
            '(setvar "FILEDIA" 0)',
            '(setvar "CMDDIA" 0)',
            '(setvar "INSUNITS" 4)',
            '_.NETLOAD',
            (New-ScrLinePath $productDll),
            '_.NETLOAD',
            (New-ScrLinePath $harnessDll)
        )

        # Source-fix acceptance is intentionally bounded and fail-fast. Do not spend a licensed
        # host run on the wider matrix until both the exact touching case and the 0.05 m
        # penetration regression are coherent on the same exact binary.
        $gateScript = @($baseScript + @('_.QS3D3681SOURCEFIXGATE', '_.QUIT', '_N'))
        $gate = Invoke-BricsCadScript -Name "source-fix-gate" -Lines $gateScript -MarkerPath $gatePath
        Require-Case $gate "touching_one_end"
        Require-Case $gate "penetration_005m"

        $geometryScript = @($baseScript + @('_.QS3D3681GEOMETRY', '_.QUIT', '_N'))
        $geometry1 = Invoke-BricsCadScript -Name "geometry-1" -Lines $geometryScript -MarkerPath $geometry1Path
        foreach ($case in @("baseline", "full_end", "partial_end", "multi_neighbor_union", "top_bottom_exclusion", "two_end_blt", "semantic_capture_refresh", "stale_missing_brep_clear", "measurement_read_only", "undo_redo")) {
            Require-Case $geometry1 $case
        }
        if (-not $geometry1.ContainsKey("full_end.contact_probe_cut_count") -or [int]$geometry1["full_end.contact_probe_cut_count"] -lt 1) {
            throw "FAIL: exact face-contact case did not exercise the production contact-probe path."
        }

        $geometry2 = Invoke-BricsCadScript -Name "geometry-2" -Lines $geometryScript -MarkerPath $geometry2Path
        foreach ($key in @("baseline.deduction_m2", "full_end.deduction_m2", "partial_end.deduction_m2", "multi_neighbor_union.deduction_m2", "top_bottom_exclusion.deduction_m2", "blt.deduction_m2", "blt.net_m2")) {
            if (-not $geometry1.ContainsKey($key) -or -not $geometry2.ContainsKey($key) -or [string]$geometry1[$key] -ne [string]$geometry2[$key]) {
                throw "FAIL: second-DWG/process isolation drifted for '$key'."
            }
        }

        New-Item -ItemType Directory -Path $scratchDir -Force | Out-Null
        foreach ($path in @($scratchDwg, $scratchQsdb, ($scratchQsdb + ".bak"))) { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
        $persistScript = @(
            '(setvar "SECURELOAD" 0)',
            '(setvar "FILEDIA" 0)',
            '(setvar "CMDDIA" 0)',
            '(setvar "INSUNITS" 4)',
            '_.SAVEAS',
            (New-ScrLinePath $scratchDwg),
            '_.NETLOAD',
            (New-ScrLinePath $productDll),
            '_.NETLOAD',
            (New-ScrLinePath $harnessDll),
            '_.QS3D3681PERSIST',
            '_.QSAVE',
            '_.QUIT',
            '_N'
        )
        $persist = Invoke-BricsCadScript -Name "persist" -Lines $persistScript -MarkerPath $persistPath
        Require-Case $persist "save"
        if (-not (Test-Path -LiteralPath $scratchDwg -PathType Leaf) -or -not (Test-Path -LiteralPath $scratchQsdb -PathType Leaf)) {
            throw "FAIL: save/cold-reopen fixture did not persist both DWG and QSDB."
        }

        $reopenScript = @($baseScript + @('_.QS3D3681REOPEN', '_.QUIT', '_N'))
        $reopen = Invoke-BricsCadScript -Name "reopen" -Lines $reopenScript -MarkerPath $reopenPath -DrawingPath $scratchDwg
        Require-Case $reopen "cold_reopen"

        $gross = [double]::Parse([string]$reopen["reopen.gross_m2"], [Globalization.CultureInfo]::InvariantCulture)
        $deduction = [double]::Parse([string]$reopen["reopen.deduction_m2"], [Globalization.CultureInfo]::InvariantCulture)
        $net = [double]::Parse([string]$reopen["reopen.net_m2"], [Globalization.CultureInfo]::InvariantCulture)
        if (-not (Near $gross 2.6688) -or -not (Near $deduction 0.3200) -or -not (Near $net 2.3488)) {
            throw "FAIL: BLT regression control drifted after cold reopen."
        }

        $overall = "LOCAL_PASS"
        $report = [ordered]@{
            schema = "qs3d-local-3681-report-v2"
            status = $overall
            issue = 3681
            exactGitSha = $headSha
            sourceFixAncestor = $sourceFixSha
            bricscad = "V25 licensed local runtime"
            pluginProductVersion = $productVersion
            pluginSha256 = $productHash
            coreProductVersion = $coreVersion
            coreSha256 = $coreHash
            harnessSha256 = $harnessHash
            touchingOneEndM2 = [double]::Parse([string]$gate["touching.deduction_m2"], [Globalization.CultureInfo]::InvariantCulture)
            penetration005mM2 = [double]::Parse([string]$gate["penetration.deduction_m2"], [Globalization.CultureInfo]::InvariantCulture)
            grossFormworkM2 = $gross
            concreteContactDeductionM2 = $deduction
            netFormworkM2 = $net
            cases = @(
                "mandatory touching-only one-end 0.1600 / 2.5088",
                "mandatory 0.05 m penetration regression 0.1600 / 2.5088",
                "exact zero-volume full vertical end-face contact",
                "exact zero-volume partial vertical end-face contact",
                "multi-neighbor union/no double subtraction",
                "top/bottom exclusion",
                "semantic capture refresh",
                "missing target BREP stale-deduction clearing",
                "read-only measurement / Undo-Redo not applicable",
                "second fresh-DWG/process isolation",
                "save/cold-reopen",
                "two-end BLT control"
            )
            startedUtc = $startedUtc.ToString("O")
            completedUtc = [DateTime]::UtcNow.ToString("O")
        }
        $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
        Write-Host "LOCAL_PASS #3681 exact-sha=$headSha touching=0.1600 penetration=0.1600 gross=2.6688 deduction=0.3200 net=2.3488"
    }
    finally { Pop-Location }
}
catch {
    $message = $_.Exception.Message
    if ($message.StartsWith("FAIL:", [StringComparison]::OrdinalIgnoreCase)) { $overall = "LOCAL_FAIL" }
    elseif ($message.StartsWith("NO_RESULT:", [StringComparison]::OrdinalIgnoreCase)) { $overall = "NO_RESULT" }
    else { $overall = "LOCAL_FAIL" }
    $report = [ordered]@{
        schema = "qs3d-local-3681-report-v2"
        status = $overall
        issue = 3681
        error = $message
        startedUtc = $startedUtc.ToString("O")
        completedUtc = [DateTime]::UtcNow.ToString("O")
    }
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
    Write-Error "$overall #3681: $message"
}
finally {
    foreach ($path in @($scratchDwg, $scratchQsdb, ($scratchQsdb + ".bak"))) {
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
    $owned = @(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue | Where-Object { $_.StartTime.ToUniversalTime() -ge $startedUtc.AddSeconds(-2) })
    foreach ($process in $owned) {
        try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch { }
    }
}

if ($overall -eq "LOCAL_PASS") { exit 0 }
if ($overall -eq "NO_RESULT") { exit 2 }
exit 1
