#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.LiveGeometry.cs"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs"
LOCATE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Locate.cs"
TRANSIENT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.TransientGeometry.cs"
BREP = ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanationService.cs"
REGION = ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryRegionPreviewService.cs"


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
    transient = read(TRANSIENT)
    brep = read(BREP)
    region = read(REGION)
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
    require(geometry, "title.Text = geometry == null", "BREP exact title is conditional on live geometry", failures)
    require(geometry, "? \"DIỄN GIẢI HÌNH HỌC\"", "unavailable geometry title omits exact claim", failures)
    require(geometry, ": \"DIỄN GIẢI HÌNH HỌC • BREP EXACT\";", "confirmed geometry title may claim exact BREP", failures)
    forbid(geometry, "title.Text = \"DIỄN GIẢI HÌNH HỌC • BREP EXACT\";", "unconditional BREP exact title", failures)
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

    require(region, "QuantityGeometryRegionPreviewService", "deduction region preview service", failures)
    require(region, "SourceHandleResolver.Resolve(geometryProject", "preview uses detached current routing", failures)
    require(region, "BooleanOperationType.BoolIntersect", "preview exact native intersection", failures)
    require(region, "OffsetBody(distanceCad)", "preview contact probe", failures)
    require(region, "OpenMode.ForRead", "preview read-only CAD inspection", failures)
    forbid(region, "OpenMode.ForWrite", "preview CAD mutation", failures)
    forbid(region, "AppendEntity", "preview database persistence", failures)

    require(transient, "TransientManager.CurrentTransientManager", "BricsCAD transient manager", failures)
    require(transient, "AddTransient(region, TransientDrawingMode.Highlight", "transient exact-region highlight", failures)
    require(transient, "EraseTransient(solid", "transient cleanup", failures)
    require(transient, "FrameworkElement.UnloadedEvent", "panel unload cleanup", failures)
    require(transient, "TreeView.SelectedItemChangedEvent", "selection-change cleanup", failures)
    require(transient, "TryRevalidateQuantityGeometry", "transient click-time revalidation", failures)
    require(transient, "PrepareQuantityGeometrySnapshot", "transient current owned geometry snapshot", failures)
    require(transient, "QuantityGeometryRegionPreviewService.Build", "transient native region build", failures)
    require(transient, "TryZoomQuantityRegion", "exact-region zoom", failures)
    forbid(transient, "OpenMode.ForWrite", "transient CAD mutation", failures)
    forbid(transient, "AppendEntity", "transient database persistence", failures)

    if failures:
        print("QS3D Quantity Insight live-BREP preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Quantity Insight routes BREP/detail/deduction locate through current owned live geometry and transient exact-region display.")
    print("NOTE: this is a static source guard; licensed BricsCAD V25 graphics/BREP runtime qualification remains separate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
