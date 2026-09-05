#!/usr/bin/env python3
"""Execute the actual WPF embedded viewport layout; no licensed CAD loaded."""
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
SCRIPT = ROOT / "scripts/test-workspace-property-viewport.ps1"


def validate_layout(source: str) -> None:
    minima = re.findall(r"PropertyList\.MinHeight\s*=\s*([^;]+);", source)
    if len(minima) != 2 or any(value.strip() != "0" for value in minima):
        raise ValueError("Both embedded and dedicated property viewports must fit their allocated space")


def main() -> int:
    source = LAYOUT.read_text(encoding="utf-8")
    validate_layout(source)
    # Prove that a correct dedicated branch cannot conceal the former embedded minimum.
    marker = "PropertyList.MinHeight = 0;"
    index = source.rfind(marker)
    mutant = source[:index] + source[index:].replace(marker, "PropertyList.MinHeight = 120;", 1)
    try:
        validate_layout(mutant)
    except ValueError:
        pass
    else:
        raise SystemExit("FAIL: embedded positive-minimum mutation escaped the viewport guard")
    shell = shutil.which("powershell")
    if not shell:
        raise SystemExit("FAIL: Windows PowerShell STA is required for the WPF viewport regression")
    result = subprocess.run(
        [shell, "-NoLogo", "-NoProfile", "-NonInteractive", "-STA", "-File", str(SCRIPT)],
        cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60,
    )
    expected = "PASS: production property viewport WPF regression; short/tall/resize/repair/dedicated restoration without CAD."
    if result.returncode != 0 or expected not in result.stdout:
        print(result.stdout + result.stderr)
        raise SystemExit("FAIL: executable production WPF property viewport regression")
    print("PASS: production WPF property viewport fits short/tall/resized/restored panes; positive-minimum mutation rejected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
