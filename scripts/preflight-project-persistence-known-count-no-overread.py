#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectPersistenceStampKnownCountNoOverreadSmoke.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ProjectPersistenceStamp source")
if not SMOKE.is_file():
    errors.append("missing persistence known-count smoke")

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

for token in (
    "using (var enumerator = values.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (bounded.Count == maximumEntries)",
    "if (bounded.Count >= knownCount)",
    "var value = enumerator.Current;",
    "bounded.Add(value);",
    'RequireStableCountEvidence(values, knownCount, collectionLabel, "before traversal", maximumEntries);',
    'RequireStableCountEvidence(values, knownCount, collectionLabel, "after traversal", maximumEntries);',
    "values is ICollection<T> genericCollection",
    "values is IReadOnlyCollection<T> readOnlyCollection",
    "values is System.Collections.ICollection nonGenericCollection",
):
    if token not in source:
        errors.append("persistence source missing no-overread/stability contract: " + token)

method = source.find("private static List<T> SnapshotBounded<T>")
move = source.find("while (enumerator.MoveNext())", method)
ceiling = source.find("bounded.Count == maximumEntries", move)
known = source.find("bounded.Count >= knownCount", move)
current = source.find("var value = enumerator.Current;", move)
retain = source.find("bounded.Add(value);", move)
under = source.find("if (bounded.Count != knownCount)", retain)
post = source.find('RequireStableCountEvidence(values, knownCount, collectionLabel, "after traversal", maximumEntries);', under)
returned = source.find("return bounded;", post)
if not (0 <= method < move < ceiling < known < current < retain < under < post < returned):
    errors.append("SnapshotBounded ordering must be MoveNext -> selected hard ceiling -> known Count -> Current -> retain -> exact count -> post Count rebind -> return")

if "foreach (var value in values)" in source[method:source.find("private static void RequireStableCountEvidence", method)]:
    errors.append("SnapshotBounded must not use foreach because foreach reads Current before loop-body admission guards")

for token in (
    "KnownCountOverrunStopsBeforeUnexpectedCurrent();",
    "MaximumCeilingStopsBeforeUnexpectedCurrent();",
    "PostTraversalCountDriftFailsClosed();",
    "ConflictingCountSurfacesFailBeforeEnumeration();",
    "StableCountedInputStillMaterializesExactly();",
    "Equal(2, values.MoveNextCalls",
    "Equal(1, values.CurrentReads",
    "Equal(10_001, values.MoveNextCalls",
    "Equal(10_000, values.CurrentReads",
    "count changed or conflicted after traversal",
    "new object[] { values, knownCount, label, 10_000 }",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("persistence smoke missing adversarial Count/Current regression: " + token)

print("QS3D project persistence known-count Current no-overread preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: persistence snapshots gate Count/selected ceiling before Current and rebind supported Count evidence before publication.")
