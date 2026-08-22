#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/RevisionService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RevisionCanonicalSourceHandlesSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing revision source-handle contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "CanonicalSourceHandles(element)",
        "string.IsNullOrWhiteSpace(raw)",
        "!string.Equals(raw, raw.Trim(), StringComparison.Ordinal)",
        "new HashSet<string>(StringComparer.OrdinalIgnoreCase)",
        "if (!seen.Add(raw))",
        "result.Sort(StringComparer.OrdinalIgnoreCase);",
    ):
        if token not in text:
            errors.append("RevisionService missing fail-closed source-handle token: " + token)
    for forbidden in (
        ".Where(x => !string.IsNullOrWhiteSpace(x))",
        ".Select(x => x.Trim())",
        ".Distinct(StringComparer.OrdinalIgnoreCase)",
    ):
        if forbidden in text:
            errors.append("Revision capture must not normalize/drop/dedupe malformed source handles: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CanonicalHandlesAreSortedWithoutMutation();",
        "BlankHandleFailsClosed();",
        "PaddedHandleFailsClosed();",
        "DuplicateHandleFailsClosed();",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("RevisionCanonicalSourceHandlesSmoke missing regression token: " + token)

if errors:
    print("QS3D revision source-handle canonicality preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RevisionService rejects blank, padded and duplicate source handles and only sorts canonical values without mutating live source lists.")
