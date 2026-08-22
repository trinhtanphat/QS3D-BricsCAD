#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
COMPACT_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
REFERENCE_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferencePaletteLayout.cs"
TREE_REL = "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs"
NAVIGATION_REL = "src/QS3D.Core/Features/FeatureNavigationContracts.cs"
RUNTIME_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
RUNTIME_REPAIR_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dRuntimeLayoutRepair.cs"
LOCAL_INBOX_REL = "docs/LOCAL-AGENT-INBOX.md"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")


def require(text, needle, scope):
    if needle not in text:
        raise SystemExit(f"FAIL: {scope} missing first-render contract: {needle}")


def forbid(text, needle, scope):
    if needle in text:
        raise SystemExit(f"FAIL: {scope} reintroduced blank-palette race: {needle}")


def require_order(text, first, second, scope):
    first_index = text.find(first)
    second_index = text.find(second)
    if first_index < 0 or second_index < 0 or first_index >= second_index:
        raise SystemExit(
            f"FAIL: {scope} must apply {first!r} before {second!r}"
        )


def require_section(text, start_heading, end_heading, scope):
    start_index = text.find(start_heading)
    end_index = text.find(end_heading, start_index + len(start_heading)) if start_index >= 0 else -1
    if start_index < 0 or end_index < 0 or start_index >= end_index:
        raise SystemExit(
            f"FAIL: {scope} missing bounded section {start_heading!r} -> {end_heading!r}"
        )
    return text[start_index:end_index]


def main():
    xaml = read(XAML_REL)
    compact = read(COMPACT_REL)
    reference = read(REFERENCE_REL)
    tree = read(TREE_REL)
    navigation = read(NAVIGATION_REL)
    runtime = read(RUNTIME_REL)
    runtime_repair = read(RUNTIME_REPAIR_REL)
    local_inbox = read(LOCAL_INBOX_REL)

    require(xaml, 'x:Name="WorkspaceContentRoot"', XAML_REL)
    require(xaml, 'Width="{Binding ViewportWidth, ElementName=WorkspaceOverflow}"', XAML_REL)
    require(xaml, 'MinWidth="560"', XAML_REL)
    require(xaml, 'HorizontalContentAlignment="Stretch"', XAML_REL)

    for token in (
        "root.MinWidth = Math.Max(root.MinWidth, 560);",
        "root.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "root.Visibility = Visibility.Visible;",
        "root.Opacity = 1d;",
        "WorkspaceOverflow.HorizontalContentAlignment = HorizontalAlignment.Stretch;",
        "workspace.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "workspace.Visibility = Visibility.Visible;",
        "workspace.Opacity = 1d;",
    ):
        require(compact, token, COMPACT_REL)

    for stale in (
        "root.MinWidth = 0;",
        "retiredColumn.MinWidth = 0;",
        "retiredColumn.MaxWidth = 0;",
        "retiredColumn.Width = new GridLength(0);",
        "if (Grid.GetColumn(child) > 0)",
        "child.Visibility = Visibility.Collapsed;",
    ):
        forbid(compact, stale, COMPACT_REL)

    for token in (
        "using System.Windows.Data;",
        "BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);",
        "root.Width = double.NaN;",
        "root.MinWidth = 0;",
        "root.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "root.Visibility = Visibility.Visible;",
        "root.Opacity = 1d;",
        "WorkspaceOverflow.HorizontalContentAlignment = HorizontalAlignment.Stretch;",
    ):
        require(reference, token, REFERENCE_REL)
    require_order(
        reference,
        "BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);",
        "root.MinWidth = 0;",
        REFERENCE_REL,
    )

    # Figure-2 parity remains a presentation contract, but the registry-driven migration makes
    # FeatureNavigationContracts the canonical owner of visible labels/order. The WPF augmenter
    # must project that registry while retaining only the legacy container migration mechanics.
    for token in (
        "WorkspaceFeatureNavigationCatalog.Navigation",
        "group => EnsureTopAlias(tree, group.LabelKey, group.LegacyLabels, group.LegacyCategory?.ToString())",
        'MoveLegacyTopLevelUnder(tree, slab, "Mái Hắt");',
        'MoveLegacyTopLevelUnder(tree, other, "Modeling");',
        "NormalizeReferenceTopLevelOrder(tree, navigation);",
        "NormalizeRegisteredChildren(groupItems, navigation);",
    ):
        require(tree, token, TREE_REL)

    navigation_groups = require_section(
        navigation,
        "private static readonly FeatureNavigationGroup[] GroupDefinitions =",
        "private static readonly FeatureNavigationRegistration[] NavigationDefinitions =",
        NAVIGATION_REL,
    )
    desired_top_level = (
        '("grid", 0, "Lưới Trục"',
        '("room-finishes", 1, "HT_Phong"',
        '("beam", 2, "Dầm"',
        '("slab", 3, "Sàn"',
        '("column", 5, "Cột"',
        '("structural-wall", 6, "Vách"',
        '("architectural-wall", 7, "Tường KT"',
        '("opening", 8, "Cửa"',
        '("stair", 9, "Cầu Thang"',
        '("foundation", 10, "Móng"',
        '("earthwork", 11, "Đào đắp"',
        '("steel", 12, "Kết cấu thép"',
        '("other", 13, "Cấu kiện khác"',
        '("custom-quantity", 14, "KL Tùy chỉnh"',
    )
    for token in desired_top_level:
        require(navigation_groups, token, NAVIGATION_REL)
    for first, second in zip(desired_top_level, desired_top_level[1:]):
        require_order(navigation_groups, first, second, NAVIGATION_REL)
    require(navigation_groups, '("slab-canopy", 4, "Mái Hắt"', NAVIGATION_REL)

    for stale in (
        'var finish = EnsureTop(tree, "HT_Phòng", null);',
        'var canopy = EnsureTop(tree, "Mái Hắt", null);',
        'EnsureTop(tree, "Modeling", null);',
    ):
        forbid(tree, stale, TREE_REL)

    for token in (
        "BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);",
        "root.Width = double.NaN;",
        "root.MinWidth = 0;",
        "root.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "root.Visibility = Visibility.Visible;",
        "workspace.Visibility = Visibility.Visible;",
        "modelPane.Visibility = Visibility.Visible;",
        "familyPane.Visibility = Visibility.Visible;",
    ):
        require(runtime, token, RUNTIME_REL)
    require_order(
        runtime,
        "BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);",
        "root.MinWidth = 0;",
        RUNTIME_REL,
    )

    # BricsCAD may show/reparent/re-layout after the startup settle. The loaded-lifetime observers
    # must detect that blank client, avoid recovery re-entry loops, keep retries bounded, and preserve
    # an existing user-adjusted model/family split when a genuine late blank-state repair reasserts
    # the authoritative five-zone defaults.
    for token in (
        "private const int Blt3dRuntimeRecoveryRetryPasses = 3;",
        "WireBlt3dRuntimeViewportRecovery();",
        "UnwireBlt3dRuntimeViewportRecovery();",
        "WorkspaceOverflow.SizeChanged += OnBlt3dRuntimeViewportSizeChanged;",
        "WorkspaceOverflow.IsVisibleChanged += OnBlt3dRuntimeViewportVisibilityChanged;",
        "WorkspaceOverflow.LayoutUpdated += OnBlt3dRuntimeViewportLayoutUpdated;",
        "WorkspaceOverflow.SizeChanged -= OnBlt3dRuntimeViewportSizeChanged;",
        "WorkspaceOverflow.IsVisibleChanged -= OnBlt3dRuntimeViewportVisibilityChanged;",
        "WorkspaceOverflow.LayoutUpdated -= OnBlt3dRuntimeViewportLayoutUpdated;",
        "private void OnBlt3dRuntimeViewportLayoutUpdated(object? sender, EventArgs e)",
        "_blt3dRuntimeViewportRecoveryApplying",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining <= 0",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining--;",
        "if (NeedsBlt3dRuntimeViewportRecovery())",
        "QueueBlt3dRuntimeViewportRecovery();",
        "if (!NeedsBlt3dRuntimeViewportRecovery())",
        "_blt3dRuntimeViewportRecoveryApplying = true;",
        "_blt3dRuntimeViewportRecoveryApplying = false;",
        "var preserveSplitterGeometry = TryCaptureBlt3dRuntimeSplitterGeometry(",
        "private bool TryCaptureBlt3dRuntimeSplitterGeometry(",
        "private void RestoreBlt3dRuntimeSplitterGeometry(",
        "if (preserveSplitterGeometry)",
        "columns[0].Width = modelWidth;",
        "columns[2].Width = familyWidth;",
        "BindingOperations.IsDataBound(root, FrameworkElement.WidthProperty)",
        "root.ActualWidth <= 1d",
        "root.ActualHeight <= 1d",
        "workspace.ActualWidth <= 1d",
        "workspace.ActualHeight <= 1d",
        "WorkspaceOverflow.VerticalContentAlignment = VerticalAlignment.Stretch;",
        "WorkspaceContentRoot.VerticalAlignment = VerticalAlignment.Stretch;",
        "InvalidateBlt3dRuntimeLayout();",
    ):
        require(runtime_repair, token, RUNTIME_REPAIR_REL)

    recovery_section = require_section(
        runtime_repair,
        "private void QueueBlt3dRuntimeViewportRecovery()",
        "private bool TryCaptureBlt3dRuntimeSplitterGeometry(",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        recovery_section,
        "var preserveSplitterGeometry = TryCaptureBlt3dRuntimeSplitterGeometry(",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining--;",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        recovery_section,
        "_blt3dRuntimeViewportRecoveryApplying = true;",
        "ReassertBlt3dRuntimeLayout();",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        recovery_section,
        "ReassertBlt3dRuntimeLayout();",
        "RestoreBlt3dRuntimeSplitterGeometry(",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        runtime_repair,
        "if (!NeedsBlt3dRuntimeViewportRecovery())",
        "_blt3dRuntimeViewportRecoveryRetriesRemaining--;",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        runtime_repair,
        "_blt3dRuntimeViewportRecoveryRetriesRemaining--;",
        "_blt3dRuntimeViewportRecoveryApplying = true;",
        RUNTIME_REPAIR_REL,
    )
    require_order(
        runtime_repair,
        "UnwireBlt3dRuntimeViewportRecovery();",
        "panel._blt3dRuntimeLayoutRepairStarted = false;",
        RUNTIME_REPAIR_REL,
    )

    forbid(
        runtime_repair,
        "WorkspaceOverflow.SizeChanged += (_, __) => ReassertBlt3dRuntimeLayout();",
        RUNTIME_REPAIR_REL,
    )
    forbid(
        runtime_repair,
        "WorkspaceOverflow.LayoutUpdated += (_, __) => ReassertBlt3dRuntimeLayout();",
        RUNTIME_REPAIR_REL,
    )

    local012 = require_section(
        local_inbox,
        "## LOCAL-012 — Project Browser native workspace and CAD selection bridge",
        "## LOCAL-013 — clean-room BRC public capability and eligible CAD quantity round-trip",
        LOCAL_INBOX_REL,
    )
    for token in (
        "palette recreation",
        "100/125/150/200% DPI",
        "narrow/normal/wide host widths",
    ):
        require(local012, token, LOCAL_INBOX_REL)

    print(
        "PASS: Workspace keeps the first-measure bootstrap, authoritative idle passes break the "
        "ViewportWidth loop, loaded-lifetime size/visibility/layout recovery is gated/re-entry-safe/"
        "bounded, the user splitter is preserved, and the registry-projected visible reference tree "
        "matches the owner figure while legacy functional children stay reachable; LOCAL-012 remains "
        "the licensed visual qualification lane."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
