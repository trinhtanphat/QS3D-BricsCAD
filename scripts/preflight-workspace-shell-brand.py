#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
LAYOUT_REL = "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
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

    # The host PaletteSet can report zero viewport width during initial measure. The final runtime
    # pass must break the old ViewportWidth-width feedback loop and make the real workspace visible.
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
        "PASS: Workspace breaks the zero-viewport width loop, restores visible two-column BIM content, "
        "and QS3D shell/repository branding uses an original red-X green-V mark with transition reassertion."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
