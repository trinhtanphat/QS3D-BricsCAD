#!/usr/bin/env python3
from pathlib import Path
import importlib.util
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def fail(message: str) -> None:
    print("ERROR: " + message)
    raise SystemExit(1)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


spec = importlib.util.spec_from_file_location("qs3d_agent_lane_collision", TARGET)
require(spec is not None and spec.loader is not None, "unable to load reservation collision gate")
module = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = module
spec.loader.exec_module(module)

require(
    hasattr(module, "validate_pending_issue_bootstrap"),
    "reservation collision gate must expose bounded pending-bootstrap validation",
)
validate = module.validate_pending_issue_bootstrap

repo = "trinhtanphat/QS3D-BricsCAD"
pending = "agent/gpt56sol-20260905-bootstrap/issue-pending-background-semantic-hardening"
numbered = "agent/gpt56sol-20260905-bootstrap/issue-5726-background-semantic-hardening"
unnumbered = "agent/gpt56sol-20260905-bootstrap/background-semantic-hardening"

require(
    validate("push", pending, [], repo, []) is True,
    "zero-diff push bootstrap with no PR must be admitted",
)
require(
    validate("pull_request", pending, [], repo, []) is False,
    "pull_request events must never receive pending-bootstrap admission",
)
require(
    validate("push", numbered, [], repo, []) is False,
    "numbered carriers must continue through canonical Reservation-v2 validation",
)
require(
    validate("push", unnumbered, [], repo, []) is False,
    "arbitrary unnumbered agent branches must not receive bootstrap admission",
)

mutated_failed = False
try:
    validate("push", pending, [], repo, ["src/QS3D.Core/QS3D.Core.csproj"])
except ValueError as exc:
    mutated_failed = "no-mutation bootstrap" in str(exc) and "QS3D.Core.csproj" in str(exc)
require(mutated_failed, "pending bootstrap with repository mutation must fail closed")

open_pr = {
    "number": 9999,
    "head": {
        "ref": pending,
        "repo": {"full_name": repo},
    },
}
pr_failed = False
try:
    validate("push", pending, [open_pr], repo, [])
except ValueError as exc:
    pr_failed = "open PR #9999" in str(exc)
require(pr_failed, "pending bootstrap with an open PR must fail closed")

foreign_pr = {
    "number": 9998,
    "head": {
        "ref": pending,
        "repo": {"full_name": "other/repository"},
    },
}
require(
    validate("push", pending, [foreign_pr], repo, []) is True,
    "same branch name in another repository must not create a false local PR collision",
)

generic_owner_failed = False
try:
    validate("push", "agent/worker/issue-pending-bootstrap", [], repo, [])
except ValueError as exc:
    generic_owner_failed = "generic branch owner token" in str(exc)
require(generic_owner_failed, "pending bootstrap must retain owner-token validation")

source = TARGET.read_text(encoding="utf-8")
main_index = source.find("def main()")
require(main_index >= 0, "reservation collision main() could not be bounded")
bootstrap_call = source.find("validate_pending_issue_bootstrap(", main_index)
issue_binding = source.find("issue_number = branch_issue_number(head_ref)", main_index)
require(bootstrap_call >= 0, "main() must invoke pending-bootstrap validation")
require(issue_binding >= 0, "main() must retain numbered Issue binding")
require(
    bootstrap_call < issue_binding,
    "pending bootstrap must be evaluated before numbered Issue binding",
)
main_bootstrap_segment = source[bootstrap_call:issue_binding]
require(
    'current_changed_paths("main")' in main_bootstrap_segment,
    "main() must derive pending-bootstrap mutation evidence from protected main",
)
require(
    "open_prs" in main_bootstrap_segment and "event_name" in main_bootstrap_segment,
    "main() must bind bootstrap admission to event type and current open-PR metadata",
)

print("Agent reservation pending-bootstrap preflight passed.")
