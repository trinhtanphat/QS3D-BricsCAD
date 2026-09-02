#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpBackgroundHostRuntime.cs"
RUNBOOK = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"

errors = []

def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")

if not DIRECT.is_file():
    errors.append("missing McpCadDirectModelRuntime.cs")
    direct = ""
else:
    direct = DIRECT.read_text(encoding="utf-8")

if not HOST.is_file():
    errors.append("missing McpBackgroundHostRuntime.cs")
    host = ""
else:
    host = HOST.read_text(encoding="utf-8")

if direct:
    save_start = direct.find("private static string Save()")
    save_end = direct.find("private static string", save_start + 1) if save_start >= 0 else -1
    save = direct[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
    if not save:
        errors.append("cannot isolate McpCadDirectModelRuntime.Save")
    else:
        require(save, "document.Database.Save();", "cad_save")
        if "SaveAs(filename, DwgVersion.Current)" in save:
            errors.append("cad_save must not SaveAs over the active drawing's current path")
        if save.count("document.Database.Save();") != 1:
            errors.append("cad_save must perform exactly one current-document Save attempt")
        require(save, 'route=Database.Save-current-document', "cad_save")
        require(save, "WaitForSavedContentDbmod()", "cad_save")
        require(save, 'dbmodAfterSave=', "cad_save diagnostics")

    # BricsCAD DBMOD is a bitmask. Persistent drawing state is represented by object
    # database (1), database-variable (4), and field (32) bits. Window/view (8/16)
    # state may change again after the native save and must not cause a false failure.
    for token in (
        "private const int DbmodPersistentContentMask = 1 | 4 | 32;",
        "private static int WaitForSavedContentDbmod()",
        "(dbmod & DbmodPersistentContentMask) == 0",
        "window/view DBMOD bits may remain after save",
    ):
        require(direct, token, "content-aware DBMOD save confirmation")
    if "dbmod == 0" in direct:
        errors.append("save completion must not require the entire DBMOD bitmask to become zero")

    def content_saved(dbmod):
        return (dbmod & (1 | 4 | 32)) == 0

    assert content_saved(0)
    assert content_saved(8)
    assert content_saved(16)
    assert content_saved(8 | 16)
    assert not content_saved(1)
    assert not content_saved(4)
    assert not content_saved(32)
    assert not content_saved(1 | 8)
    assert not content_saved(4 | 16)
    assert not content_saved(32 | 8 | 16)

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
    layout_end = direct.find("private static string", layout_start + 1) if layout_start >= 0 else -1
    layout = direct[layout_start:layout_end] if layout_start >= 0 and layout_end > layout_start else ""
    if layout and "SendStringToExecute" in layout:
        errors.append("direct layout completion route must not use asynchronous SendStringToExecute")

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
    runbook = RUNBOOK.read_text(encoding="utf-8")
    require(runbook, "background_only", "canonical safety runbook")

print("QS3D MCP current-save/layout/foreground-policy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cad_save uses one current-document Save attempt, accepts successful saves when only window/view DBMOD bits remain, bounded layout operations are direct/deterministic, and foreground status preserves background_only startup while explaining consent vs policy.")