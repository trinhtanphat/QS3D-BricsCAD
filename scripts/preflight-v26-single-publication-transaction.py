#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow_path = root / ".github" / "workflows" / "release-v26.yml"
admission_path = root / "scripts" / "assert-v26-candidate-identity.ps1"

workflow = workflow_path.read_text(encoding="utf-8")
admission = admission_path.read_text(encoding="utf-8")

held_argument = "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'"
direct_publish = "& .\\scripts\\publish-v26-release.ps1"
open_script = "$scriptHeld = Open-Held -Path $AdmittedScript -Label 'V26 admitted publication script'"
parse_script = "$admittedScriptBlock = [ScriptBlock]::Create($scriptText)"
execute_script = "& $admittedScriptBlock"
post_publish_recheck = "foreach ($item in $held) { Assert-Held -Held $item -Label 'V26 candidate identity input after publication' }"


def validate(workflow_text: str, admission_text: str) -> list[str]:
    errors: list[str] = []

    # There are two mutually-exclusive workflow branches (signed/unsigned), and each
    # must bind the exact publisher path into the held candidate admission.
    if workflow_text.count(held_argument) != 2:
        errors.append("V26 release workflow must pass the held publisher into both signed and unsigned candidate-admission branches")

    # The publisher path may occur as the admission argument, but must never be
    # invoked independently by the workflow after candidate streams are released.
    direct_lines = [
        line.strip()
        for line in workflow_text.splitlines()
        if line.strip().startswith(direct_publish)
    ]
    if direct_lines:
        errors.append("V26 release workflow must not execute a second pathname publisher outside held candidate admission")

    required_admission = [open_script, parse_script, execute_script, post_publish_recheck]
    for token in required_admission:
        if token not in admission_text:
            errors.append(f"V26 candidate admission lost held publication contract: {token}")

    positions = [admission_text.find(token) for token in required_admission]
    if all(position >= 0 for position in positions) and positions != sorted(positions):
        errors.append("V26 held publisher must be opened, parsed, executed, then candidate inputs revalidated in that order")

    # The workflow result is meaningful only after semantic admission/publication.
    identity_check = "if ($null -eq $candidateIdentity) { throw 'V26 candidate semantic admission/publication returned no identity.' }"
    if identity_check not in workflow_text:
        errors.append("V26 release workflow lost candidate semantic admission/publication result validation")

    return errors


errors = validate(workflow, admission)
if errors:
    raise SystemExit("V26 single-publication transaction guard failed: " + "; ".join(errors))

mutations = [
    (
        "second workflow publisher",
        workflow + "\n          & .\\scripts\\publish-v26-release.ps1\n",
        admission,
    ),
    (
        "held publisher binding removed",
        workflow.replace(held_argument, "-AdmittedScript $null"),
        admission,
    ),
    (
        "held publisher execution removed",
        workflow,
        admission.replace(execute_script, "$null = $admittedScriptBlock", 1),
    ),
    (
        "post-publication held revalidation removed",
        workflow,
        admission.replace(post_publish_recheck, "# post-publication revalidation removed", 1),
    ),
]
for label, mutated_workflow, mutated_admission in mutations:
    if not validate(mutated_workflow, mutated_admission):
        raise SystemExit(f"V26 single-publication regression probe did not fail closed: {label}")

print("PASS V26 release executes exactly one publisher inside held candidate admission")
