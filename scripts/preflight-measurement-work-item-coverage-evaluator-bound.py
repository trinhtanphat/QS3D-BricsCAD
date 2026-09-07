#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Mapping" / "MeasurementWorkItemCoverage.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MeasurementWorkItemCoverageEvaluatorBoundSmoke.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for token in (
    "MeasurementWorkItemCoverageReport.MaximumFindingCount",
    "project.Elements.Count > MeasurementWorkItemCoverageReport.MaximumFindingCount",
    "var quantityCount = element.Quantities.Count;",
    "var findingContribution = quantityCount == 0 ? 1 : quantityCount;",
    "MaximumFindingCount - admittedFindingCount",
    "throw CreateFindingCountException()",
    "private static void AddFinding",
):
    if token not in source:
        fail(f"coverage evaluator bound must retain source token: {token}")

snapshot_start = source.index("private static List<ElementCoverageSnapshot> SnapshotElements")
snapshot_end = source.index("private static IReadOnlyList<QuantityCoverageSnapshot> SnapshotQuantities", snapshot_start)
snapshot = source[snapshot_start:snapshot_end]
count_gate = snapshot.find("project.Elements.Count > MeasurementWorkItemCoverageReport.MaximumFindingCount")
elements_copy = snapshot.find("project.Elements.ToArray()")
quantity_count = snapshot.find("var quantityCount = element.Quantities.Count;")
quantity_snapshot = source.find("SnapshotQuantities(element)", snapshot_start)
if min(count_gate, elements_copy, quantity_count, quantity_snapshot) < 0:
    fail("coverage evaluator must expose element and quantity admission boundaries")
if not (count_gate < elements_copy < quantity_count < quantity_snapshot):
    fail("coverage evaluator must reject over-budget element/quantity counts before quantity payload snapshot materialization")

for token in (
    "ExactQuantityBoundaryIsAccepted",
    "QuantityOverflowFailsAtAdmission",
    "ElementOverflowFailsBeforeSnapshotMaterialization",
    "MaximumFindings = 10000",
    "[ModuleInitializer]",
):
    if token not in smoke:
        fail(f"coverage evaluator bound smoke must retain {token}")

print("PASS: measurement/work-item coverage evaluator enforces the shared 10,000 finding budget before payload snapshot materialization")
