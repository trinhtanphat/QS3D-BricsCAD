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

    start = source.find("private static void PopulateProxyExplodeMetrics(")
    end = source.find("private static double AddFinite(", start)
    if start < 0 or end <= start:
        fail("unable to isolate PopulateProxyExplodeMetrics")
    method = source[start:end]

    required = (
        "private const int MaxProxyExplodedParts = 4096;",
        "entity.Explode(exploded);",
        "if (exploded.Count > MaxProxyExplodedParts)",
        'Put(snapshot, "LegacyProbe.ProxyExplodeLimitExceeded", "true");',
        "foreach (DBObject item in exploded) item.Dispose();",
    )
    for token in required:
        if token not in source:
            fail(f"{REL} missing proxy-explode resource-safety contract: {token}")

    explode_pos = method.find("entity.Explode(exploded);")
    guard_pos = method.find("if (exploded.Count > MaxProxyExplodedParts)")
    mass_pos = method.find("solid.MassProperties.Volume")
    loop_pos = method.find("foreach (DBObject item in exploded)")
    if min(explode_pos, guard_pos, mass_pos, loop_pos) < 0:
        fail("proxy-explode method missing required admission/work tokens")
    if not (explode_pos < guard_pos < loop_pos < mass_pos):
        fail("exploded-part cardinality must be checked after native Explode and before per-part Solid3d metric work")

    finally_pos = method.find("finally")
    dispose_pos = method.find("foreach (DBObject item in exploded) item.Dispose();")
    if finally_pos < 0 or dispose_pos < finally_pos:
        fail("all native exploded DBObjects must remain disposed from finally on every path")

    if "Math.Min(exploded.Count, MaxProxyExplodedParts)" in method:
        fail("over-limit proxy explosions must fail closed for exact metrics, not silently truncate")

    print("PASS: V25 BLT legacy proxy explode rejects over-limit exact-metric inspection before per-part native MassProperties/Area work while disposing every exploded DBObject.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
