#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing HealthAllCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DHEALTHALL", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new ModelHealthService().Inspect(project, liveSources, liveMain)',
        'new GeneratedGeometryStaleHealthService().Inspect(project)',
        'new GeneratedRebarHealthService().InspectAll(project, liveLongitudinal, liveShape)',
        'new GeneratedHandleOwnershipHealthService().Inspect(project)',
        'new GeneratedRebarModeHealthService().Inspect(project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'LocateProjectArtifactHandles(currentProject, issue.Code).ToArray()',
        'LocateHandles(element, issue.Code).ToArray()',
        'SourceHandleResolver.Resolve(currentProject, new[] { element.Id }).ToArray()',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DHEALTHALL lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Health All command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DHEALTHALL lỗi: " +'):
        if token in text:
            errors.append("Health All command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DHEALTHALL routes through transactional Model Health publication while preserving representative aggregation, locate and error redaction.")
