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
        errors.append("missing active-family draw dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for command in ("QS3DDRAWACTIVE", "QS3DDRAWACTIVEADV"):
        if len(re.findall(r'CommandMethod\("' + command + r'"', text)) != 1:
            errors.append(command + " must be declared exactly once")

    for token in (
        'DrawActiveFamilyCore(advanced: false, operation: "QS3DDRAWACTIVE")',
        'DrawActiveFamilyCore(advanced: true, operation: "QS3DDRAWACTIVEADV")',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectFamilyActivationService.GetActive(project)",
        "Dispatch(document, family, advanced, operation)",
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
        "new DirectDrawCommands().DrawWallAdvanced()",
        "new DirectDrawCommands().DrawBeamAdvanced()",
        "new DirectDrawCommands().DrawColumnAdvanced()",
        "new DirectDrawCommands().DrawSlabAdvanced()",
        "new DirectDrawP1Commands().DrawGlassWallAdvanced()",
        "new DirectDrawP1Commands().DrawWallPierAdvanced()",
        "new DirectDrawP1Commands().DrawStructuralWallAdvanced()",
        "new DirectDrawP1Commands().DrawFoundationAdvanced()",
        "new DirectDrawOpeningCommands().DrawDoorAdvanced()",
        "new DirectDrawOpeningCommands().DrawWallOpeningAdvanced()",
        "new DirectDrawWindowCommands().DrawWindowAdvanced()",
        'family.Properties.TryGetValue("OpeningUsage"',
        'family.Properties.ContainsKey("WindowHeightM")',
        'family.Properties.ContainsKey("WindowSillHeightM")',
    ):
        if token not in text:
            errors.append("active-family draw missing: " + token)

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext.Require(document",
        "SemanticCaptureService.Capture",
        "RegenerationEngine",
        "WallSolidBuilder",
        "StructuralSolidBuilder",
    ):
        if forbidden in text:
            errors.append("active-family dispatcher must stay read-only and lifecycle-free: " + forbidden)

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    token = 'new ButtonSpec("QS3D_AUTHOR_DRAW_ACTIVE", "Vẽ Nhanh", "QS3DDRAWACTIVE")'
    if token not in text:
        errors.append("Quick Workflow Ribbon must expose the stable Vẽ Nhanh action")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWACTIVE",
        "QS3DDRAWACTIVEADV",
        "Family / Type",
        "non-creating",
        "OpeningUsage=Window",
        "LOCAL-008",
        "Ctrl+Shift+D",
    ):
        if token not in text:
            errors.append("active-family draw documentation missing: " + token)

if errors:
    print("Active-family draw preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Active-family draw preflight PASS")
