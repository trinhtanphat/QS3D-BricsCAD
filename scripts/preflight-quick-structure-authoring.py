#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-STRUCTURE-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, HUB, DOC):
    if not path.is_file():
        errors.append("missing quick-structure dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    names = (
        "QS3DDRAWWALL", "QS3DDRAWWALLADV",
        "QS3DDRAWBEAM", "QS3DDRAWBEAMADV",
        "QS3DDRAWSLAB", "QS3DDRAWSLABADV",
        "QS3DDRAWCOLUMN", "QS3DDRAWCOLUMNADV",
    )
    for name in names:
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    if 'element.Properties[' in text:
        errors.append("Direct Draw P0 configure callbacks must use ProjectElement.SetProperty, not direct element.Properties writes")
    if "element.MarkDirty(ElementDirtyFlags.Properties)" in text:
        errors.append("Direct Draw P0 must rely on SetProperty dirty/invalidation semantics, not manual Properties-only dirty flags")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var defaultsProject)" in text:
        errors.append("Direct Draw P0 Family defaults must use DirectDrawProjectPreviewContext instead of an unguarded read-only snapshot")

    freshness_counts = {
        "DirectDrawProjectPreviewContext.Capture(document)": 8,
        "var defaultsProject = projectPreview.DefaultsProject;": 8,
        "var hasDefaultsProject = projectPreview.HasProject;": 8,
        "projectPreview);": 8,
    }
    for token, expected in freshness_counts.items():
        actual = text.count(token)
        if actual != expected:
            errors.append("Direct Draw P0 preview freshness contract mismatch for " + token + ": expected " + str(expected) + ", found " + str(actual))

    canonical_setter_counts = {
        'element.SetProperty("WidthM"': 4,
        'element.SetProperty("DepthM"': 2,
        'element.SetProperty("ThicknessM"': 4,
        'element.SetProperty("HeightM"': 6,
        'element.SetProperty("BottomOffsetM"': 8,
    }
    for token, expected in canonical_setter_counts.items():
        actual = text.count(token)
        if actual != expected:
            errors.append("Direct Draw P0 canonical setter count mismatch for " + token + ": expected " + str(expected) + ", found " + str(actual))

    markers = [
        ('[CommandMethod("QS3DDRAWBEAM", CommandFlags.Modal)]', '[CommandMethod("QS3DDRAWBEAMADV", CommandFlags.Modal)]', "beam quick"),
        ('[CommandMethod("QS3DDRAWBEAMADV", CommandFlags.Modal)]', '[CommandMethod("QS3DDRAWSLAB", CommandFlags.Modal)]', "beam advanced"),
        ('[CommandMethod("QS3DDRAWSLAB", CommandFlags.Modal)]', '[CommandMethod("QS3DDRAWSLABADV", CommandFlags.Modal)]', "slab quick"),
        ('[CommandMethod("QS3DDRAWSLABADV", CommandFlags.Modal)]', '[CommandMethod("QS3DDRAWCOLUMN", CommandFlags.Modal)]', "slab advanced"),
        ('[CommandMethod("QS3DDRAWCOLUMN", CommandFlags.Modal)]', '[CommandMethod("QS3DDRAWCOLUMNADV", CommandFlags.Modal)]', "column quick"),
        ('[CommandMethod("QS3DDRAWCOLUMNADV", CommandFlags.Modal)]', 'private static void ExecuteDirect(', "column advanced"),
    ]
    slices = {}
    for start_token, end_token, label in markers:
        start = text.find(start_token)
        end = text.find(end_token, start + 1)
        if start < 0 or end < 0 or end <= start:
            errors.append(label + " command slice is missing")
        else:
            slices[label] = text[start:end]

    quick_requirements = {
        "beam quick": (
            'AcquireFixedPath(document, "Dầm nhanh", 2)',
            'FamilyNumber(defaultsProject!, ElementCategory.Beam, "WidthM", 0.3d)',
            'FamilyNumber(defaultsProject!, ElementCategory.Beam, "HeightM", 0.5d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.Beam, "BottomOffsetM", 0d)',
            '() => CreateLine(document, points[0], points[1])',
            'QS3DDRAWBEAMADV',
        ),
        "slab quick": (
            'AcquirePath(document, "Sàn nhanh", minimumPoints: 3, close: true)',
            'FamilyNumber(defaultsProject!, ElementCategory.Slab, "ThicknessM", 0.12d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.Slab, "BottomOffsetM", 0d)',
            '() => CreatePolyline(document, points, true)',
            'QS3DDRAWSLABADV',
        ),
        "column quick": (
            'new PromptPointOptions("\\nChọn tâm Cột nhanh: ")',
            'FamilyNumber(defaultsProject!, ElementCategory.Column, "WidthM", 0.4d)',
            'FamilyNumber(defaultsProject!, ElementCategory.Column, "DepthM", 0.4d)',
            'FamilyNumber(defaultsProject!, ElementCategory.Column, "HeightM", 3.6d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.Column, "BottomOffsetM", 0d)',
            'CreateColumnFootprint(document, centerResult.Value, widthM, depthM)',
            'QS3DDRAWCOLUMNADV',
        ),
    }
    for label, requirements in quick_requirements.items():
        body = slices.get(label, "")
        for token in requirements:
            if token not in body:
                errors.append(label + " missing: " + token)
        for forbidden in ("PromptPositiveMeters(", "PromptFiniteMeters("):
            if forbidden in body:
                errors.append(label + " must not require numeric prompts: " + forbidden)
        if "ExecuteDirect(" not in body:
            errors.append(label + " must reuse ExecuteDirect")
        if "element.SetProperty(" not in body:
            errors.append(label + " must configure semantic overrides through ProjectElement.SetProperty")
        if "DirectDrawProjectPreviewContext.Capture(document)" not in body or "projectPreview);" not in body:
            errors.append(label + " must carry guarded project preview freshness through ExecuteDirect")

    advanced_requirements = {
        "beam advanced": (
            'Guard(document, "QS3DDRAWBEAMADV"',
            'PromptPositiveMeters(document.Editor, "Bề rộng Dầm (m)"',
            'PromptPositiveMeters(document.Editor, "Chiều cao Dầm (m)"',
            'PromptFiniteMeters(document.Editor, "Offset đáy Dầm so với Z source (m)"',
        ),
        "slab advanced": (
            'Guard(document, "QS3DDRAWSLABADV"',
            'AcquirePath(document, "Sàn tùy chỉnh", minimumPoints: 3, close: true)',
            'PromptPositiveMeters(document.Editor, "Bề dày Sàn (m)"',
            'PromptFiniteMeters(document.Editor, "Offset đáy Sàn so với Z source (m)"',
        ),
        "column advanced": (
            'Guard(document, "QS3DDRAWCOLUMNADV"',
            'PromptPositiveMeters(document.Editor, "Bề rộng Cột (m)"',
            'PromptPositiveMeters(document.Editor, "Bề sâu Cột (m)"',
            'PromptPositiveMeters(document.Editor, "Chiều cao Cột (m)"',
            'PromptFiniteMeters(document.Editor, "Offset đáy Cột so với Z source (m)"',
        ),
    }
    for label, requirements in advanced_requirements.items():
        body = slices.get(label, "")
        for token in requirements:
            if token not in body:
                errors.append(label + " missing: " + token)
        if "ExecuteDirect(" not in body:
            errors.append(label + " must reuse ExecuteDirect")
        if "element.SetProperty(" not in body:
            errors.append(label + " must configure semantic overrides through ProjectElement.SetProperty")
        if "DirectDrawProjectPreviewContext.Capture(document)" not in body or "projectPreview);" not in body:
            errors.append(label + " must carry guarded project preview freshness through ExecuteDirect")

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    for token in (
        'Button("Vẽ Dầm", "QS3DDRAWBEAM")',
        'Button("Vẽ Cột", "QS3DDRAWCOLUMN")',
        'Button("Vẽ Sàn", "QS3DDRAWSLAB")',
    ):
        if token not in text:
            errors.append("primary quick structure Ribbon wiring missing: " + token)

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for command in ("QS3DDRAWBEAM", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB"):
        if 'Tag="' + command + '"' not in text:
            errors.append("Domain Hub missing primary quick command: " + command)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWBEAMADV",
        "QS3DDRAWSLABADV",
        "QS3DDRAWCOLUMNADV",
        "LOCAL-008",
        "ExecuteDirect",
        "Family / Type",
    ):
        if token not in text:
            errors.append("quick-structure documentation missing: " + token)

if errors:
    print("Quick structure authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick structure authoring preflight PASS")
