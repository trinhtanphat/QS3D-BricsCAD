#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbRedirectedReadPathSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB read-path integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'PersistencePathSafety.RequireNonRedirected(fullPath, "project read");',
        'PersistencePathSafety.RequireNonRedirected(backupPath, "project backup read");',
        'using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))',
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing read-path safety token: " + token)

    primary_guard = text.find('PersistencePathSafety.RequireNonRedirected(fullPath, "project read");', text.find("public ProjectLoadResult LoadWithBackupFallback"))
    fallback_try = text.find("try", text.find("public ProjectLoadResult LoadWithBackupFallback"))
    if primary_guard < 0 or fallback_try < 0 or primary_guard > fallback_try:
        errors.append("LoadWithBackupFallback must reject a redirected primary before entering recoverable-data fallback.")

    backup_guard = text.find('PersistencePathSafety.RequireNonRedirected(backupPath, "project backup read");', text.find("catch (Exception primary)"))
    backup_exists = text.find("if (!File.Exists(backupPath)) throw;", text.find("catch (Exception primary)"))
    if backup_guard < 0 or backup_exists < 0 or backup_guard > backup_exists:
        errors.append("Backup redirect validation must run before backup existence/read fallback.")

    load_document = text.find("private static XDocument LoadDocument")
    load_guard = text.find('PersistencePathSafety.RequireNonRedirected(fullPath, "project read");', load_document)
    open_stream = text.find("new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)", load_document)
    second_guard = text.find('PersistencePathSafety.RequireNonRedirected(fullPath, "project read");', load_guard + 1)
    if min(load_document, load_guard, open_stream, second_guard) < 0 or not (load_document < load_guard < open_stream < second_guard):
        errors.append("LoadDocument must validate path identity both before and immediately after opening the read handle.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RegularProjectFileRemainsReadable();",
        "RedirectedPrimaryIsRejectedWithoutBackupDowngrade();",
        "RedirectedBackupIsRejectedBeforeFallbackRead();",
        "RedirectedParentDirectoryIsRejected();",
        "File.CreateSymbolicLink(linkPath, targetPath);",
        "Directory.CreateSymbolicLink(linkPath, targetPath);",
        "store.LoadWithBackupFallback(redirected)",
        "store.LoadWithBackupFallback(primary)",
    ):
        if token not in text:
            errors.append("QsdbRedirectedReadPathSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB primary and backup reads preserve canonical path authority and reject redirected/reparse paths before fallback or XML consumption.")
