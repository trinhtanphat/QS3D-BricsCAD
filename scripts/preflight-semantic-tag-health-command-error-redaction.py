#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SemanticTagHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DTAGHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new GeneratedSemanticTagHealthService().Inspect(project)',
        'GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)',
        '.GroupBy(x => (x.Code ?? string.Empty)',
        'var ok = "Semantic Tag Health: PASS.";',
        'issues.Take(100)',
        'Locate(document, project, issues)',
        'GeneratedSemanticTagHealthService.HandlesKey',
        'CadHandleService.SelectIfAny(document, handles)',
        'var message = "QS3DTAGHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\nQS3D " + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Semantic Tag Health command contract missing token: " + token)

    for token in ('catch (Exception ex)', 'ex.Message', 'QS3DTAGHEALTH lỗi: " +'):
        if token in text:
            errors.append("Semantic Tag Health command must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DTAGHEALTH preserves persisted/runtime tag-health reporting and locate behavior while top-level exception details remain redacted.")
