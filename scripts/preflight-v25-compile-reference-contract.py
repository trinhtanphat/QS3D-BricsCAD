#!/usr/bin/env python3
from pathlib import Path, PureWindowsPath
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
HELPER = ROOT / "scripts/acquire-v25-compile-references.ps1"
WORKFLOWS = {
    "V25 integration": ROOT / ".github/workflows/bricscad-v25.yml",
    "manual V25 release": ROOT / ".github/workflows/release-v25.yml",
    "cloud V25 release": ROOT / ".github/workflows/release-v25-cloud.yml",
}
BASELINE_REFERENCES = {"BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"}
errors = []

if not PROJECT.is_file():
    errors.append(f"missing V25 project: {PROJECT.relative_to(ROOT)}")
    required_files = set()
else:
    try:
        project_root = ET.fromstring(PROJECT.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, ET.ParseError) as exc:
        errors.append(f"cannot parse V25 project: {exc}")
        required_files = set()
    else:
        required_files = set()
        prefix = "$(BRICSCAD_V25_DIR)\\"
        for reference in project_root.findall(".//Reference"):
            hint = reference.findtext("HintPath")
            if not hint or not hint.startswith(prefix):
                continue
            filename = PureWindowsPath(hint).name
            if filename:
                required_files.add(filename)

        missing_baseline = sorted(BASELINE_REFERENCES - required_files)
        if missing_baseline:
            errors.append(
                "V25 project no longer exposes required managed references: "
                + ", ".join(missing_baseline)
            )

workflow_text = {}
for label, path in WORKFLOWS.items():
    if not path.is_file():
        errors.append(f"missing {label} workflow: {path.relative_to(ROOT)}")
        continue
    try:
        text = path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {label} workflow: {exc}")
        continue
    workflow_text[label] = text
    for filename in sorted(required_files):
        if filename not in text:
            errors.append(
                f"{label} does not mention project-required compile reference {filename}"
            )

integration = workflow_text.get("V25 integration", "")
if 'BRICSCAD_V25_DIR\\TD_MgdBrep.dll' not in integration:
    errors.append("V25 integration workflow does not fail fast on missing TD_MgdBrep.dll")

manual = workflow_text.get("manual V25 release", "")
manual_marker = "@('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')"
if manual_marker not in manual:
    errors.append("manual V25 release reference gate is not synchronized with TD_MgdBrep.dll")

cloud = workflow_text.get("cloud V25 release", "")
shared_helper_marker = ".\\scripts\\acquire-v25-compile-references.ps1"
if shared_helper_marker in cloud:
    cloud_markers = {
        "shared helper result binding": '"BRICSCAD_V25_DIR=$bricsDir"',
        "shared helper exact output selection": "Select-Object -Last 1",
    }
    for label, marker in cloud_markers.items():
        if marker not in cloud:
            errors.append(f"cloud V25 release is missing {label} contract")

    if not HELPER.is_file():
        errors.append("cloud V25 release delegates compile-reference acquisition but the shared helper is missing")
    else:
        try:
            helper = HELPER.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exc:
            errors.append(f"cannot read shared V25 compile-reference helper: {exc}")
        else:
            for filename in sorted(required_files):
                if filename not in helper:
                    errors.append(
                        f"shared V25 compile-reference helper does not validate project-required reference {filename}"
                    )
            helper_markers = {
                "runtime candidate discovery": "-Filter 'BrxMgd.dll'",
                "Brx co-location": "(Join-Path $_ 'BrxMgd.dll')",
                "TD co-location": "(Join-Path $_ 'TD_Mgd.dll')",
                "BREP co-location": "(Join-Path $_ 'TD_MgdBrep.dll')",
                "fail-closed co-location result": "if ([string]::IsNullOrWhiteSpace($bricsDir))",
                "resolved directory output": "Write-Output $bricsDir",
            }
            for label, marker in helper_markers.items():
                if marker not in helper:
                    errors.append(
                        f"shared V25 compile-reference helper is missing {label} contract: {marker}"
                    )
else:
    # Legacy inline acquisition remains accepted only when it still carries the
    # complete compile-reference discovery/co-location contract itself.
    cloud_markers = {
        "BREP discovery": "-Filter 'TD_MgdBrep.dll'",
        "BREP co-location": "(Join-Path $_ 'TD_MgdBrep.dll')",
        "complete discovery count": "$brx.Count -lt 1 -or $td.Count -lt 1 -or $brep.Count -lt 1",
        "complete validation list": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
    }
    for label, marker in cloud_markers.items():
        if marker not in cloud:
            errors.append(f"cloud V25 release is missing {label} contract for TD_MgdBrep.dll")

if errors:
    print("V25 compile-reference contract preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print(
    "V25 compile-reference contract preflight PASS: "
    + ", ".join(sorted(required_files))
    + " are synchronized across integration, manual-release, and cloud-release gates/shared acquisition."
)
