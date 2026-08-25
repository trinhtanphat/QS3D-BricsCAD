#!/usr/bin/env python3
"""Classify Shared CI validation scope from lossless Git changed-path records."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]

BUILD_PREFIXES = (
    "src/",
    "tests/",
    "scripts/",
    "samples/generated/",
    ".github/workflows/",
)
BUILD_EXACT = {
    ".gitmodules",
    "Directory.Build.props",
    "QS3D.sln",
    "QS3D.V26.sln",
}
SOURCE_EXACT = {
    "CI_POLICY.md",
    "AGENTS.md",
    "README.md",
    "docs/MAIN-WRITE-AUTHORIZATION.md",
    "docs/AGENT-WORK-REGISTRATION.md",
    "docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md",
    "docs/AGENT-STATUS-MARKER-SEMANTICS.md",
    "docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md",
}


class ScopeError(RuntimeError):
    pass


def parse_nul_paths(raw: bytes) -> list[str]:
    """Decode Git `-z` path records without newline/quote tokenization."""
    if not raw:
        return []
    if not raw.endswith(b"\0"):
        raise ScopeError("Git changed-path output is not NUL-terminated")

    encoded_paths = raw[:-1].split(b"\0")
    if any(not item for item in encoded_paths):
        raise ScopeError("Git changed-path output contains an empty path record")

    paths: list[str] = []
    for item in encoded_paths:
        try:
            path = item.decode("utf-8", errors="strict")
        except UnicodeDecodeError as exc:
            raise ScopeError("Git changed-path output contains a non-UTF-8 pathname") from exc
        if "\0" in path:
            raise ScopeError("Git changed-path output contains an embedded NUL")
        paths.append(path)
    return paths


def classify_path(path: str) -> tuple[bool, bool]:
    """Return (source_validation, build_validation) for one exact Git pathname."""
    build = path.startswith(BUILD_PREFIXES) or path in BUILD_EXACT
    source = build or path in SOURCE_EXACT
    return source, build


def classify_paths(paths: list[str]) -> tuple[bool, bool]:
    source = False
    build = False
    for path in paths:
        path_source, path_build = classify_path(path)
        source = source or path_source
        build = build or path_build
    return source, build


def changed_paths(base_ref: str, head_ref: str = "HEAD", root: Path = ROOT) -> list[str]:
    completed = subprocess.run(
        [
            "git",
            "diff",
            "--no-ext-diff",
            "--no-textconv",
            "--no-renames",
            "--name-only",
            "-z",
            f"{base_ref}...{head_ref}",
            "--",
        ],
        cwd=str(root),
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise ScopeError(
            f"could not classify changed paths against {base_ref} (git exit={completed.returncode}): {stderr}"
        )
    return parse_nul_paths(completed.stdout)


def write_outputs(output_path: Path, source: bool, build: bool) -> None:
    if not str(output_path):
        raise ScopeError("GITHUB_OUTPUT path is required")
    with output_path.open("a", encoding="utf-8", newline="\n") as stream:
        stream.write(f"source_validation={'true' if source else 'false'}\n")
        stream.write(f"build_validation={'true' if build else 'false'}\n")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="fetched Git comparison base ref")
    parser.add_argument("--head", default="HEAD", help="Git comparison head ref")
    parser.add_argument("--all", action="store_true", help="force source/build validation")
    parser.add_argument("--github-output", help="GitHub Actions output file; defaults to GITHUB_OUTPUT")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    output_value = args.github_output or os.environ.get("GITHUB_OUTPUT", "")
    if not output_value:
        print("ERROR: GITHUB_OUTPUT path is required", file=sys.stderr)
        return 1

    try:
        if args.all:
            paths: list[str] = []
            source, build = True, True
        else:
            if not args.base:
                raise ScopeError("--base is required unless --all is used")
            paths = changed_paths(args.base, args.head)
            source, build = classify_paths(paths)

        write_outputs(Path(output_value), source, build)
    except (OSError, ScopeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print(f"Validation scope: source_validation={'true' if source else 'false'} build_validation={'true' if build else 'false'}")
    if paths:
        print("Candidate changed paths:")
        for path in paths:
            print(" - " + json.dumps(path, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
