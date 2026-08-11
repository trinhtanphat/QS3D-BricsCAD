#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs"
errors = []

if not COMMANDS.is_file():
    errors.append("missing rebar aggregate health command: " + str(COMMANDS.relative_to(ROOT)))
else:
    text = COMMANDS.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DREBARHEALTHALL"',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "lệnh kiểm tra không tạo project mới",
        "BbsNativeTableBuilder.Inspect(document, project)",
        "private static void ReportBlocked(Document document, string message)",
    )
    for token in required:
        if token not in text:
            errors.append("RebarHealthAllCommands.cs missing read-only health token: " + token)

    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DREBARHEALTHALL must remain diagnostic-only and must not create project state.")

    lookup = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    handle_scan = text.find('var columnHandles = Collect(project, "GeneratedRebarHandles")')
    bbs = text.find("BbsNativeTableBuilder.Inspect(document, project)")
    if lookup < 0 or handle_scan < 0 or lookup > handle_scan:
        errors.append("Read-only project lookup must happen before generated rebar handle scanning.")
    if bbs < 0 or (handle_scan >= 0 and bbs < handle_scan):
        errors.append("BBS native-table health must remain part of the aggregate after project lookup.")

print("QS3D rebar aggregate health read-only preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: QS3DREBARHEALTHALL uses read-only project lookup, blocks cleanly when no project exists, preserves BBS health coverage, and cannot silently regress to GetOrCreate.")
