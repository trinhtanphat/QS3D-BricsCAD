#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectSidecarRevisionStamp.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")

capture_start = text.find("public static ProjectSidecarRevisionStamp Capture(string primaryPath)")
if capture_start < 0:
    fail("sidecar revision Capture entrypoint is missing")
capture_end = text.find("public bool IsForPath", capture_start)
if capture_end < 0:
    fail("sidecar revision Capture boundary could not be isolated")
capture = text[capture_start:capture_end]

required = (
    "var fullPath = Path.GetFullPath(primaryPath);",
    'var backupPath = fullPath + ".bak";',
    'PersistencePathSafety.RequireNonRedirected(fullPath, "sidecar revision primary read");',
    'PersistencePathSafety.RequireNonRedirected(backupPath, "sidecar revision backup read");',
    "FileCapture.Open(fullPath)",
    "FileCapture.Open(backupPath)",
)
for marker in required:
    if marker not in capture:
        fail(f"sidecar revision capture is missing path-safety marker: {marker}")

primary_guard = capture.index('PersistencePathSafety.RequireNonRedirected(fullPath, "sidecar revision primary read");')
backup_guard = capture.index('PersistencePathSafety.RequireNonRedirected(backupPath, "sidecar revision backup read");')
primary_open = capture.index("FileCapture.Open(fullPath)")
backup_open = capture.index("FileCapture.Open(backupPath)")
if primary_guard > primary_open or backup_guard > backup_open:
    fail("sidecar revision path safety must run before either pair member is opened")

open_start = text.find("public static FileCapture Open(string path)")
open_end = text.find("public FileRevision CaptureStableRevision()", open_start)
if open_start < 0 or open_end < 0:
    fail("sidecar revision member-open boundary could not be isolated")
open_body = text[open_start:open_end]
member_guard = 'PersistencePathSafety.RequireNonRedirected(path, "sidecar revision member read");'
if open_body.count(member_guard) < 2:
    fail("sidecar revision member open must recheck the complete path graph before and after opening")
if open_body.index(member_guard) > open_body.index("File.GetAttributes(path)"):
    fail("sidecar revision member path safety must precede first pathname attribute observation")
if open_body.rindex(member_guard) > open_body.index("return new FileCapture(path, stream);"):
    fail("sidecar revision post-open path recheck must complete before the stream gains revision authority")

presence_start = text.find("public void EnsurePresenceUnchanged()")
presence_end = text.find("public void Dispose()", presence_start)
if presence_start < 0 or presence_end < 0:
    fail("sidecar revision final presence fence could not be isolated")
presence = text[presence_start:presence_end]
if member_guard not in presence:
    fail("sidecar revision final pair-presence fence must recheck redirected ancestors")

if "RequireRegularSidecar(File.GetAttributes(path));" not in text:
    fail("sidecar revision capture lost its final-member regular-file/reparse check")
if "MaxSidecarBytes" not in text or "CaptureStableRevision" not in text or "EnsurePresenceUnchanged" not in text:
    fail("sidecar revision capture lost bounded stable pair-revision safeguards")

print("PASS: sidecar revision capture repeatedly rejects redirected ancestor paths for both pair members before digest authority")
