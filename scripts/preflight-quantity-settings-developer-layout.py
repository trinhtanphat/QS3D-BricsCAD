#!/usr/bin/env python3
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml.cs"

FIELDS = [
    "FormworkToleranceBox",
    "BlindingConcreteOffsetBox",
    "MinSubtractAreaBox",
    "MinFormworkAreaBox",
    "MinConcreteVolumeBox",
    "EngulfRelPercentBox",
    "EngulfMinAreaBox",
    "RoomGapFillBox",
    "RoomSearchRadiusBox",
    "DimColorBox",
    "DimTextHeightBox",
]


def main():
    try:
        ET.parse(str(XAML))
    except ET.ParseError as exc:
        print("ERROR: QuantitySettingsWindow.xaml is not well-formed XML:", exc)
        return 1

    xaml = XAML.read_text(encoding="utf-8")
    code = CODE.read_text(encoding="utf-8")

    for field in FIELDS:
        token = 'x:Name="' + field + '"'
        count = xaml.count(token)
        if count != 1:
            print("ERROR:", field, "must appear exactly once in XAML; found", count)
            return 1
        if field not in code:
            print("ERROR:", field, "is no longer consumed by QuantitySettingsWindow code-behind.")
            return 1

    groups = [
        "Thông số engine chung",
        "Ngưỡng lọc khối lượng",
        "Ngưỡng ‘nuốt’ tấm trừ",
        "Pick Room (pick biên phòng trong View 3D)",
        "Nhãn kích thước (Dim) khi Diễn giải khối lượng",
    ]
    missing_groups = [group for group in groups if group not in xaml]
    if missing_groups:
        print("ERROR: developer settings grouping is incomplete:")
        for group in missing_groups:
            print(" -", group)
        return 1

    required = [
        "Các ngưỡng nội bộ của engine tính khối lượng — chỉ chỉnh khi hiểu rõ tác động.",
        'Background="{Binding Text, ElementName=DimColorBox}"',
        'x:Name="PrimaryCategoryList"',
        'x:Name="ReferenceCategoryList"',
        'x:Name="SelectedRuleEditor"',
        'Click="ViewReverseRule_Click"',
        'ItemsSource="{Binding IntersectionCategoryChoices}"',
    ]
    missing = [token for token in required if token not in xaml]
    if missing:
        print("ERROR: developer layout or completed intersection-browser contract was lost:")
        for token in missing:
            print(" -", token)
        return 1

    build_start = code.find("private QuantityCalculationSettings BuildSettingsFromView()")
    build_end = code.find("private void RebuildIntersectionBrowser()", build_start)
    build = code[build_start:build_end]
    for field in FIELDS:
        if field not in build:
            print("ERROR:", field, "is no longer persisted by BuildSettingsFromView().")
            return 1

    print("PASS: developer settings are grouped for screenshot parity while all eleven persisted controls and the directed Intersection Rules browser remain intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
