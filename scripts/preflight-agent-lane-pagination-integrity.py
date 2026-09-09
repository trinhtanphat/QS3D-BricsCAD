#!/usr/bin/env python3
"""Regression guard for fail-closed Reservation-v2 GitHub API pagination."""

from __future__ import annotations

import importlib.util
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

ROOT = Path(__file__).resolve().parents[1]
TARGETS = {
    "lane-collision": ROOT / "scripts" / "preflight-agent-lane-collision.py",
    "pre-acquisition": ROOT / "scripts" / "agent-reservation-precheck.py",
}


def load_target(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(f"qs3d_{name.replace('-', '_')}", path)
    if spec is None or spec.loader is None:
        raise SystemExit(f"ERROR: could not load {name} Reservation-v2 module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def request_page(url: str) -> int:
    values = parse_qs(urlsplit(url).query).get("page", [])
    if len(values) != 1:
        raise AssertionError(f"expected exactly one page query parameter: {url}")
    try:
        return int(values[0])
    except ValueError as exc:
        raise AssertionError(f"page query parameter was not an integer: {url}") from exc


def exercise_paginator(label: str, module, invoke) -> None:
    # A nominally full API page containing any malformed member must fail closed.
    # Filtering that member first would reduce the page to 99 and silently stop
    # before page 2, which can hide a later reservation or carrier collision.
    calls: list[str] = []

    def malformed_page(url: str, token: str):
        del token
        calls.append(url)
        if request_page(url) == 1:
            return [{"number": index + 1} for index in range(99)] + ["MALFORMED"]
        return [{"number": 1001}]

    original = module._request_json
    module._request_json = malformed_page
    try:
        try:
            invoke(module)
        except RuntimeError as exc:
            if "non-object" not in str(exc).lower() and "malformed" not in str(exc).lower():
                raise SystemExit(f"ERROR: {label} malformed page failed for the wrong reason: {exc}")
        else:
            raise SystemExit(
                f"ERROR: {label} accepted a malformed 100-entry page; "
                "later reservations can be skipped fail-open"
            )
    finally:
        module._request_json = original

    if len(calls) != 1:
        raise SystemExit(
            f"ERROR: {label} malformed page must fail immediately without querying later pages; "
            f"observed {len(calls)} request(s)"
        )

    # Preserve ordinary pagination: exactly 100 valid objects means page 2 is
    # required, and a short valid second page terminates deterministically.
    calls.clear()

    def two_valid_pages(url: str, token: str):
        del token
        calls.append(url)
        page = request_page(url)
        if page == 1:
            return [{"number": index + 1} for index in range(100)]
        if page == 2:
            return [{"number": 101}]
        raise AssertionError(f"unexpected {label} pagination request: {url}")

    module._request_json = two_valid_pages
    try:
        collected = invoke(module)
    finally:
        module._request_json = original

    if len(collected) != 101 or len(calls) != 2:
        raise SystemExit(
            f"ERROR: {label} valid 100+1 pagination contract regressed: "
            f"items={len(collected)} requests={len(calls)}"
        )
    if collected[-1].get("number") != 101:
        raise SystemExit(f"ERROR: {label} did not preserve the second-page reservation entry")


def main() -> None:
    collision = load_target("lane-collision", TARGETS["lane-collision"])
    precheck = load_target("pre-acquisition", TARGETS["pre-acquisition"])

    exercise_paginator(
        "lane-collision paginator",
        collision,
        lambda module: module._fetch_paged(
            "https://api.example.test", "owner/repo", "issues?state=open", "token"
        ),
    )
    exercise_paginator(
        "pre-acquisition paginator",
        precheck,
        lambda module: module._fetch_open_issues(
            "https://api.example.test", "owner/repo", "token"
        ),
    )

    print(
        "PASS: Reservation-v2 collision and pre-acquisition pagination reject malformed "
        "page members and preserve complete valid paging."
    )


if __name__ == "__main__":
    main()
