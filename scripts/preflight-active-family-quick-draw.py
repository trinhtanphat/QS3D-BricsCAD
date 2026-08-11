#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs"
DOC = ROOT / "docs/DIRECT-DRAW-ACTIVE-FAMILY.md"
errors = []

for path in (SOURCE, RIBBON, DOC):
    if not path.is_file():
        errors.append("missing active-family quick-draw dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    if len(re.findall(r'CommandMethod\("QS3DDRAWACTIVE"', text)) != 1:
        errors.append("QS3DDRAWACTIVE must be declared exactly once")

    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectFamilyActivationService.GetActive(project)",
        "new DirectDrawCommands().DrawWall()",
        "new DirectDrawCommands().DrawBeam()",
        "new DirectDrawCommands().DrawColumn()",
        "new DirectDrawCommands().DrawSlab()",
        "new DirectDrawP1Commands().DrawGlassWall()",
        "new DirectDrawP1Commands().DrawWallPier()",
        "new DirectDrawP1Commands().DrawStructuralWall()",
        "new DirectDrawP1Commands().DrawFoundation()",
        "new DirectDrawOpeningCommands().DrawDoor()",
        "new DirectDrawOpeningCommands().DrawWallOpening()",
        "new DirectDrawWindowCommands().DrawWindow()",
        'family.Properties.TryGetValue("OpeningUsage"',
        'family.Properties.ContainsKey("WindowHeightM")',
        'family.Properties.ContainsKey("WindowSillHeightM")',
    ):
        if token not in text:
            errors.append("active-family quick draw missing: " + token)

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext.Require(document",
        "DrawWallAdvanced()",
        "DrawBeamAdvanced()",
        "DrawColumnAdvanced()",
        "DrawSlabAdvanced()",
        "DrawGlassWallAdvanced()",
        "DrawWallPierAdvanced()",
        "DrawStructuralWallAdvanced()",
        "DrawFoundationAdvanced()",
        "DrawDoorAdvanced()",
        "DrawWallOpeningAdvanced()",
        "DrawWindowAdvanced()",
    ):
        if forbidden in text:
            errors.append("active-family dispatcher must stay read-only and primary-quick only: " + forbidden)

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    token = 'new ButtonSpec("QS3D_AUTHOR_DRAW_ACTIVE", "Vẽ Nhanh", "QS3DDRAWACTIVE")'
    if token not in text:
        errors.append("Quick Workflow Ribbon must expose the stable Vẽ Nhanh action")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWACTIVE",
        "Family / Type",
        "non-creating",
        "OpeningUsage=Window",
        "LOCAL-008",
        "*ADV",
    ):
        if token not in text:
            errors.append("active-family quick-draw documentation missing: " + token)

if errors:
    print("Active-family quick draw preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Active-family quick draw preflight PASS")
