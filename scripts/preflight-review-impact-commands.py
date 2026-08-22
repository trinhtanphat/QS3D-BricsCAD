#!/usr/bin/env python3
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/RulePreviewAndDiagnosticCommands.cs"
SELECTION = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs"
CORE_IMPACT = ROOT / "src/QS3D.Core/Services/DependencyImpactPlanner.cs"
CORE_REVIEW = ROOT / "src/QS3D.Core/Review/PreviewReviewSnapshot.cs"
PREFLIGHTS = [
    ROOT / "scripts/preflight-dependency-impact-plan.py",
    ROOT / "scripts/preflight-preview-review-snapshot.py",
]
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


def method_body(text, signature, next_signature):
    start = text.find(signature)
    end = text.find(next_signature, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        return ""
    return text[start:end]


for path in (COMMANDS, SELECTION, CORE_IMPACT, CORE_REVIEW):
    if not path.is_file():
        errors.append("missing review workflow source: " + str(path.relative_to(ROOT)))

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DIMPACTPREVIEW", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DREGENPREVIEWSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DRULEPREVIEWEXPORT", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEWEXPORT", CommandFlags.Modal)]',
        '[CommandMethod("QS3DREGENPREVIEWEXPORTSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        "SemanticSelectionResolver.ResolveImplied(document, project)",
        "new DependencyImpactPlanner().Plan(project, elementIds)",
        "new RegenerationPreviewService().PreviewSubset(project, elementIds)",
        "new PreviewReviewSnapshotService().Create",
        "new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName)",
        'Filter = "QS3D Preview Review (*.qsreview)|*.qsreview"',
        'DefaultExt = ".qsreview"',
        "OverwritePrompt = true",
        "snapshot.Fingerprint.Substring(0, 12)",
    ):
        require(text, token, "review/impact command adapter")

    for forbidden in (
        "ApplyProject(",
        ".Apply(project",
        "RegenerateDirty(",
        "StartTransaction(",
        "ForWrite",
    ):
        if forbidden in text:
            errors.append("review/impact command adapter must remain read-only; found: " + forbidden)

    rule_export = method_body(text, "public void ExportQuantityRuleReview()", "[CommandMethod(\"QS3DREGENPREVIEWEXPORT\"")
    regen_export = method_body(text, "public void ExportRegenerationReview()", "[CommandMethod(\"QS3DREGENPREVIEWEXPORTSEL\"")
    regen_sel_export = method_body(text, "public void ExportSelectedRegenerationReview()", "[CommandMethod(\"QS3DDIAGSUMMARY\"")
    for label, body, preview_token in (
        ("rule review export", rule_export, "new QuantityRulePreviewService().PreviewProject(project)"),
        ("regen review export", regen_export, "new RegenerationPreviewService().Preview(project)"),
        ("selected regen review export", regen_sel_export, "new RegenerationPreviewService().PreviewSubset(project, elementIds)"),
    ):
        if not body:
            errors.append("could not isolate " + label + " method body")
            continue
        confirm = body.find("if (dialog.ShowDialog() != true) return;")
        preview = body.find(preview_token)
        save = body.find("new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName)")
        if min(confirm, preview, save) < 0 or not confirm < preview < save:
            errors.append(label + " must confirm destination before preview creation and persist only after preview snapshot creation")

if SELECTION.is_file():
    text = SELECTION.read_text(encoding="utf-8")
    for token in (
        "SelectImplied()",
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
        "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)",
    ):
        require(text, token, "semantic selection resolver")
    if "OpenMode.ForWrite" in text:
        errors.append("semantic selection resolver must not open PICKFIRST entities ForWrite")

for path in PREFLIGHTS:
    if not path.is_file():
        errors.append("missing Core review preflight: " + str(path.relative_to(ROOT)))
        continue
    completed = subprocess.run([sys.executable, str(path)], cwd=str(ROOT), check=False)
    if completed.returncode != 0:
        errors.append(str(path.relative_to(ROOT)) + " failed with exit=" + str(completed.returncode))

print("QS3D V25 review impact command preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 review commands preserve existing whole-project previews, add semantic-selection impact/subset previews, and export fingerprinted review snapshots without Apply/native mutation.")
