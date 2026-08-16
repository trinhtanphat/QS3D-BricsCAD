from __future__ import annotations

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "docs" / "HOURLY-AGENT-CONTROL.md"
PREFLIGHT = ROOT / "scripts" / "preflight-hourly-agent-control.py"


class HourlyAgentControlContractTests(unittest.TestCase):
    """Independent regression coverage for hourly coordination policy drift."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.policy = POLICY.read_text(encoding="utf-8")
        cls.preflight = PREFLIGHT.read_text(encoding="utf-8")

    def test_topology_is_exactly_controller_plus_four_workers(self) -> None:
        self.assertIn("exactly five hourly schedules", self.policy)
        self.assertIn("There is one controller and four worker schedules.", self.policy)
        self.assertNotIn("QS3D-WORKER-05", self.policy)
        self.assertNotIn("one controller and five worker schedules", self.policy)
        self.assertNotIn("exactly six hourly schedules", self.policy)
        worker_ids = set(re.findall(r"QS3D-WORKER-(\d{2})", self.policy))
        self.assertEqual({"01", "02", "03", "04"}, worker_ids)

    def test_controller_executes_task_zero_after_complete_dispatch(self) -> None:
        for contract in (
            "execution lane itself (Task 0)",
            "Allocate exactly five mutually exclusive packages: Task 0 to itself and Task 1-4 to the four workers.",
            "Publish the complete Task 0-4 assignment in #1910 before any substantive Task 0 coding",
            "execute Task 0 immediately as a real engineering package rather than stopping after dispatch",
        ):
            self.assertIn(contract, self.policy)

    def test_minimum_workload_is_per_task_not_combined_or_elapsed(self) -> None:
        for contract in (
            "at least 60 minutes of substantive engineering work",
            "The minimum is per task, not combined across the pool.",
            "This is a workload-sizing rule, not an elapsed-time claim.",
            "Never pad a package with filler or unrelated work merely to satisfy the sizing rule.",
        ):
            self.assertIn(contract, self.policy)

    def test_lane_must_continue_after_small_first_subtask(self) -> None:
        self.assertIn(
            "A lane does not stop merely because its first defect/sub-item is fixed, one test turns green, or one commit is pushed.",
            self.policy,
        )
        self.assertIn(
            "continues immediately to the next justified sub-objective or fallback inside the same reserved package",
            self.policy,
        )
        self.assertIn(
            "Scheduled lanes must not stop at readiness review or analysis when the accepted package contains valid implementation work",
            self.policy,
        )

    def test_valid_assignment_authorizes_branch_scoped_engineering(self) -> None:
        for contract in (
            "that assignment explicitly authorizes the lane to perform the repository work inside the reserved scope without another owner confirmation",
            "commit, push its dedicated task branch, and open/update its PR",
            "This execution authority is branch-scoped and does not grant direct `main` write permission.",
        ):
            self.assertIn(contract, self.policy)

    def test_direct_main_bypass_is_explicitly_forbidden(self) -> None:
        for contract in (
            "Never push commits directly to `main`.",
            "Never write the default branch through the GitHub Contents API.",
            "Never update the `main` ref directly, force-update it, or use an equivalent bypass.",
            '"Push git" means push to the repository-prescribed canonical working branch/PR workflow, not direct protected-main push.',
        ):
            self.assertIn(contract, self.policy)

    def test_collision_and_takeover_safety_are_normative(self) -> None:
        for contract in (
            "First visible valid reservation owns overlapping scope.",
            "A clean Git merge does not prove semantic non-overlap.",
            "Reassignment/takeover must be written to #1910 first.",
            "Never force-push or overwrite another lane's work.",
        ):
            self.assertIn(contract, self.policy)

    def test_stale_package_must_be_replaced_not_left_idle(self) -> None:
        self.assertIn(
            "replace any package made obsolete by main drift with a new non-overlapping >=60-minute package",
            self.policy,
        )
        self.assertIn(
            "the controller must not leave the worker with stale instructions",
            self.policy,
        )

    def test_production_preflight_rejects_stale_topology(self) -> None:
        for stale in (
            "exactly six hourly schedules",
            "one controller and five worker schedules",
            "`QS3D-WORKER-05`",
            "six assignments (controller + five workers)",
        ):
            self.assertIn(repr(stale), self.preflight)
        self.assertIn("forbid(text, stale)", self.preflight)

    def test_production_preflight_pins_write_and_execution_contracts(self) -> None:
        expected_fragments = (
            "without another owner confirmation",
            "Never push commits directly to `main`.",
            "not stop at readiness review or analysis",
            "push its dedicated task branch",
        )
        for fragment in expected_fragments:
            self.assertIn(fragment, self.preflight)


if __name__ == "__main__":
    unittest.main(verbosity=2)
