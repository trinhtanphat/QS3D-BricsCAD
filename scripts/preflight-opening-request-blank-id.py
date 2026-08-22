#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing OpeningBooleanService.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

method_start = source.find("private static HashSet<string>? NormalizeRequestedOpenings")
method_end = source.find("private static bool IsOpening", method_start)
if method_start < 0 or method_end < 0:
    errors.append("missing NormalizeRequestedOpenings boundary")
    method = ""
else:
    method = source[method_start:method_end]

required = [
    "if (openingIds == null) return null;",
    "foreach (var raw in openingIds)",
    "if (string.IsNullOrWhiteSpace(raw))",
    'throw new InvalidOperationException("Target opening id cannot be empty.");',
    "requested.Add(raw.Trim());",
    "project.FindElement(id)",
]
for token in required:
    if token not in method:
        errors.append("missing blank-id fail-closed contract: " + token)

old_subset_filter = "openingIds.Where(x => !string.IsNullOrWhiteSpace(x))"
if old_subset_filter in method:
    errors.append("explicit requested opening ids must not silently drop blank/null entries")

reject_index = method.find("if (string.IsNullOrWhiteSpace(raw))")
lookup_index = method.find("project.FindElement(id)")
if reject_index < 0 or lookup_index < 0 or reject_index > lookup_index:
    errors.append("blank requested ids must be rejected before semantic target lookup")

normalize_call = source.find("var requested = NormalizeRequestedOpenings(project, openingIds);")
rollback_capture = source.find("var rollback = ProjectStateSnapshot.Capture(project);")
boolean_mutation = source.find("hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter)")
if normalize_call < 0 or rollback_capture < 0 or normalize_call > rollback_capture:
    errors.append("requested ids must be normalized before project rollback/mutation scope")
if normalize_call < 0 or boolean_mutation < 0 or normalize_call > boolean_mutation:
    errors.append("requested ids must be normalized before CAD boolean mutation")

print("QS3D opening requested-id fail-closed preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: explicit targeted opening requests reject blank/null ids before semantic lookup, project rollback scope and CAD boolean mutation; null collection keeps all-linked semantics.")
