#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR: Coordination review document-lifetime preflight failed closed:", message)
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

    retained_document = re.search(
        r"\b(?:private|protected|public|internal)\s+(?:readonly\s+)?Document\s+_[A-Za-z0-9_]+\s*;",
        source,
    )
    if retained_document:
        return fail("modeless coordination review retains a BricsCAD Document field: " + retained_document.group(0))

    retained_object_id = re.search(
        r"\b(?:private|protected|public|internal)\s+(?:readonly\s+)?(?:List|IReadOnlyList|IList|Collection)<ObjectId>\s+_[A-Za-z0-9_]+",
        source,
    )
    if retained_object_id:
        return fail("modeless coordination review retains ObjectId collection state: " + retained_object_id.group(0))

    try:
        require(source, "MdiActiveDocument", "action-time active-document resolution")
        require(source, "RequireCurrentProject(out document)", "current-document/project binding")
        require(source, "CadHandleService.Resolve(document, handles)", "fresh-document handle resolution")
        require(source, "List<string> _highlightedHandles", "portable transient highlight identity")
        require(source, "IsOriginDocument(e.Document)", "destroy-event stable origin identity")
        require(source, "AbandonDestroyedDocumentState()", "destruction-safe transient abandonment")
        require(source, "ResetTransientStateBestEffort(document)", "cleanup through a freshly resolved document")
        require(source, "document.Editor.WriteMessage", "fresh-document status routing")
        require(
            v26_project,
            '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
            "V26 shared V25 adapter-source parity",
        )
    except RuntimeError as exc:
        return fail(str(exc))

    if "private readonly Document _document" in source or "List<ObjectId> _highlighted" in source:
        return fail("legacy retained native review state remains")

    print("PASS: Coordination review keeps only stable/portable identity across modeless lifetime,")
    print("      resolves the active BricsCAD Document for each action/cleanup, abandons destroyed")
    print("      native state safely, and V26 compiles the same shared adapter source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
