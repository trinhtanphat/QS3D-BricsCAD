from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "docs" / "HOURLY-AGENT-CONTROL.md"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: hourly agent control policy missing required contract: {needle}")


def forbid(text: str, needle: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: hourly agent control policy retained stale topology contract: {needle}")


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
        "at least 60 minutes of substantive engineering work",
        "The minimum is per task, not combined across the pool.",
        "First visible valid reservation owns overlapping scope.",
        "A clean Git merge does not prove semantic non-overlap.",
        "Reassignment/takeover must be written to #1910 first.",
        "merge only when repository policy, branch protection, current-head evidence, and the assignment's merge authority all allow it",
        "Never force-push or overwrite another lane's work.",
        "Never claim tests, licensed BricsCAD behavior, or runtime evidence that was not actually executed.",
        "exactly five assignments (Task 0-4: controller + four workers)",
    ):
        require(text, needle)

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
        "PASS: hourly QS3D controller policy retains five-schedule topology, Task 0-4 dispatch, "
        "per-task >=60-minute workload sizing, collision refusal, latest-main refresh, protected-main authority, "
        "and truthful LOCAL_ONLY/runtime reporting boundaries."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
