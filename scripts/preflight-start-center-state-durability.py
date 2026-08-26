#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Services/StartCenterUserStateStore.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing StartCenterUserStateStore.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")


def method(name: str, next_name: str) -> str:
    start = source.find(name)
    end = source.find(next_name, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        errors.append("cannot isolate method contract: " + name)
        return ""
    return source[start:end]


for token in (
    "private const int MaxFileBytes = 256 * 1024;",
    "private static bool TryCommit(StartCenterUserStateSnapshot state)",
    "private static bool TrySaveCore(StartCenterUserStateSnapshot state)",
    "private static void WriteDurableTemp(string path, string serialized)",
    "FileOptions.WriteThrough",
    "stream.Flush(true);",
    "private static bool TryReplacePreservingLastKnownGood(string temp, string path, string backup)",
    "private static void TryRestoreBackup(string path, string backup)",
    'private static string BackupPath(string path) => path + ".replace.bak";',
):
    if token not in source:
        errors.append("Start Center durability contract missing: " + token)

commit = method(
    "private static bool TryCommit(StartCenterUserStateSnapshot state)",
    "private static bool TrySaveCore(StartCenterUserStateSnapshot state)",
)
save_pos = commit.find("if (!TrySaveCore(next)) return false;")
publish_pos = commit.find("_current = next;")
if save_pos < 0 or publish_pos < 0 or save_pos >= publish_pos:
    errors.append("in-memory Start Center state must publish only after durable persistence succeeds")

# _current may be initialized at startup and assigned only at the post-save commit boundary.
assignments = source.count("_current =")
if assignments != 2:
    errors.append("unexpected _current publication boundary count: " + str(assignments))

record_project = method(
    "public static bool RecordProject(string path)",
    "public static void ToggleProjectPinned(string path)",
)
if "return TryCommit(next);" not in record_project:
    errors.append("RecordProject must report persistence failure through its existing bool result")

for method_name, next_name in (
    ("public static void ToggleFavorite(string command)", "public static void RecordCommand(string command)"),
    ("public static void RecordCommand(string command)", "public static bool RecordProject(string path)"),
    ("public static void ToggleProjectPinned(string path)", "public static void RemoveProject(string path)"),
    ("public static void RemoveProject(string path)", "public static void ClearProjects()"),
    ("public static void ClearProjects()", "internal static bool TryNormalizeDwgPath"),
):
    body = method(method_name, next_name)
    if "TryCommit(next);" not in body:
        errors.append(method_name + " must fail closed through TryCommit")
    if "_current =" in body:
        errors.append(method_name + " must not publish volatile state directly")

save = method(
    "private static bool TrySaveCore(StartCenterUserStateSnapshot state)",
    "private static void WriteDurableTemp(string path, string serialized)",
)
for token in (
    "if (!TrySettingsPath(out var path)) return false;",
    "if (Encoding.UTF8.GetByteCount(serialized) > MaxFileBytes) return false;",
    "WriteDurableTemp(temp, serialized);",
    "File.Replace(temp, path, backup, true);",
    "if (!TryReplacePreservingLastKnownGood(temp, path, backup)) return false;",
    "catch (IOException) { return false; }",
    "catch (UnauthorizedAccessException) { return false; }",
):
    if token not in save:
        errors.append("save fail-closed contract missing: " + token)

if "File.Copy(temp, path, true)" in source or "File.Delete(path)" in source:
    errors.append("last-known-good settings file must not be overwritten/copied or deleted as replacement fallback")

fallback = method(
    "private static bool TryReplacePreservingLastKnownGood(string temp, string path, string backup)",
    "private static void TryRestoreBackup(string path, string backup)",
)
old_move = fallback.find("File.Move(path, backup);")
new_move = fallback.find("File.Move(temp, path);")
restore = fallback.find("TryRestoreBackup(path, backup);")
if old_move < 0 or new_move < 0 or old_move >= new_move:
    errors.append("fallback replacement must preserve the old file as backup before installing the new file")
if restore < 0 or restore <= new_move:
    errors.append("fallback replacement must restore/recover the last-known-good file after install failure")

load = method(
    "private static StartCenterUserStateSnapshot LoadCore()",
    "private static StartCenterUserStateSnapshot Normalize(StartCenterUserStateSnapshot state)",
)
for token in (
    "if (!File.Exists(loadPath))",
    "var backup = BackupPath(path);",
    "if (!File.Exists(backup)) return state;",
    "loadPath = backup;",
):
    if token not in load:
        errors.append("crash-recovery load contract missing: " + token)

print("QS3D Start Center state durability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Start Center user state is write-before-publish, bounded, durable-temp flushed, last-known-good preserving, crash-recoverable, and fail-closed on persistence errors.")
