#!/usr/bin/env python3
"""Fail closed if V26 release extraction roots consume the MSI legacy path budget."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
text = WORKFLOW.read_text(encoding="utf-8")

# Evidence from issue #5901 / run 33999192847: Windows Installer Error 1304
# occurs on this deep payload member once the full destination path crosses the
# legacy path budget. Keep a deliberate margin below MAX_PATH instead of merely
# moving the current failure by one or two characters.
HOSTED_RUNNER_TEMP = r"D:\a\_temp"
KNOWN_DEEP_V26_MEMBER = (
    r"\Bricsys\BricsCAD V26 en_US\UserDataCache\Support\en_US\DesignLibrary"
    r"\.resources\Standard Parts\Fasteners\Studs\ASME\thumbnails_x2"
    r"\ASME B18.31.2 Continuous Thread Flange Bolting Stud.png"
)
SAFE_FULL_PATH_BUDGET = 240

jobs = {
    "primary": "v26-reference-primary:",
    "fallback": "v26-reference-fallback:",
}

for label, marker in jobs.items():
    start = text.find(marker)
    if start < 0:
        print(f"ERROR: missing {label} V26 reference job marker: {marker}")
        sys.exit(1)

    next_job = re.search(r"^  [A-Za-z0-9_-]+:\s*$", text[start + len(marker):], flags=re.MULTILINE)
    end = len(text) if next_job is None else start + len(marker) + next_job.start()
    body = text[start:end]

    match = re.search(
        r"\$extract\s*=\s*Join-Path\s+\$env:RUNNER_TEMP\s+\('([^']+)'\s*\+\s*\[Guid\]::NewGuid\(\)\.ToString\('N'\)\)",
        body,
    )
    if match is None:
        print(f"ERROR: {label} V26 reference job must create a GUID-unique extraction root under RUNNER_TEMP.")
        sys.exit(1)

    prefix = match.group(1)
    modeled_root = HOSTED_RUNNER_TEMP + "\\" + prefix + ("0" * 32)
    modeled_path = modeled_root + KNOWN_DEEP_V26_MEMBER
    if len(modeled_path) > SAFE_FULL_PATH_BUDGET:
        print(
            f"ERROR: {label} V26 extraction root prefix {prefix!r} consumes too much legacy MSI path budget: "
            f"modeled deep payload path is {len(modeled_path)} chars; require <= {SAFE_FULL_PATH_BUDGET}."
        )
        sys.exit(1)

print("PASS: V26 primary/fallback release extraction roots preserve legacy MSI path-length headroom.")
