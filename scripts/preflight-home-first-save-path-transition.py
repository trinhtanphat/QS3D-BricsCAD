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

require(mutation, 'string.Equals(operation, "Lưu dự án", StringComparison.Ordinal)', "Home Save must explicitly own the unnamed-DWG first-save bootstrap")
require(mutation, 'string.Equals(operation, "Lưu thành", StringComparison.Ordinal)', "Home Save As must explicitly own the unnamed-DWG first-save bootstrap")
require(mutation, '!Path.IsPathRooted(drawing)', "first-save bootstrap must be limited to an unnamed/non-rooted DWG")
require(mutation, 'return ProjectContextCoordinator.GetOrCreate(document);', "intentional Home first-save must seed the canonical in-memory project")
require(mutation, 'string.Equals(operation, "Save Project", StringComparison.Ordinal)', "coordinator save must have an explicit cached-project path-transition boundary")
require(mutation, 'ProjectContextCoordinator.TryGetCached(document, out var cached)', "Save path transition must only reuse the canonical cached project")
require(mutation, '_ = ProjectContextCoordinator.HasPendingChanges(document);', "Save path transition must verify freshness and destination collision before reuse")

save_ui = method_body(project_ui, "public static void SaveCurrentProject()", "public static void SaveCurrentProjectAs()", "ProjectFileUiService.SaveCurrentProject")
if save_ui:
    require(save_ui, 'ExistingProjectMutationContext.Require(document, "Lưu dự án")', "Home Save must use the explicit first-save mutation boundary")
    require(save_ui, 'InvokeAcadDocumentMethod(document, "Save")', "Home Save must persist the DWG before the QS3D sidecar")
    require(save_ui, 'ProjectContextCoordinator.Save(document)', "Home Save must commit through the canonical coordinator")

save_as_ui = method_body(project_ui, "public static void SaveCurrentProjectAs()", "internal static void OpenProject", "ProjectFileUiService.SaveCurrentProjectAs")
if save_as_ui:
    require(save_as_ui, 'ExistingProjectMutationContext.Require(document, "Lưu thành")', "Home Save As must use the explicit first-save mutation boundary")
    require(save_as_ui, 'InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing)', "Home Save As must move the active DWG before sidecar commit")
    require(save_as_ui, 'ProjectContextCoordinator.Save(document)', "Home Save As must commit the canonical sidecar after the DWG path transition")
    require(save_as_ui, 'File.Exists(targetProjectPath) || File.Exists(targetProjectPath + ".bak")', "Home Save As must reject an occupied destination sidecar")

coordinator_save = method_body(coordinator, "public static string Save(Document document)", "public static ProjectState Reload(Document document)", "ProjectContextCoordinator.Save")
if coordinator_save:
    require(coordinator_save, 'ExistingProjectMutationContext.Require(document, "Save Project")', "ProjectContextCoordinator.Save must still require an existing/canonical project and never bootstrap directly")
    if "GetOrCreate(document)" in coordinator_save:
        errors.append("ProjectContextCoordinator.Save must not bootstrap a replacement project")
    require(coordinator_save, 'EnsureBackingStoreUnchanged(document, project, true, "QS3D save")', "Save must re-check path-transition freshness under the project lock")
    require(coordinator_save, 'Store.SaveNew(project, path)', "Save must use create-only persistence on a verified DWG path transition")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Home first-save bootstraps only an unnamed DWG through the explicit mouse-first boundary; cached Save/Save As path transitions remain freshness-checked, collision-safe, and coordinator-owned without allowing cold-cache replacement creation.")
