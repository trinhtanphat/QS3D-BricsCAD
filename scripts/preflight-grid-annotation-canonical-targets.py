#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GridAnnotationBuilder.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GridAnnotationBuilder.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'RequireCanonicalElements(project, elements, "Grid annotation build")',
        'RequireCanonicalElements(project, new[] { element }, "Grid annotation rebuild")',
        "private static void RequireCanonicalElements(ProjectState project, IReadOnlyList<ProjectElement> elements, string operation)",
        "new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase)",
        "if (candidate == null)",
        "if (canonical.ContainsKey(candidate.Id))",
        "if (!canonical.TryGetValue(element.Id, out var current))",
        "if (!ReferenceEquals(current, element))",
        "Refusing",
    )
    for token in required:
        if token not in text:
            errors.append("Grid annotation canonical-target contract missing: " + token)

    build_guard = text.find('RequireCanonicalElements(project, elements, "Grid annotation build")')
    snapshot = text.find("var rollback = ProjectStateSnapshot.Capture(project);")
    transaction = text.find("document.Database.TransactionManager.StartTransaction()")
    if build_guard < 0 or snapshot < 0 or transaction < 0 or not (build_guard < snapshot < transaction):
        errors.append("batch canonical validation must complete before semantic snapshot/CAD transaction")

    rebuild_start = text.find("internal static void RebuildInTransaction(")
    rebuild_guard = text.find('RequireCanonicalElements(project, new[] { element }, "Grid annotation rebuild")', rebuild_start)
    replace = text.find("ReplaceOne(document, transaction, project, element);", rebuild_start)
    if rebuild_start < 0 or rebuild_guard < 0 or replace < 0 or rebuild_guard > replace:
        errors.append("transactional rebuild must reject stale/detached Grid before ReplaceOne")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid annotation build/rebuild requires unique canonical ProjectElement instances before native replacement or metadata mutation.")
