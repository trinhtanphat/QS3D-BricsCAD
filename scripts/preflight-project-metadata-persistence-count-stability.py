#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMetadataPersistenceCountSmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing project-metadata Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("internal void ReplacePersistenceState(")
    end = text.find("private bool Remove(string key", start)
    method = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var knownCount = RequireSupportedKnownPersistenceCount(values);",
        "foreach (var item in values)",
        "if (knownCount.HasValue && observedCount >= knownCount.Value)",
        "if (knownCount.HasValue && observedCount != knownCount.Value)",
        "var finalKnownCount = RequireSupportedKnownPersistenceCount(values);",
        "throw MetadataTraversalCountChangedError();",
        "ValidateReserved(next);",
        "_items.Clear();",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("ReplacePersistenceState must rebind Count after exact traversal and reject drift before reserved validation/publication.")
    if method.count("RequireSupportedKnownPersistenceCount(values)") < 2:
        errors.append("ReplacePersistenceState must bind supported Count evidence both before and after caller-controlled traversal.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GenericCountDriftFailsAtomically",
        "ReadOnlyCountDriftFailsAtomically",
        "NonGenericCountDriftFailsAtomically",
        "PostTraversalNegativeCountFailsAtomically",
        "PostTraversalConflictingCountFailsAtomically",
        "StableCountedInputPublishes",
        "PureStreamingInputPublishes",
        "AssertSeedUnchanged",
        "Project metadata persistence input Count changed during traversal.",
        "Project metadata persistence input exposes an invalid negative Count.",
        "Project metadata persistence input exposes conflicting Count contracts.",
    ):
        if token not in text:
            errors.append("project-metadata Count-stability smoke missing regression token: " + token)

print("QS3D project-metadata persistence Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: metadata replacement rebinds deterministic Count evidence after traversal before publication.")
