#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKSPACE_XAML_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
LAYOUT_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
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
    workspace_xaml = read(WORKSPACE_XAML_REL)
    layout = read(LAYOUT_REL)
    workspace_brand = read(WORKSPACE_BRAND_REL)
    shell = read(SHELL_REL)
    activation = read(ACTIVATION_REL)
    logo = read(LOGO_REL)

    # BricsCAD PaletteSet can transiently report zero viewport width during initial measure. The
    # final runtime pass must break the old ViewportWidth feedback loop and expose real content.
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

    # The visible Workspace header owns the brand mark declaratively. This avoids depending on a
    # host-specific Loaded/visual-tree timing race for the primary X/V identity. The code-behind
    # injector remains a compatibility fallback and must short-circuit when the XAML name exists.
    for token in (
        'x:Name="Qs3dWorkspaceBrandMark"',
        'ToolTip="QS3D • X đỏ / V xanh"',
        'x:Name="Qs3dBrandRedX"',
        'Data="M 5,4 L 13,13 M 13,4 L 5,13"',
        'Stroke="#FFE84A4A"',
        'x:Name="Qs3dBrandGreenV"',
        'Data="M 17,8 L 21,13 L 27,4"',
        'Stroke="#FF52BE6C"',
        '<TextBlock Text="QS3D" FontWeight="Bold" FontSize="14"/>',
    ):
        require(workspace_xaml, token, WORKSPACE_XAML_REL)

    mark_pos = workspace_xaml.find('x:Name="Qs3dWorkspaceBrandMark"')
    qs3d_text_pos = workspace_xaml.find('<TextBlock Text="QS3D" FontWeight="Bold" FontSize="14"/>')
    if mark_pos < 0 or qs3d_text_pos < 0 or mark_pos > qs3d_text_pos:
        raise SystemExit(f"FAIL: {WORKSPACE_XAML_REL} must place the X/V mark before the QS3D header text")

    for token in (
        'private const string Qs3dWorkspaceBrandName = "Qs3dWorkspaceBrandMark";',
        "if (FindName(Qs3dWorkspaceBrandName) != null || WorkspaceContentRoot == null)",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "OnQs3dWorkspaceBrandLoaded",
        "DispatcherPriority.Loaded",
        "EnsureQs3dWorkspaceBrandMark",
        "RegisterName(Qs3dWorkspaceBrandName, mark);",
        "X đỏ / V xanh",
        "Color.FromRgb(232, 74, 74)",
        "Color.FromRgb(82, 190, 108)",
        'Geometry.Parse("M 5,4 L 13,13 M 13,4 L 5,13")',
        'Geometry.Parse("M 17,8 L 21,13 L 27,4")',
        "left.Children.Insert(0, mark);",
    ):
        require(workspace_brand, token, WORKSPACE_BRAND_REL)
    for stale in ("BLT3D.exe", "BLT3D.dll", "private-user-images", "screenshot crop"):
        forbid(workspace_brand, stale, WORKSPACE_BRAND_REL)

    # Shell branding must be QS3D-owned clean-room artwork, not screenshot-cropped BLT pixels.
    for token in (
        "Qs3dBrandIconIcoBase64",
        "red-X / green-V",
        "public static bool Reassert()",
        "ExtractLargestEmbeddedPngFromIco()",
        "qs3d-brand-icon-",
    ):
        require(shell, token, SHELL_REL)
    for stale in (
        "Blt3dIconIcoBase64",
        "cropped from the user-provided reference screenshot",
        "private-user-images",
        "BLT3D.exe",
        "BLT3D.dll",
    ):
        forbid(shell, stale, SHELL_REL)

    base64_match = re.search(
        r'private const string Qs3dBrandIconIcoBase64\s*=\s*\n?\s*"([A-Za-z0-9+/=]+)";',
        shell,
    )
    if base64_match is None or len(base64_match.group(1)) < 1000:
        raise SystemExit(f"FAIL: {SHELL_REL} must embed deterministic 16/32px QS3D brand ICO data")

    # Reassert only on host tab/workspace transitions; do not continuously overwrite manual state.
    require(activation, "Blt3dShellChromeCoordinator.Reassert();", ACTIVATION_REL)
    if activation.count("Blt3dShellChromeCoordinator.Reassert();") < 2:
        raise SystemExit(f"FAIL: {ACTIVATION_REL} must reassert shell chrome on tab transition and BIM settle")

    # Repository branding uses the same independently-authored red/green visual identity.
    for token in (
        '#E84A4A',
        '#52BE6C',
        'QS3D original red X and green V BIM/CAD product mark',
        'd="M108 122 222 282M222 122 108 282"',
        'd="M270 207 329 286 422 124"',
    ):
        require(logo, token, LOGO_REL)

    print(
        "PASS: Workspace breaks the zero-viewport width loop, declares the QS3D red-X green-V mark "
        "directly in XAML with a fallback-safe runtime injector, and keeps matching shell/repository branding."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
