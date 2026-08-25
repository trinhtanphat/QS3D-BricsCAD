#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerWindow.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR: Coordination Manager document-lifetime preflight failed closed:", message)
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

    # A modeless WPF window must not retain the proprietary host Document across
    # document close/activation boundaries. Method-local Document variables are
    # intentionally allowed after action-time resolution and identity validation.
    retained_document = re.search(
        r"\b(?:private|protected|public|internal)\s+(?:readonly\s+)?Document\s+_[A-Za-z0-9_]+\s*;",
        source,
    )
    if retained_document:
        return fail("modeless window retains a BricsCAD Document field: " + retained_document.group(0))

    try:
        require(
            source,
            "document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument",
            "action-time active-document resolution",
        )
        require(source, "RequireCurrentProject(false, out var document)", "Locate current-document binding")
        require(source, "RequireCurrentProject(true, out var document)", "mutation current-document binding")
        require(source, "project.ProjectId, _projectId", "ProjectId identity validation")
        require(source, "project.DrawingFingerprint, _drawingFingerprint", "DrawingFingerprint identity validation")
        require(source, "CadHandleService.Resolve(document, handles)", "fresh-document handle resolution")
        require(source, "document.Editor.SetImpliedSelection", "fresh-document selection")
        require(source, "TryZoomSelection(document)", "fresh-document zoom")
        require(source, "ProjectContextCoordinator.Save(document)", "fresh-document save")
        require(
            v26_project,
            '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
            "V26 shared V25 adapter-source parity",
        )
    except RuntimeError as exc:
        return fail(str(exc))

    if "_document" in source:
        return fail("legacy retained-document identifier _document remains in CoordinationManagerWindow")

    print("PASS: Coordination Manager keeps only stable identity tokens across modeless lifetime,")
    print("      resolves the active BricsCAD Document per action, validates ProjectId/fingerprint,")
    print("      and V26 continues to compile the same shared adapter source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
