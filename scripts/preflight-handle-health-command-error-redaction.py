#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedHandleOwnershipHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DHANDLEHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new GeneratedHandleOwnershipHealthService().Inspect(project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'Cad.CadHandleService.Select(document, Services.SemanticReferenceHandles.Get(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'Report(document, "QS3DHANDLEHEALTH lỗi: không thể hoàn tất health check.");',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\nQS3D " + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Handle Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DHANDLEHEALTH lỗi: " +'):
        if token in text:
            errors.append("Handle Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DHANDLEHEALTH routes through transactional Model Health publication while preserving its read-only locate and error-redaction contract.")
