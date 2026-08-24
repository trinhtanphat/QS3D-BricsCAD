#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = {
    "src/QS3D.Core/Geometry/PolygonSourceLoopRegionAssembler.cs": (
        "PolygonSourceLoopRegionAssembler",
        "PolygonSourceLoop2",
        "PolygonSourceRegion2",
        "PolygonRegionSetTopology.NormalizeAndValidate",
    ),
    "src/QS3D.BricsCAD.V25/Cad/ClosedPolygonSourceLoopReader.cs": (
        "ClosedPolygonSourceLoopReader",
        "Polyline",
        "Closed",
        "GetBulgeAt",
        "BulgedPolygonFootprintTessellator",
        "PlaneToWorld",
        "CadGeometryGuard.ToMeters",
        "Fingerprint",
        "MaxSourceVertices",
        "MaxTessellatedVertices",
    ),
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarRegionOwnershipService.cs": (
        "GeneratedRebarRegionOwnershipService",
        "QS3D_REBAR_REGION",
        "GeneratedOwnershipIdentityToken.Project",
        "GeneratedOwnershipIdentityToken.Element",
        "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot",
        "regionId",
        "MarkGenerated",
        "HasMatchingOwnership",
        "RequireMatchingOwnership",
    ),
    "src/QS3D.BricsCAD.V25/Cad/MultiRegionRebarManifest.cs": (
        "MultiRegionRebarManifest",
        "SourceManifestEntry",
        "GeneratedManifestEntry",
        "SerializeSources",
        "ParseSources",
        "SerializeGenerated",
        "ParseGenerated",
        "MaxRegions",
        "MaxHandlesPerRegion",
    ),
}


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

    if missing:
        print("LOCAL-005 native multi-region source contract is RED:")
        for item in missing:
            print(" -", item)
        return 1

    print("PASS: LOCAL-005 Core assembler, native loop reader, region ownership, and bounded manifests are present.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
