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
        "foreach (var item in values)",
        "if (knownCount.HasValue && observedCount >= knownCount.Value)",
        "throw MetadataTraversalCountMismatchError(knownCount.Value, observedCount + 1);",
        "observedCount++;",
        "if (item.Key == null)",
        "if (next.ContainsKey(item.Key))",
        "if (next.Count >= MaximumEntries)",
        "if (knownCount.HasValue && observedCount != knownCount.Value)",
        "ValidateReserved(next);",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("ReplacePersistenceState must reject known-Count overrun before unexpected-entry semantic processing while retaining final under-traversal validation.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "KnownCountOverrunWinsBeforeNullKeyValidation",
        "KnownCountOverrunWinsBeforeDuplicateKeyValidation",
        "expected 1, observed 2",
        "AssertSeedUnchanged",
        "public int Count => 1;",
    ):
        if token not in text:
            errors.append("project-metadata known-count-overrun smoke missing regression token: " + token)

print("QS3D project-metadata known-count overrun preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: project metadata persistence rejects known-Count overrun before unexpected-entry semantic processing.")
