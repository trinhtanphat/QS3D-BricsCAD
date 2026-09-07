#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
SAMPLE_ROOT = "samples/generated/"
SAMPLE_GLOB = "samples/generated/**"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def push_paths_block(text: str) -> str:
    marker = '  "push":\n    branches:\n      - main\n    paths:\n'
    start = text.find(marker)
    require(start >= 0, "V25 dispatcher must declare the protected-main push.paths block.")
    body_start = start + len(marker)
    end = text.find("\npermissions:\n", body_start)
    require(end > body_start, "V25 dispatcher push.paths block must be bounded before permissions.")
    return text[body_start:end]


def release_pathspec_block(text: str) -> str:
    marker = "          release_relevant_pathspecs=(\n"
    start = text.find(marker)
    require(start >= 0, "V25 dispatcher must declare release_relevant_pathspecs.")
    body_start = start + len(marker)
    end = text.find("          )\n", body_start)
    require(end > body_start, "V25 dispatcher release_relevant_pathspecs array must be bounded.")
    return text[body_start:end]


def validate(dispatch: str, package: str) -> None:
    package_marker = "$sampleSource = Join-Path $root 'samples/generated'"
    require(package.count(package_marker) == 1,
            "V25 package source must bind synthetic samples to samples/generated.")
    require("Join-Path $sampleSource $sampleName" in package,
            "V25 package must continue consuming files from the bound synthetic sample root.")

    push_block = push_paths_block(dispatch)
    push_entry = f'      - "{SAMPLE_GLOB}"\n'
    require(push_block.count(push_entry) == 1,
            f"V25 dispatcher push.paths must trigger on packaged {SAMPLE_GLOB} exactly once.")

    pathspec_block = release_pathspec_block(dispatch)
    pathspec_entry = f"            '{SAMPLE_ROOT}'\n"
    require(pathspec_block.count(pathspec_entry) == 1,
            f"V25 dispatcher release_relevant_pathspecs must classify packaged {SAMPLE_ROOT} drift exactly once.")

    require('git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"' in dispatch,
            "V25 dispatcher supersession fence must use release_relevant_pathspecs.")


dispatch = DISPATCH.read_text(encoding="utf-8")
package = PACKAGE.read_text(encoding="utf-8")
validate(dispatch, package)

push_entry = f'      - "{SAMPLE_GLOB}"\n'
mutated_push = dispatch.replace(push_entry, "", 1)
require(mutated_push != dispatch, "Mutation probe could not remove packaged-sample push trigger.")
try:
    validate(mutated_push, package)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed without packaged-sample push trigger.")

pathspec_entry = f"            '{SAMPLE_ROOT}'\n"
mutated_pathspec = dispatch.replace(pathspec_entry, "", 1)
require(mutated_pathspec != dispatch, "Mutation probe could not remove packaged-sample release pathspec.")
try:
    validate(mutated_pathspec, package)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed without packaged-sample release pathspec.")

print("PASS V25 cloud packaged-sample dispatcher fence")
