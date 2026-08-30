#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RebarModeHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RebarModeHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DREBARMODEHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new GeneratedRebarModeHealthService().Inspect(project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'element.Properties.TryGetValue("GeneratedRebarHandles", out var raw)',
        ': element.SourceHandles.ToArray();',
        'CadHandleService.Select(document, handles)',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DREBARMODEHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Rebar Mode Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DREBARMODEHEALTH lỗi: " +'):
        if token in text:
            errors.append("Rebar Mode Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREBARMODEHEALTH routes through transactional Model Health publication while preserving generated/source locate fallback and redacted errors.")
