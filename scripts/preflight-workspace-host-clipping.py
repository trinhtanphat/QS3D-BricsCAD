#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.HostClipping.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
errors = []

for path in (PARTIAL, XAML):
    if not path.is_file():
        errors.append("missing Workspace host-clipping dependency: " + str(path.relative_to(ROOT)))

if PARTIAL.is_file():
    partial = PARTIAL.read_text(encoding="utf-8")
    for token in (
        "public partial class WorkspacePanel",
        "RegisterHostClippingClassHandler()",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "ApplyHostClippingBoundary()",
        "ClipToBounds = true;",
        "WorkspaceOverflow.ClipToBounds = true;",
    ):
        if token not in partial:
            errors.append("Workspace host-clipping guard missing: " + token)

    for forbidden in (
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "SemanticCaptureService",
        "Viewport3D",
        "OnPickRoomClick(",
        "OnAddFinishClick(",
    ):
        if forbidden in partial:
            errors.append("Workspace host-clipping guard must remain presentation-only: " + forbidden)

if XAML.is_file():
    xaml = XAML.read_text(encoding="utf-8")
    for token in (
        'x:Name="WorkspaceOverflow"',
        'HorizontalScrollBarVisibility="Auto"',
        'VerticalScrollBarVisibility="Disabled"',
        'CanContentScroll="False"',
        'PanningMode="HorizontalOnly"',
        'x:Name="WorkspaceContentRoot"',
        'MinWidth="560"',
        'Content="Chọn phòng"',
        'Click="OnPickRoomClick"',
    ):
        if token not in xaml:
            errors.append("Workspace overflow/room-action contract missing: " + token)

if errors:
    print("Workspace host-clipping preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print(
    "Workspace host-clipping preflight PASS: the wider logical Workspace remains horizontally scrollable, "
    "while both the PaletteSet WPF host surface and WorkspaceOverflow explicitly clip descendant rendering."
)
