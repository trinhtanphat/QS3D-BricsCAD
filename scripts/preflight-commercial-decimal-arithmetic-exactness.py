#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTRACTS = ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs"
EXACT = ROOT / "src/QS3D.Core/Commercial/CommercialExactDecimalAccumulator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialDecimalArithmeticExactnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL commercial decimal exactness preflight: missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL commercial decimal exactness preflight: stale {label}: {needle}")


def main() -> None:
    contracts = CONTRACTS.read_text(encoding="utf-8")
    exact = EXACT.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    require(contracts, "CommercialExactDecimalAccumulator.AddExact(left, right, label)", "exact addition routing")
    require(contracts, "CommercialExactDecimalAccumulator.SubtractExact(left, right, label)", "exact subtraction routing")
    reject(contracts, "result == left", "operand-equality precision heuristic")
    require(exact, "internal static decimal AddExact", "signed exact addition helper")
    require(exact, "internal static decimal SubtractExact", "signed exact subtraction helper")
    require(exact, "BigInteger.Abs(signedCoefficient)", "96-bit representability check")
    require(exact, "coefficient > MaximumDecimalCoefficient", "decimal coefficient bound")
    require(smoke, "BoundaryMagnitude, 0.6m", "high-magnitude fractional regression")
    require(smoke, "Commercial addition precision loss: boundary addition.", "addition fail-closed assertion")
    require(smoke, "Commercial subtraction precision loss: boundary subtraction.", "subtraction fail-closed assertion")
    require(registration, "CommercialDecimalArithmeticExactnessSmoke.Run();", "smoke registration")

    print("PASS commercial decimal add/subtract exactness source guard")


if __name__ == "__main__":
    main()
