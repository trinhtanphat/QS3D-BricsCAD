#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqCurrentCountIntegritySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing MEP/TBQ Current-count integrity file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("public IReadOnlyList<MepTbqReportRow> BuildReport(")
    end = text.find("public string SerializeCsv", start)
    method = text[start:end] if start >= 0 and end > start else ""
    rebound = "RequireStableKnownCount(groups, knownCount);"
    move = method.find("var moved = enumerator.MoveNext();")
    post_move = method.find(rebound, move + 1)
    current = method.find("var group = enumerator.Current;", post_move + 1)
    post_current = method.find(rebound, current + 1)
    validate = method.find("if (group == null)", current + 1)
    stage = method.find("rows.Add(new MepTbqReportRow(group));", validate + 1)
    if min(move, post_move, current, post_current, validate, stage) < 0 or not (move < post_move < current < post_current < validate < stage):
        errors.append("MEP/TBQ BuildReport must order MoveNext -> Count rebound -> Current -> Count rebound -> group validation -> row staging.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "CurrentInducedDriftWinsBeforeGroupValidation",
        "StableCountedCurrentSucceeds",
        "CurrentDriftGroups",
        "_owner._count = 2;",
        "MEP/TBQ report source Count changed during enumeration.",
        "null!",
    ):
        if token not in text:
            errors.append("MEP/TBQ Current-count smoke missing regression token: " + token)

print("QS3D MEP/TBQ Current-induced Count integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: MEP/TBQ BuildReport revalidates admitted Count immediately after Current and before validation/staging.")
