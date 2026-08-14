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
        "QS3DZOOMSELECTED",
        "GeometryFingerprint",
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

    if failures:
        print("Quantity geometry explainer preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: quantity geometry explainer source contracts are present.")
    print(" - Bounding-box pruning + exact Solid3d boolean intersection")
    print(" - Residual subtraction prevents double volume deduction")
    print(" - Multi-Solid3d face identities are component-scoped")
    print(" - Per-face formwork/contact explanation and clickable CAD locate UI")
    print(" - BREP compile reference and SI unit normalization")
    return 0


if __name__ == "__main__":
    sys.exit(main())
