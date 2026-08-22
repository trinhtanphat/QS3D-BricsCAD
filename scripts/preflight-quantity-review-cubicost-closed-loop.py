#!/usr/bin/env python3
from pathlib import Path

# Issue #3500: pin the already-landed Cubicost-like Quantity Review closed loop
# without introducing a second quantity/geometry/export engine.
ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
VIEWMODEL = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs"
EXACT_FACE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.ExactFace.cs"
TRANSIENT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.TransientGeometry.cs"
EVIDENCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.QuantityEvidenceExport.cs"
EXCEL = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.ExcelRoundTrip.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"


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
    xaml = read(XAML)
    panel = read(PANEL)
    viewmodel = read(VIEWMODEL)
    geometry = read(GEOMETRY)
    exact_face = read(EXACT_FACE)
    transient = read(TRANSIENT)
    evidence = read(EVIDENCE)
    excel_bridge = read(EXCEL)
    v26 = read(V26)
    failures = []

    # One deterministic navigation authority: Floor -> Type/Category -> Name/Family -> Element.
    require(xaml, 'ItemsSource="{Binding Types}"', "Floor -> Type hierarchy", failures)
    require(xaml, 'ItemsSource="{Binding Names}"', "Type -> Name hierarchy", failures)
    require(xaml, 'ItemsSource="{Binding Items}"', "Name -> Element hierarchy", failures)
    require(xaml, 'Text="Cây cấu kiện • Floor / Type / Name / Element"', "visible hierarchy contract", failures)
    require(viewmodel, "QuantityInsightFloorViewModel", "Floor node", failures)
    require(viewmodel, "QuantityInsightTypeViewModel", "Type/category node", failures)
    require(viewmodel, "QuantityInsightNameViewModel", "Name/family node", failures)
    require(viewmodel, "QuantityInsightItemViewModel", "Element leaf", failures)
    require(viewmodel, ".GroupBy(x => string.IsNullOrWhiteSpace(x.Category)", "category grouping", failures)
    require(viewmodel, ".GroupBy(x => string.IsNullOrWhiteSpace(x.FamilyName)", "family grouping", failures)

    # Model -> Quantity and Quantity -> Model use current semantic rows and live handles.
    require(panel, "ProjectStateSnapshot.CreateDetachedCopy(project)", "detached quantity preview", failures)
    require(panel, "ProjectQuantityReportBuilder.Detail(previewProject)", "canonical detail rows", failures)
    require(panel, "ApplySelectionHighlights(project, true)", "CAD selection -> quantity highlight", failures)
    require(panel, "SourceHandleResolver.Resolve(project, currentRow.ElementIds)", "quantity -> semantic handle provenance", failures)
    require(panel, "Cad.CadHandleService.Select(document, handles)", "quantity -> live CAD selection", failures)
    require(panel, "ViewportCommands.TryZoomSelection(document)", "quantity -> live CAD zoom", failures)
    require(panel, "SameProjectIdentity(project)", "quantity row project freshness", failures)

    # Explanation must remain exact BREP, gross/deduction/net, face-based formwork.
    require(geometry, "QuantityGeometryExplanationService.Build(document, geometryProject, ids[0])", "canonical BREP explanation", failures)
    require(geometry, '"THỂ TÍCH • GỘP - TRỪ = CÒN"', "concrete gross/deduction/net explanation", failures)
    require(geometry, '"VÁN KHUÔN THEO MẶT • GỘP - TRỪ = CÒN"', "face-level formwork explanation", failures)
    require(geometry, 'Text = face.FaceId + " • " + face.FaceType', "stable displayed face identity", failures)
    require(geometry, "OnQuantityGeometryDeductionClick", "deduction locate action", failures)
    require(geometry, "TryRevalidateQuantityGeometry(document, project, option", "geometry revalidation", failures)

    # Face clicks must resolve the same live DB-resident BREP face and never mutate presentation.
    require(exact_face, "TryParseQuantityExactFaceId", "stable SOLID/FACE parser", failures)
    require(exact_face, "Cad.CadHandleService.Resolve(document, geometry.SourceHandles)", "live source handle resolution", failures)
    require(exact_face, "new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null)", "DB-resident BREP root", failures)
    require(exact_face, "new Brep(rootPath)", "native BREP enumeration", failures)
    require(exact_face, "face.SubentityPath", "native face subentity path", failures)
    require(exact_face, "Cad.CadHandleService.ClearSelection(document)", "whole-solid selection cleanup", failures)
    require(exact_face, "solid.Highlight(facePath, false)", "exact native face highlight", failures)
    require(exact_face, "entity.Unhighlight(path, false)", "exact native face cleanup", failures)
    require(exact_face, "DocumentToBeDeactivated += OnQuantityExactFaceDocumentSwitch", "multi-DWG face cleanup", failures)
    require(exact_face, "FrameworkElement.UnloadedEvent", "panel-lifecycle face cleanup", failures)
    for token in (
        "OpenMode.ForWrite",
        "SetSubentColor",
        "SetSubentityColor",
        "SetSubentMaterial",
        "SetSubentityMaterial",
        "BooleanOperation(",
        "TransformBy(",
        "UpgradeOpen(",
    ):
        forbid(exact_face, token, "read-only exact-face action", failures)

    # Deduction review must rebuild and display the exact intersection/contact transient.
    require(transient, "QuantityGeometryRegionPreviewService.Build", "exact deduction/contact region rebuild", failures)
    require(transient, "ShowQuantityRegionPreview", "transient region presentation", failures)
    require(transient, "TransientDrawingMode.Highlight", "native transient highlight mode", failures)
    require(transient, "manager.AddTransient", "transient add", failures)
    require(transient, "manager.EraseTransient", "transient cleanup", failures)
    require(transient, "TreeView.SelectedItemChangedEvent", "tree-change transient cleanup", failures)
    require(transient, "FrameworkElement.UnloadedEvent", "panel-unload transient cleanup", failures)

    # Evidence export consumes the explanation already being reviewed; it must not recalculate via a second engine.
    require(xaml, 'Content="Xuất evidence" Click="OnQuantityEvidenceExportClick"', "evidence export action", failures)
    require(evidence, "TryRevalidateQuantityGeometry(document, project, option", "evidence freshness", failures)
    require(evidence, "QuantityGeometryEvidenceAdapter.Create(freshGeometry)", "canonical geometry -> evidence adapter", failures)
    require(evidence, "XlsxQuantityEvidenceExporter.Export(dialog.FileName, evidence.Explanations)", "canonical evidence exporter", failures)
    forbid(evidence, "ProjectContextCoordinator.GetOrCreate", "no project creation during evidence export", failures)

    # Excel round-trip must reuse ED2 + Excel Locate and preserve fail-closed selection semantics.
    require(xaml, 'Content="Xuất Excel" Click="OnExcelExportClick"', "Excel export action", failures)
    require(xaml, 'Content="Truy ngược Excel" Click="OnExcelTracebackClick"', "Excel traceback action", failures)
    require(excel_bridge, "SelectedScopeItems()", "tree scope -> element projection", failures)
    require(excel_bridge, "BuildPreviewRows(project, out _)", "fresh canonical preview before export", failures)
    require(excel_bridge, "SameRow(displayedRow, matches[0])", "stale quantity/provenance refusal", failures)
    require(excel_bridge, "SourceHandleResolver.Resolve(project, elementIds)", "semantic -> CAD provenance before export", failures)
    require(excel_bridge, "Cad.CadHandleService.Resolve(document, handles)", "all-live handle pre-resolution", failures)
    require(excel_bridge, "if (resolved.Count != handles.Length)", "partial handle refusal", failures)
    require(excel_bridge, "document.Editor.SetImpliedSelection(resolved.ToArray());", "atomic PICKFIRST replacement", failures)
    require(excel_bridge, '"QS3DED2 "', "canonical ED2 export route", failures)
    require(excel_bridge, '"QS3DEXCELLOCATE "', "canonical Excel Locate route", failures)
    forbid(excel_bridge, "BLT3D", "no proprietary sibling dependency", failures)
    forbid(excel_bridge, "Assembly.Load", "no runtime sibling loading", failures)

    resolve_pos = excel_bridge.find("Cad.CadHandleService.Resolve(document, handles)")
    complete_pos = excel_bridge.find("if (resolved.Count != handles.Length)", resolve_pos)
    selection_pos = excel_bridge.find("document.Editor.SetImpliedSelection(resolved.ToArray());", complete_pos)
    if min(resolve_pos, complete_pos, selection_pos) < 0 or not (resolve_pos < complete_pos < selection_pos):
        failures.append("Excel scope must resolve and validate the complete live Handle set before changing PICKFIRST")

    revalidate_pos = exact_face.find("TryRevalidateQuantityGeometry(document, project, option")
    face_match_pos = exact_face.find("freshGeometry.FormworkFaces", revalidate_pos)
    highlight_pos = exact_face.find("TryHighlightQuantityExactFace(document, freshGeometry", face_match_pos)
    if min(revalidate_pos, face_match_pos, highlight_pos) < 0 or not (revalidate_pos < face_match_pos < highlight_pos):
        failures.append("exact face action must revalidate geometry + displayed face identity before native highlight")

    # V26 intentionally consumes the same Quantity Review C#/XAML source.
    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared C# source parity", failures)
    require(v26, '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">', "V26 shared XAML parity", failures)

    if failures:
        print("QS3D Cubicost-like Quantity Review closed-loop preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Quantity Review remains one closed loop: CAD/model <-> deterministic quantity tree <-> canonical BREP/evidence explanation <-> exact face/deduction review <-> ED2 Excel traceback, with current-DWG/project/provenance fail-closed boundaries.")
    print("NOTE: licensed interactive face/transient/save-reopen/multi-DWG acceptance remains LOCAL_ONLY and is tracked separately; this guard does not manufacture LOCAL_PASS.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
