#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

AUTHORITY_CALL_RE = re.compile(
    r"RequireCurrentMutationAuthority\s*\(\s*"
    r"document\s*,\s*project\s*,\s*expectedProjectId\s*,\s*"
    r"expectedChangeVersion\s*,\s*expectedOpeningIds\s*,\s*selected\s*\)\s*;",
    re.MULTILINE,
)

if not PATH.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    authority_call = AUTHORITY_CALL_RE.search(text)
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
    if authority_call is None:
        errors.append("Auto Host must revalidate active-document/exact-project generation after CAD matching and before semantic mutation")
    if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in text:
        errors.append("Auto Host final mutation fence must require the exact active managed Document")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in text:
        errors.append("Auto Host final mutation fence must re-read canonical project authority")
    if "!ReferenceEquals(currentProject, project)" not in text:
        errors.append("Auto Host final mutation fence must reject replacement project instances")
    if "expectedOpeningIds.SetEquals(currentOpenings.Select(x => x.Id))" not in text:
        errors.append("Auto Host final mutation fence must reject target-set drift after CAD matching")

    selected_index = text.find("var selected = ReadSelectedHandles(document);")
    empty_selection_index = text.find("if (selected.Count == 0)")
    readonly_index = text.find("if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))")
    preview_resolve_index = text.find("var previewOpenings = ResolveSelectedOpenings(previewProject, selected);")
    zero_target_index = text.find("if (previewOpenings.Count == 0)")
    project_guard_index = text.find("if (!ExistingProjectMutationContext.TryGet(document, out var project))")
    freshness_index = text.find("project.ChangeVersion != expectedChangeVersion")
    canonical_resolve_index = text.find("var openings = ResolveSelectedOpenings(project, selected);")
    target_freshness_index = text.find("expectedOpeningIds.SetEquals(openings.Select(x => x.Id))")
    scan_commit_index = text.find("transaction.Commit();", target_freshness_index)
    final_authority_index = authority_call.start() if authority_call is not None else -1
    semantic_service_index = text.find("var service = new HostLinkService();")
    rollback_index = text.find("var rollback = ProjectStateSnapshot.Capture(project);")
    link_index = text.find("service.LinkOpening(project, item.Opening.Id, item.HostId);")
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
        scan_commit_index,
        final_authority_index,
        semantic_service_index,
        rollback_index,
        link_index,
    ) < 0:
        errors.append("missing expected Auto Host selection/read-only/canonical/precommit freshness ordering tokens")
    elif not (
        selected_index < empty_selection_index < readonly_index < preview_resolve_index < zero_target_index <
        project_guard_index < freshness_index < canonical_resolve_index < target_freshness_index <
        scan_commit_index < final_authority_index < semantic_service_index < rollback_index < link_index
    ):
        errors.append("Auto Host must revalidate the exact document/project/target generation after CAD scan and before rollback snapshot or semantic linking")

    if text.count("ExistingProjectMutationContext.TryGet(document, out var project)") != 1:
        errors.append("QS3DAUTOLINKHOSTS must canonicalize the batch mutation project exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Auto Host binds canonical state once, revalidates exact document/project/target authority after CAD matching, and preserves rollback coverage before semantic mutation.")
