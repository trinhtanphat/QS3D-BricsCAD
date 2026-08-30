#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing CurtainWallFrameHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DCURTAINFRAMEHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'CadHandleService.GetLiveSolidHandles(document, handles)',
        'new GeneratedCurtainFrameHealthService().Inspect(project, live)',
        'CurtainWallFrameLiveStateService.Inspect(document, project)',
        'new GeneratedCurtainPanelHealthService().Inspect(project, live)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'CadHandleService.Select(document, ParseHandles(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DCURTAINFRAMEHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain Frame Health command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DCURTAINFRAMEHEALTH lỗi: " +'):
        if token in text:
            errors.append("Curtain Frame Health command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DCURTAINFRAMEHEALTH routes through transactional Model Health publication while preserving frame/panel aggregation, locate and redacted errors.")
