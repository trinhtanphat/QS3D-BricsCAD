#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ShapeRebarHealthCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ShapeRebarHealthCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DREBARSHAPEHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'project.Elements.SelectMany(ParseShapeHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()',
        'CadHandleService.GetLiveSolidHandles(document, handles)',
        'new GeneratedRebarHealthService().InspectShape(project, live)',
        'Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(document, issues, issue =>',
        'CadHandleService.Select(document, ParseShapeHandles(element))',
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
        'var message = "QS3DREBARSHAPEHEALTH lỗi: không thể hoàn tất health check.";',
        'PaletteCoordinator.SetStatus(message)',
        'document.Editor.WriteMessage("\\n" + message)',
        'GeneratedShapeRebarHandles',
    )
    for token in required:
        if token not in text:
            errors.append("Shape Rebar Health command contract missing token: " + token)

    for token in ('catch (System.Exception ex)', 'ex.Message', 'QS3DREBARSHAPEHEALTH lỗi: " +'):
        if token in text:
            errors.append("Shape Rebar Health command must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREBARSHAPEHEALTH preserves shape-rebar live/modeless locate behavior while top-level exception details remain redacted.")
