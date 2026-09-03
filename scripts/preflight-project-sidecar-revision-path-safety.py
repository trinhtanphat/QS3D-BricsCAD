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

if "RequireRegularSidecar(File.GetAttributes(path));" not in text:
    fail("sidecar revision capture lost its final-member regular-file/reparse check")
if "MaxSidecarBytes" not in text or "CaptureStableRevision" not in text or "EnsurePresenceUnchanged" not in text:
    fail("sidecar revision capture lost bounded stable pair-revision safeguards")

print("PASS: sidecar revision capture rejects redirected ancestor paths for both pair members before digest authority")
