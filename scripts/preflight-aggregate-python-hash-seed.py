#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-all.py"
EXPECTED_SEED = "0"
ASSIGNMENT = 'child_env["PYTHONHASHSEED"] = "0"'


def load_build_child_env(source):
    namespace = {
        "__name__": "preflight_all_hash_seed_contract",
        "__file__": str(TARGET),
    }
    exec(compile(source, str(TARGET), "exec"), namespace, namespace)
    build_child_env = namespace.get("build_child_env")
    if not callable(build_child_env):
        raise AssertionError("preflight-all.py must define build_child_env")
    return build_child_env


def assert_contract(source):
    build_child_env = load_build_child_env(source)
    inherited = {
        "PYTHONHASHSEED": "random",
        "PYTHONPATH": "attacker-controlled",
        "KEEP_ME": "present",
    }
    child = build_child_env(inherited)
    if child.get("PYTHONHASHSEED") != EXPECTED_SEED:
        raise AssertionError(
            "aggregate preflight child environment must override inherited PYTHONHASHSEED with fixed seed 0"
        )
    if "PYTHONPATH" in child:
        raise AssertionError("existing Python environment-control sanitization regressed")
    if child.get("KEEP_ME") != "present":
        raise AssertionError("unrelated inherited child environment values must be preserved")
    if child.get("PYTHONUTF8") != "1" or child.get("PYTHONIOENCODING") != "utf-8":
        raise AssertionError("existing deterministic UTF-8 child environment contract regressed")
    if child.get("PYTHONNOUSERSITE") != "1" or child.get("PYTHONDONTWRITEBYTECODE") != "1":
        raise AssertionError("existing isolated/no-bytecode child environment contract regressed")


def require_mutation_rejection(source, mutated, label):
    if mutated == source:
        raise AssertionError("mutation probe could not modify production source: " + label)
    try:
        assert_contract(mutated)
    except AssertionError:
        return
    raise AssertionError("mutation probe was not rejected: " + label)


def main():
    source = TARGET.read_text(encoding="utf-8")
    assert_contract(source)

    if source.count(ASSIGNMENT) != 1:
        raise AssertionError("fixed PYTHONHASHSEED assignment must appear exactly once in build_child_env")

    require_mutation_rejection(
        source,
        source.replace(ASSIGNMENT, 'child_env["PYTHONHASHSEED"] = child_env.get("PYTHONHASHSEED", "0")', 1),
        "inherited hash-seed bypass",
    )
    require_mutation_rejection(
        source,
        source.replace(ASSIGNMENT, 'child_env["PYTHONHASHSEED"] = "random"', 1),
        "non-deterministic hash seed",
    )
    require_mutation_rejection(
        source,
        source.replace(ASSIGNMENT, "", 1),
        "missing fixed hash seed",
    )

    print("PASS: aggregate preflight child Python hash seed is deterministic and inherited overrides fail closed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
