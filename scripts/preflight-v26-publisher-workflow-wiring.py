#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v26.yml"

if not WORKFLOW.is_file():
    print("ERROR: missing .github/workflows/release-v26.yml")
    sys.exit(1)

text = WORKFLOW.read_text(encoding="utf-8")
errors = []

publisher_call = "& .\\scripts\\publish-v26-release.ps1"
admission = "if ($null -eq $candidateIdentity) { throw 'V26 candidate semantic admission/publication returned no identity.' }"
release_job = "  release:\n"
download = "actions/download-artifact@"

if text.count(publisher_call) != 1:
    errors.append(
        f"V26 manual release workflow must invoke publish-v26-release.ps1 exactly once after candidate admission; found {text.count(publisher_call)} call(s)"
    )

release_pos = text.find(release_job)
download_pos = text.find(download, release_pos + 1 if release_pos >= 0 else 0)
admission_pos = text.find(admission, download_pos + 1 if download_pos >= 0 else 0)
publish_pos = text.find(publisher_call, admission_pos + 1 if admission_pos >= 0 else 0)

if min(release_pos, download_pos, admission_pos, publish_pos) < 0 or not (
    release_pos < download_pos < admission_pos < publish_pos
):
    errors.append(
        "V26 publisher wiring must be release job -> downloaded qualified artifact -> candidate identity admission -> publisher invocation"
    )

# Admission must bind the exact publisher script that is subsequently invoked.
if "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'" not in text:
    errors.append("V26 candidate admission must bind the exact publish-v26-release.ps1 script")

# The release job, not the qualifying self-hosted job, owns publication authority.
qualify_pos = text.find("  qualify:\n")
if qualify_pos >= 0 and release_pos > qualify_pos:
    qualify_text = text[qualify_pos:release_pos]
    if publisher_call in qualify_text:
        errors.append("V26 publisher must not execute in the qualify job")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS V26 manual release publisher workflow wiring")
