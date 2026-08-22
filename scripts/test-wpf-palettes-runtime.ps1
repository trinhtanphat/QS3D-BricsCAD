[CmdletBinding()]
param(
    [string]$PluginPath,
    [string]$BricscadDirectory = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $root 'src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll'
}
$plugin = (Resolve-Path -LiteralPath $PluginPath).Path
$searchDirectories = @((Split-Path -Parent $plugin), (Resolve-Path -LiteralPath $BricscadDirectory).Path)
$resolver = [System.ResolveEventHandler]{
    param($sender, $eventArgs)
    $fileName = ([System.Reflection.AssemblyName]$eventArgs.Name).Name + '.dll'
    foreach ($directory in $searchDirectories) {
        $candidate = Join-Path $directory $fileName
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return [System.Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $assembly = [System.Reflection.Assembly]::LoadFrom($plugin)
    foreach ($typeName in @('QS3D.BricsCAD.V25.UI.WorkspacePanel', 'QS3D.BricsCAD.V25.UI.RightPanel')) {
        $type = $assembly.GetType($typeName, $true)
        $control = [Activator]::CreateInstance($type)
        if (-not ($control -is [System.Windows.UIElement])) { throw "$typeName is not a WPF UIElement." }
        $control.Measure([System.Windows.Size]::new(1200d, 800d))
        $control.Arrange([System.Windows.Rect]::new(0d, 0d, 1200d, 800d))
        $control.UpdateLayout()
        Write-Host "PASS: $typeName initialized and completed WPF layout."
    }
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
