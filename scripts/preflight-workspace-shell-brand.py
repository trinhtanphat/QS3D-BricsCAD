#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAYOUT_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
WORKSPACE_XAML_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
WORKSPACE_BRAND_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Qs3dBrandMark.cs"
SHELL_REL = "src/QS3D.BricsCAD.V25/Ribbon/Blt3dShellChromeCoordinator.cs"
ACTIVATION_REL = "src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs"
LOGO_REL = "assets/branding/qs3d-logo.svg"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")


def require(text, needle, scope):
    if needle not in text:
        raise SystemExit(f"FAIL: {scope} missing required contract: {needle}")


def forbid(text, needle, scope):
    if needle.lower() in text.lower():
        raise SystemExit(f"FAIL: {scope} contains forbidden/stale contract: {needle}")


def main():
    layout = read(LAYOUT_REL)
    workspace_xaml = read(WORKSPACE_XAML_REL)
    shell = read(SHELL_REL)
    activation = read(ACTIVATION_REL)
    logo = read(LOGO_REL)

    # Preserve the independently valid BricsCAD PaletteSet blank-rendering fix from #2610.
    for token in (
        "BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);",
        "root.Width = double.NaN;",
        "root.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "root.Visibility = Visibility.Visible;",
        "root.Opacity = 1d;",
        "workspace.HorizontalAlignment = HorizontalAlignment.Stretch;",
        "workspace.Visibility = Visibility.Visible;",
        "modelPane.Visibility = Visibility.Visible;",
        "familyPane.Visibility = Visibility.Visible;",
    ):
        require(layout, token, LAYOUT_REL)
    require(layout, "using System.Windows.Data;", LAYOUT_REL)

    # Owner-corrected status-marker semantics: the live Workspace must not contain the X/V tile
    # that was introduced solely from progress/reporting shorthand.
    workspace_brand = ROOT / WORKSPACE_BRAND_REL
    if workspace_brand.exists():
        raise SystemExit(
            f"FAIL: {WORKSPACE_BRAND_REL} is status-derived product artwork and must remain removed"
        )

    # The cancelled #2617 carrier attempted to make the same status-derived mark declarative in
    # WorkspacePanel.xaml. Guard that exact regression path too so a future merge/replay cannot
    # bypass the removed runtime partial while still restoring the rejected product pixels.
    for stale in (
        'x:Name="Qs3dWorkspaceBrandMark"',
        'x:Name="Qs3dBrandRedX"',
        'x:Name="Qs3dBrandGreenV"',
        'ToolTip="QS3D • X đỏ / V xanh"',
        'Stroke="#FFE84A4A"',
        'Stroke="#FF52BE6C"',
    ):
        forbid(workspace_xaml, stale, WORKSPACE_XAML_REL)

    # Preserve compact shell chrome/reassert behavior, but leave application icon ownership to the
    # BricsCAD host. Never restore the old screenshot-cropped BLT3D payload or the status-derived X/V ICO.
    for token in (
        "public static bool Reassert()",
        'CollapseKnownChromeProperty(control, "QuickAccessToolBar");',
        'CollapseKnownChromeProperty(control, "ApplicationButton");',
        'CollapseKnownChromeProperty(control, "SearchBox");',
        "CollapseNonReferenceChrome(ribbonRoot);",
        "HiddenElements.Clear();",
    ):
        require(shell, token, SHELL_REL)

    # Guard both the historical helper names and lower-level host-icon takeover primitives. This
    # prevents an equivalent WPF/native icon override from returning under renamed helper methods.
    for stale in (
        "Qs3dBrandIconIcoBase64",
        "Blt3dIconIcoBase64",
        "ApplyWpfWindowIcon",
        "ApplyNativeWindowIcon",
        "WmSetIcon",
        "WM_SETICON",
        ".Icon =",
        "WindowInteropHelper",
        "SendMessage(",
        "GetHicon(",
        "Icon.FromHandle",
        "DestroyIcon(",
        "cropped from the user-provided reference screenshot",
        "private-user-images",
        "BLT3D.exe",
        "BLT3D.dll",
        "red-X / green-V app mark",
    ):
        forbid(shell, stale, SHELL_REL)

    # Existing lifecycle points remain valid for reapplying host chrome visibility after Ribbon or
    # workspace reconstruction; they no longer imply application-icon ownership.
    require(activation, "Blt3dShellChromeCoordinator.Reassert();", ACTIVATION_REL)
    if activation.count("Blt3dShellChromeCoordinator.Reassert();") < 2:
        raise SystemExit(
            f"FAIL: {ACTIVATION_REL} must reassert shell chrome on tab transition and BIM settle"
        )

    # Restore the independent pre-#2610 QS3D product logo instead of status-marker-derived artwork.
    for token in (
        "QS3D product family isometric precision cube mark",
        "#061323",
        "#0A2340",
        "#00C8FF",
        "#168BFF",
        "#2457FF",
        'd="M256 92 388 168 256 244 124 168Z"',
        'd="M124 168v152l132 78V244M388 168v152l-132 78"',
    ):
        require(logo, token, LOGO_REL)
    for stale in ("#E84A4A", "#52BE6C", "red X", "green V"):
        forbid(logo, stale, LOGO_REL)

    print(
        "PASS: Workspace blank rendering and shell lifecycle behavior remain guarded, the host retains "
        "application-icon ownership, status-derived Workspace/shell X/V artwork is absent from both "
        "runtime and declarative paths, and the repository uses the independent QS3D cube mark."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
