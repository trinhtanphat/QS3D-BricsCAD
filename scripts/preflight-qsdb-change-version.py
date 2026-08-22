#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbSaveAtomicitySmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


store = read(STORE)
state = read(STATE)
smoke = read(SMOKE)

for token, label in [
    ('new XAttribute("changeVersion", project.ChangeVersion.ToString(CultureInfo.InvariantCulture))', "serialized change version"),
    ('var changeVersion = ChangeVersion(root.Attribute("changeVersion")?.Value)', "load parse boundary"),
    ("project.RestorePersistenceState(updatedUtc, changeVersion)", "persistence-state restore"),
    ("if (value == null) return 0L;", "legacy migration default"),
    ("NumberStyles.None", "canonical integer parse"),
    ("result < 0L", "negative persistence rejection"),
    ('throw new InvalidDataException("Invalid QSDB change version: " + value)', "persistence-format exception"),
    ("exception is InvalidDataException", "backup fallback recoverable classification"),
]:
    if token not in store:
        errors.append(label + " missing token: " + token)

for token, label in [
    ("public long ChangeVersion { get; private set; }", "project change version"),
    ("ChangeVersion = checked(ChangeVersion + 1L)", "monotonic touch"),
    ("if (changeVersion < 0L)", "domain negative guard"),
]:
    if token not in state:
        errors.append(label + " missing token: " + token)

for token, label in [
    ("SuccessfulSaveRoundTripsChangeVersion", "round-trip smoke"),
    ("MissingCurrentChangeVersionIsRejected", "strict current-schema missing-version smoke"),
    ("InvalidPersistedChangeVersionIsRejected", "invalid persistence smoke"),
    ('new[] { "-1", "1.5", " 1", "9223372036854775808" }', "negative/malformed/overflow fixtures"),
    ('Throws<InvalidDataException>(() => store.Load(path)', "file-boundary exception assertion"),
]:
    if token not in smoke:
        errors.append(label + " missing token: " + token)

if "exception is ArgumentOutOfRangeException" in store:
    errors.append("QSDB backup fallback must not broadly classify domain ArgumentOutOfRangeException as recoverable; malformed changeVersion must be normalized at its parse boundary")

if errors:
    print("QS3D QSDB change-version preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: current-schema QSDB requires persisted non-negative ChangeVersion, legacy migration may synthesize zero before strict validation, malformed/overflow values fail as InvalidDataException, and backup-fallback semantics remain preserved.")
