#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
SAFETY = ROOT / "src" / "QS3D.Core" / "Persistence" / "PersistencePathSafety.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


store = STORE.read_text(encoding="utf-8")
safety = SAFETY.read_text(encoding="utf-8")

exception_token = "internal sealed class PersistencePathSafetyException : InvalidDataException"
if exception_token not in safety:
    fail("persistence path-safety violations need a typed InvalidDataException subtype so recovery can distinguish trust failures from corrupt data")

redirect_throws = (
    'throw new PersistencePathSafetyException("QS3D refused a redirected or reparse-point " + role + " path.")',
    'throw new PersistencePathSafetyException("QS3D refused a redirected or reparse-point " + role + " pathname generation.")',
)
for token in redirect_throws:
    if token not in safety:
        fail(f"redirect/reparse rejection must use the typed path-safety failure: {token}")

classifier_start = store.index("private static bool IsRecoverableDataFailure")
classifier_end = store.index("private static XElement Map", classifier_start)
classifier = store[classifier_start:classifier_end]

if "exception is PersistencePathSafetyException" not in classifier:
    fail("backup fallback recoverability classifier must explicitly exclude typed persistence path-safety failures")
if "exception is InvalidDataException" not in classifier:
    fail("ordinary InvalidDataException corruption must remain recoverable through validated backup fallback")

path_failure_index = classifier.index("exception is PersistencePathSafetyException")
data_failure_index = classifier.index("exception is InvalidDataException")
if path_failure_index > data_failure_index:
    fail("typed path-safety exclusion must take precedence over the broader InvalidDataException recovery case")

fallback_start = store.index("public ProjectLoadResult LoadWithBackupFallback")
fallback_end = store.index("private static XDocument Serialize", fallback_start)
fallback = store[fallback_start:fallback_end]
if "catch (Exception primary) when (IsRecoverableDataFailure(primary))" not in fallback:
    fail("primary fallback catch must remain filtered through IsRecoverableDataFailure")
if "catch (Exception backup) when (IsRecoverableDataFailure(backup))" not in fallback:
    fail("backup corruption aggregation must remain filtered through IsRecoverableDataFailure")

print("PASS: QSDB backup fallback distinguishes recoverable corrupt data from fail-closed persistence path-safety violations")
