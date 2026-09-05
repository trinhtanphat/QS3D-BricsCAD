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
        "var boundedSolidExtents = extents && entity is Solid3d;",
        "if (boundedSolidExtents) builder.Append(\"null\");",
        "else try { builder.Append(ExtentsJson(entity.GeometricExtents)); } catch { builder.Append(\"null\"); }",
        "if (boundedSolidExtents) builder.Append(\",\\\"extentsDeferred\\\":true\");",
    ):
        if token not in (inspect + describe):
            errors.append(f"missing bounded Solid3d extents contract: {token}")

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
        errors.append("non-Solid3d entity extents path was removed")
    if "boundedSolidExtents" not in extents_block:
        errors.append("all Solid3d read surfaces must bypass synchronous GeometricExtents")
    if "extents && details && entity is Solid3d" in describe:
        errors.append("Solid3d extents deferral must not depend on details=true; database snapshot uses details=false")

    if errors:
        print("FAIL: MCP Solid3d entity/snapshot extents guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: cad_entity_inspect and cad_database_snapshot both defer Solid3d GeometricExtents while non-Solid3d extents semantics remain available.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
