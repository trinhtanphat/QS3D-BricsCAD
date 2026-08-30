#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingCurrentCountIntegritySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing Grid Current-count integrity file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("public static IReadOnlyList<GridLabelAssignment> Renumber(")
    end = text.find("public static string FormatLabel", start)
    renumber = text[start:end] if start >= 0 and end > start else ""
    rebound = "RequireStableKnownCountDuringTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);"
    pre_move = renumber.find(rebound)
    move = renumber.find("if (!enumerator.MoveNext()) break;", pre_move + 1)
    post_move = renumber.find(rebound, move + 1)
    overrun = renumber.find("ids.Count == knownCount.Value", post_move + 1)
    ceiling = renumber.find("ids.Count == MaxGridBatch", overrun + 1)
    current = renumber.find("var value = enumerator.Current;", ceiling + 1)
    post_current = renumber.find(rebound, current + 1)
    validate = renumber.find("ids.Add(Required(value", post_current + 1)
    positions = (pre_move, move, post_move, overrun, ceiling, current, post_current, validate)
    if not renumber or min(positions) < 0 or list(positions) != sorted(positions):
        errors.append("Grid renumber must order Count rebound -> MoveNext -> Count rebound -> bounds -> Current -> Count rebound -> value validation/staging.")
    if renumber.count(rebound) < 3:
        errors.append("Grid renumber must contain the post-Current traversal Count rebound in addition to pre/post-MoveNext rebounds.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "CurrentInducedCountDriftWinsBeforeValueValidation",
        "StableCountedCurrentSucceeds",
        "CurrentDriftCollection",
        "_owner._count = 2;",
        "Grid renumber target source known Count changed during traversal.",
        "null!",
    ):
        if token not in text:
            errors.append("Grid Current-count smoke missing regression token: " + token)

print("QS3D Grid renumber Current-induced Count integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid renumber revalidates admitted Count immediately after caller-controlled Current and before value validation/staging.")
