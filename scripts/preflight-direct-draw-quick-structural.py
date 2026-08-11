#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-STRUCTURAL-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, DOC):
    if not path.is_file():
        errors.append("missing quick-structural dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")

    ranges = (
        ("QS3DDRAWBEAM", "QS3DDRAWBEAMADV", "QS3DDRAWSLAB"),
        ("QS3DDRAWSLAB", "QS3DDRAWSLABADV", "QS3DDRAWCOLUMN"),
        ("QS3DDRAWCOLUMN", "QS3DDRAWCOLUMNADV", "private static void ExecuteDirect"),
    )
    for quick_name, advanced_name, next_marker in ranges:
        quick_start = text.find('[CommandMethod("' + quick_name + '", CommandFlags.Modal)]')
        advanced_start = text.find('[CommandMethod("' + advanced_name + '", CommandFlags.Modal)]')
        next_start = text.find('[CommandMethod("' + next_marker + '", CommandFlags.Modal)]') if not next_marker.startswith("private ") else text.find(next_marker)
        if min(quick_start, advanced_start, next_start) < 0 or not (quick_start < advanced_start < next_start):
            errors.append(quick_name + "/" + advanced_name + " command split is missing or ordered incorrectly")
            continue
        quick = text[quick_start:advanced_start]
        advanced = text[advanced_start:next_start]
        if "ExecuteDirect(" not in quick or "ExecuteDirect(" not in advanced:
            errors.append(quick_name + "/" + advanced_name + " must both use canonical ExecuteDirect")
        for forbidden in ("PromptPositiveMeters(", "PromptFiniteMeters("):
            if forbidden in quick:
                errors.append(quick_name + " quick path must not show numeric parameter prompts: " + forbidden)
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var defaultsProject)" not in quick:
            errors.append(quick_name + " quick path must read compatible Family defaults before authoring")
        if advanced.count("PromptPositiveMeters(") + advanced.count("PromptFiniteMeters(") == 0:
            errors.append(advanced_name + " must preserve numeric override prompts")

    beam_start = text.find('[CommandMethod("QS3DDRAWBEAM", CommandFlags.Modal)]')
    beam_adv = text.find('[CommandMethod("QS3DDRAWBEAMADV", CommandFlags.Modal)]')
    slab_start = text.find('[CommandMethod("QS3DDRAWSLAB", CommandFlags.Modal)]')
    slab_adv = text.find('[CommandMethod("QS3DDRAWSLABADV", CommandFlags.Modal)]')
    column_start = text.find('[CommandMethod("QS3DDRAWCOLUMN", CommandFlags.Modal)]')
    column_adv = text.find('[CommandMethod("QS3DDRAWCOLUMNADV", CommandFlags.Modal)]')
    execute_start = text.find("private static void ExecuteDirect")

    beam_quick = text[beam_start:beam_adv] if beam_start >= 0 and beam_adv > beam_start else ""
    beam_advanced = text[beam_adv:slab_start] if beam_adv >= 0 and slab_start > beam_adv else ""
    slab_quick = text[slab_start:slab_adv] if slab_start >= 0 and slab_adv > slab_start else ""
    slab_advanced = text[slab_adv:column_start] if slab_adv >= 0 and column_start > slab_adv else ""
    column_quick = text[column_start:column_adv] if column_start >= 0 and column_adv > column_start else ""
    column_advanced = text[column_adv:execute_start] if column_adv >= 0 and execute_start > column_adv else ""

    checks = (
        (beam_quick, 'FamilyNumber(defaultsProject, ElementCategory.Beam, "WidthM", 0.3d)', "Beam quick WidthM"),
        (beam_quick, 'FamilyNumber(defaultsProject, ElementCategory.Beam, "HeightM", 0.5d)', "Beam quick HeightM"),
        (beam_quick, 'FamilyFiniteNumber(defaultsProject, ElementCategory.Beam, "BottomOffsetM", 0d)', "Beam quick BottomOffsetM"),
        (beam_quick, 'QS3DDRAWBEAMADV', "Beam quick ADV hint"),
        (beam_advanced, 'PromptPositiveMeters(document.Editor, "Bề rộng Dầm (m)"', "Beam ADV WidthM prompt"),
        (beam_advanced, 'PromptPositiveMeters(document.Editor, "Chiều cao Dầm (m)"', "Beam ADV HeightM prompt"),
        (beam_advanced, 'PromptFiniteMeters(document.Editor, "Offset đáy Dầm so với Z source (m)"', "Beam ADV offset prompt"),
        (slab_quick, 'FamilyNumber(defaultsProject, ElementCategory.Slab, "ThicknessM", 0.12d)', "Slab quick ThicknessM"),
        (slab_quick, 'FamilyFiniteNumber(defaultsProject, ElementCategory.Slab, "BottomOffsetM", 0d)', "Slab quick BottomOffsetM"),
        (slab_quick, 'QS3DDRAWSLABADV', "Slab quick ADV hint"),
        (slab_advanced, 'PromptPositiveMeters(document.Editor, "Bề dày Sàn (m)"', "Slab ADV thickness prompt"),
        (slab_advanced, 'PromptFiniteMeters(document.Editor, "Offset đáy Sàn so với Z source (m)"', "Slab ADV offset prompt"),
        (column_quick, 'FamilyNumber(defaultsProject, ElementCategory.Column, "WidthM", 0.4d)', "Column quick WidthM"),
        (column_quick, 'FamilyNumber(defaultsProject, ElementCategory.Column, "DepthM", 0.4d)', "Column quick DepthM"),
        (column_quick, 'FamilyNumber(defaultsProject, ElementCategory.Column, "HeightM", 3.6d)', "Column quick HeightM"),
        (column_quick, 'FamilyFiniteNumber(defaultsProject, ElementCategory.Column, "BottomOffsetM", 0d)', "Column quick BottomOffsetM"),
        (column_quick, 'QS3DDRAWCOLUMNADV', "Column quick ADV hint"),
        (column_advanced, 'PromptPositiveMeters(document.Editor, "Bề rộng Cột (m)"', "Column ADV width prompt"),
        (column_advanced, 'PromptPositiveMeters(document.Editor, "Bề sâu Cột (m)"', "Column ADV depth prompt"),
        (column_advanced, 'PromptPositiveMeters(document.Editor, "Chiều cao Cột (m)"', "Column ADV height prompt"),
        (column_advanced, 'PromptFiniteMeters(document.Editor, "Offset đáy Cột so với Z source (m)"', "Column ADV offset prompt"),
    )
    for section, token, label in checks:
        if token not in section:
            errors.append(label + " missing")

    execute = text[execute_start:text.find("private static int BuildSelected", execute_start + 1)] if execute_start >= 0 else ""
    if ".RegenerateDirtySubset(project, new[] { createdElement.Id })" not in execute:
        errors.append("quick structural Direct Draw must retain scoped semantic regeneration")
    if ".RegenerateDirty(project)" in execute:
        errors.append("quick structural Direct Draw must not regress to whole-project regeneration")

    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in (
        "QS3DDRAWBEAM", "QS3DDRAWBEAMADV",
        "QS3DDRAWSLAB", "QS3DDRAWSLABADV",
        "QS3DDRAWCOLUMN", "QS3DDRAWCOLUMNADV",
    ):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

if RIBBON.is_file():
    ribbon = RIBBON.read_text(encoding="utf-8")
    for token in (
        'new RibbonButtonSpec("Vẽ Dầm", "QS3DDRAWBEAM")',
        'new RibbonButtonSpec("Vẽ Cột", "QS3DDRAWCOLUMN")',
        'new RibbonButtonSpec("Vẽ Sàn", "QS3DDRAWSLAB")',
    ):
        if token not in ribbon:
            errors.append("primary structural Ribbon command must remain quick: " + token)

if DOC.is_file():
    doc = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWBEAMADV",
        "QS3DDRAWSLABADV",
        "QS3DDRAWCOLUMNADV",
        "No Width / Height / BottomOffset numeric prompts",
        "No Thickness / BottomOffset numeric prompts",
        "No Width / Depth / Height / BottomOffset numeric prompts",
        "scoped semantic regeneration",
        "LOCAL_ONLY",
    ):
        if token not in doc:
            errors.append("quick structural documentation missing: " + token)

if errors:
    print("Direct Draw quick structural preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Direct Draw quick structural preflight PASS")
