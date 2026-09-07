#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def function_block(text: str, name: str) -> str:
    marker = f"function {name} {{\n"
    start = text.find(marker)
    require(start >= 0, f"Updater must define {name}.")
    next_function = text.find("\nfunction ", start + len(marker))
    require(next_function > start, f"Updater {name} function must be lexically bounded.")
    return text[start:next_function]


def validate(text: str) -> None:
    block = function_block(text, "Expand-VerifiedHeldArchive")
    require("[IO.File]::Open($ZipPath" in block, "Held archive extraction must open the admitted ZIP explicitly.")
    require("[IO.FileShare]::Read" in block, "Held archive extraction must deny write/delete sharing while admitted.")
    require("ComputeHash($zipStream)" in block, "Manifest SHA-256 must be computed from the held ZIP stream.")
    require("[IO.Compression.ZipArchive]::new($zipStream" in block, "Archive validation/extraction must consume the same held ZIP stream.")
    require("$zipStream.Position = 0" in block, "Held ZIP stream must be rewound between digest and ZIP consumption.")
    require("HashSet[string]" in block and "OrdinalIgnoreCase" in block, "Extraction must reject case-insensitive duplicate destinations.")
    require("CreateNew" in block, "Held extraction must not overwrite an already-created destination leaf.")
    require("$record.Entry.Open()" in block, "Held extraction must stream entry bytes from the already-admitted archive record.")
    require("Expand-Archive" not in text, "Updater must not reopen the admitted ZIP via Expand-Archive.")
    require("Get-FileHash -LiteralPath $zipPath" not in text, "Updater must not hash the ZIP through a separate pathname reopen.")

    require(
        "$rootItem = Get-Item -LiteralPath $destinationFull -Force -ErrorAction Stop" in block,
        "Extraction must inspect the extraction root after creating/resolving it.",
    )
    require(
        "if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)" in block,
        "Extraction must reject a reparse-backed extraction root.",
    )
    require(
        "Package extraction root is a reparse point" in block,
        "Extraction must fail closed when the extraction root is a reparse point.",
    )

    directory_unsafe = "if ($record.IsDirectory) {\n                    [IO.Directory]::CreateDirectory([string]$record.Target) | Out-Null\n                    continue"
    require(directory_unsafe not in block, "Directory entries must not mutate through an unvalidated reparse-capable parent chain.")
    file_parent_unsafe = "$parent = [IO.Path]::GetDirectoryName([string]$record.Target)\n                [IO.Directory]::CreateDirectory($parent) | Out-Null\n                $cursor = Get-Item -LiteralPath $parent"
    require(file_parent_unsafe not in block, "File parents must be reparse-validated before CreateDirectory mutation.")

    require("function Assert-ExistingExtractionPathChain {" in block, "Held extraction must define an explicit root-inclusive reparse-chain validator.")
    require("function Ensure-SafeExtractionDirectory {" in block, "Held extraction must create child directories through the safe directory helper.")
    require("Assert-ExistingExtractionPathChain -Path $current -BoundaryRoot $boundaryFull" in block, "Safe directory creation must validate the current parent before mutation.")
    require("[IO.Directory]::CreateDirectory($next)" in block, "Safe directory creation must create one admitted child at a time.")
    require("Assert-ExistingExtractionPathChain -Path $next -BoundaryRoot $boundaryFull" in block, "Safe directory creation must revalidate each child after mutation.")

    root_pre = "Assert-ExistingExtractionPathChain -Path $destinationFull -BoundaryRoot $destinationFull -AllowOutsideBoundary"
    root_create = "[IO.Directory]::CreateDirectory($destinationFull)"
    root_post = "Assert-ExistingExtractionPathChain -Path $destinationFull -BoundaryRoot $destinationFull"
    require(root_pre in block and root_create in block and root_post in block, "Extraction root must use validate/create/revalidate ordering.")
    root_pre_i = block.index(root_pre)
    root_create_i = block.index(root_create, root_pre_i)
    root_post_i = block.index(root_post, root_create_i)
    require(root_pre_i < root_create_i < root_post_i, "Extraction root creation must be bracketed by path-chain validation.")

    dir_safe = "Ensure-SafeExtractionDirectory -Path ([string]$record.Target) -BoundaryRoot $destinationFull"
    require(dir_safe in block, "Directory archive records must use safe component-by-component creation.")

    parent_safe = "Ensure-SafeExtractionDirectory -Path $parent -BoundaryRoot $destinationFull"
    parent_validate = "Assert-ExistingExtractionPathChain -Path $parent -BoundaryRoot $destinationFull"
    create_new = "$output = [IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew"
    require(parent_safe in block and parent_validate in block and create_new in block, "File extraction must safely prepare and validate its parent before CreateNew.")
    parent_safe_i = block.index(parent_safe)
    create_new_i = block.index(create_new, parent_safe_i)
    require(parent_validate in block[parent_safe_i:create_new_i], "File parent must be reparse-validated after safe creation and before CreateNew.")
    require(block.rfind(parent_validate, parent_safe_i, create_new_i) > parent_safe_i, "File parent must be revalidated immediately before leaf mutation.")

    helper_start = block.index("function Ensure-SafeExtractionDirectory {")
    helper_end = block.index("\n    $zipStream =", helper_start)
    helper = block[helper_start:helper_end]
    current_validate_i = helper.index("Assert-ExistingExtractionPathChain -Path $current -BoundaryRoot $boundaryFull")
    child_create_i = helper.index("[IO.Directory]::CreateDirectory($next)")
    child_post_i = helper.index("Assert-ExistingExtractionPathChain -Path $next -BoundaryRoot $boundaryFull")
    require(current_validate_i < child_create_i < child_post_i, "Each child directory mutation must be bracketed by parent/child validation.")

    call_marker = "Expand-VerifiedHeldArchive -ZipPath $zipPath"
    require(text.count(call_marker) == 1, "Updater must invoke held archive extraction exactly once for the downloaded ZIP.")
    call_start = text.index(call_marker)
    call_window = text[call_start:call_start + 900]
    for marker in (
        "-ExpectedSha256 $expectedZipHash",
        "-DestinationRoot $extractRoot",
        "-MaxPackageBytes $maxBytes",
        "-MaxExpandedBytes $maxExpandedBytes",
        "-MaxEntries $MaxArchiveEntries",
    ):
        require(marker in call_window, f"Held archive invocation is missing {marker}.")


text = UPDATER.read_text(encoding="utf-8")
validate(text)

for marker in (
    "ComputeHash($zipStream)",
    "[IO.Compression.ZipArchive]::new($zipStream",
    "[IO.FileShare]::Read",
    "CreateNew",
    "$record.Entry.Open()",
    "$rootItem = Get-Item -LiteralPath $destinationFull -Force -ErrorAction Stop",
    "if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)",
    "function Assert-ExistingExtractionPathChain {",
    "function Ensure-SafeExtractionDirectory {",
    "Assert-ExistingExtractionPathChain -Path $current -BoundaryRoot $boundaryFull",
    "[IO.Directory]::CreateDirectory($next)",
    "Assert-ExistingExtractionPathChain -Path $next -BoundaryRoot $boundaryFull",
    "Ensure-SafeExtractionDirectory -Path ([string]$record.Target) -BoundaryRoot $destinationFull",
    "Ensure-SafeExtractionDirectory -Path $parent -BoundaryRoot $destinationFull",
    "Assert-ExistingExtractionPathChain -Path $parent -BoundaryRoot $destinationFull",
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except (SystemExit, ValueError):
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

for injected in (
    "\nExpand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force\n",
    "\n$null = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256\n",
):
    try:
        validate(text + injected)
    except SystemExit:
        pass
    else:
        raise SystemExit("Mutation probe unexpectedly passed after adding a pathname reopen primitive.")

print("PASS V25 updater held archive generation extraction fence")
