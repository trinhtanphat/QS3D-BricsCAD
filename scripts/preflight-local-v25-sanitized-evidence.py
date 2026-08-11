#!/usr/bin/env python3
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "scripts/export-local-v25-sanitized-summary.py"
RUNNER = ROOT / "scripts/run-local-v25-qualification.ps1"
TEMPLATE = ROOT / "docs/LOCAL-V25-RESULT-TEMPLATE.md"
errors = []

if not EXPORTER.is_file():
    errors.append("missing scripts/export-local-v25-sanitized-summary.py")
    text = ""
else:
    text = EXPORTER.read_text(encoding="utf-8")
    for needle in (
        "qualification-summary.md",
        "sanitized summary",
        "automatedGateStatus",
        "runtimeSmokeStatus",
        "fullInteractiveMatrixStatus",
        "customerReleaseQualified",
        "runtime was explicitly skipped",
        "NOT PROVED BY THIS SUMMARY",
        "private DWG names/content",
        "raw error messages",
        "SAFE_STEP_NAMES",
        "sanitized_step_name",
        "(redacted label)",
    ):
        if needle not in text:
            errors.append("sanitized evidence exporter missing contract token: " + needle)

if not RUNNER.is_file():
    errors.append("missing scripts/run-local-v25-qualification.ps1")
elif text:
    runner_text = RUNNER.read_text(encoding="utf-8")
    runner_step_names = re.findall(r'Invoke-QualificationStep\s+"([^"]+)"', runner_text)
    if not runner_step_names:
        errors.append("local V25 qualification runner has no discoverable fixed step names")
    for step_name in runner_step_names:
        if json.dumps(step_name) not in text:
            errors.append("sanitized evidence exporter allowlist missing canonical runner step: " + step_name)

if not TEMPLATE.is_file():
    errors.append("missing docs/LOCAL-V25-RESULT-TEMPLATE.md")
else:
    template_text = TEMPLATE.read_text(encoding="utf-8")
    for needle in (
        "docs/LOCAL-V25-QUALIFICATION.md",
        "docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md",
        "export-local-v25-sanitized-summary.py",
        "qualification-summary.md",
        "no install path",
        "NOT IMPLEMENTED",
        "NOT QUALIFIED",
        "GitHub Actions remain manual-only",
    ):
        if needle not in template_text:
            errors.append("sanitized V25 result template missing handoff token: " + needle)

if not errors:
    fixture = {
        "schema": 2,
        "status": "PASS",
        "automatedGateStatus": "PASS",
        "sourceBuildStatus": "PASS",
        "runtimeSmokeStatus": "NOT_RUN",
        "fullInteractiveMatrixStatus": "NOT_RUN",
        "customerReleaseQualified": False,
        "qualificationScope": "source-build",
        "exactSha": "a" * 40,
        "branch": "main",
        "runnerUser": "PRIVATE_USER_SENTINEL",
        "interactive": True,
        "bricsCadDir": r"C:\\Users\\PRIVATE_USER_SENTINEL\\Program Files\\Bricsys\\BricsCAD V25",
        "pluginDll": r"D:\\PRIVATE_BUILD_SENTINEL\\QS3D.BricsCAD.V25.dll",
        "pluginSha256": "b" * 64,
        "runtimeSkipped": True,
        "runtimeMetadata": r"C:\\PRIVATE_RUNTIME_SENTINEL\\runtime-metadata.json",
        "packageRequested": False,
        "releaseTag": "v0.1.0-preview.2",
        "manualScenarioChecklist": "docs/LOCAL-V25-QUALIFICATION.md",
        "steps": [
            {
                "name": "Core deterministic smoke suite",
                "status": "PASS",
                "error": r"PRIVATE_ERROR_SENTINEL C:\\Customer\\Acme\\secret.dwg",
            },
            {
                "name": "Licensed V25 NETLOAD / Ribbon / Palette runtime probe",
                "status": "SKIPPED",
                "error": "PRIVATE_STEP_ERROR_SENTINEL",
            },
            {
                "name": r"PRIVATE_STEP_NAME_SENTINEL C:\\Customer\\Acme\\secret-step.dwg",
                "status": "FAIL",
            },
            {
                "name": "![PRIVATE_MARKDOWN_STEP_SENTINEL](file:///home/private/customer.dwg)",
                "status": "FAIL",
            },
        ],
        "error": r"PRIVATE_FATAL_SENTINEL C:\\Customer\\Acme\\secret.dwg",
    }

    with tempfile.TemporaryDirectory(prefix="qs3d-v25-sanitize-") as temp_dir:
        temp = Path(temp_dir)
        source = temp / "qualification.json"
        output = temp / "qualification-summary.md"
        source.write_text(json.dumps(fixture), encoding="utf-8")
        completed = subprocess.run(
            [sys.executable, str(EXPORTER), "--input", str(source), "--output", str(output)],
            cwd=str(ROOT),
            check=False,
            capture_output=True,
            text=True,
            timeout=30,
        )
        if completed.returncode != 0:
            errors.append("sanitized evidence exporter failed on deterministic fixture: " + completed.stderr.strip())
        elif not output.is_file():
            errors.append("sanitized evidence exporter did not create qualification-summary.md")
        else:
            summary = output.read_text(encoding="utf-8")
            for forbidden in (
                "PRIVATE_USER_SENTINEL",
                "PRIVATE_BUILD_SENTINEL",
                "PRIVATE_RUNTIME_SENTINEL",
                "PRIVATE_ERROR_SENTINEL",
                "PRIVATE_STEP_ERROR_SENTINEL",
                "PRIVATE_STEP_NAME_SENTINEL",
                "PRIVATE_MARKDOWN_STEP_SENTINEL",
                "PRIVATE_FATAL_SENTINEL",
                "secret.dwg",
                "secret-step.dwg",
                "C:\\Customer",
                "/home/private",
                "file:///",
                "bricsCadDir",
                "pluginDll",
                "runtimeMetadata",
                "runnerUser",
            ):
                if forbidden in summary:
                    errors.append("sanitized summary leaked private/raw field: " + forbidden)
            for required in (
                "`" + ("a" * 40) + "`",
                "`" + ("b" * 64) + "`",
                "Automated gate status: **PASS**",
                "Source/build status: **PASS**",
                "Runtime smoke status: **NOT_RUN**",
                "Full interactive/private-DWG matrix: **NOT_RUN**",
                "Customer release qualified: **NO**",
                "Qualification scope: `source-build`",
                "Runtime skipped: **YES**",
                "This result cannot qualify a customer release",
                "Core deterministic smoke suite",
                "Licensed V25 NETLOAD / Ribbon / Palette runtime probe",
                "Step 3 (redacted label)",
                "Step 4 (redacted label)",
                "Manual/private-DWG checklist: **NOT PROVED BY THIS SUMMARY**",
                "Known blockers: `SANITIZED TEXT ONLY`",
            ):
                if required not in summary:
                    errors.append("sanitized summary missing safe handoff field: " + required)

print("QS3D local V25 sanitized evidence preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: local V25 qualification evidence can be exported to a deterministic Markdown handoff without carrying machine/user/path/private-DWG/raw-error or untrusted step-label fields, preserves canonical runner step labels and scoped schema-v2 gate states, and keeps runtime-skip visibly non-release-qualified.")
