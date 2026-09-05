#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def main() -> int:
    path = ROOT / REL
    if not path.exists():
        fail(f"missing required source: {REL}")

    source = path.read_text(encoding="utf-8")
    start = source.find("private static void PopulateExtensionDictionary")
    end = source.find("private static void PutTypedValues", start)
    if start < 0 or end < 0:
        fail("unable to locate PopulateExtensionDictionary contract")
    block = source[start:end]

    if "record.Data != null" in block:
        fail("Xrecord.Data must not be acquired once for a null probe and again for disposal")

    acquisition = "using (var data = record.Data)"
    if block.count(acquisition) != 1:
        fail("extension dictionary Xrecord.Data must be acquired exactly once into one using scope")

    data_pos = block.find(acquisition)
    null_pos = block.find("if (data != null)", data_pos)
    parse_pos = block.find("PutTypedValues(snapshot, prefix + \".Data\", data.AsArray())", data_pos)
    if min(data_pos, null_pos, parse_pos) < 0 or not (data_pos < null_pos < parse_pos):
        fail("single acquired ResultBuffer must be null-checked and parsed inside its using scope")

    print("PASS: V25 BLT extension dictionary acquires and disposes each Xrecord ResultBuffer exactly once.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
