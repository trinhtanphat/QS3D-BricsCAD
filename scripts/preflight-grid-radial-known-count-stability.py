#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridRadialOrderingPlanner.cs"
MATERIALIZER = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridSnapInputMaterializer.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridRadialKnownCountStabilitySmoke.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid radial Count stability guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid radial Count stability guard forbids {label}: {marker}")


def main() -> None:
    for path in (PLANNER, MATERIALIZER, SMOKE):
        if not path.exists():
            raise SystemExit(f"ERROR: missing source {path.relative_to(ROOT)}")

    planner = PLANNER.read_text(encoding="utf-8")
    materializer = MATERIALIZER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    require(planner, 'GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid radial ordering input")', "shared Count-aware materialization")
    forbid(planner, "Take(MaxCurves + 1)", "LINQ one-past materialization")
    forbid(planner, "using System.Linq;", "LINQ dependency used only for unsafe bounded traversal")

    before_move = materializer.find("ValidateKnownCount(curves, admittedCount, label);")
    move = materializer.find("var moved = enumerator.MoveNext();")
    after_move = materializer.find("ValidateKnownCount(curves, admittedCount, label);", before_move + 1)
    overrun = materializer.find("if (admittedCount.HasValue && result.Count >= admittedCount.Value)")
    current = materializer.find("var curve = enumerator.Current;")
    after_current = materializer.find("ValidateKnownCount(curves, admittedCount, label);", after_move + 1)
    retain = materializer.find("result.Add(curve);")
    if min(before_move, move, after_move, overrun, current, after_current, retain) < 0 or not (
        before_move < move < after_move < overrun < current < after_current < retain
    ):
        raise SystemExit(
            "ERROR: Grid radial Count stability guard requires Count rebound -> MoveNext -> Count rebound -> "
            "overrun/ceiling -> Current -> Count rebound -> retention ordering"
        )

    require(materializer, "ReadKnownCount(curves, label)", "known Count admission")
    require(materializer, "ICollection<GridReferenceCurve>", "generic collection Count surface")
    require(materializer, "IReadOnlyCollection<GridReferenceCurve>", "read-only collection Count surface")
    require(materializer, "System.Collections.ICollection", "non-generic collection Count surface")
    require(materializer, "Count cannot be negative", "negative Count rejection")
    require(materializer, "conflicting known Count values", "cross-interface Count conflict rejection")
    require(materializer, "produced more curves than its known Count", "known Count overrun rejection")
    require(materializer, "known Count reported {0} curves but traversal produced {1}", "known Count under-yield rejection")

    for marker, label in (
        ("RejectsOverCapBeforeTraversal", "over-cap regression"),
        ("RejectsNegativeCountBeforeTraversal", "negative Count regression"),
        ("RejectsConflictingCountBeforeTraversal", "conflicting Count regression"),
        ("RejectsTransientGrowthBeforeCurrent", "transient growth regression"),
        ("RejectsTransientShrinkBeforeCurrent", "transient shrink regression"),
        ("RejectsKnownCountOverrunBeforeSecondCurrent", "stable known-Count overrun regression"),
        ("RejectsKnownCountUnderYield", "under-yield regression"),
        ("StableCountedAndStreamingOrderingRemainSupported", "stable and streaming control"),
        ("Equal(0, source.CurrentReads)", "no caller Current read before transient rejection"),
        ("Equal(1, source.CurrentReads)", "one admitted Current before overrun/under-yield boundary"),
    ):
        require(smoke, marker, label)

    print("PASS Grid radial known-Count stability source guard")


if __name__ == "__main__":
    main()
