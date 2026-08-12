#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DAUTOLINKHOSTS must not create/cache an empty QS3D project directly")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)" not in text:
        errors.append("QS3DAUTOLINKHOSTS must preview selected semantic targets from existing read-only project state")
    if "ExistingProjectMutationContext.TryGet(document, out var project)" not in text:
        errors.append("QS3DAUTOLINKHOSTS must bind the canonical existing project for mutation")
    if "Auto Host không tạo project mới" not in text:
        errors.append("missing fail-closed user-facing project lifecycle message")
    if "ProjectStateSnapshot.Capture(project)" not in text:
        errors.append("Auto Host semantic mutation batch must keep rollback snapshot coverage")
    if "ResolveSelectedOpenings(previewProject, selected)" not in text or "ResolveSelectedOpenings(project, selected)" not in text:
        errors.append("Auto Host must resolve selected Door/WallOpening targets both before and after canonical bind")
    if "project.ChangeVersion != expectedChangeVersion" not in text:
        errors.append("Auto Host must fail closed on same-project semantic version drift after preview")
    if "expectedOpeningIds.SetEquals(openings.Select(x => x.Id))" not in text:
        errors.append("Auto Host must revalidate the selected Opening target set after canonical bind")

    selected_index = text.find("var selected = ReadSelectedHandles(document);")
    empty_selection_index = text.find("if (selected.Count == 0)")
    readonly_index = text.find("if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))")
    preview_resolve_index = text.find("var previewOpenings = ResolveSelectedOpenings(previewProject, selected);")
    zero_target_index = text.find("if (previewOpenings.Count == 0)")
    project_guard_index = text.find("if (!ExistingProjectMutationContext.TryGet(document, out var project))")
    freshness_index = text.find("project.ChangeVersion != expectedChangeVersion")
    canonical_resolve_index = text.find("var openings = ResolveSelectedOpenings(project, selected);")
    target_freshness_index = text.find("expectedOpeningIds.SetEquals(openings.Select(x => x.Id))")
    if min(
        selected_index,
        empty_selection_index,
        readonly_index,
        preview_resolve_index,
        zero_target_index,
        project_guard_index,
        freshness_index,
        canonical_resolve_index,
        target_freshness_index,
    ) < 0:
        errors.append("missing expected Auto Host selection/read-only/canonical freshness ordering tokens")
    elif not (
        selected_index < empty_selection_index < readonly_index < preview_resolve_index < zero_target_index <
        project_guard_index < freshness_index < canonical_resolve_index < target_freshness_index
    ):
        errors.append("Auto Host must reject empty/zero-target selection read-only before one canonical mutation bind, then revalidate project/target freshness")

    if text.count("ExistingProjectMutationContext.TryGet(document, out var project)") != 1:
        errors.append("QS3DAUTOLINKHOSTS must canonicalize the batch mutation project exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Auto Host is side-effect free for empty/zero-target selections, resolves targets read-only, binds canonical existing state once, revalidates project/target freshness, and preserves semantic rollback coverage.")
