# Exercises the full production layout method with real WPF controls, without CAD.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'Run powershell -STA.' }
$taskRoot = Split-Path $PSScriptRoot -Parent
function Read-Method([string]$File, [string]$Name) {
    $source = Get-Content -LiteralPath (Join-Path $taskRoot ('src/QS3D.BricsCAD.V25/UI/' + $File)) -Raw -Encoding UTF8
    $match = [regex]::Match($source, '(?m)^        private (?:static )?(?:void|bool) ' + [regex]::Escape($Name) + '\(')
    if (-not $match.Success) { throw "Missing production method $Name" }
    $brace = $source.IndexOf('{', $match.Index)
    $depth = 0
    for ($i = $brace; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        elseif ($source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($match.Index, $i - $match.Index + 1) }
        }
    }
    throw "Unterminated production method $Name"
}
$methods = @(
    Read-Method 'WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs' 'ApplyBlt3dFiveZoneRuntimeLayout'
    Read-Method 'WorkspacePanel.Blt3dFamilyWorkspace.cs' 'IsVisualDescendant'
    Read-Method 'WorkspacePanel.DedicatedPropertiesPalette.cs' 'RestoreEmbeddedPropertiesSlot'
) -join "`n"
$fixture = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

public sealed class PropertyViewportFixture : UserControl {
    readonly Grid WorkspaceContentRoot = new Grid();
    readonly ScrollViewer WorkspaceOverflow = new ScrollViewer();
    readonly ListBox FamilyList = new ListBox();
    readonly ListView PropertyList = new ListView();
    readonly Grid familyPane = new Grid();
    readonly DockPanel propertiesDock = new DockPanel();
    readonly Border propertiesRegion = new Border { Padding = new Thickness(7) };
    readonly Border propertyHeader = new Border { Height = 100 };
    readonly GridSplitter rowSplitter = new GridSplitter();
    readonly List<Row> rows = new List<Row>();
    GridSplitter _blt3dRuntimeColumnSplitter;
    bool _dedicatedPropertiesPaletteActive;
    Size fixtureSize;
    const double Tolerance = 1.0;

    public sealed class Row {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Value { get; set; }
    }

    PropertyViewportFixture() {
        // Preserve the production hierarchy and host clipping. The 100-DIP fixed
        // header matches LOCAL022 allocation28; the remaining area must scroll.
        ClipToBounds = true;
        WorkspaceOverflow.ClipToBounds = true;
        WorkspaceOverflow.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        WorkspaceOverflow.CanContentScroll = false;
        WorkspaceOverflow.VerticalContentAlignment = VerticalAlignment.Stretch;
        Content = WorkspaceOverflow;
        WorkspaceOverflow.Content = WorkspaceContentRoot;
        WorkspaceContentRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        WorkspaceContentRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        WorkspaceContentRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        var workspace = new Grid();
        for (int i = 0; i < 5; i++) workspace.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(workspace, 1);
        WorkspaceContentRoot.Children.Add(workspace);
        workspace.Children.Add(new Border());
        var columnSplitter = new GridSplitter();
        Grid.SetColumn(columnSplitter, 1);
        workspace.Children.Add(columnSplitter);
        Grid.SetColumn(familyPane, 2);
        workspace.Children.Add(familyPane);
        for (int i = 0; i < 3; i++) familyPane.RowDefinitions.Add(new RowDefinition());
        var familyRegion = new Border { Child = FamilyList };
        familyPane.Children.Add(familyRegion);
        FamilyList.Items.Add("Family / Type");
        Grid.SetRow(rowSplitter, 1);
        familyPane.Children.Add(rowSplitter);
        Grid.SetRow(propertiesRegion, 2);
        familyPane.Children.Add(propertiesRegion);
        propertiesRegion.Child = propertiesDock;
        DockPanel.SetDock(propertyHeader, Dock.Top);
        propertiesDock.Children.Add(propertyHeader);
        propertiesDock.Children.Add(PropertyList);
        PropertyList.MinHeight = 118; // Earlier CompactShell pass, superseded by production.
        ScrollViewer.SetCanContentScroll(PropertyList, false);
        VirtualizingPanel.SetIsVirtualizing(PropertyList, false);
        VirtualizingPanel.SetVirtualizationMode(PropertyList, VirtualizationMode.Standard);

        var editor = new FrameworkElementFactory(typeof(TextBox));
        editor.SetValue(FrameworkElement.HeightProperty, 24.0);
        editor.SetValue(FrameworkElement.WidthProperty, 88.0);
        editor.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        editor.SetBinding(TextBox.TextProperty, new Binding("Value"));
        var row = new FrameworkElementFactory(typeof(Border));
        row.SetValue(FrameworkElement.HeightProperty, 42.0);
        row.AppendChild(editor);
        PropertyList.ItemTemplate = new DataTemplate { VisualTree = row };
        var groupHeader = new FrameworkElementFactory(typeof(TextBlock));
        groupHeader.SetValue(FrameworkElement.HeightProperty, 25.0);
        groupHeader.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        PropertyList.GroupStyle.Add(new GroupStyle {
            HeaderTemplate = new DataTemplate { VisualTree = groupHeader }
        });
        string[] names = { "Family", "Category", "L1", "W1", "L2", "W2", "H1", "H2", "Material", "Volume" };
        for (int i = 0; i < names.Length; i++) rows.Add(new Row {
            Name = names[i], Group = i < 2 ? "Identity" : i < 6 ? "Dimensions" : i < 8 ? "Height" : "Other", Value = "1000"
        });
        PropertyList.ItemsSource = rows;
        CollectionViewSource.GetDefaultView(rows).GroupDescriptions.Add(new PropertyGroupDescription("Group"));
    }

    static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T) yield return (T)child;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    void Layout(double height) {
        fixtureSize = new Size(630, height);
        Measure(fixtureSize);
        Arrange(new Rect(fixtureSize));
        UpdateLayout();
        Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(delegate {}));
        UpdateLayout();
    }

    void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    void CheckViewport(string scenario) {
        var slot = LayoutInformation.GetLayoutSlot(PropertyList);
        var scroll = Descendants<ScrollViewer>(PropertyList).First();
        double available = propertiesDock.ActualHeight - propertyHeader.ActualHeight;
        Check(available > 30, scenario + ": fixture must leave a usable editor area");
        Check(PropertyList.ActualHeight <= available + Tolerance,
            scenario + ": PropertyList exceeds visible remaining area: list=" + PropertyList.ActualHeight +
            ", available=" + available + ", slot=" + slot.Height + ", viewport=" + scroll.ViewportHeight);
        Check(scroll.ViewportHeight > 20 && scroll.ViewportHeight <= available + Tolerance,
            scenario + ": scroll viewport exceeds the allocated pane");
        Check(familyPane.RowDefinitions[2].MinHeight == 120 &&
            familyPane.RowDefinitions[2].Height == new GridLength(44, GridUnitType.Star) &&
            familyPane.RowDefinitions[0].Height == new GridLength(56, GridUnitType.Star),
            scenario + ": embedded pane minimum/proportions changed");
        Check(ClipToBounds && WorkspaceOverflow.ClipToBounds &&
            WorkspaceOverflow.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled,
            scenario + ": host boundary changed");
        foreach (string name in new[] { "H2", "Volume", "Family" }) {
            Row target = rows.Single(r => r.Name == name);
            PropertyList.ScrollIntoView(target);
            Layout(fixtureSize.Height);
            var editor = Descendants<TextBox>(PropertyList).Single(e => ReferenceEquals(e.DataContext, target));
            var editorBounds = editor.TransformToAncestor(propertiesDock).TransformBounds(new Rect(editor.RenderSize));
            var scrollBounds = scroll.TransformToAncestor(propertiesDock).TransformBounds(new Rect(scroll.RenderSize));
            var visible = new Rect(0, propertyHeader.ActualHeight, propertiesDock.ActualWidth, available);
            visible.Intersect(scrollBounds);
            Check(editorBounds.Top >= visible.Top - Tolerance && editorBounds.Bottom <= visible.Bottom + Tolerance,
                scenario + ": " + name + " editor is clipped after ScrollIntoView: editor=" + editorBounds + ", visible=" + visible);
        }
        Console.WriteLine("PASS: " + scenario + " list=" + PropertyList.ActualHeight + " available=" + available);
    }

    public static void Run() {
        var fixture = new PropertyViewportFixture();
        fixture.Layout(444);
        fixture.ApplyBlt3dFiveZoneRuntimeLayout();
        fixture.Layout(444);
        fixture.CheckViewport("short hosted pane");
        fixture.Layout(800);
        fixture.CheckViewport("tall hosted pane");
        fixture.Layout(444);
        fixture.CheckViewport("tall-to-short resize");
        fixture.ApplyBlt3dFiveZoneRuntimeLayout();
        fixture.Layout(444);
        fixture.CheckViewport("repeated final layout repair");

        // Model the actual reparent boundary, then run both production restore
        // and final layout methods when the same editor returns to Workspace.
        fixture.familyPane.Children.Remove(fixture.propertiesRegion);
        var dedicatedHost = new Grid();
        dedicatedHost.Children.Add(fixture.propertiesRegion);
        fixture._dedicatedPropertiesPaletteActive = true;
        fixture.ApplyBlt3dFiveZoneRuntimeLayout();
        fixture.Layout(444);
        fixture.Check(fixture.familyPane.RowDefinitions[2].Height.Value == 0 &&
            fixture.PropertyList.MinHeight == 0 && fixture.rowSplitter.Visibility == Visibility.Collapsed,
            "dedicated layout must retire the embedded slot");
        dedicatedHost.Children.Remove(fixture.propertiesRegion);
        fixture.familyPane.Children.Add(fixture.propertiesRegion);
        RestoreEmbeddedPropertiesSlot(fixture.familyPane);
        fixture._dedicatedPropertiesPaletteActive = false;
        fixture.ApplyBlt3dFiveZoneRuntimeLayout();
        fixture.Layout(444);
        fixture.CheckViewport("dedicated-to-embedded restoration");
    }
// PRODUCTION_METHODS
}
'@
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
Add-Type -TypeDefinition ($fixture.Replace('// PRODUCTION_METHODS', $methods)) -ReferencedAssemblies PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Core
[PropertyViewportFixture]::Run()
Write-Output 'PASS: production property viewport WPF regression; short/tall/resize/repair/dedicated restoration without CAD.'
