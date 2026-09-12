#!/usr/bin/env python3
"""Create or resume a protection-safe V25 preview version PR."""

from __future__ import annotations

import argparse
from dataclasses import dataclass
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from typing import Any

MAX_PREVIEW_ORDINAL = 65535
PROJECTS = (
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
    "src/QS3D.Core/QS3D.Core.csproj",
)
PREVIEW_RE = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)-preview\.([1-9][0-9]*)$"
)


class AutoVersionError(RuntimeError):
    pass
@dataclass(frozen=True)
class PreviewIdentity:
    major: int
    minor: int
    patch: int
    ordinal: int

    @property
    def product_version(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}-preview.{self.ordinal}"

    @property
    def tag(self) -> str:
        return "v" + self.product_version

    @property
    def file_version(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}.{self.ordinal}"

    @property
    def series(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}"


def parse_preview_identity(raw: str) -> PreviewIdentity:
    if raw != raw.strip():
        raise AutoVersionError(f"preview identity has surrounding whitespace: {raw!r}")
    match = PREVIEW_RE.fullmatch(raw)
    if not match:
        raise AutoVersionError(f"preview identity is non-canonical: {raw}")
    major, minor, patch, ordinal = (int(part, 10) for part in match.groups())
    if ordinal > MAX_PREVIEW_ORDINAL:
        raise AutoVersionError(f"preview ordinal exceeds FileVersion range: {raw}")
    return PreviewIdentity(major, minor, patch, ordinal)


def choose_next_ordinal(*, committed: int, published: int, occupied: set[int]) -> int:
    candidate = max(committed, published) + 1
    while candidate in occupied:
        candidate += 1
    if candidate > MAX_PREVIEW_ORDINAL:
        raise AutoVersionError("no preview ordinal remains in FileVersion range")
    return candidate


def classify_committed_identity(
    *, committed: int, published: int, exact_tag: bool, prior_owner: bool
) -> str:
    if committed <= published or exact_tag or prior_owner:
        return "prepare"
    return "release"


def _replace_exact_element(text: str, name: str, value: str) -> str:
    pattern = re.compile(rf"(?m)^(?P<indent>\s*)<{name}>[^<]*</{name}>\s*$")
    matches = list(pattern.finditer(text))
    if len(matches) != 1:
        raise AutoVersionError(f"project must contain exactly one {name} element")
    match = matches[0]
    replacement = f"{match.group('indent')}<{name}>{value}</{name}>"
    return text[: match.start()] + replacement + text[match.end() :]

def _read_identity_from_project(text: str) -> PreviewIdentity:
    version_matches = re.findall(r"(?m)^\s*<Version>([^<]+)</Version>\s*$", text)
    file_matches = re.findall(r"(?m)^\s*<FileVersion>([^<]+)</FileVersion>\s*$", text)
    info_matches = re.findall(r"(?m)^\s*<InformationalVersion>([^<]+)</InformationalVersion>\s*$", text)
    if len(version_matches) != 1 or len(file_matches) != 1 or len(info_matches) != 1:
        raise AutoVersionError("project identity elements must each appear exactly once")
    identity = parse_preview_identity(version_matches[0])
    if info_matches[0] != identity.product_version or file_matches[0] != identity.file_version:
        raise AutoVersionError("project Version/FileVersion/InformationalVersion are not aligned")
    return identity


def rewrite_project_text(text: str, target: PreviewIdentity) -> str:
    _read_identity_from_project(text)
    rendered = _replace_exact_element(text, "Version", target.product_version)
    rendered = _replace_exact_element(rendered, "FileVersion", target.file_version)
    rendered = _replace_exact_element(rendered, "InformationalVersion", target.product_version)
    return rendered


def canonical_branch_name(issue_number: int, ordinal: int) -> str:
    if issue_number <= 0 or ordinal <= 0 or ordinal > MAX_PREVIEW_ORDINAL:
        raise AutoVersionError("issue number and preview ordinal must be positive canonical integers")
    return f"agent/qs3d-release-automation/issue-{issue_number}-v25-preview-{ordinal}"


def build_reservation_body(issue_number: int, branch: str, target: PreviewIdentity) -> str:
    expected_paths = ";".join(PROJECTS)
    return (
        f"Lane-Key: issue-{issue_number}\n"
        "Reservation-Protocol: v2\n"
        "Canonical owner/session: qs3d-release-automation\n"
        f"Canonical carrier: {branch}\n"
        f"Ownership-Key: release/v25-preview-version/{target.series}\n"
        f"Expected-Paths: {expected_paths}\n"
        f"Automation-Key: v25-auto-version-pr:{target.tag}\n"
        f"Target-Version: {target.product_version}\n"
    )

GIT_REFS_ENDPOINT = "/git/refs"
GIT_BLOBS_ENDPOINT = "/git/blobs"
GIT_TREES_ENDPOINT = "/git/trees"
GIT_COMMITS_ENDPOINT = "/git/commits"
COMPARE_ENDPOINT = "/compare/"
PULLS_ENDPOINT = "/pulls"
MAX_PAGES = 10
RESERVATION_RE = re.compile(
    r"^QS3D_V25_PREVIEW_RESERVATION ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{40}) run_id=([1-9][0-9]*)$"
)
FENCE_RE = re.compile(
    r"^QS3D_V25_PREVIEW_DISPATCH_FENCE ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{40}) run_id=([1-9][0-9]*)$"
)


class GhClient:
    def __init__(self, repository: str, *, token_env_var: str = "GH_TOKEN") -> None:
        self.repository = repository
        self.token_env_var = token_env_var

    def api(self, path: str, *, method: str = "GET", body: dict[str, Any] | None = None) -> Any:
        token = os.environ.get(self.token_env_var, "").strip()
        if not token:
            raise AutoVersionError(f"{self.token_env_var} is required for GitHub API access")
        env = dict(os.environ)
        env["GH_TOKEN"] = token
        cmd = ["gh", "api", path]
        if method != "GET":
            cmd.extend(["--method", method])
        payload = None
        if body is not None:
            cmd.extend(["--input", "-"])
            payload = json.dumps(body)
        completed = subprocess.run(cmd, input=payload, text=True, capture_output=True, env=env, check=False)
        if completed.returncode != 0:
            detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
            raise AutoVersionError(f"gh api {method} {path} failed: {detail}")
        if not completed.stdout.strip():
            return None
        try:
            return json.loads(completed.stdout)
        except json.JSONDecodeError as exc:
            raise AutoVersionError(f"gh api returned non-JSON for {path}") from exc

def _bounded_collection(client: GhClient, endpoint: str, *, label: str) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for page in range(1, MAX_PAGES + 1):
        separator = "&" if "?" in endpoint else "?"
        payload = client.api(f"{endpoint}{separator}per_page=100&page={page}")
        if not isinstance(payload, list) or any(not isinstance(item, dict) for item in payload):
            raise AutoVersionError(f"{label} enumeration returned malformed JSON")
        rows.extend(payload)
        if len(payload) < 100:
            return rows
    raise AutoVersionError(f"{label} enumeration exceeded bounded {MAX_PAGES * 100}-row scan")


def _run_git(*args: str) -> str:
    completed = subprocess.run(["git", *args], text=True, capture_output=True, check=False)
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
        raise AutoVersionError(f"git {' '.join(args)} failed: {detail}")
    return completed.stdout


def _parse_matching_tag(tag: str, series: str) -> int:
    prefix = f"v{series}-preview."
    if not tag.startswith(prefix):
        raise AutoVersionError(f"matching-series tag prefix changed unexpectedly: {tag}")
    identity = parse_preview_identity(tag[1:])
    if identity.series != series:
        raise AutoVersionError(f"matching-series tag escaped series {series}: {tag}")
    return identity.ordinal


def _collect_tag_ordinals(series: str) -> set[int]:
    prefix = f"v{series}-preview."
    raw = _run_git("tag", "--list", f"{prefix}*")
    ordinals: set[int] = set()
    for line in raw.splitlines():
        tag = line.strip()
        if tag:
            ordinals.add(_parse_matching_tag(tag, series))
    return ordinals

def _collect_release_ordinals(client: GhClient, series: str) -> set[int]:
    releases = _bounded_collection(
        client,
        f"repos/{client.repository}/releases?",
        label="published releases",
    )
    prefix = f"v{series}-preview."
    ordinals: set[int] = set()
    for release in releases:
        if bool(release.get("draft")) or not release.get("published_at"):
            continue
        tag = str(release.get("tag_name") or "")
        if tag.startswith(prefix):
            ordinals.add(_parse_matching_tag(tag, series))
    return ordinals


def _collect_ledger_ordinals(client: GhClient, issue_number: int) -> set[int]:
    comments = _bounded_collection(
        client,
        f"repos/{client.repository}/issues/{issue_number}/comments?",
        label="preview reservation ledger",
    )
    ordinals: set[int] = set()
    for comment in comments:
        user = comment.get("user") if isinstance(comment.get("user"), dict) else {}
        if str(user.get("login") or "") != "github-actions[bot]":
            continue
        body = str(comment.get("body") or "").strip()
        if not body:
            continue
        match = RESERVATION_RE.fullmatch(body) or FENCE_RE.fullmatch(body)
        if match:
            ordinal = int(match.group(1), 10)
            if ordinal > MAX_PREVIEW_ORDINAL:
                raise AutoVersionError(f"ledger ordinal exceeds FileVersion range: {ordinal}")
            ordinals.add(ordinal)
            continue
        if body.startswith("QS3D_V25_PREVIEW_RESERVATION") or body.startswith("QS3D_V25_PREVIEW_DISPATCH_FENCE"):
            raise AutoVersionError(f"malformed matching V25 preview ledger row: {body}")
    return ordinals


def _read_project_identity(path: str) -> PreviewIdentity:
    return _read_identity_from_project(Path(path).read_text(encoding="utf-8"))


def _ensure_projects_aligned() -> PreviewIdentity:
    identities = [_read_project_identity(path) for path in PROJECTS]
    if len(set(identities)) != 1:
        raise AutoVersionError("V25/V26/Core project identities are not aligned")
    return identities[0]

def _automation_key(target: PreviewIdentity) -> str:
    return f"v25-auto-version-pr:{target.tag}"


def _open_issues(client: GhClient) -> list[dict[str, Any]]:
    return _bounded_collection(
        client,
        f"repos/{client.repository}/issues?state=open&",
        label="open Issues",
    )


def _open_pulls(client: GhClient) -> list[dict[str, Any]]:
    rows = _bounded_collection(
        client,
        f"repos/{client.repository}/pulls?state=open&",
        label="open pull requests",
    )
    return rows


def _carrier_issue_for_target(client: GhClient, target: PreviewIdentity) -> dict[str, Any] | None:
    marker = f"Automation-Key: {_automation_key(target)}"
    claimants = [
        issue for issue in _open_issues(client)
        if not issue.get("pull_request") and marker in str(issue.get("body") or "")
    ]
    if len(claimants) > 1:
        numbers = ", ".join(str(item.get("number")) for item in claimants)
        raise AutoVersionError(f"multiple open automatic version carriers claim {target.tag}: {numbers}")
    return claimants[0] if claimants else None


def _validate_carrier_body(issue: dict[str, Any], target: PreviewIdentity) -> tuple[int, str]:
    number = int(issue.get("number") or 0)
    if number <= 0:
        raise AutoVersionError("automatic version carrier Issue has no valid number")
    branch = canonical_branch_name(number, target.ordinal)
    expected = build_reservation_body(number, branch, target)
    body = str(issue.get("body") or "")
    for line in expected.splitlines():
        if line not in body:
            raise AutoVersionError(f"automatic version carrier #{number} is malformed; missing {line}")
    return number, branch

def _create_or_resume_issue(client: GhClient, target: PreviewIdentity) -> tuple[int, str]:
    issue = _carrier_issue_for_target(client, target)
    title = f"chore(release): prepare {target.tag}"
    marker = f"Automation-Key: {_automation_key(target)}"
    target_marker = f"Target-Version: {target.product_version}"
    if issue is None:
        provisional = f"{marker}\n{target_marker}\nPreparation-State: pending-reservation-normalization\n"
        created = client.api(
            f"repos/{client.repository}/issues",
            method="POST",
            body={"title": title, "body": provisional},
        )
        if not isinstance(created, dict):
            raise AutoVersionError("Issue creation returned malformed JSON")
        issue = created
    number = int(issue.get("number") or 0)
    if number <= 0:
        raise AutoVersionError("automatic version carrier Issue has no valid number")
    branch = canonical_branch_name(number, target.ordinal)
    body = str(issue.get("body") or "")
    if "Reservation-Protocol: v2" not in body:
        if str(issue.get("title") or "") != title or marker not in body or target_marker not in body:
            raise AutoVersionError(f"partial automatic carrier #{number} cannot be safely resumed")
        normalized = build_reservation_body(number, branch, target)
        updated = client.api(
            f"repos/{client.repository}/issues/{number}",
            method="PATCH",
            body={"body": normalized},
        )
        if not isinstance(updated, dict):
            raise AutoVersionError("Issue normalization returned malformed JSON")
        issue = updated
    return _validate_carrier_body(issue, target)


def _run_reservation_precheck(
    issue_number: int, repository: str, *, token_env_var: str = "GH_TOKEN"
) -> None:
    cmd = [
        sys.executable,
        "scripts/agent-reservation-precheck.py",
        "--issue", str(issue_number),
        "--repository", repository,
    ]
    env = dict(os.environ)
    token = env.get(token_env_var, "").strip()
    if not token:
        raise AutoVersionError(f"{token_env_var} is required for automatic version preparation")
    env["GH_TOKEN"] = token
    completed = subprocess.run(cmd, text=True, capture_output=True, env=env, check=False)
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
        raise AutoVersionError(f"scripts/agent-reservation-precheck.py rejected carrier #{issue_number}: {detail}")

def _api_optional(client: GhClient, path: str) -> Any | None:
    try:
        return client.api(path)
    except AutoVersionError as exc:
        if "HTTP 404" in str(exc) or "Not Found" in str(exc):
            return None
        raise


def _main_sha(client: GhClient) -> str:
    payload = client.api(f"repos/{client.repository}/commits/main")
    if not isinstance(payload, dict):
        raise AutoVersionError("protected main response was not an object")
    sha = str(payload.get("sha") or "").lower()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise AutoVersionError(f"protected main returned malformed SHA: {sha}")
    return sha


def _admit_base_twice(client: GhClient, base_sha: str) -> None:
    first = _main_sha(client)
    second = _main_sha(client)
    if first != base_sha or second != base_sha:
        raise AutoVersionError(
            f"protected main moved before durable branch mutation: expected={base_sha} first={first} second={second}"
        )


def _git_show_text(base_sha: str, path: str) -> str:
    completed = subprocess.run(
        ["git", "show", f"{base_sha}:{path}"],
        text=True,
        capture_output=True,
        check=False,
        encoding="utf-8",
        errors="strict",
    )
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
        raise AutoVersionError(f"cannot read {path} at admitted base {base_sha}: {detail}")
    return completed.stdout


def _git_blob_sha(text: str) -> str:
    completed = subprocess.run(
        ["git", "hash-object", "--stdin"], input=text, text=True, capture_output=True, check=False
    )
    if completed.returncode != 0:
        raise AutoVersionError("git hash-object failed for rendered project content")
    sha = completed.stdout.strip().lower()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise AutoVersionError(f"git hash-object returned malformed SHA: {sha}")
    return sha

def _render_projects_at_base(base_sha: str, target: PreviewIdentity) -> dict[str, str]:
    rendered: dict[str, str] = {}
    current: PreviewIdentity | None = None
    for path in PROJECTS:
        source = _git_show_text(base_sha, path)
        identity = _read_identity_from_project(source)
        if current is None:
            current = identity
        elif identity != current:
            raise AutoVersionError("V25/V26/Core identities at admitted base are not aligned")
        rendered[path] = rewrite_project_text(source, target)
    return rendered


def _branch_ref_sha(client: GhClient, branch: str) -> str | None:
    payload = _api_optional(client, f"repos/{client.repository}/git/ref/heads/{branch}")
    if payload is None:
        return None
    if not isinstance(payload, dict):
        raise AutoVersionError("branch ref response was malformed")
    sha = str(((payload.get("object") or {}).get("sha") if isinstance(payload.get("object"), dict) else "") or "").lower()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise AutoVersionError(f"branch ref returned malformed SHA: {sha}")
    return sha


def _ensure_branch_ref(client: GhClient, branch: str, base_sha: str) -> str:
    existing = _branch_ref_sha(client, branch)
    if existing is not None:
        return existing
    try:
        created = client.api(
            f"repos/{client.repository}/git/refs",
            method="POST",
            body={"ref": f"refs/heads/{branch}", "sha": base_sha},
        )
    except AutoVersionError as exc:
        if "HTTP 422" not in str(exc) and "already exists" not in str(exc).lower():
            raise
        raced = _branch_ref_sha(client, branch)
        if raced is None:
            raise AutoVersionError("canonical branch creation raced but ref cannot be resolved") from exc
        return raced
    if not isinstance(created, dict):
        raise AutoVersionError("canonical automation branch creation returned malformed JSON")
    created_sha = str(((created.get("object") or {}).get("sha") if isinstance(created.get("object"), dict) else "") or "").lower()
    if created_sha != base_sha:
        raise AutoVersionError(f"canonical automation branch was not created at admitted base: {created_sha}")
    return created_sha

def _verify_generated_commit(
    client: GhClient,
    *,
    base_sha: str,
    head_sha: str,
    expected_blob_shas: dict[str, str],
) -> None:
    commit = client.api(f"repos/{client.repository}/git/commits/{head_sha}")
    if not isinstance(commit, dict):
        raise AutoVersionError("generated commit response was malformed")
    parents = commit.get("parents")
    if not isinstance(parents, list) or len(parents) != 1 or str((parents[0] or {}).get("sha") or "").lower() != base_sha:
        raise AutoVersionError("generated version commit must have admitted main as its sole parent")
    compare = client.api(f"repos/{client.repository}/compare/{base_sha}...{head_sha}")
    if not isinstance(compare, dict) or not isinstance(compare.get("files"), list):
        raise AutoVersionError("generated branch compare response was malformed")
    files = compare["files"]
    changed = {str(item.get("filename") or "") for item in files if isinstance(item, dict)}
    if changed != set(PROJECTS):
        raise AutoVersionError(f"generated version commit changed unexpected paths: {sorted(changed)}")
    for item in files:
        path = str(item.get("filename") or "")
        if str(item.get("status") or "") != "modified":
            raise AutoVersionError(f"generated version path is not a modification: {path}")
        if str(item.get("sha") or "").lower() != expected_blob_shas[path]:
            raise AutoVersionError(f"generated version blob mismatch for {path}")

def _create_or_verify_branch_commit(
    client: GhClient,
    *,
    branch: str,
    base_sha: str,
    target: PreviewIdentity,
) -> str:
    rendered = _render_projects_at_base(base_sha, target)
    expected_blob_shas = {path: _git_blob_sha(text) for path, text in rendered.items()}
    existing = _ensure_branch_ref(client, branch, base_sha)
    if existing != base_sha:
        _verify_generated_commit(
            client,
            base_sha=base_sha,
            head_sha=existing,
            expected_blob_shas=expected_blob_shas,
        )
        return existing

    base_commit = client.api(f"repos/{client.repository}/git/commits/{base_sha}")
    if not isinstance(base_commit, dict) or not isinstance(base_commit.get("tree"), dict):
        raise AutoVersionError("admitted base commit tree response was malformed")
    base_tree = str(base_commit["tree"].get("sha") or "")
    if not re.fullmatch(r"[0-9a-f]{40}", base_tree):
        raise AutoVersionError("admitted base tree SHA is malformed")

    tree_rows: list[dict[str, str]] = []
    for path in PROJECTS:
        blob = client.api(
            f"repos/{client.repository}/git/blobs",
            method="POST",
            body={"content": rendered[path], "encoding": "utf-8"},
        )
        if not isinstance(blob, dict) or str(blob.get("sha") or "").lower() != expected_blob_shas[path]:
            raise AutoVersionError(f"GitHub blob SHA mismatch for {path}")
        tree_rows.append({"path": path, "mode": "100644", "type": "blob", "sha": expected_blob_shas[path]})
    tree = client.api(
        f"repos/{client.repository}/git/trees",
        method="POST",
        body={"base_tree": base_tree, "tree": tree_rows},
    )
    if not isinstance(tree, dict):
        raise AutoVersionError("generated tree response was malformed")
    tree_sha = str(tree.get("sha") or "").lower()
    if not re.fullmatch(r"[0-9a-f]{40}", tree_sha):
        raise AutoVersionError("generated tree SHA is malformed")

    commit = client.api(
        f"repos/{client.repository}/git/commits",
        method="POST",
        body={
            "message": f"chore(release): prepare {target.tag}",
            "tree": tree_sha,
            "parents": [base_sha],
        },
    )
    if not isinstance(commit, dict):
        raise AutoVersionError("generated commit response was malformed")
    commit_sha = str(commit.get("sha") or "").lower()
    if not re.fullmatch(r"[0-9a-f]{40}", commit_sha):
        raise AutoVersionError("generated commit SHA is malformed")

    client.api(
        f"repos/{client.repository}/git/refs/heads/{branch}",
        method="PATCH",
        body={"sha": commit_sha, "force": False},
    )
    _verify_generated_commit(
        client,
        base_sha=base_sha,
        head_sha=commit_sha,
        expected_blob_shas=expected_blob_shas,
    )
    return commit_sha


def _find_open_pr(client: GhClient, branch: str) -> dict[str, Any] | None:
    matches: list[dict[str, Any]] = []
    for pull in _open_pulls(client):
        head = pull.get("head") if isinstance(pull.get("head"), dict) else {}
        base = pull.get("base") if isinstance(pull.get("base"), dict) else {}
        head_repo = head.get("repo") if isinstance(head.get("repo"), dict) else {}
        if (
            str(head.get("ref") or "") == branch
            and str(base.get("ref") or "") == "main"
            and str(head_repo.get("full_name") or "") == client.repository
        ):
            matches.append(pull)
    if len(matches) > 1:
        raise AutoVersionError(f"multiple open pull requests exist for canonical branch {branch}")
    return matches[0] if matches else None

def _target_from_carrier(issue: dict[str, Any], series: str) -> PreviewIdentity:
    body = str(issue.get("body") or "")
    matches = re.findall(r"(?m)^Target-Version:\s*(\S+)\s*$", body)
    if len(matches) != 1:
        raise AutoVersionError("automatic version carrier must state exactly one Target-Version")
    target = parse_preview_identity(matches[0])
    if target.series != series:
        raise AutoVersionError(
            f"open automatic version carrier targets wrong preview series: {target.product_version}"
        )
    return target


def _series_carrier(client: GhClient, series: str) -> dict[str, Any] | None:
    ownership = f"Ownership-Key: release/v25-preview-version/{series}"
    prefix = "Automation-Key: v25-auto-version-pr:"
    claimants = [
        issue for issue in _open_issues(client)
        if not issue.get("pull_request")
        and ownership in str(issue.get("body") or "")
        and prefix in str(issue.get("body") or "")
    ]
    if len(claimants) > 1:
        numbers = ", ".join(str(item.get("number")) for item in claimants)
        raise AutoVersionError(f"multiple unresolved automatic version carriers exist for {series}: {numbers}")
    return claimants[0] if claimants else None


def _create_or_resume_pr(
    client: GhClient,
    *,
    issue_number: int,
    branch: str,
    target: PreviewIdentity,
) -> int:
    existing = _find_open_pr(client, branch)
    if existing is not None:
        number = int(existing.get("number") or 0)
        if number <= 0:
            raise AutoVersionError("existing automatic version PR has invalid number")
        print(f"Automatic version PR already open: #{number} for {target.tag}")
        return number
    body = (
        f"Lane-Key: issue-{issue_number}\n"
        f"Automation-Key: {_automation_key(target)}\n"
        f"Target-Version: {target.product_version}\n\n"
        f"Closes #{issue_number}\n"
    )
    created = client.api(
        f"repos/{client.repository}/pulls",
        method="POST",
        body={
            "title": f"chore(release): prepare {target.tag}",
            "head": branch,
            "base": "main",
            "body": body,
            "draft": False,
        },
    )
    if not isinstance(created, dict):
        raise AutoVersionError("automatic version PR creation returned malformed JSON")
    number = int(created.get("number") or 0)
    if number <= 0:
        raise AutoVersionError("automatic version PR creation returned invalid number")
    print(f"Opened protected V25 version PR #{number} for {target.tag}")
    return number

def _identity_at_base(base_sha: str) -> PreviewIdentity:
    identities = [
        _read_identity_from_project(_git_show_text(base_sha, path))
        for path in PROJECTS
    ]
    if len(set(identities)) != 1:
        raise AutoVersionError("V25/V26/Core identities at admitted base are not aligned")
    return identities[0]


def _validate_sha(raw: str, label: str) -> str:
    sha = raw.strip().lower()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise AutoVersionError(f"{label} must be one exact 40-hex SHA: {raw}")
    return sha


def _require_main(client: GhClient, base_sha: str) -> None:
    current = _main_sha(client)
    if current != base_sha:
        raise AutoVersionError(
            f"protected main moved before version preparation: expected={base_sha} current={current}"
        )


def _published_ordinal(
    *,
    client: GhClient,
    committed: PreviewIdentity,
    published_tag: str,
) -> tuple[int, set[int]]:
    release_ordinals = _collect_release_ordinals(client, committed.series)
    latest = max(release_ordinals, default=0)
    if published_tag:
        if not published_tag.startswith("v"):
            raise AutoVersionError(f"published tag is non-canonical: {published_tag}")
        published = parse_preview_identity(published_tag[1:])
        if published.series != committed.series:
            raise AutoVersionError(
                f"published tag {published_tag} is outside committed series {committed.series}"
            )
        if published.ordinal != latest:
            raise AutoVersionError(
                f"published baseline changed during preparation: supplied={published.ordinal} latest={latest}"
            )
    elif latest != 0:
        raise AutoVersionError("published baseline was omitted although matching published releases exist")
    return latest, release_ordinals

def prepare(args: argparse.Namespace) -> int:
    repository = args.repository.strip()
    if not re.fullmatch(r"[^/\s]+/[^/\s]+", repository):
        raise AutoVersionError(f"repository must be OWNER/REPO: {repository}")
    source_sha = _validate_sha(args.source_sha, "source SHA")
    base_sha = _validate_sha(args.base_sha, "base SHA")
    _run_git("merge-base", "--is-ancestor", source_sha, base_sha)

    issue_client = GhClient(repository, token_env_var="GH_TOKEN")
    mutation_client = GhClient(repository, token_env_var="QS3D_AUTOMERGE_TOKEN")
    _require_main(issue_client, base_sha)
    committed = _identity_at_base(base_sha)
    published, release_ordinals = _published_ordinal(
        client=issue_client,
        committed=committed,
        published_tag=args.published_tag,
    )
    tag_ordinals = _collect_tag_ordinals(committed.series)
    ledger_ordinals = _collect_ledger_ordinals(issue_client, args.ledger_issue)
    occupied = set(release_ordinals) | set(tag_ordinals) | set(ledger_ordinals)

    existing_series = _series_carrier(issue_client, committed.series)
    if existing_series is not None:
        target = _target_from_carrier(existing_series, committed.series)
        issue_number, branch = _validate_carrier_body(existing_series, target)
        existing_pr = _find_open_pr(mutation_client, branch)
        if existing_pr is not None:
            number = int(existing_pr.get("number") or 0)
            print(
                f"Protected V25 version PR already exists for unresolved carrier #{issue_number}: "
                f"PR #{number} target={target.tag}"
            )
            return 0
        if target.ordinal <= max(committed.ordinal, published) or target.ordinal in occupied:
            raise AutoVersionError(
                f"unresolved automatic version carrier #{issue_number} target {target.tag} is no longer usable"
            )
    else:
        next_ordinal = choose_next_ordinal(
            committed=committed.ordinal,
            published=published,
            occupied=occupied,
        )
        target = PreviewIdentity(committed.major, committed.minor, committed.patch, next_ordinal)
        issue_number, branch = _create_or_resume_issue(issue_client, target)

    _run_reservation_precheck(issue_number, repository, token_env_var="GH_TOKEN")
    _require_main(issue_client, base_sha)
    refreshed_occupied = (
        _collect_tag_ordinals(committed.series)
        | _collect_release_ordinals(issue_client, committed.series)
        | _collect_ledger_ordinals(issue_client, args.ledger_issue)
    )
    if target.ordinal in refreshed_occupied:
        raise AutoVersionError(
            f"target {target.tag} became occupied before branch mutation; carrier #{issue_number} remains visible"
        )
    _admit_base_twice(issue_client, base_sha)
    commit_sha = _create_or_verify_branch_commit(
        mutation_client,
        branch=branch,
        base_sha=base_sha,
        target=target,
    )
    _create_or_resume_pr(
        mutation_client,
        issue_number=issue_number,
        branch=branch,
        target=target,
    )
    print(f"Prepared {target.tag} from protected main {base_sha} as branch commit {commit_sha}.")
    return 0

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create or resume a protection-safe automatic V25 preview version PR."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)
    prepare_parser = subparsers.add_parser("prepare")
    prepare_parser.add_argument("--repository", required=True)
    prepare_parser.add_argument("--source-sha", required=True)
    prepare_parser.add_argument("--base-sha", required=True)
    prepare_parser.add_argument("--published-tag", default="")
    prepare_parser.add_argument("--ledger-issue", type=int, default=1441)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.command == "prepare":
        return prepare(args)
    raise AutoVersionError(f"unsupported command: {args.command}")


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AutoVersionError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(2)
