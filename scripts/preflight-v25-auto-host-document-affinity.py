#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "AutoHostLinkCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "RequireCurrentMutationAuthority(\n                    document,\n                    nativeDatabaseIdentity,",
    "FinalizeAutoHostUi(document, nativeDatabaseIdentity, summary);",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

if "private static bool IsActiveDocument(Document document)" in text:
    errors.append("managed-document-only authority helper remains")
if "private static void FinalizeAutoHostUi(Document document, string summary)" in text:
    errors.append("post-commit UI is not bound to native database generation")

main_start = text.find("public void AutoLinkHosts()")
main_end = text.find("private static void RequireCurrentMutationAuthority", main_start)
if main_start < 0 or main_end < 0:
    errors.append("cannot isolate Auto Host command")
else:
    body = text[main_start:main_end]
    service_mutation = body.find("service.LinkOpening(project, item.Opening.Id, item.HostId)")
    authority = body.find("RequireCurrentMutationAuthority(")
    if service_mutation < 0 or authority < 0 or authority > service_mutation:
        errors.append("exact generation/project authority must precede semantic HostLink mutation")

single_start = text.find("internal static string LinkSingleOpening")
if single_start < 0:
    errors.append("LinkSingleOpening missing")
else:
    single_end = text.find("private static", single_start + 20)
    if single_end < 0:
        single_end = len(text)
    single = text[single_start:single_end]
    for needle in [
        "nativeDatabaseIdentity",
        "IsActiveDocumentGeneration",
        "expectedProjectId",
        "expectedChangeVersion",
        "currentProject.ChangeVersion != expectedChangeVersion",
    ]:
        if needle not in single:
            errors.append("single-opening generation contract missing: " + needle)
    service_mutation = single.find("new HostLinkService().LinkOpening")
    project_generation = single.find("currentProject.ChangeVersion != expectedChangeVersion")
    if service_mutation < 0 or project_generation < 0 or project_generation > service_mutation:
        errors.append("single-opening project generation must be revalidated before semantic HostLink mutation")

print("QS3D V25 Auto Host document-generation affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Auto Host CAD-read, semantic mutation, and UI publication are bound to exact document/native DB/project generation.")
