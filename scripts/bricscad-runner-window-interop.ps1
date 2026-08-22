if (-not ("Qs3dBricsCadRunnerWindowInterop" -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class Qs3dBricsCadRunnerWindowInterop
{
    private const uint WmClose = 0x0010;
    private const uint WmCommand = 0x0111;
    private const int IdNo = 7;

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder text, int capacity);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    public static int CloseProxyInformationDialogs(int processId)
    {
        if (processId <= 0) throw new ArgumentOutOfRangeException("processId");
        var closed = 0;
        EnumWindows((window, state) =>
        {
            uint ownerProcessId;
            GetWindowThreadProcessId(window, out ownerProcessId);
            if (ownerProcessId != (uint)processId || !IsWindowVisible(window)) return true;

            var title = new StringBuilder(256);
            var className = new StringBuilder(64);
            GetWindowText(window, title, title.Capacity);
            GetClassName(window, className, className.Capacity);
            if (string.Equals(title.ToString(), "Proxy Information", StringComparison.Ordinal) &&
                string.Equals(className.ToString(), "#32770", StringComparison.Ordinal) &&
                PostMessage(window, WmClose, IntPtr.Zero, IntPtr.Zero))
            {
                closed++;
            }
            return true;
        }, IntPtr.Zero);
        return closed;
    }

    public static int DiscardUnsavedProjectChangesDialogs(int processId)
    {
        if (processId <= 0) throw new ArgumentOutOfRangeException("processId");
        var discarded = 0;
        EnumWindows((window, state) =>
        {
            uint ownerProcessId;
            GetWindowThreadProcessId(window, out ownerProcessId);
            if (ownerProcessId != (uint)processId || !IsWindowVisible(window)) return true;

            var title = new StringBuilder(256);
            var className = new StringBuilder(64);
            GetWindowText(window, title, title.Capacity);
            GetClassName(window, className, className.Capacity);
            if (string.Equals(title.ToString(), "QS3D \u2014 Unsaved project changes", StringComparison.Ordinal) &&
                string.Equals(className.ToString(), "#32770", StringComparison.Ordinal) &&
                PostMessage(window, WmCommand, (IntPtr)IdNo, IntPtr.Zero))
            {
                discarded++;
            }
            return true;
        }, IntPtr.Zero);
        return discarded;
    }
}
"@
}

function Close-Qs3dProxyInformationDialog {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return 0 }
    try {
        $Process.Refresh()
        if ($Process.HasExited) { return 0 }
        return [Qs3dBricsCadRunnerWindowInterop]::CloseProxyInformationDialogs($Process.Id)
    }
    catch [InvalidOperationException] {
        return 0
    }
}

function Close-Qs3dUnsavedProjectChangesDialog {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return 0 }
    try {
        $Process.Refresh()
        if ($Process.HasExited) { return 0 }
        return [Qs3dBricsCadRunnerWindowInterop]::DiscardUnsavedProjectChangesDialogs($Process.Id)
    }
    catch [InvalidOperationException] {
        return 0
    }
}

function Get-Qs3dExactBricsCadProcesses {
    param([Parameter(Mandatory = $true)][string]$ExpectedExecutable)
    $expectedPath = [IO.Path]::GetFullPath($ExpectedExecutable)
    $matches = @()
    foreach ($record in @(Get-CimInstance Win32_Process -Filter "Name = 'bricscad.exe'")) {
        if ([string]::IsNullOrWhiteSpace([string]$record.ExecutablePath)) { continue }
        $actualPath = [IO.Path]::GetFullPath([string]$record.ExecutablePath)
        if (-not [string]::Equals($actualPath, $expectedPath, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $process = Get-Process -Id ([int]$record.ProcessId) -ErrorAction SilentlyContinue
        if ($null -ne $process) { $matches += $process }
    }
    return $matches
}

function Wait-Qs3dNoExactBricsCadProcesses {
    param(
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [ValidateRange(1, 60)][int]$TimeoutSeconds = 15
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (@(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $ExpectedExecutable).Count -eq 0) { return $true }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return @(Get-Qs3dExactBricsCadProcesses -ExpectedExecutable $ExpectedExecutable).Count -eq 0
}

function Read-Qs3dSingleProjectValue {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Element
    )
    try { [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw }
    catch { throw "Could not read QS3D project identity: $ProjectPath" }
    $values = @($project.Project.PropertyGroup | ForEach-Object {
        $property = $_.PSObject.Properties[$Element]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            ([string]$property.Value).Trim()
        }
    } | Select-Object -Unique)
    if ($values.Count -ne 1) { throw "QS3D project must declare exactly one $Element identity: $ProjectPath" }
    return [string]$values[0]
}

function Assert-Qs3dExactSourceIdentity {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$PluginDll,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$ExpectedSourceSha
    )
    $repoPath = [IO.Path]::GetFullPath($RepoRoot)
    $pluginPath = [IO.Path]::GetFullPath($PluginDll)
    $corePath = Join-Path (Split-Path -Parent $pluginPath) 'QS3D.Core.dll'
    $pluginPdb = [IO.Path]::ChangeExtension($pluginPath, '.pdb')
    $corePdb = [IO.Path]::ChangeExtension($corePath, '.pdb')
    $pluginProject = Join-Path $repoPath 'src\QS3D.BricsCAD.V25\QS3D.BricsCAD.V25.csproj'
    $coreProject = Join-Path $repoPath 'src\QS3D.Core\QS3D.Core.csproj'
    foreach ($required in @($pluginPath, $corePath, $pluginPdb, $corePdb, $pluginProject, $coreProject)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Exact-source identity input is missing: $required" }
    }

    $pluginVersion = Read-Qs3dSingleProjectValue -ProjectPath $pluginProject -Element 'InformationalVersion'
    $coreVersion = Read-Qs3dSingleProjectValue -ProjectPath $coreProject -Element 'InformationalVersion'
    if (-not [string]::Equals($pluginVersion, $coreVersion, [StringComparison]::Ordinal)) {
        throw 'V25 plugin and Core public ProductVersion declarations disagree.'
    }
    foreach ($assemblyPath in @($pluginPath, $corePath)) {
        $actualVersion = ([string](Get-Item -LiteralPath $assemblyPath).VersionInfo.ProductVersion).Trim()
        if (-not [string]::Equals($actualVersion, $pluginVersion, [StringComparison]::Ordinal)) {
            throw "Assembly public ProductVersion does not match the declared QS3D product identity: $assemblyPath"
        }
    }

    $sourceLinkPrefix = 'https://raw.githubusercontent.com/trinhtanphat/QS3D-BricsCAD/' + $ExpectedSourceSha.ToLowerInvariant() + '/'
    foreach ($pdbPath in @($pluginPdb, $corePdb)) {
        $pdbText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($pdbPath))
        if ($pdbText.IndexOf($sourceLinkPrefix, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "PDB SourceLink does not bind the binary to the exact clean Git SHA: $pdbPath"
        }
    }
}
