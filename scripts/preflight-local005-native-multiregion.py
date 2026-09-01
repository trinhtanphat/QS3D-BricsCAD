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
    "src/QS3D.BricsCAD.V25/Cad/MultiRegionTopologyFingerprint.cs": (
        "MultiRegionTopologyFingerprint",
        "Compute",
        "SHA256.Create",
        "fingerprintByHandle",
    ),
    "src/QS3D.BricsCAD.V25/Cad/SlabFoundationMultiRegionMeshSolidBuilder.cs": (
        "SlabFoundationMultiRegionMeshSolidBuilder",
        "PolygonSourceLoopRegionAssembler.Assemble",
        "PolygonalSlabMultiRegionMeshPlanner.Plan",
        "MaxBarsPerBatch = 12000",
        "ProjectStateSnapshot.Capture",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership",
        "GeneratedRebarRegionOwnershipService.RequireMatchingOwnership",
        "GeneratedRebarNativeOwnershipService.MarkGenerated",
        "GeneratedRebarRegionOwnershipService.MarkGenerated",
        "MultiRegionRebarManifest.SerializeSources",
        "MultiRegionRebarManifest.SerializeGenerated",
        "MultiRegionSourceManifest",
        "MultiRegionGeneratedManifest",
        "MultiRegionTopologyFingerprint.Compute",
        "GeneratedSlabMeshCount",
        "GeneratedFoundationMeshCount",
        "EnsureAggregateMetadataConsistency",
        "transaction.Commit()",
    ),
    "src/QS3D.BricsCAD.V25/Cad/GeneratedMultiRegionRebarRuntimeHealthService.cs": (
        "GeneratedMultiRegionRebarRuntimeHealthService",
        "Inspect",
        "MultiRegionRebarManifest.ParseSources",
        "MultiRegionRebarManifest.ParseGenerated",
        "ClosedPolygonSourceLoopReader.Read",
        "PolygonSourceLoopRegionAssembler.Assemble",
        "MultiRegionTopologyFingerprint.Compute",
        "MULTI_REGION_TOPOLOGY_FINGERPRINT_MISMATCH",
        "MULTI_REGION_GENERATED_COUNT_MISMATCH",
        "GeneratedRebarNativeOwnershipService.HasMatchingOwnership",
        "GeneratedRebarRegionOwnershipService.HasMatchingOwnership",
        "MultiRegionTopologyFingerprint",
        "DUPLICATE",
        "never mutates",
    ),
    "src/QS3D.BricsCAD.V25/MultiRegionRebarCommands.cs": (
        "QS3DSLABREBAR3DMULTI",
        "QS3DFOUNDATIONREBAR3DMULTI",
        "QS3DMULTIREBARHEALTH",
        "SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab",
        "SlabFoundationMultiRegionMeshSolidBuilder.BuildFoundation",
        "GeneratedMultiRegionRebarRuntimeHealthService",
        "TryGetReadOnly",
        "ExistingProjectMutationContext.Require",
        "EnsureSameProjectSnapshot",
        "var uiSyncFailed = false;",
        "catch { uiSyncFailed = true; }",
        "native update đã hoàn tất; một phần UI không thể đồng bộ.",
        "QS3DSLABREBAR3DMULTI không thể hoàn tất.",
        "QS3DFOUNDATIONREBAR3DMULTI không thể hoàn tất.",
        "QS3DMULTIREBARHEALTH không thể hoàn tất kiểm tra.",
        "TryWriteMessage(document",
    ),
    "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs": (
        'RectangleFootprintMode = "RectangleLocalXY"',
        'PolygonFootprintMode = "PolygonGlobalXY"',
        "RectangularSlabMeshPlanner.Plan",
        "PolygonalSlabMeshPlanner.Plan",
    ),
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs": (
        'RectangleFootprintMode = "RectangleLocalXY"',
        'PolygonFootprintMode = "PolygonGlobalXY"',
        "RectangularSlabMeshPlanner.Plan",
        "PolygonalSlabMeshPlanner.Plan",
    ),
}

V26_PROJECT = "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
V26_REQUIRED = (
    '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
    '<ProjectReference Include="..\\QS3D.Core\\QS3D.Core.csproj" />',
    '<DefineConstants>$(DefineConstants);BRICSCAD_V26</DefineConstants>',
    'BRICSCAD_V26_DIR',
)
V26_FORBIDDEN = (
    'QS3D.BricsCAD.V25\\QS3D.BricsCAD.V25.csproj',
    '<Reference Include="QS3D.BricsCAD.V25"',
    'GeneratedMultiRegionRebarRuntimeHealthService.cs;',
    'MultiRegionRebarCommands.cs;',
    'SlabFoundationMultiRegionMeshSolidBuilder.cs;',
)


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

        if relative == "src/QS3D.BricsCAD.V25/MultiRegionRebarCommands.cs":
            for forbidden in (
                "ex.Message",
                "exception.Message",
                "GetBaseException()",
                "StackTrace",
                "UI sync warning: ",
                "document.Editor.WriteMessage(\"\\n  [\"",
            ):
                if forbidden in text:
                    missing.append(relative + " (forbidden user-visible host detail token: " + forbidden + ")")

    v26_path = ROOT / V26_PROJECT
    if not v26_path.is_file():
        missing.append(V26_PROJECT + " (missing file)")
    else:
        v26_text = v26_path.read_text(encoding="utf-8")
        for token in V26_REQUIRED:
            if token not in v26_text:
                missing.append(V26_PROJECT + " (missing V26 linked-source token: " + token + ")")
        for token in V26_FORBIDDEN:
            if token in v26_text:
                missing.append(V26_PROJECT + " (forbidden V26 binary/source exclusion token: " + token + ")")

    if missing:
        print("LOCAL-005 native multi-region source contract is RED:")
        for item in missing:
            print(" -", item)
        return 1

    print("PASS: LOCAL-005 Core assembler, native loop reader, ownership/manifests, atomic materializer, read-only Health, stable command failure redaction/post-commit UI isolation, legacy rectangle/single-polygon compatibility, and V26 linked-source contract are present.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
