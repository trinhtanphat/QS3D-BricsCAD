#!/usr/bin/env python3
from pathlib import Path
import importlib.util
import os
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
TARGET = HERE / "preflight-all.py"

spec = importlib.util.spec_from_file_location("qs3d_preflight_all_child_env", TARGET)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def test_python_controls_are_removed_and_required_environment_is_preserved():
    hostile = {
        "PATH": "/toolchain/bin",
        "GITHUB_ACTIONS": "true",
        "QS3D_SENTINEL": "keep-me",
        "PYTHONPATH": "/tmp/poison",
        "PYTHONHOME": "/tmp/fake-home",
        "PYTHONSTARTUP": "/tmp/startup.py",
        "PYTHONWARNINGS": "error",
        "PYTHONINSPECT": "1",
        "PYTHONBREAKPOINT": "malicious.hook",
        "PYTHONPYCACHEPREFIX": "/tmp/cache",
        "PYTHONUSERBASE": "/tmp/userbase",
        "PYTHONUTF8": "0",
        "PYTHONIOENCODING": "latin-1",
        "PYTHONNOUSERSITE": "0",
        "PYTHONDONTWRITEBYTECODE": "0",
    }
    child = module.build_child_env(hostile)

    for name in module.PYTHON_ENVIRONMENT_CONTROLS:
        assert name not in child, name
    assert child["PATH"] == "/toolchain/bin"
    assert child["GITHUB_ACTIONS"] == "true"
    assert child["QS3D_SENTINEL"] == "keep-me"
    assert child["PYTHONUTF8"] == "1"
    assert child["PYTHONIOENCODING"] == "utf-8"
    assert child["PYTHONNOUSERSITE"] == "1"
    assert child["PYTHONDONTWRITEBYTECODE"] == "1"


def test_source_mapping_is_not_mutated():
    source = {"PYTHONPATH": "/tmp/poison", "QS3D_SENTINEL": "keep-me"}
    before = dict(source)
    module.build_child_env(source)
    assert source == before


def test_main_passes_one_sanitized_environment_to_every_child():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        scripts = root / "scripts"
        scripts.mkdir()
        gates = [scripts / "preflight-a.py", scripts / "preflight-b.py"]
        for gate in gates:
            gate.write_text("raise SystemExit(0)\n", encoding="utf-8")

        original_root = module.ROOT
        original_scripts = module.SCRIPTS
        original_self = module.SELF
        original_discover = module.discover
        original_run = module.subprocess.run
        original_environ = os.environ.copy()
        calls = []

        class Completed:
            returncode = 0

        def fake_run(args, **kwargs):
            calls.append((args, kwargs))
            return Completed()

        try:
            module.ROOT = root
            module.SCRIPTS = scripts
            module.SELF = scripts / "preflight-all.py"
            module.discover = lambda: gates
            module.subprocess.run = fake_run
            os.environ["PYTHONPATH"] = "/tmp/poison"
            os.environ["PYTHONHOME"] = "/tmp/fake-home"
            os.environ["PYTHONWARNINGS"] = "error"
            os.environ["QS3D_SENTINEL"] = "keep-me"
            assert module.main() == 0
        finally:
            module.subprocess.run = original_run
            module.discover = original_discover
            module.ROOT = original_root
            module.SCRIPTS = original_scripts
            module.SELF = original_self
            os.environ.clear()
            os.environ.update(original_environ)

        assert len(calls) == 2
        first_env = calls[0][1]["env"]
        assert calls[1][1]["env"] is first_env
        for name in module.PYTHON_ENVIRONMENT_CONTROLS:
            assert name not in first_env, name
        assert first_env["QS3D_SENTINEL"] == "keep-me"
        assert first_env["PYTHONUTF8"] == "1"
        assert first_env["PYTHONIOENCODING"] == "utf-8"
        assert first_env["PYTHONNOUSERSITE"] == "1"
        assert first_env["PYTHONDONTWRITEBYTECODE"] == "1"
        for args, kwargs in calls:
            assert args[0] == module.sys.executable
            assert kwargs["cwd"] == str(root)
            assert kwargs["check"] is False
            assert kwargs["timeout"] == module.CHILD_TIMEOUT_SECONDS


def test_real_child_cannot_observe_hostile_python_controls():
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        probe = root / "probe.py"
        probe.write_text(
            "import os, sys\n"
            "blocked = %r\n"
            "assert all(name not in os.environ for name in blocked)\n"
            "assert os.environ['PYTHONUTF8'] == '1'\n"
            "assert os.environ['PYTHONIOENCODING'] == 'utf-8'\n"
            "assert os.environ['PYTHONNOUSERSITE'] == '1'\n"
            "assert os.environ['PYTHONDONTWRITEBYTECODE'] == '1'\n"
            "assert os.environ['QS3D_SENTINEL'] == 'keep-me'\n"
            % (module.PYTHON_ENVIRONMENT_CONTROLS,),
            encoding="utf-8",
        )
        source = os.environ.copy()
        for name in module.PYTHON_ENVIRONMENT_CONTROLS:
            source[name] = "/tmp/hostile"
        source["QS3D_SENTINEL"] = "keep-me"
        completed = subprocess.run(
            [module.sys.executable, str(probe)],
            cwd=str(root),
            env=module.build_child_env(source),
            check=False,
            timeout=30,
        )
        assert completed.returncode == 0


def main():
    test_python_controls_are_removed_and_required_environment_is_preserved()
    test_source_mapping_is_not_mutated()
    test_main_passes_one_sanitized_environment_to_every_child()
    test_real_child_cannot_observe_hostile_python_controls()
    print("PASS: aggregate preflight child Python environment is deterministic and sanitized.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
