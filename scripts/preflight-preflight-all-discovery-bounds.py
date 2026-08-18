#!/usr/bin/env python3
from pathlib import Path
import importlib.util
import os
import tempfile

HERE = Path(__file__).resolve().parent
TARGET = HERE / "preflight-all.py"

spec = importlib.util.spec_from_file_location("qs3d_preflight_all", TARGET)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def write_gate(directory, name, size=1):
    path = directory / name
    path.write_bytes(b"x" * size)
    return path


def expect_runtime_error(action, fragment):
    try:
        action()
    except RuntimeError as exc:
        message = str(exc)
        if fragment not in message:
            raise AssertionError("expected %r in %r" % (fragment, message))
        return
    raise AssertionError("expected RuntimeError containing %r" % fragment)


def with_root(root):
    module.ROOT = root
    module.SCRIPTS = root / "scripts"
    module.SCRIPTS.mkdir(parents=True, exist_ok=True)
    module.SELF = module.SCRIPTS / "preflight-all.py"


def test_exact_count_and_ordering():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        with_root(root)
        candidates = []
        for index in range(module.MAX_FEATURE_GATES):
            candidates.append(write_gate(module.SCRIPTS, "preflight-%04d.py" % index))
        ordered = module.validate_candidates(reversed(candidates))
        assert len(ordered) == module.MAX_FEATURE_GATES
        assert ordered[0].name == "preflight-0000.py"
        assert ordered[-1].name == "preflight-%04d.py" % (module.MAX_FEATURE_GATES - 1)


def test_count_boundary_plus_one_fails_before_inspection():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        with_root(root)
        missing = [module.SCRIPTS / ("preflight-missing-%04d.py" % index) for index in range(module.MAX_FEATURE_GATES + 1)]
        expect_runtime_error(
            lambda: module.validate_candidates(missing),
            "discovery count %d exceeds maximum %d" % (module.MAX_FEATURE_GATES + 1, module.MAX_FEATURE_GATES),
        )


def test_discovery_stops_at_boundary_plus_one():
    class CountingScripts:
        def __init__(self):
            self.emitted = 0

        def glob(self, pattern):
            assert pattern == "preflight-*.py"
            for index in range(module.MAX_FEATURE_GATES + 2):
                self.emitted += 1
                if self.emitted > module.MAX_FEATURE_GATES + 1:
                    raise AssertionError("discover consumed candidates beyond the rejection boundary")
                yield Path("/virtual/scripts/preflight-%04d.py" % index)

    original_root = module.ROOT
    original_scripts = module.SCRIPTS
    original_self = module.SELF
    fake_scripts = CountingScripts()
    module.ROOT = Path("/virtual")
    module.SCRIPTS = fake_scripts
    module.SELF = Path("/virtual/scripts/preflight-all.py")
    try:
        expect_runtime_error(
            module.discover,
            "discovery count %d exceeds maximum %d" % (module.MAX_FEATURE_GATES + 1, module.MAX_FEATURE_GATES),
        )
    finally:
        module.ROOT = original_root
        module.SCRIPTS = original_scripts
        module.SELF = original_self

    assert fake_scripts.emitted == module.MAX_FEATURE_GATES + 1


def test_source_size_exact_bound_and_boundary_plus_one():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        with_root(root)
        exact = write_gate(module.SCRIPTS, "preflight-exact.py", module.MAX_FEATURE_GATE_SOURCE_BYTES)
        assert module.validate_candidates([exact]) == [exact]
        oversized = write_gate(module.SCRIPTS, "preflight-oversized.py", module.MAX_FEATURE_GATE_SOURCE_BYTES + 1)
        expect_runtime_error(
            lambda: module.validate_candidates([oversized]),
            "source size %d bytes exceeds maximum %d"
            % (module.MAX_FEATURE_GATE_SOURCE_BYTES + 1, module.MAX_FEATURE_GATE_SOURCE_BYTES),
        )


def test_invalid_discovery_never_launches_children():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        with_root(root)
        write_gate(module.SCRIPTS, "preflight-oversized.py", module.MAX_FEATURE_GATE_SOURCE_BYTES + 1)
        calls = []
        original_run = module.subprocess.run
        module.subprocess.run = lambda *args, **kwargs: calls.append((args, kwargs))
        try:
            assert module.main() == 1
        finally:
            module.subprocess.run = original_run
        assert calls == []


def test_legacy_symlink_and_case_collision_guards():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        with_root(root)
        first = write_gate(module.SCRIPTS, "preflight-Alpha.py")
        second = write_gate(module.SCRIPTS, "preflight-alpha.py")
        expect_runtime_error(
            lambda: module.validate_candidates([first, second]),
            "case-insensitive preflight filename collision",
        )

        target = write_gate(module.SCRIPTS, "target.py")
        link = module.SCRIPTS / "preflight-link.py"
        try:
            os.symlink(target, link)
        except (OSError, NotImplementedError):
            return
        expect_runtime_error(lambda: module.validate_candidates([link]), "is symlink")


def main():
    test_exact_count_and_ordering()
    test_count_boundary_plus_one_fails_before_inspection()
    test_discovery_stops_at_boundary_plus_one()
    test_source_size_exact_bound_and_boundary_plus_one()
    test_invalid_discovery_never_launches_children()
    test_legacy_symlink_and_case_collision_guards()
    print("PASS: aggregate preflight discovery bounds are deterministic and fail before execution.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
