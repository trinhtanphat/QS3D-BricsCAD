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
end = source.find("private void RefreshRecentProjects()", start)
method = source[start:end] if start >= 0 and end > start else ""

if not method:
    errors.append("RefreshHomeShell method was not found")
else:
    required = (
        "var document = Application.DocumentManager.MdiActiveDocument;",
        "if (document != null)",
        "else\n            {\n                Title = \"QS3D — Khởi đầu\";\n            }",
        "_floorText.Text = \"Tầng —\";",
        "_elevationText.Text = \"•  Cao độ 0.000 m\";",
        "RefreshRecentProjects();",
    )
    for token in required:
        if token not in method:
            errors.append("Start Center title-reset contract missing: " + token.replace("\n", " "))

    title_reset = method.find("else\n            {\n                Title = \"QS3D — Khởi đầu\";\n            }")
    floor_reset = method.find("_floorText.Text = \"Tầng —\";")
    if title_reset < 0 or floor_reset < 0 or title_reset >= floor_reset:
        errors.append("null-document title reset must happen before shared status reset")

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext",
        "ProjectFileUiService.Save",
        "SendStringToExecute",
        ".Touch(",
    ):
        if forbidden in method:
            errors.append("RefreshHomeShell title reset must remain display-only: " + forbidden)

print("QS3D Start Center null-document title-reset preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Start Center clears a closed DWG name when no document is active while preserving display-only refresh behavior.")
