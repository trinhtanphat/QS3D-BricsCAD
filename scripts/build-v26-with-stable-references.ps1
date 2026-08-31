[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$StatePath,
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$Configuration = 'Release',
    [string]$Platform = 'x64'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredReferences = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$assertScript = Join-Path $PSScriptRoot 'assert-v26-host-reference-safety.ps1'
if (-not (Test-Path -LiteralPath $assertScript -PathType Leaf)) {
    throw "Missing V26 host-reference verifier: $assertScript"
}
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
    throw "V26 project is missing: $ProjectPath"
}

$canonicalDir = [IO.Path]::GetFullPath($BricsCadDir)
$previousBricsCadDir = [Environment]::GetEnvironmentVariable('BRICSCAD_V26_DIR', 'Process')
$locks = New-Object 'System.Collections.Generic.List[System.IO.FileStream]'
try {
    # Bind this build to the exact #4445-admitted host state before opening any
    # build reference. Verification must succeed again after all locks are held.
    & $assertScript -BricsCadDir $canonicalDir -VerifyStatePath $StatePath | Out-Null
    if (-not $?) { throw 'Initial V26 host-reference state verification failed.' }

    foreach ($name in $requiredReferences) {
        $path = Join-Path $canonicalDir $name
        $stream = [IO.File]::Open(
            $path,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        $locks.Add($stream) | Out-Null
    }

    # FileShare.Read denies write/delete/replace for every managed reference.
    # Revalidation after lock admission proves these held files are still the
    # generations recorded in the bounded state manifest.
    & $assertScript -BricsCadDir $canonicalDir -VerifyStatePath $StatePath | Out-Null
    if (-not $?) { throw 'V26 host-reference state changed while build locks were being acquired.' }

    [Environment]::SetEnvironmentVariable('BRICSCAD_V26_DIR', $canonicalDir, 'Process')
    $arguments = @(
        'build',
        $ProjectPath,
        '-c', $Configuration,
        "-p:Platform=$Platform"
    )
    & dotnet @arguments
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -ne 0) {
        throw "V26 plugin build failed with exit code $buildExitCode."
    }

    # Keep all managed-reference locks until the exact admitted state has been
    # revalidated after MSBuild/compiler consumption finishes.
    & $assertScript -BricsCadDir $canonicalDir -VerifyStatePath $StatePath | Out-Null
    if (-not $?) { throw 'V26 host-reference state changed during plugin build.' }
}
finally {
    [Environment]::SetEnvironmentVariable('BRICSCAD_V26_DIR', $previousBricsCadDir, 'Process')
    for ($index = $locks.Count - 1; $index -ge 0; $index--) {
        $locks[$index].Dispose()
    }
}
