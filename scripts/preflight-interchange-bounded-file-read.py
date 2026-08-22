#!/usr/bin/env python3
from pathlib import Path

SOURCE = Path("src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs")
text = SOURCE.read_text(encoding="utf-8")

required = (
    "var bytes = ReadFileBytesBounded(fullPath);",
    "private static byte[] ReadFileBytesBounded(string fullPath)",
    "while (total < MaxFileBytes)",
    "if (stream.ReadByte() != -1)",
    'throw new InvalidDataException("Semantic snapshot exceeds the guarded " + MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");',
)
for needle in required:
    if needle not in text:
        raise SystemExit("FAIL: missing bounded-read contract: " + needle)

if "File.ReadAllBytes(fullPath)" in text:
    raise SystemExit("FAIL: ValidateFile still contains an unbounded File.ReadAllBytes path")

print("PASS: ProjectInterchangeJsonValidator enforces MaxFileBytes while reading")
