[CmdletBinding()]
param(
    [string]$ThemePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml

if ([string]::IsNullOrWhiteSpace($ThemePath)) {
    $ThemePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\QS3D.BricsCAD.V25\UI\Theme.xaml'
}

$resolved = (Resolve-Path -LiteralPath $ThemePath).Path
$reader = [System.Xml.XmlReader]::Create($resolved)
try { $theme = [System.Windows.Markup.XamlReader]::Load($reader) }
finally { $reader.Dispose() }
if (-not ($theme -is [System.Windows.ResourceDictionary])) { throw 'Theme.xaml did not load as a ResourceDictionary.' }

$controls = @(
    [System.Windows.Controls.Button]::new(),
    [System.Windows.Controls.ComboBox]::new(),
    [System.Windows.Controls.ComboBoxItem]::new(),
    [System.Windows.Controls.TextBox]::new(),
    [System.Windows.Controls.GridViewColumnHeader]::new(),
    [System.Windows.Controls.DataGrid]::new(),
    [System.Windows.Controls.DataGridRow]::new(),
    [System.Windows.Controls.DataGridCell]::new(),
    [System.Windows.Controls.Primitives.DataGridColumnHeader]::new(),
    [System.Windows.Controls.ToolTip]::new()
)

foreach ($control in $controls) {
    $style = $theme[$control.GetType()]
    if ($style) { $control.Style = $style }
    $control.ApplyTemplate() | Out-Null
    $backgroundProperty = $control.GetType().GetProperty('Background')
    if ($backgroundProperty) {
        $background = $backgroundProperty.GetValue($control, $null)
        if ($background -ne $null -and -not ($background -is [System.Windows.Media.Brush])) {
            throw "$($control.GetType().Name).Background resolved to $($background.GetType().FullName), expected Brush."
        }
    }
}

$card = [System.Windows.Controls.Border]::new()
$card.Style = $theme['Card']
$card.ApplyTemplate() | Out-Null
if (-not ($card.Background -is [System.Windows.Media.Brush])) { throw 'Card.Background did not resolve to Brush.' }

Write-Host "PASS: Theme.xaml loaded and styled $($controls.Count + 1) WPF control types with Brush-valued backgrounds."
