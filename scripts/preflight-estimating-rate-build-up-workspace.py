#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Cost/EstimatingRateBuildUpWorkspace.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/EstimatingRateBuildUpCommands.cs"
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/EstimatingRateBuildUpWindow.xaml.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/EstimatingRateBuildUpWindow.xaml"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingRateBuildUpSmoke.cs"
SMOKE_ENTRYPOINT = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestEntryPoint.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


def text(path: Path) -> str:
    if not path.is_file():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


core = text(CORE)
command = text(COMMAND)
ui = text(UI)
xaml = text(XAML)
smoke = text(SMOKE)
smoke_entrypoint = text(SMOKE_ENTRYPOINT)

required_core = [
    "public enum EstimatingResourceCategory",
    "Material = 0",
    "Labour = 1",
    "Plant = 2",
    "Subcontract = 3",
    "CostDecimalMath.ApplyPercentagePreservingPrecision",
    "CostDecimalMath.AddPreservingNonZeroContribution",
    "new CostResourceComponent(",
    "new CostRateBuildUp(",
    "EstimatingRevisionStatus.Draft",
    "EstimatingRevisionStatus.Reviewed",
    "EstimatingRevisionStatus.Approved",
    "CreateNextRevision(",
    "MarkReviewed(",
    "MarkApproved(",
]
for token in required_core:
    if token not in core:
        fail(f"Core estimating workflow lost required authority/lifecycle token: {token}")

if "DirectUnitCost =" in core or "OverheadUnitCost =" in core or "ProfitUnitCost =" in core:
    fail("Estimating workflow must not reimplement CostRateBuildUp monetary result fields.")

required_ui = [
    ".Evaluate()",
    ".MarkReviewed(",
    ".MarkApproved(",
    ".CreateNextRevision(",
    "CostRateBuildUp result",
    "database.UnmanagedObject != _nativeDatabaseIdentity",
]
for token in required_ui:
    if token not in ui:
        fail(f"Estimating UI lost required thin-adapter/document-affinity token: {token}")

for forbidden in [
    "DirectUnitCost =",
    "OverheadUnitCost =",
    "ProfitUnitCost =",
    "UnitRate = result.DirectUnitCost",
    "overhead / 100",
    "profit / 100",
]:
    if forbidden in ui:
        fail(f"Estimating UI contains forbidden monetary arithmetic token: {forbidden}")

for token in [
    'CommandMethod("QS3DESTIMATE"',
    'CommandMethod("QS3DRATEBUILDUP"',
    "Application.ShowModelessWindow",
    "GetNativeDatabaseIdentity",
]:
    if token not in command:
        fail(f"Estimating command surface lost required token: {token}")

for token in [
    "Material • Labour • Plant • Subcontract",
    "quotation provenance",
    'Click="Evaluate_Click"',
    'Click="Review_Click"',
    'Click="Approve_Click"',
]:
    if token not in xaml:
        fail(f"Estimating XAML lost professional workflow surface token: {token}")

for token in [
    "Equal(500m, result.DirectUnitCost",
    "Equal(50m, result.OverheadUnitCost",
    "Equal(55m, result.ProfitUnitCost",
    "Equal(605m, result.UnitRate",
    "EstimatingRevisionStatus.Approved",
    "PreviousRevisionId",
    "Currency mismatch",
]:
    if token not in smoke:
        fail(f"Estimating smoke lost deterministic workflow oracle: {token}")

if "[ModuleInitializer]" in smoke or "RegisterAndRun()" in smoke:
    fail("Estimating smoke must use the explicit smoke runner instead of module-initializer side effects.")

if "EstimatingRateBuildUpSmoke.Run();" not in smoke_entrypoint:
    fail("Estimating smoke is not registered through SmokeTestEntryPoint.")

print("PASS: estimating rate build-up workspace preserves Core arithmetic authority, provenance, revision lifecycle, explicit smoke registration and thin UI boundaries.")
sys.exit(0)
