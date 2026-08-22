#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "test-wpf-palettes-runtime.ps1"
TIMEOUT_SECONDS = 60


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def main() -> int:
    if not SCRIPT.is_file():
        fail("missing offline palette runner: scripts/test-wpf-palettes-runtime.ps1")

    shell = shutil.which("pwsh") or shutil.which("powershell")
    if not shell:
        fail("PowerShell is required to execute the offline palette regression")

    try:
        completed = subprocess.run(
            [shell, "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(SCRIPT)],
            cwd=str(ROOT),
            text=True,
            capture_output=True,
            timeout=TIMEOUT_SECONDS,
            check=False,
        )
    except subprocess.TimeoutExpired:
        fail(f"offline palette runner timed out after {TIMEOUT_SECONDS} seconds")
    except OSError as exc:
        fail(f"could not launch PowerShell: {exc}")

    output = (completed.stdout or "") + (completed.stderr or "")
    if completed.returncode != 0:
        print(output)
        fail(f"offline palette runner exited with {completed.returncode}")

    required = (
        "PASS: WorkspacePanel source contract is structurally valid without loading BricsCAD/plugin assemblies.",
        "PASS: RightPanel source contract is structurally valid without loading BricsCAD/plugin assemblies.",
        "PASS: offline palette qualification completed using source/XAML checks only.",
    )
    for token in required:
        if token not in output:
            print(output)
            fail(f"offline palette runner did not emit required success evidence: {token}")

    forbidden_failure_markers = (
        "Cannot convert System.Object[] to System.Xml.XmlNamespaceManager",
        "parameter Namespaces",
        "ArgumentTransformationMetadataException",
    )
    for token in forbidden_failure_markers:
        if token in output:
            print(output)
            fail(f"namespace-manager pipeline regression reappeared: {token}")

    print(
        "PASS: offline palette PowerShell executes both WorkspacePanel and RightPanel namespace-aware XPath contracts "
        "without XmlNamespaceManager pipeline unrolling or hosted BricsCAD/plugin assembly loading."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
