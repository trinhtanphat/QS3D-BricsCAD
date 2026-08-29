[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [Parameter(Mandatory = $true)][string]$SnapshotDir,
    [string]$Configuration = 'Release',
    [string]$Platform = 'x64'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredNames = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
$repoPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $project.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "V25 build project must remain inside the repository: $project"
}
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "V25 build project is missing: $project"
}
$projectItem = Get-Item -LiteralPath $project -Force
if (($projectItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "V25 build project must be an ordinary non-reparse file: $project"
}

$snapshot = [IO.Path]::GetFullPath($SnapshotDir)
$statePath = Join-Path $snapshot 'reference-state.json'
$resolvedSnapshot = (& (Join-Path $PSScriptRoot 'snapshot-v25-compile-references.ps1') `
    -BricsCadDir $BricsCadDir `
    -SnapshotDir $snapshot `
    -StatePath $statePath | Select-Object -Last 1)
if ([string]::IsNullOrWhiteSpace([string]$resolvedSnapshot)) {
    throw 'V25 compile-reference snapshot helper returned no snapshot directory.'
}
if (-not [string]::Equals([IO.Path]::GetFullPath([string]$resolvedSnapshot), $snapshot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'V25 compile-reference snapshot helper returned an unexpected directory.'
}

$locks = New-Object 'System.Collections.Generic.List[System.IO.FileStream]'
$previousBricsCadDir = [Environment]::GetEnvironmentVariable('BRICSCAD_V25_DIR', 'Process')
try {
    foreach ($name in $requiredNames) {
        $path = Join-Path $snapshot $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "V25 compile-reference snapshot is missing: $name"
        }
        $item = Get-Item -LiteralPath $path -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "V25 compile-reference snapshot must be an ordinary non-reparse file: $path"
        }
        # FileShare.Read permits compiler reads but denies writers and delete/replace while the child build runs.
        $locks.Add([IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read))
    }

    # This verifier is a PowerShell script and fails by throwing under ErrorActionPreference=Stop.
    # Do not consult $LASTEXITCODE here: it belongs to native processes and may be null or stale.
    & (Join-Path $PSScriptRoot 'assert-v25-compile-reference-state.ps1') `
        -StatePath $statePath `
        -BricsCadDir $snapshot

    [Environment]::SetEnvironmentVariable('BRICSCAD_V25_DIR', $snapshot, 'Process')
    $arguments = @('build', $project, '-c', $Configuration, "-p:Platform=$Platform")
    & dotnet @arguments
    $buildExitCode = $LASTEXITCODE

    & (Join-Path $PSScriptRoot 'assert-v25-compile-reference-state.ps1') `
        -StatePath $statePath `
        -BricsCadDir $snapshot
    if ($buildExitCode -ne 0) {
        throw "V25 build failed with exit code $buildExitCode."
    }
}
finally {
    [Environment]::SetEnvironmentVariable('BRICSCAD_V25_DIR', $previousBricsCadDir, 'Process')
    for ($index = $locks.Count - 1; $index -ge 0; $index--) {
        $locks[$index].Dispose()
    }
}

Write-Host 'PASS: V25 build consumed only locked, verified compile-reference generations.'
