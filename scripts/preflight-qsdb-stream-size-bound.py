#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing QsdbProjectStore source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start_token = "private static XDocument LoadDocument(string path)"
    end_token = "private static void ValidateSerializedFile(string path)"
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate QsdbProjectStore.LoadDocument")
    else:
        block = text[start:end]
        open_token = "new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)"
        size_token = "if (stream.Length > MaxProjectFileBytes)"
        reader_token = "XmlReader.Create(stream, settings)"
        required = (
            open_token,
            size_token,
            reader_token,
            'throw new InvalidDataException("QSDB project exceeds the maximum supported file size of 64 MiB.");',
            "DtdProcessing = DtdProcessing.Prohibit",
            "XmlResolver = null",
            "MaxCharactersInDocument = MaxProjectFileBytes",
        )
        for token in required:
            if token not in block:
                errors.append("missing QSDB stream-size contract token: " + token)

        positions = [block.find(open_token), block.find(size_token), block.find(reader_token)]
        if all(position >= 0 for position in positions) and positions != sorted(positions):
            errors.append("QSDB byte-size guard must run after opening and before parsing the same stream")
        if "new FileInfo(" in block or ".Length > MaxProjectFileBytes" in block.replace("stream.Length > MaxProjectFileBytes", ""):
            errors.append("LoadDocument contains a path/file-metadata size guard instead of the parsed-stream guard")

print("QS3D QSDB parsed-stream size-bound preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QsdbProjectStore enforces the 64 MiB byte bound on the exact stream before XmlReader parses it.")
