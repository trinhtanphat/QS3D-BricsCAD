#!/usr/bin/env python3
"""Source regression for #6117: native DWG commit must not be reported as full Save failure."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectFileUiService.cs"
text = SOURCE.read_text(encoding="utf-8")
errors: list[str] = []


def require(needle: str, label: str) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def method_slice(start: str, end: str) -> str:
    a = text.find(start)
    b = text.find(end, a + len(start)) if a >= 0 else -1
    if a < 0 or b < 0:
        errors.append(f"cannot isolate method: {start}")
        return ""
    return text[a:b]


save = method_slice("public static void SaveCurrentProject()", "public static void SaveCurrentProjectAs()")
save_as = method_slice("public static void SaveCurrentProjectAs()", "internal static void OpenProject(")

for body, operation, native_call in (
    (save, "Save", 'InvokeAcadDocumentMethod(document, "Save");'),
    (save_as, "SaveAs", 'InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing);'),
):
    if not body:
        continue
    flag = "var nativeDrawingCommitted = false;"
    committed = "nativeDrawingCommitted = true;"
    partial = "ShowPostNativeCommitFailure("
    for needle, label in ((flag, f"{operation} commit flag"), (native_call, f"{operation} native call"), (committed, f"{operation} commit publication"), (partial, f"{operation} partial-failure classification")):
        if needle not in body:
            errors.append(f"{operation}: missing {label}: {needle}")
    native_at = body.find(native_call)
    committed_at = body.find(committed)
    project_save_at = body.find("ProjectContextCoordinator.Save(document)")
    partial_at = body.find(partial)
    if min(native_at, committed_at, project_save_at, partial_at) >= 0:
        if not native_at < committed_at < project_save_at < partial_at:
            errors.append(
                f"{operation}: native commit classification ordering must be native call -> commit flag -> project save -> partial-failure branch"
            )

require("private static void ShowPostNativeCommitFailure(", "partial-commit presenter")
require("MessageBoxImage.Warning", "partial-commit warning severity")
require("Bản vẽ BricsCAD đã được lưu", "stable Save partial-success text")
require("Save As của bản vẽ BricsCAD đã hoàn tất", "stable SaveAs partial-success text")

helper = method_slice("private static void ShowPostNativeCommitFailure(", "private static void ShowError(")
if helper:
    for forbidden in ("exception.Message", "exception.ToString()", "InnerException", "StackTrace"):
        if forbidden in helper:
            errors.append(f"partial-commit presenter leaks exception detail: {forbidden}")
    for forbidden in ("InvokeAcadDocumentMethod", "ProjectContextCoordinator.Save(", "DocumentManager.Open", "SendStringToExecute"):
        if forbidden in helper:
            errors.append(f"partial-commit presenter must be presentation-only: {forbidden}")
    try_at = helper.find("try")
    show_at = helper.find("System.Windows.MessageBox.Show(")
    catch_at = helper.find("catch", show_at if show_at >= 0 else 0)
    if min(try_at, show_at, catch_at) < 0 or not try_at < show_at < catch_at:
        errors.append("partial-commit presenter must contain its own best-effort try/catch so UI failure cannot escape after native commit")

if errors:
    print("ERROR: V25 project Save native-commit truth preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    sys.exit(1)

print("OK: V25 project Save/SaveAs preserves partial-success truth after native DWG commit.")
