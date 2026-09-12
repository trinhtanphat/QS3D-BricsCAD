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
        "FinalizeUi(document, nativeDatabaseIdentity, message);",
        "private static IntPtr GetNativeDatabaseIdentity(Document document)",
        "return document.Database.UnmanagedObject;",
        "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
        "document.Database.UnmanagedObject == nativeDatabaseIdentity",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    ]
    for needle in required:
        if needle not in text:
            errors.append(f"{label}: missing contract: {needle}")

    finalize = text.find("private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)")
    if finalize < 0:
        errors.append(f"{label}: FinalizeUi must carry the captured native database generation")
    else:
        body_end = text.find("private static", finalize + 20)
        if body_end < 0:
            body_end = len(text)
        body = text[finalize:body_end]
        regen = body.find("document.Editor.Regen();")
        fence = body.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;")
        if regen < 0:
            errors.append(f"{label}: missing post-commit Regen")
        elif fence < 0 or fence > regen:
            errors.append(f"{label}: exact document-generation fence must precede post-commit Regen")

    for forbidden in [
        "private static bool IsActiveDocument(Document document)",
        "FinalizeUi(document, message);",
    ]:
        if forbidden in text:
            errors.append(f"{label}: stale document-only UI topology remains: {forbidden}")

print("QS3D V25 structural post-commit document-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: structural authoring post-commit UI is bound to the exact managed document/native database generation.")
