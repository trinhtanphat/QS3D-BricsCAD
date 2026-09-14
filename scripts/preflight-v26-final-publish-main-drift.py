#!/usr/bin/env python3
"""Guard V26 final publish PATCH against stale protected-main drift."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"


class GuardFailure(RuntimeError):
    pass


def _final_publish_section(text: str) -> str:
    marker = "$publishPatchAttempted = $true"
    start = text.find(marker)
    if start < 0:
        raise GuardFailure("missing final publish transaction marker")
    return text[start:]


def validate(publisher: str) -> None:
    section = _final_publish_section(publisher)
    admission = "Assert-ProtectedMainStableForPublisherMutation -Phase 'final-release-publish'"
    patch = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    post = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"

    if section.count(admission) != 1:
        raise GuardFailure("final V26 publish must have exactly one immediate protected-main admission")
    if section.count(patch) != 1:
        raise GuardFailure("final V26 publish PATCH must appear exactly once")
    if section.count(post) != 1:
        raise GuardFailure("post-PATCH protected-main reconciliation must remain exactly once")

    admission_index = section.find(admission)
    patch_index = section.find(patch)
    post_index = section.find(post)
    request_index = section.find("$publishRequest = @{")
    invalidated_index = section.find("$publicationSafetyInvalidated = $true")
    if min(request_index, invalidated_index) < 0:
        raise GuardFailure("final V26 publication safety markers are missing")
    if not (request_index < admission_index < invalidated_index < patch_index < post_index):
        raise GuardFailure(
            "final protected-main admission must be after request construction, before safety invalidation/PATCH, with post-PATCH reconciliation preserved"
        )


def _must_reject(text: str, label: str) -> None:
    try:
        validate(text)
    except GuardFailure:
        return
    raise GuardFailure(f"mutation self-check was not rejected: {label}")


def main() -> int:
    publisher = PUBLISHER.read_text(encoding="utf-8")
    validate(publisher)

    admission = "Assert-ProtectedMainStableForPublisherMutation -Phase 'final-release-publish'"
    _must_reject(publisher.replace(admission, "# removed final admission", 1), "missing final admission")
    _must_reject(
        publisher.replace(admission, "# moved final admission", 1).replace(
            "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'",
            admission + "\n    Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'",
            1,
        ),
        "final admission moved after PATCH",
    )

    print("PASS: V26 publisher re-admits protected main immediately before final public release PATCH.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (GuardFailure, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
