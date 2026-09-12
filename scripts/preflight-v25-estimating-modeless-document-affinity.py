#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "EstimatingRateBuildUpCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private sealed class WindowOwner",
    "WeakReference<Document>",
    "NativeDatabaseIdentity",
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "CloseUnpublishedCandidate(candidate)",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);")
publish = text.find("_window = candidate;", show)
if show < 0:
    errors.append("modeless host call not found")
elif publish < 0:
    errors.append("published-window assignment not found after modeless host call")
else:
    between = text[show:publish]
    if "IsActiveDocumentGeneration(document, requestedIdentity)" not in between:
        errors.append("exact managed/native generation must be revalidated after modeless host pumping before publication")
    if "CloseUnpublishedCandidate(candidate)" not in between:
        errors.append("stale post-host candidate must be terminal-close attempted before abandoning publication")

if "private static IntPtr _nativeDatabaseIdentity;" in text:
    errors.append("raw global native identity must not be the sole published-window ownership authority")

print("QS3D V25 estimating modeless document-generation affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: estimating modeless publication is bound to the exact managed document/native database generation.")
