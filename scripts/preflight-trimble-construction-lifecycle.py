#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LIFECYCLE = ROOT / "src/QS3D.Core/BenchmarkParity/QsTrimbleConstructionLifecycle.cs"
INTEROP = ROOT / "src/QS3D.Core/BenchmarkParity/QsTrimbleProjectControlsInterop.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTrimbleConstructionLifecycleSmoke.cs"
DOC = ROOT / "docs/benchmark-parity/trimble-construction-lifecycle.md"

for path in (LIFECYCLE, INTEROP, SMOKE, DOC):
    if not path.is_file():
        raise SystemExit("Trimble construction lifecycle preflight missing file: " + str(path.relative_to(ROOT)))

lifecycle = LIFECYCLE.read_text(encoding="utf-8")
interop = INTEROP.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "SupplierLifecycleStatus",
    "SubcontractCommitment",
    "PurchaseOrderDelivery",
    "FieldProgressRecord",
    "TrimbleConstructionLifecycleEngine",
    "BuildProjectControls",
    "CommitmentRemaining",
    "ScheduleVariance",
    "DeliveryStatus.Cancelled",
):
    if token not in lifecycle:
        raise SystemExit("Trimble lifecycle contract missing: " + token)

for token in (
    "TrimbleProjectControlsInterop",
    "CurrentSchemaVersion",
    "ordered/commitment",
    "delivered/ordered",
    "actual/commitment",
    "commitmentVariance",
    "costToComplete",
    "ToCsv",
):
    if token not in interop:
        raise SystemExit("Trimble ERP/project-controls contract missing: " + token)

for token in (
    "RejectsUnapprovedSupplier();",
    "ordered value excludes cancelled PO",
    "actual/commitment",
    "deterministic package ordering",
    "ERP CSV header is not stable",
    "fail closed when a ratio denominator is zero",
):
    if token not in smoke:
        raise SystemExit("Trimble lifecycle smoke evidence missing: " + token)

for token in (
    "supplier lifecycle status",
    "subcontract commitments",
    "purchase-order delivery tracking",
    "actual-vs-commitment",
    "ERP / project-controls boundary",
):
    if token not in doc:
        raise SystemExit("Trimble lifecycle documentation missing: " + token)

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in lifecycle or forbidden in interop:
        raise SystemExit("Trimble lifecycle leaked host dependency: " + forbidden)

print("Trimble construction lifecycle parity preflight: PASS")
