#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawRepeatedCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void RequireActiveDocumentGeneration(",
    "database.UnmanagedObject == nativeDatabaseIdentity",
    "RunCore(document, nativeDatabaseIdentity, category, label, expectedProjectId, expectedFamilyId);",
    "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity, label",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

run_start = text.find("private static void Run(")
core_start = text.find("private static void RunCore(", run_start)
if run_start < 0 or core_start < 0:
    errors.append("cannot isolate repeated command admission")
else:
    run = text[run_start:core_start]
    capture = run.find("var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);")
    call = run.find("RunCore(document, nativeDatabaseIdentity")
    if capture < 0 or call < 0 or capture > call:
        errors.append("native database identity must be captured before repeated command core")

core_end = text.find("private static RepeatedWholeCommandRollbackException", core_start)
if core_start < 0 or core_end < 0:
    errors.append("cannot isolate repeated command core")
else:
    core = text[core_start:core_end]
    if "IntPtr nativeDatabaseIdentity" not in core:
        errors.append("repeated command core does not carry native database identity")
    if core.count("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity") < 4:
        errors.append("repeated prompts/drag/mutation handoff are not sufficiently generation-fenced")
    execute = core.find("DirectDrawCommands.ExecuteDirect(")
    before_execute = core.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity", 0, execute)
    if execute < 0 or before_execute < 0:
        errors.append("ExecuteDirect handoff lacks an exact native-generation fence")

report_start = text.find("private static void Report(")
if report_start >= 0:
    report = text[report_start:]
    if "IntPtr nativeDatabaseIdentity" not in report:
        errors.append("repeated status/error publication remains managed-document-only")

print("QS3D V25 repeated Direct Draw document-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: repeated Direct Draw prompt, mutation handoff, and publication are bound to exact managed/native document generation.")
