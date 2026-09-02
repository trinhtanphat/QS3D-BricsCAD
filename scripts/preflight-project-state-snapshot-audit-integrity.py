#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyIdentitySmoke.cs"
NULL_FIDELITY = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelitySmoke.cs"


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        raise AssertionError(f"missing {label}: {token}")
    return pos


def main() -> int:
    snapshot = SNAPSHOT.read_text(encoding="utf-8")
    audit = AUDIT.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    null_fidelity = NULL_FIDELITY.read_text(encoding="utf-8")

    validator = require(
        snapshot,
        "AuditTrail.ValidateSnapshotHistory(source);",
        "snapshot-compatible centralized AuditTrail validation",
    )
    audit_copy = require(snapshot, "target.AuditEvents.Clear();", "audit materialization")
    validate_method = require(snapshot, "private static void ValidateCollectionEntries(ProjectState source)", "snapshot validation method")
    null_guard = require(snapshot, 'RequireNoNullEntries(source.AuditEvents, "audit event");', "audit null guard")
    unique_ids = require(snapshot, 'RequireUniqueIds(source.Zones, x => x.Id, "zone");', "post-audit identity validation")

    if not (audit_copy < validate_method < null_guard < validator < unique_ids):
        raise AssertionError("snapshot audit validation must run inside ValidateCollectionEntries after null checks and before later materialization-dependent validation")

    validate_slice = snapshot[validate_method:snapshot.find("private static void RequireCanonicalFamilyProperties", validate_method)]
    for token in (
        "GetStoredEventValidationError(",
        "ContainsInvalidXmlCharacters(",
        "IsCanonicalOptionalIdentity(",
    ):
        if token in validate_slice:
            raise AssertionError("snapshot must centralize audit policy in AuditTrail rather than copy it: " + token)

    snapshot_api = require(audit, "internal static void ValidateSnapshotHistory(ProjectState project)", "narrow snapshot audit API")
    snapshot_mode = require(audit, "allowNullActionBacking: true", "legacy null-action snapshot mode")
    shared_validator = require(audit, "GetStoredEventValidationError(existing, allowNullActionBacking)", "shared stored-event validator")
    null_branch = require(audit, "if (!allowNullActionBacking)", "strict null-action branch")
    if not (snapshot_api < snapshot_mode < shared_validator < null_branch):
        raise AssertionError("AuditTrail snapshot validation must explicitly route through the shared validator with narrow null-action compatibility")

    require(audit, "var validationError = GetStoredEventValidationError(item);", "strict public Events validation")
    require(audit, "ValidateExistingHistory(requireAppendCapacity: true, additionalTextCharacters: newTextCharacters);", "strict Record validation")
    require(audit, "ValidateExistingHistory(requireAppendCapacity: false);", "strict Clear validation")

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

    require(null_fidelity, "Action = null!", "historical null audit-action fixture")
    require(null_fidelity, 'IsNull(audit.Action, label + " audit action");', "historical null audit-action fidelity assertion")

    print("PASS: project state snapshot audit integrity preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
