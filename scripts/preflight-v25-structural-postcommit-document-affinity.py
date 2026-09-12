#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCES = [
    ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamRebarCommands.cs",
    ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs",
]
errors = []

for source in SOURCES:
    text = source.read_text(encoding="utf-8")
    label = source.name
    required = [
        "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);",
        "FinalizeUi(document, nativeDatabaseIdentity, message);",
        "private static IntPtr GetNativeDatabaseIdentity(Document document)",
        "return document.Database.UnmanagedObject;",
        "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
        "document.Database.UnmanagedObject == nativeDatabaseIdentity",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
        "RefreshModelTree(document, nativeDatabaseIdentity);",
        "private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)",
        "ex.GetType().Name",
    ]
    for needle in required:
        if needle not in text:
            errors.append(f"{label}: missing contract: {needle}")

    mutation = text.find("BuildSelected(document, project, selectedIds")
    mutation_fence = text.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);", 0, mutation)
    if mutation < 0:
        errors.append(f"{label}: structural BuildSelected mutation call not found")
    elif mutation_fence < 0:
        errors.append(f"{label}: exact document-generation authority fence must precede geometry handoff")

    finalize = text.find("private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)")
    if finalize < 0:
        errors.append(f"{label}: FinalizeUi must carry the captured native database generation")
    else:
        body_end = text.find("private static", finalize + 20)
        if body_end < 0:
            body_end = len(text)
        body = text[finalize:body_end]
        refresh = body.find("RefreshModelTree(document, nativeDatabaseIdentity);")
        regen = body.find("document.Editor.Regen();")
        first_fence = body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;")
        if refresh < 0 or regen < 0:
            errors.append(f"{label}: missing post-commit refresh/Regen")
        elif first_fence < 0 or first_fence > refresh or refresh > regen:
            errors.append(f"{label}: exact generation fence must precede ordered refresh -> Regen")
        elif body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", refresh) < 0:
            errors.append(f"{label}: generation must be revalidated after palette refresh before Regen")
        if "ex.Message" in body:
            errors.append(f"{label}: post-commit UI warning must redact exception message")

    refresh_method = text.find("private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)")
    if refresh_method >= 0:
        refresh_end = text.find("private static", refresh_method + 20)
        if refresh_end < 0:
            refresh_end = len(text)
        refresh_body = text[refresh_method:refresh_end]
        if "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;" not in refresh_body:
            errors.append(f"{label}: palette refresh helper lacks exact generation fence")

    for forbidden in [
        "private static bool IsActiveDocument(Document document)",
        "FinalizeUi(document, message);",
    ]:
        if forbidden in text:
            errors.append(f"{label}: stale mutation UI topology remains: {forbidden}")

print("QS3D V25 structural document-generation/geometry-handoff preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: structural authoring geometry handoff and post-commit UI are bound to the exact managed document/native database generation.")
