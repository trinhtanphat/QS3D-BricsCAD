#!/usr/bin/env python3
"""Regression guard for protection-safe automatic V25 preview version PRs."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "v25-auto-version-pr.py"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
PROJECT_PATHS = (
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
    "src/QS3D.Core/QS3D.Core.csproj",
)


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def expect_error(fn, label: str) -> None:
    try:
        fn()
    except Exception:
        return
    fail(f"expected fail-closed error: {label}")

def load_helper():
    if not HELPER.exists():
        fail("automatic V25 version-PR helper is missing")
    spec = importlib.util.spec_from_file_location("v25_auto_version_pr", HELPER)
    if spec is None or spec.loader is None:
        fail("automatic V25 version-PR helper cannot be loaded")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def test_identity_core(module) -> None:
    identity = module.parse_preview_identity("0.2.0-preview.6")
    if identity.product_version != "0.2.0-preview.6" or identity.tag != "v0.2.0-preview.6":
        fail("canonical preview identity rendering changed")
    if module.choose_next_ordinal(committed=6, published=6, occupied={6, 7}) != 8:
        fail("burned next ordinal must be skipped monotonically")
    if module.classify_committed_identity(
        committed=7, published=6, exact_tag=False, prior_owner=False
    ) != "release":
        fail("newer unburned committed identity must stay on release path")
    if module.classify_committed_identity(
        committed=7, published=6, exact_tag=True, prior_owner=False
    ) != "prepare":
        fail("tagged committed identity must request protected version preparation")
    if module.classify_committed_identity(
        committed=7, published=6, exact_tag=False, prior_owner=True
    ) != "prepare":
        fail("prior-owned committed identity must request protected version preparation")
    if module.classify_committed_identity(
        committed=6, published=6, exact_tag=False, prior_owner=False
    ) != "prepare":
        fail("published committed identity must request protected version preparation")

    for invalid in (
        "0.2.0-preview.07",
        "0.2.0-preview.0",
        "00.2.0-preview.7",
        "0.2.0-preview.65536",
        "0.2.0-preview.bad",
        " 0.2.0-preview.7",
    ):
        expect_error(lambda value=invalid: module.parse_preview_identity(value), invalid)
    expect_error(
        lambda: module.choose_next_ordinal(committed=65535, published=65535, occupied=set()),
        "preview ordinal exhaustion",
    )


def test_project_rendering(module) -> None:
    source = """<Project>\n  <PropertyGroup>\n    <Version>0.2.0-preview.6</Version>\n    <AssemblyVersion>0.1.0.0</AssemblyVersion>\n    <FileVersion>0.2.0.6</FileVersion>\n    <InformationalVersion>0.2.0-preview.6</InformationalVersion>\n  </PropertyGroup>\n</Project>\n"""
    target = module.parse_preview_identity("0.2.0-preview.8")
    rendered = module.rewrite_project_text(source, target)
    for token in (
        "<Version>0.2.0-preview.8</Version>",
        "<FileVersion>0.2.0.8</FileVersion>",
        "<InformationalVersion>0.2.0-preview.8</InformationalVersion>",
        "<AssemblyVersion>0.1.0.0</AssemblyVersion>",
    ):
        if token not in rendered:
            fail(f"project renderer lost required identity token: {token}")
    if rendered.count("<Version>") != 1 or rendered.count("<FileVersion>") != 1:
        fail("project renderer must preserve exactly one identity element")
    expect_error(
        lambda: module.rewrite_project_text(source.replace("<Version>", "<Version>bad</Version><Version>"), target),
        "duplicate Version element",
    )


def test_reservation_metadata(module) -> None:
    target = module.parse_preview_identity("0.2.0-preview.8")
    branch = module.canonical_branch_name(7001, target.ordinal)
    expected_branch = "agent/qs3d-release-automation/issue-7001-v25-preview-8"
    if branch != expected_branch:
        fail(f"canonical automation branch changed: {branch}")
    body = module.build_reservation_body(7001, branch, target)
    for token in (
        "Lane-Key: issue-7001",
        "Reservation-Protocol: v2",
        "Canonical owner/session: qs3d-release-automation",
        f"Canonical carrier: {expected_branch}",
        "Ownership-Key: release/v25-preview-version/0.2.0",
        "Automation-Key: v25-auto-version-pr:v0.2.0-preview.8",
        "Target-Version: 0.2.0-preview.8",
    ):
        if token not in body:
            fail(f"generated reservation body missing: {token}")
    expected_paths = "Expected-Paths: " + ";".join(PROJECT_PATHS)
    if expected_paths not in body:
        fail("generated reservation Expected-Paths must be exactly the three aligned project files")


class FakeLedgerClient:
    repository = "owner/repo"

    def __init__(self, rows):
        self.rows = rows

    def api(self, path: str, *, method: str = "GET", body=None):
        if "/issues/1441/comments?" not in path or method != "GET":
            fail(f"unexpected fake ledger API call: {method} {path}")
        return self.rows


def test_ledger_authority(module) -> None:
    bot_sha = "a" * 40
    outsider_sha = "b" * 40
    rows = [
        {"user": {"login": "someone"}, "body": f"QS3D_V25_PREVIEW_RESERVATION ordinal=7 source_sha={outsider_sha} run_id=10"},
        {"user": {"login": "github-actions[bot]"}, "body": f"QS3D_V25_PREVIEW_RESERVATION ordinal=8 source_sha={bot_sha} run_id=11"},
    ]
    if module._collect_ledger_ordinals(FakeLedgerClient(rows), 1441) != {8}:
        fail("only canonical github-actions[bot] ledger rows may burn automatic preview ordinals")
    malformed = [{"user": {"login": "github-actions[bot]"}, "body": "QS3D_V25_PREVIEW_RESERVATION ordinal=bad"}]
    expect_error(lambda: module._collect_ledger_ordinals(FakeLedgerClient(malformed), 1441), "malformed bot ledger row")

class FakeBranchRaceClient:
    repository = "owner/repo"

    def __init__(self, base_sha: str, error_type):
        self.base_sha = base_sha
        self.error_type = error_type
        self.created = False

    def api(self, path: str, *, method: str = "GET", body=None):
        if "/git/ref/heads/" in path and method == "GET":
            if not self.created:
                raise self.error_type("HTTP 404 Not Found")
            return {"object": {"sha": self.base_sha}}
        if path.endswith("/git/refs") and method == "POST":
            self.created = True
            raise self.error_type("HTTP 422 Reference already exists")
        fail(f"unexpected fake branch API call: {method} {path}")


def test_branch_creation_race(module) -> None:
    base_sha = "c" * 40
    client = FakeBranchRaceClient(base_sha, module.AutoVersionError)
    try:
        resolved = module._ensure_branch_ref(client, "agent/qs3d-release-automation/issue-7-v25-preview-8", base_sha)
    except Exception as exc:
        fail(f"canonical branch creation race must reconcile an exact-base ref: {exc}")
    if resolved != base_sha:
        fail("canonical branch creation race returned the wrong ref SHA")

def test_helper_source_contract() -> None:
    source = HELPER.read_text(encoding="utf-8")
    required = (
        "scripts/agent-reservation-precheck.py",
        "/git/refs",
        "/git/blobs",
        "/git/trees",
        "/git/commits",
        "/compare/",
        "/pulls",
        '"force": False',
        "Closes #",
        "Automation-Key:",
        "Target-Version:",
    )
    for token in required:
        if token not in source:
            fail(f"automatic version-PR helper missing durable transaction token: {token}")
    for forbidden in (
        "git push",
        "--force",
        "/pulls/{pull_number}/merge",
        "refs/heads/main\"},",
    ):
        if forbidden in source:
            fail(f"automatic version-PR helper contains forbidden direct integration primitive: {forbidden}")


def test_workflow_contract() -> None:
    source = DISPATCH.read_text(encoding="utf-8")
    required = (
        "QS3D_AUTOMERGE_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
        'GH_TOKEN="${QS3D_AUTOMERGE_TOKEN}"',
        "python scripts/v25-auto-version-pr.py prepare",
        "protected V25 version PR preparation",
        "committed_preview_ordinal <= published_preview_ordinal",
        "already has Git tag ${tag}",
        "already belongs to earlier protected-main source",
        "gh workflow run release-v25-cloud.yml",
        '-f source_sha="${source_sha}"',
        '-f release_tag="${tag}"',
    )
    for token in required:
        if token not in source:
            fail(f"dispatcher missing protected version-PR routing token: {token}")
    for forbidden in (
        "git push origin main",
        "git push --force",
        "gh pr merge",
        "/pulls/${pull_number}/merge",
    ):
        if forbidden in source:
            fail(f"dispatcher contains forbidden protected-main integration primitive: {forbidden}")


def main() -> int:
    module = load_helper()
    test_identity_core(module)
    test_project_rendering(module)
    test_reservation_metadata(module)
    test_ledger_authority(module)
    test_branch_creation_race(module)
    test_helper_source_contract()
    test_workflow_contract()
    print(
        "PASS: automatic V25 version preparation is monotonic, Reservation-v2 visible, atomic-branch based, protected-main safe, and dispatcher-integrated"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())