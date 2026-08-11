#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RightPanel.xaml"
errors = []

if not XAML.is_file():
    errors.append("missing RightPanel.xaml")
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RightPanel.xaml is not well-formed XML: " + str(exc))

    required_visual = (
        '<ResourceDictionary Source="Theme.xaml"/>',
        'x:Key="RightBadge"',
        'BasedOn="{StaticResource StatusPill}"',
        'x:Key="RightToolbarBand"',
        'x:Key="RightListSurface"',
        'BasedOn="{StaticResource PremiumCard}"',
        'Background="{StaticResource LuxuryBrush}"',
        'Background="{StaticResource BgRaisedBrush}"',
        'Text="QUẢN LÝ BẢN VẼ"',
        'Text="QUẢN LÝ LỚP"',
    )
    for token in required_visual:
        if token not in text:
            errors.append("Right Panel luxury hierarchy missing: " + token)

    required_wiring = (
        'PreviewKeyDown="OnRightPanelPreviewKeyDown"',
        'x:Name="DrawingList"',
        'SelectionChanged="OnDrawingSelectionChanged"',
        'PreviewMouseRightButtonDown="OnDrawingListPreviewMouseRightButtonDown"',
        'x:Name="LayerSearchBox"',
        'TextChanged="OnLayerSearchChanged"',
        'x:Name="LayerList"',
        'PreviewMouseRightButtonDown="OnLayerListPreviewMouseRightButtonDown"',
        'Click="OnClearDrawingSelectionClick"',
        'Click="OnAttachXrefClick"',
        'Click="OnReloadXrefClick"',
        'Click="OnMoveDrawingClick"',
        'Click="OnLockXrefClick"',
        'Click="OnUnlockXrefClick"',
        'Click="OnZoomWindowClick"',
        'Click="OnDeleteDrawingClick"',
        'Click="OnRefreshClick"',
        'Click="OnShowLayersClick"',
        'Click="OnHideLayersClick"',
        'Click="OnLockLayersClick"',
        'Click="OnUnlockLayersClick"',
        'Click="OnInvertSelectionClick"',
        'Click="OnClearLayerSelectionClick"',
        'Checked="OnLayerChecked"',
        'Unchecked="OnLayerUnchecked"',
    )
    for token in required_wiring:
        if token not in text:
            errors.append("Right Panel behavior wiring missing: " + token)

    required_xref_state = (
        'DisplayMemberBinding="{Binding Name}"',
        'DisplayMemberBinding="{Binding LockState}"',
        'DisplayMemberBinding="{Binding InstanceText}"',
        'Header="Tỉ lệ"',
        'DisplayMemberBinding="{Binding ScaleText}"',
    )
    for token in required_xref_state:
        if token not in text:
            errors.append("Right Panel Xref display contract missing: " + token)

    required_layer_state = (
        'IsChecked="{Binding IsVisible, Mode=TwoWay}"',
        'IsChecked="{Binding IsLocked}"',
        'Background="{Binding ColorBrush}"',
        'Text="{Binding Name}"',
        'Text="{Binding ColorIndex}"',
        'SelectionMode="Extended"',
    )
    for token in required_layer_state:
        if token not in text:
            errors.append("Right Panel layer state contract missing: " + token)

    for forbidden in (
        "DropShadowEffect",
        "BlurEffect",
        "Storyboard",
        "BeginStoryboard",
        'Margin="-',
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "GetOrCreate",
    ):
        if forbidden in text:
            errors.append("Right Panel luxury lane contains forbidden presentation/behavior token: " + forbidden)

if errors:
    print("Right Panel luxury UI preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Right Panel luxury UI preflight PASS: Drawing/Xref and Layer management retain all existing actions, context/keyboard wiring and Xref scale state while using shared premium-card/status-pill/raised/champagne hierarchy without heavy effects.")
