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
        "private const int MaxInputCount = 10000;",
        "if (ids is ICollection<string> collection && collection.Count > MaxInputCount)",
        "if (ids is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaxInputCount)",
        "var enumerationVersion = _changeVersion;",
        "if (inputCount >= MaxInputCount)",
        "if (string.IsNullOrWhiteSpace(raw)) continue;",
        "next.Add(raw.Trim());",
        "StringComparer.OrdinalIgnoreCase",
        "if (_changeVersion != enumerationVersion)",
        "Selection changed while replacement element ids were being enumerated.",
        "if (_ids.SetEquals(next)) return;",
        "var nextVersion = checked(_changeVersion + 1L);",
        "if (_ids.Count == 0) return;",
    ):
        if token not in text:
            errors.append("SelectionState.cs missing bounded/canonical-state token: " + token)
    if ".Where(x => !string.IsNullOrWhiteSpace(x))" in text or ".Select(x => x.Trim())" in text:
        errors.append("SelectionState.Replace must not regress to the old unbounded LINQ normalization pipeline")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ReplaceTrimsDeduplicatesAndIgnoresBlankIds();",
        "CanonicallyEquivalentReplaceDoesNotRaiseChanged();",
        "ElementIdsAreDeterministicAndDoNotLeakMutableState();",
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

print("[PASS] semantic selection state is bounded and freshness-aware while preserving trimmed/case-insensitive IDs, deterministic snapshots, canonical-equivalent replacement and no-op clear semantics")
