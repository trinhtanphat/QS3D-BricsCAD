#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMetadataPersistenceMidCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/project-metadata-persistence-mid-count-integrity.md"
errors = []

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        errors.append("missing project-metadata mid-Count integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("internal void ReplacePersistenceState(")
    end = source.find("private bool Remove(string key", start)
    method = source[start:end] if start >= 0 and end > start else ""
    required = (
        "var knownCount = RequireSupportedKnownPersistenceCount(values);",
        "using (var enumerator = values.GetEnumerator())",
        "while (true)",
        "RequireStableKnownPersistenceCount(values, knownCount);",
        "if (!enumerator.MoveNext()) break;",
        "if (knownCount.HasValue && observedCount >= knownCount.Value)",
        "if (observedCount >= MaximumEntries)",
        "var item = enumerator.Current;",
        "if (knownCount.HasValue && observedCount != knownCount.Value)",
        "var finalKnownCount = RequireSupportedKnownPersistenceCount(values);",
        "ValidateReserved(next);",
        "_items.Clear();",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("metadata persistence traversal ordering no longer pins admission, guarded advancement, Current, final cardinality/rebind and publication.")
    if method.count("RequireStableKnownPersistenceCount(values, knownCount)") < 3:
        errors.append("metadata persistence must revalidate admitted Count before MoveNext, after successful MoveNext and after traversal.")
    first_stable = method.find("RequireStableKnownPersistenceCount(values, knownCount);")
    move_next = method.find("if (!enumerator.MoveNext()) break;", first_stable)
    second_stable = method.find("RequireStableKnownPersistenceCount(values, knownCount);", first_stable + 1)
    overrun = method.find("if (knownCount.HasValue && observedCount >= knownCount.Value)", second_stable)
    cap = method.find("if (observedCount >= MaximumEntries)", overrun)
    current = method.find("var item = enumerator.Current;", cap)
    if min(first_stable, move_next, second_stable, overrun, cap, current) < 0 or not (
        first_stable < move_next < second_stable < overrun < cap < current
    ):
        errors.append("metadata persistence must fail closed on Count drift around MoveNext before caller Current is observed.")
    if "while (enumerator.MoveNext())" in method or "foreach (var item in values)" in method:
        errors.append("metadata persistence regressed to implicit caller advancement without a pre-MoveNext Count-stability boundary.")
    for token in (
        "private static void RequireStableKnownPersistenceCount(",
        "var observedCount = RequireSupportedKnownPersistenceCount(values);",
        "throw MetadataTraversalCountChangedError();",
    ):
        if token not in source:
            errors.append("metadata persistence stable-Count helper token missing: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "CountDriftAfterCurrentFailsBeforeNextMoveNext",
        "MoveNextInducedCountDriftFailsBeforeCurrent",
        "CrossInterfaceConflictAfterCurrentFailsBeforeNextMoveNext",
        "NegativeCountAfterCurrentFailsBeforeNextMoveNext",
        "StableMultiInterfaceCountPublishes",
        "PureStreamingInputRemainsSupported",
        "AssertSeedUnchanged",
        "Equal(1, input.MoveNextCalls",
        "Equal(0, input.CurrentReads",
    ):
        if token not in smoke:
            errors.append("project-metadata mid-Count smoke missing regression token: " + token)

if RUNBOOK.is_file():
    runbook = RUNBOOK.read_text(encoding="utf-8")
    for token in ("before every `MoveNext`", "after successful `MoveNext`", "before `Current`", "atomic"):
        if token not in runbook:
            errors.append("project-metadata mid-Count runbook token missing: " + token)

print("QS3D project-metadata persistence mid-traversal Count integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: metadata persistence rejects mid-traversal Count drift before further advancement or Current publication work.")
