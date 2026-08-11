#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing Commands.cs")
    text = ""
else:
    text = COMMAND.read_text(encoding="utf-8")


def region(start_token, end_token=None):
    start = text.find(start_token)
    if start < 0:
        errors.append("missing command region: " + start_token)
        return ""
    end = text.find(end_token, start + len(start_token)) if end_token else len(text)
    if end_token and end < 0:
        errors.append("missing command region end: " + end_token)
        return text[start:]
    return text[start:end]


def require(scope, token, label):
    if token not in scope:
        errors.append(label + " missing token: " + token)


def forbid(scope, token, label):
    if token in scope:
        errors.append(label + " forbidden token: " + token)


bq = region('CommandMethod("QS3DBQ"', 'CommandMethod("QS3DED2"')
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "ProjectQuantityReportBuilder.Group(previewProject)",
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var currentProject)",
):
    require(bq, token, "QS3DBQ")
forbid(bq, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DBQ")
forbid(bq, "RegenerateProject(project)", "QS3DBQ")

ed2 = region('CommandMethod("QS3DED2"', 'CommandMethod("QS3DBBS"')
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RegenerateProject(exportProject)",
    "ProjectQuantityReportBuilder.Detail(exportProject",
    "ProjectQuantityReportBuilder.Group(exportProject",
):
    require(ed2, token, "QS3DED2")
forbid(ed2, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DED2")
forbid(ed2, "RegenerateProject(project)", "QS3DED2")

bbs = region('CommandMethod("QS3DBBS"', 'CommandMethod("QS3DREGEN"')
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RegenerateProject(exportProject)",
    "ProjectRebarScheduleBuilder.Build(exportProject)",
):
    require(bbs, token, "QS3DBBS")
forbid(bbs, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DBBS")
forbid(bbs, "RegenerateProject(project)", "QS3DBBS")

regen = region('CommandMethod("QS3DREGEN"', 'CommandMethod("QS3DSAVE"')
require(regen, 'ExistingProjectMutationContext.Require(doc, "Regenerate project")', "QS3DREGEN")
forbid(regen, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DREGEN")

refresh = region('CommandMethod("QS3DREFRESH"', 'CommandMethod("QS3DTAKEOFF"')
require(refresh, "ExistingProjectMutationContext.TryGet(doc, out var project)", "QS3DREFRESH")
forbid(refresh, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DREFRESH")

link = region('CommandMethod("QS3DLINKHOST"', 'CommandMethod("QS3DFINISH"')
for token in (
    "Cad.EntitySnapshotReader.ReadCurrentSelection(doc)",
    "if (selectedHandles.Count == 0)",
    'ExistingProjectMutationContext.Require(doc, "Manual Host Link")',
    "ProjectStateSnapshot.Capture(project)",
    "project.FindElement(openingId)",
):
    require(link, token, "QS3DLINKHOST")
forbid(link, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DLINKHOST")
selection_pos = link.find("Cad.EntitySnapshotReader.ReadCurrentSelection(doc)")
bind_pos = link.find('ExistingProjectMutationContext.Require(doc, "Manual Host Link")')
if selection_pos >= 0 and bind_pos >= 0 and selection_pos >= bind_pos:
    errors.append("QS3DLINKHOST must remain selection-first before canonical project binding")

health = region('CommandMethod("QS3DHEALTH"', 'CommandMethod("QS3DLOCATE"')
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var currentProject)",
    "currentProject.FindElement(issue.ElementId)",
    "SourceHandleResolver.Resolve(currentProject",
):
    require(health, token, "QS3DHEALTH")
forbid(health, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DHEALTH")

locate = region('CommandMethod("QS3DLOCATE"', 'CommandMethod("QS3DEXCELLOCATE"')
require(locate, "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)", "QS3DLOCATE")
forbid(locate, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DLOCATE")

excel = region('CommandMethod("QS3DEXCELLOCATE"', "private static IReadOnlyList<string> ResolveEd2Selection")
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "if (!hasProject)",
    "lookup.UsesLegacyDecimalHandles",
):
    require(excel, token, "QS3DEXCELLOCATE")
forbid(excel, "ProjectContextCoordinator.GetOrCreate(doc)", "QS3DEXCELLOCATE")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Commands.cs review/export/locate paths are non-creating, semantic mutations use canonical existing state, and modeless callbacks re-resolve current project state.")
