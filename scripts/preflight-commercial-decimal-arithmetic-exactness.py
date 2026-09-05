#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTRACTS = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
EXACT = ROOT / "src/QS3D.Core/Commercial/CommercialExactDecimalAccumulator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CostBenchmarkMedianPrecisionSmoke.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL commercial decimal exactness preflight: missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL commercial decimal exactness preflight: stale {label}: {needle}")


def require_arithmetic_zero_before_scale_rejection(text: str) -> None:
    arithmetic = text.find("private static decimal MaterializeArithmetic(")
    aggregate = text.find("private static decimal MaterializeAggregate(", arithmetic + 1)
    zero_check = text.find("if (signedCoefficient.IsZero)", arithmetic, aggregate)
    zero_return = text.find("return 0m;", zero_check, aggregate)
    scale_reject = text.find("if (scale > 28)", zero_return, aggregate)
    if not (0 <= arithmetic < zero_check < zero_return < scale_reject < aggregate):
        raise SystemExit(
            "FAIL commercial decimal exactness preflight: arithmetic zero must canonicalize before scale representability rejection"
        )


def main() -> None:
    contracts = CONTRACTS.read_text(encoding="utf-8")
    exact = EXACT.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    require(contracts, "CommercialExactDecimalAccumulator.AddExact(left, right, label)", "exact addition routing")
    require(contracts, "CommercialExactDecimalAccumulator.SubtractExact(left, right, label)", "exact subtraction routing")
    reject(contracts, "result == left", "operand-equality precision heuristic")
    require(exact, "internal static decimal AddExact", "signed exact addition helper")
    require(exact, "internal static decimal SubtractExact", "signed exact subtraction helper")
    require(exact, "BigInteger.Abs(signedCoefficient)", "96-bit representability check")
    require(exact, "coefficient > MaximumDecimalCoefficient", "decimal coefficient bound")
    require(exact, "maximumAtScale", "true-overflow versus precision-loss classification")
    require_arithmetic_zero_before_scale_rejection(exact)
    require(smoke, "CommercialBoundaryMagnitude, 0.6m", "high-magnitude fractional regression")
    require(smoke, "Commercial addition precision loss: boundary addition.", "addition fail-closed assertion")
    require(smoke, "Commercial subtraction precision loss: boundary subtraction.", "subtraction fail-closed assertion")
    require(smoke, "decimal.MaxValue, 1m", "true addition overflow compatibility regression")
    require(smoke, "decimal.MinValue, 1m", "true subtraction overflow compatibility regression")
    require(smoke, "true addition overflow overflowed decimal arithmetic.", "addition overflow contract assertion")
    require(smoke, "true subtraction overflow overflowed decimal arithmetic.", "subtraction overflow contract assertion")
    require(smoke, "CommercialAdditionCancellationCanonicalizesZeroScale", "addition cancellation canonical-zero regression")
    require(smoke, "CommercialSubtractionCancellationCanonicalizesZeroScale", "subtraction cancellation canonical-zero regression")
    require(smoke, "DecimalScale(result)", "zero representation scale assertion")
    require(smoke, "InvokeCommercialGuard", "already-registered commercial guard regression execution")

    print("PASS commercial decimal add/subtract exactness source guard")


if __name__ == "__main__":
    main()
