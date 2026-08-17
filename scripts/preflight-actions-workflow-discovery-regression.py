from __future__ import annotations

import contextlib
import importlib.util
import io
import tempfile
from pathlib import Path

SCRIPT = Path(__file__).resolve().with_name("check-actions-pinned.py")


def load_checker():
    spec = importlib.util.spec_from_file_location("check_actions_pinned_under_test", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot import {SCRIPT}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write_valid(path: Path) -> None:
    path.write_text(
        "name: test\n"
        "on:\n"
        "  push:\n"
        "jobs:\n"
        "  test:\n"
        "    runs-on: ubuntu-latest\n"
        "    steps:\n"
        "      - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683\n",
        encoding="utf-8",
    )


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    checker = load_checker()

    with tempfile.TemporaryDirectory(prefix="qs3d-actions-discovery-") as temp_name:
        root = Path(temp_name)
        workflows = root / ".github" / "workflows"
        workflows.mkdir(parents=True)

        write_valid(workflows / "b.yaml")
        write_valid(workflows / "A.yml")
        (workflows / "ignored.txt").write_text("uses: bad/action@main\n", encoding="utf-8")

        paths, errors = checker.discover_workflow_paths(workflows)
        require(not errors, f"ordinary discovery failed: {errors}")
        require([path.name for path in paths] == ["A.yml", "b.yaml"], "workflow ordering is not deterministic")

        for path in paths:
            text, read_error = checker.read_workflow_source(path)
            require(read_error is None and text is not None, f"ordinary read failed: {read_error}")
            require(not checker.scan_workflow_text(path.name, text), f"ordinary workflow failed scan: {path}")

        nonregular = workflows / "directory.yml"
        nonregular.mkdir()
        paths, errors = checker.discover_workflow_paths(workflows)
        require(not paths, "non-regular candidate must invalidate discovery before scanning")
        require(any("regular file" in error for error in errors), f"missing non-regular diagnostic: {errors}")
        nonregular.rmdir()

        oversize = workflows / "oversize.yml"
        oversize.write_bytes(b"#" * (checker.MAX_WORKFLOW_SOURCE_BYTES + 1))
        paths, errors = checker.discover_workflow_paths(workflows)
        require(not paths, "oversize candidate must invalidate discovery before scanning")
        require(any("exceeds" in error for error in errors), f"missing oversize diagnostic: {errors}")
        oversize.unlink()

        upper = workflows / "Case.yml"
        lower = workflows / "case.yml"
        write_valid(upper)
        try:
            write_valid(lower)
            if upper.resolve() != lower.resolve():
                paths, errors = checker.discover_workflow_paths(workflows)
                require(not paths, "case-insensitive collision must invalidate discovery")
                require(any("case-insensitive" in error for error in errors), f"missing collision diagnostic: {errors}")
        finally:
            if lower.exists() and lower.resolve() != upper.resolve():
                lower.unlink()
            if upper.exists():
                upper.unlink()

        outside = root / "outside.yml"
        write_valid(outside)
        symlink = workflows / "linked.yml"
        try:
            symlink.symlink_to(outside)
        except (OSError, NotImplementedError):
            pass
        else:
            paths, errors = checker.discover_workflow_paths(workflows)
            require(not paths, "symlink candidate must invalidate discovery")
            require(any("symlink" in error for error in errors), f"missing symlink diagnostic: {errors}")
            symlink.unlink()

        bad_utf8 = workflows / "bad-utf8.yml"
        bad_utf8.write_bytes(b"\xff\xfe")
        paths, errors = checker.discover_workflow_paths(workflows)
        require(not errors and bad_utf8 in paths, f"metadata discovery should accept bounded regular bytes: {errors}")
        _, read_error = checker.read_workflow_source(bad_utf8)
        require(read_error is not None and "UTF-8" in read_error, f"invalid UTF-8 must fail closed: {read_error}")
        bad_utf8.unlink()

        blocker = workflows / "blocker.yml"
        blocker.mkdir()
        original_root = checker.ROOT
        original_workflows = checker.WORKFLOWS
        original_reader = checker.read_workflow_source
        reads = []

        def forbidden_read(path: Path):
            reads.append(path)
            raise AssertionError("source read occurred before discovery validation completed")

        checker.ROOT = root
        checker.WORKFLOWS = workflows
        checker.read_workflow_source = forbidden_read
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                result = checker.main()
            require(result == 1, "main must fail closed on invalid discovery metadata")
            require(not reads, f"scanner read workflow source before validating all candidates: {reads}")
        finally:
            checker.ROOT = original_root
            checker.WORKFLOWS = original_workflows
            checker.read_workflow_source = original_reader
            blocker.rmdir()

    print("PASS: Actions workflow discovery is deterministic, bounded, and fail-closed before scanning.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
