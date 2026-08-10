#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs": [
        "SemanticHandleOwnershipResolver", "selected.Contains(handle)", "GeneratedSolidHandle",
        "PhysicalOpeningCutSolidHandle", "GeneratedRebarHandles", "GeneratedShapeRebarHandles",
        "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles", "GeneratedSlabMeshHandles",
        "GeneratedWallMeshHandles", "GeneratedCurtainFrameHandles", "ambiguously owned by semantic elements",
    ],
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs": [
        "SelectImplied", "StartOpenCloseTransaction", "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)"
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": [
        "SemanticSelectionResolver.ResolveImplied(", "floor.assign", "EnsureBoundDrawingIsActive"
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": [
        "SemanticSelectionResolver.ResolveImplied(", "material.assign", "MdiActiveDocument"
    ],
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs": [
        "ModuleInitializer", "UnrelatedAmbiguityDoesNotBlockCleanSelection", "SelectedAmbiguityIsRejected",
        "GeneratedMultiHandleResolvesOwner", 'SemanticHandleOwnershipResolver.Resolve(project, new[] { "AA" })',
        'SemanticHandleOwnershipResolver.Resolve(project, new[] { "BB" })'
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing semantic-selection file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

adapter = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs"
if adapter.is_file():
    text = adapter.read_text(encoding="utf-8")
    for stale in ("BuildOwnershipIndex(project)", "private static readonly string[] SingleHandleKeys", "private static void Add("):
        if stale in text:
            errors.append("SemanticSelectionResolver still duplicates whole-project ownership logic: " + stale)

print("QS3D semantic selection preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: bound-drawing Floor/Material bulk selection resolves only selected ownership handles, rejects selected ambiguity, ignores unrelated conflicts and supports generated geometry channels.")
