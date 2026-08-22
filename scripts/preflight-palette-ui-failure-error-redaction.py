#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing PaletteCoordinator.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'catch (Exception)\n            {\n                ReportPaletteFailure("Workspace");',
        'catch (Exception)\n            {\n                ReportPaletteFailure("Safe Mode");',
        'catch (Exception)\n            {\n                ReportPaletteFailure("Status");',
        'private static void ReportPaletteFailure(string operation)',
        '"\\nQS3D " + operation + " UI error: không thể hoàn tất thao tác giao diện."',
        'Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(',
        '// Error reporting must never recurse into palette creation or mask the original failure.',
        'EnsureCreated();',
        'PersistPaletteLayout();',
    )
    for token in required:
        if token not in text:
            errors.append("Palette failure redaction contract missing token: " + token)

    forbidden = (
        'ReportPaletteFailure(string operation, Exception',
        'ReportPaletteFailure("Workspace", ex)',
        'ReportPaletteFailure("Safe Mode", ex)',
        'ReportPaletteFailure("Status", ex)',
        'DescribeException(',
        'error.Message',
        'current.Message',
        'ex.Message',
    )
    for token in forbidden:
        if token in text:
            errors.append("Palette failure path must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Palette UI failures keep operation context and protected Editor reporting without reflecting exception or inner-exception messages.")
