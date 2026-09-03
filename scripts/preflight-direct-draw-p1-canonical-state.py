#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
P1 = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs"
BUILD = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
errors = []

for path in (P1, BUILD):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    p1 = P1.read_text(encoding="utf-8")
    build = BUILD.read_text(encoding="utf-8")

    capture_id = "var createdElementId = createdElement.Id;"
    nested_build = "new Build3DCommands().Build3D();"
    post_active = 'EnsureActive(document, operation + " / post QS3DBUILD3D");'
    canonical_lookup = "string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase)"
    missing_guard = "Semantic element Direct Draw P1 không còn tồn tại sau QS3DBUILD3D; operation được rollback."
    handle_read = 'createdElement.Properties.TryGetValue("GeneratedSolidHandle"'
    cleanup = "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)"
    restore = "rollback.Restore(project)"

    for token in (capture_id, nested_build, post_active, canonical_lookup, missing_guard, handle_read, cleanup, restore):
        if token not in p1:
            errors.append("Direct Draw P1 missing canonical-state token: " + token)

    id_pos = p1.find(capture_id)
    build_pos = p1.find(nested_build)
    active_pos = p1.find(post_active)
    lookup_pos = p1.find(canonical_lookup)
    handle_pos = p1.find(handle_read)
    cleanup_pos = p1.find(cleanup)
    restore_pos = p1.find(restore)
    if min(id_pos, build_pos, active_pos, lookup_pos, handle_pos, cleanup_pos, restore_pos) >= 0:
        if not id_pos < build_pos < active_pos < lookup_pos < handle_pos < cleanup_pos < restore_pos:
            errors.append("Direct Draw P1 must capture stable Id -> nested build -> revalidate active DWG -> re-resolve canonical element -> read generated ownership; rollback must erase CAD before restoring project")

        between_build_and_lookup = p1[build_pos + len(nested_build):lookup_pos]
        if 'createdElement.Properties.TryGetValue("GeneratedSolidHandle"' in between_build_and_lookup:
            errors.append("Direct Draw P1 must not read GeneratedSolidHandle from the pre-QS3DBUILD3D element reference")
        if "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement)" in between_build_and_lookup:
            errors.append("Direct Draw P1 must not use stale generated ownership before canonical re-resolution")

    if "semanticRollback.Restore(project);" not in build:
        errors.append("QS3DBUILD3D no longer exposes the snapshot-restore behavior that requires P1 canonical re-resolution")
    stable_failure = 'Report(document, "QS3DBUILD3D lỗi: không thể hoàn tất native rebuild cho selection hiện tại.");'
    if stable_failure not in build:
        errors.append("QS3DBUILD3D stable command-surface failure reporting contract changed; review P1 nested-call failure propagation")
    for forbidden in ("operationError.Message", "ex.Message", "uiError.Message"):
        if forbidden in build:
            errors.append("QS3DBUILD3D nested-call surface must not expose raw caught host detail: " + forbidden)

print("QS3D Direct Draw P1 canonical-state preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Direct Draw P1 never trusts a pre-QS3DBUILD3D ProjectElement reference after the nested command can restore ProjectState; canonical ownership is re-resolved by stable Id before live-handle validation and rollback cleanup, while the nested Build3D failure surface remains stable/redacted.")
