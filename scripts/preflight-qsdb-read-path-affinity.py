#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
load_start = source.index("private static XDocument LoadDocument(string path)")
load_end = source.index("private static void ValidateSerializedFile", load_start)
load = source[load_start:load_end]

open_token = "using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))"
bind_token = 'PersistencePathSafety.RequireExclusiveOpenStillBound(stream, fullPath, "project read")'
length_token = "if (stream.Length > MaxProjectFileBytes)"
reader_token = "XmlReader.Create(stream, settings)"

for token in (open_token, bind_token, length_token, reader_token):
    if token not in load:
        fail(f"QSDB read path-affinity contract must retain token: {token}")

open_index = load.index(open_token)
bind_index = load.index(bind_token)
length_index = load.index(length_token)
reader_index = load.index(reader_token)
if not (open_index < bind_index < length_index < reader_index):
    fail("QSDB held read stream must be bound to the canonical pathname generation before length inspection or XML parsing")

print("PASS: QSDB read stream is bound to the admitted canonical pathname generation before parsing")
