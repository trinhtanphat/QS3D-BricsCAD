#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MUTATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "ExistingProjectMutationContext.cs"
PROJECT_UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectFileUiService.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"

errors = []


def require(text, token, message):
    if token not in text:
        errors.append(message)


def forbid(text, token, message):
    if token in text:
        errors.append(message)


def method_body(source, start_token, end_token, name):
    start = source.find(start_token)
    end = source.find(end_token, start + 1)
    if start < 0 or end <= start:
        errors.append("cannot isolate " + name)
        return ""
    return source[start:end]


mutation = MUTATION.read_text(encoding="utf-8")
project_ui = PROJECT_UI.read_text(encoding="utf-8")
coordinator = COORDINATOR.read_text(encoding="utf-8")

create_ui = method_body(project_ui, "public static void CreateNewDrawing()", "public static void OpenProjectFromPicker()", "ProjectFileUiService.CreateNewDrawing")
if create_ui:
    require(create_ui, 'ProjectContextCoordinator.Forget(document);', "Create New must clear any stale project state for the newly-created document")
    require(create_ui, '_ = ProjectContextCoordinator.GetOrCreate(document);', "Create New must explicitly seed the canonical in-memory project before first save")

forbid(mutation, 'string.Equals(operation, "Lưu dự án", StringComparison.Ordinal)', "generic mutation guard must not infer project creation from the localized Home Save label")
forbid(mutation, 'string.Equals(operation, "Lưu thành", StringComparison.Ordinal)', "generic mutation guard must not infer project creation from the localized Home Save As label")
forbid(mutation, 'IsMouseFirstUnsavedProjectSave', "generic mutation guard must not contain a hidden first-save bootstrap path")
require(mutation, 'string.Equals(operation, "Save Project", StringComparison.Ordinal)', "coordinator save must have an explicit cached-project path-transition boundary")
require(mutation, 'ProjectContextCoordinator.TryGetCached(document, out var cached)', "Save path transition must only reuse the canonical cached project")
require(mutation, '_ = ProjectContextCoordinator.HasPendingChanges(document);', "Save path transition must verify freshness and destination collision before reuse")
require(mutation, 'if (!TryGet(document, out var project))', "cold-cache mutation must still require an already-existing project")
require(mutation, 'thao tác này không tạo project mới.', "cold-cache mutation must retain the explicit no-bootstrap failure contract")

save_ui = method_body(project_ui, "public static void SaveCurrentProject()", "public static void SaveCurrentProjectAs()", "ProjectFileUiService.SaveCurrentProject")
if save_ui:
    require(save_ui, 'ExistingProjectMutationContext.Require(document, "Lưu dự án")', "Home Save must require the already-seeded/existing canonical project before DWG save")
    require(save_ui, 'InvokeAcadDocumentMethod(document, "Save")', "Home Save must persist the DWG before the QS3D sidecar")
    require(save_ui, 'ProjectContextCoordinator.Save(document)', "Home Save must commit through the canonical coordinator")

save_as_ui = method_body(project_ui, "public static void SaveCurrentProjectAs()", "internal static void OpenProject", "ProjectFileUiService.SaveCurrentProjectAs")
if save_as_ui:
    require(save_as_ui, 'ExistingProjectMutationContext.Require(document, "Lưu thành")', "Home Save As must require the already-seeded/existing canonical project before path transition")
    require(save_as_ui, 'InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing)', "Home Save As must move the active DWG before sidecar commit")
    require(save_as_ui, 'ProjectContextCoordinator.Save(document)', "Home Save As must commit the canonical sidecar after the DWG path transition")
    require(save_as_ui, 'File.Exists(targetProjectPath) || File.Exists(targetProjectPath + ".bak")', "Home Save As must reject an occupied destination sidecar")

coordinator_save = method_body(coordinator, "public static string Save(Document document)", "public static ProjectState Reload(Document document)", "ProjectContextCoordinator.Save")
if coordinator_save:
    require(coordinator_save, 'ExistingProjectMutationContext.Require(document, "Save Project")', "ProjectContextCoordinator.Save must still require an existing/canonical project and never bootstrap directly")
    forbid(coordinator_save, "GetOrCreate(document)", "ProjectContextCoordinator.Save must not bootstrap a replacement project")
    require(coordinator_save, 'EnsureBackingStoreUnchanged(document, project, true, "QS3D save")', "Save must re-check path-transition freshness under the project lock")
    require(coordinator_save, 'Store.SaveNew(project, path)', "Save must use create-only persistence on a verified DWG path transition")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Create New explicitly seeds the canonical project; generic/cold-cache mutation remains non-creating; cached Save/Save As path transitions stay freshness-checked, collision-safe, and coordinator-owned.")
