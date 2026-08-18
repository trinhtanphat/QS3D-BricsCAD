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
        "RequireObservedHistoryCount(storedCount, observed);",
        "return snapshot.AsReadOnly();",
        "private static void RequireObservedHistoryCount(int storedCount, int observed)",
        "private static AuditEvent Clone(AuditEvent item)",
    ):
        if token not in text:
            errors.append("AuditTrail.cs missing validated deep snapshot token: " + token)
    count_pos = text.find("var storedCount = RequireSupportedHistoryCount(requireAppendCapacity: false);")
    snapshot_pos = text.find("var snapshot = new List<AuditEvent>(storedCount);", count_pos)
    validation_pos = text.find("var validationError = GetStoredEventValidationError(item);")
    clone_pos = text.find("snapshot.Add(Clone(item!));", validation_pos)
    equality_pos = text.find("RequireObservedHistoryCount(storedCount, observed);", clone_pos)
    return_pos = text.find("return snapshot.AsReadOnly();", equality_pos)
    if count_pos < 0 or snapshot_pos < 0 or count_pos >= snapshot_pos:
        errors.append("AuditTrail.Events must allocate its read snapshot from the single validated stored Count.")
    if "new List<AuditEvent>(_events.Count)" in text:
        errors.append("AuditTrail.Events must not re-read mutable backing Count after validation.")
    if validation_pos < 0 or clone_pos < 0 or validation_pos >= clone_pos:
        errors.append("AuditTrail.Events must validate stored history before deep-cloning each event.")
    if equality_pos < 0 or return_pos < 0 or equality_pos >= return_pos:
        errors.append("AuditTrail.Events must reject a stored Count that disagrees with traversal before returning its snapshot.")
    if text.count("RequireObservedHistoryCount(storedCount, observed);") < 2:
        errors.append("AuditTrail reads and modification validation must both enforce stored Count versus traversal equality.")
    if "var storedCount = RequireSupportedHistoryCount(requireAppendCapacity);" not in text:
        errors.append("AuditTrail modification validation must preserve the single validated stored Count for traversal equality.")
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
        "RejectsUnderreportedReadWithoutMutation();",
        "RejectsUnderreportedRecordWithoutMutation();",
        "RejectsUnderreportedClearWithoutMutation();",
        "RejectsOverreportedReadWithoutMutation();",
        "private sealed class DishonestCountHistory : IList<AuditEvent>",
        "public int Count => _reportedCount;",
        "Equal(0, history.AddRequests, \"underreported record add requests\");",
        "Equal(0, history.ClearRequests, \"underreported clear mutation requests\");",
        "var history = new DishonestCountHistory(2, CanonicalEvent());",
    ):
        if token not in text:
            errors.append("AuditTrailHistoryBoundSmoke.cs missing Count-versus-traversal fail-closed regression token: " + token)

if REG.is_file() and "AuditTrailSnapshotSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Audit snapshot integrity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: AuditTrail reads and mutations validate stored history, enforce Count-versus-traversal equality, and reject dishonest history before mutation.")