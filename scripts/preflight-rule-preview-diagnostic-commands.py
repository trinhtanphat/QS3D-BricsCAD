#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RulePreviewAndDiagnosticCommands.cs"
RULES = ROOT / "src/QS3D.Core/Rules/QuantityRulePreviewService.cs"
REGEN = ROOT / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
DIAG = ROOT / "src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs"
errors = []

for path in (SOURCE, RULES, REGEN, DIAG):
    if not path.is_file():
        errors.append("missing preview/diagnostic command contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DRULEPREVIEW", CommandFlags.Modal)]',
        "new QuantityRulePreviewService().PreviewProject(project)",
        '[CommandMethod("QS3DREGENPREVIEW", CommandFlags.Modal)]',
        "new RegenerationPreviewService().Preview(project)",
        "preview.HealthDiff.NewErrorCount",
        "Không mutate project",
        '[CommandMethod("QS3DDIAGSUMMARY", CommandFlags.Modal)]',
        "new ComprehensiveModelHealthService().Inspect(project)",
        "ProjectDiagnosticSummaryExporter.Export(dialog.FileName, project, issues)",
        "privacy-safe",
        "if (dialog.ShowDialog() != true) return;",
        "FinalizeExportUi(document, status);",
    ):
        if token not in text:
            errors.append("RulePreviewAndDiagnosticCommands missing read-only/export token: " + token)

    for forbidden in (
        ".ApplyProject(",
        ".ApplyElement(",
        "RegenerateDirty(",
        "project.Touch()",
    ):
        if forbidden in text:
            errors.append("Preview/diagnostic adapter commands must remain read-only with respect to live semantic/native project state: " + forbidden)

if RULES.is_file() and "ProjectStateSnapshot.CreateDetachedCopy(project)" not in RULES.read_text(encoding="utf-8"):
    errors.append("Quantity rule preview lost detached-state execution.")

if REGEN.is_file():
    regen = REGEN.read_text(encoding="utf-8")
    for token in ("ProjectStateSnapshot.CreateDetachedCopy(project)", "NewEngine().RegenerateDirty(detached)"):
        if token not in regen:
            errors.append("Regeneration preview lost detached-state execution: " + token)

if DIAG.is_file() and "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);" not in DIAG.read_text(encoding="utf-8"):
    errors.append("Diagnostic summary lost atomic publication.")

if errors:
    print("QS3D rule/regeneration preview + diagnostic command preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DRULEPREVIEW and QS3DREGENPREVIEW are detached/read-only dry-runs, while QS3DDIAGSUMMARY atomically exports the privacy-safe aggregate diagnostic contract. V25 runtime qualification remains local.")
