#!/usr/bin/env python3
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SAFETY = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomFinishTreeVirtualizationSafety.cs"
ROOM = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomWorkspacePane.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"

errors = []
for path in (SAFETY, ROOM, WORKSPACE):
    if not path.is_file():
        errors.append("missing Room-finish lifecycle source: " + str(path.relative_to(ROOT)))

if not errors:
    safety = SAFETY.read_text(encoding="utf-8")
    room = ROOM.read_text(encoding="utf-8")
    workspace = WORKSPACE.read_text(encoding="utf-8")

    for token in (
        'private const string RoomFinishTreeIdentity = "RoomFinishTree";',
        "FindSingleRoomFinishTree(root)",
        "Workspace contains more than one Room finish TreeView owner before first layout.",
        "tree.Name = RoomFinishTreeIdentity;",
        "VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);",
        "VirtualizingPanel.SetIsVirtualizing(tree, false);",
        "ScrollViewer.SetCanContentScroll(tree, false);",
        "EnsureRoomFinishStaticItemsPreLayout(tree);",
        'Header = "Trát Trần"',
    ):
        if token not in safety:
            errors.append("Room-finish pre-layout identity/state contract missing: " + token)

    forbidden_late_tokens = (
        "finishTree.Items.Add(",
        "RoomPaneDescendants<TreeView>(roomPane).FirstOrDefault()",
        "VirtualizingPanel.SetVirtualizationMode",
    )
    for token in forbidden_late_tokens:
        if token in room:
            errors.append("Room Loaded/SystemIdle presentation still mutates TreeView lifecycle: " + token)

    tree_count = workspace.count("<TreeView>") + workspace.count("<TreeView ")
    if tree_count != 2:
        errors.append("Workspace TreeView inventory must remain exactly two controls for this lifecycle probe; found %d" % tree_count)

if errors:
    print("V25 Room-finish executable lifecycle preflight failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

# The source checks above run on every platform. Protected V25 CI is Windows; there we additionally
# execute an STA WPF RED/GREEN reproducer through the real Dispatcher/HwndSource lifecycle. This
# intentionally does not load BricsCAD and therefore cannot consume a licensed host allocation.
if os.name != "nt":
    print("V25 Room-finish executable lifecycle source contract passed (WPF runtime probe requires Windows)")
    sys.exit(0)

powershell = r'''
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml

$source = @"
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;

public static class RoomFinishTreeLifecycleProbe
{
    private const int WM_SIZE = 0x0005;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public static void Run()
    {
        RunRed();
        RunGreen();
        Console.WriteLine("Room-finish WPF lifecycle RED/GREEN passed");
    }

    private static void RunRed()
    {
        var tree = CreateTree("RedRoomFinishTree", 64);
        VirtualizingPanel.SetIsVirtualizing(tree, true);
        ScrollViewer.SetCanContentScroll(tree, true);
        VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);

        var window = CreateHost(tree);
        Exception observed = null;
        try
        {
            window.Show();
            window.UpdateLayout();
            Drain(window.Dispatcher);

            // Known RED mechanism: a measured VirtualizingStackPanel must reject a later mode flip.
            VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Recycling);
            DispatchSystemIdleTwice(window.Dispatcher, delegate { });
            SendWmSize(window, 438, 278);
            window.UpdateLayout();
            Drain(window.Dispatcher);
        }
        catch (Exception ex)
        {
            observed = Unwrap(ex);
        }
        finally
        {
            try { window.Close(); } catch { }
        }

        if (observed == null ||
            observed.ToString().IndexOf("Cannot change the VirtualizationMode attached property", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("RED did not reproduce the post-Measure VirtualizationMode failure.", observed);
    }

    private static void RunGreen()
    {
        var tree = CreateTree("RoomFinishTree", 0);
        VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);
        VirtualizingPanel.SetIsVirtualizing(tree, false);
        ScrollViewer.SetCanContentScroll(tree, false);
        AddStaticRoomFinishItems(tree);

        RequireLocalValue(tree, VirtualizingPanel.VirtualizationModeProperty, "VirtualizationMode before Measure");
        RequireLocalValue(tree, VirtualizingPanel.IsVirtualizingProperty, "IsVirtualizing before Measure");
        RequireLocalValue(tree, ScrollViewer.CanContentScrollProperty, "CanContentScroll before Measure");

        var beforeCount = tree.Items.Count;
        var window = CreateHost(tree);
        var presentationRan = false;
        try
        {
            window.Show(); // construction -> first Measure -> Loaded
            window.UpdateLayout();
            if (!tree.IsLoaded)
                throw new InvalidOperationException("GREEN TreeView did not reach Loaded.");

            // Production still uses a double-SystemIdle presentation/layout pass. The GREEN path
            // proves that pass can run after first Measure without mutating the static TreeView.
            DispatchSystemIdleTwice(window.Dispatcher, delegate
            {
                presentationRan = true;
                if (tree.Items.Count != beforeCount)
                    throw new InvalidOperationException("SystemIdle presentation mutated RoomFinishTree items.");
                if (VirtualizingPanel.GetVirtualizationMode(tree) != VirtualizationMode.Standard)
                    throw new InvalidOperationException("SystemIdle presentation changed RoomFinishTree mode.");
            });
            Drain(window.Dispatcher);

            if (!presentationRan)
                throw new InvalidOperationException("GREEN double-SystemIdle presentation did not execute.");

            // Force the hosted HWND through WM_SIZE, matching the licensed failure's final layout edge.
            SendWmSize(window, 452, 291);
            window.Width = 452;
            window.Height = 291;
            window.UpdateLayout();
            Drain(window.Dispatcher);

            if (tree.Items.Count != beforeCount)
                throw new InvalidOperationException("WM_SIZE changed the sealed RoomFinishTree item set.");
            if (VirtualizingPanel.GetVirtualizationMode(tree) != VirtualizationMode.Standard ||
                VirtualizingPanel.GetIsVirtualizing(tree) ||
                ScrollViewer.GetCanContentScroll(tree))
                throw new InvalidOperationException("RoomFinishTree local virtualization state drifted after WM_SIZE.");

            RequireLocalValue(tree, VirtualizingPanel.VirtualizationModeProperty, "VirtualizationMode after WM_SIZE");
            RequireLocalValue(tree, VirtualizingPanel.IsVirtualizingProperty, "IsVirtualizing after WM_SIZE");
            RequireLocalValue(tree, ScrollViewer.CanContentScrollProperty, "CanContentScroll after WM_SIZE");
        }
        finally
        {
            try { window.Close(); } catch { }
        }
    }

    private static TreeView CreateTree(string name, int generatedItems)
    {
        var tree = new TreeView { Name = name };
        var factory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
        tree.ItemsPanel = new ItemsPanelTemplate(factory);
        for (var index = 0; index < generatedItems; index++)
            tree.Items.Add(new TreeViewItem { Header = "Item " + index });
        return tree;
    }

    private static void AddStaticRoomFinishItems(TreeView tree)
    {
        tree.Items.Add(new TreeViewItem { Header = "Sàn Hoàn Thiện", Tag = "FloorFinish" });
        tree.Items.Add(new TreeViewItem { Header = "Chống Thấm", Tag = "Waterproofing" });
        tree.Items.Add(new TreeViewItem { Header = "Chân Tường", Tag = "Skirting" });
        tree.Items.Add(new TreeViewItem { Header = "Hoàn Thiện Tường", Tag = "WallFinish" });
        tree.Items.Add(new TreeViewItem { Header = "Trần Hoàn Thiện", Tag = "CeilingFinish" });
        tree.Items.Add(new TreeViewItem { Header = "Trát Trần", Tag = "CeilingFinish" });
    }

    private static Window CreateHost(TreeView tree)
    {
        var innerDock = new DockPanel();
        innerDock.Children.Add(tree);
        var border = new Border { Child = innerDock };
        var grid = new Grid();
        grid.Children.Add(border);
        var outerDock = new DockPanel();
        outerDock.Children.Add(grid);
        return new Window
        {
            Width = 420,
            Height = 260,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = outerDock
        };
    }

    private static void DispatchSystemIdleTwice(Dispatcher dispatcher, Action action)
    {
        dispatcher.BeginInvoke(
            DispatcherPriority.SystemIdle,
            new Action(delegate
            {
                dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, action);
            }));
        Drain(dispatcher);
        Drain(dispatcher);
    }

    private static void Drain(Dispatcher dispatcher)
    {
        var frame = new DispatcherFrame();
        dispatcher.BeginInvoke(
            DispatcherPriority.SystemIdle,
            new Action(delegate { frame.Continue = false; }));
        Dispatcher.PushFrame(frame);
    }

    private static void SendWmSize(Window window, int width, int height)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Hosted WPF window has no HWND before WM_SIZE.");
        var packed = new IntPtr(((height & 0xffff) << 16) | (width & 0xffff));
        SendMessage(handle, WM_SIZE, IntPtr.Zero, packed);
    }

    private static void RequireLocalValue(DependencyObject owner, DependencyProperty property, string label)
    {
        var source = DependencyPropertyHelper.GetValueSource(owner, property);
        if (source.BaseValueSource != BaseValueSource.Local)
            throw new InvalidOperationException(label + " is not a local pre-layout value; actual=" + source.BaseValueSource);
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex.InnerException != null &&
               (ex is System.Reflection.TargetInvocationException || ex is System.AggregateException))
            ex = ex.InnerException;
        return ex;
    }
}
"@

$refs = @(
    [System.Windows.Controls.TreeView].Assembly.Location,
    [System.Windows.Media.Visual].Assembly.Location,
    [System.Windows.DependencyObject].Assembly.Location,
    [System.Xaml.XamlReader].Assembly.Location
) | Select-Object -Unique

Add-Type -TypeDefinition $source -ReferencedAssemblies $refs
[RoomFinishTreeLifecycleProbe]::Run()
'''

completed = subprocess.run(
    ["powershell.exe", "-NoProfile", "-NonInteractive", "-STA", "-ExecutionPolicy", "Bypass", "-Command", "-"],
    input=powershell,
    text=True,
    capture_output=True,
    timeout=45,
)

if completed.stdout:
    print(completed.stdout.rstrip())
if completed.returncode != 0:
    if completed.stderr:
        print(completed.stderr.rstrip(), file=sys.stderr)
    print("V25 Room-finish executable lifecycle preflight failed: WPF RED/GREEN probe exit=%d" % completed.returncode, file=sys.stderr)
    sys.exit(completed.returncode or 1)

print("V25 Room-finish executable lifecycle preflight passed")
