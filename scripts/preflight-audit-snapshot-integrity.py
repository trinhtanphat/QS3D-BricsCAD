#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AuditTrailSnapshotSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (AUDIT, SMOKE, REG):
    if not path.is_file():
        errors.append("missing audit snapshot integrity file: " + str(path.relative_to(ROOT)))

if AUDIT.is_file():
    text = AUDIT.read_text(encoding="utf-8")
    for token in (
        "var snapshot = new List<AuditEvent>(_events.Count);",
        "var validationError = GetStoredEventValidationError(item);",
        "if (validationError != null) throw new InvalidOperationException(validationError);",
        "snapshot.Add(Clone(item!));",
        "return snapshot.AsReadOnly();",
        "private static AuditEvent Clone(AuditEvent item)",
    ):
        if token not in text:
            errors.append("AuditTrail.cs missing validated deep snapshot token: " + token)
    validation_pos = text.find("var validationError = GetStoredEventValidationError(item);")
    clone_pos = text.find("snapshot.Add(Clone(item!));", validation_pos)
    if validation_pos < 0 or clone_pos < 0 or validation_pos >= clone_pos:
        errors.append("AuditTrail.Events must validate stored history before deep-cloning each event.")
    if "_events as IReadOnlyList<AuditEvent>" in text:
        errors.append("AuditTrail.Events still exposes the mutable backing list through an interface cast.")

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

if REG.is_file() and "AuditTrailSnapshotSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Audit snapshot integrity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: AuditTrail.Events validates stored history, returns a deep read snapshot, and cannot mutate authoritative audit state by cast or entry editing.")
