#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RevisionWindow.xaml"
errors = []

if not XAML.is_file():
    errors.append("missing RevisionWindow.xaml")
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RevisionWindow.xaml is not well-formed XML: " + str(exc))

    required = (
        '<ResourceDictionary Source="Theme.xaml"/>',
        'Style="{StaticResource PremiumCard}"',
        'Style="{StaticResource StatusPill}"',
        'Background="{StaticResource BgRaisedBrush}"',
        'Background="{StaticResource LuxuryBrush}"',
        'x:Name="Header"',
        'x:Name="Tabs"',
        'x:Name="Grid"',
        'x:Name="SemanticGrid"',
        'x:Name="Totals"',
        'Click="OnLocateClick"',
        'MouseDoubleClick="OnGridDoubleClick"',
        'MouseDoubleClick="OnSemanticGridDoubleClick"',
        'Header="Khối lượng"',
        'Header="Ngữ nghĩa"',
        'Text="READ-ONLY REVIEW"',
        'Text="REVISION REVIEW"',
        'Text="DOUBLE-CLICK ROW TO LOCATE"',
        'Binding="{Binding ElementId}"',
        'Binding="{Binding Category}"',
        'Binding="{Binding QuantityName}"',
        'Binding="{Binding Change}"',
        'Binding="{Binding Before, StringFormat=0.###}"',
        'Binding="{Binding After, StringFormat=0.###}"',
        'Binding="{Binding Delta, StringFormat=0.###}"',
        'Binding="{Binding PercentChange, StringFormat=0.##}"',
        'Binding="{Binding IdentityChangeCount}"',
        'Binding="{Binding PropertyChangeCount}"',
        'Binding="{Binding QuantityChangeCount}"',
        'Binding="{Binding OmittedSourceReferenceChangeCount}"',
    )
    for token in required:
        if token not in text:
            errors.append("Revision luxury/read-only contract missing: " + token)

    if text.count('IsReadOnly="True"') < 2:
        errors.append("both Revision grids must remain read-only")

    for forbidden in (
        "DropShadowEffect",
        "BlurEffect",
        "Storyboard",
        "BeginStoryboard",
        'Margin="-',
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "GetOrCreate",
        "Save(",
        "Apply(",
    ):
        if forbidden in text:
            errors.append("Revision luxury lane contains forbidden presentation/behavior token: " + forbidden)

if errors:
    print("Revision luxury UI preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Revision luxury UI preflight PASS: quantity/semantic comparison stays fully read-only and locate-only while using the shared raised, premium-card, status-pill and restrained luxury hierarchy without heavy effects.")
