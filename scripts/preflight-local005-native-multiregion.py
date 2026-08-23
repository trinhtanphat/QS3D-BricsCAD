#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = {
    "src/QS3D.Core/Geometry/PolygonSourceLoopRegionAssembler.cs": (
        "PolygonSourceLoopRegionAssembler",
        "PolygonSourceLoop2",
        "PolygonSourceRegion2",
    ),
    "src/QS3D.BricsCAD.V25/Cad/ClosedPolygonSourceLoopReader.cs": (
        "ClosedPolygonSourceLoopReader",
        "BulgedPolygonFootprintTessellator",
    ),
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarRegionOwnershipService.cs": (
        "GeneratedRebarRegionOwnershipService",
        "QS3D_REBAR_REGION",
    ),
    "src/QS3D.BricsCAD.V25/Cad/MultiRegionRebarManifest.cs": (
        "MultiRegionRebarManifest",
        "RegionId",
    ),
    "src/QS3D.BricsCAD.V25/Cad/SlabFoundationMultiRegionMeshSolidBuilder.cs": (
        "SlabFoundationMultiRegionMeshSolidBuilder",
        "PolygonalSlabMultiRegionMeshPlanner.Plan",
        "ProjectStateSnapshot.Capture",
        "GeneratedRebarRegionOwnershipService",
        "12000",
    ),
    "src/QS3D.BricsCAD.V25/Cad/GeneratedMultiRegionRebarRuntimeHealthService.cs": (
        "GeneratedMultiRegionRebarRuntimeHealthService",
        "MultiRegionRebarManifest",
    ),
    "src/QS3D.BricsCAD.V25/MultiRegionRebarCommands.cs": (
        "CommandMethod",
        "MULTIREGION",
    ),
}


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    missing = []
    for relative, tokens in REQUIRED_FILES.items():
        path = ROOT / relative
        if not path.is_file():
            missing.append(relative + " (missing file)")
            continue
        text = path.read_text(encoding="utf-8")
        for token in tokens:
            if token not in text:
                missing.append(relative + " (missing token: " + token + ")")

    slab = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs"
    foundation = ROOT / "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs"
    for path, legacy_tokens in (
        (slab, ("RectangularSlabMeshPlanner.Plan", "PolygonalSlabMeshPlanner.Plan", "GeneratedSlabMeshHandles")),
        (foundation, ("RectangularSlabMeshPlanner.Plan", "PolygonalSlabMeshPlanner.Plan", "GeneratedFoundationMeshHandles")),
    ):
        if not path.is_file():
            missing.append(str(path.relative_to(ROOT)) + " (legacy compatibility file missing)")
            continue
        text = path.read_text(encoding="utf-8")
        for token in legacy_tokens:
            if token not in text:
                missing.append(str(path.relative_to(ROOT)) + " (legacy compatibility token missing: " + token + ")")

    v26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
    if not v26.is_file():
        missing.append("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj (missing)")
    else:
        text = v26.read_text(encoding="utf-8")
        if "..\\QS3D.BricsCAD.V25\\**\\*.cs" not in text and "../QS3D.BricsCAD.V25/**/*.cs" not in text:
            missing.append("V26 project does not link V25 shared source wildcard")
        for filename in (
            "ClosedPolygonSourceLoopReader.cs",
            "GeneratedRebarRegionOwnershipService.cs",
            "MultiRegionRebarManifest.cs",
            "SlabFoundationMultiRegionMeshSolidBuilder.cs",
            "GeneratedMultiRegionRebarRuntimeHealthService.cs",
            "MultiRegionRebarCommands.cs",
        ):
            if filename in text and ("Remove=" in text or "Exclude=" in text):
                missing.append("V26 project appears to exclude new shared source: " + filename)

    if missing:
        print("LOCAL-005 native multi-region source contract is RED:")
        for item in missing:
            print(" -", item)
        return 1

    print("PASS: LOCAL-005 native multi-region source contract is present.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
