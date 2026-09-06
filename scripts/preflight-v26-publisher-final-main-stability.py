#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

WORKFLOW_BINDING = '"V26_RELEASE_ADMITTED_MAIN_SHA=$publishMain" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append'
PUBLISHER_ENV = "'V26_RELEASE_ADMITTED_MAIN_SHA'"
PUBLISHER_RAW = "$admittedMain = [string]$env:V26_RELEASE_ADMITTED_MAIN_SHA"
PUBLISHER_CANONICAL = "if ($admittedMain -notmatch '^[0-9a-f]{40}$')"
PUBLISHER_MAIN_GET = '$publisherMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main" -Headers $headers'
PUBLISHER_MAIN_RAW = "$publisherMain = [string]$publisherMainResponse.sha"
PUBLISHER_MAIN_CANONICAL = "if ($publisherMain -notmatch '^[0-9a-f]{40}$')"
PUBLISHER_EQUALITY = "[string]::Equals($publisherMain, $admittedMain, [StringComparison]::Ordinal)"
PUBLISHER_STABILITY_CALL = "Assert-AdmittedProtectedMainStable"
FIRST_MUTATION = "Invoke-RestMethod -Method Post"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(workflow: str, publisher: str) -> None:
    require(WORKFLOW_BINDING in workflow,
            "V26 release workflow must export the exact publish-time protected-main SHA for publisher admission.")
    require(PUBLISHER_ENV in publisher,
            "V26 publisher must require the workflow-admitted protected-main identity.")
    require(PUBLISHER_RAW in publisher,
            "V26 publisher must bind the admitted main SHA from the raw environment value.")
    require(PUBLISHER_CANONICAL in publisher,
            "V26 publisher must require one canonical lowercase 40-hex admitted-main SHA without normalization.")
    require("V26_RELEASE_ADMITTED_MAIN_SHA).Trim" not in publisher and "$admittedMain.Trim(" not in publisher,
            "V26 publisher must not trim or normalize the admitted protected-main identity.")
    require(PUBLISHER_MAIN_GET in publisher,
            "V26 publisher must re-read protected main through the authenticated GitHub API.")
    require(PUBLISHER_MAIN_RAW in publisher and PUBLISHER_MAIN_CANONICAL in publisher,
            "V26 publisher must validate the raw publisher-time protected-main SHA canonically.")
    require(PUBLISHER_EQUALITY in publisher,
            "V26 publisher must require exact ordinal equality with the workflow-admitted protected-main SHA.")

    first_post = publisher.find(FIRST_MUTATION)
    require(first_post >= 0, "Expected V26 publisher remote mutation marker was not found.")
    main_get = publisher.find(PUBLISHER_MAIN_GET)
    equality = publisher.find(PUBLISHER_EQUALITY)
    require(0 <= main_get < first_post and 0 <= equality < first_post,
            "V26 publisher protected-main revalidation must complete before the first remote mutation.")

    # The stability verifier must fail closed rather than merely log a mismatch.
    equality_window = publisher[equality:equality + 900]
    require("throw" in equality_window.lower(),
            "V26 publisher protected-main mismatch must fail closed before mutation.")


def expect_failure(workflow: str, publisher: str, label: str) -> None:
    try:
        validate(workflow, publisher)
    except SystemExit:
        return
    raise SystemExit(f"Mutation probe unexpectedly passed: {label}")


workflow = WORKFLOW.read_text(encoding="utf-8")
publisher = PUBLISHER.read_text(encoding="utf-8")
validate(workflow, publisher)

expect_failure(workflow.replace(WORKFLOW_BINDING, "# removed admitted-main binding", 1), publisher,
               "workflow binding removal")
expect_failure(workflow, publisher.replace(PUBLISHER_MAIN_GET, "# removed protected-main reread", 1),
               "publisher main reread removal")
expect_failure(workflow, publisher.replace(PUBLISHER_RAW, "$admittedMain = ([string]$env:V26_RELEASE_ADMITTED_MAIN_SHA).Trim()", 1),
               "admitted-main normalization")
expect_failure(workflow, publisher.replace(PUBLISHER_EQUALITY, "[string]::Equals($publisherMain, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)", 1),
               "admitted-main equality removal")

# Moving the exact re-read/equality block after the first remote mutation must be rejected.
start = publisher.find(PUBLISHER_MAIN_GET)
end = publisher.find("\n", publisher.find("throw", publisher.find(PUBLISHER_EQUALITY)))
if start >= 0 and end > start:
    block = publisher[start:end + 1]
    without = publisher[:start] + publisher[end + 1:]
    mutation = without.find(FIRST_MUTATION)
    if mutation >= 0:
        moved = without[:mutation] + without[mutation:mutation + len(FIRST_MUTATION)] + "\n" + block + without[mutation + len(FIRST_MUTATION):]
        expect_failure(workflow, moved, "publisher revalidation moved after first mutation")

print("PASS V26 publisher final protected-main stability guard")
