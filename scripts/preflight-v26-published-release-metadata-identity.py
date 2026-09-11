#!/usr/bin/env python3
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
publisher = publisher_path.read_text(encoding="utf-8")


def published_assertion(text: str) -> str:
    start = text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction {")
    if start < 0:
        raise SystemExit("V26 publisher could not locate published-release identity assertion")
    end = text.find("\nfunction ", start + 1)
    if end < 0:
        end = text.find("\n$isPrerelease =", start)
    if end < 0:
        raise SystemExit("V26 publisher could not bound published-release identity assertion")
    return text[start:end]


def published_identity_calls(text: str) -> list[str]:
    lines = text.splitlines()
    calls: list[str] = []
    marker = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
    for index, line in enumerate(lines):
        if line.strip() != marker:
            continue
        call = [line]
        cursor = index + 1
        while cursor < len(lines):
            call.append(lines[cursor])
            if not lines[cursor].rstrip().endswith("`"):
                break
            cursor += 1
        calls.append("\n".join(call))
    return calls


def has_active_line(text: str, literal: str) -> bool:
    return re.search(r"(?m)^\s*" + re.escape(literal) + r"\s*$", text) is not None


def validate(text: str) -> list[str]:
    errors: list[str] = []
    assertion = published_assertion(text)

    required_assertion_tokens = (
        "[string]::Equals([string]$ReleaseSnapshot.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal)",
        "[string]::Equals([string]$ReleaseSnapshot.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)",
        "[bool]$ReleaseSnapshot.prerelease -ne $IsPrerelease",
        "[string]::Equals([string]$ReleaseSnapshot.name, $ExpectedReleaseName, [StringComparison]::Ordinal)",
        "[string]::Equals([string]$ReleaseSnapshot.body, $ExpectedReleaseBody, [StringComparison]::Ordinal)",
    )
    for token in required_assertion_tokens:
        if token not in assertion:
            errors.append(f"Published V26 release identity assertion missing metadata binding: {token}")
    for parameter in (
        "[Parameter(Mandatory = $true)][string]$ExpectedReleaseName,",
        "[Parameter(Mandatory = $true)][string]$ExpectedReleaseBody,",
    ):
        if not has_active_line(assertion, parameter):
            errors.append(f"Published V26 release identity assertion missing active metadata parameter: {parameter}")

    snapshot = "$expectedPublishedBody = [string]$release.body"
    if not has_active_line(text, snapshot):
        errors.append("V26 publisher does not actively snapshot the exact server-admitted draft body before publication")

    initial_marker_check = "([string]$release.body).IndexOf($draftTransactionMarker, [StringComparison]::Ordinal) -lt 0) {"
    marker_index = text.find(initial_marker_check)
    snapshot_index = text.find(snapshot)
    if marker_index < 0:
        errors.append("Initial V26 draft admission no longer binds the run-unique transaction marker")
    elif snapshot_index >= 0 and snapshot_index < marker_index:
        errors.append("V26 publisher snapshots admitted release body before validating the transaction marker")

    publish_request_start = text.find("$publishRequest = @{")
    publish_call = text.find("Invoke-RestMethod -Method Patch -Uri $releaseUri", publish_request_start)
    if publish_request_start < 0 or publish_call < 0:
        errors.append("V26 final publication does not use an explicit atomic publish request")
    else:
        publish_request = text[publish_request_start:publish_call]
        for literal in (
            "draft = $false",
            "tag_name = $env:RELEASE_TAG",
            "target_commitish = $env:GITHUB_SHA",
            "prerelease = $isPrerelease",
            "name = $expectedReleaseName",
            "body = $expectedPublishedBody",
        ):
            if not has_active_line(publish_request, literal):
                errors.append(f"V26 atomic final publish request missing active qualified metadata: {literal}")
        patch_line_end = text.find("\n", publish_call)
        patch_line = text[publish_call:patch_line_end]
        if "-Body $publishRequest" not in patch_line:
            errors.append("V26 final publish PATCH is not bound to the qualified atomic publish request")

    calls = published_identity_calls(text)
    expected_calls = (
        ("$currentRelease", "pre-delete authoritative compensation proof", "$ExpectedReleaseName", "$ExpectedReleaseBody"),
        ("$remainingRelease", "post-delete surviving-release compensation proof", "$ExpectedReleaseName", "$ExpectedReleaseBody"),
        ("$published", "direct publish acknowledgement proof", "$expectedReleaseName", "$expectedPublishedBody"),
        ("$reconciledRelease", "ambiguous publish acknowledgement reconciliation proof", "$expectedReleaseName", "$expectedPublishedBody"),
    )
    if len(calls) != len(expected_calls):
        errors.append(
            f"V26 publisher must have exactly {len(expected_calls)} identity assertion calls; found {len(calls)}"
        )
    for snapshot, label, expected_name, expected_body in expected_calls:
        snapshot_line = f"-ReleaseSnapshot {snapshot} `"
        matches = [call for call in calls if has_active_line(call, snapshot_line)]
        if len(matches) != 1:
            errors.append(f"V26 publisher must have exactly one {label}; found {len(matches)}")
            continue
        call = matches[0]
        for literal, binding_label in (
            (f"-ExpectedReleaseName {expected_name} `", "exact expected release name"),
            (f"-ExpectedReleaseBody {expected_body} `", "exact admitted release body"),
        ):
            if not has_active_line(call, literal):
                errors.append(f"{label} omits active {binding_label} wiring")

    return errors


def require_mutation_failure(label: str, mutated: str) -> None:
    if mutated == publisher:
        raise SystemExit(f"{label} mutation probe could not mutate publisher fixture")
    if not validate(mutated):
        raise SystemExit(f"{label} mutation probe did not fail closed")


def comment_identity_binding(text: str, snapshot: str, binding: str) -> str:
    lines = text.splitlines(keepends=True)
    snapshot_line = f"-ReleaseSnapshot {snapshot} `"
    in_call = False
    for index, line in enumerate(lines):
        stripped = line.strip()
        if stripped == snapshot_line:
            in_call = True
            continue
        if not in_call:
            continue
        if stripped == binding:
            indent = line[: len(line) - len(line.lstrip())]
            ending = "\n" if line.endswith("\n") else ""
            lines[index] = indent + "# " + binding + ending
            return "".join(lines)
        if stripped and not stripped.endswith("`"):
            break
    return text


errors = validate(publisher)
if errors:
    raise SystemExit("V26 published release metadata identity preflight failed: " + "; ".join(errors))

require_mutation_failure(
    "published release name comparison",
    publisher.replace(
        "  if (-not [string]::Equals([string]$ReleaseSnapshot.name, $ExpectedReleaseName, [StringComparison]::Ordinal)) { throw \"Published V26 release name mismatch during acknowledgement reconciliation.\" }\n",
        "",
        1,
    ),
)
require_mutation_failure(
    "published release body comparison",
    publisher.replace(
        "  if (-not [string]::Equals([string]$ReleaseSnapshot.body, $ExpectedReleaseBody, [StringComparison]::Ordinal)) { throw \"Published V26 release body transaction identity mismatch during acknowledgement reconciliation.\" }\n",
        "",
        1,
    ),
)
for label, literal in (
    ("atomic publish tag binding", "    tag_name = $env:RELEASE_TAG\n"),
    ("atomic publish target binding", "    target_commitish = $env:GITHUB_SHA\n"),
    ("atomic publish prerelease binding", "    prerelease = $isPrerelease\n"),
    ("atomic publish name binding", "    name = $expectedReleaseName\n"),
    ("atomic publish body binding", "    body = $expectedPublishedBody\n"),
):
    require_mutation_failure(label, publisher.replace(literal, "    # " + literal.strip() + "\n", 1))
for label, snapshot_name, body_binding in (
    ("commented compensation pre-delete expected body wiring", "$currentRelease", "-ExpectedReleaseBody $ExpectedReleaseBody `"),
    ("commented compensation post-delete expected body wiring", "$remainingRelease", "-ExpectedReleaseBody $ExpectedReleaseBody `"),
    ("commented direct publish expected body wiring", "$published", "-ExpectedReleaseBody $expectedPublishedBody `"),
    ("commented acknowledgement expected body wiring", "$reconciledRelease", "-ExpectedReleaseBody $expectedPublishedBody `"),
):
    require_mutation_failure(label, comment_identity_binding(publisher, snapshot_name, body_binding))


print("PASS final V26 publication atomically preserves exact qualified mutable release metadata identity")
