#!/usr/bin/env python3
"""Regression guard for fail-closed Reservation-v2 GitHub API pagination."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_agent_lane_collision", TARGET)
    if spec is None or spec.loader is None:
        raise SystemExit("ERROR: could not load agent lane collision preflight module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> None:
    module = load_target()

    # A nominally full API page containing any malformed member must fail closed.
    # Filtering that member first would reduce the page to 99 and silently stop
    # before page 2, which can hide a later reservation or carrier collision.
    calls: list[str] = []

    def malformed_page(url: str, token: str):
        del token
        calls.append(url)
        if "page=1" in url:
            return [{"number": index + 1} for index in range(99)] + ["MALFORMED"]
        return [{"number": 1001}]

    original = module._request_json
    module._request_json = malformed_page
    try:
        try:
            module._fetch_paged("https://api.example.test", "owner/repo", "issues?state=open", "token")
        except RuntimeError as exc:
            if "non-object" not in str(exc).lower() and "malformed" not in str(exc).lower():
                raise SystemExit(f"ERROR: malformed page failed for the wrong reason: {exc}")
        else:
            raise SystemExit(
                "ERROR: paginated reservation scan accepted a malformed 100-entry page; "
                "later pages can be skipped fail-open"
            )
    finally:
        module._request_json = original

    if len(calls) != 1:
        raise SystemExit(
            "ERROR: malformed page must fail immediately without querying later pages; "
            f"observed {len(calls)} request(s)"
        )

    # Preserve ordinary pagination: exactly 100 valid objects means page 2 is
    # required, and a short valid second page terminates deterministically.
    calls.clear()

    def two_valid_pages(url: str, token: str):
        del token
        calls.append(url)
        if "page=1" in url:
            return [{"number": index + 1} for index in range(100)]
        if "page=2" in url:
            return [{"number": 101}]
        raise AssertionError(f"unexpected pagination request: {url}")

    module._request_json = two_valid_pages
    try:
        collected = module._fetch_paged(
            "https://api.example.test", "owner/repo", "pulls?state=open", "token"
        )
    finally:
        module._request_json = original

    if len(collected) != 101 or len(calls) != 2:
        raise SystemExit(
            "ERROR: valid 100+1 pagination contract regressed: "
            f"items={len(collected)} requests={len(calls)}"
        )
    if collected[-1].get("number") != 101:
        raise SystemExit("ERROR: second-page reservation entry was not preserved")

    print("PASS: Reservation-v2 pagination rejects malformed page members and preserves complete valid paging.")


if __name__ == "__main__":
    main()
