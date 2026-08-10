#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

paths = {
    "service": ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs",
    "command": ROOT / "src/QS3D.BricsCAD.V25/Commands.cs",
    "smoke": ROOT / "tests/QS3D.Core.SmokeTests/ComprehensiveModelHealthSmoke.cs",
}
for path in paths.values():
    if not path.is_file():
        errors.append("missing comprehensive-health file: " + str(path.relative_to(ROOT)))

service_tokens = (
    "new ModelHealthService().Inspect",
    "new RoomFinishHealthService().Inspect",
    "new DependencyHealthService().Inspect",
    "new LevelReferenceHealthService().Inspect",
    "new GeneratedHandleOwnershipHealthService().Inspect",
    "new GeneratedRebarOwnershipHealthService().Inspect",
    "new GeneratedGeometryStaleHealthService().Inspect",
    "new GeneratedRebarModeHealthService().Inspect",
    "new RebarFabricationQualificationHealthService().Inspect",
    "new GeneratedRebarHealthService().InspectAll",
    "new GeneratedTieRebarHealthService().Inspect",
    "new GeneratedBeamStirrupHealthService().Inspect",
    "new GeneratedSlabMeshHealthService().Inspect",
    "new GeneratedWallMeshHealthService().Inspect",
    "new GeneratedFoundationMeshHealthService().Inspect",
    "new GeneratedCurtainFrameHealthService().Inspect",
    "public static bool TargetsGeneratedOutput(ModelHealthIssue issue)",
    'code.IndexOf("GENERATED", StringComparison.OrdinalIgnoreCase)',
    '"SHAPE_REBAR"',
    '"TIE_REBAR"',
    '"BEAM_STIRRUP"',
    '"SLAB_MESH"',
    '"WALL_MESH"',
    '"FOUNDATION_MESH"',
    '"CURTAIN_FRAME"',
)
if paths["service"].is_file():
    text = paths["service"].read_text(encoding="utf-8")
    for token in service_tokens:
        if token not in text:
            errors.append("ComprehensiveModelHealthService.cs missing diagnostic/locate stage: " + token)
    if 'code.EndsWith("_STALE"' not in text:
        errors.append("Comprehensive health must de-duplicate repeated stale diagnostics by code/element.")

if paths["command"].is_file():
    text = paths["command"].read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DHEALTH"',
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "Cad.CadHandleService.GetLiveSolidHandles(doc, generatedHandles)",
        "new ComprehensiveModelHealthService().Inspect(project, liveSources, liveGeneratedSolids)",
        "ComprehensiveModelHealthService.TargetsGeneratedOutput(issue)",
        "GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element)",
    ):
        if token not in text:
            errors.append("Commands.cs missing comprehensive-health token: " + token)
    health = text[text.find('CommandMethod("QS3DHEALTH"'):text.find('CommandMethod("QS3DLOCATE"')]
    if 'issue.Code.IndexOf("GENERATED"' in health:
        errors.append("QS3DHEALTH locate still guesses generated issue ownership from the literal GENERATED substring.")
    if "ParseGeneratedRebarHandles" in text:
        errors.append("Commands.cs still contains the legacy single-slot generated rebar health helper.")
    if 'TryGetValue("GeneratedSolidHandle", out var handle)' in health:
        errors.append("QS3DHEALTH still manually scopes live generated geometry to GeneratedSolidHandle.")

if paths["smoke"].is_file():
    text = paths["smoke"].read_text(encoding="utf-8")
    for token in (
        "GeneratedRebarHandles",
        "GeneratedShapeRebarHandles",
        "GeneratedTieRebarHandles",
        "GeneratedBeamStirrupHandles",
        "GeneratedSlabMeshHandles",
        "GeneratedWallMeshHandles",
        "GeneratedFoundationMeshHandles",
        "GeneratedCurtainFrameHandles",
        'HasCode(issues, "DEPENDENCY_CYCLE")',
        'HasCode(issues, "GENERATED_SOLID_STALE")',
        'HasCode(issues, "TOP_LEVEL_REQUIRES_BOTTOM_LEVEL")',
        'HasCode(issues, "UNLINKED_ROOM_FINISH")',
        'HasCode(issues, "REBAR_FAB_OUTPUT_MISSING")',
        "CoversGeneratedLocateTargetClassification",
        '"CURTAIN_FRAME_COUNT_INVALID"',
        '"TIE_REBAR_CATEGORY_MISMATCH"',
        '"MISSING_FAMILY"',
    ):
        if token not in text:
            errors.append("ComprehensiveModelHealthSmoke.cs missing coverage token: " + token)

print("QS3D comprehensive model-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: QS3DHEALTH comprehensive Core health includes semantic/generated diagnostics and classifies generated-subsystem issues for generated CAD locate without literal-code guessing in the command.")
