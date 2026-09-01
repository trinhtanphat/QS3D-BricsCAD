#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyIdentitySmoke.cs"


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        raise AssertionError(f"missing {label}: {token}")
    return pos


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    validator = require(
        source,
        "_ = AuditTrail.ForProject(source).Events;",
        "canonical AuditTrail validation reuse",
    )
    audit_copy = require(source, "target.AuditEvents.Clear();", "audit materialization")
    validate_method = require(source, "private static void ValidateCollectionEntries(ProjectState source)", "snapshot validation method")
    null_guard = require(source, 'RequireNoNullEntries(source.AuditEvents, "audit event");', "audit null guard")
    unique_ids = require(source, 'RequireUniqueIds(source.Zones, x => x.Id, "zone");', "post-audit identity validation")

    if not (audit_copy < validate_method < null_guard < validator < unique_ids):
        raise AssertionError("snapshot audit validation must run inside ValidateCollectionEntries after null checks and before later materialization-dependent validation")

    forbidden = (
        "GetStoredEventValidationError(",
        "ContainsInvalidXmlCharacters(",
        "IsCanonicalOptionalIdentity(",
    )
    validate_slice = source[validate_method:source.find("private static void RequireCanonicalFamilyProperties", validate_method)]
    for token in forbidden:
        if token in validate_slice:
            raise AssertionError("snapshot must reuse AuditTrail validation rather than copy audit policy: " + token)

    for token in (
        "RejectsInvalidDirectAuditHistory();",
        "PreservesCanonicalUnicodeAuditHistory();",
        '"non-UTC audit timestamp"',
        '"padded audit action"',
        '"padded audit element id"',
        '"XML-invalid audit detail"',
        '"padded audit correlation id"',
        '"Review-\\U0001F680"',
        "!ReferenceEquals(copy, audit)",
    ):
        require(smoke, token, "deterministic snapshot audit smoke coverage")

    print("PASS: project state snapshot audit integrity preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
