#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"


def between(text: str, start: str, end: str) -> str:
    a = text.find(start)
    if a < 0:
        raise SystemExit(f"missing start marker: {start}")
    b = text.find(end, a + len(start))
    if b < 0:
        raise SystemExit(f"missing end marker: {end}")
    return text[a:b]


def main() -> int:
    runtime = RUNTIME.read_text(encoding="utf-8")
    # BuildStatusJson was split into lane-specific status methods. The inspect
    # implementation still ends immediately before the BricsCAD status builder.
    inspect = between(runtime, "private static string InspectEntity", "private static string BuildBricscadStatusJson")
    describe = between(runtime, "private static string DescribeEntity", "private static Entity OpenEntity")
    snapshot = between(runtime, "private static string BuildDatabaseSnapshotJson", "private static string BuildViewStateJson")

    errors = []
    for token in (
        "DescribeEntity(entity, true, true)",
        "var boundedSolidInspect = extents && details && entity is Solid3d;",
        "if (boundedSolidInspect) builder.Append(\"null\");",
        "else try { builder.Append(ExtentsJson(entity.GeometricExtents)); } catch { builder.Append(\"null\"); }",
        "if (boundedSolidInspect) builder.Append(\",\\\"extentsDeferred\\\":true\");",
    ):
        if token not in (inspect + describe):
            errors.append(f"missing bounded inspect contract: {token}")

    if "DescribeEntity(entity, true, false)" not in snapshot:
        errors.append("database snapshot must retain its extents=true/details=false call shape")

    extents_block = between(describe, "if (extents)", "if (details)")
    if "entity.GeometricExtents" not in extents_block:
        errors.append("lightweight/snapshot entity extents path was removed")
    if "boundedSolidInspect" not in extents_block:
        errors.append("Solid3d inspect must bypass synchronous GeometricExtents")

    if errors:
        print("FAIL: MCP cad_entity_inspect bounded Solid3d guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: cad_entity_inspect bounds Solid3d inspection without forcing synchronous GeometricExtents, while database snapshot retains extents semantics.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
