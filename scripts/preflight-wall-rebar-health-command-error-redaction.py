#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing StructuralWallMeshHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DWALLREBARHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'GeneratedWallMeshHandles',
        'CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase))',
        'new GeneratedWallMeshHealthService().Inspect(project, live)',
        'issues.Take(50)',
        'if (issues.Count > 50) document.Editor.WriteMessage("\\n  … health output truncated.");',
        'var message = "QS3DWALLREBARHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Wall Rebar Health command contract missing token: " + token)

    for token in ('catch (System.Exception ex)', 'ex.Message', 'QS3DWALLREBARHEALTH lỗi: " +'):
        if token in text:
            errors.append("Wall Rebar Health command must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DWALLREBARHEALTH preserves wall-mesh live health reporting while top-level exception details remain redacted.")
