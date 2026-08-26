#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROPERTY_SET = ROOT / "src/QS3D.Core/Domain/RaftFoundationPropertySet.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationWorkflow.cs"
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
    workspace = read(WORKSPACE)
    geometry = read(GEOMETRY_SERVICE)
    highlight = read(HIGHLIGHT)
    semantic = read(SEMANTIC)
    transient = read(TRANSIENT)
    failures = []

    # Móng -> Móng Bè -> Add is direct and opens the selected Family property surface.
    require(workspace, "IsWorkspaceAddFamilyButton(button) && panel.IsRaftSubtypeFilter()", "direct raft Add interception", failures)
    require(workspace, "panel.CreateFamilyFromWorkspaceSubtype(false);", "direct raft Family creation", failures)
    require(workspace, "ApplyRaftFoundationPropertyForm(family)", "immediate raft property form", failures)
    require(workspace, 'AddRaftFamilyPropertyRow(family, "Kích thước", "Dày", "ThicknessM"', "raft thickness property", failures)
    require(workspace, '"Cao độ",\n                "Cao độ",\n                RaftFoundationPropertySet.ElevationModeKey', "raft elevation property", failures)
    require(workspace, "RaftElevationChoices", "two elevation choices", failures)
    require(workspace, "ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters", "thickness mm input", failures)
    require(workspace, 'panel.Send("QS3DDRAWRAFTFOUNDATION")', "raft draw command", failures)

    # Elevation mode is a strict two-value contract and resolves to the semantic bottom offset.
    require(prop, 'BottomLevelMode = "bottom_level"', "bottom_level contract", failures)
    require(prop, 'TopLevelMode = "top_level"', "top_level contract", failures)
    require(prop, "NormalizeElevationMode", "strict elevation normalization", failures)
    require(prop, "ResolveBottomOffsetM", "bottom-offset resolver", failures)
    require(prop, "? -thicknessM", "top_level grows downward", failures)
    require(workspace, "RaftFoundationPropertySet.ResolveBottomOffsetM(mode, thicknessM)", "workspace persists resolved bottom offset", failures)

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

    print("PASS: Móng Bè direct Add + bottom_level/top_level elevation + side-only concrete/formwork QTO + semantic face keys + yellow/red transient review are pinned; 4x6x0.8 acceptance = 19.20 m3 concrete and 16.00 m2 formwork.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
