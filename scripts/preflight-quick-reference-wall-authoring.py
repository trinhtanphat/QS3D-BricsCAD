#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawReferenceWallCommands.cs"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-REFERENCE-WALL-2026-08-11.md"
errors = []

for path in (SOURCE, DOC):
    if not path.is_file():
        errors.append("missing quick-reference-wall dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in ("QS3DDRAWWALLREF", "QS3DDRAWWALLREFADV"):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    for token in (
        'DrawWallFromReferenceCore(promptParameters: false, operation: "QS3DDRAWWALLREF")',
        'DrawWallFromReferenceCore(promptParameters: true, operation: "QS3DDRAWWALLREFADV")',
        "var lengthM = reference.LengthM;",
        'FamilyNumber(defaultsProject, ElementCategory.ArchitecturalWall, "ThicknessM", 0.2d)',
        'FamilyNumber(defaultsProject, ElementCategory.ArchitecturalWall, "HeightM", 3.6d)',
        'FamilyFiniteNumber(defaultsProject, ElementCategory.ArchitecturalWall, "BottomOffsetM", 0d)',
        "if (promptParameters)",
        'PromptPositiveMeters(document.Editor, "Chiều dài Tường (m)", lengthM)',
        'PromptPositiveMeters(document.Editor, "Bề dày Tường (m)", thicknessM)',
        'PromptPositiveMeters(document.Editor, "Chiều cao Tường (m)", heightM)',
        'PromptFiniteMeters(document.Editor, "Offset đáy Tường so với Z tham chiếu (m)", bottomOffsetM)',
        "QS3DDRAWWALLREFADV",
        "reference.CreateCenteredEndpoints(document, lengthM)",
        "SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall)",
        "ProjectStateSnapshot.Capture(project)",
        ".RegenerateDirtySubset(project, new[] { createdElementId })",
        "WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall)",
        "GeneratedGeometryService.RequireMatchingOwnership",
    ):
        if token not in text:
            errors.append("quick reference-wall contract missing: " + token)

    if ".RegenerateDirty(project)" in text:
        errors.append("reference-wall authoring must not regenerate unrelated dirty project elements")

    gate = text.find("if (promptParameters)")
    else_pos = text.find("else", gate)
    endpoints = text.find("var endpoints = reference.CreateCenteredEndpoints(document, lengthM)", else_pos)
    if min(gate, else_pos, endpoints) < 0 or not (gate < else_pos < endpoints):
        errors.append("reference wall must resolve defaults -> optional advanced prompts -> endpoints")
    else:
        quick_body = text[else_pos:endpoints]
        if "PromptPositiveMeters(" in quick_body or "PromptFiniteMeters(" in quick_body:
            errors.append("primary reference-wall quick branch must not prompt numeric parameters")
        if "QS3DDRAWWALLREFADV" not in quick_body:
            errors.append("primary reference-wall status must advertise QS3DDRAWWALLREFADV")

    reference_start = text.find("private static ReferenceLinePlan? AcquireReferenceLine")
    execute_start = text.find("private static void Execute(")
    if reference_start < 0 or execute_start < 0 or reference_start > execute_start:
        errors.append("reference acquisition/execute lifecycle is missing")
    else:
        reference_body = text[reference_start:execute_start]
        if "as Line" not in reference_body or "OpenMode.ForRead" not in reference_body:
            errors.append("reference LINE must remain read-only input")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWWALLREFADV",
        "reference LINE",
        "Family / Type",
        "LOCAL-008",
        "read-only",
        "WallSolidBuilder",
        "operation-scoped",
    ):
        if token not in text:
            errors.append("quick reference-wall documentation missing: " + token)

if errors:
    print("Quick reference-wall authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick reference-wall authoring preflight PASS")
