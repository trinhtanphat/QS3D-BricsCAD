#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMetadataCurrentCountSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing project metadata Current Count-stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var knownCount = RequireSupportedKnownPersistenceCount(values);",
        "while (true)",
        "RequireStableKnownPersistenceCount(values, knownCount);",
        "if (!enumerator.MoveNext()) break;",
        "var item = enumerator.Current;",
        "observedCount++;",
        "if (item.Key == null)",
        "next.Add(item.Key, item.Value ?? string.Empty);",
        "ValidateReserved(next);",
        "_items.Clear();",
    ):
        if token not in source:
            errors.append("project metadata source missing Current Count-stability token: " + token)

    loop = source.find("while (true)")
    before_move = source.find("RequireStableKnownPersistenceCount(values, knownCount);", loop)
    move_next = source.find("if (!enumerator.MoveNext()) break;", loop)
    after_move = source.find("RequireStableKnownPersistenceCount(values, knownCount);", before_move + 1)
    current = source.find("var item = enumerator.Current;", loop)
    after_current = source.find("RequireStableKnownPersistenceCount(values, knownCount);", current)
    observed = source.find("observedCount++;", current)
    null_check = source.find("if (item.Key == null)", current)
    add = source.find("next.Add(item.Key, item.Value ?? string.Empty);", current)
    validate = source.find("ValidateReserved(next);", add)
    publish = source.find("_items.Clear();", validate)
    if min(loop, before_move, move_next, after_move, current, after_current, observed, null_check, add, validate, publish) < 0 or not (
        loop < before_move < move_next < after_move < current < after_current < observed < null_check < add < validate < publish
    ):
        errors.append("project metadata persistence must rebind Count around MoveNext and immediately after Current before item staging and publication")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CurrentInducedCountDriftFailsBeforeItemValidation();",
        "DriftOnCurrentCollection",
        "_owner._drifted = true;",
        'new KeyValuePair<string, string>(null!, "must-not-be-validated")',
        '"Project metadata persistence input Count changed during traversal."',
        "Equal(1, input.MoveNextCalls",
        "Equal(1, input.CurrentReads",
        "Equal(1, project.Metadata.Count",
    ):
        if token not in smoke:
            errors.append("project metadata Current Count smoke missing assertion/control: " + token)

print("QS3D project metadata Current Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: project metadata persistence rebinds admitted Count immediately after Current before returned-item staging/publication.")
