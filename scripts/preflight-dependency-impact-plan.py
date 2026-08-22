#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "DependencyImpactPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DependencyImpactPlannerSmoke.cs"
STRUCTURAL_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DependencyImpactSourceStructuralFreshnessSmoke.cs"


def require(text, token, label):
    if token not in text:
        print(f"ERROR: missing {label}: {token}")
        return False
    return True


def main():
    if not SOURCE.exists() or not SMOKE.exists() or not STRUCTURAL_SMOKE.exists():
        print("ERROR: dependency impact planner source/smoke file is missing.")
        return 1
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    structural_smoke = STRUCTURAL_SMOKE.read_text(encoding="utf-8")
    ok = True
    for token, label in [
        ("public sealed class DependencyImpactPlanner", "planner"),
        ("SourceChangeVersion", "stale-binding version"),
        ("GetDirectDependents", "existing dependency graph reuse"),
        ("CauseElementId", "direct cause"),
        ("RootElementId", "root provenance"),
        ("OrderBy(x => x.Depth)", "deterministic depth ordering"),
        ("var sourceChangeVersion = project.ChangeVersion;", "captured change version"),
        ("var sourceElementOwnership = SnapshotElementOwnership(project);", "captured element ownership"),
        ("CanonicalRoots(sourceElementIds, sourceElementOwnership.Count)", "captured-ownership root bound"),
        ("RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);", "structural freshness guard"),
        ("project.ChangeVersion != expectedChangeVersion", "concurrent change guard"),
        ("project.Elements.Count != expectedOwnership.Count", "structural cardinality guard"),
        ("!ReferenceEquals(original, element)", "element ownership identity guard"),
        ("Duplicate dependency impact source id", "duplicate root fail-closed guard"),
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
    for token, label in [
        ("RemovedDependentDuringSourceEnumerationFailsClosed", "structural remove regression"),
        ("ReplacedDependentDuringSourceEnumerationFailsClosed", "structural replacement regression"),
        ("StablePlanStillIncludesDependent", "stable structural regression"),
        ("Project element ownership changed while dependency impact was being planned", "structural freshness diagnostic"),
    ]:
        ok = require(structural_smoke, token, label) and ok

    plan_start = source.find("public DependencyImpactPlan Plan(ProjectState project, IEnumerable<string> sourceElementIds)")
    graph_start = source.find("var graph = new DependencyGraph();", plan_start)
    if plan_start < 0 or graph_start <= plan_start:
        print("ERROR: cannot isolate dependency impact planning preamble.")
        ok = False
    else:
        preamble = source[plan_start:graph_start]
        version = preamble.find("var sourceChangeVersion = project.ChangeVersion;")
        ownership = preamble.find("var sourceElementOwnership = SnapshotElementOwnership(project);")
        roots = preamble.find("CanonicalRoots(sourceElementIds, sourceElementOwnership.Count)")
        freshness = preamble.find("RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);")
        if min(version, ownership, roots, freshness) < 0 or not (version < ownership < roots < freshness):
            print("ERROR: change-version and structural ownership snapshots must precede caller root enumeration and freshness validation.")
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

    freshness_start = source.find("private static void RequireProjectFresh(")
    canonical_after_freshness = source.find("private static IReadOnlyList<string> CanonicalRoots", freshness_start)
    if freshness_start < 0 or canonical_after_freshness <= freshness_start:
        print("ERROR: cannot isolate dependency impact structural-freshness boundary.")
        ok = False
    else:
        freshness = source[freshness_start:canonical_after_freshness]
        for token in (
            "project.ChangeVersion != expectedChangeVersion",
            "project.Elements.Count != expectedOwnership.Count",
            "var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
            "!expectedOwnership.TryGetValue(element.Id, out var original)",
            "!ReferenceEquals(original, element)",
        ):
            if token not in freshness:
                print("ERROR: dependency impact structural freshness lost token: " + token)
                ok = False

    for legacy in (
        "CanonicalRoots(sourceElementIds);",
        "CanonicalRoots(sourceElementIds, project.Elements.Count)",
        "var sourceElementCount = project.Elements.Count;",
    ):
        if legacy in source:
            print("ERROR: dependency impact planner uses a legacy count-only materialization/freshness pattern: " + legacy)
            ok = False

    lowered = source.lower()
    if "bricscad" in lowered or "teigha" in lowered:
        print("ERROR: dependency impact planner must remain Core-only and CAD-runtime independent.")
        ok = False
    if not ok:
        return 1
    print("PASS: dependency impact planner is deterministic, read-only, Core-only, bounds root enumeration by a pre-enumeration ownership snapshot, and rejects ChangeVersion/cardinality/reference-identity drift before graph work and before return.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
