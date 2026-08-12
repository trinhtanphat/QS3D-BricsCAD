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
        "AuditTrail.ForProject(project).Record(",
        '"grid.annotation.replace"',
    )
    for token in required:
        if token not in text:
            errors.append("Grid annotation canonical/audit contract missing: " + token)

    build_start = text.find("public static int Build(")
    rebuild_start = text.find("internal static void RebuildInTransaction(")
    if build_start < 0 or rebuild_start < 0 or rebuild_start <= build_start:
        errors.append("unable to isolate Grid annotation Build lifecycle")
        build_body = ""
    else:
        build_body = text[build_start:rebuild_start]

    build_guard = build_body.find('RequireCanonicalElements(project, elements, "Grid annotation build")')
    snapshot = build_body.find("var rollback = ProjectStateSnapshot.Capture(project);")
    transaction = build_body.find("document.Database.TransactionManager.StartTransaction()")
    replace = build_body.find("foreach (var element in elements) ReplaceOne(document, transaction, project, element);")
    commit = build_body.find("transaction.Commit();")
    if min(build_guard, snapshot, transaction, replace, commit) < 0 or not (build_guard < snapshot < transaction < replace < commit):
        errors.append("batch lifecycle must remain canonical validation -> semantic snapshot -> CAD transaction -> audited ReplaceOne batch -> CAD commit")

    # ReplaceOne records grid.annotation.replace through AuditTrail, and AuditTrail owns the
    # semantic ProjectState.Touch for each successful annotation mutation. A second Build-level
    # Touch would double-advance ChangeVersion beyond its audit-owned mutations.
    if "project.Touch();" in build_body:
        errors.append("Grid annotation Build must not perform an explicit project.Touch(); revision is audit-owned by ReplaceOne")

    replace_one_start = text.find("private static void ReplaceOne(")
    audit = text.find("AuditTrail.ForProject(project).Record(", replace_one_start)
    audit_action = text.find('"grid.annotation.replace"', audit)
    if replace_one_start < 0 or audit < replace_one_start or audit_action < audit:
        errors.append("ReplaceOne must retain the grid.annotation.replace AuditTrail record that owns revision advancement")

    rebuild_guard = text.find('RequireCanonicalElements(project, new[] { element }, "Grid annotation rebuild")', rebuild_start)
    rebuild_replace = text.find("ReplaceOne(document, transaction, project, element);", rebuild_start)
    if rebuild_start < 0 or rebuild_guard < 0 or rebuild_replace < 0 or rebuild_guard > rebuild_replace:
        errors.append("transactional rebuild must reject stale/detached Grid before ReplaceOne")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid annotation build/rebuild requires unique canonical ProjectElement instances before native replacement or metadata mutation.")
print("PASS: Grid annotation Build revision advancement is audit-owned by ReplaceOne and has no redundant project.Touch().")
