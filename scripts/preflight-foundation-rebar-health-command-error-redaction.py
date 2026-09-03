#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing FoundationMeshHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DFOUNDATIONREBARHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'CadHandleService.GetLiveSolidHandles(document, handles)',
        'new GeneratedFoundationMeshHealthService().Inspect(project, live)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'CadHandleService.Select(document, ParseHandles(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DFOUNDATIONREBARHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
        'FoundationMeshSolidBuilder.HandlesKey',
    )
    for token in required:
        if token not in text:
            errors.append("Foundation Rebar Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DFOUNDATIONREBARHEALTH lỗi: " +'):
        if token in text:
            errors.append("Foundation Rebar Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DFOUNDATIONREBARHEALTH routes through transactional Model Health publication while preserving live-handle locate and redacted errors.")
