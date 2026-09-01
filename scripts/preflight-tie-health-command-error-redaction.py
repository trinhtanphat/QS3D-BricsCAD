#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ColumnTieHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DREBARTIEHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'project.Elements.SelectMany(ParseHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()',
        'CadHandleService.GetLiveSolidHandles(document, handles)',
        'new GeneratedTieRebarHealthService().Inspect(project, live)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'CadHandleService.Select(document, ParseHandles(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DREBARTIEHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Tie Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DREBARTIEHEALTH lỗi: " +'):
        if token in text:
            errors.append("Tie Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREBARTIEHEALTH routes through transactional Model Health publication while preserving locate behavior and redacted top-level errors.")
