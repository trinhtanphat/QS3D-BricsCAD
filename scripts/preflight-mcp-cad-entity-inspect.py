#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        raise SystemExit(f"missing method marker: {signature}")
    next_method = source.find("\n        private static ", start + len(signature))
    return source[start:] if next_method < 0 else source[start:next_method]


def main() -> int:
    runtime = RUNTIME.read_text(encoding="utf-8")
    inspect = method_block(runtime, "private static string InspectEntity")
    describe = method_block(runtime, "private static string DescribeEntity")
    snapshot = method_block(runtime, "private static string BuildDatabaseSnapshotJson")

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

    extents_start = describe.find("if (extents)")
    details_start = describe.find("if (details)", extents_start)
    if extents_start < 0 or details_start <= extents_start:
        errors.append("cannot inspect entity extents/details boundary")
        extents_block = ""
    else:
        extents_block = describe[extents_start:details_start]
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