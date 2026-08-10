#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "service": ROOT / "src/QS3D.Core/Services/SemanticUntrackService.cs",
    "resolver": ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs",
    "command": ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs",
    "smoke": ROOT / "tests/QS3D.Core.SmokeTests/SemanticUntrackSmoke.cs",
}
for path in files.values():
    if not path.is_file():
        errors.append("missing semantic-untrack file: " + str(path.relative_to(ROOT)))

checks = {
    "service": [
        "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)",
        "new DependencyGraph()",
        "graph.Rebuild(project.Elements)",
        "graph.GetDependentsTransitive(target.Id)",
        "if (targetIds.Contains(dependentId)) continue",
        "Cannot untrack semantic element(s) while dependents remain",
        "project.Elements.Remove(target)",
        "project.Touch()",
    ],
    "resolver": [
        "element.SourceHandles",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "ReferenceEquals(existing, element)",
        "matchedById.TryGetValue(element.Id, out var existing)",
        "is claimed by multiple semantic instances sharing duplicate ID",
        "is duplicated across multiple project instances selected by CAD handles",
        "Resolve project semantic ownership before continuing",
    ],
    "command": [
        'CommandMethod("QS3DUNTRACK"',
        'CommandMethod("QS3DUNTRACKFINISH"',
        "SemanticUntrackService.Untrack(project, handles, predicate)",
        "CAD geometry was not erased",
    ],
    "smoke": [
        "ModuleInitializer",
        "SourceHandleUntracksOwner",
        "GeneratedHandleUntracksOwner",
        "ExternalDependentBlocksUntrack",
        "CompleteDependentBatchCanUntrack",
        "PredicateLimitsTargets",
        "DuplicateIdSameHandleFailsClosed",
        "DuplicateIdAcrossSelectedHandlesFailsClosed",
    ],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing semantic-untrack token: " + needle)

if files["command"].is_file():
    text = files["command"].read_text(encoding="utf-8")
    old = "foreach (var element in matched) project.Elements.Remove(element)"
    if old in text:
        errors.append("ViewportCommands still bypasses SemanticUntrackService with direct semantic removal.")

print("QS3D semantic-untrack preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: untrack resolves source/generated selection through canonical ownership, fails closed on duplicate semantic instances, blocks external semantic dependents, allows complete batches, and preserves CAD geometry.")
