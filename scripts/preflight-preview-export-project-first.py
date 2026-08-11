#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "RulePreviewAndDiagnosticCommands.cs"

METHODS = {
    "public void ExportQuantityRuleReview()": (
        "if (!TryGetReadOnlyProject(document, \"Rule Preview Export\", out var project)) return;",
        "var dialog = CreateReviewDialog(document, \"rule-review\");",
    ),
    "public void ExportRegenerationReview()": (
        "if (!TryGetReadOnlyProject(document, \"Regen Preview Export\", out var project)) return;",
        "var dialog = CreateReviewDialog(document, \"regen-review\");",
    ),
    "public void ExportSelectedRegenerationReview()": (
        "if (!TryGetReadOnlyProject(document, \"Regen Selection Review Export\", out var project)) return;",
        "var dialog = CreateReviewDialog(document, \"regen-selection-review\");",
    ),
    "public void ExportDiagnosticSummary()": (
        "if (!TryGetReadOnlyProject(document, \"Diagnostic Summary\", out var project)) return;",
        "var dialog = new SaveFileDialog",
    ),
}

errors = []

if not PATH.is_file():
    errors.append("missing RulePreviewAndDiagnosticCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    markers = list(METHODS.keys()) + ["private static bool TryGetReadOnlyProject"]
    for index, marker in enumerate(markers[:-1]):
        start = text.find(marker)
        end = text.find(markers[index + 1], start + len(marker))
        if start < 0 or end < 0:
            errors.append("cannot isolate export method: " + marker)
            continue
        body = text[start:end]
        project_guard, dialog_token = METHODS[marker]
        guard_at = body.find(project_guard)
        dialog_at = body.find(dialog_token)
        if guard_at < 0:
            errors.append(marker + " missing read-only project guard.")
        if dialog_at < 0:
            errors.append(marker + " missing expected save dialog.")
        if guard_at >= 0 and dialog_at >= 0 and guard_at > dialog_at:
            errors.append(marker + " must validate project state before opening a save dialog.")

    selected_start = text.find("public void ExportSelectedRegenerationReview()")
    selected_end = text.find("public void ExportDiagnosticSummary()", selected_start)
    if selected_start >= 0 and selected_end >= 0:
        selected = text[selected_start:selected_end]
        selection_guard = selected.find("if (elementIds.Count == 0)")
        dialog = selected.find("var dialog = CreateReviewDialog(document, \"regen-selection-review\");")
        if selection_guard < 0 or dialog < 0 or selection_guard > dialog:
            errors.append("selected regeneration export must validate semantic selection before opening a save dialog.")

    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("preview/diagnostic inspection and export commands must remain read-only.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: preview/diagnostic exports validate project and selection state before opening save dialogs, without creating project state.")
