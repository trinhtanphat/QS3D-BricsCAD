#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "src/QS3D.Core/Services/SelectionState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SelectionStateSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (STATE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing selection-state contract file: " + str(path.relative_to(ROOT)))

if STATE.is_file():
    text = STATE.read_text(encoding="utf-8")
    for token in (
        ".Where(x => !string.IsNullOrWhiteSpace(x))",
        ".Select(x => x.Trim())",
        "StringComparer.OrdinalIgnoreCase",
        "if (_ids.SetEquals(next)) return;",
        "if (_ids.Count == 0) return;",
    ):
        if token not in text:
            errors.append("SelectionState.cs missing canonical-state token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ReplaceTrimsDeduplicatesAndIgnoresBlankIds();",
        "CanonicallyEquivalentReplaceDoesNotRaiseChanged();",
        "ClearRaisesOnlyWhenStateChanges();",
        'state.Replace(new[] { " A ", "a", " B", "   " });',
        'state.Replace(new[] { " b ", " A ", "a" });',
    ):
        if token not in text:
            errors.append("SelectionStateSmoke.cs missing regression token: " + token)

if REG.is_file() and "SelectionStateSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("selection-state smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] semantic selection state is statically guarded for trimmed/case-insensitive IDs, canonical-equivalent replacement and no-op clear semantics")
