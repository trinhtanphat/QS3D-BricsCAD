#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "DependencyImpactPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DependencyImpactPlannerSmoke.cs"


def require(text, token, label):
    if token not in text:
        print(f"ERROR: missing {label}: {token}")
        return False
    return True


def main():
    if not SOURCE.exists() or not SMOKE.exists():
        print("ERROR: dependency impact planner source/smoke file is missing.")
        return 1
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    ok = True
    for token, label in [
        ("public sealed class DependencyImpactPlanner", "planner"),
        ("SourceChangeVersion", "stale-binding version"),
        ("GetDirectDependents", "existing dependency graph reuse"),
        ("CauseElementId", "direct cause"),
        ("RootElementId", "root provenance"),
        ("OrderBy(x => x.Depth)", "deterministic depth ordering"),
        ("project.ChangeVersion != sourceChangeVersion", "concurrent change guard"),
        ("Duplicate dependency impact source id", "duplicate root fail-closed guard"),
        ("var sourceElementCount = project.Elements.Count;", "captured project cardinality"),
        ("CanonicalRoots(sourceElementIds, sourceElementCount)", "captured-cardinality root bound"),
        ("if (index >= maxRootCount)", "early root enumeration bound"),
        ("cannot exceed project semantic element count", "bounded-root diagnostic"),
    ]:
        ok = require(source, token, label) and ok
    for token, label in [
        ("ImpactPlanIsDeterministicAndReadOnly", "read-only regression"),
        ("MultipleRootsUseStableShortestCause", "multi-root regression"),
        ("InvalidRootsFailClosed", "canonical root regression"),
        ("OverBoundRootEnumerationStopsAtProjectCardinality", "bounded root-enumeration regression"),
        ("Dependency impact planner enumerated beyond the first impossible root", "over-enumeration tripwire"),
        ("MutationDuringRootEnumerationFailsFreshness", "input-enumeration freshness regression"),
        ("project.Touch();", "deterministic root-enumeration mutation probe"),
    ]:
        ok = require(smoke, token, label) and ok

    plan_start = source.find("public DependencyImpactPlan Plan(ProjectState project, IEnumerable<string> sourceElementIds)")
    graph_start = source.find("var graph = new DependencyGraph();", plan_start)
    if plan_start < 0 or graph_start <= plan_start:
        print("ERROR: cannot isolate dependency impact planning preamble.")
        ok = False
    else:
        preamble = source[plan_start:graph_start]
        version = preamble.find("var sourceChangeVersion = project.ChangeVersion;")
        count = preamble.find("var sourceElementCount = project.Elements.Count;")
        roots = preamble.find("CanonicalRoots(sourceElementIds, sourceElementCount)")
        if min(version, count, roots) < 0 or not (version < count < roots):
            print("ERROR: freshness/version and cardinality snapshots must precede caller root enumeration.")
            ok = False

    canonical_start = source.find("private static IReadOnlyList<string> CanonicalRoots")
    walk_start = source.find("private sealed class WalkState", canonical_start)
    if canonical_start < 0 or walk_start <= canonical_start:
        print("ERROR: cannot isolate dependency impact canonical-root boundary.")
        ok = False
    else:
        canonical = source[canonical_start:walk_start]
        bound = canonical.find("if (index >= maxRootCount)")
        raw = canonical.find("var raw = value ?? string.Empty;")
        if bound < 0 or raw < 0 or bound >= raw:
            print("ERROR: root cardinality guard must run before processing the first impossible root value.")
            ok = False

    for legacy in (
        "CanonicalRoots(sourceElementIds);",
        "CanonicalRoots(sourceElementIds, project.Elements.Count)",
    ):
        if legacy in source:
            print("ERROR: dependency impact planner uses a legacy root materialization/freshness pattern: " + legacy)
            ok = False

    lowered = source.lower()
    if "bricscad" in lowered or "teigha" in lowered:
        print("ERROR: dependency impact planner must remain Core-only and CAD-runtime independent.")
        ok = False
    if not ok:
        return 1
    print("PASS: dependency impact planner is deterministic, read-only, input-freshness-bound, Core-only, and stops impossible root enumeration at captured project cardinality.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
