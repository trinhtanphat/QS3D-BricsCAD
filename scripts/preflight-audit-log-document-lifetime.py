#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "AuditLogWindow.xaml.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR: Audit Log document-lifetime preflight failed closed:", message)
    return 1


def require(text, needle, description):
    if needle not in text:
        raise RuntimeError(description + ": missing " + repr(needle))


def main():
    try:
        source = SOURCE.read_text(encoding="utf-8")
        v26_project = V26_PROJECT.read_text(encoding="utf-8")
    except OSError as exc:
        return fail(str(exc))

    # Modeless projection code must not retain a proprietary host Document across
    # close/wrapper-lifetime boundaries. A method-local Document is allowed only
    # after resolving a currently live wrapper for the bound native database.
    retained_document = re.search(
        r"\b(?:private|protected|public|internal)\s+(?:readonly\s+)?Document\s+_[A-Za-z0-9_]+\s*;",
        source,
    )
    if retained_document:
        return fail("modeless Audit Log retains a BricsCAD Document field: " + retained_document.group(0))

    try:
        require(source, "using BcadApplication = Bricscad.ApplicationServices.Application;", "DocumentManager alias")
        require(source, "private readonly IntPtr _nativeDatabaseIdentity;", "stable native database identity")
        require(source, "DocumentBoundWindowLifetime.Attach(this, document);", "document-bound close/disposal lifetime")
        require(source, "TryResolveBoundDocument(out var document)", "reload-time live-document resolution")
        require(source, "foreach (Document candidate in BcadApplication.DocumentManager)", "live DocumentManager enumeration")
        require(source, "candidate == null || candidate.IsDisposed", "disposed-wrapper rejection")
        require(source, "database.UnmanagedObject == _nativeDatabaseIdentity", "bound native database validation")
        require(source, "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", "fresh-document project read")
        require(source, "DrawingLabel(document)", "fresh-document drawing label")
        require(
            v26_project,
            '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
            "V26 shared V25 adapter-source parity",
        )
    except RuntimeError as exc:
        return fail(str(exc))

    if "_document" in source:
        return fail("legacy retained-document identifier _document remains in AuditLogWindow")

    print("PASS: Audit Log keeps only stable native identity across modeless lifetime,")
    print("      resolves a live matching BricsCAD Document for each reload, and V26")
    print("      continues to compile the same shared adapter source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
