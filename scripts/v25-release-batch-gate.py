#!/usr/bin/env python3
"""Evaluate the V25 preview batching policy from Git history."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import re
import subprocess
import sys

DEFAULT_SERIES_PREFIX = "v0.1.0-preview."
DEFAULT_MINIMUM_CHANGES = 10
MAX_PREVIEW_ORDINAL = 65535

RELEASE_RELEVANT_PREFIXES = (
    "src/",
    "tests/",
    "scripts/",
)
RELEASE_RELEVANT_EXACT_PATHS = {
    "Directory.Build.props",
    "QS3D.sln",
    "QS3D.V26.sln",
    ".github/workflows/release-v25-cloud.yml",
    ".github/workflows/dispatch-v25-cloud-after-main-integration.yml",
}


class GateError(RuntimeError):
    pass


def run_git(*args: str) -> str:
    completed = subprocess.run(
        ["git", *args],
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        encoding="utf-8",
        errors="replace",
    )
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
        raise GateError(f"git {' '.join(args)} failed: {detail}")
    return completed.stdout.strip()


def is_release_relevant(path: str) -> bool:
    normalized = path.strip().replace("\\", "/")
    if normalized in RELEASE_RELEVANT_EXACT_PATHS:
        return True
    return any(normalized.startswith(prefix) for prefix in RELEASE_RELEVANT_PREFIXES)


def parse_preview_ordinal(tag: str, series_prefix: str, source_label: str) -> int:
    pattern = re.compile(rf"^{re.escape(series_prefix)}([1-9][0-9]*)$")
    match = pattern.fullmatch(tag)
    if not match:
        raise GateError(f"{source_label} is non-canonical: {tag}")
    ordinal_text = match.group(1)
    if len(ordinal_text) > 5:
        raise GateError(f"{source_label} exceeds FileVersion range: {tag}")
    ordinal = int(ordinal_text, 10)
    if ordinal > MAX_PREVIEW_ORDINAL:
        raise GateError(f"{source_label} exceeds FileVersion range: {tag}")
    return ordinal


def parse_preview_tags(series_prefix: str) -> list[tuple[int, str]]:
    raw = run_git("tag", "--list", f"{series_prefix}*")
    if not raw:
        return []

    parsed: list[tuple[int, str]] = []
    for tag in raw.splitlines():
        tag = tag.strip()
        ordinal = parse_preview_ordinal(tag, series_prefix, "matching-series tag")
        parsed.append((ordinal, tag))
    return sorted(parsed)


def parse_explicit_published_tag(raw_tag: str, series_prefix: str) -> str | None:
    if raw_tag == "":
        return None
    if raw_tag != raw_tag.strip():
        raise GateError(f"published preview baseline has surrounding whitespace: {raw_tag!r}")
    parse_preview_ordinal(raw_tag, series_prefix, "published preview baseline")
    run_git("rev-parse", "--verify", f"refs/tags/{raw_tag}^{{commit}}")
    return raw_tag


def changed_paths_for_first_parent_commit(commit: str) -> list[str]:
    parents = run_git("rev-list", "--parents", "-n", "1", commit).split()
    if not parents or parents[0].lower() != commit.lower():
        raise GateError(f"could not resolve commit parents for {commit}")

    if len(parents) == 1:
        output = run_git("diff-tree", "--root", "--no-commit-id", "--name-only", "-r", commit)
    else:
        output = run_git("diff", "--name-only", parents[1], commit, "--")
    return [line.strip().replace("\\", "/") for line in output.splitlines() if line.strip()]


def collect_relevant_integrations(source_sha: str, previous_tag: str | None) -> list[tuple[str, str, list[str]]]:
    if previous_tag:
        run_git("merge-base", "--is-ancestor", previous_tag, source_sha)
        range_spec = f"{previous_tag}..{source_sha}"
        raw_commits = run_git("rev-list", "--first-parent", "--reverse", range_spec)
    else:
        raw_commits = run_git("rev-list", "--first-parent", "--reverse", source_sha)

    integrations: list[tuple[str, str, list[str]]] = []
    for commit in [line.strip() for line in raw_commits.splitlines() if line.strip()]:
        paths = changed_paths_for_first_parent_commit(commit)
        relevant_paths = sorted({path for path in paths if is_release_relevant(path)})
        if not relevant_paths:
            continue
        subject = run_git("show", "-s", "--format=%s", commit)
        integrations.append((commit, subject, relevant_paths))
    return integrations


def write_github_output(values: dict[str, str]) -> None:
    output_path = os.environ.get("GITHUB_OUTPUT", "").strip()
    if not output_path:
        return
    with Path(output_path).open("a", encoding="utf-8", newline="\n") as handle:
        for key, value in values.items():
            if "\n" in value or "\r" in value:
                raise GateError(f"GitHub output {key} contains a newline")
            handle.write(f"{key}={value}\n")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Evaluate the automatic V25 preview release batch threshold.")
    parser.add_argument("--source-sha", required=True, help="Exact 40-hex main source commit to evaluate.")
    parser.add_argument("--minimum-changes", type=int, default=DEFAULT_MINIMUM_CHANGES)
    parser.add_argument("--series-prefix", default=DEFAULT_SERIES_PREFIX)
    parser.add_argument(
        "--previous-published-tag",
        default=None,
        help=(
            "Explicit published preview baseline. Pass an empty value to state that no published preview exists; "
            "omit this option only for legacy standalone local-tag discovery."
        ),
    )
    parser.add_argument("--force", action="store_true", help="Allow a non-empty sub-threshold batch for an explicit manual release.")
    parser.add_argument("--require-ready", action="store_true", help="Exit non-zero unless the batch is eligible to publish.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source_sha = args.source_sha.strip().lower()
    if not re.fullmatch(r"[0-9a-f]{40}", source_sha):
        raise GateError(f"source SHA must be exactly 40 lowercase/uppercase hex characters: {args.source_sha}")
    if args.minimum_changes < 1:
        raise GateError("minimum changes must be at least 1")
    if not args.series_prefix or any(ch.isspace() for ch in args.series_prefix):
        raise GateError("series prefix must be a non-empty token without whitespace")

    resolved_source = run_git("rev-parse", "--verify", f"{source_sha}^{{commit}}").lower()
    if resolved_source != source_sha:
        raise GateError(f"source SHA resolved unexpectedly: expected {source_sha}, got {resolved_source}")

    if args.previous_published_tag is None:
        tags = parse_preview_tags(args.series_prefix)
        previous_tag = tags[-1][1] if tags else None
        baseline_mode = "legacy-local-tag-discovery"
    else:
        previous_tag = parse_explicit_published_tag(args.previous_published_tag, args.series_prefix)
        baseline_mode = "explicit-published-release"

    integrations = collect_relevant_integrations(source_sha, previous_tag)
    change_count = len(integrations)
    threshold_ready = change_count >= args.minimum_changes
    forced = bool(args.force and change_count > 0 and not threshold_ready)
    eligible = threshold_ready or forced

    print("QS3D V25 preview batch gate")
    print(f"Source SHA: {source_sha}")
    print(f"Baseline mode: {baseline_mode}")
    print(f"Previous preview: {previous_tag or '(none)'}")
    print(f"Release-relevant main integrations: {change_count}/{args.minimum_changes}")
    if integrations:
        print("Pending release-relevant integrations:")
        for commit, subject, paths in integrations[-25:]:
            path_summary = ", ".join(paths[:4])
            if len(paths) > 4:
                path_summary += f", +{len(paths) - 4} more"
            print(f" - {commit[:12]} {subject} [{path_summary}]")
        if len(integrations) > 25:
            print(f" - ... {len(integrations) - 25} earlier integration(s) omitted from log")

    if forced:
        print("Eligibility: FORCED manual sub-threshold release (non-empty batch).")
    elif threshold_ready:
        print("Eligibility: READY — automatic batch threshold satisfied.")
    elif change_count == 0:
        print("Eligibility: WAIT — no release-relevant integrations since the previous preview.")
    else:
        print(f"Eligibility: WAIT — {args.minimum_changes - change_count} more release-relevant integration(s) required.")

    write_github_output({
        "previous_tag": previous_tag or "",
        "baseline_mode": baseline_mode,
        "change_count": str(change_count),
        "minimum_changes": str(args.minimum_changes),
        "threshold_ready": str(threshold_ready).lower(),
        "forced": str(forced).lower(),
        "eligible": str(eligible).lower(),
    })

    if args.require_ready and not eligible:
        return 3
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except GateError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(2)
