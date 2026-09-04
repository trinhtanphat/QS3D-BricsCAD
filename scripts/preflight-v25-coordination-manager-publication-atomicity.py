#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("ERROR: " + message)
    sys.exit(1)


def require(token: str, message: str) -> None:
    if token not in text:
        fail(message)


def order(first: str, second: str, message: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        fail(message)


require("private static PublishedManager? _publicationInFlight;", "missing exact unpublished publication reservation")
require("private static PublishedManager? _cleanupInFlight;", "missing exact modeless cleanup reservation")
require("private static bool _nativePublicationCallActive;", "missing native publication-stack reentrancy fence")
require("PrepareUnpublishedCandidate()", "stale unpublished candidates must be terminally reconciled before a new publication")
require("TryCloseManager(", "manager cleanup must use one exact-instance helper")
require("ReferenceEquals(_cleanupInFlight, manager)", "cleanup release must be exact-instance bound")
require("ReferenceEquals(_publicationInFlight, manager)", "publication release must be exact-instance bound")
require("if (_nativePublicationCallActive || _cleanupInFlight != null)", "reentrant invocation must fail closed while native show/close stack is active")
require("_publicationInFlight = published;", "candidate must reserve singleton ownership before native modeless publication")
require("_nativePublicationCallActive = true;", "native publication stack must be fenced before ShowModelessWindow")
require("_nativePublicationCallActive = false;", "native publication stack fence must unwind deterministically")
require("if (!publishedWindow.IsLoaded)", "non-loaded native publication must not be committed as published")
require("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)", "active document must be revalidated after native publication before commit")
require("_published = published;", "successful exact candidate must transition to published ownership")
require("_publicationInFlight = null;", "successful publication must release unpublished ownership")

order("_publicationInFlight = published;", "Application.ShowModelessWindow", "singleton reservation must precede native ShowModelessWindow")
order("_nativePublicationCallActive = true;", "Application.ShowModelessWindow", "native reentrancy fence must precede ShowModelessWindow")
order("Application.ShowModelessWindow", "if (!publishedWindow.IsLoaded)", "terminal-load check must follow native publication")
order("if (!publishedWindow.IsLoaded)", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)", "document-affinity check must follow terminal loaded-state validation")
order("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)", "_published = published;", "published ownership cannot be committed before active-document revalidation")
order("_published = published;", "_publicationInFlight = null;", "transition must publish exact owner before dropping unpublished reservation")

# Existing defect signature: native show followed immediately by first singleton assignment.
defect = "Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);\n                _published = published;"
if defect in text:
    fail("native publication still has an unreserved singleton gap")

print("PASS: Coordination Manager modeless publication/cleanup ownership is reentrancy-safe, document-affine, and exact-instance bound")
