#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
AGENTS = ROOT / "AGENTS.md"
REMOTE_SCOPE = ROOT / "docs" / "REMOTE-AGENT-SCOPE.md"
INBOX_REF = "`docs/LOCAL-AGENT-INBOX.md`"

VALID_PRIORITIES = {"P0", "P1", "P2"}
VALID_STATUSES = {"OPEN", "IN_PROGRESS", "PASS", "BLOCKED"}
REQUIRED_FIELDS = (
    "Priority",
    "Status",
    "Area",
    "Why local",
    "Scenario",
    "Evidence required",
    "Evidence",
    "Related docs",
    "Updated",
)
PLACEHOLDER_EVIDENCE = {"", "PENDING_LOCAL", "TBD", "TODO", "N/A", "NONE", "-"}


def fail(message: str) -> None:
    print(f"[FAIL] local-agent-handoff: {message}", file=sys.stderr)


def field(section: str, name: str) -> str | None:
    aliases = (name, "Related source/docs") if name == "Related docs" else (name,)
    for candidate in aliases:
        match = re.search(rf"^- {re.escape(candidate)}:\s*(.*?)\s*$", section, re.MULTILINE)
        if match:
            return match.group(1).strip()
    return None


def main() -> int:
    errors: list[str] = []

    for path in (INBOX, AGENTS, REMOTE_SCOPE):
        if not path.is_file():
            errors.append(f"missing required file: {path.relative_to(ROOT)}")

    if errors:
        for error in errors:
            fail(error)
        return 1

    inbox = INBOX.read_text(encoding="utf-8")
    agents = AGENTS.read_text(encoding="utf-8")
    remote_scope = REMOTE_SCOPE.read_text(encoding="utf-8")

    if "single live queue for LOCAL_ONLY work" not in inbox:
        errors.append("inbox must declare itself as the single live LOCAL_ONLY queue")
    if "LOCAL_ONLY" not in agents or "## Unavailable-work handoff" not in agents:
        errors.append("AGENTS.md lost the LOCAL_ONLY handoff contract")
    if INBOX_REF not in agents:
        errors.append("AGENTS.md must route local work through docs/LOCAL-AGENT-INBOX.md")
    if "same task branch/PR" not in agents:
        errors.append("AGENTS.md must require same-branch registration of new/changed LOCAL_ONLY scenarios")
    if "LOCAL_ONLY" not in remote_scope or "LOCAL_PASS" not in remote_scope:
        errors.append("REMOTE-AGENT-SCOPE.md lost LOCAL_ONLY/LOCAL_PASS vocabulary")
    if INBOX_REF not in remote_scope:
        errors.append("REMOTE-AGENT-SCOPE.md must route local work through docs/LOCAL-AGENT-INBOX.md")
    if "same batch" not in remote_scope:
        errors.append("REMOTE-AGENT-SCOPE.md must require same-batch inbox updates")

    matches = list(
        re.finditer(
            r"^## (?P<id>LOCAL-\d{3}) — (?P<title>.+?)\s*$",
            inbox,
            re.MULTILINE,
        )
    )
    if not matches:
        errors.append("inbox has no LOCAL-### work items")

    seen: set[str] = set()
    for index, match in enumerate(matches):
        item_id = match.group("id")
        if item_id in seen:
            errors.append(f"duplicate item id: {item_id}")
            continue
        seen.add(item_id)

        start = match.end()
        end = matches[index + 1].start() if index + 1 < len(matches) else len(inbox)
        section = inbox[start:end]

        values: dict[str, str] = {}
        for name in REQUIRED_FIELDS:
            value = field(section, name)
            if value is None or not value:
                errors.append(f"{item_id}: missing/non-empty field '{name}'")
            else:
                values[name] = value

        priority = values.get("Priority")
        status = values.get("Status")
        evidence = values.get("Evidence", "")

        if priority and priority not in VALID_PRIORITIES:
            errors.append(f"{item_id}: invalid Priority '{priority}'")
        if status and status not in VALID_STATUSES:
            errors.append(f"{item_id}: invalid Status '{status}'")
        if status == "PASS" and evidence.upper() in PLACEHOLDER_EVIDENCE:
            errors.append(f"{item_id}: PASS requires concrete evidence tied to an exact SHA")

        related = values.get("Related docs", "")
        if related and "`docs/" not in related:
            errors.append(f"{item_id}: Related docs must reference at least one docs/ path")

    if errors:
        for error in errors:
            fail(error)
        return 1

    print(
        f"[PASS] local-agent-handoff: {len(matches)} structured LOCAL_ONLY items; "
        "canonical inbox routing + same-branch priority/status/evidence contract valid"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
