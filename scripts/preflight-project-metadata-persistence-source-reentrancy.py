#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMetadataPersistenceSourceReentrancySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "private long _mutationVersion;",
    "var targetMutationVersion = _mutationVersion;",
    "RequireStablePersistenceTarget(targetMutationVersion);",
    "private void RequireStablePersistenceTarget(long expectedMutationVersion)",
    "Project metadata changed while persistence input was being enumerated.",
    "var nextMutationVersion = checked(_mutationVersion + 1L);",
    "_mutationVersion = nextMutationVersion;",
]
required_smoke = [
    "FinalCountReentrancyCannotOverwriteNewerMetadata();",
    "MoveNextReentrancyCannotOverwriteNewerMetadata();",
    "StableCountedReplacementPreservesContract();",
    "CountReads == 7",
    "project.Metadata[\"intruder\"] = \"nested\";",
    "False(project.Metadata.ContainsKey(\"outer\")",
    "Equal(7, input.CountReads, \"stable Count observations\")",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Project metadata persistence-source reentrancy preflight failed; missing contract token(s): " + repr(missing))

final_count = source.find("var finalKnownCount = RequireSupportedKnownPersistenceCount(values);")
post_target = source.find("RequireStablePersistenceTarget(targetMutationVersion);", final_count)
publication = source.find("_items.Clear();", final_count)
if final_count < 0 or post_target < 0 or publication < 0 or not (final_count < post_target < publication):
    raise SystemExit("Project metadata persistence-source reentrancy preflight failed: final Count must be followed by target-version validation before publication")

move_next = source.find("if (!enumerator.MoveNext()) break;")
post_move = source.find("RequireStablePersistenceTarget(targetMutationVersion);", move_next)
current = source.find("var item = enumerator.Current;", move_next)
post_current = source.find("RequireStablePersistenceTarget(targetMutationVersion);", current)
if move_next < 0 or post_move < 0 or current < 0 or post_current < 0 or not (move_next < post_move < current < post_current):
    raise SystemExit("Project metadata persistence-source reentrancy preflight failed: MoveNext/Current callbacks must be target-version guarded")

print("PASS project metadata persistence-source reentrancy guard")
