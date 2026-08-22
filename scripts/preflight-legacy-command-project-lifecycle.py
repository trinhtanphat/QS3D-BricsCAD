#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
errors = []

if not SOURCE.is_file():
    print("ERROR: missing Commands.cs")
    sys.exit(1)

text = SOURCE.read_text(encoding="utf-8")


def section(command, next_command=None):
    start = text.find('[CommandMethod("' + command + '"')
    if start < 0:
        errors.append("missing command: " + command)
        return ""
    if next_command is None:
        return text[start:]
    end = text.find('[CommandMethod("' + next_command + '"', start + 1)
    if end < 0:
        errors.append("missing next command boundary: " + next_command)
        return text[start:]
    return text[start:end]

checks = [
    ("QS3DBQ", "QS3DED2", (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var currentProject)",
        "ProjectStateSnapshot.CreateDetachedCopy(currentProject)",
        "RegenerateProject(previewProject)",
        "SourceHandleResolver.Resolve(currentProject, row.ElementIds)",
    )),
    ("QS3DED2", "QS3DBBS", (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "ProjectQuantityReportBuilder.Detail(previewProject",
        "ProjectQuantityReportBuilder.Group(previewProject",
        "EnsureEd2HandlesAreLive(doc, details);",
        "if (dialog.ShowDialog() != true) return;",
        "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);",
    )),
    ("QS3DBBS", "QS3DREGEN", (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "ProjectRebarScheduleBuilder.Build(previewProject)",
    )),
    ("QS3DLINKHOST", "QS3DFINISH", (
        'ExistingProjectMutationContext.Require(doc, "Link opening host")',
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "var currentOpening = project.FindElement(opening.Id)",
        'currentOpening.Properties.TryGetValue("HostWallId"',
    )),
    ("QS3DHEALTH", "QS3DLOCATE", (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var currentProject)",
        "currentProject.FindElement(issue.ElementId)",
        "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
    )),
    ("QS3DLOCATE", "QS3DEXCELLOCATE", (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    )),
    ("QS3DEXCELLOCATE", None, (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "Excel Locate cần một QS3D project hiện hữu",
    )),
]

for command, next_command, required in checks:
    body = section(command, next_command)
    if not body:
        continue
    for token in required:
        if token not in body:
            errors.append(command + " missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(doc)" in body:
        errors.append(command + " must not create/cache project state on this lifecycle path.")

ed2 = section("QS3DED2", "QS3DBBS")
if ed2:
    project_pos = ed2.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
    snapshot_pos = ed2.find("ProjectStateSnapshot.CreateDetachedCopy(project)", project_pos + 1)
    details_pos = ed2.find("ProjectQuantityReportBuilder.Detail(previewProject", snapshot_pos + 1)
    summary_pos = ed2.find("ProjectQuantityReportBuilder.Group(previewProject", details_pos + 1)
    live_pos = ed2.find("EnsureEd2HandlesAreLive(doc, details);", summary_pos + 1)
    dialog_pos = ed2.find("var dialog = new SaveFileDialog", live_pos + 1)
    confirm_pos = ed2.find("if (dialog.ShowDialog() != true) return;", dialog_pos + 1)
    export_pos = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);", confirm_pos + 1)
    if min(project_pos, snapshot_pos, details_pos, summary_pos, live_pos, dialog_pos, confirm_pos, export_pos) < 0:
        errors.append("QS3DED2 missing existing-project/detached-report/live-handle/save/export lifecycle stage.")
    elif not project_pos < snapshot_pos < details_pos < summary_pos < live_pos < dialog_pos < confirm_pos < export_pos:
        errors.append("QS3DED2 must validate existing detached report/live-handle exportability before SaveFileDialog, then write only after confirmation.")
    if confirm_pos >= 0 and "XlsxQuantityExporter.ExportEd2(" in ed2[:confirm_pos]:
        errors.append("QS3DED2 must not write XLSX before Save confirmation.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: legacy BQ/ED2/BBS/Health/Locate paths are non-creating; ED2 validates detached exportability before Save and writes only after confirmation; Link Host keeps canonical existing-state re-resolution.")
