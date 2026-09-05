#!/usr/bin/env python3
from pathlib import Path
import importlib.util
import json
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
INVENTORY_SCRIPT = ROOT / "scripts" / "test-inventory.py"


def fail(message):
    print("ERROR:", message)
    return 1


def load_inventory_module():
    if not INVENTORY_SCRIPT.is_file():
        raise RuntimeError("missing scripts/test-inventory.py")
    spec = importlib.util.spec_from_file_location("qs3d_test_inventory", INVENTORY_SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError("cannot load scripts/test-inventory.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def verify_synthetic_inventory(module):
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        core = root / "tests" / "QS3D.Core.SmokeTests"
        agent = root / "tests" / "QS3D.AgentHarness.Core.SmokeTests"
        cli = root / "tests" / "QS3D.Code.Cli.SmokeTests"
        local = root / "tests" / "QS3D.BricsCAD.V25.LocalQualification"
        perf = root / "tests" / "QS3D.Core.PerfHarness"
        for project in (core, agent, cli, local, perf):
            write(project / (project.name + ".csproj"), "<Project />\n")

        write(
            core / "SmokeTestRegistration.cs",
            "internal static class SmokeTestRegistration { static void RunAll() { AlphaSmoke.Run(); BetaSmoke.Run(); /* FakeSmoke.Run(); */ var text = \"StringSmoke.Run();\"; } }\n",
        )
        write(
            core / "Program.cs",
            "Test(\"one\", () => { });\nTest(\"two\", () => { });\n// Test(\"fake\", () => { });\nvar text = \"Test(\\\"fake-string\\\", () => { });\";\n",
        )
        write(
            agent / "Program.cs",
            "Run(nameof(FirstScenario), FirstScenario);\nRun(nameof(SecondScenario), SecondScenario);\n// Run(nameof(FakeScenario), FakeScenario);\n",
        )
        write(
            cli / "Program.cs",
            "Run(\"first scenario\", FirstScenario);\nRun(\"second scenario\", SecondScenario);\nvar text = \"Run(\\\"fake\\\", Fake);\";\n",
        )

        write(root / "scripts" / "preflight-all.py", "print('aggregate')\n")
        write(root / "scripts" / "preflight-alpha.py", "print('a')\n")
        write(root / "scripts" / "preflight-beta.py", "print('b')\n")
        write(root / ".github" / "workflows" / "ci.yml", "name: ci\n")
        write(root / ".github" / "workflows" / "release.yaml", "name: release\n")

        inventory = module.collect_inventory(root)
        expected = {
            "automated_smoke_regression_scenarios": 8,
            "smoke_suites": {
                "QS3D.Core.SmokeTests": 4,
                "QS3D.AgentHarness.Core.SmokeTests": 2,
                "QS3D.Code.Cli.SmokeTests": 2,
            },
            "preflight_feature_gates": 2,
            "test_harness_projects": 5,
            "github_actions_workflows": 2,
        }
        if inventory != expected:
            raise RuntimeError(
                "synthetic inventory mismatch: expected "
                + json.dumps(expected, sort_keys=True)
                + " got "
                + json.dumps(inventory, sort_keys=True)
            )

        duplicate = core / "SmokeTestRegistration.cs"
        duplicate.write_text(
            "internal static class SmokeTestRegistration { static void RunAll() { AlphaSmoke.Run(); AlphaSmoke.Run(); } }\n",
            encoding="utf-8",
        )
        try:
            module.collect_inventory(root)
        except module.InventoryError as exc:
            if "duplicate Core smoke registration" not in str(exc):
                raise RuntimeError("duplicate registration failed with wrong diagnostic: " + str(exc)) from exc
        else:
            raise RuntimeError("duplicate Core smoke registration was accepted")


def verify_repository_inventory(module):
    inventory = module.collect_inventory(ROOT)
    minimums = {
        "automated_smoke_regression_scenarios": 289,
        "preflight_feature_gates": 1660,
        "test_harness_projects": 5,
        "github_actions_workflows": 12,
    }
    suite_minimums = {
        "QS3D.Core.SmokeTests": 271,
        "QS3D.AgentHarness.Core.SmokeTests": 9,
        "QS3D.Code.Cli.SmokeTests": 9,
    }
    for key, minimum in minimums.items():
        actual = inventory.get(key)
        if not isinstance(actual, int) or actual < minimum:
            raise RuntimeError(key + " regressed below " + str(minimum) + ": " + repr(actual))
    suites = inventory.get("smoke_suites")
    if not isinstance(suites, dict):
        raise RuntimeError("smoke_suites is not an object")
    for key, minimum in suite_minimums.items():
        actual = suites.get(key)
        if not isinstance(actual, int) or actual < minimum:
            raise RuntimeError(key + " regressed below " + str(minimum) + ": " + repr(actual))
    if inventory["automated_smoke_regression_scenarios"] != sum(suites.values()):
        raise RuntimeError("automated smoke/regression total does not equal suite sum")
    return inventory


def main():
    try:
        module = load_inventory_module()
        verify_synthetic_inventory(module)
        inventory = verify_repository_inventory(module)
    except (OSError, RuntimeError, ValueError) as exc:
        return fail(exc)

    print("QS3D test inventory preflight")
    print(json.dumps(inventory, sort_keys=True, separators=(",", ":")))
    print("PASS: verification inventory is deterministic, duplicate-safe and above protected baselines.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
