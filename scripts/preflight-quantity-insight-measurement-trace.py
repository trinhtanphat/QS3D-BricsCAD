#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityGeometryExplanation.cs"
SERVICE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "QuantityGeometryExplanationService.cs"
ADAPTER = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityGeometryEvidenceAdapter.cs"
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DetailExplainer.MeasurementTrace.cs"
EXACT_FACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DetailExplainer.ExactFace.cs"
PROJECTION = ROOT / "src" / "QS3D.Core" / "Export" / "QuantityEvidenceExportProjection.cs"
XLSX = ROOT / "src" / "QS3D.Core" / "Export" / "XlsxQuantityEvidenceExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityMeasurementTraceSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def fail(message, details=()):
    print("ERROR:", message)
    for detail in details:
        print(" -", detail)
    return 1


def require(path, tokens, label):
    if not path.exists():
        return fail(label + " file is missing", [str(path.relative_to(ROOT))])
    text = path.read_text(encoding="utf-8")
    missing = [token for token in tokens if token not in text]
    if missing:
        return fail(label + " contract is incomplete", ["missing: " + token for token in missing])
    return text


def main():
    core = require(CORE, [
        "MeasurementKind",
        "MeasurementLength",
        "MeasurementHeight",
        "HasMeasurementTrace",
        "measurement trace must provide kind, length and height together",
        "measurement trace does not reconcile with exact BREP gross area",
        "face.MeasurementLength * face.MeasurementHeight",
    ], "Core exact-face measurement")
    if isinstance(core, int):
        return core

    service = require(SERVICE, [
        "SourceObjectId",
        "ReadLiveFaceExtents",
        "new FullSubentityPath(new[] { liveSolid.ObjectId }, SubentityId.Null)",
        "new Brep(rootPath)",
        "liveSolid.GetSubentityGeometricExtents(face.SubentityPath)",
        "TryBuildRectangleMeasurement",
        "Math.Sqrt(dx * dx + dy * dy)",
        "Math.Abs(measuredArea - grossAreaCad) > tolerance",
        'measurementKind = "brep-rectangle-extents-v1"',
        "ExternalBoundedSurface",
        "external.IsPlane",
        "external.BaseSurface is PlanarEntity basePlane",
        "MeasurementLength = seed.MeasurementLengthCad * lengthToMeter",
        "MeasurementHeight = seed.MeasurementHeightCad * lengthToMeter",
    ], "V25 exact-BREP measurement")
    if isinstance(service, int):
        return service
    if "face.Surface as PlanarEntity" in service:
        return fail("Quantity geometry must not regress to the V25-incompatible direct BREP surface cast")

    adapter = require(ADAPTER, [
        "face.HasMeasurementTrace ? \"BREP validated face length × height\" : \"BREP exact face gross area\"",
        'new QuantityEvidenceOperand("length"',
        'new QuantityEvidenceOperand("height"',
        "QuantityEvidenceSelector.ForFaceKey(elementId, faceId)",
    ], "canonical quantity evidence")
    if isinstance(adapter, int):
        return adapter

    ui = require(UI, [
        'content.StartsWith("S gộp:", StringComparison.Ordinal)',
        "TryResolveQuantityExactFaceButton(button, out var faceId)",
        "face.HasMeasurementTrace",
        'button.Content = "S gộp: " + FormatQuantityMeasurement(face.MeasurementLength)',
        '" × " + FormatQuantityMeasurement(face.MeasurementHeight)',
        'FormatQuantityMeasurement(face.GrossArea) + " m²"',
    ], "Quantity Insight measurement UI")
    if isinstance(ui, int):
        return ui
    forbidden_ui = ["OpenMode.ForWrite", "AppendEntity(", "UpgradeOpen(", ".Erase("]
    found_ui = [token for token in forbidden_ui if token in ui]
    if found_ui:
        return fail("measurement trace UI must remain read-only/transient", ["forbidden: " + token for token in found_ui])

    exact = require(EXACT_FACE, [
        "TryResolveQuantityExactFaceButton",
        "TryRevalidateQuantityGeometry(document, project, option",
        "solid.Highlight(facePath, false)",
    ], "existing exact native face locate")
    if isinstance(exact, int):
        return exact

    projection = require(PROJECTION, [
        "public string Operands { get; set; } = string.Empty;",
        "Operands = FormatOperands(contribution.Operands)",
        'operand.Key + "=" + operand.Value.ToString("G29", CultureInfo.InvariantCulture)',
    ], "quantity evidence export projection")
    if isinstance(projection, int):
        return projection

    xlsx = require(XLSX, [
        '"Operands"',
        'ValidateText(row.Operands, index, "Operands")',
        "AppendTextCell(builder, CellReference(17, rowNumber), row.Operands)",
    ], "quantity evidence XLSX")
    if isinstance(xlsx, int):
        return xlsx

    smoke = require(SMOKE, [
        "MeasurementLength = 1.50d",
        "MeasurementHeight = 0.20d",
        'Equal(1.50m, contribution.Operands.Single(x => x.Key == "length").Value',
        'Contains(xml, "length=1.5 m"',
        "Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(mismatched))",
        "Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(partial))",
    ], "measurement trace smoke")
    if isinstance(smoke, int):
        return smoke

    registration = require(REGISTRATION, ["QuantityMeasurementTraceSmoke.Run();"], "smoke registration")
    if isinstance(registration, int):
        return registration

    build_pos = service.find("TryBuildRectangleMeasurement(")
    reconcile_pos = service.find("Math.Abs(measuredArea - grossAreaCad) > tolerance", build_pos)
    publish_pos = service.find('measurementKind = "brep-rectangle-extents-v1"', reconcile_pos)
    if min(build_pos, reconcile_pos, publish_pos) < 0 or not (build_pos < reconcile_pos < publish_pos):
        return fail("measurement dimensions must reconcile to exact BREP area before the trace is published")

    print("PASS: Quantity Insight face measurement trace is exact-BREP-derived, fail-closed against non-reconciling dimensions, preserved in evidence/XLSX, and retains exact native face locate behavior.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
