from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "docs" / "HOURLY-AGENT-CONTROL.md"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: hourly agent control policy missing required contract: {needle}")


def forbid(text: str, needle: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: hourly agent control policy retained stale topology contract: {needle}")


def require_order(text: str, earlier: str, later: str) -> None:
    left = text.find(earlier)
    right = text.find(later)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(
            "FAIL: hourly agent control policy ordering contract is missing or reversed: "
            f"{earlier!r} must precede {later!r}"
        )


def main() -> int:
    text = POLICY.read_text(encoding="utf-8")

    for needle in (
        "exactly five hourly schedules",
        "`QS3D-CONTROL`: the authoritative coordinator and an execution lane itself (Task 0).",
        "`QS3D-WORKER-01` (Task 1).",
        "`QS3D-WORKER-02` (Task 2).",
        "`QS3D-WORKER-03` (Task 3).",
        "`QS3D-WORKER-04` (Task 4).",
        "There is one controller and four worker schedules.",
        "Allocate exactly five mutually exclusive packages: Task 0 to itself and Task 1-4 to the four workers.",
        "Publish the complete Task 0-4 assignment in #1910 before any substantive Task 0 coding",
        "cycle timestamp and exact baseline SHA",
        "at least 60 minutes of substantive engineering work",
        "preferably 60-120 minutes",
        "The minimum is per task, not combined across the pool.",
        "Never pad a package with filler",
        "owned component/files plus explicit exclusions or collision boundaries",
        "one primary objective",
        "two to four concrete related sub-objectives or fallback items",
        "tests/preflights/build/CI required",
        "branch/PR plan and canonical integration surface",
        "A lane does not stop merely because its first defect/sub-item is fixed, one test turns green, or one commit is pushed.",
        "continues immediately to the next justified sub-objective or fallback inside the same reserved package",
        "If one sub-item becomes blocked, the lane should work another non-overlapping sub-item inside the same package when safe.",
        "If main drift makes a worker package obsolete",
        "record the replacement with the new baseline in #1910",
        "First visible valid reservation owns overlapping scope.",
        "A clean Git merge does not prove semantic non-overlap.",
        "Reassignment/takeover must be written to #1910 first.",
        "merge only when repository policy, branch protection, current-head evidence, and the assignment's merge authority all allow it",
        '"Push git" means push to the repository-prescribed canonical working branch/PR workflow, not direct protected-main push.',
        "Never force-push or overwrite another lane's work.",
        "Never claim tests, licensed BricsCAD behavior, or runtime evidence that was not actually executed.",
        "exactly five assignments (Task 0-4: controller + four workers)",
    ):
        require(text, needle)

    require_order(
        text,
        "Publish the complete Task 0-4 assignment in #1910 before any substantive Task 0 coding",
        "Only after the complete assignment is visible, execute Task 0 immediately",
    )

    for stale in (
        "exactly six hourly schedules",
        "one controller and five worker schedules",
        "`QS3D-WORKER-05`",
        "six assignments (controller + five workers)",
    ):
        forbid(text, stale)

    topology = [
        "`QS3D-CONTROL`",
        "`QS3D-WORKER-01`",
        "`QS3D-WORKER-02`",
        "`QS3D-WORKER-03`",
        "`QS3D-WORKER-04`",
    ]
    if len(topology) != 5 or any(text.count(name) == 0 for name in topology):
        raise SystemExit("FAIL: hourly control topology must retain exactly one controller identity and four worker identities")

    print(
        "PASS: hourly QS3D controller policy retains five-schedule topology, pre-code dispatch ordering, "
        "substantial Task 0-4 package fields, continuation/fallback semantics, stale-package replacement, "
        "collision refusal, protected-main workflow, and truthful runtime reporting boundaries."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
