#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GRAPH = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphDirectDependentsSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (GRAPH, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing dependency graph contract file: " + str(path.relative_to(ROOT)))

if GRAPH.is_file():
    text = GRAPH.read_text(encoding="utf-8")
    for token in (
        "var normalizedSourceId = (sourceId ?? string.Empty).Trim();",
        "queue.Enqueue(normalizedSourceId);",
        "seen.Add(normalizedSourceId);",
        "next.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
        "return result.AsReadOnly();",
        "dependents.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
    ):
        if token not in text:
            errors.append("DependencyGraph.cs missing deterministic/normalized traversal token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "TransitiveLookupNormalizesAndIsDeterministic();",
        "TransitiveCycleDoesNotReturnSource();",
        'graph.GetDependentsTransitive(" ROOT ")',
        'new[] { "A-CHILD", "Z-CHILD", "A-LEAF", "Z-LEAF" }',
        'new[] { "A", "B" }',
        'graph.GetDependentsTransitive(" ").Count != 0',
    ):
        if token not in text:
            errors.append("DependencyGraphDirectDependentsSmoke.cs missing transitive regression token: " + token)

if REGISTRATION.is_file() and "DependencyGraphDirectDependentsSmoke.Run();" not in REGISTRATION.read_text(encoding="utf-8"):
    errors.append("dependency graph smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] dependency graph direct/transitive lookup is statically guarded for normalized IDs, deterministic traversal, bounded cycles and blank/missing lookups")
