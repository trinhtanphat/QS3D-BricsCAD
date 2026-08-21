from __future__ import annotations

import importlib.util
import stat
import tempfile
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch


SCRIPT = Path(__file__).with_name("check-actions-pinned.py")
SPEC = importlib.util.spec_from_file_location("check_actions_pinned", SCRIPT)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError("Cannot load check-actions-pinned.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def main() -> int:
    with tempfile.TemporaryDirectory() as temp_dir:
        workflows = Path(temp_dir) / "workflows"
        workflows.mkdir()
        regular = workflows / "regular.yml"
        candidate = workflows / "candidate.yml"
        regular.write_text("name: regular\non: workflow_dispatch\n", encoding="utf-8")
        candidate.write_text("name: candidate\non: workflow_dispatch\n", encoding="utf-8")

        original_lstat = Path.lstat
        original_resolve = Path.resolve

        def fake_lstat(path: Path):
            metadata = original_lstat(path)
            if path == candidate:
                return SimpleNamespace(
                    st_mode=metadata.st_mode,
                    st_size=metadata.st_size,
                    st_file_attributes=MODULE.WINDOWS_REPARSE_POINT_ATTRIBUTE,
                )
            return metadata

        def guarded_resolve(path: Path, *args, **kwargs):
            if path == candidate:
                raise AssertionError("reparse candidate reached path resolution")
            return original_resolve(path, *args, **kwargs)

        with patch.object(Path, "lstat", fake_lstat), patch.object(Path, "resolve", guarded_resolve):
            paths, errors = MODULE.discover_workflow_paths(workflows)

        if paths:
            raise AssertionError(f"unsafe discovery returned validated paths: {paths}")
        if not any("must not be a reparse point" in error for error in errors):
            raise AssertionError(f"reparse candidate was not rejected: {errors}")
        if any("regular.yml" in error for error in errors):
            raise AssertionError(f"regular workflow was rejected unexpectedly: {errors}")

        candidate.unlink()
        paths, errors = MODULE.discover_workflow_paths(workflows)
        if errors:
            raise AssertionError(f"regular workflow discovery failed: {errors}")
        if paths != [regular]:
            raise AssertionError(f"regular workflow discovery changed: {paths}")

    print("PASS: action-pinning workflow discovery rejects reparse-point candidates before resolution")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
