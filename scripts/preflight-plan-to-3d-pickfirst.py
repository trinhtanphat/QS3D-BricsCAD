#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

commands = (
    "QS3DCONVERT2D",
    "QS3DPLAN2WALLS",
    "QS3DCONVERT2DADV",
)
for command in commands:
    expected = f'[CommandMethod("{command}", CommandFlags.Modal | CommandFlags.UsePickSet)]'
    if expected not in text:
        raise SystemExit(f"{command} must preserve PICKFIRST via CommandFlags.UsePickSet")

required = (
    "private static IReadOnlyList<ObjectId>? AcquireSelection(Document document)",
    "var implied = document.Editor.SelectImplied();",
    "if (ids.Length > 0) return ids.Distinct().ToList().AsReadOnly();",
    "var selection = document.Editor.GetSelection();",
    "var selectedIds = AcquireSelection(document);",
    "var sources = PreflightSources(document, selectedIds);",
    "var projectPreview = DirectDrawProjectPreviewContext.Capture(document);",
    "var refreshedSources = PreflightSources(document, selectedIds);",
    "var project = projectPreview.ResolveForMutation(document, operation);",
    "var commitSources = PreflightSources(document, selectedIds);",
    "RequireSameSources(sources, commitSources);",
    "RequireFreshSources(project, sources);",
    "var rollback = ProjectStateSnapshot.Capture(project);",
    ".RegenerateDirtySubset(project, new[] { element.Id });",
)
missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("Plan-to-3D PICKFIRST/freshness contract missing: " + " | ".join(missing))

acquire = text.index("var selectedIds = AcquireSelection(document);")
preview = text.index("var projectPreview = DirectDrawProjectPreviewContext.Capture(document);")
if acquire >= preview:
    raise SystemExit("selection must remain before project preview/mutation")

method = text.index("private static IReadOnlyList<ObjectId>? AcquireSelection(Document document)")
implied = text.index("var implied = document.Editor.SelectImplied();", method)
fallback = text.index("var selection = document.Editor.GetSelection();", method)
if implied >= fallback:
    raise SystemExit("PICKFIRST must remain before explicit GetSelection fallback")

resolve = text.index("var project = projectPreview.ResolveForMutation(document, operation);")
commit_preflight = text.index("var commitSources = PreflightSources(document, selectedIds);", resolve)
commit_compare = text.index("RequireSameSources(sources, commitSources);", commit_preflight)
fresh = text.index("RequireFreshSources(project, sources);", commit_compare)
snapshot = text.index("var rollback = ProjectStateSnapshot.Capture(project);", fresh)
if not (resolve < commit_preflight < commit_compare < fresh < snapshot):
    raise SystemExit("PICKFIRST change must not weaken post-resolve source freshness ordering")

for forbidden in ("RegenerateDirty(project)", "GetOrCreate(document)"):
    if forbidden in text:
        raise SystemExit("forbidden broad/creating path introduced: " + forbidden)

print("PASS: Plan-to-3D commands expose PICKFIRST while preserving explicit fallback, commit-time source freshness, scoped regeneration and rollback boundaries.")
