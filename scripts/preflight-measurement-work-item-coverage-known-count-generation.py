#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mapping/MeasurementWorkItemCoverageReport.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageKnownCountGenerationSmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)

required_source = [
    "using (var enumerator = findings.GetEnumerator())",
    'RevalidateKnownCount(findings, knownCount, "MoveNext");',
    'RevalidateKnownCount(findings, knownCount, "Current");',
    'RevalidateKnownCount(findings, knownCount, "completed traversal");',
    "private static void RevalidateKnownCount(",
    '"Coverage report input known Count changed during " + boundary',
]
for token in required_source:
    if token not in source:
        errors.append("missing coverage known-Count generation contract: " + token)

loop_start = source.find("while (true)")
move = source.find("var hasNext = enumerator.MoveNext();", loop_start)
move_rebind = source.find('RevalidateKnownCount(findings, knownCount, "MoveNext");', move)
break_check = source.find("if (!hasNext) break;", move_rebind)
capacity = source.find("if (index >= MaximumFindingCount)", break_check)
current = source.find("var finding = enumerator.Current;", capacity)
current_rebind = source.find('RevalidateKnownCount(findings, knownCount, "Current");', current)
null_check = source.find("if (finding == null)", current_rebind)
row_add = source.find("rows.Add(new MeasurementWorkItemCoverageReportRow(finding));", null_check)
if min(loop_start, move, move_rebind, break_check, capacity, current, current_rebind, null_check, row_add) < 0:
    errors.append("could not resolve explicit coverage traversal admission ordering")
elif not (loop_start < move < move_rebind < break_check < capacity < current < current_rebind < null_check < row_add):
    errors.append("coverage Count must rebind after MoveNext and Current before returned finding acceptance")

terminal_rebind = source.find('RevalidateKnownCount(findings, knownCount, "completed traversal");')
final_count = source.find("if (knownCount.HasValue && index != knownCount.Value)", terminal_rebind)
if terminal_rebind < 0 or final_count < 0 or terminal_rebind > final_count:
    errors.append("terminal Count rebound must precede final Count/traversal comparison")

required_smoke = [
    "using System.Runtime.CompilerServices;",
    "MoveNextInducedCountDriftFailsBeforeCurrent();",
    "CurrentInducedCountDriftFailsBeforeFindingAcceptance();",
    "StableCountedSourceRemainsAccepted();",
    "PureStreamingSourceRemainsAccepted();",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic coverage generation smoke contract: " + token)

print("QS3D coverage report known-Count generation-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: counted coverage-report traversal rebinds the admitted Count after MoveNext, after Current and at completion before item/report acceptance, with stable and streaming controls.")
