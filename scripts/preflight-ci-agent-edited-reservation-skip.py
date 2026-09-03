#!/usr/bin/env python3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
STEP_NAME = "      - name: Agent reservation / Lane-Key / path collision gate\n"
EXPECTED_IF = "        if: ${{ github.event_name == 'push' || github.event_name == 'pull_request' }}"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")

    require('      - edited\n' in text, "shared CI must still subscribe to pull_request edited events")

    start = text.find(STEP_NAME)
    require(start >= 0, "reservation collision step is missing from shared CI")
    end = text.find("\n      - name:", start + len(STEP_NAME))
    if end < 0:
        end = len(text)
    block = text[start:end]

    require(
        EXPECTED_IF in block,
        "reservation gate must validate every push and pull_request event, including edited agent/** PR metadata events",
    )
    require(
        "github.event.action != 'edited'" not in block
        and "!startsWith(github.event.pull_request.head.ref, 'agent/')" not in block,
        "reservation gate must not restore an edited-agent bypass",
    )
    require(
        "run: python scripts/preflight-agent-lane-collision.py" in block,
        "reservation gate must continue invoking the canonical lane-collision validator",
    )

    print("PASS: edited agent PR metadata events remain fail-closed through the reservation collision gate")


if __name__ == "__main__":
    main()
