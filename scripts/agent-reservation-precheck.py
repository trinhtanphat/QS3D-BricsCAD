#!/usr/bin/env python3
"""Fail closed before repository mutation when an earlier Reservation-v2 owner exists."""

from __future__ import annotations

import argparse
import json
import os
import re
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone

PROTOCOL_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?reservation[- ]protocol\s*:\s*([^\r\n]*)$")
LANE_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?lane[- ]key\s*:\s*([^\r\n]*)$")
OWNER_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?canonical owner/session\s*:\s*([^\r\n]*)$")
CARRIER_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?canonical carrier\s*:\s*([^\r\n]*)$")
OWNERSHIP_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?ownership[- ]key\s*:\s*([^\r\n]*)$")
EXPECTED_PATHS_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?expected[- ]paths\s*:\s*([^\r\n]*)$")

VALID_OWNERSHIP_RE = re.compile(r"^[a-z0-9][a-z0-9._/-]{4,120}$")
VALID_OWNER_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._:@/|+~-]{5,200}$")
MAX_EXPECTED_PATHS = 64
MAX_PAGES = 10


def _strip_inline_comment(raw: str) -> str:
    return str(raw or "").split("<!--", 1)[0].strip()


def _single_field(pattern: re.Pattern[str], body: str | None, label: str) -> str | None:
    values = [_strip_inline_comment(match.group(1)) for match in pattern.finditer(body or "")]
    values = [value for value in values if value]
    unique: list[str] = []
    for value in values:
        if value not in unique:
            unique.append(value)
    if len(unique) > 1:
        raise ValueError(f"conflicting {label} values: {', '.join(unique)}")
    return unique[0] if unique else None


def normalize_ownership_key(raw: str) -> str:
    value = _strip_inline_comment(raw).lower()
    if not VALID_OWNERSHIP_RE.fullmatch(value):
        raise ValueError(
            "Ownership-Key must be a stable 5-121 character lowercase semantic key "
            "using letters, digits, '.', '_', '/', or '-'"
        )
    if re.fullmatch(r"(?:issue|task|lane)[-_.:/]?\d+", value):
        raise ValueError("Ownership-Key must describe semantic ownership, not merely an issue/task/lane number")
    return value


def parse_expected_paths(raw: str) -> list[str]:
    value = _strip_inline_comment(raw)
    if not value:
        raise ValueError("Expected-Paths is empty")
    parts = [part.strip().replace("\\", "/") for part in value.split(";")]
    if any(not part for part in parts):
        raise ValueError("Expected-Paths contains an empty entry")
    if len(parts) > MAX_EXPECTED_PATHS:
        raise ValueError(f"Expected-Paths exceeds {MAX_EXPECTED_PATHS} entries")

    result: list[str] = []
    for part in parts:
        if part.startswith("/") or part.startswith("./") or part.startswith("../"):
            raise ValueError(f"Expected-Paths entry must be repository-relative: {part}")
        if "//" in part or "/../" in f"/{part}/" or "/./" in f"/{part}/":
            raise ValueError(f"Expected-Paths entry contains unsafe traversal: {part}")
        if any(ch in part for ch in "*?[]{}"):
            raise ValueError(f"Expected-Paths does not accept glob syntax: {part}")
        normalized = part.rstrip("/") + "/" if part.endswith("/") else part
        if normalized not in result:
            result.append(normalized)
    return result


def path_matches_claim(path: str, claim: str) -> bool:
    normalized_path = str(path).replace("\\", "/").lstrip("/")
    if claim.endswith("/"):
        return normalized_path.startswith(claim)
    return normalized_path == claim


def path_claims_overlap(left: str, right: str) -> bool:
    left = str(left).replace("\\", "/").lstrip("/")
    right = str(right).replace("\\", "/").lstrip("/")
    if left == right:
        return True
    if left.endswith("/") and path_matches_claim(right.rstrip("/"), left):
        return True
    if right.endswith("/") and path_matches_claim(left.rstrip("/"), right):
        return True
    return False


def overlapping_claims(left: list[str], right: list[str]) -> list[tuple[str, str]]:
    overlaps: list[tuple[str, str]] = []
    for lvalue in left:
        for rvalue in right:
            if path_claims_overlap(lvalue, rvalue):
                overlaps.append((lvalue, rvalue))
    return overlaps


def parse_iso8601(raw: str) -> datetime:
    value = str(raw or "").strip()
    if not value:
        raise ValueError("missing created_at timestamp")
    if value.endswith("Z"):
        value = value[:-1] + "+00:00"
    parsed = datetime.fromisoformat(value)
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def reservation_order(issue: dict) -> tuple[datetime, int]:
    return parse_iso8601(str(issue.get("created_at") or "")), int(issue.get("number") or 0)


def _reservation_core(issue: dict, *, strict: bool) -> tuple[str, list[str]] | None:
    if issue.get("pull_request"):
        return None
    if str(issue.get("state") or "").lower() != "open":
        return None

    body = str(issue.get("body") or "")
    protocol = (_single_field(PROTOCOL_RE, body, "Reservation-Protocol") or "").lower()
    if protocol != "v2":
        if strict:
            raise ValueError("current Issue must state exact 'Reservation-Protocol: v2'")
        return None

    ownership_raw = _single_field(OWNERSHIP_RE, body, "Ownership-Key")
    expected_raw = _single_field(EXPECTED_PATHS_RE, body, "Expected-Paths")
    if ownership_raw is None or expected_raw is None:
        if strict:
            missing = "Ownership-Key" if ownership_raw is None else "Expected-Paths"
            raise ValueError(f"current Reservation-v2 Issue is missing {missing}")
        return None

    ownership = normalize_ownership_key(ownership_raw)
    expected_paths = parse_expected_paths(expected_raw)
    return ownership, expected_paths


def _validate_current_identity(issue: dict) -> None:
    number = int(issue.get("number") or 0)
    if number <= 0:
        raise ValueError("current Issue number is missing or invalid")
    if issue.get("pull_request"):
        raise ValueError(f"#{number} resolves to a pull request, not a reservation Issue")
    if str(issue.get("state") or "").lower() != "open":
        raise ValueError(f"reservation Issue #{number} is not open")

    body = str(issue.get("body") or "")
    lane = (_single_field(LANE_RE, body, "Lane-Key") or "").lower()
    if lane != f"issue-{number}":
        raise ValueError(f"reservation Issue #{number} must state exact 'Lane-Key: issue-{number}'")

    owner = _single_field(OWNER_RE, body, "Canonical owner/session")
    if owner is None or not VALID_OWNER_RE.fullmatch(owner):
        raise ValueError("Canonical owner/session is missing or invalid")

    carrier = _single_field(CARRIER_RE, body, "Canonical carrier")
    if carrier is None or not carrier.startswith("agent/") or f"issue-{number}" not in carrier:
        raise ValueError(
            f"Canonical carrier for Issue #{number} must be an agent/** branch containing issue-{number}"
        )

    _reservation_core(issue, strict=True)


def find_preacquisition_conflicts(current_issue: dict, open_issues: list[dict]) -> list[dict]:
    """Return earlier valid Reservation-v2 ownership/path conflicts for current_issue."""
    current_number = int(current_issue.get("number") or 0)
    if current_number <= 0:
        raise ValueError("current Issue number is missing or invalid")
    current_core = _reservation_core(current_issue, strict=True)
    if current_core is None:
        raise ValueError("current Reservation-v2 Issue is not active")
    current_ownership, current_paths = current_core
    current_order = reservation_order(current_issue)

    conflicts: list[dict] = []
    for peer in open_issues:
        if not isinstance(peer, dict) or peer.get("pull_request"):
            continue
        try:
            peer_number = int(peer.get("number") or 0)
        except (TypeError, ValueError):
            continue
        if peer_number <= 0 or peer_number == current_number:
            continue
        try:
            if reservation_order(peer) >= current_order:
                continue
            peer_core = _reservation_core(peer, strict=False)
            if peer_core is None:
                continue
            peer_ownership, peer_paths = peer_core
        except (TypeError, ValueError):
            # Malformed peer metadata is not promoted into a valid Reservation-v2 owner.
            continue

        if peer_ownership == current_ownership:
            conflicts.append(
                {
                    "issue_number": peer_number,
                    "kind": "ownership-key",
                    "ownership_key": current_ownership,
                }
            )

        overlaps = overlapping_claims(current_paths, peer_paths)
        if overlaps:
            conflicts.append(
                {
                    "issue_number": peer_number,
                    "kind": "expected-path",
                    "overlaps": overlaps,
                }
            )

    return sorted(
        conflicts,
        key=lambda item: (int(item["issue_number"]), str(item["kind"])),
    )


def _request_json(url: str, token: str) -> object:
    headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": "Bearer " + token,
        "User-Agent": "qs3d-agent-reservation-precheck",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.loads(response.read().decode("utf-8"))


def _fetch_issue(api_url: str, repository: str, issue_number: int, token: str) -> dict:
    owner_repo = urllib.parse.quote(repository, safe="/")
    url = f"{api_url.rstrip('/')}/repos/{owner_repo}/issues/{issue_number}"
    payload = _request_json(url, token)
    if not isinstance(payload, dict):
        raise RuntimeError(f"GitHub Issue #{issue_number} response was not an object")
    return payload


def _fetch_open_issues(api_url: str, repository: str, token: str) -> list[dict]:
    owner_repo = urllib.parse.quote(repository, safe="/")
    collected: list[dict] = []
    for page in range(1, MAX_PAGES + 1):
        url = (
            f"{api_url.rstrip('/')}/repos/{owner_repo}/issues"
            f"?state=open&per_page=100&page={page}"
        )
        payload = _request_json(url, token)
        if not isinstance(payload, list):
            raise RuntimeError("GitHub open-Issues response was not a list")
        page_items = [item for item in payload if isinstance(item, dict)]
        collected.extend(page_items)
        if len(page_items) < 100:
            return collected
    raise RuntimeError(
        f"GitHub open-Issues list exceeded {MAX_PAGES * 100} entries; refusing incomplete reservation scan"
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Check a Reservation-v2 Issue for earlier semantic/path ownership before repository mutation."
    )
    parser.add_argument("--issue", type=int, required=True, help="Reservation Issue number to acquire")
    parser.add_argument(
        "--repository",
        default=os.environ.get("GITHUB_REPOSITORY", ""),
        help="owner/repo (defaults to GITHUB_REPOSITORY)",
    )
    parser.add_argument(
        "--api-url",
        default=os.environ.get("GITHUB_API_URL", "https://api.github.com"),
        help="GitHub API base URL",
    )
    args = parser.parse_args()

    repository = str(args.repository or "").strip()
    token = str(os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN") or "").strip()
    if not repository:
        print("ERROR: --repository or GITHUB_REPOSITORY is required")
        return 2
    if not token:
        print("ERROR: GITHUB_TOKEN or GH_TOKEN is required for the authenticated reservation scan")
        return 2

    try:
        current = _fetch_issue(args.api_url, repository, int(args.issue), token)
        _validate_current_identity(current)
        open_issues = _fetch_open_issues(args.api_url, repository, token)
        conflicts = find_preacquisition_conflicts(current, open_issues)
    except (
        OSError,
        RuntimeError,
        TypeError,
        ValueError,
        json.JSONDecodeError,
        UnicodeError,
        urllib.error.URLError,
    ) as exc:
        print("ERROR: Reservation-v2 pre-acquisition scan failed closed:", exc)
        return 2

    if conflicts:
        print("ERROR: earlier active Reservation-v2 ownership exists; no repository mutation is allowed:")
        for item in conflicts:
            number = int(item["issue_number"])
            if item["kind"] == "ownership-key":
                print(f" - Issue #{number}: Ownership-Key {item['ownership_key']}")
            else:
                detail = ", ".join(
                    f"{left} <-> {right}" for left, right in item.get("overlaps", [])[:8]
                )
                print(f" - Issue #{number}: Expected-Paths overlap {detail}")
        print(
            "Reuse the earlier canonical carrier, or explicitly release/reassign/supersede it first. "
            "DUPLICATE_CARRIER / NO MUTATION."
        )
        return 1

    print(
        f"PASS: Issue #{int(args.issue)} has no earlier open valid Reservation-v2 "
        "Ownership-Key/Expected-Paths conflict; pre-acquisition is clear for repository mutation."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
