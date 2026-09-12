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
    "private static void PublishImportSuccessIfActive(Document document, IntPtr nativeDatabaseIdentity",
    "private static void ReportIfActive(Document document, IntPtr nativeDatabaseIdentity",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

start = text.find("public void Import()")
end = text.find("private static bool IsActiveDocumentGeneration", start)
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
        suffix = body[capture:]
        next_fence = suffix.find("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)")
        if next_fence < 0:
            errors.append("QS3DBLTIMPORT must revalidate exact generation after semantic capture before continuing")

    publish = body.find("PublishImportSuccessIfActive(document, nativeDatabaseIdentity")
    if publish < 0:
        errors.append("success publication must use the generation-safe helper")
    elif body.rfind("RequireActiveDocumentGeneration(document, nativeDatabaseIdentity)", 0, publish) < 0:
        errors.append("success publication must be preceded by an exact generation fence")

    if "ReportIfActive(document, nativeDatabaseIdentity, \"QS3DBLTIMPORT\", error)" not in body:
        errors.append("failure publication must be generation-safe")

publish_start = text.find("private static void PublishImportSuccessIfActive(Document document, IntPtr nativeDatabaseIdentity")
publish_end = text.find("private static void ReportIfActive", publish_start)
if publish_start < 0 or publish_end < 0:
    errors.append("generation-safe success helper not found")
else:
    publish_body = text[publish_start:publish_end]
    if "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)" not in publish_body:
        errors.append("success helper must suppress stale-generation publication")
    if "TryWriteMessage(document, message)" not in publish_body:
        errors.append("success helper must keep UI output best-effort")

report_start = text.find("private static void Report(Document document, string operation, Exception error)")
report_end = text.find("private static void TryWriteMessage", report_start)
if report_start < 0 or report_end < 0:
    errors.append("BLT legacy Report helper not found")
else:
    report_body = text[report_start:report_end]
    if "GetBaseException().Message" in report_body or "error.Message" in report_body:
        errors.append("BLT legacy public failure publication must redact raw exception messages")
    if "error.GetType().Name" not in report_body:
        errors.append("BLT legacy public failure publication must retain type-only diagnostic evidence")

print("QS3D V25 BLT legacy import document-generation affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: BLT legacy import fences scan, semantic mutation and UI publication to the exact document/native database generation.")
