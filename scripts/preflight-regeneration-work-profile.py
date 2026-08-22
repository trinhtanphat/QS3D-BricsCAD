#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfileSmoke.cs"
DOC = ROOT / "docs/REGENERATION-WORK-PROFILE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing regeneration work profile file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
engine = read(ENGINE)
smoke = read(SMOKE)
doc = read(DOC)

for token in (
    "RegenerationWorkScope",
    "RegenerationWorkItem",
    "RegenerationCategoryWork",
    "RegenerationWorkProfile",
    "RegenerationWorkProfiler",
    "ProfileSubset",
    "TopologicalDirtyOrder",
    "InternalDependencyEdgeCount",
    "MaxDependencyDepth",
    "SemanticDirtyElementCount",
    "GeometryOnlyDirtyElementCount",
    "SourceChangeVersion",
    "Project changed while regeneration work was being profiled",
):
    if token not in source:
        errors.append("regeneration work profiler missing contract token: " + token)

for forbidden in (
    "Stopwatch",
    "RegenerateDirty(",
    "RegenerateDirtySubset(",
    "IElementRegenerator",
    "RegeneratorCatalog",
    "MarkClean(",
    "MarkDirty(",
    "project.Touch()",
):
    if forbidden in source:
        errors.append("regeneration work profiler must remain read-only/non-timing: " + forbidden)

for shared in (
    "Regeneration target id cannot be blank at index ",
    "Regeneration target id must be canonical without surrounding whitespace: ",
    "Duplicate regeneration target id: ",
    "Unknown regeneration target: ",
):
    if shared not in source or shared not in engine:
        errors.append("profiler/engine subset target contract drifted: " + shared)

for token in (
    "ProjectProfileIsDeterministicAndReadOnly",
    "SubsetProfileMirrorsTargetSemantics",
    "GeometryOnlyDirtyWorkIsVisibleButNotSemantic",
    "MalformedTargetsFailClosed",
    "DeepChainProfilesWithoutRecursion",
    "const int count = 2048",
    "ModuleInitializer",
):
    if token not in smoke:
        errors.append("regeneration work profile smoke missing regression token: " + token)

for token in (
    "initial work shape",
    "does not benchmark elapsed time",
    "does not invoke regenerators",
    "LOCAL_ONLY",
    "RegenerateDirtySubset",
):
    if token not in doc:
        errors.append("regeneration work profile handoff missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: deterministic regeneration work profiling remains read-only, mirrors subset target validation, and makes no elapsed-time/runtime performance claim.")
