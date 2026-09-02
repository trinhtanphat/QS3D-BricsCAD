#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message: str) -> int:
    print("ERROR:", message)
    return 1


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
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

    if "github.event.action != 'edited'" in step or "!startsWith(github.event.pull_request.head.ref, 'agent/')" in step:
        return fail(
            "reservation/collision validation must not skip pull_request edited events for agent branches; "
            "PR metadata edits can refresh required contexts after reservation ownership changes"
        )

    required = "github.event_name == 'push' || github.event_name == 'pull_request'"
    if required not in step:
        return fail("reservation/collision gate must run for every push and pull_request validation event")

    print("PASS: shared CI revalidates reservation/collision state for every push and pull_request event, including edited PR metadata.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
