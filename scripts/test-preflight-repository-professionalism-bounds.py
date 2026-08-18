#!/usr/bin/env python3
"""Hermetic regression for bounded repository-professionalism orchestration scans."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import tempfile

SCRIPT = Path(__file__).with_name("preflight-repository-professionalism.py")
spec = spec_from_file_location("repository_professionalism", SCRIPT)
if spec is None or spec.loader is None:
    raise RuntimeError("cannot load repository professionalism preflight")
module = module_from_spec(spec)
spec.loader.exec_module(module)


def inspect(root: Path) -> list[str]:
    original = module.ROOT
    try:
        module.ROOT = root
        failures: list[str] = []
        module.reject_external_orchestration_artifacts(failures)
        return failures
    finally:
        module.ROOT = original


def main() -> int:
    with tempfile.TemporaryDirectory() as raw:
        root = Path(raw)
        (root / "docs").mkdir()
        (root / "docs" / "normal.md").write_text("ordinary repository documentation\n", encoding="utf-8")
        failures = inspect(root)
        assert failures == [], failures

        oversized = root / "docs" / "oversized.txt"
        oversized.write_bytes(b"x" * (module.MAX_ORCHESTRATION_SCAN_BYTES + 1))
        failures = inspect(root)
        assert len(failures) == 1, failures
        assert "exceeds" in failures[0] and "byte safety bound" in failures[0], failures
        assert "oversized.txt" in failures[0], failures

        oversized.unlink()
        leaked = root / "docs" / "topology.md"
        leaked.write_text("QS3D-CONTROL\nQS3D-WORKER-01\n", encoding="utf-8")
        failures = inspect(root)
        assert len(failures) == 1, failures
        assert "external scheduler topology leaked" in failures[0], failures

    print("PASS: repository professionalism orchestration scan is bounded and preserves topology detection.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
