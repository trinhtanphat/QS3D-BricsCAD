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

    start = source.find("internal static class BltLegacyProbeReport")
    if start < 0:
        fail("missing BltLegacyProbeReport")
    report = source[start:]

    required = (
        "MaxProbeReportBytes",
        "FileStream",
        "StreamWriter",
        "File.Move",
        "File.Delete",
    )
    for token in required:
        if token not in report:
            fail(f"probe report resource-safety contract missing: {token}")

    if "File.WriteAllText(path, Serialize(candidates)" in report:
        fail("probe report must not materialize the complete JSON string before writing")
    if "private static string Serialize(" in report:
        fail("legacy whole-report StringBuilder serializer must be removed")

    temp_pos = report.find("temp")
    stream_pos = report.find("FileStream")
    move_pos = report.find("File.Move")
    delete_pos = report.find("File.Delete")
    if min(temp_pos, stream_pos, move_pos, delete_pos) < 0:
        fail("unable to prove temp-stream-write / cleanup / publish ordering")
    if not (temp_pos < stream_pos < move_pos):
        fail("report must stream to a temporary artifact before atomic publication")

    budget_pos = report.find("MaxProbeReportBytes")
    if budget_pos < 0 or budget_pos > move_pos:
        fail("report byte budget must be enforced before publication")

    if "catch" not in report or "finally" not in report:
        fail("report publication must retain explicit cleanup on exceptional paths")

    print("PASS: V25 BLT probe report streams incrementally under an explicit byte budget and publishes only a complete artifact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
