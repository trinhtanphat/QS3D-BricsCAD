#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
SAMPLE_ROOT = "samples/generated/"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def array_block(text: str, variable: str) -> str:
    marker = f"          ${variable} = @(\n"
    start = text.find(marker)
    require(start >= 0, f"V25 cloud release must declare ${variable}.")
    body_start = start + len(marker)
    end = text.find("          )\n", body_start)
    require(end > body_start, f"V25 cloud release ${variable} array must be bounded.")
    return text[body_start:end]


def validate(workflow: str, package: str) -> None:
    package_marker = "$sampleSource = Join-Path $root 'samples/generated'"
    require(package.count(package_marker) == 1,
            "V25 package source must bind synthetic samples to samples/generated.")
    require("Join-Path $sampleSource $sampleName" in package,
            "V25 package must continue consuming files from the bound synthetic sample root.")

    for variable in ("preMutationReleaseRelevantPaths", "finalReleaseRelevantPaths"):
        block = array_block(workflow, variable)
        sample_entry = f"            '{SAMPLE_ROOT}',\n"
        require(block.count(sample_entry) == 1,
                f"${variable} must classify packaged {SAMPLE_ROOT} drift as release-relevant exactly once.")

    require("git diff --quiet --no-ext-diff $env:SOURCE_SHA $preMutationMain -- $preMutationReleaseRelevantPaths" in workflow,
            "Pre-mutation release drift admission must use its bounded release-relevant path set.")
    require("git diff --quiet --no-ext-diff $env:SOURCE_SHA $finalMain -- $finalReleaseRelevantPaths" in workflow,
            "Final publication drift admission must use its bounded release-relevant path set.")


workflow = WORKFLOW.read_text(encoding="utf-8")
package = PACKAGE.read_text(encoding="utf-8")
validate(workflow, package)

sample_entry = f"            '{SAMPLE_ROOT}',\n"
for variable in ("preMutationReleaseRelevantPaths", "finalReleaseRelevantPaths"):
    marker = f"          ${variable} = @(\n"
    start = workflow.find(marker)
    require(start >= 0, f"Mutation probe could not find ${variable}.")
    body_start = start + len(marker)
    end = workflow.find("          )\n", body_start)
    block = workflow[body_start:end]
    require(sample_entry in block, f"Mutation probe could not find {SAMPLE_ROOT} in ${variable}.")
    mutated_block = block.replace(sample_entry, "", 1)
    mutated = workflow[:body_start] + mutated_block + workflow[end:]
    try:
        validate(mutated, package)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed without packaged sample drift in ${variable}.")

print("PASS V25 cloud packaged-sample protected-main drift fence")
