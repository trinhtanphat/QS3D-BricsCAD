[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$PluginDll,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [ValidateRange(10, 900)][int]$StartupTimeoutSeconds = 120,
    [switch]$DemandLoadOnly,
    [switch]$SkipScreenshot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'v25-profile-sandbox.ps1')
$coreScript = Join-Path $PSScriptRoot 'test-bricscad-v25-runtime-core.ps1'
if (-not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
    throw "V25 runtime core script is missing: $coreScript"
}

$script:Qs3dGracefulCloseAttempted = $false
$script:Qs3dGracefulCloseSucceeded = $false
$script:Qs3dForceCloseFallbackUsed = $false

# The stable runtime core owns the exact process PID and calls Stop-Process only
# from its final cleanup. Shadow that cmdlet while the core is dot-sourced so
# every owned host receives a bounded graceful close attempt before force is
# used as a last-resort containment fallback.
function Stop-Process {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int[]]$Id,
        [switch]$Force
    )

    foreach ($processId in $Id) {
        $owned = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($null -eq $owned) { continue }
        try {
            $owned.Refresh()
            if (-not $owned.HasExited) {
                $script:Qs3dGracefulCloseAttempted = $true
                try {
                    if ($owned.CloseMainWindow()) {
                        $script:Qs3dGracefulCloseSucceeded = $owned.WaitForExit(5000)
                    }
                }
                catch {
                    $script:Qs3dGracefulCloseSucceeded = $false
                }
            }
            $owned.Refresh()
            if (-not $owned.HasExited) {
                $script:Qs3dForceCloseFallbackUsed = $true
                Microsoft.PowerShell.Management\Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            }
        }
        finally {
            $owned.Dispose()
        }
    }
}

$runtimeError = $null
$cleanupError = $null
$sandbox = $null
$profileEvidence = $null
$effectiveProfile = $Profile

try {
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        Assert-Qs3dNoBricsCadProcess
        $sandbox = New-Qs3dV25ProfileSandbox -SourceProfile $Profile
        $effectiveProfile = $sandbox.NonceProfile
    }

    $coreArgs = @{
        BricsCadDir = $BricsCadDir
        PluginDll = $PluginDll
        Profile = $effectiveProfile
        ArtifactDir = $ArtifactDir
        StartupTimeoutSeconds = $StartupTimeoutSeconds
        DemandLoadOnly = [bool]$DemandLoadOnly
        SkipScreenshot = [bool]$SkipScreenshot
    }
    . $coreScript @coreArgs
}
catch {
    $runtimeError = $_
}
finally {
    if ($null -ne $sandbox) {
        try {
            Assert-Qs3dNoBricsCadProcess
            $profileEvidence = Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox
            $evidenceRoot = if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
                Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\bricscad-v25-runtime'
            } else {
                [IO.Path]::GetFullPath($ArtifactDir)
            }
            New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
            $evidencePath = Join-Path $evidenceRoot 'profile-sandbox-metadata.json'
            [ordered]@{
                status = 'PASS'
                source_profile_sha256 = Get-Qs3dStringArrayHash -Values @([string]$sandbox.SourceProfile)
                cur_profile_restored = [bool]$profileEvidence.cur_profile_restored
                profile_inventory_restored = [bool]$profileEvidence.profile_inventory_restored
                nonce_profile_removed = [bool]$profileEvidence.nonce_profile_removed
                zero_bricscad_processes = [bool]$profileEvidence.zero_bricscad_processes
                profile_inventory_before_sha256 = $profileEvidence.profile_inventory_before_sha256
                profile_inventory_after_sha256 = $profileEvidence.profile_inventory_after_sha256
                graceful_close_attempted = [bool]$script:Qs3dGracefulCloseAttempted
                graceful_close_succeeded = [bool]$script:Qs3dGracefulCloseSucceeded
                force_close_fallback_used = [bool]$script:Qs3dForceCloseFallbackUsed
            } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $evidencePath -Encoding UTF8
        }
        catch {
            $cleanupError = $_
        }
    }
}

if ($null -ne $cleanupError) {
    if ($null -ne $runtimeError) {
        throw "V25 runtime failed ('$($runtimeError.Exception.Message)') and protected profile cleanup also failed ('$($cleanupError.Exception.Message)')."
    }
    throw $cleanupError
}
if ($null -ne $runtimeError) {
    throw $runtimeError
}
