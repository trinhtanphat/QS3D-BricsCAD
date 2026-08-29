#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMetadataKnownCountOverrunSmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing project-metadata known-count-overrun file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("internal void ReplacePersistenceState(")
    end = text.find("private bool Remove(string key", start)
    method = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var knownCount = RequireSupportedKnownPersistenceCount(values);",
        "using (var enumerator = values.GetEnumerator())",
        "while (true)",
        "RequireStableKnownPersistenceCount(values, knownCount);",
        "if (!enumerator.MoveNext()) break;",
        "if (knownCount.HasValue && observedCount >= knownCount.Value)",
        "throw MetadataTraversalCountMismatchError(knownCount.Value, observedCount + 1);",
        "if (observedCount >= MaximumEntries)",
        "var item = enumerator.Current;",
        "observedCount++;",
        "if (item.Key == null)",
        "if (next.ContainsKey(item.Key))",
        "if (knownCount.HasValue && observedCount != knownCount.Value)",
        "ValidateReserved(next);",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("ReplacePersistenceState must use stable-Count -> MoveNext -> Count/safety guards -> Current -> semantic processing, while retaining final under-traversal validation.")

    first_stable = method.find("RequireStableKnownPersistenceCount(values, knownCount);")
    move_next = method.find("if (!enumerator.MoveNext()) break;", first_stable)
    second_stable = method.find("RequireStableKnownPersistenceCount(values, knownCount);", first_stable + 1)
    overrun = method.find("if (knownCount.HasValue && observedCount >= knownCount.Value)", second_stable)
    cap = method.find("if (observedCount >= MaximumEntries)", overrun)
    current = method.find("var item = enumerator.Current;", cap)
    if min(first_stable, move_next, second_stable, overrun, cap, current) < 0 or not (
        first_stable < move_next < second_stable < overrun < cap < current
    ):
        errors.append("ReplacePersistenceState must revalidate admitted Count around MoveNext and keep known-count overrun/cap rejection before caller-controlled Current.")

    if "while (enumerator.MoveNext())" in method or "foreach (var item in values)" in method:
        errors.append("ReplacePersistenceState must not use implicit advancement because Count stability and known-count overrun must be enforced before caller-controlled Current.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "KnownCountOverrunWinsBeforeUnexpectedCurrent",
        "KnownCountOverrunWinsBeforeNullKeyValidation",
        "KnownCountOverrunWinsBeforeDuplicateKeyValidation",
        "StableCountedInputRemainsAccepted",
        "ThrowOnUnexpectedCurrent",
        "MoveNextCalls",
        "CurrentAccesses",
        "Equal(2, input.MoveNextCalls",
        "Equal(1, input.CurrentAccesses",
        "IReadOnlyCollection<KeyValuePair<string, string>>",
        "ICollection",
        "AssertSeedUnchanged",
    ):
        if token not in text:
            errors.append("project-metadata known-count-overrun smoke missing regression token: " + token)

print("QS3D project-metadata known-count overrun preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: project metadata persistence rejects known-Count overrun before caller-controlled Current and semantic processing under stable Count evidence.")
