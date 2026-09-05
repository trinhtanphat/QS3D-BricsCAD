#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LEGACY = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFamilyWorkspace.cs"
PATCH = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFamilyModeVectorIcons.cs"
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Blt3dVectorIcon.cs"


def require(source: str, needle: str, label: str, errors: list[str]) -> None:
    if needle not in source:
        errors.append(f"missing {label}: {needle}")


def main() -> int:
    errors: list[str] = []
    legacy = LEGACY.read_text(encoding="utf-8")
    icons = ICONS.read_text(encoding="utf-8")

    # #4586 owns the legacy Add-routing source. This lane must remain additive and must
    # patch only the two already-existing mode-card placeholders after the chooser is built.
    require(legacy, 'CreateBlt3dModeCard("◩", "Tham số"', "parameter placeholder anchor", errors)
    require(legacy, 'CreateBlt3dModeCard("▣", "Solid3D"', "Solid3D placeholder anchor", errors)

    if not PATCH.is_file():
        errors.append(f"missing vector-mode presentation patch: {PATCH.relative_to(ROOT)}")
        patch = ""
    else:
        patch = PATCH.read_text(encoding="utf-8")

    for needle, label in (
        ("RegisterBlt3dFamilyModeVectorIconsBootstrap", "class-handler bootstrap"),
        ("DispatcherPriority.ContextIdle", "post-chooser dispatcher ordering"),
        ('FindModeCardButton("Tham số")', "parameter card lookup"),
        ('FindModeCardButton("Solid3D")', "Solid3D card lookup"),
        ("Blt3dVectorIcon.ApplyModeCard(parameterButton, Blt3dVectorIcon.Parameter, 30d)", "parameter vector application"),
        ("Blt3dVectorIcon.ApplyModeCard(solid3dButton, Blt3dVectorIcon.Solid3D, 30d)", "Solid3D vector application"),
    ):
        require(patch, needle, label, errors)

    for needle, label in (
        ("internal const string Parameter =", "parameter vector geometry"),
        ("internal const string Solid3D =", "Solid3D vector geometry"),
        ("internal static void ApplyModeCard(Button? button, string geometryData", "mode-card vector renderer"),
        ("Shape.StrokeProperty", "foreground-bound vector stroke"),
    ):
        require(icons, needle, label, errors)

    if errors:
        print("ERROR: BLT3D Family mode vector-icon preflight failed closed:")
        for error in errors:
            print(f" - {error}")
        return 1

    print("PASS: BLT3D Family mode cards replace font glyph placeholders with bounded WPF vector icons without mutating #4586 source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
