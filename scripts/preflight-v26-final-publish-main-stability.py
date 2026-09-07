#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
POST_CHECK = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
INVALIDATED_SET = "$publicationSafetyInvalidated = $true"
INVALIDATED_GUARD = "if ($publicationSafetyInvalidated)"
ACK_SUCCESS = "treating publication as committed"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    patch = text.find(PATCH)
    require(patch >= 0, "V26 publisher final publish PATCH was not found.")

    post_check = text.find(POST_CHECK, patch)
    require(post_check > patch,
            "V26 publisher must revalidate protected main after the final publish PATCH.")

    invalidated_set = text.find(INVALIDATED_SET, patch)
    require(invalidated_set > patch,
            "V26 post-publish protected-main failure must mark publication safety invalidated.")

    ack = text.find(ACK_SUCCESS, patch)
    require(ack > patch, "V26 ambiguous publication acknowledgement recovery marker was not found.")

    invalidated_guard = text.rfind(INVALIDATED_GUARD, patch, ack)
    require(invalidated_guard > patch,
            "V26 acknowledgement recovery must reject a known protected-main safety invalidation before treating publication as committed.")


text = PUBLISHER.read_text(encoding="utf-8")
validate(text)

for marker in (POST_CHECK, INVALIDATED_SET, INVALIDATED_GUARD):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V26 final-publish protected-main stability fence")
