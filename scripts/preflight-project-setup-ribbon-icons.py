#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_REL = "src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs"
V26_REL = "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing Project Setup ribbon icon contract: {needle}")


def main():
    project = read(PROJECT_REL)
    v26 = read(V26_REL)

    for needle in (
        'new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\\ndự án", "QS3DPROJECTINFO", RibbonIconKind.Qs3dLogo)',
        'new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\\ntầng", "QS3DLEVELS", RibbonIconKind.Structure)',
        'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTPROPERTIES", RibbonIconKind.Settings)',
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", CreateIcon(spec.Icon, 16));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Icon, 32));',
        'private static object CreateIcon(RibbonIconKind icon, int pixelSize)',
        'return Qs3dBrandIconFactory.Create(pixelSize);',
        'thread.CurrentCulture = CultureInfo.InvariantCulture;',
        'return RibbonIconFactory.Create(icon, pixelSize);',
        'thread.CurrentCulture = previousCulture;',
    ):
        require(project, needle, PROJECT_REL)

    loop_start = project.index("foreach (var spec in BltButtons)")
    loop_end = project.index("_initialized = true;", loop_start)
    visible_loop = project[loop_start:loop_end]
    if 'SetProperty(button, "ShowImage", false);' in visible_loop:
        raise SystemExit("FAIL: visible Project Setup buttons must never revert to text-only/missing-image mode")

    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', V26_REL)

    print(
        "PASS: THIẾT LẬP DỰ ÁN creates explicit 16/32 px deterministic icons for "
        "Thông tin dự án, Cài đặt tầng and Thuộc tính dự án, with V26 inheriting the same source."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
