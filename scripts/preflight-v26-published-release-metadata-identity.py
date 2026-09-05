#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
publisher = publisher_path.read_text(encoding="utf-8")


def published_assertion(text: str) -> str:
    start = text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction {")
    end = text.find("\n$isPrerelease =", start)
    if start < 0 or end < 0:
        raise SystemExit("V26 publisher could not bound published-release identity assertion")
    return text[start:end]


def validate(text: str) -> list[str]:
    errors: list[str] = []
    assertion = published_assertion(text)

    required_assertion_tokens = (
        "[string]$ExpectedReleaseName",
        "[string]$ExpectedReleaseBody",
        "[string]::Equals([string]$ReleaseSnapshot.name, $ExpectedReleaseName, [StringComparison]::Ordinal)",
        "[string]::Equals([string]$ReleaseSnapshot.body, $ExpectedReleaseBody, [StringComparison]::Ordinal)",
    )
    for token in required_assertion_tokens:
        if token not in assertion:
            errors.append(f"Published V26 release identity assertion missing metadata binding: {token}")

    snapshot = "$expectedPublishedBody = [string]$release.body"
    if snapshot not in text:
        errors.append("V26 publisher does not snapshot the exact server-admitted draft body before publication")

    initial_marker_check = "([string]$release.body).IndexOf($draftTransactionMarker, [StringComparison]::Ordinal) -lt 0"
    if initial_marker_check not in text:
        errors.append("Initial V26 draft admission no longer binds the run-unique transaction marker")
    elif snapshot in text and text.find(snapshot) < text.find(initial_marker_check):
        errors.append("V26 publisher snapshots admitted release body before validating the transaction marker")

    publish_request_start = text.find("$publishRequest = @{")
    publish_call = text.find("Invoke-RestMethod -Method Patch -Uri $releaseUri", publish_request_start)
    if publish_request_start < 0 or publish_call < 0:
        errors.append("V26 final publication does not use an explicit atomic publish request")
    else:
        publish_request = text[publish_request_start:publish_call]
        for token in (
            "draft = $false",
            "name = $expectedReleaseName",
            "body = $expectedPublishedBody",
        ):
            if token not in publish_request:
                errors.append(f"V26 atomic final publish request missing qualified metadata: {token}")
        patch_line_end = text.find("\n", publish_call)
        patch_line = text[publish_call:patch_line_end]
        if "-Body $publishRequest" not in patch_line:
            errors.append("V26 final publish PATCH is not bound to the qualified atomic publish request")

    call_marker = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
    calls = text.split(call_marker)[1:]
    if len(calls) != 2:
        errors.append(f"V26 publisher must have exactly two final identity assertion calls; found {len(calls)}")
    else:
        for index, call_tail in enumerate(calls, start=1):
            call = call_tail.split("\n}", 1)[0]
            if "-ExpectedReleaseName $expectedReleaseName" not in call:
                errors.append(f"Published-release identity call {index} omits exact expected release name")
            if "-ExpectedReleaseBody $expectedPublishedBody" not in call:
                errors.append(f"Published-release identity call {index} omits exact admitted release body")

    return errors


def require_mutation_failure(label: str, mutated: str) -> None:
    if mutated == publisher:
        raise SystemExit(f"{label} mutation probe could not mutate publisher fixture")
    if not validate(mutated):
        raise SystemExit(f"{label} mutation probe did not fail closed")


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
require_mutation_failure(
    "atomic publish name binding",
    publisher.replace("    name = $expectedReleaseName\n", "", 1),
)
require_mutation_failure(
    "atomic publish body binding",
    publisher.replace("    body = $expectedPublishedBody\n", "", 1),
)
require_mutation_failure(
    "direct publish expected body wiring",
    publisher.replace("    -ExpectedReleaseBody $expectedPublishedBody `\n", "", 1),
)
last_body_arg = publisher.rfind("          -ExpectedReleaseBody $expectedPublishedBody `\n")
if last_body_arg < 0:
    raise SystemExit("acknowledgement expected-body mutation probe could not locate call-site fixture")
require_mutation_failure(
    "acknowledgement expected body wiring",
    publisher[:last_body_arg] + publisher[last_body_arg + len("          -ExpectedReleaseBody $expectedPublishedBody `\n"):],
)

print("PASS final V26 publication atomically preserves exact admitted release name/body transaction identity")
