#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeImportCommands.cs"
project_tools = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"

errors = []
for path in (command, project_tools):
    if not path.exists():
        errors.append(f"missing required source: {path.relative_to(root)}")

if not errors:
    c = command.read_text(encoding="utf-8")
    ui = project_tools.read_text(encoding="utf-8")

    required = [
        '[CommandMethod("QS3DINTERCHANGEIMPORT", CommandFlags.Modal)]',
        "ProjectInterchangeImportPreview.Plan(project, json)",
        "if (preview.CollisionCount == 0)",
        "ProjectInterchangeAppendOnlyImporter.Plan(project, json)",
        "ProjectInterchangeAppendOnlyImporter.Import(project, json)",
        "ProjectInterchangeKeepTargetImporter.Plan(project, json)",
        "ProjectInterchangeKeepTargetImporter.Import(project, json)",
        "InterchangeUseSourceElementImportService.Plan(project, json)",
        "InterchangeUseSourceElementImportService.Import(document, json)",
        "System.Windows.MessageBoxButton.YesNoCancel",
        "System.Windows.MessageBoxResult.Cancel",
        "System.Windows.MessageBoxResult.No",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "EnsureActive(document",
        "không nhận CAD ownership từ source",
        "rebuild explicit",
    ]
    for needle in required:
        if needle not in c:
            errors.append(f"generic import selector missing contract: {needle}")

    if c.index("ProjectInterchangeImportPreview.Plan(project, json)") > c.index("if (preview.CollisionCount == 0)"):
        errors.append("import preview must run before append-vs-collision policy routing")

    forbidden = [
        "GeneratedDependentGeometryInvalidator.Prepare",
        "target.Properties.Clear()",
        "SourceHandles.Clear()",
        "QS3DBUILD3D",
        "transaction.Commit()",
    ]
    for needle in forbidden:
        if needle in c:
            errors.append(f"selector must delegate mutation instead of duplicating lower-layer behavior: {needle}")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEIMPORT"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEIMPORT command registration count must be 1, got {registrations}")

    # Project Tools is the user-facing discoverability surface. Keep specialist commands visible
    # so runtime qualification can exercise each policy path independently.
    if ui.count('Tag="QS3DINTERCHANGEIMPORT"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEIMPORT exactly once")
    if ui.count('Tag="QS3DINTERCHANGEAPPEND"') != 1:
        errors.append("Project Tools must preserve the dedicated append-only command")
    if ui.count('Tag="QS3DINTERCHANGEUSESOURCE"') != 1:
        errors.append("Project Tools must preserve the dedicated UseSource Element command")
    for needle in [
        "Nạp Snapshot (Chọn policy)",
        "Append-only khi không collision",
        "KeepTarget hoặc Replace Element semantic",
    ]:
        if needle not in ui:
            errors.append(f"Project Tools missing generic policy UX: {needle}")

if errors:
    print("preflight-interchange-import-selector: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-import-selector: PASS")
print("Generic import command routes explicitly to Append-only, KeepTarget, or CAD-safe Element UseSource without duplicating mutation logic.")
