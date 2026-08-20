#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
VIEW_MODEL = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/QuantityInsightCommands.cs"
PALETTE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"


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


def method_slice(text, start, end):
    start_index = text.find(start)
    end_index = text.find(end, start_index + len(start)) if start_index >= 0 else -1
    if start_index < 0 or end_index < 0:
        return ""
    return text[start_index:end_index]


def main():
    panel = read(PANEL)
    view_model = read(VIEW_MODEL)
    command = read(COMMAND)
    palette = read(PALETTE)
    failures = []

    require(command, "EntitySnapshotReader.ReadImpliedSelection(document)", "command reads native selection snapshots", failures)
    require(command, "PaletteCoordinator.SetInspection(snapshots)", "command forwards snapshots", failures)
    require(palette, "_quantityInsightPanel?.SetInspectionReadOnly(snapshots, project);", "palette forwards snapshots without requiring a project", failures)

    require(panel, "if (project == null)", "explicit no-project branch", failures)
    require(panel, "ShowSelectionGeometryFallback(activeDocument);", "no-project selection fallback", failures)
    require(panel, "if (_selectionSnapshots.Count > 0)", "loaded/refresh fallback retention", failures)
    require(panel, "ShowSelectionGeometryFallback(document);", "refresh keeps selected geometry visible", failures)
    require(panel, "VolumeDrawingUnitsCubed", "native volume metric", failures)
    require(panel, "SurfaceAreaDrawingUnitsSquared", "native surface-area metric", failures)
    require(panel, "AreaDrawingUnitsSquared", "native planar-area metric", failures)
    require(panel, "LengthDrawingUnits", "native length metric", failures)
    require(panel, '"DU³"', "raw volume unit is explicit", failures)
    require(panel, '"DU²"', "raw area unit is explicit", failures)
    require(panel, '"DU"', "raw length unit is explicit", failures)
    require(panel, "if (_selectionGeometryFallback)", "projectless locate routing", failures)
    require(panel, "Cad.EntitySnapshotReader.ReadHandles(document, handles)", "locate revalidates live handle", failures)
    require(panel, "Cad.CadHandleService.Select(document, handles)", "locate selects live handle", failures)
    require(panel, "ReferenceEquals(document, _boundDocument)", "locate stays bound to source DWG", failures)

    require(view_model, "ReplaceSelectionGeometry", "fallback view-model path", failures)
    require(view_model, 'GrossConcreteText = "—";', "no fake concrete total", failures)
    require(view_model, 'DeductionText = "—";', "no fake deduction total", failures)
    require(view_model, 'NetConcreteText = "—";', "no fake net total", failures)
    require(view_model, 'FormworkText = "—";', "no fake formwork total", failures)
    require(view_model, 'LengthText = "—";', "no fake project length total", failures)

    fallback = method_slice(panel, "private void ShowSelectionGeometryFallback", "private static QuantityInsightItemViewModel ToSelectionGeometryItem")
    if not fallback:
        failures.append("fallback method boundary not found")
    else:
        for forbidden in (
            "ProjectContextCoordinator",
            "ExistingProjectMutationContext",
            "Save",
            "Persist",
            "CreateProject",
            "OpenMode.ForWrite",
            "AppendEntity",
        ):
            forbid(fallback, forbidden, "read-only fallback must not bootstrap/mutate project or CAD", failures)

    if failures:
        print("QS3D Quantity Insight no-project preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Quantity Insight keeps selected native geometry visible without a QS3D project, labels raw drawing units explicitly, and preserves the no-bootstrap/read-only boundary.")
    print("NOTE: this is a static source guard; licensed BricsCAD interaction/visual qualification remains LOCAL_ONLY.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
