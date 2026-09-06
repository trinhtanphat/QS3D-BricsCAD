# Aggregate Python hash-seed determinism

## Scope

This qualification covers only the Python child-interpreter environment constructed by `scripts/preflight-all.py`. It does not change feature-gate discovery, admitted source bytes, child timeouts, process-tree cleanup, output bounds, UTF-8 behavior, user-site isolation, bytecode policy, or any C01-C04 product behavior.

## Contract

Every aggregate feature gate must start with `PYTHONHASHSEED=0`, regardless of whether the parent process omitted the variable or supplied a different value. Python consumes this setting at interpreter startup, so pinning it in the environment passed to `subprocess.Popen` makes hash-dependent iteration deterministic on both Windows and Linux runners.

This seed affects Python's randomized object hashing only. It does not weaken SHA-256, package checksums, signatures, artifact identity, or other cryptographic release controls.

The aggregate must continue to strip inherited Python startup controls such as `PYTHONPATH`, retain unrelated environment variables, and keep its existing `PYTHONUTF8=1`, `PYTHONIOENCODING=utf-8`, `PYTHONNOUSERSITE=1`, and `PYTHONDONTWRITEBYTECODE=1` settings.

## Automated qualification

`scripts/preflight-aggregate-python-hash-seed.py` executes the production `build_child_env` function against an inherited environment containing `PYTHONHASHSEED=random` and verifies that the child environment contains exactly the reviewed fixed value `0`. It also checks the existing isolation/encoding contract.

Mutation probes must fail when the fixed assignment is removed, changed to `random`, or replaced with logic that preserves an inherited seed. The assignment is required exactly once so duplicate/conflicting seed writes cannot silently make the final startup value order-dependent.

Because the guard follows the standard `preflight-*.py` naming contract, `scripts/preflight-all.py` auto-discovers it. A valid carrier therefore requires a fresh exact-head Shared run in which the aggregate feature-source-guard step and the downstream required jobs succeed.

## Manual review checklist

Review the candidate diff and confirm that the production change is limited to the fixed hash-seed assignment plus this guard/runbook. Confirm that no workflow adds `continue-on-error`, no preflight is removed or skipped, no timeout is relaxed, and no release/product path is modified as a workaround.

If deterministic hash seeding ever becomes incompatible with a legitimate guard, fix the guard's explicit ordering or its deterministic contract. Do not restore randomized child hashing or accept stale GREEN evidence.
