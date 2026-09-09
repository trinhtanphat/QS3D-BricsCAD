#!/usr/bin/env python3
"""Regression guard for pre-mutation Reservation-v2 acquisition checks."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts/agent-reservation-precheck.py"
errors: list[str] = []


def expect(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


if not HELPER.is_file():
    errors.append("missing scripts/agent-reservation-precheck.py")
    module = None
else:
    spec = importlib.util.spec_from_file_location("agent_reservation_precheck", HELPER)
    if spec is None or spec.loader is None:
        errors.append("could not load agent reservation precheck helper")
        module = None
    else:
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)

if module is not None:
    expect(module.path_claims_overlap("scripts/a.py", "scripts/a.py"), "exact-file overlap must be detected")
    expect(module.path_claims_overlap("src/Owned/", "src/Owned/File.cs"), "directory-prefix overlap must be detected")
    expect(not module.path_claims_overlap("src/A/", "src/B/File.cs"), "non-overlapping directory claims must stay independent")

    current = {
        "number": 200,
        "state": "open",
        "created_at": "2026-09-09T00:10:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ci/current\nExpected-Paths: scripts/a.py; src/Owned/",
    }
    exact_peer = {
        "number": 100,
        "state": "open",
        "created_at": "2026-09-09T00:00:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ci/other\nExpected-Paths: scripts/a.py",
    }
    directory_peer = {
        "number": 101,
        "state": "open",
        "created_at": "2026-09-09T00:01:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ci/other-2\nExpected-Paths: src/Owned/File.cs",
    }
    ownership_peer = {
        "number": 102,
        "state": "open",
        "created_at": "2026-09-09T00:02:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ci/current\nExpected-Paths: docs/unrelated.md",
    }
    later_peer = {
        "number": 300,
        "state": "open",
        "created_at": "2026-09-09T00:20:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ci/current\nExpected-Paths: scripts/a.py",
    }
    malformed_peer = {
        "number": 99,
        "state": "open",
        "created_at": "2026-09-08T23:59:00Z",
        "body": "Reservation-Protocol: v2\nOwnership-Key: ???\nExpected-Paths: ",
    }

    conflicts = module.find_preacquisition_conflicts(
        current,
        [current, exact_peer, directory_peer, ownership_peer, later_peer, malformed_peer],
    )
    numbers = {int(item["issue_number"]) for item in conflicts}
    kinds = {(int(item["issue_number"]), str(item["kind"])) for item in conflicts}
    expect(200 not in numbers, "current Issue must exclude itself")
    expect(300 not in numbers, "later reservation must not displace the earlier current reservation")
    expect((100, "expected-path") in kinds, "earlier exact-file owner must block acquisition")
    expect((101, "expected-path") in kinds, "earlier directory-prefix owner must block acquisition")
    expect((102, "ownership-key") in kinds, "earlier semantic Ownership-Key owner must block acquisition")
    expect(99 not in numbers, "malformed peer metadata must not crash or masquerade as a valid reservation")

for rel, required in {
    "AGENTS.md": (
        "agent-reservation-precheck.py --issue <N>",
        "before repository mutation",
        "all open Reservation-v2 Issues",
    ),
    "docs/AGENT-RESERVATION-V2.md": (
        "agent-reservation-precheck.py --issue <N>",
        "pre-acquisition",
        "all open v2 Issues",
    ),
}.items():
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing {rel}")
        continue
    text = path.read_text(encoding="utf-8")
    for token in required:
        expect(token in text, f"{rel} missing pre-acquisition contract token: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} reservation precheck regression error(s).")
    raise SystemExit(1)

print("PASS: Reservation-v2 pre-acquisition helper and mandatory agent contract are regression-covered.")
