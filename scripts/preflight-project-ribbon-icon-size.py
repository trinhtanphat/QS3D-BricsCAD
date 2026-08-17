#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "ProjectRibbonAugmenter.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        'new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\\ndự án", "QS3DPROJECTINFO", RibbonIconKind.Qs3dLogo)',
        'new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\\ntầng", "QS3DLEVELS", RibbonIconKind.Structure)',
        'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTPROPERTIES", RibbonIconKind.Settings)',
        'SetProperty(button, "Image", CreateIcon(spec.Icon, 16));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Icon, 32));',
        'SetEnumProperty(button, "Size", "Large");',
        'private static void SetEnumProperty(object target, string name, string value)',
        'property.PropertyType.IsEnum',
        'Enum.Parse(property.PropertyType, value, true)',
    )
    missing = [token for token in required if token not in text]
    if missing:
        for token in missing:
            print("ERROR: project Ribbon icon-size contract missing:", token)
        return 1

    if text.count('SetEnumProperty(button, "Size", "Large");') != 1:
        print("ERROR: BLT project setup must have exactly one shared native Large-size assignment inside its button loop.")
        return 1

    print(
        "PASS: THIẾT LẬP DỰ ÁN keeps 16 px fallback images, supplies 32 px LargeImage assets, "
        "and requests BricsCAD's native Large Ribbon item layout for all three project buttons."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
