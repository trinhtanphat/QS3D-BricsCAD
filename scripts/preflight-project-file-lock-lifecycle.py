from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectFileLock.cs"
text = SOURCE.read_text(encoding="utf-8")

marker = "public void Dispose()"
start = text.find(marker)
if start < 0:
    raise SystemExit("ProjectFileLock.Dispose lifecycle method is missing")
body = text[start:]

stream_read = "var stream = _stream;"
null_guard = "if (stream == null) return;"
dispose = "stream.Dispose();"
clear = "_stream = null;"
for token in (stream_read, null_guard, dispose, clear):
    if token not in body:
        raise SystemExit(f"ProjectFileLock.Dispose lifecycle token missing: {token}")

# Release ownership must be proven before the wrapper forgets the held stream.
# Clearing first makes a throwing FileStream.Dispose() permanently non-retryable.
if body.index(clear) < body.index(dispose):
    raise SystemExit("ProjectFileLock clears ownership before FileStream.Dispose completes")

# Preserve the acquisition fence: exact exclusive handle/path binding must remain
# ahead of destructive truncation and metadata publication.
acquire = text[text.find("public static ProjectFileLock Acquire"):start]
bind = "PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, \"project-lock\");"
truncate = "stream.SetLength(0);"
if bind not in acquire or truncate not in acquire or acquire.index(bind) > acquire.index(truncate):
    raise SystemExit("ProjectFileLock must bind the exclusive handle before truncation")

print("PASS project file lock lifecycle source guard")
