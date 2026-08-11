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


def method_body(text: str, signature: str, next_signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature)) if next_signature else len(text)
    if end < 0:
        end = len(text)
    return text[start:end]


def require_order(label: str, body: str, *tokens: str) -> None:
    positions = []
    for token in tokens:
        pos = body.find(token)
        if pos < 0:
            errors.append(label + " missing token: " + token)
        positions.append(pos)
    if positions and min(positions) >= 0 and positions != sorted(positions):
        errors.append(label + " ordering is unsafe: " + " -> ".join(tokens))


if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")

    command_tokens = (
        '[CommandMethod("QS3DRULEPREVIEW", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEW", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEWSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DIMPACTPREVIEW", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DRULEPREVIEWEXPORT", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEWEXPORT", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEWEXPORTSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DDIAGSUMMARY", CommandFlags.Modal)]',
        "ProjectContextCoordinator.TryGetReadOnly(document, out project)",
        "TryGetReadOnlyProject(Document document, string operation, out ProjectState project)",
        "chưa có QS3D project hiện hữu; chưa tạo project mới",
        "FinalizeExportUi(Document document, string status)",
    )
    for token in command_tokens:
        if token not in text:
            errors.append("RulePreviewAndDiagnosticCommands missing read-only/export token: " + token)

    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("Preview/diagnostic commands must not create/cache a project; use TryGetReadOnly only")

    for forbidden in (
        ".ApplyProject(",
        ".ApplyElement(",
        "RegenerateDirty(",
        "project.Touch()",
    ):
        if forbidden in text:
            errors.append("Preview/diagnostic adapter commands must remain read-only with respect to live semantic/native project state: " + forbidden)

    rule = method_body(text, "public void PreviewQuantityRules()", "public void PreviewRegeneration()")
    require_order(
        "Rule Preview",
        rule,
        'TryGetReadOnlyProject(document, "Rule Preview", out var project)',
        "new QuantityRulePreviewService().PreviewProject(project)",
    )

    regen = method_body(text, "public void PreviewRegeneration()", "public void PreviewSelectedRegeneration()")
    require_order(
        "Regen Preview",
        regen,
        'TryGetReadOnlyProject(document, "Regen Preview", out var project)',
        "new RegenerationPreviewService().Preview(project)",
    )

    regen_sel = method_body(text, "public void PreviewSelectedRegeneration()", "public void PreviewDependencyImpact()")
    require_order(
        "Regen Selection Preview",
        regen_sel,
        'TryGetReadOnlyProject(document, "Regen Preview Selection", out var project)',
        "ResolveSelectedSemanticIds(document, project)",
        "new RegenerationPreviewService().PreviewSubset(project, elementIds)",
    )

    impact = method_body(text, "public void PreviewDependencyImpact()", "public void ExportQuantityRuleReview()")
    require_order(
        "Dependency Impact Preview",
        impact,
        'TryGetReadOnlyProject(document, "Dependency Impact", out var project)',
        "ResolveSelectedSemanticIds(document, project)",
        "new DependencyImpactPlanner().Plan(project, elementIds)",
    )

    rule_export = method_body(text, "public void ExportQuantityRuleReview()", "public void ExportRegenerationReview()")
    require_order(
        "Rule Preview Export",
        rule_export,
        "if (dialog.ShowDialog() != true) return;",
        'TryGetReadOnlyProject(document, "Rule Preview Export", out var project)',
        "new QuantityRulePreviewService().PreviewProject(project)",
        "new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName)",
        "ReportReviewExport(document, snapshot, dialog.FileName)",
    )

    regen_export = method_body(text, "public void ExportRegenerationReview()", "public void ExportSelectedRegenerationReview()")
    require_order(
        "Regen Preview Export",
        regen_export,
        "if (dialog.ShowDialog() != true) return;",
        'TryGetReadOnlyProject(document, "Regen Preview Export", out var project)',
        "new RegenerationPreviewService().Preview(project)",
        "new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName)",
        "ReportReviewExport(document, snapshot, dialog.FileName)",
    )

    regen_sel_export = method_body(text, "public void ExportSelectedRegenerationReview()", "public void ExportDiagnosticSummary()")
    require_order(
        "Regen Selection Preview Export",
        regen_sel_export,
        "if (dialog.ShowDialog() != true) return;",
        'TryGetReadOnlyProject(document, "Regen Selection Review Export", out var project)',
        "ResolveSelectedSemanticIds(document, project)",
        "new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName)",
        "ReportReviewExport(document, snapshot, dialog.FileName)",
    )

    diagnostic = method_body(text, "public void ExportDiagnosticSummary()", "private static bool TryGetReadOnlyProject")
    require_order(
        "Diagnostic Summary",
        diagnostic,
        "if (dialog.ShowDialog() != true) return;",
        'TryGetReadOnlyProject(document, "Diagnostic Summary", out var project)',
        "new ComprehensiveModelHealthService().Inspect(project)",
        "ProjectDiagnosticSummaryExporter.Export(dialog.FileName, project, issues)",
        "FinalizeExportUi(document,",
    )
    dialog_at = diagnostic.find("if (dialog.ShowDialog() != true) return;")
    if dialog_at >= 0:
        before_dialog = diagnostic[:dialog_at]
        for forbidden in (
            "TryGetReadOnlyProject(",
            "ProjectContextCoordinator.",
            "ComprehensiveModelHealthService",
            "project.",
        ):
            if forbidden in before_dialog:
                errors.append("Diagnostic Summary Cancel path must not access/scan project before save confirmation: " + forbidden)

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

print("PASS: preview/diagnostic commands use read-only project lookup, review exports confirm destination before project access, previews execute on detached Core state, and Diagnostic Summary confirms destination before health scanning and atomically publishes aggregate output.")
