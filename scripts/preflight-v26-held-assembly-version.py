#!/usr/bin/env python3
"""Exercise the actual PowerShell held-stream metadata child without a CAD host."""
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
TEST = ROOT / "scripts/test-v26-held-assembly-version.ps1"


def main() -> int:
    shell = shutil.which("pwsh")
    if not shell:
        raise SystemExit("FAIL: PowerShell 7 is required for held assembly version regression")
    try:
        result = subprocess.run(
            [shell, "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(TEST)],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=180, check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SystemExit(f"FAIL: held assembly version regression could not finish: {error}") from error
    output = result.stdout + result.stderr
    expected = (
        "PASS: actual held-stream child returns exactly one System.Version and preserves held bytes",
        "PASS: full package validator accepts matching plugin and Core versions",
        "PASS: full package validator rejects wrong assembly version",
        "PASS: full package validator rejects independent plugin and Core version mismatches",
        "PASS: actual held-stream child rejects malformed and empty images without output",
    )
    if result.returncode != 0 or any(marker not in output for marker in expected):
        print(output)
        raise SystemExit(f"FAIL: held assembly version regression exited {result.returncode} or omitted evidence")
    print("PASS: V26 held assembly metadata scalar result and rejection regressions (actual stdin child)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
