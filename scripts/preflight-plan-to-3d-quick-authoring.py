#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs"
DOC = ROOT / "docs/PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
errors = []

if not SOURCE.is_file():
    errors.append("missing PlanTo3DCommands.cs")
if not DOC.is_file():
    errors.append("missing PLAN-TO-3D-WORKFLOW.md")
if not INBOX.is_file():
    errors.append("missing LOCAL-AGENT-INBOX.md")


def local_section(text, heading):
    start = text.find(heading)
    if start < 0:
        errors.append("missing local handoff section: " + heading)
        return ""
    end = text.find("\n## ", start + len(heading))
    return text[start:] if end < 0 else text[start:end]

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")
    inbox = INBOX.read_text(encoding="utf-8")
    local014 = local_section(inbox, "## LOCAL-014")

    for token in (
        '[CommandMethod("QS3DCONVERT2D", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DCONVERT2D", promptStyle: false)',
        '[CommandMethod("QS3DPLAN2WALLS", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DPLAN2WALLS", promptStyle: false)',
        '[CommandMethod("QS3DCONVERT2DADV", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DCONVERT2DADV", promptStyle: true)',
        'private static void ConvertPlanWalls(string operation, bool promptStyle)',
        'DirectDrawProjectPreviewContext.Capture(document)',
        'var defaultsProject = projectPreview.DefaultsProject;',
        'var defaultThicknessM = defaultsProject != null ? FamilyNumber(defaultsProject, "ThicknessM", 0.2d) : 0.2d;',
        'var defaultHeightM = defaultsProject != null ? FamilyNumber(defaultsProject, "HeightM", 3.0d) : 3.0d;',
        'var defaultBottomOffsetM = defaultsProject != null ? FamilyFiniteNumber(defaultsProject, "BottomOffsetM", 0d) : 0d;',
        'promptStyle\n                    ? PromptPositiveMeters',
        'promptStyle\n                    ? PromptFiniteMeters',
        'RequireSameSources(sources, refreshedSources)',
        'projectPreview.ResolveForMutation(document, operation)',
        'RegenerateDirtySubset(project, new[] { element.Id })',
    ):
        if token not in source:
            errors.append("PlanTo3D quick-authoring contract missing: " + token)

    quick_start = source.find('[CommandMethod("QS3DCONVERT2D", CommandFlags.Modal)]')
    adv_start = source.find('[CommandMethod("QS3DCONVERT2DADV", CommandFlags.Modal)]')
    convert_start = source.find("private static void ConvertPlanWalls", adv_start + 1)
    if min(quick_start, adv_start, convert_start) < 0 or not (quick_start < adv_start < convert_start):
        errors.append("PlanTo3D quick/advanced command split is missing or ordered unexpectedly")

    preview_at = source.find("DirectDrawProjectPreviewContext.Capture(document)", convert_start)
    defaults_at = source.find("var defaultsProject = projectPreview.DefaultsProject;", preview_at)
    prompts_at = source.find("double? thicknessM = promptStyle", defaults_at)
    refresh_at = source.find("RequireSameSources(sources, refreshedSources)", prompts_at)
    resolve_at = source.find("projectPreview.ResolveForMutation(document, operation)", refresh_at)
    if min(preview_at, defaults_at, prompts_at, refresh_at, resolve_at) < 0 or not (
        preview_at < defaults_at < prompts_at < refresh_at < resolve_at
    ):
        errors.append("quick/advanced paths must share preview defaults, then source revalidation, then guarded mutation resolution")

    for stale in (
        "hasDefaultsProject ? FamilyNumber",
        "hasDefaultsProject ? FamilyFiniteNumber",
        "ProjectContextCoordinator.GetOrCreate(document)",
    ):
        if stale in source:
            errors.append("PlanTo3D quick-authoring preflight found stale/duplicated project-default path: " + stale)

    for token in (
        "QS3DCONVERT2DADV",
        "không mở ba numeric prompt",
        "ThicknessM=0.2 m",
        "HeightM=3.0 m",
        "BottomOffsetM=0 m",
        "RegenerateDirtySubset",
        "quick/no-prompt path",
        "LOCAL-014",
    ):
        if token not in doc:
            errors.append("PlanTo3D docs missing quick-authoring token: " + token)

    for token in (
        "QS3DCONVERT2D",
        "QS3DPLAN2WALLS",
        "QS3DCONVERT2DADV",
        "no Thickness/Height/BottomOffset prompt",
        "deterministic same-ObjectId LINE/open-POLYLINE geometry fingerprints",
        "PENDING_LOCAL",
        "preflight-plan-to-3d-source-geometry-freshness.py",
        "preflight-plan-to-3d-quick-authoring.py",
        "preflight-plan-to-3d-scoped-regeneration.py",
    ):
        if token not in local014:
            errors.append("LOCAL-014 quick-authoring handoff missing: " + token)

    for misplaced in ("QS3DDRAWWINDOW", "QuickWorkflowRibbonAugmenter", "QS3DCUTSELECTEDOPENINGS"):
        if misplaced in local014:
            errors.append("LOCAL-014 must not absorb Direct Draw Window/Ribbon/cut runtime scope: " + misplaced)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Plan-to-3D quick/advanced paths share guarded project preview defaults and mutation resolution; exact runtime/default/cancel/scoped-regeneration proof remains assigned to LOCAL-014 without absorbing Direct Draw Window scope.")
