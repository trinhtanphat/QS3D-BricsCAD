#!/usr/bin/env python3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
STEP_NAME = "      - name: Agent reservation / Lane-Key / path collision gate\n"
EXPECTED_IF = (
    "        if: ${{ github.event_name == 'push' || "
    "(github.event_name == 'pull_request' && "
    "(github.event.action != 'edited' || "
    "!startsWith(github.event.pull_request.head.ref, 'agent/'))) }}"
)


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
        "reservation gate must skip only edited agent/** PR events while preserving push, opened/synchronize/reopened, and edited integration/non-agent validation",
    )
    require(
        "run: python scripts/preflight-agent-lane-collision.py" in block,
        "reservation gate must continue invoking the canonical lane-collision validator",
    )

    print("PASS: edited agent PR metadata events skip only the reservation collision step")


if __name__ == "__main__":
    main()
