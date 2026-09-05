#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
publisher = publisher_path.read_text(encoding="utf-8")


def bounded_function(text: str, name: str, next_name: str) -> str:
    start = text.find(f"function {name} {{")
    if start < 0:
        raise SystemExit(f"V26 publisher missing {name}")
    end = text.find(f"function {next_name} {{", start + 1)
    if end < 0:
        raise SystemExit(f"V26 publisher could not bound {name}")
    return text[start:end]


def validate(text: str) -> list[str]:
    errors: list[str] = []
    assertion = bounded_function(
        text,
        "Assert-PublishedReleaseMatchesVerifiedTransaction",
        "Resolve-AmbiguousDraftCreate",
    ) if text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction {") < text.find("function Resolve-AmbiguousDraftCreate {") else None
    if assertion is None:
        # Current source declares Resolve-AmbiguousDraftCreate first; bound the
        # final assertion against the first statement that follows it.
        start = text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction {")
        end = text.find("\n$isPrerelease =", start)
        if start < 0 or end < 0:
            return ["V26 publisher could not bound published-release identity assertion"]
        assertion = text[start:end]

    required_assertion_tokens = (
        "[string]$ExpectedReleaseName",
        "[string]$ExpectedReleaseBody",
        "[string]$ReleaseSnapshot.name",
        "$ExpectedReleaseName",
        "[StringComparison]::Ordinal",
        "[string]$ReleaseSnapshot.body",
        "$ExpectedReleaseBody",
    )
    for token in required_assertion_tokens:
        if token not in assertion:
            errors.append(f"Published V26 release identity assertion missing metadata binding: {token}")

    if "$expectedPublishedBody = [string]$release.body" not in text:
        errors.append("V26 publisher does not snapshot the exact server-admitted draft body before publication")

    calls = text.split("Assert-PublishedReleaseMatchesVerifiedTransaction")[2:]
    if len(calls) < 2:
        errors.append("V26 publisher must bind both direct publish and acknowledgement reconciliation paths")
    else:
        for index, call_tail in enumerate(calls[:2], start=1):
            call = call_tail.split("\n}", 1)[0]
            if "-ExpectedReleaseName $expectedReleaseName" not in call:
                errors.append(f"Published-release identity call {index} omits exact expected release name")
            if "-ExpectedReleaseBody $expectedPublishedBody" not in call:
                errors.append(f"Published-release identity call {index} omits exact admitted release body")

    initial_marker_check = "([string]$release.body).IndexOf($draftTransactionMarker, [StringComparison]::Ordinal) -lt 0"
    if initial_marker_check not in text:
        errors.append("Initial V26 draft admission no longer binds the run-unique transaction marker")

    return errors


errors = validate(publisher)
if errors:
    raise SystemExit("V26 published release metadata identity preflight failed: " + "; ".join(errors))

print("PASS final V26 publication preserves exact admitted release name/body transaction identity")
