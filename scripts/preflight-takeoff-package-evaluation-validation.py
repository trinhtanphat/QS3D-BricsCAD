from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageUx.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageEvaluationValidationSmoke.cs").read_text(encoding="utf-8")

required_source = [
    '"PKG.EVALUATION_FAILED"',
    'TakeoffPackageReadiness.Blocked',
    'Enumerable.Empty<TakeoffWorkflowLine>()',
    'BuildInventoryAndEstimate(evidence, bim, formula, rateProvider)',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"takeoff package evaluation guard missing production token: {token}")

required_smoke = [
    "BlocksFormulaFailure();",
    "BlocksNonFiniteRate();",
    "PreservesSuccessfulEvaluation();",
    'x.Code == "PKG.EVALUATION_FAILED"',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"takeoff package evaluation guard missing smoke token: {token}")

print("takeoff package evaluation validation guard: PASS")
