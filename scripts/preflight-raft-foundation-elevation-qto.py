#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROPERTY_SET = ROOT / "src/QS3D.Core/Domain/RaftFoundationPropertySet.cs"
LEVEL_PLACEMENT = ROOT / "src/QS3D.Core/Domain/RaftFoundationLevelPlacement.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationWorkflow.cs"
FAMILY_SUBTYPE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtype.cs"
BLT_WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs"
VISIBLE_ADD_ROUTE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationVisibleAddRoute.cs"
CAD_VERTICAL = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadElementVerticalPlacement.cs"
GEOMETRY_SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanationService.cs"
HIGHLIGHT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.RaftHighlight.cs"
SEMANTIC = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.RaftSemanticFaces.cs"
TRANSIENT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.TransientGeometry.cs"


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


def close(actual, expected, tolerance=1e-9):
    return abs(actual - expected) <= tolerance


def main():
    prop = read(PROPERTY_SET)
    placement = read(LEVEL_PLACEMENT)
    workspace = read(WORKSPACE)
    family_subtype = read(FAMILY_SUBTYPE)
    blt_workspace = read(BLT_WORKSPACE)
    visible_add = read(VISIBLE_ADD_ROUTE)
    cad_vertical = read(CAD_VERTICAL)
    geometry = read(GEOMETRY_SERVICE)
    highlight = read(HIGHLIGHT)
    semantic = read(SEMANTIC)
    transient = read(TRANSIENT)
    failures = []

    # The rendered BLT3D label is + Add. Pin that cross-file label to the one authoritative
    # raft route so the live click cannot fall through to the generic Family mode chooser or
    # be dispatched a second time by the legacy Draw handler.
    require(blt_workspace, 'RenameBlt3dButton("+ Thêm", "+ Add")', "rendered + Add label", failures)
    require(blt_workspace, 'string.Equals(text, "+ Add", StringComparison.Ordinal)', "BLT3D Add recognizer", failures)
    require(visible_add, 'RaftVisibleAddLabel = "+ Add"', "raft visible Add contract", failures)
    require(visible_add, "panel.IsRaftSubtypeFilter()", "visible Add raft subtype guard", failures)
    require(visible_add, "e.Handled = true;", "visible Add claims routed click", failures)
    require(visible_add, "panel.CreateFamilyFromWorkspaceSubtype(false);", "visible Add direct Family creation", failures)
    forbid(workspace, "IsWorkspaceAddFamilyButton(button)", "legacy raft Add interception must be absent", failures)
    require(workspace, 'if (!string.Equals(button.Content as string, "Vẽ 3D", StringComparison.Ordinal) ||', "legacy raft workflow Draw-only routing", failures)

    # Add/select/property must remain in the primary Family render path. A separate selection
    # handler in the raft file is intentionally not accepted because generic rendering can win.
    require(family_subtype, "var name = !string.IsNullOrWhiteSpace(subtype)", "deterministic subtype family naming", failures)
    require(family_subtype, "NextSubtypeFamilyName(subtype, existingNames)", "next unique Móng Bè-n naming", failures)
    require(family_subtype, "FamilyList.SelectedItem = live;", "new Family auto-selection", failures)
    require(family_subtype, "RaftFoundationLevelPlacement.EnsureDefaults(project, family);", "Add seeds real raft Level placement", failures)
    require(family_subtype, "if (family != null && RaftFoundationPropertySet.IsRaftFamily(family))", "primary family renderer raft branch", failures)
    require(family_subtype, "ApplyRaftFoundationPropertyForm(family);", "primary family renderer dedicated raft form", failures)

    # Owner-required dedicated Móng Bè schema. The generic Foundation Bề dày/Offset đáy schema
    # is not a valid substitute for a raft Family.
    require(workspace, 'Name = "Tên Family"', "Information/Tên Family", failures)
    require(workspace, 'categoryRow.Value = RaftFoundationPropertySet.SubtypeName;', "Information/Loại cấu kiện Móng Bè", failures)
    require(workspace, 'Name = "Tầng"', "Information/Tầng", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Kích thước", "Dày", RaftFoundationPropertySet.ThicknessKey', "Kích thước/Dày", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Cao độ", "Cách đặt", RaftFoundationPropertySet.ElevationModeKey', "Cao độ/Cách đặt", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Cao độ", "Cao độ đầu", RaftLevelSelectionKey', "Cao độ/Cao độ đầu", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Display", "Màu sắc", RaftColorModeKey', "Display/Màu sắc", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Display", "Độ trong suốt", RaftTransparencyKey', "Display/Độ trong suốt", failures)
    require(workspace, '"ByLayer"', "Display/ByLayer", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Metadata", "Mark", RaftMarkKey', "Metadata/Mark", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Metadata", "Comment", RaftCommentKey', "Metadata/Comment", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Metadata", "WBS", RaftWbsKey', "Metadata/WBS", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Vật liệu", "Vật liệu", RaftMaterialKey', "Vật liệu/Vật liệu", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Vật liệu", "Loại vật liệu", RaftMaterialTypeKey', "Vật liệu/Loại vật liệu", failures)
    forbid(workspace, '"Bề dày"', "raft dedicated renderer generic thickness label", failures)
    forbid(workspace, '"Offset đáy"', "raft dedicated renderer generic bottom-offset label", failures)

    # Exactly two semantic modes with one authoritative project-Level binding. Changing mode or
    # Level clears the opposite binding, and UI choices come from real project Floors/Levels.
    require(prop, 'BottomLevelMode = "bottom_level"', "bottom_level contract", failures)
    require(prop, 'TopLevelMode = "top_level"', "top_level contract", failures)
    require(prop, "NormalizeElevationMode", "strict two-mode normalization", failures)
    require(prop, "ActiveLevelKey", "active Level key contract", failures)
    require(prop, "OppositeLevelKey", "opposite Level key contract", failures)
    require(workspace, "project.Floors.OrderBy(x => x.ElevationM)", "real project Level choices", failures)
    require(workspace, "RaftFoundationPropertySet.ActiveLevelKey(mode)", "active Level mutation", failures)
    require(workspace, "RaftFoundationPropertySet.OppositeLevelKey(mode)", "opposite Level mutation", failures)
    require(workspace, "matches[0].Id", "persist selected project Level id", failures)
    require(workspace, "RaftFoundationLevelPlacement.Resolve(project, owned);", "property edit fail-closed placement validation", failures)

    # Core owns the only elevation arithmetic. Missing/invalid active Level or a retained opposite
    # binding is an error; geometry and element metadata copy the same resolved Family placement.
    require(placement, "Cao độ đầu chưa chọn Level", "missing active Level fails closed", failures)
    require(placement, "Móng Bè chỉ được giữ một Level binding", "opposite Level fails closed", failures)
    require(placement, "top = level.ElevationM;", "top_level top equals Level", failures)
    require(placement, "bottom = top - thicknessM;", "top_level grows downward", failures)
    require(placement, "bottom = level.ElevationM;", "bottom_level bottom equals Level", failures)
    require(placement, "top = bottom + thicknessM;", "bottom_level grows upward", failures)
    require(placement, "ApplyFamilyPlacementToElement", "Family placement copied to element metadata", failures)
    require(placement, "element.SetProperty(activeKey, levelId);", "element persists active Level id", failures)
    require(placement, "element.Properties.Remove(oppositeKey);", "element clears opposite Level id", failures)

    # Native builders consume the Core raft placement rather than legacy source-relative Z.
    require(cad_vertical, "RaftFoundationLevelPlacement.Resolve(project, element, family)", "native raft placement resolver", failures)
    require(cad_vertical, "placement.UsesBottomLevel || placement.UsesTopLevel", "native absolute Level bottom", failures)
    require(cad_vertical, "placement.BottomElevationM", "native bottom uses resolved elevation", failures)
    require(cad_vertical, "UsesBottomLevel || UsesTopLevel", "fingerprint uses resolved Level bottom", failures)
    require(workspace, 'panel.Send("QS3DDRAWRAFTFOUNDATION")', "raft native draw command", failures)

    # Acceptance arithmetic: a 4 m x 6 m x 0.8 m rectangular raft.
    length = 4.0
    width = 6.0
    thickness = 0.8
    concrete = length * width * thickness
    formwork = 2.0 * (length + width) * thickness
    side_areas = [length * thickness, width * thickness, length * thickness, width * thickness]
    if not close(concrete, 19.2):
        failures.append("4x6x0.8 concrete acceptance must be 19.20 m3")
    if not close(formwork, 16.0):
        failures.append("4x6x0.8 vertical-side formwork acceptance must be 16.00 m2")
    expected_sides = [3.2, 4.8, 3.2, 4.8]
    if any(not close(actual, expected) for actual, expected in zip(side_areas, expected_sides)):
        failures.append("4x6x0.8 side rows must be 3.20/4.80/3.20/4.80 m2")

    # Runtime quantity remains the existing exact-BREP engine; Foundation formwork is vertical Side only.
    require(geometry, "QuantityGeometryExplanation", "canonical exact-BREP explanation", failures)
    require(geometry, "category == ElementCategory.Foundation", "Foundation BREP classification", failures)
    require(geometry, 'string.Equals(faceType, "Side", StringComparison.Ordinal)', "Foundation side-only formwork", failures)

    # Semantic row identity is stable at the QS3D layer; native FaceId is only re-resolved for the fresh snapshot.
    require(semantic, '"Side:OuterLoop:Edge"', "semantic side key", failures)
    require(semantic, "ResolveFreshRaftQuantityFace", "fresh semantic face resolution", failures)
    require(semantic, "semanticMatches.Count == 1", "semantic uniqueness boundary", failures)
    forbid(semantic, "SubentityId", "persistent native subentity identity", failures)

    # Yellow is counted/included, red is deduction/intersection; both are non-persistent transients.
    require(highlight, "solid.ColorIndex = 2", "yellow included overlay", failures)
    require(highlight, "ShowRaftRedDeductions", "red deduction overlay routing", failures)
    require(highlight, "TransientDrawingMode.DirectTopmost", "raft transient mode", failures)
    require(highlight, "manager.EraseTransient", "raft transient cleanup", failures)
    require(transient, "region.ColorIndex = 1", "red deduction transient", failures)
    require(transient, "TransientDrawingMode.DirectTopmost", "deduction transient mode", failures)
    for source, label in ((highlight, "raft highlight"), (transient, "deduction highlight")):
        forbid(source, "OpenMode.ForWrite", label + " must not mutate DB entities", failures)
        forbid(source, "AppendEntity", label + " must not persist transient geometry", failures)

    # This feature lane is concrete + formwork only. No reinforcement authoring is allowed here.
    for source, label in ((workspace, "workspace"), (highlight, "highlight"), (semantic, "semantic face")):
        for token in ("Rebar", "Reinforcement", "Reinforcing", "Cốt thép"):
            forbid(source, token, label + " rebar scope", failures)

    if failures:
        print("Raft foundation elevation/QTO preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: rendered + Add routes directly to Móng Bè-n; primary dedicated Properties, one real Level binding, bottom_level/top_level native placement, and side-only QTO review are pinned. 4x6x0.8 acceptance = 19.20 m3 concrete and 16.00 m2 formwork.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
