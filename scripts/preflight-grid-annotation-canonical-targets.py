#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GridAnnotationBuilder.cs"
HEALTH = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedGridAnnotationRuntimeHealthService.cs"
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
        "var authoritativeOwnerId = source.OwnerId;",
        "authoritativeOwnerId.IsNull || !authoritativeOwnerId.IsValid",
        "ValidatePrevious(document.Database, transaction, project, element, authoritativeOwnerId)",
        "ErasePrevious(transaction, project, element, previous, authoritativeOwnerId)",
        "if (entity.OwnerId != authoritativeOwnerId)",
        "drift sang owner space/layout khác authoritative Grid source",
        "owner space/layout changed after validation",
        "transaction.GetObject(authoritativeOwnerId, OpenMode.ForWrite, false) as BlockTableRecord",
    )
    for token in required:
        if token not in text:
            errors.append("Grid annotation canonical/owner-space/audit contract missing: " + token)

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

    if "project.Touch();" in build_body:
        errors.append("Grid annotation Build must not perform an explicit project.Touch(); revision is audit-owned by ReplaceOne")

    replace_one_start = text.find("private static void ReplaceOne(")
    validate_previous_start = text.find("private static IReadOnlyList<KeyValuePair<string, ObjectId>> ValidatePrevious(")
    erase_previous_start = text.find("private static void ErasePrevious(")
    audit = text.find("AuditTrail.ForProject(project).Record(", replace_one_start)
    audit_action = text.find('"grid.annotation.replace"', audit)
    if replace_one_start < 0 or audit < replace_one_start or audit_action < audit:
        errors.append("ReplaceOne must retain the grid.annotation.replace AuditTrail record that owns revision advancement")

    owner_capture = text.find("var authoritativeOwnerId = source.OwnerId;", replace_one_start)
    validate_call = text.find("ValidatePrevious(document.Database, transaction, project, element, authoritativeOwnerId)", replace_one_start)
    erase_call = text.find("ErasePrevious(transaction, project, element, previous, authoritativeOwnerId)", replace_one_start)
    owner_open = text.find("transaction.GetObject(authoritativeOwnerId, OpenMode.ForWrite, false) as BlockTableRecord", replace_one_start)
    if min(owner_capture, validate_call, erase_call, owner_open) < 0 or not (owner_capture < validate_call < erase_call < owner_open):
        errors.append("owner-space lifecycle must remain source owner capture -> validate all previous owners -> erase -> reopen authoritative owner for creation")

    if validate_previous_start < 0 or erase_previous_start < 0 or erase_previous_start <= validate_previous_start:
        errors.append("unable to isolate Grid annotation previous-owner validation")
    else:
        validate_body = text[validate_previous_start:erase_previous_start]
        owner_check = validate_body.find("if (entity.OwnerId != authoritativeOwnerId)")
        generated_ownership = validate_body.find("GeneratedGeometryService.RequireMatchingOwnership(")
        result_add = validate_body.find("result.Add(new KeyValuePair<string, ObjectId>(handle, id));")
        if min(owner_check, generated_ownership, result_add) < 0 or not (owner_check < generated_ownership < result_add):
            errors.append("every previous generated annotation must prove authoritative owner space before generated-ownership acceptance")

        erase_end = text.find("private static ObjectId ResolveHandle(", erase_previous_start)
        erase_body = text[erase_previous_start:erase_end] if erase_end > erase_previous_start else ""
        owner_recheck = erase_body.find("if (entity.OwnerId != authoritativeOwnerId)")
        erase_ownership = erase_body.find("GeneratedGeometryService.RequireMatchingOwnership(")
        erase_call_actual = erase_body.find("entity.Erase();")
        if min(owner_recheck, erase_ownership, erase_call_actual) < 0 or not (owner_recheck < erase_ownership < erase_call_actual):
            errors.append("destructive erase must re-check owner space and generated ownership before entity.Erase()")

    rebuild_guard = text.find('RequireCanonicalElements(project, new[] { element }, "Grid annotation rebuild")', rebuild_start)
    rebuild_replace = text.find("ReplaceOne(document, transaction, project, element);", rebuild_start)
    if rebuild_start < 0 or rebuild_guard < 0 or rebuild_replace < 0 or rebuild_guard > rebuild_replace:
        errors.append("transactional rebuild must reject stale/detached Grid before ReplaceOne")

if not HEALTH.is_file():
    errors.append("missing GeneratedGridAnnotationRuntimeHealthService.cs")
else:
    health = HEALTH.read_text(encoding="utf-8")
    health_required = (
        "TryResolveAuthoritativeOwner(",
        "element.SourceHandles",
        '"GRID_ANNOTATION_SOURCE_HANDLE_COUNT"',
        '"GRID_ANNOTATION_SOURCE_HANDLE_INVALID"',
        '"GRID_ANNOTATION_SOURCE_MISSING"',
        '"GRID_ANNOTATION_SOURCE_OWNER_INVALID"',
        "authoritativeOwnerId = source.OwnerId;",
        "owner is BlockTableRecord",
        "bool hasAuthoritativeOwner",
        "ObjectId authoritativeOwnerId",
        "if (hasAuthoritativeOwner && entity.OwnerId != authoritativeOwnerId)",
        '"GRID_ANNOTATION_CAD_OWNER_SPACE_MISMATCH"',
        "drift sang owner space/layout khác authoritative Grid source",
        "GeneratedGeometryService.HasMatchingOwnership(entity, project, element)",
    )
    for token in health_required:
        if token not in health:
            errors.append("Grid annotation runtime-health owner-space contract missing: " + token)

    inspect_start = health.find("public static IReadOnlyList<ModelHealthIssue> Inspect(")
    source_resolution = health.find("var hasAuthoritativeOwner = TryResolveAuthoritativeOwner(", inspect_start)
    inspect_handle = health.find("InspectHandle(document, transaction, project, element, handles[index], index, hasAuthoritativeOwner, authoritativeOwnerId, issues)", inspect_start)
    if min(inspect_start, source_resolution, inspect_handle) < 0 or not (inspect_start < source_resolution < inspect_handle):
        errors.append("runtime health must resolve authoritative source owner before inspecting generated annotation handles")

    handle_start = health.find("private static void InspectHandle(")
    owner_mismatch = health.find("if (hasAuthoritativeOwner && entity.OwnerId != authoritativeOwnerId)", handle_start)
    xdata_ownership = health.find("GeneratedGeometryService.HasMatchingOwnership(entity, project, element)", handle_start)
    text_stale = health.find("if (entity is DBText text)", handle_start)
    if min(handle_start, owner_mismatch, xdata_ownership, text_stale) < 0 or not (handle_start < owner_mismatch < xdata_ownership < text_stale):
        errors.append("runtime health must report owner-space mismatch independently before XData ownership/text freshness diagnostics")

    if "entity.Erase();" in health or "OpenMode.ForWrite" in health:
        errors.append("Grid annotation runtime health must remain read-only and never erase or open CAD objects ForWrite")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid annotation build/rebuild requires unique canonical ProjectElement instances before native replacement or metadata mutation.")
print("PASS: Grid annotation replacement proves generated entities remain in the authoritative source owner space/layout before any destructive erase, and re-checks that invariant at erase.")
print("PASS: Grid annotation runtime health resolves the authoritative Grid source owner and surfaces cross-owner-space/layout drift without native mutation.")
print("PASS: Grid annotation Build revision advancement is audit-owned by ReplaceOne and has no redundant project.Touch().")
