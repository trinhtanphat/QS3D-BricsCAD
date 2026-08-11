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

        if ($typeName -eq 'QS3D.BricsCAD.V25.UI.WorkspacePanel') {
            $wideScroller = [System.Windows.Media.VisualTreeHelper]::GetChild($control, 0)
            if (-not ($wideScroller -is [System.Windows.Controls.ScrollViewer])) {
                throw 'WorkspacePanel template root must be the guarded horizontal ScrollViewer.'
            }
            if ($wideScroller.ComputedHorizontalScrollBarVisibility -ne [System.Windows.Visibility]::Collapsed) {
                throw 'WorkspacePanel must not show horizontal overflow at 1200x800.'
            }

            $compact = [Activator]::CreateInstance($type)
            $dataContextMarker = [object]::new()
            $compact.DataContext = $dataContextMarker
            $compact.Measure([System.Windows.Size]::new(460d, 420d))
            $compact.Arrange([System.Windows.Rect]::new(0d, 0d, 460d, 420d))
            $compact.UpdateLayout()
            $compactScroller = [System.Windows.Media.VisualTreeHelper]::GetChild($compact, 0)
            if (-not ($compactScroller -is [System.Windows.Controls.ScrollViewer])) {
                throw 'Compact WorkspacePanel template root must remain the guarded horizontal ScrollViewer.'
            }
            if ([Math]::Abs($compact.ActualWidth - 460d) -gt 0.1d -or [Math]::Abs($compact.ActualHeight - 420d) -gt 0.1d) {
                throw "WorkspacePanel did not honor compact 460x420 arrange: $($compact.ActualWidth)x$($compact.ActualHeight)."
            }
            if ($compactScroller.HorizontalScrollBarVisibility -ne [System.Windows.Controls.ScrollBarVisibility]::Auto -or
                $compactScroller.ComputedHorizontalScrollBarVisibility -ne [System.Windows.Visibility]::Visible) {
                throw 'Compact WorkspacePanel must expose automatic horizontal overflow.'
            }
            if ($compactScroller.ExtentWidth -lt 559.5d -or $compactScroller.ViewportWidth -ge $compactScroller.ExtentWidth) {
                throw "Compact WorkspacePanel must retain scrollable 560-DIP content: extent=$($compactScroller.ExtentWidth), viewport=$($compactScroller.ViewportWidth)."
            }
            if ($compactScroller.VerticalScrollBarVisibility -ne [System.Windows.Controls.ScrollBarVisibility]::Disabled -or
                $compactScroller.ComputedVerticalScrollBarVisibility -eq [System.Windows.Visibility]::Visible -or
                $compactScroller.ExtentHeight -gt ($compactScroller.ViewportHeight + 0.1d)) {
                throw "WorkspacePanel outer viewport must not add vertical overflow: extent=$($compactScroller.ExtentHeight), viewport=$($compactScroller.ViewportHeight)."
            }
            if (-not ($compact.Content -is [System.Windows.Controls.Grid]) -or
                -not [object]::ReferenceEquals($compact.DataContext, $dataContextMarker) -or
                -not [object]::ReferenceEquals($compact.Content.DataContext, $dataContextMarker)) {
                throw 'WorkspacePanel custom template must preserve its original Grid content and inherited DataContext.'
            }
            foreach ($name in @('FamilySearch', 'PropertySearch')) {
                $focusTarget = $compact.FindName($name)
                if (-not ($focusTarget -is [System.Windows.Controls.Control]) -or
                    -not $focusTarget.Focusable -or -not $focusTarget.IsTabStop) {
                    throw "WorkspacePanel custom template must preserve keyboard focus for $name."
                }
            }
            Write-Host "PASS: WorkspacePanel honors compact 460x420 overflow without vertical clipping and preserves content, DataContext and keyboard focus targets."
        }
    }
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
