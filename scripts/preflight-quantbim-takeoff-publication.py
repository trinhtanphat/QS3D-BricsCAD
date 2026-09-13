#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimTakeoffPublication.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsQuantBimTakeoffPublicationSmoke.cs"
DOC = ROOT / "docs/benchmark-parity/quantbim-takeoff-publication.md"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"QuantBIM takeoff-publication preflight failed: missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"QuantBIM takeoff-publication preflight failed: forbidden {label}: {needle}")


def main() -> int:
    for path in (SOURCE, SMOKE, DOC):
        if not path.is_file():
            raise SystemExit(f"QuantBIM takeoff-publication preflight failed: missing {path.relative_to(ROOT)}")

    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    require(source, "QuantBimTakeoffPublicationSnapshot", "immutable publication snapshot contract")
    require(source, "QuantBimTakeoffPublisher", "publication coordinator")
    require(source, "expectedRevision", "generation fence")
    require(source, "StringComparison.Ordinal", "exact revision comparison")
    require(source, "duplicate element GUID", "duplicate IFC identity rejection")
    require(source, "missing IFC element", "missing selection rejection")
    require(source, "QuantBimTakeoffPublicationCodec", "versioned interchange codec")
    require(source, "QS3D-QUANTBIM-TAKEOFF-PUBLICATION/1", "versioned interchange header")
    require(source, "CultureInfo.InvariantCulture", "culture-stable numeric interchange")
    require(source, "new IfcQtoWorkbench().Aggregate", "reuse of canonical IFC QTO aggregation")
    forbid(source, "Bricscad.", "BricsCAD dependency")
    forbid(source, "Autodesk.AutoCAD", "CAD-host dependency")

    require(smoke, "[ModuleInitializer]", "automatic deterministic smoke registration")
    require(smoke, "stale expected revision rejected", "stale-generation regression")
    require(smoke, "missing selection GUID rejected", "missing-selection regression")
    require(smoke, "duplicate IFC GUID rejected", "duplicate-identity regression")
    require(smoke, "deterministic publication roundtrip", "interchange roundtrip regression")

    require(doc, "BricsCAD", "architecture-boundary documentation")
    require(doc, "generation", "generation-fence documentation")
    require(doc, "evidence", "evidence traceability documentation")
    return 0


if __name__ == "__main__":
    sys.exit(main())
