#!/usr/bin/env python3
"""Fail PR preflight when two active agent carriers claim the same Lane-Key."""

from __future__ import annotations

import json
import os
import re
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

EXPLICIT_LANE_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?lane[- ]key\s*:\s*([^\r\n]*)$")
ISSUE_FIELD_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?issue\s*:\s*#?(\d+)\s*(?:<!--.*)?$")
CLOSING_RE = re.compile(r"(?i)\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+#(\d+)\b")
VALID_KEY_RE = re.compile(r"^[a-z0-9][a-z0-9._-]{2,80}$")
LOCKED_PREFIXES = ("agent/", "integration/")
MAX_PAGES = 10


def _strip_inline_comment(raw: str) -> str:
    return str(raw or "").split("<!--", 1)[0].strip()


def normalize_lane_key(raw: str) -> str:
    value = _strip_inline_comment(raw).lower()
    if not value:
        raise ValueError("Lane-Key is empty")

    issue_match = re.fullmatch(r"(?:issue[-_:# ]*)?#?(\d+)", value)
    if issue_match:
        return "issue-" + issue_match.group(1)

    value = re.sub(r"\s+", "-", value)
    if not VALID_KEY_RE.fullmatch(value):
        raise ValueError(
            "Lane-Key must be issue-<number> or a stable 3-81 character lowercase key "
            "using letters, digits, '.', '_' or '-'"
        )
    return value


def _unique_or_error(values: list[str], source: str) -> str | None:
    normalized = []
    for raw in values:
        key = normalize_lane_key(raw)
        if key not in normalized:
            normalized.append(key)
    if len(normalized) > 1:
        raise ValueError(f"conflicting {source} lane keys: {', '.join(normalized)}")
    return normalized[0] if normalized else None


def extract_lane_key(body: str | None) -> str | None:
    text = body or ""
    explicit_lines = [_strip_inline_comment(match.group(1)) for match in EXPLICIT_LANE_RE.finditer(text)]
    explicit = [value for value in explicit_lines if value]
    if explicit:
        return _unique_or_error(explicit, "explicit")

    issue_fields = ["issue-" + match.group(1) for match in ISSUE_FIELD_RE.finditer(text)]
    if issue_fields:
        return _unique_or_error(issue_fields, "Issue field")

    closing = ["issue-" + number for number in CLOSING_RE.findall(text)]
    return _unique_or_error(closing, "closing reference")


def requires_lane_lock(head_ref: str, head_repo: str, repository: str, actor: str) -> bool:
    if actor == "dependabot[bot]":
        return False
    if head_repo != repository:
        return False
    return head_ref.startswith(LOCKED_PREFIXES)


def find_duplicate_carriers(current_number: int, current_key: str, open_prs: list[dict]) -> list[tuple[int, str]]:
    conflicts: list[tuple[int, str]] = []
    for candidate in open_prs:
        try:
            number = int(candidate.get("number", 0))
        except (TypeError, ValueError):
            continue
        if number == current_number:
            continue
        try:
            peer_key = extract_lane_key(candidate.get("body"))
        except ValueError as exc:
            # Malformed metadata on another PR cannot safely establish ownership.
            # Ignore it here; that PR's own preflight is responsible for rejecting it.
            print(f"WARN: PR #{number} has malformed lane metadata: {exc}")
            continue
        if peer_key == current_key:
            head = ((candidate.get("head") or {}).get("ref") or "<unknown>").strip()
            conflicts.append((number, head))
    return sorted(conflicts)


def _request_json(url: str, token: str) -> object:
    if not token:
        raise RuntimeError("GITHUB_TOKEN is required for the PR Lane-Key runtime gate")
    headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": "Bearer " + token,
        "User-Agent": "qs3d-agent-lane-collision-preflight",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.loads(response.read().decode("utf-8"))


def fetch_open_prs(api_url: str, repository: str, token: str) -> list[dict]:
    owner_repo = urllib.parse.quote(repository, safe="/")
    collected: list[dict] = []
    for page in range(1, MAX_PAGES + 1):
        url = f"{api_url.rstrip('/')}/repos/{owner_repo}/pulls?state=open&per_page=100&page={page}"
        payload = _request_json(url, token)
        if not isinstance(payload, list):
            raise RuntimeError("GitHub open-PR response was not a list")
        page_items = [item for item in payload if isinstance(item, dict)]
        collected.extend(page_items)
        if len(page_items) < 100:
            return collected
    raise RuntimeError(f"open PR list exceeded {MAX_PAGES * 100} entries; refusing incomplete collision scan")


def _event_actor(event: dict) -> str:
    sender = event.get("sender") or {}
    return str(sender.get("login") or os.environ.get("GITHUB_ACTOR") or "")


def validate_pull_request_event(event: dict, repository: str, open_prs: list[dict]) -> tuple[str | None, list[tuple[int, str]]]:
    pr = event.get("pull_request")
    if not isinstance(pr, dict):
        raise ValueError("pull_request event payload is missing pull_request object")

    head = pr.get("head") or {}
    head_repo_data = head.get("repo") or {}
    head_ref = str(head.get("ref") or "")
    head_repo = str(head_repo_data.get("full_name") or "")
    actor = _event_actor(event)

    if not requires_lane_lock(head_ref, head_repo, repository, actor):
        return None, []

    try:
        number = int(pr.get("number") or event.get("number"))
    except (TypeError, ValueError) as exc:
        raise ValueError("pull request number is missing or invalid") from exc

    lane_key = extract_lane_key(pr.get("body"))
    if lane_key is None:
        raise ValueError(
            f"PR #{number} head '{head_ref}' requires a Lane-Key in the PR body; "
            "use 'Lane-Key: issue-<number>' or a stable integration batch key"
        )

    return lane_key, find_duplicate_carriers(number, lane_key, open_prs)


def main() -> int:
    if os.environ.get("QS3D_AGENT_LANE_COLLISION_RUNTIME") != "1":
        print("PASS: agent Lane-Key collision runtime is disabled; hermetic regression remains active via preflight-all.")
        return 0

    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        print("ERROR: agent Lane-Key collision runtime may run only for pull_request events")
        return 1

    event_path = os.environ.get("GITHUB_EVENT_PATH")
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not event_path or not repository or not token:
        print("ERROR: PR Lane-Key runtime requires GITHUB_EVENT_PATH, GITHUB_REPOSITORY and GITHUB_TOKEN")
        return 1

    try:
        event = json.loads(Path(event_path).read_text(encoding="utf-8"))
        if not isinstance(event, dict):
            raise ValueError("event payload root must be an object")

        pr = event.get("pull_request") or {}
        head = pr.get("head") or {}
        head_repo = (head.get("repo") or {}).get("full_name") or ""
        head_ref = head.get("ref") or ""
        if not requires_lane_lock(str(head_ref), str(head_repo), repository, _event_actor(event)):
            print("PASS: PR is outside the same-repository agent/integration Lane-Key lock scope.")
            return 0

        api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com")
        open_prs = fetch_open_prs(api_url, repository, token)
        lane_key, conflicts = validate_pull_request_event(event, repository, open_prs)
    except (OSError, ValueError, RuntimeError, json.JSONDecodeError, urllib.error.URLError) as exc:
        print("ERROR: agent Lane-Key collision preflight failed closed:", exc)
        return 1

    if conflicts:
        print(f"ERROR: Lane-Key '{lane_key}' already has another open canonical carrier:")
        for number, head_ref in conflicts:
            print(f" - PR #{number}: {head_ref}")
        print("Close/explicitly supersede the old carrier before opening a replacement. DUPLICATE_CARRIER / NO MUTATION.")
        return 1

    print(f"PASS: Lane-Key '{lane_key}' has exactly one open carrier (this PR).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
