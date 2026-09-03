#!/usr/bin/env python3
"""Require fail-closed final draft-release revalidation before publication."""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def main() -> int:
    source = WORKFLOW.read_text(encoding="utf-8")
    failures: list[str] = []

    final_patch = source.find("$publishBody = @{ draft = $false } | ConvertTo-Json")
    if final_patch < 0:
        failures.append("could not locate the final draft=false publish transition")
    else:
        prefix = source[:final_patch]
        final_get = prefix.rfind("$finalDraftRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers")
        integrity = prefix.rfind("verify-v25-package.ps1")
        if final_get < 0 or final_get < integrity:
            failures.append(
                "workflow does not re-fetch the draft release after byte/package verification "
                "and immediately before the draft=false publication transition"
            )

    required_signals = (
        "$finalDraftRelease.draft -ne $true",
        "$finalDraftRelease.prerelease -ne $true",
        "$finalDraftRelease.tag_name",
        "$finalDraftRelease.target_commitish",
        "$verifiedReleaseAssetIds",
        "$finalReleaseAssetIds",
        "Compare-Object",
    )
    missing = [token for token in required_signals if token not in source]
    if missing:
        failures.append(
            "final publish admission does not pin draft/tag/target and stable verified asset "
            "identity; missing: " + ", ".join(missing)
        )

    if re.search(r"(?is)\$publishBody\s*=.*?draft\s*=\s*\$false.*?Invoke-RestMethod\s+-Method\s+Patch", source) is None:
        failures.append("final publish transition contract changed unexpectedly")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: cloud V25 draft release state is revalidated immediately before publish")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
