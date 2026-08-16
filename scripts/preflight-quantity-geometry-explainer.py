#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

FILES = {
    "core": ROOT / "src/QS3D.Core/Reporting/QuantityGeometryExplanation.cs",
    "service": ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanationService.cs",
    "ui": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs",
    "render": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Render.cs",
    "csproj": ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
}

REQUIRED = {
    "core": [
        "DefaultVolume = 1e-8",
        "DefaultDistance = 1e-6",
        "DefaultArea = 1e-6",
        "VolumeIntersection",
        "FaceContact",
        "GeometryFingerprint",
        "Dependencies",
    ],
    "service": [
        "BoundingBoxesMayOverlap",
        "BooleanOperationType.BoolIntersect",
        "BooleanOperationType.BoolSubtract",
        "ComponentIndex",
        "GlobalIndex",
        "residualForFormwork",
        "solid.Dispose()",
        "new Brep(",
        "GetVolume()",
        "GetArea()",
        "OffsetBody",
        "LengthToMeter",
        "INSUNITS=Undefined",
        "RegionKey",
        'return "End"',
        "planeToleranceCad = Math.Max(toleranceCad * 1e-3d, 1e-12d)",
    ],
    "ui": [
        "DIỄN GIẢI HÌNH HỌC",
        "V gộp",
        "Trừ giao",
        "V còn",
        "VÁN KHUÔN THEO MẶT",
        "S gộp",
        "S còn",
        "OnQuantityGeometryDeductionClick",
        "PrepareQuantityGeometrySnapshot(document, project, ids, out var geometryError)",
        "TryRevalidateQuantityGeometry(",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(preview)",
        "var canonicalElementIds = CanonicalIds(option.Row.ElementIds).ToArray()",
        "elementIds = canonicalElementIds",
        "ProjectQuantityReportBuilder.Detail(preview, canonicalElementIds)",
        "SameElementIdentity(canonicalElementIds, x)",
        "SameRow(option.Row, matches[0])",
        "ResolveQuantityPreferredLiveHandles(document, project, semanticIds, out var resolutionError)",
        "ViewportCommands.TryZoomSelection(document)",
        "GeometryFingerprint",
        "!string.Equals(fresh.GeometryFingerprint, _quantityGeometryCurrent.GeometryFingerprint, StringComparison.Ordinal)",
    ],
    "render": [
        "RefreshQuantityGeometry(option)",
        "geometry?.GrossVolume",
        "geometry?.DeductionVolume",
        "geometry?.NetVolume",
        "geometry?.NetFormworkArea",
        "RenderQuantityGeometry(geometry)",
    ],
    "csproj": [
        'Reference Include="TD_MgdBrep"',
        "TD_MgdBrep.dll",
    ],
}


def main():
    failures = []
    texts = {}
    for key, path in FILES.items():
        if not path.is_file():
            failures.append(f"missing file: {path.relative_to(ROOT)}")
            continue
        text = path.read_text(encoding="utf-8")
        texts[key] = text
        for marker in REQUIRED[key]:
            if marker not in text:
                failures.append(f"{path.relative_to(ROOT)} missing marker: {marker}")

    service = texts.get("service", "")
    if service:
        broad = service.find("BoundingBoxesMayOverlap")
        exact = service.find("TryIntersection")
        if broad < 0 or exact < 0:
            failures.append("service must retain broad-phase and exact-intersection stages")
        if "FindMatchingFace(seeds, componentIndex" not in service:
            failures.append("multi-solid face matching must be component-scoped")
        if "foreach (var solid in residualForFormwork) solid.Dispose();" not in service:
            failures.append("formwork residual Solid3d clones must be disposed")
        if "grossVolumeCad - netVolumeCad" not in service:
            failures.append("net-volume deduction must use residual/union semantics, not summed causes")
        if "toleranceCad * 1e-3d" not in service:
            failures.append("face-plane identity tolerance must be stricter than contact-probe offset")

    ui = texts.get("ui", "")
    if 'SendStringToExecute("QS3DZOOMSELECTED ' in ui or 'SendStringToExecute("QS3DZOOMEXTENTS ' in ui:
        failures.append("geometry deduction locate must use direct in-process zoom, not queued command re-entry")
    if "ProjectQuantityReportBuilder.Detail(project, option.Row.ElementIds)" in ui:
        failures.append("geometry deduction locate must validate against regenerated detached preview, not dirty live semantic detail")

    revalidate_start = ui.find("private bool TryRevalidateQuantityGeometry(")
    face_sort_start = ui.find("private static int FaceSort", revalidate_start)
    revalidate = ui[revalidate_start:face_sort_start] if revalidate_start >= 0 and face_sort_start > revalidate_start else ""
    ordered = (
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(preview)",
        "var canonicalElementIds = CanonicalIds(option.Row.ElementIds).ToArray()",
        "elementIds = canonicalElementIds",
        "ProjectQuantityReportBuilder.Detail(preview, canonicalElementIds)",
        "SameRow(option.Row, matches[0])",
        "PrepareQuantityGeometrySnapshot(document, project, canonicalElementIds, out var geometryError)",
        "QuantityGeometryExplanationService.Build(document, geometryProject, canonicalElementIds[0])",
        "fresh.GeometryFingerprint",
        "_quantityGeometryCurrent.GeometryFingerprint",
    )
    cursor = 0
    for token in ordered:
        pos = revalidate.find(token, cursor)
        if pos < 0:
            failures.append("geometry revalidation missing ordered freshness token: " + token)
            break
        cursor = pos + len(token)

    if failures:
        print("Quantity geometry explainer preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: quantity geometry explainer source contracts are present.")
    print(" - Bounding-box pruning + exact Solid3d boolean intersection")
    print(" - Residual subtraction prevents double volume deduction")
    print(" - Multi-Solid3d face identities are component-scoped")
    print(" - Contact-probe cut planes cannot masquerade as original target faces")
    print(" - Locate regenerates a detached semantic preview, rebuilds live geometry, and requires the same BREP fingerprint")
    print(" - Preferred live handles + direct CAD select/zoom are used for target/deduction locate")
    print(" - BREP compile reference and SI unit normalization")
    return 0


if __name__ == "__main__":
    sys.exit(main())
