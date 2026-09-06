#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
SAFETY = ROOT / "src" / "QS3D.Core" / "Persistence" / "PersistencePathSafety.cs"
REDIRECT_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "PersistenceRedirectedPathSmoke.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


store = STORE.read_text(encoding="utf-8")
safety = SAFETY.read_text(encoding="utf-8")
redirect_smoke = REDIRECT_SMOKE.read_text(encoding="utf-8")

exception_token = "internal sealed class PersistencePathSafetyException : IOException"
if exception_token not in safety:
    fail("persistence path-safety violations need a typed IOException so backup recovery cannot classify trust failures as corrupt data")

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

if "exception is InvalidDataException" not in classifier:
    fail("ordinary InvalidDataException corruption must remain recoverable through validated backup fallback")
if "exception is IOException" in classifier or "PersistencePathSafetyException" in classifier:
    fail("backup fallback must not broaden recovery to IOException/path-safety failures")

fallback_start = store.index("public ProjectLoadResult LoadWithBackupFallback")
fallback_end = store.index("private static XDocument Serialize", fallback_start)
fallback = store[fallback_start:fallback_end]
if "catch (Exception primary) when (IsRecoverableDataFailure(primary))" not in fallback:
    fail("primary fallback catch must remain filtered through IsRecoverableDataFailure")
if "catch (Exception backup) when (IsRecoverableDataFailure(backup))" not in fallback:
    fail("backup corruption aggregation must remain filtered through IsRecoverableDataFailure")

if '"QS3D.Core.Persistence.PersistencePathSafetyException"' not in redirect_smoke:
    fail("redirected-path smoke must expect the exact typed persistence path-safety exception")
if "exception is IOException" not in redirect_smoke:
    fail("redirected-path smoke must retain the non-recoverable IO exception-family contract")
if "if (exception is InvalidDataException) return true;" in redirect_smoke:
    fail("redirected-path smoke must not accept the old recoverable InvalidDataException path-trust contract")
if "return exception is InvalidOperationException && IsRedirectRefusal(exception.InnerException);" not in redirect_smoke:
    fail("redirected project-lock smoke must preserve typed path refusal through the lock wrapper")

print("PASS: QSDB backup fallback keeps corrupt-data recovery while path-safety failures remain fail-closed typed IO failures")
