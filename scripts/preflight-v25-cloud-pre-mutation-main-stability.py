#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow = (root / ".github/workflows/release-v25-cloud.yml").read_text(encoding="utf-8")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    publish = text[text.find("      - name: Publish GitHub prerelease"):]
    first_post = publish.find('$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"')
    anchors = [
        publish.find('$preMutationMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main"'),
        publish.find("$preMutationMainRef = 'refs/remotes/origin/qs3d-release-pre-mutation-main'"),
        publish.find("$fetchedPreMutationMain -ne $preMutationMain"),
        publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationMain"),
        publish.find('$preMutationPublishMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main"'),
        publish.find("if ($preMutationPublishMain -ne $preMutationMain)"),
        publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationPublishMain"),
        first_post,
    ]
    if min(anchors) < 0:
        errors.append("pre-mutation exact-source ancestry/reconfirmation flow is incomplete")
    elif anchors != sorted(anchors):
        errors.append("pre-mutation order must remain main API/fetch -> ancestry -> reconfirmation -> moved-main ancestry -> draft POST")
    pre_window = publish[:first_post] if first_post >= 0 else publish
    for forbidden in ("$preMutationReleaseRelevantPaths", "$preMutationReleaseDriftStatus", "V25_RELEASE_SUPERSEDED", "successful no-op before the first persistent release mutation"):
        if forbidden in pre_window:
            errors.append(f"pre-mutation admission still contains stale-source suppression: {forbidden}")
    return errors

errors = validate(workflow)
if errors:
    raise SystemExit("V25 cloud pre-mutation pinned-source admission failed: " + "; ".join(errors))

mutated = workflow.replace("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationPublishMain", "git merge-base --is-ancestor $env:SOURCE_SHA $preMutationMain", 1)
if not validate(mutated):
    raise SystemExit("V25 pre-mutation moved-main ancestry mutation probe did not fail closed")
print("PASS V25 cloud pre-mutation admission preserves exact SOURCE_SHA ancestry even when protected main advances")
