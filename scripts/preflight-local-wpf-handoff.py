#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
wrapper = ROOT / "scripts" / "run-local-v25-wpf-smoke.ps1"
theme_smoke = ROOT / "scripts" / "test-wpf-theme-runtime.ps1"
palette_smoke = ROOT / "scripts" / "test-wpf-palettes-runtime.ps1"
runbook = ROOT / "docs" / "LOCAL-V25-WPF-SMOKE.md"
errors = []

for path in (wrapper, theme_smoke, palette_smoke, runbook):
    if not path.is_file():
        errors.append("missing local WPF handoff file: " + str(path.relative_to(ROOT)))

if wrapper.is_file():
    text = wrapper.read_text(encoding="utf-8")
    for needle in (
        "test-wpf-theme-runtime.ps1",
        "test-wpf-palettes-runtime.ps1",
        "BrxMgd.dll",
        "TD_Mgd.dll",
        "This is an early local failure detector only",
        "does not replace licensed BricsCAD V25 NETLOAD",
    ):
        if needle not in text:
            errors.append("local WPF wrapper missing boundary token: " + needle)

if runbook.is_file():
    text = runbook.read_text(encoding="utf-8")
    for needle in (
        "run-local-v25-wpf-smoke.ps1",
        "offline WPF smoke PASS",
        "does not launch BricsCAD",
        "LOCAL-V25-QUALIFICATION.md",
        "LOCAL-AGENT-REMAINING-GATES-2026-08-10.md",
        "LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md",
    ):
        if needle not in text:
            errors.append("local WPF runbook missing scope token: " + needle)

print("QS3D local WPF handoff preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: local agents have an explicit offline WPF theme/palette smoke path with truthful boundaries before the licensed V25 runtime/private-DWG matrix.")
