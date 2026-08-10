#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
wrapper = ROOT / "scripts" / "run-local-v25-wpf-smoke.ps1"
theme_smoke = ROOT / "scripts" / "test-wpf-theme-runtime.ps1"
palette_smoke = ROOT / "scripts" / "test-wpf-palettes-runtime.ps1"
issue_index = ROOT / "docs" / "REMAINING-ISSUE-INDEX-2026-08-10.md"
agents = ROOT / "AGENTS.md"
errors = []

for path in (wrapper, theme_smoke, palette_smoke, issue_index, agents):
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
            errors.append("local WPF smoke wrapper missing boundary token: " + needle)

if issue_index.is_file():
    text = issue_index.read_text(encoding="utf-8")
    for needle in (
        "run-local-v25-wpf-smoke.ps1",
        "#72", "#73", "#74", "#75", "#76", "#77",
        "#79", "#80", "#81", "#82", "#83", "#84",
        "source-implemented / statically guarded",
        "GitHub Actions remain manual-only",
    ):
        if needle not in text:
            errors.append("remaining issue index missing local handoff/tracking token: " + needle)

if agents.is_file() and "docs/REMAINING-ISSUE-INDEX-2026-08-10.md" not in agents.read_text(encoding="utf-8"):
    errors.append("AGENTS.md must include the remaining issue index in the handoff reading path")

print("QS3D local WPF / remaining-work handoff preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: local agents have an explicit offline WPF smoke wrapper and a guarded issue index for unresolved runtime/product work without weakening runtime or CI boundaries.")
