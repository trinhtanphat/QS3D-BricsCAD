#!/usr/bin/env python3
import re
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


def has_active_line(text: str, literal: str) -> bool:
    return re.search(r"(?m)^\s*" + re.escape(literal) + r"\s*$", text) is not None


def validate(text: str) -> list[str]:
    errors: list[str] = []
    assertion = published_assertion(text)

    required_assertion_tokens = (
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
            "name = $expectedReleaseName",
            "body = $expectedPublishedBody",
        ):
            if not has_active_line(publish_request, literal):
                errors.append(f"V26 atomic final publish request missing active qualified metadata: {literal}")
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
            for literal, label in (
                ("-ExpectedReleaseName $expectedReleaseName `", "exact expected release name"),
                ("-ExpectedReleaseBody $expectedPublishedBody `", "exact admitted release body"),
            ):
                if not has_active_line(call, literal):
                    errors.append(f"Published-release identity call {index} omits active {label} wiring")

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
    "commented atomic publish name binding",
    publisher.replace("    name = $expectedReleaseName\n", "    # name = $expectedReleaseName\n", 1),
)
require_mutation_failure(
    "commented atomic publish body binding",
    publisher.replace("    body = $expectedPublishedBody\n", "    # body = $expectedPublishedBody\n", 1),
)
require_mutation_failure(
    "commented direct publish expected body wiring",
    publisher.replace("    -ExpectedReleaseBody $expectedPublishedBody `\n", "    # -ExpectedReleaseBody $expectedPublishedBody `\n", 1),
)
last_body_arg = publisher.rfind("          -ExpectedReleaseBody $expectedPublishedBody `\n")
if last_body_arg < 0:
    raise SystemExit("acknowledgement expected-body mutation probe could not locate call-site fixture")
require_mutation_failure(
    "commented acknowledgement expected body wiring",
    publisher[:last_body_arg] + "          # -ExpectedReleaseBody $expectedPublishedBody `\n" + publisher[last_body_arg + len("          -ExpectedReleaseBody $expectedPublishedBody `\n"):],
)

print("PASS final V26 publication atomically preserves exact admitted release name/body transaction identity")
