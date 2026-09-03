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
publisher_binding = "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'"
admission_result = "if ($null -eq $candidateIdentity) { throw 'V26 candidate semantic admission/publication returned no identity.' }"
release_job = "  release:\n"
download = "actions/download-artifact@"

release_pos = text.find(release_job)
qualify_pos = text.find("  qualify:\n")

if release_pos < 0:
    errors.append("V26 manual release workflow must define the release job")
    release_text = ""
else:
    release_text = text[release_pos:]

# Publication now executes inside assert-v26-candidate-identity.ps1 while the
# qualified candidate generations and publisher script are held open. The
# workflow must therefore bind the publisher into both signed/unsigned
# admission branches and must not invoke it a second time by pathname.
if text.count(publisher_call) != 0:
    errors.append(
        f"V26 manual release workflow must not invoke publish-v26-release.ps1 outside held candidate admission; found {text.count(publisher_call)} direct call(s)"
    )

if release_text.count(publisher_binding) != 2:
    errors.append(
        f"V26 release job must bind publish-v26-release.ps1 into both signed and unsigned candidate-admission branches; found {release_text.count(publisher_binding)} binding(s)"
    )

if release_text:
    download_pos = release_text.find(download)
    first_binding_pos = release_text.find(publisher_binding, download_pos + 1 if download_pos >= 0 else 0)
    second_binding_pos = release_text.find(
        publisher_binding,
        first_binding_pos + len(publisher_binding) if first_binding_pos >= 0 else 0,
    )
    admission_pos = release_text.find(
        admission_result,
        second_binding_pos + len(publisher_binding) if second_binding_pos >= 0 else 0,
    )

    if min(download_pos, first_binding_pos, second_binding_pos, admission_pos) < 0 or not (
        download_pos < first_binding_pos < second_binding_pos < admission_pos
    ):
        errors.append(
            "V26 publisher wiring must be downloaded qualified artifact -> signed/unsigned held publisher admission -> semantic admission/publication result validation"
        )

# The release job, not the qualifying self-hosted job, owns publication authority.
if qualify_pos >= 0 and release_pos > qualify_pos:
    qualify_text = text[qualify_pos:release_pos]
    if publisher_call in qualify_text or publisher_binding in qualify_text:
        errors.append("V26 publisher authority must not be wired into the qualify job")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS V26 manual release publisher workflow wiring")
