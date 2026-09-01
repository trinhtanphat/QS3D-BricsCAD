#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RebarHealthAllCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DREBARHEALTHALL", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new GeneratedRebarHealthService().InspectAll(project, liveColumn, liveShape)',
        'new GeneratedTieRebarHealthService().Inspect(project, liveTie)',
        'new GeneratedBeamStirrupHealthService().Inspect(project, liveStirrup)',
        'new GeneratedSlabMeshHealthService().Inspect(project, liveSlabMesh)',
        'new GeneratedWallMeshHealthService().Inspect(project, liveWallMesh)',
        'new GeneratedFoundationMeshHealthService().Inspect(project, liveFoundationMesh)',
        'new GeneratedRebarOwnershipHealthService().Inspect(project)',
        'new RebarFabricationQualificationHealthService().Inspect(project)',
        'BbsNativeTableBuilder.Inspect(document, project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'HandlesForIssue(element, issue.Code)',
        'GeneratedHandleOwnershipPolicy.RebarHandleKeys',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DREBARHEALTHALL lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Rebar Health All command contract missing token: " + token)

    for token in ('Application.ShowModelessWindow(', 'new ModelHealthWindow(', 'catch (System.Exception ex)', 'ex.Message', 'QS3DREBARHEALTHALL lỗi: " +'):
        if token in text:
            errors.append("Rebar Health All command must not bypass presenter or reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREBARHEALTHALL routes through transactional Model Health publication while preserving all-rebar aggregation, locate and error redaction.")
