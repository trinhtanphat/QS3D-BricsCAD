from __future__ import annotations

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "docs" / "HOURLY-AGENT-CONTROL.md"
PREFLIGHT = ROOT / "scripts" / "preflight-hourly-agent-control.py"


class HourlyAgentControlContractTests(unittest.TestCase):
    """Independent regression coverage for the hourly coordination contract.

    This suite intentionally does not duplicate the production preflight's implementation.
    It validates the policy/preflight pair as text so topology regressions are caught even
    when a stale preflight is accidentally edited to accept the same bad contract.
    """

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

    def test_controller_executes_task_zero_and_dispatch_is_exactly_five(self) -> None:
        self.assertIn("execution lane itself (Task 0)", self.policy)
        self.assertIn(
            "Allocate exactly five mutually exclusive packages: Task 0 to itself and Task 1-4 to the four workers.",
            self.policy,
        )
        self.assertIn(
            "perform a real high-priority audit/fix/integration package rather than stopping after dispatch",
            self.policy,
        )

    def test_minimum_workload_is_per_task_not_combined_or_elapsed(self) -> None:
        self.assertIn("at least 60 minutes of substantive engineering work", self.policy)
        self.assertIn("The minimum is per task, not combined across the pool.", self.policy)
        self.assertIn("This is a workload-sizing rule, not an elapsed-time claim.", self.policy)

    def test_collision_and_protected_main_safety_are_normative(self) -> None:
        for contract in (
            "First visible valid reservation owns overlapping scope.",
            "A clean Git merge does not prove semantic non-overlap.",
            "Reassignment/takeover must be written to #1910 first.",
            "Never force-push or overwrite another lane's work.",
        ):
            self.assertIn(contract, self.policy)

        self.assertIn("branch protection", self.policy)
        self.assertIn("current-head evidence", self.policy)

    def test_production_preflight_rejects_stale_topology(self) -> None:
        # Guard the guard: the production preflight itself must explicitly reject the
        # historical six-lane spellings instead of only checking positive phrases.
        for stale in (
            "exactly six hourly schedules",
            "one controller and five worker schedules",
            "`QS3D-WORKER-05`",
            "six assignments (controller + five workers)",
        ):
            self.assertIn(repr(stale), self.preflight)
        self.assertIn("forbid(text, stale)", self.preflight)


if __name__ == "__main__":
    unittest.main(verbosity=2)
