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
        "InterchangeConfirmationGuard.RequireFresh(",
        "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)",
        "ProjectInterchangeKeepTargetImporter.Plan(project, json)",
        "ProjectInterchangeKeepTargetImporter.Import(project, json)",
        "InterchangeUseSourceElementImportService.Plan(project, json)",
        "InterchangeUseSourceElementImportService.Import(document, json)",
        "InterchangeUseSourceCatalogImportService.Plan(project, json)",
        "InterchangeUseSourceCatalogImportService.Import(document, json)",
        "InterchangeUseSourceAllImportService.Plan(project, json)",
        "InterchangeUseSourceAllImportService.Import(document, json)",
        "CollisionPolicyChoice.UseSourceElement",
        "CollisionPolicyChoice.UseSourceCatalog",
        "CollisionPolicyChoice.UseSourceAll",
        "YES — REPLACE ALL SEMANTIC (ATOMIC)",
        "MỘT ProjectStateSnapshot và MỘT native CAD transaction",
        "ALL không sequentially chạy hai importer partial",
        "NO — chọn PARTIAL scope",
        "YES — REPLACE ELEMENT SEMANTIC",
        "NO — REPLACE CATALOG SEMANTIC",
        "selector chỉ chạy đúng một path được chọn và không sequence hai partial importer",
        "System.Windows.MessageBoxButton.YesNoCancel",
        "System.Windows.MessageBoxResult.Cancel",
        "System.Windows.MessageBoxResult.No",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "EnsureActive(document",
        "Incoming source CAD handles không trở thành target ownership",
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
            errors.append(f"selector must delegate exactly one mutation policy instead of duplicating lower-layer behavior: {needle}")

    switch_match = re.search(r"switch \(choice\.Value\)(.*?)default:", c, re.S)
    if not switch_match:
        errors.append("generic selector switch dispatch not found")
    else:
        switch_body = switch_match.group(1)
        for case_name, runner in [
            ("KeepTarget", "RunKeepTarget"),
            ("UseSourceElement", "RunUseSourceElement"),
            ("UseSourceCatalog", "RunUseSourceCatalog"),
            ("UseSourceAll", "RunUseSourceAll"),
        ]:
            pattern = rf"case CollisionPolicyChoice\.{case_name}:\s*{runner}\([^;]+;\s*return;"
            if not re.search(pattern, switch_body, re.S):
                errors.append(f"selector must dispatch {case_name} to exactly its dedicated runner")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEIMPORT"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEIMPORT command registration count must be 1, got {registrations}")

    for tag, label in [
        ('QS3DINTERCHANGEIMPORT', "generic import selector"),
        ('QS3DINTERCHANGEAPPEND', "dedicated append-only command"),
        ('QS3DINTERCHANGEUSESOURCEALL', "dedicated atomic UseSource ALL command"),
        ('QS3DINTERCHANGEUSESOURCE', "dedicated UseSource Element command"),
        ('QS3DINTERCHANGEUSESOURCECATALOG', "dedicated UseSource Catalog command"),
    ]:
        if ui.count(f'Tag="{tag}"') != 1:
            errors.append(f"Project Tools must expose {label} exactly once")

    for needle in [
        "Nạp Snapshot (Chọn policy)",
        "Append-only khi không collision",
        "KeepTarget, Replace ALL semantic, Replace Element semantic hoặc Replace Catalog semantic",
        "Nạp Snapshot (Replace ALL semantic)",
        "một CAD transaction",
        "Nạp Snapshot (Replace Catalog semantic)",
    ]:
        if needle not in ui:
            errors.append(f"Project Tools missing all-scope/catalog policy UX: {needle}")

if errors:
    print("preflight-interchange-import-selector: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-import-selector: PASS")
print("Generic import routes to one policy only; append-only revalidates project freshness before mutation and all lower-layer mutation boundaries remain delegated.")
