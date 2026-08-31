#!/usr/bin/env python3
"""Fail closed on duplicate agent lane/reservation/path ownership."""

from __future__ import annotations

import json
import os
import re
import subprocess
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

EXPLICIT_LANE_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?lane[- ]key\s*:\s*([^\r\n]*)$")
ISSUE_FIELD_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?issue\s*:\s*#?(\d+)\s*(?:<!--.*)?$")
CLOSING_RE = re.compile(r"(?i)\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+#(\d+)\b")
PROTOCOL_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?reservation[- ]protocol\s*:\s*([^\r\n]*)$")
OWNER_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?canonical owner/session\s*:\s*([^\r\n]*)$")
CARRIER_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?canonical carrier\s*:\s*([^\r\n]*)$")
OWNERSHIP_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?ownership[- ]key\s*:\s*([^\r\n]*)$")
EXPECTED_PATHS_RE = re.compile(r"(?im)^\s*(?:[-*]\s*)?expected[- ]paths\s*:\s*([^\r\n]*)$")

VALID_KEY_RE = re.compile(r"^[a-z0-9][a-z0-9._-]{2,80}$")
VALID_OWNERSHIP_RE = re.compile(r"^[a-z0-9][a-z0-9._/-]{4,120}$")
VALID_BRANCH_OWNER_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._:@/+~-]{5,160}$")
VALID_OWNER_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._:@/|+~-]{5,200}$")
AGENT_BRANCH_RE = re.compile(r"^agent/([^/]+)/(.+)$")
ISSUE_IN_BRANCH_RE = re.compile(r"(?:^|[/_-])issue[-_]?(\d+)(?:$|[/_-])", re.IGNORECASE)
GENERIC_OWNER_RE = re.compile(
    r"^(?:agent|ai|chatgpt|gpt(?:[-_.]?\d+(?:[-_.]\d+)*)?(?:[-_.]?(?:sol|thinking))?|"
    r"claude|codex|controller|worker|runner|bot|"
    r"c\d{1,3}|w\d{1,3}|task\d{1,4}|lane\d{1,4}|local\d{1,4})$",
    re.IGNORECASE,
)

LOCKED_PREFIXES = ("agent/", "integration/")
MARKER_PATH = "docs/agent-reservation-v2.marker"
MAX_PAGES = 10
MAX_EXPECTED_PATHS = 64


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
    normalized: list[str] = []
    for raw in values:
        key = normalize_lane_key(raw)
        if key not in normalized:
            normalized.append(key)
    if len(normalized) > 1:
        raise ValueError(f"conflicting {source} lane keys: {', '.join(normalized)}")
    return normalized[0] if normalized else None


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


def extract_lane_evidence(body: str | None) -> list[str]:
    """Collect individually valid lane claims even when peer metadata is malformed."""
    text = body or ""
    evidence: list[str] = []

    for match in EXPLICIT_LANE_RE.finditer(text):
        raw = _strip_inline_comment(match.group(1))
        if not raw:
            continue
        try:
            key = normalize_lane_key(raw)
        except ValueError:
            continue
        if key not in evidence:
            evidence.append(key)

    for match in ISSUE_FIELD_RE.finditer(text):
        key = "issue-" + match.group(1)
        if key not in evidence:
            evidence.append(key)

    for number in CLOSING_RE.findall(text):
        key = "issue-" + number
        if key not in evidence:
            evidence.append(key)

    return evidence


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
    normalized_path = path.replace("\\", "/").lstrip("/")
    if claim.endswith("/"):
        return normalized_path.startswith(claim)
    return normalized_path == claim


def path_claims_overlap(left: str, right: str) -> bool:
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


def branch_issue_number(head_ref: str) -> int | None:
    match = ISSUE_IN_BRANCH_RE.search(head_ref)
    return int(match.group(1)) if match else None


def branch_owner_token(head_ref: str) -> str | None:
    match = AGENT_BRANCH_RE.fullmatch(head_ref)
    return match.group(1) if match else None


def validate_owner_token(token: str) -> str:
    value = token.strip()
    if not VALID_BRANCH_OWNER_RE.fullmatch(value):
        raise ValueError(
            "agent branch owner token must be a stable 6-161 character repository-safe identifier"
        )
    if GENERIC_OWNER_RE.fullmatch(value):
        raise ValueError(
            f"generic branch owner token '{value}' is forbidden by reservation v2; "
            "schedule/model labels such as C01/C02/worker/controller are display metadata only"
        )
    return value


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


def reservation_order(item: dict) -> tuple[datetime, int]:
    return parse_iso8601(str(item.get("created_at") or "")), int(item.get("number") or 0)


def _run_git(args: list[str]) -> str:
    completed = subprocess.run(
        ["git", *args],
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise RuntimeError(f"git {' '.join(args)} failed: {detail}")
    return completed.stdout.strip()


def marker_activation_time() -> datetime | None:
    marker = Path(MARKER_PATH)
    if not marker.is_file():
        return None
    raw = _run_git(["log", "--diff-filter=A", "--format=%cI", "-1", "--", MARKER_PATH])
    if not raw:
        raise RuntimeError(f"{MARKER_PATH} exists but its activation commit could not be resolved")
    return parse_iso8601(raw.splitlines()[0])


def reservation_v2_required(issue: dict, activation: datetime | None) -> bool:
    protocol = (_single_field(PROTOCOL_RE, issue.get("body"), "Reservation-Protocol") or "").lower()
    if protocol:
        if protocol != "v2":
            raise ValueError(f"unsupported Reservation-Protocol '{protocol}'")
        return True
    if activation is None:
        return False
    return parse_iso8601(str(issue.get("created_at") or "")) >= activation


def validate_v2_issue(
    issue: dict,
    issue_number: int,
    head_ref: str,
    branch_token: str,
) -> tuple[str, str, str, list[str]]:
    if issue.get("pull_request"):
        raise ValueError(f"#{issue_number} resolves to a pull request, not a reservation Issue")
    if str(issue.get("state") or "").lower() != "open":
        raise ValueError(f"reservation Issue #{issue_number} is not open")

    body = issue.get("body") or ""
    protocol = (_single_field(PROTOCOL_RE, body, "Reservation-Protocol") or "").lower()
    if protocol != "v2":
        raise ValueError(f"reservation Issue #{issue_number} must state 'Reservation-Protocol: v2'")

    lane_key = extract_lane_key(body)
    expected_lane = f"issue-{issue_number}"
    if lane_key != expected_lane:
        raise ValueError(
            f"reservation Issue #{issue_number} must state exact 'Lane-Key: {expected_lane}'"
        )

    owner = _single_field(OWNER_RE, body, "Canonical owner/session")
    if owner is None or not VALID_OWNER_RE.fullmatch(owner):
        raise ValueError(
            "Canonical owner/session must be a stable 6-201 character identity; "
            "account:<login>|session:<opaque> is recommended"
        )
    if GENERIC_OWNER_RE.fullmatch(owner):
        raise ValueError("Canonical owner/session cannot be a generic schedule/model/worker label")

    carrier = _single_field(CARRIER_RE, body, "Canonical carrier")
    if carrier != head_ref:
        raise ValueError(
            f"Canonical carrier must exactly match current branch '{head_ref}', got '{carrier or '<missing>'}'"
        )

    ownership_raw = _single_field(OWNERSHIP_RE, body, "Ownership-Key")
    if ownership_raw is None:
        raise ValueError("reservation v2 requires Ownership-Key")
    ownership_key = normalize_ownership_key(ownership_raw)

    expected_raw = _single_field(EXPECTED_PATHS_RE, body, "Expected-Paths")
    if expected_raw is None:
        raise ValueError("reservation v2 requires Expected-Paths")
    expected_paths = parse_expected_paths(expected_raw)

    if branch_token.lower() not in owner.lower() and owner.lower() not in branch_token.lower():
        owner_bits = [bit for bit in re.split(r"[:|/@]+", owner.lower()) if len(bit) >= 6]
        if owner_bits and not any(bit in branch_token.lower() or branch_token.lower() in bit for bit in owner_bits):
            raise ValueError(
                f"branch owner token '{branch_token}' is not visibly bound to Canonical owner/session '{owner}'"
            )

    return lane_key, owner, ownership_key, expected_paths


def canonical_ownership_conflict(
    current_issue: dict,
    current_key: str,
    open_issues: list[dict],
    activation: datetime | None,
) -> tuple[int, str] | None:
    contenders: list[dict] = []
    for candidate in open_issues:
        if candidate.get("pull_request"):
            continue
        try:
            if not reservation_v2_required(candidate, activation):
                continue
            peer_raw = _single_field(OWNERSHIP_RE, candidate.get("body"), "Ownership-Key")
            if peer_raw is None or normalize_ownership_key(peer_raw) != current_key:
                continue
        except (ValueError, TypeError):
            continue
        contenders.append(candidate)

    if not contenders:
        return None
    winner = min(contenders, key=reservation_order)
    current_number = int(current_issue.get("number") or 0)
    winner_number = int(winner.get("number") or 0)
    if winner_number == current_number:
        return None
    return winner_number, str(winner.get("title") or "")


def canonical_expected_path_conflict(
    current_issue: dict,
    current_paths: list[str],
    open_issues: list[dict],
    activation: datetime | None,
) -> tuple[int, list[tuple[str, str]]] | None:
    current_number = int(current_issue.get("number") or 0)
    current_order = reservation_order(current_issue)
    conflicts: list[tuple[tuple[datetime, int], int, list[tuple[str, str]]]] = []

    for candidate in open_issues:
        if candidate.get("pull_request"):
            continue
        peer_number = int(candidate.get("number") or 0)
        if peer_number == current_number:
            continue
        try:
            if not reservation_v2_required(candidate, activation):
                continue
            raw = _single_field(EXPECTED_PATHS_RE, candidate.get("body"), "Expected-Paths")
            if raw is None:
                continue
            peer_paths = parse_expected_paths(raw)
        except (ValueError, TypeError):
            continue
        overlaps = overlapping_claims(current_paths, peer_paths)
        if not overlaps:
            continue
        peer_order = reservation_order(candidate)
        if peer_order < current_order:
            conflicts.append((peer_order, peer_number, overlaps))

    if not conflicts:
        return None
    conflicts.sort(key=lambda row: row[0])
    _, number, overlaps = conflicts[0]
    return number, overlaps


def requires_lane_lock(head_ref: str, head_repo: str, repository: str, actor: str) -> bool:
    if actor == "dependabot[bot]":
        return False
    if head_repo != repository:
        return False
    return head_ref.startswith(LOCKED_PREFIXES)


def find_duplicate_carriers(
    current_number: int,
    current_key: str,
    open_prs: list[dict],
) -> list[tuple[int, str]]:
    conflicts: list[tuple[int, str]] = []
    for candidate in open_prs:
        try:
            number = int(candidate.get("number", 0))
        except (TypeError, ValueError):
            continue
        if number == current_number:
            continue
        head = ((candidate.get("head") or {}).get("ref") or "<unknown>").strip()
        try:
            peer_key = extract_lane_key(candidate.get("body"))
        except ValueError as exc:
            peer_evidence = extract_lane_evidence(candidate.get("body"))
            if current_key in peer_evidence:
                print(
                    f"WARN: PR #{number} has malformed lane metadata but visibly claims "
                    f"Lane-Key '{current_key}': {exc}"
                )
                conflicts.append((number, head))
            else:
                print(f"WARN: PR #{number} has malformed lane metadata unrelated to '{current_key}': {exc}")
            continue
        if peer_key == current_key:
            conflicts.append((number, head))
    return sorted(set(conflicts))


def _request_json(url: str, token: str) -> object:
    if not token:
        raise RuntimeError("GITHUB_TOKEN is required for the authenticated agent collision gate")
    headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": "Bearer " + token,
        "User-Agent": "qs3d-agent-reservation-collision-preflight",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.loads(response.read().decode("utf-8"))


def _fetch_paged(api_url: str, repository: str, endpoint: str, token: str) -> list[dict]:
    owner_repo = urllib.parse.quote(repository, safe="/")
    collected: list[dict] = []
    separator = "&" if "?" in endpoint else "?"
    for page in range(1, MAX_PAGES + 1):
        url = (
            f"{api_url.rstrip('/')}/repos/{owner_repo}/{endpoint}"
            f"{separator}per_page=100&page={page}"
        )
        payload = _request_json(url, token)
        if not isinstance(payload, list):
            raise RuntimeError(f"GitHub {endpoint} response was not a list")
        page_items = [item for item in payload if isinstance(item, dict)]
        collected.extend(page_items)
        if len(page_items) < 100:
            return collected
    raise RuntimeError(
        f"GitHub {endpoint} list exceeded {MAX_PAGES * 100} entries; refusing incomplete collision scan"
    )


def fetch_open_prs(api_url: str, repository: str, token: str) -> list[dict]:
    return _fetch_paged(api_url, repository, "pulls?state=open", token)


def fetch_open_issues(api_url: str, repository: str, token: str) -> list[dict]:
    return _fetch_paged(api_url, repository, "issues?state=open", token)


def fetch_issue(api_url: str, repository: str, issue_number: int, token: str) -> dict:
    owner_repo = urllib.parse.quote(repository, safe="/")
    url = f"{api_url.rstrip('/')}/repos/{owner_repo}/issues/{issue_number}"
    payload = _request_json(url, token)
    if not isinstance(payload, dict):
        raise RuntimeError(f"GitHub Issue #{issue_number} response was not an object")
    return payload


def fetch_pr_files(
    api_url: str,
    repository: str,
    pr_number: int,
    token: str,
) -> list[str]:
    items = _fetch_paged(api_url, repository, f"pulls/{pr_number}/files?", token)
    return [str(item.get("filename") or "") for item in items if item.get("filename")]


def current_changed_paths(base_ref: str) -> list[str]:
    raw = _run_git(["diff", "--name-only", "--diff-filter=ACMRTUXB", f"origin/{base_ref}...HEAD"])
    return [line.strip().replace("\\", "/") for line in raw.splitlines() if line.strip()]


def _event_actor(event: dict) -> str:
    sender = event.get("sender") or {}
    return str(sender.get("login") or os.environ.get("GITHUB_ACTOR") or "")


def validate_pull_request_event(
    event: dict,
    repository: str,
    open_prs: list[dict],
) -> tuple[str | None, list[tuple[int, str]]]:
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

    explicit_lane_key = extract_lane_key(pr.get("body"))
    issue_number = branch_issue_number(head_ref) if head_ref.startswith("agent/") else None
    if issue_number is not None:
        lane_key = f"issue-{issue_number}"
        if explicit_lane_key is not None and explicit_lane_key != lane_key:
            raise ValueError(
                f"PR #{number} Lane-Key '{explicit_lane_key}' does not match "
                f"branch-derived Lane-Key '{lane_key}'"
            )
    else:
        lane_key = explicit_lane_key
        if lane_key is None:
            raise ValueError(
                f"PR #{number} head '{head_ref}' requires a Lane-Key in the PR body; "
                "use 'Lane-Key: issue-<number>' or a stable integration batch key"
            )
    return lane_key, find_duplicate_carriers(number, lane_key, open_prs)


def current_context(event_name: str, event: dict, repository: str) -> tuple[str, str, str, int]:
    actor = _event_actor(event)
    if event_name == "pull_request":
        pr = event.get("pull_request")
        if not isinstance(pr, dict):
            raise ValueError("pull_request event payload is missing pull_request object")
        head = pr.get("head") or {}
        head_ref = str(head.get("ref") or "")
        head_repo = str((head.get("repo") or {}).get("full_name") or "")
        number = int(pr.get("number") or event.get("number") or 0)
        return head_ref, head_repo, actor, number

    if event_name == "push":
        head_ref = str(os.environ.get("GITHUB_REF_NAME") or "")
        if not head_ref:
            ref = str(event.get("ref") or "")
            head_ref = ref.removeprefix("refs/heads/")
        return head_ref, repository, actor, 0

    raise ValueError(f"unsupported event '{event_name}'")


def peer_reservation_time(peer_pr: dict, open_issues: list[dict]) -> tuple[datetime, int]:
    peer_ref = str((peer_pr.get("head") or {}).get("ref") or "")
    issue_number = branch_issue_number(peer_ref)
    if issue_number is not None:
        for issue in open_issues:
            if int(issue.get("number") or 0) == issue_number and not issue.get("pull_request"):
                try:
                    return reservation_order(issue)
                except (ValueError, TypeError):
                    break
    return parse_iso8601(str(peer_pr.get("created_at") or "")), int(peer_pr.get("number") or 0)


def canonical_open_pr_path_conflicts(
    current_issue: dict,
    current_head_ref: str,
    changed_paths: list[str],
    open_prs: list[dict],
    open_issues: list[dict],
    api_url: str,
    repository: str,
    token: str,
    current_pr_number: int,
) -> list[tuple[int, str, list[str]]]:
    if not changed_paths:
        return []
    current_order = reservation_order(current_issue)
    changed = set(changed_paths)
    conflicts: list[tuple[int, str, list[str]]] = []

    for peer in open_prs:
        peer_number = int(peer.get("number") or 0)
        if peer_number == current_pr_number:
            continue
        peer_ref = str((peer.get("head") or {}).get("ref") or "")
        if peer_ref == current_head_ref or not peer_ref.startswith(LOCKED_PREFIXES):
            continue
        head_repo = str(((peer.get("head") or {}).get("repo") or {}).get("full_name") or "")
        if head_repo and head_repo != repository:
            continue
        try:
            if peer_reservation_time(peer, open_issues) >= current_order:
                continue
        except (ValueError, TypeError):
            pass
        peer_files = set(fetch_pr_files(api_url, repository, peer_number, token))
        overlap = sorted(changed.intersection(peer_files))
        if overlap:
            conflicts.append((peer_number, peer_ref, overlap))
    return sorted(conflicts)


def main() -> int:
    if os.environ.get("QS3D_AGENT_LANE_COLLISION_RUNTIME") != "1":
        print(
            "PASS: agent reservation collision runtime is disabled; "
            "hermetic regression remains active via preflight-all."
        )
        return 0

    event_name = os.environ.get("GITHUB_EVENT_NAME", "")
    if event_name not in {"push", "pull_request"}:
        print("ERROR: agent reservation collision runtime supports only push and pull_request events")
        return 1

    event_path = os.environ.get("GITHUB_EVENT_PATH")
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not event_path or not repository or not token:
        print(
            "ERROR: agent reservation collision runtime requires "
            "GITHUB_EVENT_PATH, GITHUB_REPOSITORY and GITHUB_TOKEN"
        )
        return 1

    try:
        event = json.loads(Path(event_path).read_text(encoding="utf-8"))
        if not isinstance(event, dict):
            raise ValueError("event payload root must be an object")
        head_ref, head_repo, actor, current_pr_number = current_context(
            event_name, event, repository
        )
        if not requires_lane_lock(head_ref, head_repo, repository, actor):
            print("PASS: event is outside the same-repository agent/integration collision lock scope.")
            return 0

        api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com")
        open_prs = fetch_open_prs(api_url, repository, token)

        if event_name == "pull_request":
            lane_key, lane_conflicts = validate_pull_request_event(event, repository, open_prs)
            if lane_conflicts:
                print(f"ERROR: Lane-Key '{lane_key}' already has another open canonical carrier:")
                for number, peer_ref in lane_conflicts:
                    print(f" - PR #{number}: {peer_ref}")
                print(
                    "Close/explicitly supersede the old carrier before replacement. "
                    "DUPLICATE_CARRIER / NO MUTATION."
                )
                return 1

        if head_ref.startswith("integration/"):
            print("PASS: integration carrier passed PR Lane-Key uniqueness; reservation v2 binds agent/**.")
            return 0

        issue_number = branch_issue_number(head_ref)
        if issue_number is None:
            raise ValueError(
                f"agent branch '{head_ref}' must include issue-<number> so branch CI can bind a visible reservation"
            )
        branch_token = branch_owner_token(head_ref)
        if branch_token is None:
            raise ValueError(f"agent branch '{head_ref}' does not match agent/<owner>/<scope>")

        issue = fetch_issue(api_url, repository, issue_number, token)
        activation = marker_activation_time()
        if not reservation_v2_required(issue, activation):
            print(
                f"PASS: legacy reservation Issue #{issue_number} predates reservation-v2 activation; "
                "existing PR Lane-Key uniqueness remains enforced."
            )
            return 0

        validate_owner_token(branch_token)
        lane_key, owner, ownership_key, expected_paths = validate_v2_issue(
            issue, issue_number, head_ref, branch_token
        )
        open_issues = fetch_open_issues(api_url, repository, token)

        ownership_conflict = canonical_ownership_conflict(
            issue, ownership_key, open_issues, activation
        )
        if ownership_conflict is not None:
            peer_number, peer_title = ownership_conflict
            raise ValueError(
                f"Ownership-Key '{ownership_key}' was reserved earlier by Issue #{peer_number} "
                f"({peer_title}); DUPLICATE_CARRIER / NO MUTATION"
            )

        path_claim_conflict = canonical_expected_path_conflict(
            issue, expected_paths, open_issues, activation
        )
        if path_claim_conflict is not None:
            peer_number, overlaps = path_claim_conflict
            detail = ", ".join(f"{left} <-> {right}" for left, right in overlaps[:8])
            raise ValueError(
                f"Expected-Paths overlaps earlier reservation Issue #{peer_number}: {detail}; "
                "DUPLICATE_CARRIER / NO MUTATION"
            )

        base_ref = (
            str(os.environ.get("GITHUB_BASE_REF") or "").strip()
            if event_name == "pull_request"
            else "main"
        ) or "main"
        changed_paths = current_changed_paths(base_ref)
        undeclared = [
            path for path in changed_paths
            if not any(path_matches_claim(path, claim) for claim in expected_paths)
        ]
        if undeclared:
            raise ValueError(
                "branch changed path(s) outside Expected-Paths: " + ", ".join(undeclared[:20])
            )

        pr_path_conflicts = canonical_open_pr_path_conflicts(
            issue,
            head_ref,
            changed_paths,
            open_prs,
            open_issues,
            api_url,
            repository,
            token,
            current_pr_number,
        )
        if pr_path_conflicts:
            lines = []
            for number, peer_ref, overlap in pr_path_conflicts[:8]:
                lines.append(f"PR #{number} {peer_ref}: {', '.join(overlap[:8])}")
            raise ValueError(
                "current changed paths overlap an earlier open agent/integration PR: "
                + " | ".join(lines)
                + "; DUPLICATE_CARRIER / NO MUTATION"
            )

    except (
        OSError,
        ValueError,
        RuntimeError,
        json.JSONDecodeError,
        urllib.error.URLError,
        subprocess.SubprocessError,
    ) as exc:
        print("ERROR: agent reservation collision preflight failed closed:", exc)
        print("See docs/AGENT-RESERVATION-V2.md for reservation-v2 recovery.")
        return 1

    print(
        f"PASS: reservation v2 '{lane_key}' owner '{owner}' / Ownership-Key '{ownership_key}' "
        f"is canonical with {len(expected_paths)} declared path claim(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
