#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-WALL-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, HUB, DOC):
    if not path.is_file():
        errors.append("missing quick-wall dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    quick_start = text.find('[CommandMethod("QS3DDRAWWALL", CommandFlags.Modal)]')
    advanced_start = text.find('[CommandMethod("QS3DDRAWWALLADV", CommandFlags.Modal)]')
    beam_start = text.find('[CommandMethod("QS3DDRAWBEAM", CommandFlags.Modal)]')
    if min(quick_start, advanced_start, beam_start) < 0 or not (quick_start < advanced_start < beam_start):
        errors.append("quick/advanced wall command ordering is missing or invalid")
    else:
        quick = text[quick_start:advanced_start]
        advanced = text[advanced_start:beam_start]

        for token in (
            'Guard(document, "QS3DDRAWWALL"',
            'AcquireFixedPath(document, "Tường nhanh", 2)',
            'DirectDrawProjectPreviewContext.Capture(document)',
            'var defaultsProject = projectPreview.DefaultsProject;',
            'FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "ThicknessM", 0.2d)',
            'FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "HeightM", 3.6d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "BottomOffsetM", 0d)',
            '() => CreateLine(document, points[0], points[1])',
            'ExecuteDirect(',
            'projectPreview);',
            'element.SetProperty("ThicknessM"',
            'element.SetProperty("HeightM"',
            'element.SetProperty("BottomOffsetM"',
            'QS3DDRAWWALLADV',
        ):
            if token not in quick:
                errors.append("QS3DDRAWWALL quick path missing: " + token)

        for forbidden in (
            'AcquirePath(document',
            'PromptPositiveMeters(',
            'PromptFiniteMeters(',
            'CreatePolyline(document',
        ):
            if forbidden in quick:
                errors.append("QS3DDRAWWALL quick path must not require advanced interaction: " + forbidden)

        for token in (
            'Guard(document, "QS3DDRAWWALLADV"',
            'AcquirePath(document, "Tường tùy chỉnh", minimumPoints: 2, close: false)',
            'PromptPositiveMeters(document.Editor, "Bề dày Tường (m)"',
            'PromptPositiveMeters(document.Editor, "Chiều cao Tường (m)"',
            'PromptFiniteMeters(document.Editor, "Offset đáy Tường so với Z source (m)"',
            'points.Count == 2 ? CreateLine(document, points[0], points[1]) : CreatePolyline(document, points, false)',
            'ExecuteDirect(',
        ):
            if token not in advanced:
                errors.append("QS3DDRAWWALLADV must preserve advanced wall authoring: " + token)

    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in ("QS3DDRAWWALL", "QS3DDRAWWALLADV"):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    if 'Button("Vẽ Tường", "QS3DDRAWWALL")' not in text:
        errors.append("TẠO MỚI Ribbon primary Vẽ Tường must continue to invoke QS3DDRAWWALL")

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    if 'Tag="QS3DDRAWWALL"' not in text:
        errors.append("Full Domain Hub must continue to expose QS3DDRAWWALL")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "pick point 1",
        "pick point 2",
        "QS3DDRAWWALLADV",
        "LOCAL-008",
        "SemanticCaptureService",
        "ProjectStateSnapshot",
    ):
        if token not in text:
            errors.append("quick-wall documentation missing: " + token)

if errors:
    print("Quick Wall authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick Wall authoring preflight PASS")
