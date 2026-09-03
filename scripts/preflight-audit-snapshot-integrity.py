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
        "snapshot.Add(Clone(item));",
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
    clone_pos = text.find("snapshot.Add(Clone(item));", validation_pos)
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
    if "_events as IReadOnlyList<AuditEvent>" in text:
        errors.append("AuditTrail.Events still exposes the mutable backing list through an interface cast.")

    events_pos = text.find("public IReadOnlyList<AuditEvent> Events")
    for_project_pos = text.find("public static AuditTrail ForProject", events_pos)
    events_text = text[events_pos:for_project_pos] if events_pos >= 0 and for_project_pos > events_pos else ""
    events_loop_pos = events_text.find("while (true)")
    events_pre_count_pos = events_text.find("RequireStableHistoryCount(storedCount);", events_loop_pos)
    events_move_pos = events_text.find("if (!enumerator.MoveNext())", events_pre_count_pos)
    events_terminal_count_pos = events_text.find("RequireStableHistoryCount(storedCount);", events_move_pos + 1)
    events_break_pos = events_text.find("break;", events_terminal_count_pos)
    events_post_count_pos = events_text.find("RequireStableHistoryCount(storedCount);", events_break_pos + 1)
    events_gate_pos = events_text.find("RequireCanReadCurrent(storedCount, observed);", events_post_count_pos)
    events_current_pos = events_text.find("var item = enumerator.Current;", events_gate_pos)
    if not (
        events_loop_pos >= 0
        and events_loop_pos < events_pre_count_pos < events_move_pos < events_terminal_count_pos
        < events_break_pos < events_post_count_pos < events_gate_pos < events_current_pos
    ):
        errors.append("AuditTrail.Events must rebind stored Count before/after MoveNext and before Current.")

    record_pos = text.find("public void Record(")
    clear_pos = text.find("public void Clear()", record_pos)
    record_text = text[record_pos:clear_pos] if record_pos >= 0 and clear_pos > record_pos else ""
    record_validate_pos = record_text.find(
        "ValidateExistingHistory(requireAppendCapacity: true, additionalTextCharacters: newTextCharacters);"
    )
    record_add_pos = record_text.find("_events.Add(item);")
    if record_validate_pos < 0 or record_add_pos < 0 or record_validate_pos >= record_add_pos:
        errors.append("AuditTrail.Record must validate existing history and aggregate text capacity before adding a new audit event.")

    clear_method_pos = text.find("public void Clear()")
    validate_method_pos = text.find("private int ValidateExistingHistory", clear_method_pos)
    clear_text = text[clear_method_pos:validate_method_pos] if clear_method_pos >= 0 and validate_method_pos > clear_method_pos else ""
    clear_validate_pos = clear_text.find("var observed = ValidateExistingHistory(requireAppendCapacity: false);")
    clear_noop_pos = clear_text.find("if (observed == 0) return;")
    clear_mutation_pos = clear_text.find("_events.Clear();")
    if clear_validate_pos < 0 or clear_noop_pos < 0 or clear_mutation_pos < 0 or not (clear_validate_pos < clear_noop_pos < clear_mutation_pos):
        errors.append("AuditTrail.Clear must validate/traverse stored history before deciding that Clear is a no-op.")
    if "_events.Count == 0" in clear_text:
        errors.append("AuditTrail.Clear must not trust mutable backing Count as an early-return substitute for traversal.")

    supported_count_method_pos = text.find("private int RequireSupportedHistoryCount", validate_method_pos)
    validate_text = text[validate_method_pos:supported_count_method_pos] if validate_method_pos >= 0 and supported_count_method_pos > validate_method_pos else ""
    validate_count_pos = validate_text.find("var storedCount = RequireSupportedHistoryCount(requireAppendCapacity);")
    validate_enumerator_pos = validate_text.find("using (var enumerator = _events.GetEnumerator())", validate_count_pos)
    validate_loop_pos = validate_text.find("while (true)", validate_enumerator_pos)
    validate_pre_count_pos = validate_text.find("RequireStableHistoryCount(storedCount);", validate_loop_pos)
    validate_move_pos = validate_text.find("if (!enumerator.MoveNext())", validate_pre_count_pos)
    validate_terminal_count_pos = validate_text.find("RequireStableHistoryCount(storedCount);", validate_move_pos + 1)
    validate_break_pos = validate_text.find("break;", validate_terminal_count_pos)
    validate_post_count_pos = validate_text.find("RequireStableHistoryCount(storedCount);", validate_break_pos + 1)
    validate_can_read_pos = validate_text.find("RequireCanReadCurrent(storedCount, observed);", validate_post_count_pos)
    validate_current_pos = validate_text.find("var existing = enumerator.Current;", validate_can_read_pos)
    validate_equality_pos = validate_text.find("RequireObservedHistoryCount(storedCount, observed);", validate_current_pos)
    validate_stable_pos = validate_text.find("RequireStableHistoryCount(storedCount);", validate_equality_pos)
    validate_return_pos = validate_text.find("return observed;", validate_stable_pos)
    if not (
        validate_count_pos >= 0
        and validate_count_pos < validate_enumerator_pos < validate_loop_pos < validate_pre_count_pos
        < validate_move_pos < validate_terminal_count_pos < validate_break_pos < validate_post_count_pos
        < validate_can_read_pos < validate_current_pos < validate_equality_pos < validate_stable_pos < validate_return_pos
    ):
        errors.append(
            "AuditTrail.ValidateExistingHistory must enforce Count -> pre-Move rebound -> MoveNext -> post-Move rebound -> Current-read guard -> Current -> equality -> stable Count -> return ordering."
        )
    if "while (enumerator.MoveNext())" in validate_text or "foreach (var existing in _events)" in validate_text:
        errors.append("AuditTrail.ValidateExistingHistory must keep explicit Count-safe enumeration around caller-controlled MoveNext/Current.")

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
        "RejectsOverreportedRecordWithoutMutation();",
        "RejectsOverreportedClearWithoutMutation();",
        "private sealed class DishonestCountHistory : IList<AuditEvent>",
        "public int Count => _reportedCount;",
        "Equal(0, history.AddRequests, \"underreported record add requests\");",
        "Equal(0, history.ClearRequests, \"underreported clear mutation requests\");",
        "Equal(0, history.AddRequests, \"overreported record add requests\");",
        "Equal(0, history.ClearRequests, \"overreported clear mutation requests\");",
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

print("PASS: AuditTrail reads and mutations validate stored history with Count-safe MoveNext/Current ordering and reject dishonest history before mutation.")
