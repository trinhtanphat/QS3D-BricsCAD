from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ZoneAssignmentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "zone-assignment-count-integrity.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    start = source.find("public static int Assign(ProjectState project, string zoneId, IEnumerable<ProjectElement> elements)")
    end = source.find("public static bool Delete", start)
    assign = source[start:end] if start >= 0 and end > start else ""

    rebound = "RequireStableAssignmentTargetKnownCount(elements, knownTargetCount);"
    acquire = "using (var enumerator = elements.GetEnumerator())"
    move = "var moved = enumerator.MoveNext();"
    known_guard = "observedEntries >= knownTargetCount.Value"
    cap_guard = "observedEntries >= MaxAssignmentTargetEntries"
    current = "var element = enumerator.Current;"

    first_rebound = assign.find(rebound)
    acquire_pos = assign.find(acquire, first_rebound + len(rebound))
    second_rebound = assign.find(rebound, acquire_pos + len(acquire))
    loop_pos = assign.find("while (true)", second_rebound + len(rebound))
    third_rebound = assign.find(rebound, loop_pos + len("while (true)"))
    move_pos = assign.find(move, third_rebound + len(rebound))
    fourth_rebound = assign.find(rebound, move_pos + len(move))
    known_guard_pos = assign.find(known_guard, fourth_rebound + len(rebound))
    cap_guard_pos = assign.find(cap_guard, known_guard_pos + len(known_guard))
    current_pos = assign.find(current, cap_guard_pos + len(cap_guard))
    fifth_rebound = assign.find(rebound, current_pos + len(current))
    final_rebound = assign.find(rebound, fifth_rebound + len(rebound))

    ordered = [
        first_rebound,
        acquire_pos,
        second_rebound,
        loop_pos,
        third_rebound,
        move_pos,
        fourth_rebound,
        known_guard_pos,
        cap_guard_pos,
        current_pos,
        fifth_rebound,
        final_rebound,
    ]
    if not assign or any(pos < 0 for pos in ordered) or ordered != sorted(ordered):
        raise SystemExit(
            "Zone assignment must enforce Count rebound -> GetEnumerator -> rebound -> MoveNext rebound -> known/cap guards -> Current rebound -> final rebound ordering")

    if "while (enumerator.MoveNext())" in assign:
        raise SystemExit("Zone assignment must not hide MoveNext inside the loop condition because Count must rebound immediately after MoveNext")
    require(source, "SnapshotAssignmentTargetKnownCount(elements)", "known-Count snapshot")
    require(source, "Zone assignment target collection known count changed during enumeration", "Count drift diagnostic")
    require(smoke, "KnownCountOverrunRejectsBeforeCurrentRead", "N+1 Current no-overread regression")
    require(smoke, "ExactTraversalCountDriftFailsClosed", "post-traversal Count drift regression")
    require(smoke, "AcquisitionCountDriftRejectsBeforeMoveNext", "GetEnumerator Count drift regression")
    require(smoke, "MoveNextCountDriftRejectsBeforeCurrentRead", "MoveNext Count drift regression")
    require(smoke, "CurrentCountDriftRejectsBeforeMutation", "Current Count drift regression")
    require(smoke, "StableCountedInputAssigns", "stable counted control")
    require(smoke, "StreamingInputRemainsAccepted", "streaming control")
    require(smoke, "StreamingHardCapRejectsBeforeCurrentRead", "streaming hard-cap Current no-overread regression")
    require(runbook, "GetEnumerator", "enumerator acquisition boundary documentation")
    require(runbook, "MoveNext", "MoveNext boundary documentation")
    require(runbook, "Current", "Current boundary documentation")
    require(runbook, "transient", "transient Count drift documentation")

    print("PASS Zone assignment Count integrity preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())