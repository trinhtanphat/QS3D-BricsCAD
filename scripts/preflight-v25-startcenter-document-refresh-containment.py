#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

refresh_start = source.find("private void RefreshHomeShell(bool recordActiveDrawing)")
helper_start = source.find(
    "private static bool TryReadDocumentPath(Bricscad.ApplicationServices.Document document, out string path)",
    refresh_start if refresh_start >= 0 else 0,
)
refresh_end = helper_start if helper_start >= 0 else source.find(
    "private void RefreshRecentProjects()", refresh_start if refresh_start >= 0 else 0
)
refresh = source[refresh_start:refresh_end if refresh_end >= 0 else len(source)] if refresh_start >= 0 else ""
if refresh_start < 0:
    errors.append("missing RefreshHomeShell")
else:
    required = [
        'Title = "QS3D — Khởi đầu";',
        '_floorText.Text = "Tầng —";',
        '_elevationText.Text = "•  Cao độ 0.000 m";',
        'var document = Application.DocumentManager.MdiActiveDocument;',
        'var hasDocumentPath = TryReadDocumentPath(document, out var path);',
        'if (hasDocumentPath && recordActiveDrawing && StartCenterUserStateStore.TryNormalizeDwgPath(path, out var normalized))',
    ]
    for needle in required:
        if needle not in refresh:
            errors.append("Start Center refresh containment contract missing: " + needle)

    title_pos = refresh.find('Title = "QS3D — Khởi đầu";')
    floor_pos = refresh.find('_floorText.Text = "Tầng —";')
    elevation_pos = refresh.find('_elevationText.Text = "•  Cao độ 0.000 m";')
    document_pos = refresh.find('var document = Application.DocumentManager.MdiActiveDocument;')
    read_pos = refresh.find('var hasDocumentPath = TryReadDocumentPath(document, out var path);')
    record_pos = refresh.find('StartCenterUserStateStore.RecordProject(normalized);')
    if min(title_pos, floor_pos, elevation_pos, document_pos, read_pos) < 0 or not (
        title_pos < document_pos and floor_pos < document_pos and elevation_pos < document_pos < read_pos
    ):
        errors.append("neutral title/floor/elevation must be established before any active Document lookup/native path read")
    if record_pos >= 0 and read_pos >= 0 and record_pos < read_pos:
        errors.append("recent-project recording must occur only after a trustworthy exact-Document path read")

    if "document.Name" in refresh:
        errors.append("RefreshHomeShell must not dereference native Document.Name outside containment")

helper_end = source.find("private void RefreshRecentProjects()", helper_start if helper_start >= 0 else 0)
helper = source[helper_start:helper_end if helper_end >= 0 else len(source)] if helper_start >= 0 else ""
if helper_start < 0:
    errors.append("missing exact-Document TryReadDocumentPath helper")
else:
    for needle in [
        "path = string.Empty;",
        "if (document == null) return false;",
        "try",
        "path = document.Name ?? string.Empty;",
        "return true;",
        "catch",
        "return false;",
    ]:
        if needle not in helper:
            errors.append("document path helper must contain stale native Name access: " + needle)

    for forbidden in [
        "MdiActiveDocument",
        "ProjectContextCoordinator",
        "RecordProject",
        "StartTransaction",
        "DocumentLock",
        "SendStringToExecute",
        "Dispatcher.BeginInvoke",
    ]:
        if forbidden in helper:
            errors.append("document path helper must remain exact-instance/read-only: " + forbidden)

print("QS3D V25 Start Center document refresh containment preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Start Center resets neutral document UI first, contains exact-Document Name reads, and records recents only from trustworthy paths.")
