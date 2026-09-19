#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbBackupIdentitySmoke.cs"

store = STORE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_store_markers = (
    "LoadWithBackupFallback",
    "TryGetCanonicalProjectIdentity",
    "Validated QSDB backup project identity does not match the failed primary project identity.",
)
required_smoke_markers = (
    "RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsPadded",
    "RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsMissing",
    "RejectsCrossProjectBackupWhenParseablePrimaryIdentityIsBlank",
)

missing = [marker for marker in required_store_markers if marker not in store]
missing += [marker for marker in required_smoke_markers if marker not in smoke]
if missing:
    raise SystemExit("QSDB backup identity preflight failed; missing contract marker(s): " + ", ".join(missing))

print("QSDB backup identity preflight passed")
