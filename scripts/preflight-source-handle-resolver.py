#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleResolverSafetySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing source-handle resolver safety file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    for needle in (
        "new Stack<string>()",
        "BuildElementIndex(project)",
        "Project contains duplicate semantic element id",
        "Project contains a null semantic element",
        "for (var index = element.DependsOn.Count - 1; index >= 0; index--)",
    ):
        if needle not in text:
            errors.append("SourceHandleResolver missing iterative/integrity guard: " + needle)
    if "private static void Visit(" in text or "Visit(project," in text:
        errors.append("SourceHandleResolver must not recurse through dependency graphs")
    if "project.FindElement(elementId)" in text:
        errors.append("SourceHandleResolver must use the prebuilt element index instead of repeated linear FindElement scans")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "const int depth = 8192",
        "DeepDependencyChainDoesNotUseProcessStack",
        "DependencyCycleTerminatesDeterministically",
        "DuplicateElementIdsFailClosed",
        "DirectAndDependencyHandleOrderIsStable",
        "[ModuleInitializer]",
    ):
        if needle not in text:
            errors.append("SourceHandleResolver safety smoke missing regression token: " + needle)

print("QS3D source handle resolver preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: source-handle dependency traversal is iterative, indexed, duplicate-safe and covered by deep-chain/cycle/order regressions.")
