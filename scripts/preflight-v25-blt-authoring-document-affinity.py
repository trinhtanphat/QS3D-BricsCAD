#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BltToolCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "Action<Document, IntPtr> action",
    "var nativeDatabaseIdentity = document.Database.UnmanagedObject;",
    "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)",
    "RegenAndReportIfActive(document, nativeDatabaseIdentity",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

# The three BLT geometry authoring workflows retain ObjectIds across one or more
# Editor prompts. Each must fence the exact managed/native generation after the
# last prompt and before the transaction/LockDocument mutation handoff.
commands = [
    ("public void LowerPilesToPileCap()", "public void CreateLeanConcrete()"),
    ("public void CreateLeanConcrete()", "public void CreateFoundationExcavationVolume()"),
    ("public void CreateFoundationExcavationVolume()", "[CommandMethod(\"QS3DMCPSETTINGS\"")
]
for start_marker, end_marker in commands:
    start = text.find(start_marker)
    end = text.find(end_marker, start + 1)
    if start < 0 or end < 0:
        errors.append("cannot isolate authoring command: " + start_marker)
        continue
    body = text[start:end]
    lock = body.find("using (document.LockDocument())")
    fence = body.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)", 0, lock if lock >= 0 else len(body))
    if lock < 0:
        errors.append(start_marker + ": native mutation lock not found")
    elif fence < 0:
        errors.append(start_marker + ": exact document/native generation must be fenced immediately before mutation handoff")

# A committed native transaction must not be reported as command failure merely
# because Regen/status pumps host work or the originating generation becomes stale.
for marker in ["transaction.Commit();"]:
    if marker not in text:
        errors.append("expected native commit marker missing")
if "document.Editor.Regen();\n                Report(" in text:
    errors.append("raw Regen + Report after commit must be replaced by generation-safe post-commit UI publication")

run_start = text.find("private static void Run(string operation")
run_end = text.find("private static Extents3d RequireExtents", run_start)
if run_start < 0 or run_end < 0:
    errors.append("Run helper not found")
else:
    run_body = text[run_start:run_end]
    if "ex.Message" in run_body:
        errors.append("common command failure path must redact raw exception messages")
    if "Report(document, operation + \" lỗi:" in run_body:
        errors.append("common command failure path must not publish through an unvalidated captured document")

print("QS3D V25 BLT structural-authoring document-generation affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: BLT structural authoring fences retained ObjectIds and post-commit UI to the exact document/native database generation.")
