#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
PACKAGE = ROOT / "scripts" / "package-v26.ps1"
SAMPLE_ROOT = "samples/generated/"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def final_classifier_block(text: str) -> tuple[int, int, str]:
    marker = "          $finalReleaseRelevantPaths = @(\n"
    start = text.find(marker)
    require(start >= 0, "V26 cloud release must declare $finalReleaseRelevantPaths.")
    body_start = start + len(marker)
    end = text.find("          )\n", body_start)
    require(end > body_start, "V26 cloud final release-relevant path array must be bounded.")
    return body_start, end, text[body_start:end]


def validate(workflow: str, package: str) -> None:
    package_marker = "$sampleSource = Join-Path $root 'samples/generated'"
    require(package.count(package_marker) == 1,
            "V26 package source must bind synthetic samples to samples/generated.")
    require("Join-Path $sampleSource $sampleName" in package,
            "V26 package must continue consuming files from the bound synthetic sample root.")

    _, _, block = final_classifier_block(workflow)
    sample_entry = f"            '{SAMPLE_ROOT}',\n"
    require(block.count(sample_entry) == 1,
            f"$finalReleaseRelevantPaths must classify packaged {SAMPLE_ROOT} drift exactly once.")
    require("git diff --quiet --no-ext-diff $env:GITHUB_SHA $finalMain -- $finalReleaseRelevantPaths" in workflow,
            "V26 final publication drift admission must use its bounded release-relevant path set.")
    require("if ($publishMain -ne $finalMain)" in workflow,
            "V26 final publication admission must still fail closed when protected main moves after classification.")


workflow = WORKFLOW.read_text(encoding="utf-8")
package = PACKAGE.read_text(encoding="utf-8")
validate(workflow, package)

body_start, end, block = final_classifier_block(workflow)
sample_entry = f"            '{SAMPLE_ROOT}',\n"
require(sample_entry in block, "Mutation probe could not find packaged sample root in V26 final classifier.")

mutations = {
    "removed": block.replace(sample_entry, "", 1),
    "commented": block.replace(sample_entry, f"            # '{SAMPLE_ROOT}',\n", 1),
    "duplicated": block.replace(sample_entry, sample_entry + sample_entry, 1),
}
for name, mutated_block in mutations.items():
    mutated = workflow[:body_start] + mutated_block + workflow[end:]
    try:
        validate(mutated, package)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed with {name} packaged sample classifier entry.")

print("PASS V26 cloud packaged-sample protected-main drift fence")
