#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AuditTrailSnapshotSmoke.cs"
HISTORY_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AuditTrailHistoryBoundSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (AUDIT, SMOKE, HISTORY_SMOKE, REG):
    if not path.is_file():
        errors.append("missing audit snapshot integrity file: " + str(path.relative_to(ROOT)))

if AUDIT.is_file():
    text = AUDIT.read_text(encoding="utf-8")
    for token in (
        "var storedCount = RequireSupportedHistoryCount(requireAppendCapacity: false);",
        "var snapshot = new List<AuditEvent>(storedCount);",
        "var validationError = GetStoredEventValidationError(item);",
        "if (validationError != null) throw new InvalidOperationException(validationError);",
        "snapshot.Add(Clone(item!));",
        "return snapshot.AsReadOnly();",
        "private static AuditEvent Clone(AuditEvent item)",
    ):
        if token not in text:
            errors.append("AuditTrail.cs missing validated deep snapshot token: " + token)
    count_pos = text.find("var storedCount = RequireSupportedHistoryCount(requireAppendCapacity: false);")
    snapshot_pos = text.find("var snapshot = new List<AuditEvent>(storedCount);", count_pos)
    validation_pos = text.find("var validationError = GetStoredEventValidationError(item);")
    clone_pos = text.find("snapshot.Add(Clone(item!));", validation_pos)
    if count_pos < 0 or snapshot_pos < 0 or count_pos >= snapshot_pos:
        errors.append("AuditTrail.Events must allocate its read snapshot from the single validated stored Count.")
    if "new List<AuditEvent>(_events.Count)" in text:
        errors.append("AuditTrail.Events must not re-read mutable backing Count after validation.")
    if validation_pos < 0 or clone_pos < 0 or validation_pos >= clone_pos:
        errors.append("AuditTrail.Events must validate stored history before deep-cloning each event.")
    if "_events as IReadOnlyList<AuditEvent>" in text:
        errors.append("AuditTrail.Events still exposes the mutable backing list through an interface cast.")

    clear_pos = text.find("public void Clear()")
    next_method_pos = text.find("private int ValidateExistingHistory", clear_pos)
    clear_text = text[clear_pos:next_method_pos] if clear_pos >= 0 and next_method_pos > clear_pos else ""
    clear_validate_pos = clear_text.find("var observed = ValidateExistingHistory(requireAppendCapacity: false);")
    clear_noop_pos = clear_text.find("if (observed == 0) return;")
    clear_mutation_pos = clear_text.find("_events.Clear();")
    if clear_validate_pos < 0 or clear_noop_pos < 0 or clear_mutation_pos < 0 or not (clear_validate_pos < clear_noop_pos < clear_mutation_pos):
        errors.append("AuditTrail.Clear must validate/traverse stored history before deciding that Clear is a no-op.")
    if "_events.Count == 0" in clear_text:
        errors.append("AuditTrail.Clear must not trust mutable backing Count as an early-return substitute for traversal.")
    if "private int ValidateExistingHistory(bool requireAppendCapacity)" not in text or "return observed;" not in text:
        errors.append("AuditTrail history validation must expose observed traversal count for Clear no-op semantics.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "EventsDoNotLeakBackingCollectionOrMutableEntries",
        "exposed[0].Action = \"MUTATED\"",
        "project.AuditEvents[0].Action == \"first\"",
        "An Audit Events read should be an immutable point-in-time snapshot.",
    ):
        if token not in text:
            errors.append("AuditTrailSnapshotSmoke.cs missing integrity regression token: " + token)

if HISTORY_SMOKE.is_file():
    text = HISTORY_SMOKE.read_text(encoding="utf-8")
    for token in (
        "ClearsDishonestZeroCountHistory();",
        "private sealed class ZeroCountHistory : IList<AuditEvent>",
        "public int Count => 0;",
        "Equal(1, history.EnumeratorRequests, \"zero-count clear enumeration requests\");",
        "Equal(1, history.ClearRequests, \"zero-count clear mutation requests\");",
    ):
        if token not in text:
            errors.append("AuditTrailHistoryBoundSmoke.cs missing dishonest-zero-count Clear regression token: " + token)

if REG.is_file() and "AuditTrailSnapshotSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Audit snapshot integrity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: AuditTrail reads use validated snapshots and Clear validates traversal before zero-history no-op semantics.")