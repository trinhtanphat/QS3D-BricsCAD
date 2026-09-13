from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SUITE = ROOT / "src/QS3D.Core/BenchmarkParity/QsBenchmarkParitySuite.cs"
QUANT = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimEvidence.cs"


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(message)


def forbid(text: str, token: str, message: str) -> None:
    if token in text:
        raise SystemExit(message)


def main() -> int:
    suite = SUITE.read_text(encoding="utf-8")
    quant = QUANT.read_text(encoding="utf-8")

    require(suite, "SumFiniteQuantities(IEnumerable<double> values, string label)", "benchmark quantity aggregation must use one finite compensated helper")
    require(suite, "new QuantityReportMath.FiniteAccumulator()", "benchmark quantity aggregation helper must use the canonical finite accumulator")
    require(suite, 'SumFiniteQuantities(g.Select(x => x.Quantity), "takeoff inventory quantity")', "TakeoffPackage must use compensated finite aggregation")
    require(suite, 'SumFiniteQuantities(g.Select(x => x.Quantity), "IFC QTO inventory quantity")', "IfcQtoWorkbench must use compensated finite aggregation")
    require(quant, 'SumFiniteQuantities(g.Select(x => x.Quantity), "QuantBIM BOQ quantity")', "QuantBIM BOQ must use compensated finite aggregation")

    forbid(suite, "g.Sum(x => x.Quantity)", "benchmark quantity inventory must not reintroduce naive LINQ Sum(double)")
    forbid(quant, "g.Sum(x => x.Quantity)", "QuantBIM BOQ must not reintroduce naive LINQ Sum(double)")

    takeoff = suite.split("public sealed class TakeoffPackage", 1)[1].split("public sealed class TakeoffInventoryLine", 1)[0]
    if ".OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)" not in takeoff:
        raise SystemExit("Takeoff inventory ordering must be canonical by classification then unit")

    ifc = suite.split("public sealed class IfcQtoWorkbench", 1)[1].split("public sealed class ConstructionCommitment", 1)[0]
    if ".OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)" not in ifc:
        raise SystemExit("IFC QTO inventory ordering must be canonical by classification then unit")

    print("PASS: benchmark quantity aggregation uses compensated finite sums and canonical inventory ordering")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
