from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Interoperability/InteroperabilityFacts.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    require(
        text,
        "MatchesFactSetProvenance",
        "Interoperability fact-set membership must use a dedicated full-provenance predicate.",
    )
    require(
        text,
        "record.Identity.Provenance.MatchesFactSetProvenance(provenance)",
        "InteroperabilityFactSet.Create must reject records whose full provenance differs from the fact-set header.",
    )
    require(
        text,
        "string.Equals(SourceSchemaVersion, other.SourceSchemaVersion, StringComparison.Ordinal)",
        "Fact-set provenance equality must include SourceSchemaVersion.",
    )
    require(
        text,
        "string.Equals(ImportBatchId, other.ImportBatchId, StringComparison.Ordinal)",
        "Fact-set provenance equality must include ImportBatchId.",
    )
    require(
        text,
        "string.Equals(ScopeKey, other.ScopeKey, StringComparison.Ordinal)",
        "Fact-set provenance equality must preserve the existing source scope boundary.",
    )
    print("PASS: interoperability fact-set provenance membership is fail-closed across batch/schema revisions.")


if __name__ == "__main__":
    main()
