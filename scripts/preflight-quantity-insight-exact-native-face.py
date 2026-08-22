#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DetailExplainer.ExactFace.cs"
GEOMETRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DetailExplainer.Geometry.cs"
TRANSIENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DetailExplainer.TransientGeometry.cs"


def fail(message, details=()):
    print("ERROR:", message)
    for detail in details:
        print(" -", detail)
    return 1


def main():
    if not SOURCE.exists():
        return fail("exact native BREP face source is missing", [str(SOURCE.relative_to(ROOT))])

    source = SOURCE.read_text(encoding="utf-8")
    geometry = GEOMETRY.read_text(encoding="utf-8")
    transient = TRANSIENT.read_text(encoding="utf-8")

    required = [
        "TryRevalidateQuantityGeometry(document, project, option",
        "TryParseQuantityExactFaceId",
        "QuantityExactFaceSolidCount",
        "Cad.CadHandleService.Resolve(document, geometry.SourceHandles)",
        "new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null)",
        "new Brep(rootPath)",
        "face.SubentityPath",
        "Cad.CadHandleService.ClearSelection(document)",
        "solid.Highlight(facePath, false)",
        "entity.Unhighlight(path, false)",
        "DocumentToBeDeactivated += OnQuantityExactFaceDocumentSwitch",
        "DocumentBecameCurrent += OnQuantityExactFaceDocumentSwitch",
        "DocumentToBeDeactivated -= OnQuantityExactFaceDocumentSwitch",
        "DocumentBecameCurrent -= OnQuantityExactFaceDocumentSwitch",
        "TreeView.SelectedItemChangedEvent",
        "Selector.SelectionChangedEvent",
        "FrameworkElement.UnloadedEvent",
        "e.Handled = true",
        "whole Solid3d không được chọn",
    ]
    missing = [token for token in required if token not in source]
    if missing:
        return fail("exact native BREP face contract is incomplete", ["missing: " + token for token in missing])

    geometry_required = [
        'Text = face.FaceId + " • " + face.FaceType',
        'AddQuantityGeometryValue("S gộp"',
        'AddQuantityGeometryValue("S còn"',
        "TryRevalidateQuantityGeometry(",
        "fresh.GeometryFingerprint",
        "_quantityGeometryCurrent.GeometryFingerprint",
        "OnQuantityGeometryDeductionClick",
    ]
    missing_geometry = [token for token in geometry_required if token not in geometry]
    if missing_geometry:
        return fail("Quantity Insight geometry surface no longer exposes the expected exact-face/revalidation seam", ["missing: " + token for token in missing_geometry])

    transient_required = [
        "QuantityGeometryDeduction",
        "BuildQuantityRegionEntities",
        "ClearQuantityRegionPreview",
    ]
    missing_transient = [token for token in transient_required if token not in transient]
    if missing_transient:
        return fail("existing exact deduction/contact transient workflow was removed", ["missing: " + token for token in missing_transient])

    forbidden = [
        "OpenMode.ForWrite",
        "SetSubentColor",
        "SetSubentityColor",
        "SetSubentMaterial",
        "SetSubentityMaterial",
        "BooleanOperation(",
        "TransformBy(",
        ".Erase(",
        "UpgradeOpen(",
    ]
    found = [token for token in forbidden if token in source]
    if found:
        return fail("exact-face locate must remain read-only and transient", ["forbidden: " + token for token in found])

    root_pos = source.find("new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null)")
    brep_pos = source.find("new Brep(rootPath)", root_pos)
    path_pos = source.find("face.SubentityPath", brep_pos)
    clear_pos = source.find("Cad.CadHandleService.ClearSelection(document)", path_pos)
    highlight_pos = source.find("solid.Highlight(facePath, false)", clear_pos)
    if min(root_pos, brep_pos, path_pos, clear_pos, highlight_pos) < 0 or not (
        root_pos < brep_pos < path_pos < clear_pos < highlight_pos
    ):
        return fail("native face resolution must be DB-resident FullSubentityPath -> BREP face -> clear whole selection -> subentity highlight")

    revalidate_pos = source.find("TryRevalidateQuantityGeometry(document, project, option")
    face_match_pos = source.find("freshGeometry.FormworkFaces", revalidate_pos)
    native_pos = source.find("TryHighlightQuantityExactFace(document, freshGeometry", face_match_pos)
    if min(revalidate_pos, face_match_pos, native_pos) < 0 or not (revalidate_pos < face_match_pos < native_pos):
        return fail("face click must revalidate canonical geometry and exact face identity before touching native highlight state")

    print("PASS: Quantity Insight exact formwork-face actions revalidate current geometry and highlight only the resolved DB-resident native BREP subentity, with stale/document/selection cleanup and no persistent CAD mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
