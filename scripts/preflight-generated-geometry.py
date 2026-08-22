#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/ProjectElement.cs",
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmokeRegistration.cs",
    "tests/QS3D.Core.SmokeTests/RebarOwnershipHealthSmoke.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing generated-geometry lifecycle file: " + rel)

checks = {
    "src/QS3D.Core/Domain/ProjectElement.cs": [
        "GeneratedSolidStateKey", "GeneratedRebarStateKey", "GeneratedShapeRebarStateKey",
        "GeneratedSolidStaleSnapshotKey", "GeneratedRebarStaleSnapshotKey", "GeneratedShapeRebarStaleSnapshotKey",
        "MarkGeneratedGeometryStale", "IsGeneratedSolidStale", "IsGeneratedRebarStale", "IsGeneratedShapeRebarStale",
        "ClearGeneratedSolidStale", "ClearGeneratedRebarStale", "ClearGeneratedShapeRebarStale"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs": [
        "GENERATED_SOLID_STALE", "REBAR_GENERATED_STALE", "SHAPE_REBAR_GENERATED_STALE"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs": [
        "Refusing to orphan or overwrite generated geometry ownership", "ClearGeneratedSolidStale"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": [
        "SourceHandles", "GeneratedSolidHandle", "PhysicalOpeningCutSolidHandle", "AddProtected", "EnsureOwned"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": [
        "GeneratedGeometryService.PrepareReplacement", "GeneratedRebarOwnershipGuard.Build", "ClearGeneratedGeometryStale"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs": [
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "GeneratedRebarHandles"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs": [
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "GeneratedShapeRebarHandles",
        "OpenSelectedSource", "DistributionCentered", "edgeInset"
    ],
    "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs": [
        "ResolveUniqueWallOwners", "GeneratedDependentGeometryInvalidator.Prepare", "invalidation.CommitMetadata"
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs": [
        "GeneratedOutputsBecomeStaleAfterSemanticEdit", "ReplacedHandleAutoResolvesOnlyItsOwnStaleKind",
        "ExplicitClearPreservesOtherStaleKinds", "ElementsWithoutGeneratedOutputsRemainFresh"
    ],
    "tests/QS3D.Core.SmokeTests/RebarOwnershipHealthSmoke.cs": [
        "RebarCannotClaimHostGeneratedSolid", "ShapeCannotClaimAnotherElementsSource", "ShapeHealthSeesColumnRebarConflict"
    ],
}
for rel, needles in checks.items():
    path = ROOT / rel
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(rel + " missing guard/token: " + needle)

print("QS3D generated-geometry lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: generated host/rebar/shape ownership, invalidation, stale-state lifecycle, health command and smoke guards are present.")
