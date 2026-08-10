#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

runner = ROOT / "scripts/run-local-v25-qualification.ps1"
runbook = ROOT / "docs/LOCAL-V25-QUALIFICATION.md"
agents = ROOT / "AGENTS.md"
gitignore = ROOT / ".gitignore"

for path in (runner, runbook, agents, gitignore):
    if not path.is_file():
        errors.append("missing local V25 qualification contract file: " + str(path.relative_to(ROOT)))

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

if agents.is_file():
    text = agents.read_text(encoding="utf-8")
    if "docs/LOCAL-V25-QUALIFICATION.md" not in text:
        errors.append("AGENTS.md must route local-capable agents to LOCAL-V25-QUALIFICATION.md")
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

print("PASS: local V25 work is exact-SHA/clean-tree gated, runs source/Core/adapter/runtime checks, records local evidence outside Git, and hands interactive/private-DWG scenarios to local-capable agents without weakening manual-only CI policy.")
