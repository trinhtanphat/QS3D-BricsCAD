#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
EXPECTED_HEAD_EXPRESSION = "${{ github.event_name == 'pull_request' && github.event.pull_request.head.sha || github.sha }}"


def fail(message: str) -> int:
    print("ERROR:", message)
    return 1


def _pull_request_trigger_block(text: str) -> str | None:
    marker = '  "pull_request":'
    if text.count(marker) != 1:
        return None

    _, tail = text.split(marker, 1)
    lines = []
    for line in tail.splitlines()[1:]:
        if line and not line.startswith(" "):
            break
        if re.match(r'^  [^\s].*:\s*$', line):
            break
        lines.append(line)
    return "\n".join(lines)


def _job_block(text: str, job_name: str, next_job_name: str) -> str | None:
    marker = f"  {job_name}:"
    next_marker = f"  {next_job_name}:"
    if text.count(marker) != 1 or text.count(next_marker) != 1:
        return None
    _, tail = text.split(marker, 1)
    block, _ = tail.split(next_marker, 1)
    return block


def _step_block(job: str, marker: str) -> str | None:
    if job.count(marker) != 1:
        return None
    _, tail = job.split(marker, 1)
    lines = []
    for line in tail.splitlines()[1:]:
        if re.match(r"^\s{6}- (?:name|uses):", line):
            break
        lines.append(line)
    return "\n".join(lines)


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")

    pull_request_trigger = _pull_request_trigger_block(text)
    if pull_request_trigger is None:
        return fail("shared CI must contain exactly one pull_request trigger")
    if not re.search(r"(?m)^\s{4}types:\s*$", pull_request_trigger):
        return fail("shared CI pull_request trigger must declare explicit event types")
    edited_events = re.findall(r"(?m)^\s{6}-\s+edited\s*$", pull_request_trigger)
    if len(edited_events) != 1:
        return fail("shared CI must subscribe exactly once to pull_request edited events")

    marker = "- name: Agent reservation / Lane-Key / path collision gate"
    if text.count(marker) != 1:
        return fail("shared CI must contain exactly one reservation/collision gate step")

    _, tail = text.split(marker, 1)
    step_lines = []
    for line in tail.splitlines()[1:]:
        if re.match(r"^\s{6}- name:", line):
            break
        step_lines.append(line)
    step = "\n".join(step_lines)

    if "preflight-agent-lane-collision.py" not in step:
        return fail("reservation/collision step must execute preflight-agent-lane-collision.py")

    if_lines = [line.strip() for line in step_lines if line.strip().startswith("if:")]
    if len(if_lines) != 1:
        return fail("reservation/collision step must contain exactly one admission condition")

    expected_if = "if: ${{ github.event_name == 'push' || github.event_name == 'pull_request' }}"
    if if_lines[0] != expected_if:
        return fail(
            "reservation/collision gate must run for every push and pull_request validation event with no action/head-ref bypass"
        )

    preflight_job = _job_block(text, "preflight", "core")
    if preflight_job is None:
        return fail("shared CI must contain one preflight job before one core job")

    checkout_marker = "- uses: actions/checkout@"
    if preflight_job.count(checkout_marker) != 1:
        return fail("preflight job must contain exactly one checkout step")
    checkout_step = _step_block(preflight_job, checkout_marker)
    if checkout_step is None:
        return fail("could not parse preflight checkout step")

    expected_ref = f"ref: {EXPECTED_HEAD_EXPRESSION}"
    if expected_ref not in checkout_step:
        return fail("preflight checkout must pin pull_request runs to the exact PR head SHA and other runs to github.sha")
    if "persist-credentials: false" not in checkout_step:
        return fail("preflight checkout must keep persisted GitHub credentials disabled")

    binding_marker = "- name: Exact candidate SHA binding"
    binding_step = _step_block(preflight_job, binding_marker)
    if binding_step is None:
        return fail("preflight job must contain exactly one exact candidate SHA binding step")
    if f"QS3D_EXPECTED_HEAD_SHA: {EXPECTED_HEAD_EXPRESSION}" not in binding_step:
        return fail("exact candidate SHA binding must derive the expected SHA from PR head or github.sha")
    if "git rev-parse HEAD" not in binding_step:
        return fail("exact candidate SHA binding must read the checked-out HEAD")
    if not re.search(r"(?i)(throw|exit\s+1).*(head|sha|candidate)", binding_step):
        return fail("exact candidate SHA binding must fail closed when the checkout does not match the expected candidate")

    print(
        "PASS: shared CI revalidates reservation state on PR edits and binds preflight execution to the exact candidate SHA."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
