#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SafeGeneratedHandleOwnershipHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        '[CommandMethod("QS3DOWNERSHIPHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'new SafeGeneratedHandleOwnershipHealthService().Inspect(project)',
        'ModelHealthWindowPresenter.Show(document, issues, issue =>',
        'CadHandleService.Select(document, SemanticReferenceHandles.Get(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'Report(document, "QS3DOWNERSHIPHEALTH lỗi: không thể hoàn tất health check.");',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\nQS3D " + message)',
    )
    for token in required:
        if token not in text:
            errors.append("Ownership Health command contract missing token: " + token)

    forbidden = (
        'Application.ShowModelessWindow(',
        'new ModelHealthWindow(',
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DOWNERSHIPHEALTH lỗi: " +',
    )
    for token in forbidden:
        if token in text:
            errors.append("Ownership Health command must not bypass presenter or reflect exception detail: " + token)

    if text.count('PaletteCoordinator.SetStatus(message)') != 1:
        errors.append("Ownership Health command must keep exactly one Palette status sink in Report().")
    if text.count('document.Editor.WriteMessage("\\nQS3D " + message)') != 1:
        errors.append("Ownership Health command must keep exactly one Editor sink in Report().")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DOWNERSHIPHEALTH routes through transactional Model Health publication while preserving read-only locate and redacted error semantics.")
