#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing BltStartCenterWindow.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

start = source.find("private void RefreshHomeShell(bool recordActiveDrawing)")
helper = source.find(
    "private static bool TryReadDocumentPath(Bricscad.ApplicationServices.Document document, out string path)",
    start if start >= 0 else 0,
)
end = helper if helper > start else source.find("private void RefreshRecentProjects()", start)
method = source[start:end] if start >= 0 and end > start else ""

if not method:
    errors.append("RefreshHomeShell method was not found")
else:
    required = (
        'Title = "QS3D — Khởi đầu";',
        '_floorText.Text = "Tầng —";',
        '_elevationText.Text = "•  Cao độ 0.000 m";',
        "var document = Application.DocumentManager.MdiActiveDocument;",
        "RefreshRecentProjects();",
    )
    for token in required:
        if token not in method:
            errors.append("Start Center title-reset contract missing: " + token)

    title_reset = method.find('Title = "QS3D — Khởi đầu";')
    floor_reset = method.find('_floorText.Text = "Tầng —";')
    elevation_reset = method.find('_elevationText.Text = "•  Cao độ 0.000 m";')
    document_lookup = method.find("var document = Application.DocumentManager.MdiActiveDocument;")
    if min(title_reset, floor_reset, elevation_reset, document_lookup) < 0 or not (
        title_reset < document_lookup
        and floor_reset < document_lookup
        and elevation_reset < document_lookup
    ):
        errors.append("document-neutral title/floor/elevation reset must happen before active-document/native access")

    # A title derived from a document may only be published after the neutral reset. This preserves
    # the historical null-document guarantee while also covering stale native wrappers that throw.
    dynamic_title = method.find('Title = "QS3D — " + display;')
    if dynamic_title >= 0 and dynamic_title < document_lookup:
        errors.append("drawing title publication must follow the neutral reset and document lookup")

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext",
        "ProjectFileUiService.Save",
        "SendStringToExecute",
        ".Touch(",
    ):
        if forbidden in method:
            errors.append("RefreshHomeShell title reset must remain display-only: " + forbidden)

print("QS3D Start Center null/stale-document title-reset preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Start Center clears closed/stale DWG title and status before fallible document access while preserving display-only refresh behavior.")
