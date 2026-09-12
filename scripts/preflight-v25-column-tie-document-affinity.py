#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ColumnTieCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "FinalizeUi(document, nativeDatabaseIdentity, message);",
    "RefreshModelTree(document, nativeDatabaseIdentity);",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

command_start = text.find("public void BuildColumnTies()")
command_end = text.find("private static void FinalizeUi", command_start)
if command_start < 0 or command_end < 0:
    errors.append("cannot isolate Column Tie authoring command body")
else:
    body = text[command_start:command_end]
    mutation = body.find("ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds)")
    if mutation < 0:
        errors.append("Column Tie native geometry handoff missing")
    elif body.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);", 0, mutation) < 0:
        errors.append("exact document/native generation fence must precede retained ObjectId geometry handoff")

finalize_start = text.find("private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)")
if finalize_start < 0:
    errors.append("FinalizeUi must carry captured native database generation")
else:
    finalize_end = text.find("private static", finalize_start + 20)
    if finalize_end < 0:
        finalize_end = len(text)
    body = text[finalize_start:finalize_end]
    refresh = body.find("RefreshModelTree(document, nativeDatabaseIdentity);")
    regen = body.find("document.Editor.Regen();")
    status = body.find("TrySetPaletteStatus(document, nativeDatabaseIdentity, message);")
    output = body.find("document.Editor.WriteMessage(\"\\nQS3D \" + message);")
    first_fence = body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;")
    if min(refresh, regen, status, output) < 0:
        errors.append("post-commit UI path must use generation-safe refresh/Regen/status/output")
    elif not (first_fence >= 0 and first_fence < refresh < regen < status < output):
        errors.append("post-commit UI ordering must be fenced refresh -> Regen -> status -> output")
    else:
        if body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", refresh) < 0:
            errors.append("generation must be revalidated after palette refresh before Regen")
        if body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", regen) < 0:
            errors.append("generation must be revalidated after Regen before status")
        if body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", status) < 0:
            errors.append("generation must be revalidated after status before editor output")
    if "ex.Message" in body:
        errors.append("post-commit UI warning must redact exception message")
    if "ex.GetType().Name" not in body:
        errors.append("post-commit UI warning should retain type-only diagnostic evidence")

forbidden = [
    "FinalizeUi(document, message);",
    "private static void Report(Document document, string message)",
]
for needle in forbidden:
    if needle in text:
        errors.append("stale document-only UI topology remains: " + needle)

print("QS3D V25 Column Tie document-generation/geometry-handoff preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Column Tie geometry handoff and post-commit UI are bound to exact managed document/native database generation.")
