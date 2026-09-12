#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BltLegacyCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

start = text.find("public void Import()")
end = text.find("private static void ApplyLegacyEvidence", start)
if start < 0 or end < 0:
    errors.append("cannot isolate QS3DBLTIMPORT body")
else:
    body = text[start:end]
    if "nativeDatabaseIdentity" not in body:
        errors.append("QS3DBLTIMPORT must capture the originating native database generation")
    ensure = body.find("DrawingUnitWorkflow.EnsureResolved")
    scan = body.find("BltLegacyCadInspector.ReadCurrentSpace(document)")
    if ensure < 0 or scan < 0:
        errors.append("expected unit-resolution/legacy-scan markers missing")
    elif "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)" not in body[ensure:scan]:
        errors.append("QS3DBLTIMPORT must revalidate exact document/native generation after unit resolution before scanning")

    capture = body.find("SemanticCaptureService.CaptureSnapshot(")
    if capture < 0:
        errors.append("semantic capture mutation handoff missing")
    else:
        prefix = body[:capture]
        if prefix.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)") < 0:
            errors.append("QS3DBLTIMPORT must fence exact generation before semantic capture mutation")

    success = body.find("QS3D BLT Import: ")
    if success < 0:
        errors.append("success publication marker missing")
    elif body.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)", 0, success) < 0:
        errors.append("success publication must be generation-safe")

report_start = text.find("private static void Report(Document document, string operation, Exception error)")
report_end = text.find("}\n    }\n\n    internal static class BltLegacyCadInspector", report_start)
if report_start < 0 or report_end < 0:
    errors.append("BLT legacy Report helper not found")
else:
    report_body = text[report_start:report_end]
    if "GetBaseException().Message" in report_body or "error.Message" in report_body:
        errors.append("BLT legacy public failure publication must redact raw exception messages")

print("QS3D V25 BLT legacy import document-generation affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: BLT legacy import fences scan, semantic mutation and UI publication to the exact document/native database generation.")
