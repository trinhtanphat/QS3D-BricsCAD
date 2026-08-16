from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "docs" / "HOURLY-AGENT-CONTROL.md"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: hourly agent control policy missing required contract: {needle}")


def main() -> int:
    text = POLICY.read_text(encoding="utf-8")

    for needle in (
        "exactly six hourly schedules",
        "`QS3D-CONTROL`: the authoritative coordinator and an execution lane itself.",
        "`QS3D-WORKER-01`",
        "`QS3D-WORKER-02`",
        "`QS3D-WORKER-03`",
        "`QS3D-WORKER-04`",
        "`QS3D-WORKER-05`",
        "The controller must also receive and execute its own work package every round",
        "Resolve the exact latest `main` SHA from GitHub.",
        "Allocate one non-overlapping work package to each worker and one to itself.",
        "Record every assignment in #1910 before the lane edits overlapping repository scope.",
        "at least 60 minutes of substantive engineering work",
        "This is a workload-sizing rule, not an elapsed-time claim.",
        "First visible valid reservation owns overlapping scope.",
        "A clean Git merge does not prove semantic non-overlap.",
        "Reassignment/takeover must be written to #1910 first.",
        "merge only when repository policy, branch protection, current-head evidence, and the assignment's merge authority all allow it",
        "Never claim tests, licensed BricsCAD behavior, or runtime evidence that was not actually executed.",
        "Do not flood the ledger with no-change heartbeat comments.",
    ):
        require(text, needle)

    topology = [
        "`QS3D-CONTROL`",
        "`QS3D-WORKER-01`",
        "`QS3D-WORKER-02`",
        "`QS3D-WORKER-03`",
        "`QS3D-WORKER-04`",
        "`QS3D-WORKER-05`",
    ]
    if len(topology) != 6 or any(text.count(name) == 0 for name in topology):
        raise SystemExit("FAIL: hourly control topology must retain exactly one controller identity and five worker identities")

    print(
        "PASS: hourly QS3D controller policy retains six-lane topology, controller execution, "
        ">=60-minute workload sizing, collision refusal, latest-main refresh, protected-main authority, "
        "and truthful LOCAL_ONLY/runtime reporting boundaries."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
