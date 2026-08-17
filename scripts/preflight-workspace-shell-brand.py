#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
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
    layout = read(LAYOUT_REL)
    shell = read(SHELL_REL)
    activation = read(ACTIVATION_REL)
    logo = read(LOGO_REL)

    workspace_brand_path = ROOT / WORKSPACE_BRAND_REL
    if workspace_brand_path.exists():
        raise SystemExit(
            f"FAIL: {WORKSPACE_BRAND_REL} must stay removed; status-derived X/V artwork does not belong in Workspace"
        )

    # Preserve the independent #2610 rendering fix. BricsCAD PaletteSet can transiently report zero
    # viewport width during initial measure; the final runtime pass must break that feedback loop.
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

    # Shell lifecycle remains idempotent, but BricsCAD owns its application/window icon. QS3D may
    # reassert only the reference-excluded chrome visibility treatment.
    for token in (
        "public static bool Reassert()",
        'CollapseKnownChromeProperty(control, "QuickAccessToolBar");',
        'CollapseKnownChromeProperty(control, "ApplicationButton");',
        'CollapseKnownChromeProperty(control, "SearchBox");',
        "CollapseNonReferenceChrome(ribbonRoot);",
        "HiddenElements.Clear();",
        "BricsCAD keeps full ownership of its window/application",
    ):
        require(shell, token, SHELL_REL)

    for stale in (
        "Qs3dBrandIconIcoBase64",
        "Blt3dIconIcoBase64",
        "ApplyWpfWindowIcon",
        "ApplyNativeWindowIcon",
        "LoadEmbeddedIcon",
        "ExtractLargestEmbeddedPngFromIco",
        "WmSetIcon",
        "WM_SETICON",
        "LoadImage(",
        "SendMessage(",
        "DestroyIcon(",
        "red-X",
        "green-V",
        "X/V mark",
        "X đỏ",
        "V xanh",
        "private-user-images",
        "screenshot crop",
    ):
        forbid(shell, stale, SHELL_REL)

    # Reassert only on host tab/workspace transitions; do not continuously overwrite manual state.
    require(activation, "Blt3dShellChromeCoordinator.Reassert();", ACTIVATION_REL)
    if activation.count("Blt3dShellChromeCoordinator.Reassert();") < 2:
        raise SystemExit(f"FAIL: {ACTIVATION_REL} must reassert shell chrome on tab transition and BIM settle")

    # Repository branding returns to the clean-room QS3D precision-cube identity that predated the
    # status-marker misunderstanding.
    for token in (
        "QS3D CAD",
        "QS3D product family isometric precision cube mark",
        "#061323",
        "#168BFF",
        "#33C5FF",
        'd="M256 92 388 168 256 244 124 168Z"',
    ):
        require(logo, token, LOGO_REL)

    for stale in (
        "#E84A4A",
        "#52BE6C",
        "red X",
        "green V",
        'd="M108 122 222 282M222 122 108 282"',
        'd="M270 207 329 286 422 124"',
    ):
        forbid(logo, stale, LOGO_REL)

    print(
        "PASS: Workspace keeps the zero-viewport rendering fix, status-derived X/V artwork is absent, "
        "BricsCAD retains host icon ownership, shell visibility reassert remains idempotent, and the "
        "repository uses the clean-room QS3D precision-cube brand mark."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
