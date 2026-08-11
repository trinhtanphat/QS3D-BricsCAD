#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    if not TARGET.exists():
        return fail("ProjectContextCoordinator.cs is missing")

    text = TARGET.read_text(encoding="utf-8")
    required = [
        "Dictionary<Document, string> UnsavedProjectPaths",
        "UnsavedProjectPaths.TryGetValue(document, out var existingPath)",
        "UnsavedProjectPaths[document] = path;",
        "CleanupObsoleteUnsavedProject(document, path);",
        "if (SameDrawingName(obsoletePath, currentPath)) return;",
        "if (File.Exists(obsoletePath)) File.Delete(obsoletePath);",
        "if (File.Exists(obsoletePath + \".bak\")) File.Delete(obsoletePath + \".bak\");",
        "UnsavedProjectPaths.Remove(document);",
        "UnsavedProjectKeys.Remove(document);",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        return fail("SAVEAS recovery cleanup invariant is incomplete: " + ", ".join(missing))

    save_start = text.find("public static string Save(Document document)")
    save_end = text.find("public static ProjectState Reload(Document document)", save_start)
    if save_start < 0 or save_end < 0:
        return fail("Save method boundaries were not found")

    save = text[save_start:save_end]
    store = save.find("Store.Save(project, path);")
    marked = save.find("MarkSaved(project);")
    cleanup = save.find("CleanupObsoleteUnsavedProject(document, path);")
    if min(store, marked, cleanup) < 0 or not store < marked < cleanup:
        return fail("named sidecar must commit and be marked saved before obsolete recovery cleanup")

    cleanup_start = text.find("private static void CleanupObsoleteUnsavedProject")
    cleanup_end = text.find("private static bool TryGetExistingProjectPath", cleanup_start)
    if cleanup_start < 0 or cleanup_end < 0:
        return fail("obsolete recovery cleanup helper boundaries were not found")

    cleanup_body = text[cleanup_start:cleanup_end]
    try_guard = cleanup_body.find("try\n            {")
    same_path_guard = cleanup_body.find("if (SameDrawingName(obsoletePath, currentPath)) return;")
    primary_delete = cleanup_body.find("File.Delete(obsoletePath);")
    backup_delete = cleanup_body.find("File.Delete(obsoletePath + \".bak\");")
    state_remove = cleanup_body.rfind("UnsavedProjectPaths.Remove(document);")
    if min(try_guard, same_path_guard, primary_delete, backup_delete, state_remove) < 0:
        return fail("cleanup helper is missing a required SAVEAS promotion guard")
    if not try_guard < same_path_guard < primary_delete < state_remove or not try_guard < same_path_guard < backup_delete < state_remove:
        return fail("path comparison and cleanup must remain inside the post-commit best-effort region")
    if "catch (Exception)" not in cleanup_body:
        return fail("post-commit recovery cleanup is not best-effort")

    print("PASS: SAVEAS promotes the named sidecar before best-effort cleanup of the exact unsaved .qsdb/.bak pair.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
