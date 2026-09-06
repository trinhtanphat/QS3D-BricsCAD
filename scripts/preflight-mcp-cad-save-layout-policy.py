#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
NATIVE_SAVE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpBackgroundHostRuntime.cs"
RUNBOOK = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")


direct = read(DIRECT)
native_save = read(NATIVE_SAVE)
host = read(HOST)

if direct:
    save_start = direct.find("private static string Save()")
    save_end = direct.find("private static string SaveAs", save_start)
    save = direct[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
    if not save:
        errors.append("cannot isolate McpCadDirectModelRuntime.Save")
    else:
        require(save, "McpNativeCurrentDocumentSave.SaveCurrentDocument(", "cad_save")
        require(save, "dbmodAfterSave", "cad_save diagnostics")
        require(save, 'route\\\":\\\"native-QSAVE-current-document', "cad_save")
        if "document.Database.Save();" in save or "document.Database.SaveAs(" in save:
            errors.append("cad_save must not write the already-open active drawing through Database.Save/SaveAs")
        if save.count("McpNativeCurrentDocumentSave.SaveCurrentDocument(") != 1:
            errors.append("cad_save must invoke exactly one host-owned current-document save lifecycle")

    synchronous = (
        "Application.DocumentManager.ExecuteInCommandContextAsync(" in native_save
        and 'document.Editor.Command("_.QSAVE");' in native_save
    )
    if synchronous:
        for token in (
            "McpDiagnosticHub.InvokeInCadContext(() =>",
            "document.Database.SaveAs(fullPath, DwgVersion.Current);",
            "McpNativeCurrentDocumentSave.SaveCurrentDocument(",
            "route=Database.SaveAs+native-QSAVE",
            "dbmodAfterSave",
        ):
            require(direct, token, "synchronous SaveAs completion")
        save_as_start = direct.find("private static string SaveAs")
        save_as_end = direct.find("private static bool TryParseDirectLayoutCommand", save_as_start)
        save_as = direct[save_as_start:save_as_end] if save_as_start >= 0 and save_as_end > save_as_start else ""
        if "WaitForSavedContentDbmod();" in save_as:
            errors.append("synchronous SaveAs must not treat a blind DBMOD poll as terminal completion")
    else:
        for token in (
            "private const int DbmodPersistentContentMask = 1 | 4 | 32;",
            "private static int WaitForSavedContentDbmod()",
            "(dbmod & DbmodPersistentContentMask) == 0",
            "window/view DBMOD bits may remain after save",
        ):
            require(direct, token, "legacy content-aware SaveAs DBMOD confirmation")
        if "dbmod == 0" in direct:
            errors.append("save completion must not require the entire DBMOD bitmask to become zero")

    for token in (
        'string.Equals(command, "-LAYOUT", StringComparison.Ordinal)',
        'string.Equals(command, "LAYOUT", StringComparison.Ordinal)',
        "TryParseDirectLayoutCommand",
        "LayoutManager.Current.CreateLayout",
        "LayoutManager.Current.CurrentLayout",
        "LayoutManager.Current.DeleteLayout",
        'route=LayoutManager-direct',
    ):
        require(direct, token, "direct layout command route")
    layout_start = direct.find("private static string ExecuteDirectLayoutCommand")
    layout_end = direct.find("private static bool LayoutExists", layout_start)
    layout = direct[layout_start:layout_end] if layout_start >= 0 and layout_end > layout_start else ""
    if layout and "SendStringToExecute" in layout:
        errors.append("direct layout completion route must not use asynchronous SendStringToExecute")

if native_save:
    synchronous = (
        "Application.DocumentManager.ExecuteInCommandContextAsync(" in native_save
        and 'document.Editor.Command("_.QSAVE");' in native_save
    )
    if synchronous:
        for token in (
            "Task.WaitAny(", "WaitForCleanDbmod", "document.IsReadOnly",
            'Application.GetSystemVariable("CMDACTIVE")',
            "DbmodPersistentContentMask = 1 | 4 | 32",
            'Application.GetSystemVariable("DBMOD")',
            "(dbmod & DbmodPersistentContentMask) == 0",
            "Do not retry automatically",
        ):
            require(native_save, token, "synchronous native current-document QSAVE lifecycle")
        for forbidden in ("document.SendStringToExecute(", "ManualResetEventSlim", "CommandEnded +=", "CommandCancelled +=", "CommandFailed +="):
            if forbidden in native_save:
                errors.append("synchronous native QSAVE retains obsolete event topology: " + forbidden)
        if native_save.count('document.Editor.Command("_.QSAVE");') != 1:
            errors.append("synchronous native current-document QSAVE must execute exactly one native attempt")
    else:
        for token in (
            "McpCadMutationCoordinator.QueueNativeCommand", "document.SendStringToExecute(", "_.QSAVE",
            "ManualResetEventSlim", "CommandEnded", "CommandCancelled", "CommandFailed",
            "document.IsReadOnly", 'Application.GetSystemVariable("CMDACTIVE")',
            "DbmodPersistentContentMask = 1 | 4 | 32", 'Application.GetSystemVariable("DBMOD")',
            "(dbmod & DbmodPersistentContentMask) == 0", "Do not retry automatically",
        ):
            require(native_save, token, "legacy native current-document QSAVE lifecycle")
        if native_save.count("document.SendStringToExecute(") != 1:
            errors.append("legacy native current-document QSAVE must queue exactly one native attempt")
    if "Database.Save();" in native_save or "Database.SaveAs(" in native_save:
        errors.append("native current-document QSAVE helper must not write the active path through Database.Save/SaveAs")

if host:
    require(host, '\\"processStartDefault\\":\\"background_only\\"', "foreground policy status")
    require(host, '\\"requiresLocalReenableAfterRestart\\":true', "foreground policy status")
    require(host, '\\"consentState\\":\\"', "foreground policy status")
    require(host, '\\"policyMeaning\\":\\"local-consent-and-policy-must-both-be-enabled\\"', "foreground policy status")
    if "_interactionPolicy = ForegroundFallback" in host:
        errors.append("foreground policy must not become the process-start default")

if not RUNBOOK.is_file():
    errors.append("missing MCP-CANONICAL-RUNBOOK.md")
else:
    require(RUNBOOK.read_text(encoding="utf-8"), "background_only", "canonical safety runbook")

print("QS3D MCP current-save/layout/foreground-policy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: cad_save uses one host-owned native QSAVE lifecycle, SaveAs follows the admitted completion topology, layout operations remain direct/deterministic, and foreground policy stays background_only.")