#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeExportSafetySmoke.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs"
errors = []

for path in (SOURCE, SMOKE, COMMAND):
    if not path.is_file():
        errors.append("missing interchange export safety contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "stream.Flush(true);",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
        "if (value.Kind != DateTimeKind.Utc)",
    ):
        if token not in text:
            errors.append("ProjectInterchangeJsonExporter.cs missing safety token: " + token)
    if "File.Copy(tempPath, fullPath, true)" in text or "File.Copy(fullPath, backupPath, true)" in text:
        errors.append("Interchange exporter must not maintain a second overwrite-copy atomic fallback.")
    if "value.ToUniversalTime()" in text:
        errors.append("Interchange exporter must reject non-UTC timestamps instead of interpreting them with machine timezone.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsNonUtcBuild();",
        "FailedExportPreservesExistingDestination();",
        "SuccessfulExportReplacesDestination();",
        'Equal("old-good", File.ReadAllText(path)',
    ):
        if token not in text:
            errors.append("ProjectInterchangeExportSafetySmoke.cs missing regression token: " + token)

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    export_start = text.find('[CommandMethod("QS3DINTERCHANGEJSON"')
    append_start = text.find('[CommandMethod("QS3DINTERCHANGEAPPEND"', export_start + 1)
    ensure_start = text.find("private static void EnsureActive", append_start + 1)
    if export_start < 0 or append_start < 0 or ensure_start < 0:
        errors.append("ProjectInterchangeCommands.cs missing command boundaries")
    else:
        export_body = text[export_start:append_start]
        append_body = text[append_start:ensure_start]

        export_tokens = (
            "if (dialog.ShowDialog() != true) return;",
            "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
            "ProjectStateSnapshot.CreateDetachedCopy(project)",
            "RegenerateDirty(snapshot)",
            "ProjectInterchangeJsonExporter.Export(dialog.FileName, snapshot);",
            "FinalizeUi(\n                    document,",
        )
        positions = []
        for token in export_tokens:
            pos = export_body.find(token)
            if pos < 0:
                errors.append("Interchange JSON command missing read-only/export token: " + token)
            positions.append(pos)
        if positions and min(positions) >= 0 and positions != sorted(positions):
            errors.append("Interchange JSON must confirm destination, resolve an existing project read-only, regenerate detached state, commit JSON, then finalize UI")
        if "ProjectContextCoordinator.GetOrCreate(document)" in export_body:
            errors.append("Interchange JSON export must not create/cache a project; use TryGetReadOnly")
        if "RegenerateDirty(project)" in export_body:
            errors.append("Interchange JSON export must never regenerate the live project")

        write = export_body.find("ProjectInterchangeJsonExporter.Export(dialog.FileName, snapshot);")
        finalize = export_body.find("FinalizeUi(", write + 1)
        if write >= 0 and finalize >= 0:
            between = export_body[write + 1:finalize]
            if "PaletteCoordinator." in between or "Editor.WriteMessage" in between:
                errors.append("Interchange JSON must not perform fallible UI work after persistent export and before FinalizeUi")

        import_at = append_body.find("ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)")
        final_at = append_body.find("FinalizeUi(", import_at + 1)
        if import_at < 0 or final_at < 0 or import_at >= final_at:
            errors.append("Interchange Append must finalize UI only after semantic import succeeds")
        elif "Editor.WriteMessage" in append_body[import_at:final_at]:
            errors.append("Interchange Append must not perform uncaught Editor reporting between committed semantic import and FinalizeUi")

        for token in (
            "private static void FinalizeUi(Document document, string status, string detail)",
            "private static void ReportUi(Document document, string status)",
            "try { PaletteCoordinator.SetStatus(status); } catch { }",
            'try { document.Editor.WriteMessage("\\nQS3D " + status); } catch { }',
        ):
            if token not in text:
                errors.append("Interchange command UI isolation missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange JSON export confirms destination before read-only project lookup, regenerates only detached state, atomically publishes output, and keeps post-export/post-import UI best-effort without changing persistent success semantics.")
