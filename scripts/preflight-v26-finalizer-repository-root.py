#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WRAPPER = ROOT / "scripts" / "finalize-v26-signed-package.ps1"
TEMPLATE = ROOT / "scripts" / "finalize-v25-signed-package.ps1"
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"

errors: list[str] = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required release script: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


wrapper = read(WRAPPER)
template = read(TEMPLATE)
generator = read(GENERATOR)

exact_cleanup = "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity"
required_wrapper_tokens = (
    "$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'",
    "$tempScript = Join-Path $PSScriptRoot ('.finalize-v26-signed-package.generated.' + [Guid]::NewGuid().ToString('N') + '.ps1')",
    "& $generator -SourceScript 'finalize-v25-signed-package.ps1' -OutputPath $tempScript",
    "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
    "& $tempScript @forward",
    exact_cleanup,
)
for token in required_wrapper_tokens:
    if token not in wrapper:
        errors.append(f"V26 finalizer wrapper is missing repository-root preservation token: {token}")

for forbidden in (
    "[IO.Path]::GetTempPath()",
    "qs3d-v26-finalizer-",
    "$tempRoot",
    "Remove-Item -LiteralPath $tempScript",
    "Remove-Item $tempScript",
):
    if forbidden in wrapper:
        errors.append(f"V26 finalizer wrapper violates repository-root/exact-generation cleanup contract: {forbidden}")

required_template_tokens = (
    "$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'",
    "Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
    "$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'",
)
for token in required_template_tokens:
    if token not in template:
        errors.append(f"V25 finalizer containment contract changed unexpectedly: {token}")

required_generator_tokens = (
    "'finalize-v25-signed-package.ps1'",
    "$generated = $text.Replace('V25', 'V26').Replace('v25', 'v26')",
    "$stagePath = Join-Path $parent",
    "[IO.File]::WriteAllText($stagePath, $generated",
    "Assert-SafeExistingOutputLeaf -Path $outputFull",
    "$outputParentHandle = Open-AdmittedOutputParent -Path $parent",
    "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
    "V26 generation output must be fresh; destination appeared before publication.",
    "[IO.File]::Move($stagePath, $outputFull)",
)
for token in required_generator_tokens:
    if token not in generator:
        errors.append(f"V25→V26 transformer contract changed unexpectedly: {token}")

for forbidden in (
    "[IO.File]::WriteAllText($outputFull",
    "[IO.File]::Replace(",
):
    if forbidden in generator:
        errors.append(f"V25→V26 transformer must not use in-place/existing-destination publication: {forbidden}")

# The generated script must live directly in scripts/, because the inherited
# template calculates repositoryRoot as the parent of its PSScriptRoot. Cleanup
# occurs only after execution and uses exact-generation deletion rather than a
# pathname unlink, so repository-root preservation and cleanup ownership remain
# independent invariants.
if wrapper:
    temp_index = wrapper.find("$tempScript = Join-Path $PSScriptRoot")
    generate_index = wrapper.find("& $generator -SourceScript 'finalize-v25-signed-package.ps1' -OutputPath $tempScript")
    identity_index = wrapper.find("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream")
    execute_index = wrapper.find("& $tempScript @forward")
    cleanup_index = wrapper.find(exact_cleanup, execute_index + 1 if execute_index >= 0 else 0)
    if min(temp_index, generate_index, identity_index, execute_index, cleanup_index) >= 0:
        if not temp_index < generate_index < identity_index < execute_index < cleanup_index:
            errors.append(
                "generated V26 finalizer lifecycle is not create -> generate -> capture identity -> execute -> exact-generation cleanup"
            )

# The transformer publishes only a fresh output generation. It pins the output-parent
# generation, writes an ordinary sibling stage, revalidates the held parent immediately
# before publication, proves the destination is still absent, then atomically moves the
# stage into the selected repository-local finalizer path. Existing-destination replace
# semantics are intentionally forbidden because they break the generation boundary.
if generator:
    parent_index = generator.find("$parent = Split-Path -Parent $outputFull")
    admission_index = generator.find("$outputParentHandle = Open-AdmittedOutputParent -Path $parent")
    stage_index = generator.find("$stagePath = Join-Path $parent")
    write_index = generator.find("[IO.File]::WriteAllText($stagePath, $generated")
    binding_after_write_index = generator.find(
        "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
        write_index + 1 if write_index >= 0 else 0,
    )
    fresh_before_move_index = generator.find(
        "V26 generation output must be fresh; destination appeared before publication.",
        write_index + 1 if write_index >= 0 else 0,
    )
    move_index = generator.find("[IO.File]::Move($stagePath, $outputFull)")
    if min(
        parent_index,
        admission_index,
        stage_index,
        write_index,
        binding_after_write_index,
        fresh_before_move_index,
        move_index,
    ) >= 0:
        if not (
            parent_index
            < admission_index
            < stage_index
            < write_index
            < binding_after_write_index
            < fresh_before_move_index
            < move_index
        ):
            errors.append(
                "V25→V26 transformer publication is not parent -> held admission -> sibling stage -> write -> revalidate -> fresh proof -> atomic move"
            )

if errors:
    print("V26 finalizer repository-root preflight FAILED:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("V26 finalizer repository-root preflight PASS")
