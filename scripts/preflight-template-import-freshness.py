#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "TemplateCommands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing TemplateCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    start = text.find('CommandMethod("QS3DTEMPLATEIMPORT"')
    end = text.find("private static void FinalizeExportUi", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QS3DTEMPLATEIMPORT region")
    else:
        imp = text[start:end]
        tokens = {
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "change_version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "dialog": "if (dialog.ShowDialog() != true) return;",
            "load": "var profile = store.Load(dialog.FileName);",
            "confirm": "MessageBox.Show(confirmText",
            "active_doc": "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, doc)",
            "bind": 'ExistingProjectMutationContext.Require(doc, "Template Import")',
            "fresh_project": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "snapshot": "ProjectStateSnapshot.Capture(project)",
            "apply": "store.Apply(project, profile)",
            "regen": "RegenerateDirty(project)",
            "restore": "rollback.Restore(project)",
            "rollback_ui": "RefreshProjectBestEffort(doc);",
            "success_ui": "FinalizeImportUi(doc, message);",
        }
        positions = {}
        for name, token in tokens.items():
            positions[name] = imp.find(token)
            if positions[name] < 0:
                errors.append("template import missing freshness token: " + token)

        ordered = (
            "readonly",
            "project_id",
            "change_version",
            "dialog",
            "load",
            "confirm",
            "active_doc",
            "bind",
            "fresh_project",
            "fresh_version",
            "snapshot",
            "apply",
            "regen",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("template import must pin read-only project freshness before dialog/confirmation and revalidate before mutation")

        if positions.get("restore", -1) >= 0 and positions.get("rollback_ui", -1) >= 0:
            if positions["rollback_ui"] <= positions["restore"]:
                errors.append("rollback UI refresh must run only after authoritative ProjectState restore")

        if positions.get("regen", -1) >= 0 and positions.get("success_ui", -1) >= 0:
            if positions["success_ui"] <= positions["regen"]:
                errors.append("successful import UI finalization must remain after semantic regeneration")

        if "ProjectContextCoordinator.GetOrCreate(doc)" in imp:
            errors.append("template import must never bootstrap replacement project state")

    finalizer_start = text.find("private static void FinalizeImportUi")
    refresh_start = text.find("private static void RefreshProjectBestEffort")
    guard_start = text.find("private static void Guard")
    if min(finalizer_start, refresh_start, guard_start) < 0:
        errors.append("missing template import UI/rollback helpers")
    else:
        finalizer = text[finalizer_start:refresh_start]
        refresh = text[refresh_start:guard_start]
        guard = text[guard_start:]
        if "try { PaletteCoordinator.RefreshProject(); }" not in finalizer or "catch (System.Exception)" not in finalizer:
            errors.append("successful template import UI refresh must remain best-effort and redact caught host detail")
        if "TryWrite(document, \"\\n\" + ImportUiWarning);" not in finalizer:
            errors.append("successful template import UI warning must use the stable redacted warning")
        if "try { PaletteCoordinator.RefreshProject(); }" not in refresh or "catch (System.Exception)" not in refresh:
            errors.append("rollback palette refresh must remain best-effort and redact caught host detail")
        if "TryWrite(document, \"\\n\" + RollbackUiWarning);" not in refresh:
            errors.append("rollback palette refresh warning must use the stable redacted warning")
        if 'var message = operation + " lỗi; thao tác không hoàn tất. Xem log chẩn đoán nếu cần chi tiết kỹ thuật.";' not in guard:
            errors.append("outer template guard must report a stable operation-specific failure")
        if 'TryWrite(document, "\\n" + message);' not in guard:
            errors.append("outer template guard editor reporting must stay best-effort")
        if "try { PaletteCoordinator.SetStatus(message); } catch { }" not in guard:
            errors.append("outer template guard status reporting must stay best-effort")

        forbidden = ("ex.Message", "warning.Message", "importError.Message", "restoreError.Message")
        for token in forbidden:
            if token in finalizer or token in refresh or token in guard:
                errors.append("template UI/guard failure surfaces must not expose raw caught exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: template import pins existing-project freshness across review/confirmation and preserves rollback/committed UI reporting with host-detail redaction.")
