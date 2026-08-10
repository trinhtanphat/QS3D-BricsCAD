#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

runner = ROOT / "scripts/run-local-v25-qualification.ps1"
runbook = ROOT / "docs/LOCAL-V25-QUALIFICATION.md"
remaining_gates = ROOT / "docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md"
issue_index = ROOT / "docs/REMAINING-ISSUE-INDEX-2026-08-10.md"
agents = ROOT / "AGENTS.md"
gitignore = ROOT / ".gitignore"

for path in (runner, runbook, remaining_gates, issue_index, agents, gitignore):
    if not path.is_file():
        errors.append("missing local V25 qualification/handoff contract file: " + str(path.relative_to(ROOT)))

if runner.is_file():
    text = runner.read_text(encoding="utf-8")
    required = (
        "git status --porcelain",
        "scripts/preflight-ci-manual-only.py",
        "scripts/preflight.py",
        "scripts/preflight-all.py",
        "src/QS3D.Core/QS3D.Core.csproj",
        "tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj",
        "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
        "BRICSCAD_V25_DIR",
        "test-wpf-theme-runtime.ps1",
        "test-wpf-palettes-runtime.ps1",
        "WPF theme resource smoke",
        "WPF Workspace / RightPanel layout smoke",
        "test-bricscad-v25-runtime.ps1",
        "qualification.json",
        "runtimeSkipped",
        "manualScenarioChecklist",
        "Working tree is dirty. Qualification must run against an exact reproducible SHA.",
        "This does not replace the manual/private-DWG scenario checklist",
    )
    for needle in required:
        if needle not in text:
            errors.append("local V25 runner missing fail-closed token: " + needle)
    if "-SkipRuntime" not in text:
        errors.append("local V25 runner must expose explicit runtime-skip state for diagnostics")

if runbook.is_file():
    text = runbook.read_text(encoding="utf-8")
    required = (
        "run-local-v25-qualification.ps1",
        "exact Git SHA",
        "Direct Draw",
        "Door / Opening",
        "Room / HT_PHÒNG",
        "Curtain / Glass Wall",
        "Structural / rebar families",
        "Project lifecycle",
        "Clean customer install lifecycle",
        "private DWG",
        "GitHub Actions remain manual-only",
    )
    for needle in required:
        if needle not in text:
            errors.append("local V25 runbook missing scenario/evidence token: " + needle)

if remaining_gates.is_file():
    text = remaining_gates.read_text(encoding="utf-8")
    for needle in (
        "Curtain panel-by-panel native glass",
        "physical L/T/X wall junction geometry",
        "standard-specific fabrication-grade rebar",
        "signing / installer / updater qualification",
        "Never change `FAIL`, `NOT IMPLEMENTED` or `NOT QUALIFIED` to PASS",
    ):
        if needle not in text:
            errors.append("remaining local gate handoff missing boundary token: " + needle)

if issue_index.is_file():
    text = issue_index.read_text(encoding="utf-8")
    for needle in (
        "#72", "#73", "#74", "#75", "#76", "#77",
        "#79", "#80", "#81", "#82", "#83", "#84",
        "source-implemented / statically guarded",
        "CI_POLICY.md",
        "LOCAL-V25-QUALIFICATION.md",
        "LOCAL-AGENT-REMAINING-GATES-2026-08-10.md",
    ):
        if needle not in text:
            errors.append("remaining issue index missing tracking/boundary token: " + needle)

if agents.is_file():
    text = agents.read_text(encoding="utf-8")
    if "docs/LOCAL-V25-QUALIFICATION.md" not in text:
        errors.append("AGENTS.md must route local-capable agents to LOCAL-V25-QUALIFICATION.md")
    if "docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md" not in text:
        errors.append("AGENTS.md must route unresolved local gates through LOCAL-AGENT-REMAINING-GATES")
    if "docs/REMAINING-ISSUE-INDEX-2026-08-10.md" not in text:
        errors.append("AGENTS.md must route broader unresolved work through the remaining issue index")
    if "scripts/run-local-v25-qualification.ps1" not in text:
        errors.append("AGENTS.md must name the canonical local V25 runner")

if gitignore.is_file():
    ignored = {line.strip() for line in gitignore.read_text(encoding="utf-8").splitlines() if line.strip() and not line.lstrip().startswith("#")}
    if "artifacts/" not in ignored:
        errors.append(".gitignore must keep local runtime evidence under artifacts/ untracked")

print("QS3D local V25 qualification preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: local V25 work is exact-SHA/clean-tree gated, runs source/Core/adapter/WPF/runtime checks, records local evidence outside Git, and keeps unresolved runtime/product work explicitly routed without weakening manual-only CI policy.")
