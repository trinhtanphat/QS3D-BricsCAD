#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/SemanticChangeReview.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticChangeReviewSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/semantic-change-review-revision-identity.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: semantic review revision identity guard missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "using System.Xml;", "XML dependency")
    require(source, "raw.Any(char.IsControl)", "control rejection")
    require(source, "XmlConvert.VerifyXmlChars(raw);", "XML character validation")
    require(source, "contains characters that are invalid in XML", "stable invalid-XML diagnostic")

    build = source.index("public SemanticChangeReview Build")
    detach_before = source.index('RevisionSnapshotDetacher.Capture(before, "semantic review before")', build)
    detach_after = source.index('RevisionSnapshotDetacher.Capture(after, "semantic review after")', detach_before)
    before_id = source.index('CanonicalRevisionId(beforeSnapshot.Id, "before revision id")', detach_after)
    after_id = source.index('CanonicalRevisionId(afterSnapshot.Id, "after revision id")', before_id)
    index_before = source.index('Index(beforeSnapshot, "before")', after_id)

    if not (build < detach_before < detach_after < before_id < after_id < index_before):
        raise SystemExit("ERROR: semantic review revision IDs must be admitted from detached snapshots before indexing.")

    for forbidden in (
        'CanonicalRevisionId(before.Id, "before revision id")',
        'CanonicalRevisionId(after.Id, "after revision id")',
    ):
        if forbidden in source[build:]:
            raise SystemExit("ERROR: semantic review revision identity still consults live caller state: " + forbidden)

    require(smoke, "MalformedRevisionIdsFailClosed", "hostile revision-id regression")
    require(smoke, 'Id = "R\\uD800"', "lone high-surrogate probe")
    require(smoke, 'Id = "R\\uDC00"', "lone low-surrogate probe")
    require(smoke, 'Id = "R\\u0001"', "control probe")
    require(smoke, "SupplementaryRevisionIdsRemainExact", "valid supplementary Unicode control")

    require(runbook, "malformed UTF-16", "malformed UTF-16 contract")
    require(runbook, "XML-invalid", "XML-invalid contract")
    require(runbook, "supplementary-plane Unicode", "valid Unicode contract")
    require(runbook, "Runtime: NOT_APPLICABLE", "runtime boundary")

    print("PASS semantic change review revision identity from detached generation")
    return 0


if __name__ == "__main__":
    sys.exit(main())
