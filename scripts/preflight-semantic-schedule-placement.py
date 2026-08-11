#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSchedulePlacementPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticSchedulePlacementSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def main():
    failures = []
    for path in (PLANNER, SMOKE, REGISTRATION):
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    planner = PLANNER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    require(planner, "public sealed class SemanticSchedulePlacementItem", "placement request model", failures)
    require(planner, "public sealed class SemanticSchedulePlacementPlan", "placement result model", failures)
    require(planner, "public string ScheduleId { get; }", "stable schedule identity", failures)
    require(planner, "var id = Required(schedule.Id, \"availableSchedules.Id\");", "persisted SemanticScheduleDefinition.Id identity", failures)
    require(planner, "new Dictionary<string, SemanticScheduleDefinition>(StringComparer.OrdinalIgnoreCase)", "case-insensitive schedule catalog", failures)
    require(planner, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive request uniqueness", failures)
    require(planner, "private const int MaxItems = 128;", "bounded schedule count", failures)
    require(planner, "if (count > MaxItems)", "bounded available schedule enumeration", failures)
    require(planner, "var materialized = MaterializeItems(items);", "bounded placement-item materialization", failures)
    require(planner, "if (result.Count >= MaxItems)", "bounded placement-item enumeration", failures)
    if "items.ToList()" in planner:
        failures.append("bounded placement-item enumeration regressed to unbounded items.ToList()")
    require(planner, "PositiveFinite(item.WidthMm", "finite positive width guard", failures)
    require(planner, "PositiveFinite(item.HeightMm", "finite positive height guard", failures)
    require(planner, "placement.Xmm + placement.WidthMm > sheet.WidthMm", "existing view paper-bound guard", failures)
    require(planner, "options.MarginLeftMm", "paper margin contract", failures)
    require(planner, "options.ReservedBottomMm", "reserved title-block region contract", failures)
    require(planner, ".OrderByDescending(x => x.HeightMm)", "deterministic packing order", failures)
    require(planner, ".ThenBy(x => x.ScheduleId, StringComparer.OrdinalIgnoreCase)", "deterministic schedule-ID tie-break", failures)
    require(planner, "could not be placed without overlapping existing sheet content", "fail-closed overlap result", failures)

    for forbidden in ("Bricscad.", "Teigha.", "ObjectId", "Handle"):
        if forbidden in planner:
            failures.append(f"pure-Core schedule placement must remain native-handle free: found {forbidden!r}")

    require(smoke, "AvoidsExistingViewsDeterministically", "existing-view collision smoke", failures)
    require(smoke, "ExistingViewOutsideScheduleMarginRemainsValid", "paper-edge view regression smoke", failures)
    require(smoke, "ReservedBottomAreaIsRespected", "reserved title-block smoke", failures)
    require(smoke, "MissingScheduleFailsClosed", "missing schedule smoke", failures)
    require(smoke, "DuplicateRequestedScheduleFailsClosed", "duplicate request smoke", failures)
    require(smoke, "DuplicateAvailableScheduleFailsClosed", "duplicate catalog smoke", failures)
    require(smoke, "TooManyAvailableSchedulesFailClosed", "available schedule cardinality smoke", failures)
    require(smoke, "TooManyPlacementItemsFailClosed", "placement-item cardinality smoke", failures)
    require(smoke, "OversizedScheduleFailsClosed", "oversized schedule smoke", failures)
    require(smoke, "InvalidGeometryFailsClosed", "non-finite geometry smoke", failures)
    require(registration, "SemanticSchedulePlacementSmoke.Run();", "smoke registration", failures)

    if failures:
        print("QS3D Semantic Schedule Placement preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Semantic Schedule Placement remains pure-Core and native-handle free.")
    print("PASS: persisted SemanticScheduleDefinition.Id is the stable placement identity.")
    print("PASS: available schedules and placement items fail closed beyond 128 entries.")
    print("PASS: deterministic bounded packing avoids existing views and reserved paper regions.")
    print("PASS: missing/duplicate IDs, invalid geometry and unplaceable schedules fail closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
