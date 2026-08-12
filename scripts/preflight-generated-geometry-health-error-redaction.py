#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedGeometryHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        '[CommandMethod("QS3DGENERATEDHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new GeneratedGeometryStaleHealthService().Inspect(project)',
        'Report(document, "QS3DGENERATEDHEALTH lỗi: không thể hoàn tất health check.");',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\nQS3D " + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Generated Geometry Health contract missing token: " + token)

    forbidden = (
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DGENERATEDHEALTH lỗi: " +',
    )
    for token in forbidden:
        if token in text:
            errors.append("Generated Geometry Health must not reflect exception detail: " + token)

    if text.count('PaletteCoordinator.SetStatus(message)') != 1:
        errors.append("Generated Geometry Health must keep exactly one Palette status sink in Report().")
    if text.count('document.Editor.WriteMessage("\\nQS3D " + message)') != 1:
        errors.append("Generated Geometry Health must keep exactly one Editor sink in Report().")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DGENERATEDHEALTH keeps its read-only health/reporting contract while top-level exception details remain redacted from Palette and Editor output.")
