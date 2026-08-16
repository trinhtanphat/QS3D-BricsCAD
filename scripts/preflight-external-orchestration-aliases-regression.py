#!/usr/bin/env python3
from pathlib import Path
import runpy
import tempfile


ROOT = Path(__file__).resolve().parents[1]
GUARD = ROOT / "scripts" / "preflight-external-orchestration-aliases.py"


def write(root: Path, relative: str, text: str) -> None:
    path = root / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def main() -> int:
    namespace = runpy.run_path(str(GUARD), run_name="external_orchestration_alias_regression")
    scan_tree = namespace["scan_tree"]
    with tempfile.TemporaryDirectory() as temp_dir:
        fixture = Path(temp_dir)
        write(fixture, "docs/product-control-notes.md", "The model controller may use a worker thread for bounded parsing work.\n")
        assert scan_tree(fixture) == [], "ordinary product prose must remain accepted"
        write(fixture, "docs/hourly-control.md", "Automation notes live here.\n")
        failures = scan_tree(fixture)
        assert any("hourly-control.md" in failure for failure in failures), failures
        (fixture / "docs" / "hourly-control.md").unlink()
        write(fixture, "config/scheduled-worker-pool.json", "{}\n")
        failures = scan_tree(fixture)
        assert any("scheduled-worker-pool.json" in failure for failure in failures), failures
        (fixture / "config" / "scheduled-worker-pool.json").unlink()
        write(fixture, "docs/automation-notes.md", "CONTROL schedule coordinates WORKER-5 and WORKER-4 lanes for repository tasks.\n")
        failures = scan_tree(fixture)
        assert any("automation-notes.md" in failure for failure in failures), failures
        (fixture / "docs" / "automation-notes.md").unlink()
        write(fixture, "ops/coordination.txt", "The controller scheduler assigns six active lanes and worker-2 executes one task.\n")
        failures = scan_tree(fixture)
        assert any("coordination.txt" in failure for failure in failures), failures
        (fixture / "ops" / "coordination.txt").unlink()
        assert scan_tree(fixture) == [], "fixture cleanup should restore an accepted tree"
    print("PASS: external orchestration alias guard rejects renamed scheduler topology without product false positives.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
