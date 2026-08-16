[CmdletBinding()]
param(
    [string]$PluginPath,
    [string]$BricscadDirectory = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# IMPORTANT: this is an OFFLINE source-contract check. It must never load the
# QS3D plugin or BricsCAD managed/native assemblies into standalone PowerShell.
# Real palette construction/layout belongs to the licensed in-host V25 runtime
# probe. PluginPath/BricscadDirectory are retained only for caller compatibility.
$null = $PluginPath
$null = $BricscadDirectory

$root = Split-Path -Parent $PSScriptRoot
$workspacePath = Join-Path $root 'src\QS3D.BricsCAD.V25\UI\WorkspacePanel.xaml'
$rightPanelPath = Join-Path $root 'src\QS3D.BricsCAD.V25\UI\RightPanel.xaml'

function Read-XamlDocument {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required palette XAML source is missing: $Path"
    }

    try {
        return [xml](Get-Content -LiteralPath $Path -Raw -Encoding UTF8)
    }
    catch {
        throw "Palette XAML is not well-formed XML: $Path :: $($_.Exception.Message)"
    }
}

function New-XamlNamespaceManager {
    param([Parameter(Mandatory = $true)][xml]$Document)

    $manager = [System.Xml.XmlNamespaceManager]::new($Document.NameTable)
    $manager.AddNamespace('p', 'http://schemas.microsoft.com/winfx/2006/xaml/presentation')
    $manager.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
    return $manager
}

function Require-Node {
    param(
        [Parameter(Mandatory = $true)][xml]$Document,
        [Parameter(Mandatory = $true)][System.Xml.XmlNamespaceManager]$Namespaces,
        [Parameter(Mandatory = $true)][string]$XPath,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $node = $Document.SelectSingleNode($XPath, $Namespaces)
    if ($null -eq $node) { throw $Message }
    return $node
}

$workspace = Read-XamlDocument $workspacePath
$workspaceNs = New-XamlNamespaceManager $workspace
$workspaceRoot = Require-Node $workspace $workspaceNs '/p:UserControl' 'WorkspacePanel.xaml must have a UserControl root.'
if ($workspaceRoot.GetAttribute('Class', 'http://schemas.microsoft.com/winfx/2006/xaml') -ne 'QS3D.BricsCAD.V25.UI.WorkspacePanel') {
    throw 'WorkspacePanel.xaml x:Class no longer matches the V25 WorkspacePanel contract.'
}

$workspaceOverflow = Require-Node $workspace $workspaceNs "//p:ScrollViewer[@x:Name='WorkspaceOverflow']" 'WorkspacePanel must expose WorkspaceOverflow.'
if ($workspaceOverflow.GetAttribute('HorizontalScrollBarVisibility') -ne 'Auto' -or
    $workspaceOverflow.GetAttribute('VerticalScrollBarVisibility') -ne 'Disabled' -or
    $workspaceOverflow.GetAttribute('CanContentScroll') -ne 'False' -or
    $workspaceOverflow.GetAttribute('PanningMode') -ne 'HorizontalOnly') {
    throw 'WorkspaceOverflow must keep the host-safe horizontal-only overflow contract.'
}

$workspaceRootGrid = Require-Node $workspace $workspaceNs "//p:Grid[@x:Name='WorkspaceContentRoot']" 'WorkspacePanel must expose WorkspaceContentRoot.'
$minWidth = 0d
if (-not [double]::TryParse($workspaceRootGrid.GetAttribute('MinWidth'), [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$minWidth) -or $minWidth -lt 560d) {
    throw 'WorkspaceContentRoot must retain at least the 560-DIP compact overflow width.'
}
if ($workspaceRootGrid.GetAttribute('Width') -notmatch 'ViewportWidth.*WorkspaceOverflow') {
    throw 'WorkspaceContentRoot width must stay bound to WorkspaceOverflow.ViewportWidth.'
}
foreach ($name in @('FamilySearch', 'PropertySearch')) {
    Require-Node $workspace $workspaceNs "//*[@x:Name='$name']" "WorkspacePanel must retain keyboard-focus target $name." | Out-Null
}
$workspaceTheme = Require-Node $workspace $workspaceNs "//p:ResourceDictionary[@Source='Theme.xaml']" 'WorkspacePanel must merge Theme.xaml.'
$null = $workspaceTheme
Write-Host 'PASS: WorkspacePanel source contract is structurally valid without loading BricsCAD/plugin assemblies.'

$rightPanel = Read-XamlDocument $rightPanelPath
$rightNs = New-XamlNamespaceManager $rightPanel
$rightRoot = Require-Node $rightPanel $rightNs '/p:UserControl' 'RightPanel.xaml must have a UserControl root.'
if ($rightRoot.GetAttribute('Class', 'http://schemas.microsoft.com/winfx/2006/xaml') -ne 'QS3D.BricsCAD.V25.UI.RightPanel') {
    throw 'RightPanel.xaml x:Class no longer matches the V25 RightPanel contract.'
}
foreach ($name in @('DrawingHeaderGrid', 'DrawingList', 'LayerHeaderGrid', 'LayerSearchBox', 'LayerList')) {
    Require-Node $rightPanel $rightNs "//*[@x:Name='$name']" "RightPanel must retain named contract element $name." | Out-Null
}
$rightTheme = Require-Node $rightPanel $rightNs "//p:ResourceDictionary[@Source='Theme.xaml']" 'RightPanel must merge Theme.xaml.'
$null = $rightTheme
Write-Host 'PASS: RightPanel source contract is structurally valid without loading BricsCAD/plugin assemblies.'

Write-Host 'PASS: offline palette qualification completed using source/XAML checks only.'
Write-Host 'Licensed in-host BricsCAD V25 runtime remains the authority for palette construction, layout, host theme, HiDPI, and native integration.'
