#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing src/QS3D.BricsCAD.V25/SupportBundleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private static void PublishSupportBundle")
    end = text.find("private static void FinalizeSupportBundleUi", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate PublishSupportBundle helper")
    else:
        publish = text[start:end]
        tokens = (
            "var fullPath = Path.GetFullPath(path);",
            "var directory = Path.GetDirectoryName(fullPath);",
            "Guid.NewGuid().ToString(\"N\") + \".tmp\"",
            "File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)",
            "new StreamWriter(stream, new System.Text.UTF8Encoding(false))",
            "writer.Flush();",
            "stream.Flush(true);",
            "if (File.Exists(fullPath))",
            "File.Replace(temp, fullPath, null, true);",
            "File.Move(temp, fullPath);",
            "finally",
            "if (File.Exists(temp)) File.Delete(temp);",
        )
        positions = []
        for token in tokens:
            at = publish.find(token)
            positions.append(at)
            if at < 0:
                errors.append("Support Bundle atomic publisher missing token: " + token)
        if all(at >= 0 for at in positions) and positions != sorted(positions):
            errors.append("Support Bundle publisher must write/flush temp before replace-or-move and clean it in finally")

        forbidden = (
            "File.WriteAllLines(fullPath",
            "File.WriteAllText(fullPath",
            "File.Open(fullPath, FileMode.Create",
            "File.Create(fullPath",
        )
        for token in forbidden:
            if token in publish:
                errors.append("Support Bundle publisher must not truncate/open destination before temp publish: " + token)

        create_at = publish.find("File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)")
        flush_at = publish.find("stream.Flush(true);")
        replace_at = publish.find("File.Replace(temp, fullPath, null, true);")
        move_at = publish.find("File.Move(temp, fullPath);")
        cleanup_at = publish.find("if (File.Exists(temp)) File.Delete(temp);")
        if min(create_at, flush_at, replace_at, move_at, cleanup_at) >= 0:
            if not create_at < flush_at < replace_at < move_at < cleanup_at:
                errors.append("Support Bundle atomic publish ordering drifted")

    command_start = text.find('[CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)]')
    helper_start = text.find("private static void PublishSupportBundle", command_start)
    if command_start < 0 or helper_start <= command_start:
        errors.append("cannot isolate QS3DSUPPORTBUNDLE command")
    else:
        command = text[command_start:helper_start]
        dialog_at = command.find("if (dialog.ShowDialog() != true) return;")
        readonly_at = command.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
        publish_at = command.find("PublishSupportBundle(dialog.FileName, lines);")
        finalize_at = command.find("FinalizeSupportBundleUi(document, dialog.FileName);")
        if min(dialog_at, readonly_at, publish_at, finalize_at) < 0 or not dialog_at < readonly_at < publish_at < finalize_at:
            errors.append("Support Bundle command must preserve dialog -> read-only project -> atomic publish -> UI ordering")
        if "File.WriteAllLines(dialog.FileName" in command or "File.WriteAllText(dialog.FileName" in command:
            errors.append("Support Bundle command must not bypass the atomic publisher")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Support Bundle writes a same-directory temp, durably flushes it, atomically replaces/moves the destination, and cleans leftovers without bypassing read-only/privacy ordering.")
