from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MATH = ROOT / "src" / "QS3D.Core" / "Services" / "QuantityMath.cs"
SEMANTIC = ROOT / "src" / "QS3D.Core" / "Services" / "SemanticRegenerators.cs"
STRUCTURAL = ROOT / "src" / "QS3D.Core" / "Services" / "StructuralRegenerator.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "WallOpeningHostCanonicalitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "quantity-positive-failclosed.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"forbidden {label}: {token}")


def main() -> int:
    math = MATH.read_text(encoding="utf-8")
    semantic = SEMANTIC.read_text(encoding="utf-8")
    structural = STRUCTURAL.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(math, "public static double Positive(double value)", "shared positive quantity helper")
    require(math, "if (!IsFinite(value) || value < 0d)", "fail-closed finite/non-negative validation")
    require(math, "return value == 0d ? 0d : value;", "canonical zero preservation")
    forbid(math, "value > 0d && IsFinite(value) ? value : 0d", "silent invalid-to-zero normalization")

    require(semantic, "Quantities.TryGetValue(\"OpeningAreaM2\", out var stored)) area = QuantityMath.Positive(stored);", "architectural linked-opening cache consumer")
    require(structural, "Quantities.TryGetValue(\"OpeningAreaM2\", out var stored)) area = QuantityMath.Positive(stored);", "structural linked-opening cache consumer")

    require(smoke, "ArchitecturalWallRejectsCorruptCleanOpeningCache", "architectural corrupt-cache regression")
    require(smoke, "StructuralWallRejectsCorruptCleanOpeningCache", "structural corrupt-cache regression")
    require(smoke, "DirtyOpeningRecomputesInsteadOfTrustingCorruptCache", "dirty-child recompute control")
    require(smoke, "NegativeSemanticDimensionFailsClosed", "negative semantic dimension regression")
    require(smoke, "double.NaN", "non-finite cache regression")
    require(smoke, "double.PositiveInfinity", "positive infinity cache regression")
    require(smoke, "double.NegativeInfinity", "negative infinity cache regression")

    require(runbook, "QuantityMath.Positive", "runbook helper contract")
    require(runbook, "OpeningAreaM2", "runbook wall cache boundary")

    print("PASS quantity positive fail-closed preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
