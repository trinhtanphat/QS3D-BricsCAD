#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.CompactShell.cs"
errors = []

for path in (XAML, PARTIAL):
    if not path.is_file():
        errors.append("missing Workspace responsive-shell dependency: " + str(path.relative_to(ROOT)))

if XAML.is_file():
    xaml = XAML.read_text(encoding="utf-8")
    for token in (
        'x:Name="WorkspaceOverflow"',
        'x:Name="WorkspaceContentRoot"',
        'HorizontalScrollBarVisibility="Auto"',
        'HorizontalContentAlignment="Stretch"',
    ):
        if token not in xaml:
            errors.append("Workspace ScrollViewer composition missing: " + token)

if PARTIAL.is_file():
    partial = PARTIAL.read_text(encoding="utf-8")

    if "Content is Grid root" in partial:
        errors.append(
            "Workspace compact shell must not assume UserControl.Content is a Grid after WorkspaceOverflow became the content root"
        )

    if partial.count("var root = WorkspaceContentRoot;") < 2:
        errors.append(
            "Workspace compact shell must resolve the named WorkspaceContentRoot for both body and header tuning"
        )

    for token in (
        "WorkspaceOverflow.SizeChanged += OnCompactViewportSizeChanged",
        "WorkspaceOverflow.ScrollChanged += OnCompactViewportScrollChanged",
        "PinCompactChromeToViewport()",
        "chrome.Width = viewportWidth",
        "chrome.HorizontalAlignment = HorizontalAlignment.Left",
        "new TranslateTransform(horizontalOffset, 0)",
        "header.SizeChanged += OnCompactHeaderSizeChanged",
        "ApplyCompactHeaderBreakpoint(header)",
        "TuneModelSectionHeaderCollision()",
    ):
        if token not in partial:
            errors.append("Workspace responsive compact-shell guard missing: " + token)

    for forbidden in (
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "SemanticCaptureService",
        "Viewport3D",
    ):
        if forbidden in partial:
            errors.append("Workspace responsive compact-shell must remain presentation-only: " + forbidden)

if errors:
    print("Workspace ScrollViewer compact-shell preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print(
    "Workspace ScrollViewer compact-shell preflight PASS: the presentation layer targets the named content grid, "
    "keeps header/footer chrome pinned to the live palette viewport during horizontal overflow, and remains presentation-only."
)
