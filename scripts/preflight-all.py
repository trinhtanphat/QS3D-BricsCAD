#!/usr/bin/env python3
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()


def discover():
    return [
        path
        for path in sorted(SCRIPTS.glob("preflight-*.py"), key=lambda p: p.name.lower())
        if path.resolve() != SELF
    ]


def main():
    gates = discover()
    if not gates:
        print("ERROR: no feature preflight gates were discovered.")
        return 1

    print("QS3D aggregate feature preflight")
    print("Discovered", len(gates), "feature gate(s):")
    for path in gates:
        print(" -", path.relative_to(ROOT))

    failed = []
    child_env = os.environ.copy()
    child_env["PYTHONUTF8"] = "1"
    child_env["PYTHONIOENCODING"] = "utf-8"
    for path in gates:
        rel = path.relative_to(ROOT)
        print("\n===", rel, "===")
        try:
            completed = subprocess.run(
                [sys.executable, str(path)],
                cwd=str(ROOT),
                check=False,
                env=child_env,
                timeout=180,
            )
        except subprocess.TimeoutExpired:
            print("ERROR:", rel, "timed out after 180 seconds.")
            failed.append((str(rel), "timeout"))
            continue
        except OSError as exc:
            print("ERROR: failed to start", rel, "-", exc)
            failed.append((str(rel), "launch"))
            continue

        if completed.returncode != 0:
            failed.append((str(rel), "exit=" + str(completed.returncode)))

    if failed:
        print("\nAggregate preflight FAILED:")
        for path, reason in failed:
            print(" -", path, reason)
        print("FAILED with", len(failed), "feature gate failure(s).")
        return 1

    print("\nPASS: all", len(gates), "discovered feature preflight gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
