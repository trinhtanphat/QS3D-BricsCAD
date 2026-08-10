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
    ]:
        ok = require(source, token, label) and ok
    for token, label in [
        ("ImpactPlanIsDeterministicAndReadOnly", "read-only regression"),
        ("MultipleRootsUseStableShortestCause", "multi-root regression"),
        ("InvalidRootsFailClosed", "canonical root regression"),
    ]:
        ok = require(smoke, token, label) and ok
    lowered = source.lower()
    if "bricscad" in lowered or "teigha" in lowered:
        print("ERROR: dependency impact planner must remain Core-only and CAD-runtime independent.")
        ok = False
    if not ok:
        return 1
    print("PASS: dependency impact planner is deterministic, read-only, stale-bound, and Core-only.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
