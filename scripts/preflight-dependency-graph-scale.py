#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphScaleSmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing dependency scale safety file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    for needle in (
        "new Stack<VisitFrame>()",
        "Dependency ordering contains duplicate semantic element id",
        "Dependency ordering cannot contain a null semantic element",
        "Dependency cycle detected at",
    ):
        if needle not in text:
            errors.append("DependencyGraph missing iterative/integrity guard: " + needle)
    if "private static void Visit(" in text or "Visit(dependency" in text:
        errors.append("DependencyGraph.TopologicalDirtyOrder must not recurse through semantic dependency chains")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "const int depth = 8192",
        "DeepDirtyChainOrdersWithoutProcessRecursion",
        "DuplicateDirtyIdsFailClosed",
        "[ModuleInitializer]",
    ):
        if needle not in text:
            errors.append("Dependency graph scale smoke missing regression token: " + needle)

print("QS3D dependency graph scale preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: dirty semantic dependency ordering is iterative, duplicate-safe and covered by a deep-chain regression.")
