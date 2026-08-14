#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawWindowCommands.cs"
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "QuickWorkflowRibbonAugmenter.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
PLAN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
SCHEDULE = ROOT / "src" / "QS3D.Core" / "Reporting" / "DoorOpeningSchedule.cs"
DOC = ROOT / "docs" / "PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing quick-workflow file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


window = read(WINDOW)
ribbon = read(RIBBON)
coordinator = read(COORDINATOR)
plugin = read(PLUGIN)
plan = read(PLAN)
schedule = read(SCHEDULE)
doc = read(DOC)
inbox = read(INBOX)


def local_section(local_id):
    heading = "## " + local_id + " —"
    start = inbox.find(heading)
    if start < 0:
        return ""
    end = inbox.find("\n## LOCAL-", start + len(heading))
    return inbox[start:] if end < 0 else inbox[start:end]

for token, label in (
    ('CommandMethod("QS3DDRAWWINDOW"', "Window command"),
    ('ElementCategory.WallOpening', "canonical WallOpening category"),
    ('SetProperty("OpeningUsage", "Window")', "Window semantic usage"),
    ('SetProperty("SillHeightM"', "window sill property"),
    ('AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)', "window exact-project Auto Host"),
    ('createdElementId', "stable semantic id across Auto Host"),
    ('var promptUnit = CadUnitService.GetLengthUnit(document);', "unit captured before prompts"),
    ('var projectPreview = DirectDrawProjectPreviewContext.Capture(document);', "project identity captured before prompts"),
    ('BindProjectAfterPrompts(document, projectPreview, expectedProjectChangeVersion, operation)', "post-prompt project freshness bind"),
    ('RequireExactProject(document, project', "exact canonical project execution"),
    ('RegenerateDirtySubset(project, new[] { createdElement.Id })', "opening-only first regeneration"),
    ('RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })', "opening-and-host second regeneration"),
    ('rollback.Restore(project)', "window semantic rollback"),
    ('EraseSource(document, sourceId)', "window source rollback"),
    ('QS3DCUTSELECTEDOPENINGS', "explicit targeted-cut handoff"),
):
    if token not in window:
        errors.append(label + " missing token: " + token)

if 'ElementCategory.Window' in window:
    errors.append("window authoring must reuse WallOpening instead of introducing an adapter-local Window category")
if 'new AutoHostLinkCommands().AutoLinkHosts()' in window:
    errors.append("window authoring must not invoke the public Auto Host command wrapper")
if '.RegenerateDirty(project)' in window:
    errors.append("window authoring must not regenerate unrelated dirty project elements")

for token, label in (
    ('private const string AuthorTabId = "QS3D_AUTHOR";', "author tab binding"),
    ('"2D → Tường 3D", "QS3DCONVERT2D"', "2D-to-3D ribbon entry"),
    ('"Vẽ Cửa Sổ", "QS3DDRAWWINDOW"', "window ribbon entry"),
    ('"Vật liệu", "QS3DMATERIALS"', "material ribbon entry"),
    ('var button = FindById(items as IEnumerable, spec.Id) ?? FindByText(items, spec.Text);', "idempotent ribbon reconciliation"),
):
    if token not in ribbon:
        errors.append(label + " missing token: " + token)

bootstrap = coordinator.find("RibbonBootstrapper.TryInitialize()")
project_ribbon = coordinator.find("ProjectRibbonAugmenter.TryInitialize()")
quick = coordinator.find("QuickWorkflowRibbonAugmenter.TryInitialize()")
quantity = coordinator.find("QuantityReferenceRibbonAugmenter.TryInitialize()")
if min(bootstrap, project_ribbon, quick, quantity) < 0 or not bootstrap < project_ribbon < quick < quantity:
    errors.append("QuickWorkflowRibbonAugmenter must initialize through RibbonInitializationCoordinator after base/project and before quantity augmentation")
for token in ("RibbonInitializationCoordinator.Start();", "QuickWorkflowRibbonAugmenter.Reset();"):
    if token not in plugin:
        errors.append("PluginEntry missing coordinated quick-workflow lifecycle token: " + token)

for token, label in (
    ('CommandMethod("QS3DCONVERT2D"', "2D conversion command"),
    ('CommandMethod("QS3DPLAN2WALLS"', "2D conversion alias"),
    ('GeneratedGeometryService.FindMatchingOwnedHandles', "owned rollback discovery"),
    ('rollback.Restore(project)', "plan conversion rollback"),
):
    if token not in plan:
        errors.append(label + " missing token: " + token)

for token, label in (
    ('ScheduleCategory(element)', "schedule category normalization"),
    ('Properties.TryGetValue("OpeningUsage"', "schedule OpeningUsage read"),
    ('? "Window"', "schedule Window group"),
):
    if token not in schedule:
        errors.append(label + " missing token: " + token)

for token in ('QS3DCONVERT2D', 'QS3DDRAWWINDOW', 'OpeningUsage=Window', 'QS3DMATERIALS', 'Preview-to-commit freshness', 'LOCAL-008', 'LOCAL-014'):
    if token not in doc:
        errors.append("workflow doc missing token: " + token)

boundary_start = doc.find("Exact BricsCAD V25 proof")
boundary_end = doc.find("\n## Bước 3", boundary_start + 1)
boundary = doc[boundary_start:boundary_end] if boundary_start >= 0 and boundary_end > boundary_start else ""
for token in ('QS3DCONVERT2D', 'QS3DPLAN2WALLS', 'QS3DCONVERT2DADV', 'LOCAL-014'):
    if token not in boundary:
        errors.append("workflow runtime boundary must assign Plan-to-3D token to LOCAL-014: " + token)
for token in ('QS3DDRAWWINDOW', 'Ribbon', 'Auto Host', 'QS3DCUTSELECTEDOPENINGS', 'LOCAL-008'):
    if token not in boundary:
        errors.append("workflow runtime boundary must assign Window/finish token to LOCAL-008: " + token)
for token in ('PENDING_LOCAL', 'source review không được coi là `LOCAL_PASS`'):
    if token not in boundary:
        errors.append("workflow runtime boundary must remain static-only/PENDING_LOCAL: " + token)

local_008 = local_section("LOCAL-008")
local_014 = local_section("LOCAL-014")
for token in ('Status: OPEN', 'Evidence: PENDING_LOCAL', 'QS3DDRAWWINDOW', 'OpeningUsage=Window', 'Auto Host', 'QS3DCUTSELECTEDOPENINGS', 'Ribbon'):
    if token not in local_008:
        errors.append("LOCAL-008 must own Window/finish runtime qualification: " + token)
for token in ('Status: OPEN', 'Evidence: PENDING_LOCAL', 'QS3DCONVERT2D', 'QS3DPLAN2WALLS', 'QS3DCONVERT2DADV', 'PENDING_LOCAL / DO_NOT_RETRY_REMOTE'):
    if token not in local_014:
        errors.append("LOCAL-014 must own Plan-to-3D runtime qualification: " + token)
if 'Status: PASS' in local_008 or 'Status: PASS' in local_014:
    errors.append("LOCAL-008/LOCAL-014 must not be promoted by a static workflow preflight")

if errors:
    print("QS3D 2D-plan -> 3D finish workflow preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: quick 2D->3D workflow is discoverable in the grouped Author Ribbon, Window authoring reuses guarded WallOpening+AutoHost semantics with rollback, Ribbon initialization follows the bounded coordinator, schedules preserve a Window usage group, and local-runtime qualification remains pending until exact evidence exists.")
