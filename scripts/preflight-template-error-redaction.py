#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/TemplateCommands.cs"

errors = []
if not SOURCE.is_file():
    errors.append("missing TemplateCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'private const string ExportUiWarning',
        'private const string ImportUiWarning',
        'private const string RollbackUiWarning',
        'catch (System.Exception)',
        'TryWrite(document, "\\n" + ExportUiWarning);',
        'TryWrite(document, "\\n" + ImportUiWarning);',
        'TryWrite(document, "\\n" + RollbackUiWarning);',
        'var warning = false;',
        'if (!warning) return;',
        'var message = operation + " lỗi; thao tác không hoàn tất. Xem log chẩn đoán nếu cần chi tiết kỹ thuật.";',
        'rollback.Restore(project);',
        'RefreshProjectBestEffort(doc);',
        'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, doc)',
        'project.ChangeVersion != expectedChangeVersion',
    )
    for token in required:
        if token not in text:
            errors.append("Template redaction contract missing token: " + token)

    forbidden = (
        "ex.Message",
        "warning.Message",
        "importError.Message",
        "restoreError.Message",
        'PaletteCoordinator.SetStatus(operation + " lỗi: "',
        'operation + " error: "',
    )
    for token in forbidden:
        if token in text:
            errors.append("Template command must not expose raw caught exception detail: " + token)

    rollback_idx = text.find("rollback.Restore(project);")
    refresh_idx = text.find("RefreshProjectBestEffort(doc);")
    rethrow_idx = text.find("throw;", refresh_idx)
    if min(rollback_idx, refresh_idx, rethrow_idx) < 0 or not (rollback_idx < refresh_idx < rethrow_idx):
        errors.append("Template import failure must rollback, best-effort refresh, then rethrow to the redacted command boundary.")

    active_idx = text.find("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, doc)")
    mutate_idx = text.find('ExistingProjectMutationContext.Require(doc, "Template Import")')
    if min(active_idx, mutate_idx) < 0 or active_idx > mutate_idx:
        errors.append("Template import must reject active-DWG drift before acquiring mutable project state.")

if errors:
    for error in errors:
        print("ERROR: " + error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Template import/export failure surfaces stay host-detail-redacted while preserving active-DWG affinity, project-version freshness, rollback-before-report, and best-effort UI recovery.")
