#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
COMPACT_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
REFERENCE_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferencePaletteLayout.cs"
RUNTIME_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
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
    runtime = read(RUNTIME_REL)
    local_inbox = read(LOCAL_INBOX_REL)

    # XAML keeps a non-zero bootstrap floor while BricsCAD performs its first PaletteSet measure.
    # The legacy ViewportWidth binding may still be present, but it must not be allowed to coerce
    # the entire client to zero before the authoritative idle pass can break the feedback loop.
    require(xaml, 'x:Name="WorkspaceContentRoot"', XAML_REL)
    require(xaml, 'Width="{Binding ViewportWidth, ElementName=WorkspaceOverflow}"', XAML_REL)
    require(xaml, 'MinWidth="560"', XAML_REL)
    require(xaml, 'HorizontalContentAlignment="Stretch"', XAML_REL)

    # Loaded-time compact presentation must preserve a measurable/visible client and leave final
    # model/family column geometry to the Reference/FiveZone owners. It must never zero every live
    # column while the Width binding is still active.
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

    # ApplicationIdle is the earliest authoritative presentation pass. It must break the zero-width
    # binding before removing the bootstrap minimum, so even if SystemIdle is delayed the client is
    # already stretch-sized and visible.
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

    # The later SystemIdle pass remains the final idempotent repair and must retain the same ordering.
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

    # Real BricsCAD first-render/HiDPI proof is LOCAL_ONLY. Reuse the canonical Workspace local
    # handoff instead of inventing another queue. Validate only the stable scenario identity here:
    # LOCAL-012's workflow status/evidence is intentionally mutable as local qualification advances.
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
        "PASS: Workspace keeps a non-zero first-measure bootstrap, CompactShell no longer collapses "
        "live columns during Loaded, ApplicationIdle/SystemIdle break the ViewportWidth loop before "
        "allowing zero minimum width, and the existing LOCAL-012 BricsCAD visual scenario remains referenced."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
