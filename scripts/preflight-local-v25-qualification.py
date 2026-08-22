#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

runner = ROOT / "scripts/run-local-v25-qualification.ps1"
runbook = ROOT / "docs/LOCAL-V25-QUALIFICATION.md"
wpf_runbook = ROOT / "docs/LOCAL-V25-WPF-SMOKE.md"
wpf_wrapper = ROOT / "scripts/run-local-v25-wpf-smoke.ps1"
agents = ROOT / "AGENTS.md"
gitignore = ROOT / ".gitignore"

for path in (runner, runbook, wpf_runbook, wpf_wrapper, agents, gitignore):
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
        "run-local-v25-wpf-smoke.ps1",
        "Offline WPF theme / Workspace / RightPanel smoke",
        "wpfSmokeStatus",
        "test-bricscad-v25-runtime.ps1",
        "qualification.json",
        "runtimeSkipped",
        "manualScenarioChecklist",
        "Working tree is dirty. Qualification must run against an exact reproducible SHA.",
        "This does not replace the manual/private-DWG scenario checklist",
        "[switch]$SignPackage",
        "SigningCertThumbprint",
        "TimestampUrl",
        "sign-v25.ps1",
        "verify-v25-signatures.ps1",
        "finalize-v25-signed-package.ps1",
        "-Confirm:$false",
        "signingRequested",
        "signingQualified",
        "signerThumbprint",
        "packageZipSha256",
        "Signed release qualification requires the real licensed V25 runtime gate.",
        "Resolve-PythonInterpreter",
        "QS3D_PYTHON",
        "-PythonPath",
        "steps = $steps.ToArray()",
    )
    for needle in required:
        if needle not in text:
            errors.append("local V25 runner missing fail-closed token: " + needle)
    if "-SkipRuntime" not in text:
        errors.append("local V25 runner must expose explicit runtime-skip state for diagnostics")
    if 'Invoke-ExternalChecked "python"' in text:
        errors.append("local V25 runner must not invoke the Windows Store python alias directly")
    if "steps = @($steps)" in text:
        errors.append("local V25 runner must not trigger PowerShell generic-list array conversion failure")

    adapter_pos = text.find('"BricsCAD V25 adapter Release build"')
    wpf_pos = text.find('"Offline WPF theme / Workspace / RightPanel smoke"')
    runtime_pos = text.find('"Licensed V25 NETLOAD / Ribbon / Palette runtime probe"')
    if min(adapter_pos, wpf_pos, runtime_pos) < 0:
        errors.append("local V25 build/WPF/runtime flow is incomplete")
    elif not adapter_pos < wpf_pos < runtime_pos:
        errors.append("local V25 flow must be adapter build -> offline WPF smoke -> licensed runtime probe")

    package_pos = text.find('"package-v25.ps1"')
    sign_pos = text.find('"sign-v25.ps1"')
    verify_pos = text.find('"verify-v25-signatures.ps1"')
    finalize_pos = text.find('"finalize-v25-signed-package.ps1"')
    if min(package_pos, sign_pos, verify_pos, finalize_pos) < 0:
        errors.append("local V25 signing flow is incomplete")
    elif not package_pos < sign_pos < verify_pos < finalize_pos:
        errors.append("local V25 signing flow must be package -> sign -> verify -> finalize")

    forbidden = (
        ".pfx",
        "SigningPassword",
        "CertificatePassword",
        "PfxPassword",
    )
    for needle in forbidden:
        if needle.lower() in text.lower():
            errors.append("local V25 runner must not accept or reference persisted private-key material/passwords: " + needle)

if wpf_wrapper.is_file():
    text = wpf_wrapper.read_text(encoding="utf-8")
    for needle in (
        "test-wpf-theme-runtime.ps1",
        "test-wpf-palettes-runtime.ps1",
        "does not replace licensed BricsCAD V25 NETLOAD",
    ):
        if needle not in text:
            errors.append("local WPF wrapper missing truthful smoke token: " + needle)

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

    for stale in (
        "scripts\\local-v25-qualification.ps1",
        "-RunRuntime",
        "-BuildPackage",
    ):
        if stale in text:
            errors.append("local V25 runbook contains stale/non-canonical runner syntax: " + stale)

if wpf_runbook.is_file():
    text = wpf_runbook.read_text(encoding="utf-8")
    for needle in (
        "early local failure detector",
        "does not launch BricsCAD",
        "offline WPF smoke PASS",
    ):
        if needle not in text:
            errors.append("local WPF runbook missing qualification-boundary token: " + needle)

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

print("PASS: local V25 work is exact-SHA/clean-tree gated, runs source/Core/adapter/offline-WPF/runtime checks, optionally signs/verifies/finalizes the exact package with a Windows-store certificate plus HTTPS timestamp, records evidence outside Git, and hands interactive/private-DWG scenarios to local-capable agents without weakening manual-only CI policy.")
