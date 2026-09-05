from pathlib import Path

root = Path(__file__).resolve().parents[1]
accumulator = (root / "src/QS3D.Core/Commercial/CommercialExactDecimalAccumulator.cs").read_text(encoding="utf-8")
contracts = (root / "src/QS3D.Core/Commercial/CommercialContracts.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/CommercialMultiplyPrecisionSmoke.cs").read_text(encoding="utf-8")

for token in [
    "internal static decimal MultiplyExact(decimal left, decimal right, string label)",
    "leftCoefficient * rightCoefficient",
    "leftScale + rightScale",
    '"Commercial multiplication precision loss: " + label + "."',
    "while (scale > 28 && signedCoefficient % 10 == 0)",
    "if (scale > 28)",
    "throw new OverflowException(precisionLossMessage);",
]:
    if token not in accumulator:
        raise SystemExit("Missing exact commercial multiplication contract: " + token)

multiply_start = contracts.index("internal static decimal Multiply(decimal left, decimal right, string label)")
multiply_end = contracts.index("internal static decimal Add(decimal left, decimal right, string label)", multiply_start)
multiply = contracts[multiply_start:multiply_end]
if "CommercialExactDecimalAccumulator.MultiplyExact(left, right, label)" not in multiply:
    raise SystemExit("CommercialGuard.Multiply must delegate to exact multiplication.")
if "checked(left * right)" in multiply:
    raise SystemExit("CommercialGuard.Multiply must not use native decimal multiplication that can silently round.")

for token in [
    "NonRepresentableProductRejectsInsteadOfRounding",
    "ExactScaleTwentyEightProductRemainsAccepted",
    "ReducibleHighScaleProductRemainsAccepted",
    "OverflowContractRemainsFailClosed",
    "0.0000000000000000000000000001m",
    "1.5m",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic commercial multiplication smoke contract: " + token)

print("Commercial exact multiplication precision preflight passed.")
