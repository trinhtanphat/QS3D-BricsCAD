param(
    [Parameter(Mandatory = $true)][ValidateSet(25, 26)][int]$HostMajor,
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [string]$Profile = "Default",
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
        if (-not $Marker.ContainsKey($entry.Key)) { throw "Repeated-mode $Phase marker is missing '$($entry.Key)'." }
        if (-not [string]::Equals([string]$Marker[$entry.Key], [string]$entry.Value, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Repeated-mode $Phase marker '$($entry.Key)' expected '$($entry.Value)' but was '$($Marker[$entry.Key])'."
        }
    }
}

function Wait-ForMarkerAndOwnedHost {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    while ([DateTime]::UtcNow -lt $Deadline) {
        $processes = @(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $ExpectedExecutable)
        foreach ($process in $processes) {
            Close-Qs3dProxyInformationDialog -Process $process | Out-Null
            Close-Qs3dUnsavedProjectChangesDialog -Process $process | Out-Null
        }
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            if ($processes.Count -ne 1) {
                throw "Repeated-mode ESC ready marker requires exactly one owned BricsCAD process."
            }
            return $processes[0]
        }
        if ($processes.Count -eq 0) {
            throw "BricsCAD exited before the repeated-mode ESC ready marker."
        }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out waiting for the repeated-mode ESC ready marker."
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

    [Qs3dExactEscapeInput]::ShowWindowAsync($window, 9) | Out-Null
    $foregroundDeadline = [DateTime]::UtcNow.AddSeconds(10)
    $foregroundMatches = $false
    while (-not $foregroundMatches -and [DateTime]::UtcNow -lt $foregroundDeadline) {
        [Qs3dExactEscapeInput]::SetForegroundWindow($window) | Out-Null
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

function Send-ExactProcessCtrlTab {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable
    )
    Bind-ExactProcessForeground -Process $Process -ExpectedExecutable $ExpectedExecutable

    [Qs3dExactEscapeInput]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)
    [Qs3dExactEscapeInput]::keybd_event(0x09, 0, 0, [UIntPtr]::Zero)
    [Qs3dExactEscapeInput]::keybd_event(0x09, 0, 2, [UIntPtr]::Zero)
    [Qs3dExactEscapeInput]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero)
}

function Stop-OwnedHosts {
    param([Parameter(Mandatory = $true)][string]$ExpectedExecutable)
    foreach ($process in @(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $ExpectedExecutable)) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit(10000) | Out-Null
            }
        }
        catch { }
    }
}

function Wait-ForFilesAndExit {
    param(
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][DateTime]$Deadline
    )
    $allObserved = $false
    while ([DateTime]::UtcNow -lt $Deadline) {
        $processes = @(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $ExpectedExecutable)
        foreach ($process in $processes) {
            Close-Qs3dProxyInformationDialog -Process $process | Out-Null
            Close-Qs3dUnsavedProjectChangesDialog -Process $process | Out-Null
        }
        $allObserved = @($Paths | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -eq 0
        if ($allObserved -and $processes.Count -eq 0) { return }
        if (-not $allObserved -and $processes.Count -eq 0) {
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

if ([string]::IsNullOrWhiteSpace($DotNetExe)) {
    $gitCommonDir = (& git -C $repoRoot rev-parse --path-format=absolute --git-common-dir).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitCommonDir)) {
        throw "Cannot resolve the shared Git directory for dotnet discovery."
    }
    $mainWorktreeRoot = Split-Path -Parent ([IO.Path]::GetFullPath($gitCommonDir))
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
    $ArtifactDir = Join-Path $repoRoot "artifacts\local-direct-draw-repeat\$gitHead-v$HostMajor"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
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

$oldHostDir = [Environment]::GetEnvironmentVariable("BRICSCAD_V$($HostMajor)_DIR", "Process")
$oldDotNetRoot = [Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")
$oldDotNetRootX64 = [Environment]::GetEnvironmentVariable("DOTNET_ROOT_X64", "Process")
$oldEvidence = $env:QS3D_REPEAT_EVIDENCE_DIR
$oldDrawing = $env:QS3D_REPEAT_DWG
$oldNonce = $env:QS3D_REPEAT_NONCE
$oldSecondDrawing = $env:QS3D_REPEAT_SECOND_DWG
$qualificationError = $null
$cleanupError = $null
$fixtureHash = (Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256).Hash.ToUpperInvariant()
$startedAt = [DateTime]::UtcNow
$drawingPersisted = $false
$sidecarPersisted = $false
$processCleanup = $false
$privateCleanup = $false
$physicalEscPassed = $false
$planarUcsPassed = $false
$documentSwitchPassed = $false

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

    $sessionOneLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'),
        "QS3DDRAWBEAMREPEAT", "0,0", "5000,0", "10000,0", "",
        "QS3DREPEATVERIFYAFTER",
        "_.U", "QS3DREPEATVERIFYUNDO",
        "_.REDO", "QS3DREPEATVERIFYREDO",
        "QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptOne, $sessionOneLines, [Text.Encoding]::ASCII)
    $argumentsOne = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptOne + '"'
    Start-Process -FilePath $bricscadExe -ArgumentList $argumentsOne -WorkingDirectory $runRoot -WindowStyle Hidden | Out-Null
    Wait-ForFilesAndExit -Paths @($phasePaths.after, $phasePaths.undo, $phasePaths.redo) `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.after) -Phase "after" -ExpectedSegments 2
    Require-Marker -Marker (Read-Marker $phasePaths.undo) -Phase "undo" -ExpectedSegments 0
    Require-Marker -Marker (Read-Marker $phasePaths.redo) -Phase "redo" -ExpectedSegments 2
    $drawingPersisted = -not [string]::Equals(
        (Get-FileHash -LiteralPath $drawing -Algorithm SHA256).Hash,
        $fixtureHash,
        [StringComparison]::OrdinalIgnoreCase)
    if (-not $drawingPersisted) { throw "Repeated-mode DWG changes were not persisted." }
    $sidecarPersisted = Test-Path -LiteralPath $sidecar -PathType Leaf
    if (-not $sidecarPersisted) { throw "Repeated-mode semantic sidecar was not persisted." }

    $sessionTwoLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DREPEATVERIFYCOLD", "_.QUIT", "_Y"
    )
    [IO.File]::WriteAllLines($scriptTwo, $sessionTwoLines, [Text.Encoding]::ASCII)
    $argumentsTwo = '"' + $drawing + '" /P "' + $Profile + '" /B "' + $scriptTwo + '"'
    Start-Process -FilePath $bricscadExe -ArgumentList $argumentsTwo -WorkingDirectory $runRoot -WindowStyle Hidden | Out-Null
    Wait-ForFilesAndExit -Paths @($phasePaths.cold_reopen) `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.cold_reopen) -Phase "cold_reopen" -ExpectedSegments 2

    Copy-Item -LiteralPath $FixtureDwg -Destination $escapeDrawing
    $env:QS3D_REPEAT_DWG = $escapeDrawing
    $escapeSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'), "QS3DREPEATARMESC"
    )
    [IO.File]::WriteAllLines($scriptEscape, $escapeSessionLines, [Text.Encoding]::ASCII)
    $escapeArguments = '"' + $escapeDrawing + '" /P "' + $Profile + '" /B "' + $scriptEscape + '"'
    Start-Process -FilePath $bricscadExe -ArgumentList $escapeArguments -WorkingDirectory $runRoot -WindowStyle Hidden | Out-Null
    $escapeProcess = Wait-ForMarkerAndOwnedHost -Path $phasePaths.esc_ready `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.esc_ready) -Phase "esc_ready" `
        -ExpectedSegments 1 -ExpectedStatus "READY"
    Send-ExactProcessEscape -Process $escapeProcess -ExpectedExecutable $bricscadExe
    Wait-ForFilesAndExit -Paths @($phasePaths.esc) `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.esc) -Phase "esc" -ExpectedSegments 1
    $physicalEscPassed = $true

    Copy-Item -LiteralPath $FixtureDwg -Destination $ucsDrawing
    $env:QS3D_REPEAT_DWG = $ucsDrawing
    $ucsSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4",
        "_.UCS", "_Z", "30",
        "NETLOAD", ('"' + $pluginDll + '"'),
        "QS3DDRAWBEAMREPEAT", "0,0", "5000,0", "10000,0", "",
        "_.UCS", "_W", "QS3DREPEATVERIFYUCS", "_.QUIT", "_N"
    )
    [IO.File]::WriteAllLines($scriptUcs, $ucsSessionLines, [Text.Encoding]::ASCII)
    $ucsArguments = '"' + $ucsDrawing + '" /P "' + $Profile + '" /B "' + $scriptUcs + '"'
    Start-Process -FilePath $bricscadExe -ArgumentList $ucsArguments -WorkingDirectory $runRoot -WindowStyle Hidden | Out-Null
    Wait-ForFilesAndExit -Paths @($phasePaths.planar_ucs) `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.planar_ucs) -Phase "planar_ucs" -ExpectedSegments 2
    if (-not [string]::Equals(
        (Get-FileHash -LiteralPath $ucsDrawing -Algorithm SHA256).Hash,
        $fixtureHash,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Repeated-mode planar-UCS disposable drawing bytes changed."
    }
    $planarUcsPassed = $true

    Copy-Item -LiteralPath $FixtureDwg -Destination $switchDrawingA
    Copy-Item -LiteralPath $FixtureDwg -Destination $switchDrawingB
    $env:QS3D_REPEAT_DWG = $switchDrawingA
    $env:QS3D_REPEAT_SECOND_DWG = $switchDrawingB
    $switchSessionLines = @(
        "FILEDIA", "0", "CMDECHO", "1", "TILEMODE", "1", "INSUNITS", "4", "UCS", "W",
        "NETLOAD", ('"' + $pluginDll + '"'),
        "_.OPEN", ('"' + $switchDrawingB + '"'), "QS3DREPEATARMSWITCH"
    )
    [IO.File]::WriteAllLines($scriptSwitch, $switchSessionLines, [Text.Encoding]::ASCII)
    $switchArguments = '"' + $switchDrawingA + '" /P "' + $Profile + '" /B "' + $scriptSwitch + '"'
    Start-Process -FilePath $bricscadExe -ArgumentList $switchArguments -WorkingDirectory $runRoot -WindowStyle Hidden | Out-Null
    $switchProcess = Wait-ForMarkerAndOwnedHost -Path $phasePaths.document_switch_ready `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
    Require-Marker -Marker (Read-Marker $phasePaths.document_switch_ready) `
        -Phase "document_switch_ready" -ExpectedSegments 1 -ExpectedStatus "READY"
    Send-ExactProcessCtrlTab -Process $switchProcess -ExpectedExecutable $bricscadExe
    Wait-ForFilesAndExit -Paths @($phasePaths.document_switch) `
        -ExpectedExecutable $bricscadExe -Deadline ([DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds))
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
}
catch {
    $qualificationError = $_.Exception
}
finally {
    try {
        Stop-OwnedHosts -ExpectedExecutable $bricscadExe
        if (-not (Wait-Qs3dNoExactBricsCadProcesses -ExpectedExecutable $bricscadExe -TimeoutSeconds 15)) {
            throw "Repeated-mode BricsCAD process cleanup is incomplete."
        }
        $processCleanup = $true

        foreach ($path in @(
            $scriptOne, $scriptTwo, $scriptEscape, $scriptUcs, $scriptSwitch,
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
        Remove-Item -LiteralPath $runRoot -Force
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
        [Environment]::SetEnvironmentVariable("BRICSCAD_V$($HostMajor)_DIR", $oldHostDir, "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $oldDotNetRoot, "Process")
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT_X64", $oldDotNetRootX64, "Process")
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
    accepted_segments = 2
    drawjig_preview = $true
    enter_termination = $true
    exact_process_physical_esc_termination = $physicalEscPassed
    supported_planar_ucs = $planarUcsPassed
    exact_process_document_switch_isolation = $documentSwitchPassed
    whole_command_undo = $true
    whole_command_redo = $true
    save_cold_reopen = $true
    drawing_persisted = $drawingPersisted
    sidecar_persisted = $sidecarPersisted
    process_cleanup_verified = $processCleanup
    private_cleanup_verified = $privateCleanup
    started_at = $startedAt.ToString("O")
    completed_at = [DateTime]::UtcNow.ToString("O")
    error_class = if ($null -ne $qualificationError) { $qualificationError.GetType().Name } elseif ($null -ne $cleanupError) { $cleanupError.GetType().Name } else { "NONE" }
}
$metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

if ($null -ne $qualificationError) { throw $qualificationError }
if ($null -ne $cleanupError) { throw $cleanupError }

Write-Host "QS3D BricsCAD V$HostMajor production repeated Direct Draw runtime PASS"
Write-Host "Exact SHA: $gitHead"
Write-Host "Metadata: $metadataPath"
