#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs"
errors = []

if not path.is_file():
    errors.append("missing SlabMeshCommands.cs")
    text = ""
else:
    text = path.read_text(encoding="utf-8")

required = (
    'CommandMethod("QS3DSLABREBAR3D"',
    "CadSelectionGuard.AcquireCurrentSelection(document)",
    "expectedProjectId = previewProject.ProjectId",
    "expectedChangeVersion = previewProject.ChangeVersion",
    "expectedTargetIds.SetEquals",
    "ExistingProjectMutationContext.Require(document, \"Slab Mesh 3D\")",
    "SlabMeshSolidBuilder.BuildSelected(document, project)",
    'Report(document, "QS3DSLABREBAR3D không thể hoàn tất. Kiểm tra selection/project và thử lại.")',
    'CommandMethod("QS3DSLABREBARHEALTH"',
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "GeneratedSlabMeshHealthService().Inspect(project, live)",
    'Report(document, "QS3DSLABREBARHEALTH không thể hoàn tất kiểm tra. Project/native geometry không bị thay đổi.")',
    "Report(document, message);",
    "TryWriteMessage(document, \"\\n  [\" + issue.Severity",
    "var uiSyncFailed = false;",
    "try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }",
    "try { document.Editor.Regen(); } catch { uiSyncFailed = true; }",
    "try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }",
    "native update đã hoàn tất; một phần UI không thể đồng bộ.",
)
for token in required:
    if token not in text:
        errors.append("missing slab-mesh redaction/lifecycle token: " + token)

for forbidden in ("ex.Message", "Exception.Message", "GetBaseException()", "StackTrace", "UI sync warning:"):
    if forbidden in text:
        errors.append("raw host exception detail remains: " + forbidden)

build_at = text.find("SlabMeshSolidBuilder.BuildSelected(document, project)")
finalize_at = text.find("FinalizeUi(document, message)", build_at)
if build_at < 0 or finalize_at < 0 or build_at >= finalize_at:
    errors.append("successful native slab-mesh build must precede best-effort post-commit UI finalization")

preview_at = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
mutation_at = text.find('ExistingProjectMutationContext.Require(document, "Slab Mesh 3D")')
if preview_at < 0 or mutation_at < 0 or preview_at >= mutation_at:
    errors.append("selection/read-only preview must remain ahead of the existing-project mutation context")

health_at = text.find("public void SlabMeshHealth()")
health_end = text.find("private static List<ProjectElement>", health_at)
health = text[health_at:health_end] if health_at >= 0 and health_end > health_at else ""
if "ExistingProjectMutationContext.Require" in health or "SlabMeshSolidBuilder.BuildSelected" in health:
    errors.append("Slab Mesh Health must remain read-only")
if 'PaletteCoordinator.SetStatus(message);\n                document.Editor.WriteMessage' in health:
    errors.append("health output must route through fail-isolated report/write helpers")

if errors:
    print("Slab Mesh command failure-redaction guard FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Slab Mesh command failure-redaction guard PASS")
