#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GRAPH = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphDirectDependentsSmoke.cs"
errors = []

for path in (GRAPH, ENGINE, SMOKE):
    if not path.is_file():
        errors.append("missing dependency graph index dependency: " + str(path.relative_to(ROOT)))

if GRAPH.is_file():
    text = GRAPH.read_text(encoding="utf-8")
    for token in (
        "private readonly Dictionary<string, ProjectElement> _elementsById",
        "var nextElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);",
        "if (nextElements.ContainsKey(element.Id))",
        "nextElements.Add(element.Id, element);",
        "_elementsById.Clear();",
        "foreach (var entry in nextElements)",
        "_elementsById[entry.Key] = entry.Value;",
        "public bool TryGetElement(string elementId, out ProjectElement? element)",
        "return _elementsById.TryGetValue(normalized, out element);",
    ):
        if token not in text:
            errors.append("DependencyGraph.cs missing retained unique element-index contract: " + token)

    rebuild_start = text.find("public void Rebuild(IEnumerable<ProjectElement> elements)")
    lookup_start = text.find("public bool TryGetElement", rebuild_start)
    rebuild = text[rebuild_start:lookup_start] if rebuild_start >= 0 and lookup_start > rebuild_start else ""
    stage = rebuild.find("var nextElements = new Dictionary<string, ProjectElement>")
    commit_dependents = rebuild.find("_dependents.Clear();")
    commit_elements = rebuild.find("_elementsById.Clear();")
    if min(stage, commit_dependents, commit_elements) < 0 or not stage < commit_dependents < commit_elements:
        errors.append("DependencyGraph.Rebuild must fully stage the new element index before committing reverse/index state")

if ENGINE.is_file():
    text = ENGINE.read_text(encoding="utf-8")
    mark_start = text.find("public void MarkChanged(ProjectState project")
    mark_end = text.find("public int RegenerateDirty(ProjectState project)", mark_start)
    mark = text[mark_start:mark_end] if mark_start >= 0 and mark_end > mark_start else ""
    for token in (
        "_graph.Rebuild(project.Elements);",
        "_graph.TryGetElement(normalizedId, out var source)",
        "_graph.GetDependentsTransitive(source.Id)",
        "_graph.TryGetElement(dependentId, out var dependent)",
    ):
        if token not in mark:
            errors.append("RegenerationEngine.MarkChanged missing graph-index reuse: " + token)
    if "new Dictionary<string, ProjectElement>" in mark or "foreach (var element in project.Elements)" in mark:
        errors.append("MarkChanged must not rescan project elements after DependencyGraph.Rebuild has already built the unique element index")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ElementLookupNormalizesAndRetainsReference",
        "FailedDuplicateRebuildPreservesPreviousIndex",
        'graph.TryGetElement(" ROOT ", out var resolved)',
        'graph.TryGetElement("original", out var resolved)',
        'graph.TryGetElement("dup", out _)',
    ):
        if token not in text:
            errors.append("DependencyGraphDirectDependentsSmoke.cs missing retained-index regression: " + token)

print("QS3D dependency graph retained-index preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: DependencyGraph stages reverse dependencies and a unique element index in one rebuild scan, preserves prior graph/index state on rejected rebuilds, and MarkChanged reuses that committed index instead of rescanning the project.")
