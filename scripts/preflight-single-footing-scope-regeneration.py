#!/usr/bin/env python3
"""Execute host-free production scope/editor regression; never launch CAD."""
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/test-single-footing-scope-regeneration.ps1"


def main() -> int:
    shell = shutil.which("pwsh")
    if not shell or not SCRIPT.is_file():
        raise SystemExit("FAIL: PowerShell 7 and the SingleFooting scope regression are required")
    try:
        result = subprocess.run(
            [shell, "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(SCRIPT)],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=60, check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SystemExit(f"FAIL: SingleFooting scope regression could not finish: {error}") from error
    output = result.stdout + result.stderr
    expected = (
        "PASS: production scope routing retains specialized Family presentation",
        "PASS: actual six-mm renderer/row setter invokes native-regeneration boundary",
        "PASS: initial and replacement ViewModels register presenter before DataContext publication",
    )
    if result.returncode != 0 or any(marker not in output for marker in expected):
        print(output)
        raise SystemExit(f"FAIL: SingleFooting scope regression exited {result.returncode} or omitted evidence")
    print("PASS: executable SingleFooting Family-scope routing, six-mm editor and mutation-boundary regression (host-free)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
