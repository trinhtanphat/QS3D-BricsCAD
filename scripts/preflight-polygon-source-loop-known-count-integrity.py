from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/PolygonSourceLoopRegionAssembler.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PolygonSourceLoopRegionAssemblerSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/polygon-source-loop-known-count-integrity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = [
    "var knownCount = ResolveKnownCount(sourceLoops);",
    'RequireStableKnownCount(sourceLoops, knownCount, "before MoveNext");',
    "var moved = enumerator.MoveNext();",
    'RequireStableKnownCount(sourceLoops, knownCount, "after MoveNext");',
    "if (knownCount.HasValue && materialized.Count >= knownCount.Value)",
    "if (materialized.Count >= MaxSourceLoops)",
    "var current = enumerator.Current;",
    'RequireStableKnownCount(sourceLoops, knownCount, "after Current");',
    "materialized.Add(current);",
    "if (knownCount.HasValue && materialized.Count != knownCount.Value)",
    'RequireStableKnownCount(sourceLoops, knownCount, "after traversal");',
    "sourceLoops as ICollection<PolygonSourceLoop2>",
    "sourceLoops as IReadOnlyCollection<PolygonSourceLoop2>",
    "sourceLoops as System.Collections.ICollection",
]
required_smoke = [
    "KnownCountOverrunStopsBeforeUnexpectedCurrent();",
    "TransientMoveNextCountDriftFailsClosed();",
    "TransientCurrentCountDriftFailsClosed();",
    "KnownCountUnderYieldFailsClosed();",
    "StableCountedSourceStillAssembles();",
    "PureStreamingSourceStillAssembles();",
    "Equal(0, source.CurrentReads);",
]
missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Polygon source-loop known-Count integrity preflight failed; missing: " + ", ".join(missing))

start = source.index("public static PolygonSourceRegionAssembly2 Assemble")
end = source.index("private static int? ResolveKnownCount", start)
assemble = source[start:end]
if "sourceLoops.Take(MaxSourceLoops + 1).ToList()" in assemble:
    raise SystemExit("Polygon source-loop assembly regressed to post-materialization capacity validation.")

order = [
    assemble.index('RequireStableKnownCount(sourceLoops, knownCount, "before MoveNext");'),
    assemble.index("var moved = enumerator.MoveNext();"),
    assemble.index('RequireStableKnownCount(sourceLoops, knownCount, "after MoveNext");'),
    assemble.index("if (!moved) break;"),
    assemble.index("if (knownCount.HasValue && materialized.Count >= knownCount.Value)"),
    assemble.index("if (materialized.Count >= MaxSourceLoops)"),
    assemble.index("var current = enumerator.Current;"),
    assemble.index('RequireStableKnownCount(sourceLoops, knownCount, "after Current");'),
    assemble.index("materialized.Add(current);"),
    assemble.index("if (knownCount.HasValue && materialized.Count != knownCount.Value)"),
    assemble.index('RequireStableKnownCount(sourceLoops, knownCount, "after traversal");'),
]
if order != sorted(order) or len(set(order)) != len(order):
    raise SystemExit("Polygon source-loop Count/capacity traversal ordering is not fail-closed.")

for token in ("Issue: #5009", "Lane-Key: `issue-5009`", "1024", "MoveNext", "Current", "pure streaming"):
    if token not in runbook:
        raise SystemExit("Polygon source-loop known-Count runbook missing token: " + token)

print("PASS polygon source-loop known-Count integrity source guard")
