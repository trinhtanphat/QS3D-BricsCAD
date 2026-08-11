#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
AUDIT = UI / "AuditLogWindow.xaml"
HEALTH = UI / "ModelHealthWindow.xaml"
errors = []

for path in (AUDIT, HEALTH):
    if not path.is_file():
        errors.append("missing diagnostics UI surface: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    try:
        ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append(path.name + " is not well-formed XML: " + str(exc))
        continue

    for token in (
        '<ResourceDictionary Source="Theme.xaml"/>',
        'Style="{StaticResource PremiumCard}"',
        'Style="{StaticResource StatusPill}"',
        'Background="{StaticResource BgRaisedBrush}"',
        'Background="{StaticResource LuxuryBrush}"',
    ):
        if token not in text:
            errors.append(path.name + " missing premium/luxury hierarchy contract: " + token)

    for forbidden in (
        "DropShadowEffect",
        "BlurEffect",
        "Storyboard",
        "BeginStoryboard",
        'Margin="-',
        "SendStringToExecute",
        "ProjectContextCoordinator",
    ):
        if forbidden in text:
            errors.append(path.name + " contains forbidden presentation/behavior token: " + forbidden)

if AUDIT.is_file():
    audit = AUDIT.read_text(encoding="utf-8")
    for token in (
        'x:Name="SearchBox"',
        'TextChanged="OnSearchChanged"',
        'x:Name="Grid"',
        'IsReadOnly="True"',
        'x:Name="Summary"',
        'Header="Thời gian UTC"',
        'Header="Hành động"',
        'Header="Element"',
        'Header="Nội dung"',
        'Header="Người thực hiện"',
        'Header="Correlation"',
        'Text="READ ONLY"',
        'Text="DÒNG SỰ KIỆN"',
        'Text="MỚI NHẤT HIỂN THỊ TRƯỚC"',
    ):
        if token not in audit:
            errors.append("Audit Log contract missing: " + token)

if HEALTH.is_file():
    health = HEALTH.read_text(encoding="utf-8")
    for token in (
        'x:Name="SummaryText"',
        'x:Name="SearchBox"',
        'TextChanged="OnFilterChanged"',
        'x:Name="SeverityCombo"',
        'SelectionChanged="OnFilterChanged"',
        'x:Name="VisibleCountText"',
        'x:Name="IssueGrid"',
        'MouseDoubleClick="OnGridDoubleClick"',
        'Click="OnLocateClick"',
        'IsReadOnly="True"',
        'Tag="All"',
        'Tag="Error"',
        'Tag="Warning"',
        'Tag="Info"',
        'Text="READ-ONLY TRIAGE"',
        'Text="DANH SÁCH VẤN ĐỀ"',
        'Text="ISSUE → CAD LOCATE"',
    ):
        if token not in health:
            errors.append("Model Health contract missing: " + token)

if errors:
    print("Diagnostics luxury UI preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Diagnostics luxury UI preflight PASS: Audit Log and Model Health retain read-only/filter/locate wiring while using the shared premium card, raised-surface, status-pill and restrained luxury hierarchy without heavy effects.")
