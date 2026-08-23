param(
    [Parameter(Mandatory = $true)][ValidateSet(25, 26)][int]$HostMajor,
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$Profile,
    [string]$FixtureDwg = "",
    [string]$ArtifactDir = "",
    [string]$DotNetExe = "",
    [ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
. (Join-Path $PSScriptRoot "bricscad-runner-window-interop.ps1")

if ($null -eq ("Qs3dExactEscapeInput" -as [type])) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class Qs3dExactEscapeInput
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint attachThread, uint attachToThread, bool attach);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr window);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr window, bool altTab);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr window);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
"@
}

function Require-ContainedPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $full = [IO.Path]::GetFullPath($Path)
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $prefix = $rootFull + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label escaped its intended root."
    }
    return $full
}

function Read-Marker {
    param([Parameter(Mandatory = $true)][string]$Path)
    $result = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separator = $line.IndexOf('=')
        if ($separator -le 0) { throw "Malformed repeated-mode marker line." }
        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        if ($result.ContainsKey($key)) { throw "Duplicate repeated-mode marker key: $key" }
        $result[$key] = $value
    }
    return $result
}

function Set-MarkerFailureMetadata {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$FallbackPhase,
        [Parameter(Mandatory = $true)][string]$FallbackCode
    )
    $phase = if ($Marker.ContainsKey("phase")) { [string]$Marker["phase"] } else { $FallbackPhase }
    $code = if ($Marker.ContainsKey("error_code")) { [string]$Marker["error_code"] } else { $FallbackCode }
    if ($phase -notmatch '^[a-z0-9_]{1,64}$') { $phase = "marker_phase_rejected" }
    if ($code -notmatch '^[A-Z0-9_]{1,96}$' -or [string]::Equals($code, "NONE", [StringComparison]::Ordinal)) {
        $code = $FallbackCode
    }
    $script:failurePhase = $phase
    $script:failureCode = $code
}

function Require-Marker {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][int]$ExpectedSegments,
        [string]$ExpectedStatus = "PASS"
    )
    $expected = [ordered]@{
        status = $ExpectedStatus
        schema = "QS3D_DIRECT_DRAW_REPEAT_RUNTIME_V1"
        phase = $Phase
        nonce = $script:nonce
        host_major = [string]$HostMajor
        adapter = "QS3D.BricsCAD.V$HostMajor"
        production_command = "QS3DDRAWBEAMREPEAT"
        semantic_segments = [string]$ExpectedSegments
        source_type = "LINE"
        native_type = "Solid3d"
        preview_type = "DrawJigProfileStrip"
        undo_scope = "WholeCommand"
        error_code = "NONE"
    }
    foreach ($entry in $expected.GetEnumerator()) {
        if (-not $Marker.ContainsKey($entry.Key)) {
            Set-MarkerFailureMetadata -Marker $Marker -FallbackPhase $Phase -FallbackCode "MARKER_KEY_MISSING"
            throw "Repeated-mode $Phase marker is missing '$($entry.Key)'."
        }
        if (-not [string]::Equals([string]$Marker[$entry.Key], [string]$entry.Value, [StringComparison]::OrdinalIgnoreCase)) {
            Set-MarkerFailureMetadata -Marker $Marker -FallbackPhase $Phase -FallbackCode "MARKER_CONTRACT_REJECTED"
            throw "Repeated-mode $Phase marker '$($entry.Key)' expected '$($entry.Value)' but was '$($Marker[$entry.Key])'."
        }
    }
}

function Require-RuntimeIdentity {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedAssembly
    )
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $marker = Read-Marker $Path
    foreach ($key in @("status", "assembly", "native_runtime_major", "native_runtime_matches")) {
        if (-not $marker.ContainsKey($key)) { throw "Runtime identity marker is missing '$key'." }
    }
    if (-not [string]::Equals([string]$marker.status, "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        throw "The loaded QS3D runtime identity probe did not pass."
    }
    if (-not [string]::Equals(
        [IO.Path]::GetFullPath([string]$marker.assembly),
        [IO.Path]::GetFullPath($ExpectedAssembly),
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "BricsCAD loaded a different QS3D assembly before the exact candidate NETLOAD."
    }
    if (-not [string]::Equals([string]$marker.native_runtime_major, [string]$HostMajor, [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$marker.native_runtime_matches, "true", [StringComparison]::OrdinalIgnoreCase)) {
        throw "The loaded candidate does not match the requested BricsCAD host major."
    }
    return $true
}

function Require-OwnedProcessIdentity {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $Process.Refresh()
    if ($Process.HasExited) { return }
    if (-not [string]::Equals(
        [IO.Path]::GetFullPath($Process.Path),
        [IO.Path]::GetFullPath($ExpectedExecutable),
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Runner-owned process is not the requested BricsCAD executable."
    }
}

function Set-Qs3dDemandLoadControls {
    param(
        [Parameter(Mandatory = $true)][string]$RegistryPath,
        [Parameter(Mandatory = $true)][int]$ExpectedCurrent,
        [Parameter(Mandatory = $true)][int]$NewValue
    )
    $current = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name "LoadCtrls" -ErrorAction Stop)
    if ($current -ne $ExpectedCurrent) {
        throw "QS3D DemandLoad controls changed concurrently; refusing to overwrite them."
    }
    Set-ItemProperty -LiteralPath $RegistryPath -Name "LoadCtrls" -Value $NewValue -ErrorAction Stop
    $readback = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name "LoadCtrls" -ErrorAction Stop)
    if ($readback -ne $NewValue) { throw "QS3D DemandLoad control readback did not match the requested guarded value." }
}

function Restore-Qs3dDemandLoadControls {
    param(
        [Parameter(Mandatory = $true)][string]$RegistryPath,
        [Parameter(Mandatory = $true)][int]$OriginalValue,
        [Parameter(Mandatory = $true)][int]$IsolatedValue
    )
    $current = [int](Get-ItemPropertyValue -LiteralPath $RegistryPath -Name "LoadCtrls" -ErrorAction Stop)
    if ($current -eq $OriginalValue) { return }
    Set-Qs3dDemandLoadControls -RegistryPath $RegistryPath -ExpectedCurrent $IsolatedValue -NewValue $OriginalValue
}

function Wait-ForMarkerAndOwnedHost {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentityPath,
        [Parameter(Mandatory = $true)][string]$ExpectedAssembly,
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    Require-OwnedProcessIdentity -Process $Process -ExpectedExecutable $ExpectedExecutable
    $identityValidated = $false
    while ([DateTime]::UtcNow -lt $Deadline) {
        $Process.Refresh()
        $alive = -not $Process.HasExited
        if ($alive) {
            Close-Qs3dProxyInformationDialog -Process $Process | Out-Null
            Close-Qs3dUnsavedProjectChangesDialog -Process $Process | Out-Null
        }
        if (-not $identityValidated) {
            $identityValidated = Require-RuntimeIdentity -Path $RuntimeIdentityPath -ExpectedAssembly $ExpectedAssembly
        }
        if ($identityValidated -and (Test-Path -LiteralPath $Path -PathType Leaf)) {
            if (-not $alive) { throw "The runner-owned BricsCAD process exited before physical input." }
            return $Process
        }
        if (-not $alive) {
            throw "BricsCAD exited before the repeated-mode input-ready marker."
        }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out waiting for the repeated-mode ESC ready marker."
}

function Start-ExactCandidateHost {
    param(
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string]$Arguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentityPath,
        [Parameter(Mandatory = $true)][string]$PluginDll,
        [Parameter(Mandatory = $true)][DateTime]$Deadline,
        [Parameter(Mandatory = $true)]$OwnedProcesses,
        [Parameter(Mandatory = $true)][bool]$IsolateDemandLoad,
        [string]$DemandLoadRegistryPath = "",
        [int]$OriginalDemandLoadControls = 0,
        [int]$IsolatedDemandLoadControls = 0,
        [Parameter(Mandatory = $true)][ref]$IsolationCount,
        [Parameter(Mandatory = $true)][ref]$DemandLoadRestored,
        [switch]$RequiresForegroundInput
    )
    $changed = $false
    try {
        if ($IsolateDemandLoad) {
            Set-Qs3dDemandLoadControls -RegistryPath $DemandLoadRegistryPath `
                -ExpectedCurrent $OriginalDemandLoadControls -NewValue $IsolatedDemandLoadControls
            $changed = $true
            $DemandLoadRestored.Value = $false
            $IsolationCount.Value = [int]$IsolationCount.Value + 1
        }
        if ($RequiresForegroundInput) {
            $process = Start-Process -FilePath $Executable -ArgumentList $Arguments `
                -WorkingDirectory $WorkingDirectory -PassThru -WindowStyle Normal
        }
        else {
            $process = Start-Process -FilePath $Executable -ArgumentList $Arguments `
                -WorkingDirectory $WorkingDirectory -PassThru -WindowStyle Hidden
        }
        $OwnedProcesses.Add($process)
        return Wait-ForMarkerAndOwnedHost -Path $RuntimeIdentityPath `
            -RuntimeIdentityPath $RuntimeIdentityPath -ExpectedAssembly $PluginDll `
            -Process $process -ExpectedExecutable $Executable -Deadline $Deadline
    }
    finally {
        if ($changed) {
            Restore-Qs3dDemandLoadControls -RegistryPath $DemandLoadRegistryPath `
                -OriginalValue $OriginalDemandLoadControls -IsolatedValue $IsolatedDemandLoadControls
            $DemandLoadRestored.Value = $true
        }
    }
}

function Bind-ExactProcessForeground {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $Process.Refresh()
    if ($Process.HasExited) { throw "Owned BricsCAD exited before ESC input." }
    $actualExecutable = [IO.Path]::GetFullPath($Process.Path)
    if (-not [string]::Equals(
        $actualExecutable,
        [IO.Path]::GetFullPath($ExpectedExecutable),
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "ESC input target executable did not match the guarded BricsCAD host."
    }

    $window = [IntPtr]::Zero
    $windowDeadline = [DateTime]::UtcNow.AddSeconds(10)
    while ($window -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $windowDeadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Owned BricsCAD exited before exposing its exact window." }
        $window = $Process.MainWindowHandle
        if ($window -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 100 }
    }
    if ($window -eq [IntPtr]::Zero) { throw "Owned BricsCAD did not expose an exact main window for ESC input." }

    $windowProcessId = [uint32]0
    $windowThreadId = [Qs3dExactEscapeInput]::GetWindowThreadProcessId($window, [ref]$windowProcessId)
    if ($windowThreadId -eq 0 -or $windowProcessId -ne [uint32]$Process.Id) {
        throw "Exact BricsCAD main window did not belong to the guarded process."
    }

    $runnerThreadId = [Qs3dExactEscapeInput]::GetCurrentThreadId()
    $activationShell = New-Object -ComObject WScript.Shell
    $foregroundDeadline = [DateTime]::UtcNow.AddSeconds(10)
    $foregroundMatches = $false
    while (-not $foregroundMatches -and [DateTime]::UtcNow -lt $foregroundDeadline) {
        $attached = $false
        try {
            if ($runnerThreadId -ne $windowThreadId) {
                $attached = [Qs3dExactEscapeInput]::AttachThreadInput($runnerThreadId, $windowThreadId, $true)
            }
            $activationShell.AppActivate($Process.Id) | Out-Null
            [Qs3dExactEscapeInput]::ShowWindowAsync($window, 9) | Out-Null
            [Qs3dExactEscapeInput]::SwitchToThisWindow($window, $true)
            [Qs3dExactEscapeInput]::BringWindowToTop($window) | Out-Null
            [Qs3dExactEscapeInput]::SetForegroundWindow($window) | Out-Null
            [Qs3dExactEscapeInput]::SetActiveWindow($window) | Out-Null
            [Qs3dExactEscapeInput]::SetFocus($window) | Out-Null
        }
        finally {
            if ($attached) {
                [Qs3dExactEscapeInput]::AttachThreadInput($runnerThreadId, $windowThreadId, $false) | Out-Null
            }
        }
        Start-Sleep -Milliseconds 50
        $foreground = [Qs3dExactEscapeInput]::GetForegroundWindow()
        $foregroundProcessId = [uint32]0
        [Qs3dExactEscapeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundProcessId) | Out-Null
        $foregroundMatches = $foregroundProcessId -eq [uint32]$Process.Id
    }
    if (-not $foregroundMatches) { throw "Could not bind key input to the exact owned BricsCAD process." }
}

function Send-ExactProcessEscape {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    Bind-ExactProcessForeground -Process $Process -ExpectedExecutable $ExpectedExecutable

    [Qs3dExactEscapeInput]::keybd_event(0x1B, 0, 0, [UIntPtr]::Zero)
    [Qs3dExactEscapeInput]::keybd_event(0x1B, 0, 2, [UIntPtr]::Zero)
}

function Stop-OwnedHosts {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][Diagnostics.Process[]]$Processes,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    $expectedPath = [IO.Path]::GetFullPath($ExpectedExecutable)
    foreach ($process in $Processes) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                $actualPath = [IO.Path]::GetFullPath($process.Path)
                if (-not [string]::Equals($actualPath, $expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Runner-owned process identity changed before cleanup."
                }
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit(10000) | Out-Null
            }
        }
        catch { }
    }
}

function Wait-OwnedHostsExited {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][Diagnostics.Process[]]$Processes,
        [ValidateRange(1, 60)][int]$TimeoutSeconds = 15
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $alive = @($Processes | Where-Object {
            try { $_.Refresh(); -not $_.HasExited }
            catch { $false }
        })
        if ($alive.Count -eq 0) { return $true }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

function Wait-ForFilesAndExit {
    param(
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentityPath,
        [Parameter(Mandatory = $true)][string]$ExpectedAssembly,
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline,
        [switch]$RequestCloseAfterEvidence,
        [switch]$CancelActiveCommandBeforeClose,
        [switch]$TerminateOwnedProcessAfterEvidence
    )
    Require-OwnedProcessIdentity -Process $Process -ExpectedExecutable $ExpectedExecutable
    $allObserved = $false
    $identityValidated = $false
    $closeRequested = $false
    $evidenceObservedAt = [DateTime]::MinValue
    while ([DateTime]::UtcNow -lt $Deadline) {
        $Process.Refresh()
        $alive = -not $Process.HasExited
        if ($alive) {
            Close-Qs3dProxyInformationDialog -Process $Process | Out-Null
            Close-Qs3dUnsavedProjectChangesDialog -Process $Process | Out-Null
        }
        if (-not $identityValidated) {
            $identityValidated = Require-RuntimeIdentity -Path $RuntimeIdentityPath -ExpectedAssembly $ExpectedAssembly
        }
        $allObserved = $identityValidated -and
            @($Paths | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -eq 0
        if ($allObserved -and $evidenceObservedAt -eq [DateTime]::MinValue) {
            $evidenceObservedAt = [DateTime]::UtcNow
        }
        if ($allObserved -and $alive -and $RequestCloseAfterEvidence -and -not $closeRequested) {
            Require-OwnedProcessIdentity -Process $Process -ExpectedExecutable $ExpectedExecutable
            if ($CancelActiveCommandBeforeClose) {
                Send-ExactProcessEscape -Process $Process -ExpectedExecutable $ExpectedExecutable
                Start-Sleep -Milliseconds 250
            }
            $Process.CloseMainWindow() | Out-Null
            $closeRequested = $true
        }
        if ($allObserved -and $alive -and $TerminateOwnedProcessAfterEvidence -and
            $evidenceObservedAt -ne [DateTime]::MinValue -and
            [DateTime]::UtcNow -ge $evidenceObservedAt.AddSeconds(10)) {
            Require-OwnedProcessIdentity -Process $Process -ExpectedExecutable $ExpectedExecutable
            $Process.Refresh()
            if (-not $Process.HasExited) {
                Stop-Process -Id $Process.Id -Force -ErrorAction Stop
                if (-not $Process.WaitForExit(10000)) {
                    throw "Exact runner-owned BricsCAD process did not terminate after complete evidence."
                }
                $script:ownedEvidenceTerminationCount++
            }
            return
        }
        if ($allObserved -and -not $alive) { return }
        if (-not $allObserved -and -not $alive) {
            throw "BricsCAD exited before repeated-mode evidence was complete."
        }
        Start-Sleep -Milliseconds 250
    }
    if (-not $allObserved) { throw "Timed out waiting for repeated-mode evidence." }
    throw "Timed out waiting for BricsCAD repeated-mode process cleanup."
}

function Assert-V26DotNetRuntime {
    param([Parameter(Mandatory = $true)][string]$DotNetExecutable)

    $root = [IO.Path]::GetFullPath((Split-Path -Parent $DotNetExecutable))
    $fxr = @(Get-ChildItem -LiteralPath (Join-Path $root "host\fxr") -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "hostfxr.dll") -PathType Leaf)
    })
    $core = @(Get-ChildItem -LiteralPath (Join-Path $root "shared\Microsoft.NETCore.App") -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "coreclr.dll") -PathType Leaf)
    })
    $desktop = @(Get-ChildItem -LiteralPath (Join-Path $root "shared\Microsoft.WindowsDesktop.App") -Directory -ErrorAction Stop | Where-Object {
        $_.Name -match '^8\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "PresentationFramework.dll") -PathType Leaf)
    })
    if ($fxr.Count -eq 0 -or $core.Count -eq 0 -or $desktop.Count -eq 0) {
        throw "V26 repeated-mode qualification requires a complete .NET 8 WindowsDesktop runtime beside dotnet.exe."
    }
    return $root
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::UserInteractive) {
    throw "Repeated Direct Draw qualification requires an interactive Windows session."
}
if ([string]::IsNullOrWhiteSpace($Profile)) {
    throw "Repeated Direct Draw qualification requires an initialized nonblank BricsCAD profile."
}

$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
if (-not (Test-Path -LiteralPath $bricscadExe -PathType Leaf)) { throw "BricsCAD executable is missing." }
$hostVersion = (Get-Item -LiteralPath $bricscadExe).VersionInfo
if ($hostVersion.FileMajorPart -ne $HostMajor) {
    throw "Expected BricsCAD V$HostMajor but found '$($hostVersion.FileVersion)'."
}
if (@(Get-Process -Name "bricscad" -ErrorAction SilentlyContinue).Count -ne 0) {
    throw "Close every BricsCAD process before the dedicated repeated-mode run."
}

$gitCommonDir = (& git -C $repoRoot rev-parse --path-format=absolute --git-common-dir).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitCommonDir)) {
    throw "Cannot resolve the shared Git directory for qualification tooling/artifacts."
}
$mainWorktreeRoot = Split-Path -Parent ([IO.Path]::GetFullPath($gitCommonDir))
if ([string]::IsNullOrWhiteSpace($DotNetExe)) {
    $DotNetExe = Join-Path $mainWorktreeRoot "artifacts\tooling\dotnet\dotnet.exe"
}
$DotNetExe = [IO.Path]::GetFullPath($DotNetExe)
if (-not (Test-Path -LiteralPath $DotNetExe -PathType Leaf)) { throw "dotnet.exe is missing." }
$dotNetRoot = Split-Path -Parent $DotNetExe
if ($HostMajor -eq 26) {
    $dotNetRoot = Assert-V26DotNetRuntime -DotNetExecutable $DotNetExe
}
if ([string]::IsNullOrWhiteSpace($FixtureDwg)) {
    $FixtureDwg = Join-Path $repoRoot "samples\generated\QS3D-Sample.dwg"
}
$FixtureDwg = [IO.Path]::GetFullPath($FixtureDwg)
if (-not (Test-Path -LiteralPath $FixtureDwg -PathType Leaf)) { throw "Fixture DWG is missing." }

$gitHead = (& git -C $repoRoot rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $gitHead -notmatch '^[0-9a-f]{40}$') { throw "Cannot resolve exact Git HEAD." }
$dirty = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw "git status failed." }
if ($dirty.Count -ne 0) { throw "Repeated-mode qualification requires a clean committed worktree." }

if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $mainWorktreeRoot `
        ("artifacts\q3612\" + $gitHead.Substring(0, 12) + "-v" + $HostMajor)
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$longestPrivatePath = Join-Path $ArtifactDir `
    ("private-" + ("0" * 32) + "\repeat-runtime-document-switch.txt")
if ($longestPrivatePath.Length -gt 210) {
    throw "Repeated-mode private paths are too long for .NET Framework atomic marker publication; choose a shorter ArtifactDir."
}
New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$script:nonce = [Guid]::NewGuid().ToString("N")
$runRoot = Require-ContainedPath -Path (Join-Path $ArtifactDir ("private-" + $script:nonce)) -Root $ArtifactDir -Label "run root"
New-Item -ItemType Directory -Path $runRoot | Out-Null
$drawing = Require-ContainedPath -Path (Join-Path $runRoot "repeat-a.dwg") -Root $runRoot -Label "disposable drawing"
$sidecar = [IO.Path]::ChangeExtension($drawing, ".qsdb")
$escapeDrawing = Require-ContainedPath -Path (Join-Path $runRoot "repeat-esc.dwg") -Root $runRoot -Label "ESC disposable drawing"
$escapeSidecar = [IO.Path]::ChangeExtension($escapeDrawing, ".qsdb")
$ucsDrawing = Require-ContainedPath -Path (Join-Path $runRoot "repeat-planar-ucs.dwg") -Root $runRoot -Label "planar-UCS disposable drawing"
$ucsSidecar = [IO.Path]::ChangeExtension($ucsDrawing, ".qsdb")
$switchDrawingA = Require-ContainedPath -Path (Join-Path $runRoot "repeat-switch-a.dwg") -Root $runRoot -Label "switch drawing A"
$switchDrawingB = Require-ContainedPath -Path (Join-Path $runRoot "repeat-switch-b.dwg") -Root $runRoot -Label "switch drawing B"
$switchSidecarA = [IO.Path]::ChangeExtension($switchDrawingA, ".qsdb")
$switchSidecarB = [IO.Path]::ChangeExtension($switchDrawingB, ".qsdb")
$scriptOne = Require-ContainedPath -Path (Join-Path $runRoot "repeat-session1.private.scr") -Root $runRoot -Label "session-one script"
$scriptTwo = Require-ContainedPath -Path (Join-Path $runRoot "repeat-session2.private.scr") -Root $runRoot -Label "session-two script"
$scriptEscape = Require-ContainedPath -Path (Join-Path $runRoot "repeat-esc.private.scr") -Root $runRoot -Label "ESC session script"
$scriptUcs = Require-ContainedPath -Path (Join-Path $runRoot "repeat-planar-ucs.private.scr") -Root $runRoot -Label "planar-UCS session script"
$scriptSwitch = Require-ContainedPath -Path (Join-Path $runRoot "repeat-switch.private.scr") -Root $runRoot -Label "document-switch session script"
$metadataPath = Join-Path $ArtifactDir "repeat-v$HostMajor-metadata.json"
$phasePaths = [ordered]@{
    runtime_session1 = Join-Path $runRoot "repeat-runtime-session1.txt"
    runtime_cold = Join-Path $runRoot "repeat-runtime-cold.txt"
    runtime_esc = Join-Path $runRoot "repeat-runtime-esc.txt"
    runtime_planar_ucs = Join-Path $runRoot "repeat-runtime-planar-ucs.txt"
    runtime_document_switch = Join-Path $runRoot "repeat-runtime-document-switch.txt"
    after = Join-Path $runRoot "repeat-after.txt"
    undo = Join-Path $runRoot "repeat-undo.txt"
    redo = Join-Path $runRoot "repeat-redo.txt"
    cold_reopen = Join-Path $runRoot "repeat-cold-reopen.txt"
    esc_ready = Join-Path $runRoot "repeat-esc-ready.txt"
    esc = Join-Path $runRoot "repeat-esc.txt"
    planar_ucs = Join-Path $runRoot "repeat-planar-ucs.txt"
    document_switch_ready = Join-Path $runRoot "repeat-document-switch-ready.txt"
    document_switch = Join-Path $runRoot "repeat-document-switch.txt"
}

$pluginProject = if ($HostMajor -eq 25) {
    Join-Path $repoRoot "src\QS3D.BricsCAD.V25\QS3D.BricsCAD.V25.csproj"
}
else {
    Join-Path $repoRoot "src\QS3D.BricsCAD.V26\QS3D.BricsCAD.V26.csproj"
}
$pluginDll = if ($HostMajor -eq 25) {
    Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
}
else {
    Join-Path $repoRoot "src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll"
}

$demandLoadRegistryPath =
    "Registry::HKEY_CURRENT_USER\Software\Bricsys\BricsCAD\V$($HostMajor)x64\en_US\Applications\QS3D"
$isolateDemandLoad = $false
$demandLoadOriginalControls = 0
$demandLoadIsolatedControls = 0
if (Test-Path -LiteralPath $demandLoadRegistryPath -PathType Container) {
    $demandLoadOriginalControls =
        [int](Get-ItemPropertyValue -LiteralPath $demandLoadRegistryPath -Name "LoadCtrls" -ErrorAction Stop)
    if (($demandLoadOriginalControls -band 2) -ne 0) {
        $commandsPath = $demandLoadRegistryPath + "\Commands"
        if (-not (Test-Path -LiteralPath $commandsPath -PathType Container)) {
            throw "Cannot isolate startup DemandLoad without the installed command-trigger registration."
        }
        $runtimeCommand = [string](
            Get-ItemPropertyValue -LiteralPath $commandsPath -Name "QS3DRUNTIMEPROBE" -ErrorAction Stop)
        if (-not [string]::Equals($runtimeCommand, "QS3DRUNTIMEPROBE", [StringComparison]::Ordinal)) {
            throw "Installed command-trigger registration is not canonical."
        }
        $demandLoadIsolatedControls = [int](($demandLoadOriginalControls -band (-bnot 2)) -bor 4)
        $isolateDemandLoad = $true
    }
}

$oldHostDir = [Environment]::GetEnvironmentVariable("BRICSCAD_V$($HostMajor)_DIR", "Process")
$oldDotNetRoot = [Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")
$oldDotNetRootX64 = [Environment]::GetEnvironmentVariable("DOTNET_ROOT_X64", "Process")
$oldRuntimeResult = $env:QS3D_RUNTIME_RESULT
$oldEvidence = $env:QS3D_REPEAT_EVIDENCE_DIR
$oldDrawing = $env:QS3D_REPEAT_DWG
$oldNonce = $env:QS3D_REPEAT_NONCE
$oldSecondDrawing = $env:QS3D_REPEAT_SECOND_DWG
$qualificationError = $null
$cleanupError = $null
$script:failurePhase = "NONE"
$script:failureCode = "NONE"
$script:currentPhase = "setup"
$script:ownedEvidenceTerminationCount = 0
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$startedAt = [DateTime]::UtcNow
$drawingPersisted = $false
$sidecarPersisted = $false
$processCleanup = $false
$privateCleanup = $false
$acceptedSegments = 0
$drawJigPreviewPassed = $false
$enterTerminationPassed = $false
$wholeCommandUndoPassed = $false
$wholeCommandRedoPassed = $false
$saveColdReopenPassed = $false
$exactRuntimeIdentityPassed = $false
$demandLoadIsolationCount = 0
$demandLoadRestored = $true
$physicalEscPassed = $false
$planarUcsPassed = $false
$documentSwitchPassed = $false
$ownedProcesses = New-Object System.Collections.Generic.List[Diagnostics.Process]

try {
    [Environment]::SetEnvironmentVariable("BRICSCAD_V$($HostMajor)_DIR", $BricsCadDir, "Process")
    if ($HostMajor -eq 26) {
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $dotNetRoot, "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT_X64", $dotNetRoot, "Process")
    }
    & $DotNetExe build $pluginProject -c Release "-p:Platform=x64" "-nodeReuse:false"
    if ($LASTEXITCODE -ne 0) { throw "V$HostMajor repeated-mode build failed." }
    Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $pluginDll -ExpectedSourceSha $gitHead
    python (Join-Path $repoRoot "scripts\preflight-direct-draw-repeated-mode.py")
    if ($LASTEXITCODE -ne 0) { throw "Repeated-mode static guard failed." }

    Copy-Item -LiteralPath $FixtureDwg -Destination $drawing
    if (Test-Path -LiteralPath $sidecar) { throw "Disposable repeated-mode sidecar already exists." }
    $env:QS3D_REPEAT_EVIDENCE_DIR = $runRoot
    $env:QS3D_REPEAT_DWG = $drawing
    $env:QS3D_REPEAT_NONCE = $script:nonce
    $env:QS3D_RUNTIME_RESULT = $phasePaths.runtime_session1

    $script:currentPhase = "author_undo_redo_save"
    $sessionOneLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'),
        "QS3DRUNTIMEPROBE",
        "QS3DREPEATARMSEQUENCE",
        "QS3DDRAWBEAMREPEAT", "0,0", "5000,0", "10000,0", "",
        "_.U", "_.REDO", "QS3DREPEATVERIFYREDO",
        "QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptOne, $sessionOneLines, [Text.Encoding]::ASCII)
    $argumentsOne = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptOne + '"'
    $processOne = Start-ExactCandidateHost -Executable $bricscadExe -Arguments $argumentsOne `
        -WorkingDirectory $runRoot -RuntimeIdentityPath $phasePaths.runtime_session1 `
        -PluginDll $pluginDll -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -OwnedProcesses $ownedProcesses -IsolateDemandLoad $isolateDemandLoad `
        -DemandLoadRegistryPath $demandLoadRegistryPath `
        -OriginalDemandLoadControls $demandLoadOriginalControls `
        -IsolatedDemandLoadControls $demandLoadIsolatedControls `
        -IsolationCount ([ref]$demandLoadIsolationCount) -DemandLoadRestored ([ref]$demandLoadRestored)
    Wait-ForFilesAndExit -Paths @($phasePaths.after, $phasePaths.undo, $phasePaths.redo) `
        -RuntimeIdentityPath $phasePaths.runtime_session1 -ExpectedAssembly $pluginDll `
        -Process $processOne `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.after) -Phase "after" -ExpectedSegments 2
    $acceptedSegments = 2
    $drawJigPreviewPassed = $true
    $enterTerminationPassed = $true
    Require-Marker -Marker (Read-Marker $phasePaths.undo) -Phase "undo" -ExpectedSegments 0
    $wholeCommandUndoPassed = $true
    Require-Marker -Marker (Read-Marker $phasePaths.redo) -Phase "redo" -ExpectedSegments 2
    $wholeCommandRedoPassed = $true
    $drawingPersisted = -not [string]::Equals(
        (Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash,
        $fixtureHash,
        [StringComparison]::OrdinalIgnoreCase)
    if (-not $drawingPersisted) { throw "Repeated-mode DWG changes were not persisted." }
    $sidecarPersisted = Test-Path -LiteralPath $sidecar -PathType Leaf
    if (-not $sidecarPersisted) { throw "Repeated-mode semantic sidecar was not persisted." }

    $script:currentPhase = "cold_reopen"
    $sessionTwoLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DRUNTIMEPROBE",
        "QS3DREPEATVERIFYCOLD", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptTwo, $sessionTwoLines, [Text.Encoding]::ASCII)
    $env:QS3D_RUNTIME_RESULT = $phasePaths.runtime_cold
    $argumentsTwo = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptTwo + '"'
    $processTwo = Start-ExactCandidateHost -Executable $bricscadExe -Arguments $argumentsTwo `
        -WorkingDirectory $runRoot -RuntimeIdentityPath $phasePaths.runtime_cold `
        -PluginDll $pluginDll -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -OwnedProcesses $ownedProcesses -IsolateDemandLoad $isolateDemandLoad `
        -DemandLoadRegistryPath $demandLoadRegistryPath `
        -OriginalDemandLoadControls $demandLoadOriginalControls `
        -IsolatedDemandLoadControls $demandLoadIsolatedControls `
        -IsolationCount ([ref]$demandLoadIsolationCount) -DemandLoadRestored ([ref]$demandLoadRestored)
    Wait-ForFilesAndExit -Paths @($phasePaths.cold_reopen) `
        -RuntimeIdentityPath $phasePaths.runtime_cold -ExpectedAssembly $pluginDll `
        -Process $processTwo `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.cold_reopen) -Phase "cold_reopen" -ExpectedSegments 2
    $saveColdReopenPassed = $true

    $script:currentPhase = "esc"
    Copy-Item -LiteralPath $FixtureDwg -Destination $escapeDrawing
    $env:QS3D_REPEAT_DWG = $escapeDrawing
    $escapeSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DRUNTIMEPROBE", "QS3DREPEATARMESC"
    )
    [IO.File]::WriteAllLines($scriptEscape, $escapeSessionLines, [Text.Encoding]::ASCII)
    $env:QS3D_RUNTIME_RESULT = $phasePaths.runtime_esc
    $escapeArguments = '"' + $escapeDrawing + '" /P "' + $Profile + '" /B "' + $scriptEscape + '"'
    $escapeProcess = Start-ExactCandidateHost -Executable $bricscadExe -Arguments $escapeArguments `
        -WorkingDirectory $runRoot -RuntimeIdentityPath $phasePaths.runtime_esc `
        -PluginDll $pluginDll -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -OwnedProcesses $ownedProcesses -IsolateDemandLoad $isolateDemandLoad `
        -DemandLoadRegistryPath $demandLoadRegistryPath `
        -OriginalDemandLoadControls $demandLoadOriginalControls `
        -IsolatedDemandLoadControls $demandLoadIsolatedControls `
        -IsolationCount ([ref]$demandLoadIsolationCount) -DemandLoadRestored ([ref]$demandLoadRestored) `
        -RequiresForegroundInput
    $escapeProcess = Wait-ForMarkerAndOwnedHost -Path $phasePaths.esc_ready `
        -RuntimeIdentityPath $phasePaths.runtime_esc -ExpectedAssembly $pluginDll `
        -Process $escapeProcess `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.esc_ready) -Phase "esc_ready" `
        -ExpectedSegments 1 -ExpectedStatus "READY"
    Send-ExactProcessEscape -Process $escapeProcess -ExpectedExecutable $bricscadExe
    Wait-ForFilesAndExit -Paths @($phasePaths.esc) `
        -RuntimeIdentityPath $phasePaths.runtime_esc -ExpectedAssembly $pluginDll `
        -Process $escapeProcess `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -RequestCloseAfterEvidence -CancelActiveCommandBeforeClose -TerminateOwnedProcessAfterEvidence
    Require-Marker -Marker (Read-Marker $phasePaths.esc) -Phase "esc" -ExpectedSegments 1
    $physicalEscPassed = $true

    $script:currentPhase = "planar_ucs"
    Copy-Item -LiteralPath $FixtureDwg -Destination $ucsDrawing
    $env:QS3D_REPEAT_DWG = $ucsDrawing
    $ucsSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4",
        "_.UCS", "_Z", "30",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DRUNTIMEPROBE",
        "QS3DDRAWBEAMREPEAT", "0,0", "5000,0", "10000,0", "",
        "_.UCS", "_W", "QS3DREPEATVERIFYUCS"
    )
    [IO.File]::WriteAllLines($scriptUcs, $ucsSessionLines, [Text.Encoding]::ASCII)
    $env:QS3D_RUNTIME_RESULT = $phasePaths.runtime_planar_ucs
    $ucsArguments = '"' + $ucsDrawing + '" /P "' + $Profile + '" /B "' + $scriptUcs + '"'
    $ucsProcess = Start-ExactCandidateHost -Executable $bricscadExe -Arguments $ucsArguments `
        -WorkingDirectory $runRoot -RuntimeIdentityPath $phasePaths.runtime_planar_ucs `
        -PluginDll $pluginDll -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -OwnedProcesses $ownedProcesses -IsolateDemandLoad $isolateDemandLoad `
        -DemandLoadRegistryPath $demandLoadRegistryPath `
        -OriginalDemandLoadControls $demandLoadOriginalControls `
        -IsolatedDemandLoadControls $demandLoadIsolatedControls `
        -IsolationCount ([ref]$demandLoadIsolationCount) -DemandLoadRestored ([ref]$demandLoadRestored)
    Wait-ForFilesAndExit -Paths @($phasePaths.planar_ucs) `
        -RuntimeIdentityPath $phasePaths.runtime_planar_ucs -ExpectedAssembly $pluginDll `
        -Process $ucsProcess `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -TerminateOwnedProcessAfterEvidence
    Require-Marker -Marker (Read-Marker $phasePaths.planar_ucs) -Phase "planar_ucs" -ExpectedSegments 2
    if (-not [string]::Equals(
        (Get-FileHash -LiteralPath $ucsDrawing -Algorithm SHA256).Hash,
        $fixtureHash,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Repeated-mode planar-UCS disposable drawing bytes changed."
    }
    if (Test-Path -LiteralPath $ucsSidecar -PathType Leaf) {
        throw "Repeated-mode planar-UCS probe unexpectedly persisted a semantic sidecar."
    }
    $planarUcsPassed = $true

    $script:currentPhase = "document_switch"
    Copy-Item -LiteralPath $FixtureDwg -Destination $switchDrawingA
    Copy-Item -LiteralPath $FixtureDwg -Destination $switchDrawingB
    $env:QS3D_REPEAT_DWG = $switchDrawingA
    $env:QS3D_REPEAT_SECOND_DWG = $switchDrawingB
    $switchSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DRUNTIMEPROBE",
        "_.OPEN", ('"' + $switchDrawingB + '"'), "QS3DREPEATARMSWITCH"
    )
    [IO.File]::WriteAllLines($scriptSwitch, $switchSessionLines, [Text.Encoding]::ASCII)
    $env:QS3D_RUNTIME_RESULT = $phasePaths.runtime_document_switch
    $switchArguments = '"' + $switchDrawingA + '" /P "' + $Profile + '" /B "' + $scriptSwitch + '"'
    $switchProcess = Start-ExactCandidateHost -Executable $bricscadExe -Arguments $switchArguments `
        -WorkingDirectory $runRoot -RuntimeIdentityPath $phasePaths.runtime_document_switch `
        -PluginDll $pluginDll -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -OwnedProcesses $ownedProcesses -IsolateDemandLoad $isolateDemandLoad `
        -DemandLoadRegistryPath $demandLoadRegistryPath `
        -OriginalDemandLoadControls $demandLoadOriginalControls `
        -IsolatedDemandLoadControls $demandLoadIsolatedControls `
        -IsolationCount ([ref]$demandLoadIsolationCount) -DemandLoadRestored ([ref]$demandLoadRestored)
    $switchProcess = Wait-ForMarkerAndOwnedHost -Path $phasePaths.document_switch_ready `
        -RuntimeIdentityPath $phasePaths.runtime_document_switch -ExpectedAssembly $pluginDll `
        -Process $switchProcess `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.document_switch_ready) `
        -Phase "document_switch_ready" -ExpectedSegments 1 -ExpectedStatus "READY"
    Wait-ForFilesAndExit -Paths @($phasePaths.document_switch) `
        -RuntimeIdentityPath $phasePaths.runtime_document_switch -ExpectedAssembly $pluginDll `
        -Process $switchProcess `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)) `
        -TerminateOwnedProcessAfterEvidence
    Require-Marker -Marker (Read-Marker $phasePaths.document_switch) `
        -Phase "document_switch" -ExpectedSegments 1
    foreach ($switchDrawing in @($switchDrawingA, $switchDrawingB)) {
        if (-not [string]::Equals(
            (Get-FileHash -LiteralPath $switchDrawing -Algorithm SHA256).Hash,
            $fixtureHash,
            [StringComparison]::OrdinalIgnoreCase)) {
            throw "Repeated-mode document-switch disposable drawing bytes changed."
        }
    }
    $documentSwitchPassed = $true
    $exactRuntimeIdentityPassed = $true
    $script:currentPhase = "complete"
}
catch {
    $qualificationError = $_.Exception
    if ([string]::Equals($script:failurePhase, "NONE", [StringComparison]::Ordinal)) {
        $script:failurePhase = $script:currentPhase
        $script:failureCode = "RUNNER_" + $_.Exception.GetType().Name.ToUpperInvariant()
    }
}
finally {
    try {
        Stop-OwnedHosts -Processes @($ownedProcesses) -ExpectedExecutable $bricscadExe
        if (-not (Wait-OwnedHostsExited -Processes @($ownedProcesses) -TimeoutSeconds 15)) {
            throw "Repeated-mode BricsCAD process cleanup is incomplete."
        }
        $processCleanup = $true

        foreach ($path in @(
            $scriptOne, $scriptTwo, $scriptEscape, $scriptUcs, $scriptSwitch,
            $phasePaths.runtime_session1, $phasePaths.runtime_cold, $phasePaths.runtime_esc,
            $phasePaths.runtime_planar_ucs, $phasePaths.runtime_document_switch,
            $phasePaths.after, $phasePaths.undo, $phasePaths.redo, $phasePaths.cold_reopen,
            $phasePaths.esc_ready, $phasePaths.esc,
            $phasePaths.planar_ucs,
            $phasePaths.document_switch_ready, $phasePaths.document_switch,
            $sidecar, ($sidecar + ".bak"), ($sidecar + ".lock"),
            $escapeSidecar, ($escapeSidecar + ".bak"), ($escapeSidecar + ".lock"),
            $ucsSidecar, ($ucsSidecar + ".bak"), ($ucsSidecar + ".lock"),
            $switchSidecarA, ($switchSidecarA + ".bak"), ($switchSidecarA + ".lock"),
            $switchSidecarB, ($switchSidecarB + ".bak"), ($switchSidecarB + ".lock"),
            $drawing, [IO.Path]::ChangeExtension($drawing, ".bak"),
            [IO.Path]::ChangeExtension($drawing, ".dwl"), [IO.Path]::ChangeExtension($drawing, ".dwl2"),
            $escapeDrawing, [IO.Path]::ChangeExtension($escapeDrawing, ".bak"),
            [IO.Path]::ChangeExtension($escapeDrawing, ".dwl"), [IO.Path]::ChangeExtension($escapeDrawing, ".dwl2"),
            $ucsDrawing, [IO.Path]::ChangeExtension($ucsDrawing, ".bak"),
            [IO.Path]::ChangeExtension($ucsDrawing, ".dwl"), [IO.Path]::ChangeExtension($ucsDrawing, ".dwl2"),
            $switchDrawingA, [IO.Path]::ChangeExtension($switchDrawingA, ".bak"),
            [IO.Path]::ChangeExtension($switchDrawingA, ".dwl"), [IO.Path]::ChangeExtension($switchDrawingA, ".dwl2"),
            $switchDrawingB, [IO.Path]::ChangeExtension($switchDrawingB, ".bak"),
            [IO.Path]::ChangeExtension($switchDrawingB, ".dwl"), [IO.Path]::ChangeExtension($switchDrawingB, ".dwl2")
        )) {
            $contained = Require-ContainedPath -Path $path -Root $runRoot -Label "private cleanup target"
            Remove-Item -LiteralPath $contained -Force -ErrorAction SilentlyContinue
        }
        if (@(Get-ChildItem -LiteralPath $runRoot -Force).Count -ne 0) {
            throw "Repeated-mode private run root retained files."
        }
        $cleanupDeadline = [DateTime]::UtcNow.AddSeconds(5)
        while (Test-Path -LiteralPath $runRoot -PathType Container) {
            try {
                Remove-Item -LiteralPath $runRoot -Force -ErrorAction Stop
            }
            catch {
                if ([DateTime]::UtcNow -ge $cleanupDeadline) { throw }
                Start-Sleep -Milliseconds 100
            }
        }
        $privateCleanup = -not (Test-Path -LiteralPath $runRoot)
        if (-not [string]::Equals(
            (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash,
            $fixtureHash,
            [StringComparison]::OrdinalIgnoreCase)) {
            throw "Repository fixture changed during repeated-mode qualification."
        }
    }
    catch {
        $cleanupError = $_.Exception
    }
    finally {
        try {
            if ($isolateDemandLoad) {
                Restore-Qs3dDemandLoadControls -RegistryPath $demandLoadRegistryPath `
                    -OriginalValue $demandLoadOriginalControls -IsolatedValue $demandLoadIsolatedControls
                $demandLoadRestored = $true
            }
        }
        catch {
            $demandLoadRestored = $false
            if ($null -eq $cleanupError) { $cleanupError = $_.Exception }
        }
        [Environment]::SetEnvironmentVariable("BRICSCAD_V$($HostMajor)_DIR", $oldHostDir, "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $oldDotNetRoot, "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT_X64", $oldDotNetRootX64, "Process")
        $env:QS3D_RUNTIME_RESULT = $oldRuntimeResult
        $env:QS3D_REPEAT_EVIDENCE_DIR = $oldEvidence
        $env:QS3D_REPEAT_DWG = $oldDrawing
        $env:QS3D_REPEAT_NONCE = $oldNonce
        $env:QS3D_REPEAT_SECOND_DWG = $oldSecondDrawing
    }
}

$metadata = [ordered]@{
    status = if ($null -eq $qualificationError -and $null -eq $cleanupError) { "PASS" } else { "FAIL" }
    qualification_boundary = "LOCAL_008_P03_PRODUCTION_REPEATED_DIRECT_DRAW"
    git_sha = $gitHead
    host_major = $HostMajor
    bricscad_file_version = $hostVersion.FileVersion
    plugin_sha256 = if (Test-Path -LiteralPath $pluginDll) { (Get-FileHash -LiteralPath $pluginDll -Algorithm SHA256).Hash.ToUpperInvariant() } else { "MISSING" }
    fixture_sha256 = $fixtureHash
    accepted_segments = $acceptedSegments
    exact_loaded_candidate_every_session = $exactRuntimeIdentityPassed
    startup_demandload_isolation_count = $demandLoadIsolationCount
    startup_demandload_restored = $demandLoadRestored
    drawjig_preview = $drawJigPreviewPassed
    enter_termination = $enterTerminationPassed
    exact_process_physical_esc_termination = $physicalEscPassed
    supported_planar_ucs = $planarUcsPassed
    native_document_switch_isolation = $documentSwitchPassed
    whole_command_undo = $wholeCommandUndoPassed
    whole_command_redo = $wholeCommandRedoPassed
    save_cold_reopen = $saveColdReopenPassed
    drawing_persisted = $drawingPersisted
    sidecar_persisted = $sidecarPersisted
    process_cleanup_verified = $processCleanup
    private_cleanup_verified = $privateCleanup
    owned_process_terminations_after_complete_evidence = $script:ownedEvidenceTerminationCount
    started_at = $startedAt.ToString("O")
    completed_at = [DateTime]::UtcNow.ToString("O")
    failure_phase = $script:failurePhase
    failure_code = $script:failureCode
    qualification_error_class = if ($null -ne $qualificationError) { $qualificationError.GetType().Name } else { "NONE" }
    cleanup_error_class = if ($null -ne $cleanupError) { $cleanupError.GetType().Name } else { "NONE" }
    error_class = if ($null -ne $qualificationError) { $qualificationError.GetType().Name } elseif ($null -ne $cleanupError) { $cleanupError.GetType().Name } else { "NONE" }
}
$metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $qualificationError) { throw $qualificationError }
if ($null -ne $cleanupError) { throw $cleanupError }

Write-Host "QS3D BricsCAD V$HostMajor production repeated Direct Draw runtime PASS"
Write-Host "Exact SHA: $gitHead"
Write-Host "Metadata: $metadataPath"
