#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Ed2NumericParitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing ED2 numeric-parity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


exporter = read(EXPORTER)
smoke = read(SMOKE)
registration = read(REGISTRATION)


def require(text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(label + " missing contract token: " + token)


require(exporter, (
    "ValidateEd2NumericParity(detailRows, summaryRows);",
    "private static void ValidateEd2NumericParity(",
    "summary.ElementIds",
    "ValidateEd2SummaryIdentity(summary, detail)",
    "ValidateEd2SummaryHandleParity(summary, group)",
    "summary.Count != group.Count",
    'RequireAggregateParity(summary.GrossConcreteM3, group, x => x.GrossConcreteM3, "GrossConcreteM3")',
    'RequireAggregateParity(summary.DeductionM3, group, x => x.DeductionM3, "DeductionM3")',
    'RequireAggregateParity(summary.NetConcreteM3, group, x => x.NetConcreteM3, "NetConcreteM3")',
    'RequireAggregateParity(summary.FormworkM2, group, x => x.FormworkM2, "FormworkM2")',
    'RequireAggregateParity(summary.LengthM, group, x => x.LengthM, "LengthM")',
    'RequireAggregateParity(summary.OuterPerimeterM, group, x => x.OuterPerimeterM, "OuterPerimeterM")',
    'RequireAggregateParity(summary.InnerPerimeterM, group, x => x.InnerPerimeterM, "InnerPerimeterM")',
    'RequireAggregateParity(summary.DoorAreaM2, group, x => x.DoorAreaM2, "DoorAreaM2")',
    'RequireAggregateParity(summary.SideAreaM2, group, x => x.SideAreaM2, "SideAreaM2")',
    'RequireAggregateParity(summary.BottomAreaM2, group, x => x.BottomAreaM2, "BottomAreaM2")',
    'RequireAggregateParity(summary.TopAreaM2, group, x => x.TopAreaM2, "TopAreaM2")',
    'RequireAggregateParity(summary.OtherAreaM2, group, x => x.OtherAreaM2, "OtherAreaM2")',
    "RequireDensityParity(summary, group);",
    "RequireMassParity(summary, group);",
    "left.HasValue == right.HasValue",
    "ED2 density must be greater than zero when present.",
    "ED2 mass must be non-negative when present.",
    'new InvalidDataException("ED2 TONG_HOP " + field + " does not equal the CHI_TIET aggregate.")',
), "XlsxQuantityExporter.cs")

validation = exporter.find("ValidateEd2NumericParity(detailRows, summaryRows);")
publication = exporter.find("ExportCore(path, detailRows, summaryRows);")
if min(validation, publication) < 0 or validation >= publication:
    errors.append("ED2 numeric parity must be validated before temp-package creation/publication")

require(smoke, (
    "CanonicalNumericParityPublishes();",
    "NumericDriftPreservesExistingDestination();",
    "NullDensityAndMassRulesRemainExplicit();",
    "SummaryHandleSwapsFailClosed();",
    "swapped-summary-handles.xlsx",
    "row => row.Count++",
    "row => row.GrossConcreteM3 += 0.5d",
    "row => row.FormworkM2 += 0.5d",
    "row => row.LengthM += 0.5d",
    "row => row.DensityKgM3 = 2500d",
    "row => row.MassKg = row.MassKg.GetValueOrDefault() + 1d",
    "File.WriteAllBytes(path, sentinel);",
    "File.ReadAllBytes(path).SequenceEqual(sentinel)",
    'Path.GetFileName(path) + ".*.tmp"',
    "inventedDensity.DensityKgM3 = 2400d;",
    "inventedMass.MassKg = 10d;",
    "Explicit mass must remain available when density is null.",
    "details.All(x => x.MassKg.HasValue)",
), "Ed2NumericParitySmoke.cs")
require(registration, ("Ed2NumericParitySmoke.Run();",), "SmokeTestRegistration.cs")

print("QS3D ED2 CHI_TIET/TONG_HOP numeric-parity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 validates per-summary count, volume, area, length, density and mass against its CHI_TIET elements before atomic XLSX publication, preserving explicit null density/mass semantics and existing destinations on refusal.")
