#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.LiveGeometry.cs"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs"
LOCATE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Locate.cs"
BREP = ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanationService.cs"


def read(path):
    if not path.is_file():
        raise SystemExit("ERROR: missing required source file: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + repr(needle))


def forbid(text, needle, label, failures):
    if needle in text:
        failures.append(label + ": forbidden " + repr(needle))


def main():
    live = read(LIVE)
    geometry = read(GEOMETRY)
    locate = read(LOCATE)
    brep = read(BREP)
    failures = []

    require(live, "PrepareQuantityGeometrySnapshot", "detached geometry snapshot", failures)
    require(live, "ResolveQuantityPreferredLiveHandles", "preferred live-handle resolver", failures)
    require(live, "GeneratedGeometryService.HasMatchingOwnership", "native ownership revalidation", failures)
    require(live, "GeneratedGeometryService.FindMatchingOwnedHandles", "owner-XData recovery", failures)
    require(live, "detachedElement.SourceHandles.Add(generatedHandle);", "generated-solid snapshot routing", failures)
    require(live, "GeneratedSolidStatus.Invalid", "stale/foreign generated-solid fail closed", failures)
    require(live, "QuarantinedGeometryHandle", "candidate quarantine", failures)
    require(live, "OpenMode.ForRead", "read-only CAD inspection", failures)
    forbid(live, "OpenMode.ForWrite", "read-only CAD inspection", failures)

    require(geometry, "PrepareQuantityGeometrySnapshot(document, project, ids", "BREP refresh uses owned snapshot", failures)
    require(geometry, "QuantityGeometryExplanationService.Build(document, geometryProject, ids[0])", "BREP build uses detached snapshot", failures)
    require(geometry, "OnQuantityGeometryTargetClick", "gross/net row locate", failures)
    require(geometry, "LocateQuantityGeometryTarget", "target highlight/zoom", failures)
    require(geometry, "OnQuantityGeometryDeductionClick", "deduction row locate", failures)
    require(geometry, "TryRevalidateQuantityGeometry", "click-time BREP fingerprint revalidation", failures)
    require(geometry, "ResolveQuantityPreferredLiveHandles(document, project, semanticIds", "deduction uses current preferred geometry", failures)
    require(geometry, "fresh.GeometryFingerprint", "fresh geometry fingerprint", failures)
    forbid(geometry, ".Concat(deduction.SourceHandles", "stale deduction handle fallback", failures)

    require(locate, "ResolveQuantityPreferredLiveHandles(document, project, matches[0].ElementIds", "detail locate prefers owned live geometry", failures)
    forbid(locate, "SourceHandleResolver.Resolve(project, matches[0].ElementIds)", "detail locate source-first fallback", failures)

    require(brep, "BoundingBoxesMayOverlap", "broad-phase bounding box", failures)
    require(brep, "BooleanOperationType.BoolIntersect", "exact native intersection", failures)
    require(brep, "BooleanOperationType.BoolSubtract", "residual native subtraction", failures)
    require(brep, "brep.GetVolume()", "native BREP volume", failures)
    require(brep, "face.GetArea()", "native BREP face area", failures)

    if failures:
        print("QS3D Quantity Insight live-BREP preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Quantity Insight routes BREP/detail/deduction locate through current owned live geometry.")
    print("NOTE: this is a static source guard; licensed BricsCAD V25 graphics/BREP runtime qualification remains separate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
