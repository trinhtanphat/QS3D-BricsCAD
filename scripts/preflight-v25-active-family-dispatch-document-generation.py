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
    "private static void RequireActiveDocumentGeneration(",
    "database.UnmanagedObject == nativeDatabaseIdentity",
    "RequireCurrentDispatchSnapshot(\n                    document,\n                    nativeDatabaseIdentity,",
    "Report(document, nativeDatabaseIdentity,",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

snapshot_start = text.find("private static ProjectFamily RequireCurrentDispatchSnapshot")
snapshot_end = text.find("private static void Dispatch(", snapshot_start)
if snapshot_start < 0 or snapshot_end < 0:
    errors.append("cannot isolate dispatch snapshot authority")
else:
    snapshot = text[snapshot_start:snapshot_end]
    if "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity, operation);" not in snapshot:
        errors.append("dispatch snapshot is not fenced to exact native database generation")

core_start = text.find("private static void DrawActiveFamilyCore")
core_end = text.find("private static ProjectFamily RequireCurrentDispatchSnapshot", core_start)
if core_start < 0 or core_end < 0:
    errors.append("cannot isolate active-family command core")
else:
    body = text[core_start:core_end]
    capture = body.find("var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);")
    fence = body.find("RequireCurrentDispatchSnapshot(")
    dispatch = body.find("Dispatch(document, nativeDatabaseIdentity")
    repeated = body.find("DispatchRepeated(\n                        document,\n                        nativeDatabaseIdentity,")
    if capture < 0 or fence < 0 or capture > fence:
        errors.append("native database identity must be captured before dispatch authority validation")
    if dispatch < 0:
        errors.append("Quick/Advanced dispatch does not carry native database identity")
    elif fence > dispatch:
        errors.append("generation authority must precede Quick/Advanced dispatch")
    if repeated < 0:
        errors.append("repeated dispatch does not carry native database identity")
    elif fence > repeated:
        errors.append("generation authority must precede repeated dispatch")

for method_name, next_name in [
    ("private static void Dispatch(", "private static void DispatchRepeated("),
    ("private static void DispatchRepeated(", "private static bool IsWindowFamily("),
]:
    start = text.find(method_name)
    end = text.find(next_name, start)
    if start < 0 or end < 0:
        errors.append("cannot isolate " + method_name)
        continue
    body = text[start:end]
    if "IntPtr nativeDatabaseIdentity" not in body:
        errors.append(method_name + " does not receive native database identity")
    if "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity, operation);" not in body:
        errors.append(method_name + " lacks exact-generation fence at target handoff")

report_start = text.find("private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)")
if report_start < 0:
    errors.append("status/editor publication is not generation-bound")
else:
    report = text[report_start:]
    if report.count("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)") < 2:
        errors.append("status/editor publication must revalidate generation before and after editor output")

if "private static void Report(Document document, string message)" in text:
    errors.append("status/editor publication remains managed-document-only")

print("QS3D V25 Active Family dispatch document-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Active Family Direct Draw snapshot, target handoff, and UI publication are bound to exact managed/native document generation.")
