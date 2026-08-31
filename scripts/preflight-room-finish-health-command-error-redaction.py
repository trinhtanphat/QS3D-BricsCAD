#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RoomFinishHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RoomFinishHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DROOMFINISHHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new RoomFinishHealthService().Inspect(project).ToList()',
        'if (issues.Count == 0) return;',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'SourceHandleResolver.Resolve(currentProject, new[] { issue.ElementId })',
        'CadHandleService.Select(document, handles)',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var status = "QS3DROOMFINISHHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(status)',
        'document.Editor.WriteMessage("\\n" + status)',
    )
    for token in required:
        if token not in text:
            errors.append("Room Finish Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DROOMFINISHHEALTH lỗi: " +'):
        if token in text:
            errors.append("Room Finish Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DROOMFINISHHEALTH routes through transactional Model Health publication while preserving review/locate and redacted top-level errors.")
