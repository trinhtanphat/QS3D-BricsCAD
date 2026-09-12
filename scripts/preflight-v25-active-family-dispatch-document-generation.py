#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ActiveFamilyQuickDrawCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "RequireCurrentDispatchSnapshot(\n                    document,\n                    nativeDatabaseIdentity,",
    "Report(document, nativeDatabaseIdentity,",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

snapshot_start = text.find("private static ProjectFamily RequireCurrentDispatchSnapshot")
snapshot_end = text.find("private static void Dispatch", snapshot_start)
if snapshot_start < 0 or snapshot_end < 0:
    errors.append("cannot isolate dispatch snapshot authority")
else:
    snapshot = text[snapshot_start:snapshot_end]
    if "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)" not in snapshot:
        errors.append("dispatch snapshot is not fenced to exact native database generation")

core_start = text.find("private static void DrawActiveFamilyCore")
core_end = text.find("private static ProjectFamily RequireCurrentDispatchSnapshot", core_start)
if core_start < 0 or core_end < 0:
    errors.append("cannot isolate active-family command core")
else:
    body = text[core_start:core_end]
    capture = body.find("var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);")
    dispatch = body.find("Dispatch(document, dispatchFamily")
    repeated = body.find("DispatchRepeated(document, dispatchFamily")
    fence = body.find("RequireCurrentDispatchSnapshot(")
    if capture < 0 or fence < 0 or capture > fence:
        errors.append("native database identity must be captured before dispatch authority validation")
    if dispatch >= 0 and fence > dispatch:
        errors.append("generation authority must precede Quick/Advanced dispatch")
    if repeated >= 0 and fence > repeated:
        errors.append("generation authority must precede repeated dispatch")

if "private static void Report(Document document, string message)" in text:
    errors.append("status/editor publication remains managed-document-only")

print("QS3D V25 Active Family dispatch document-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Active Family Direct Draw dispatch and UI publication are bound to exact managed/native document generation.")
