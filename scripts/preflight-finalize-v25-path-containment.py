#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "finalize-v25-signed-package.ps1"

errors: list[str] = []
if not TARGET.is_file():
    errors.append("missing scripts/finalize-v25-signed-package.ps1")
    source = ""
else:
    source = TARGET.read_text(encoding="utf-8")

required_tokens = (
    "function Test-PathEqualOrContained",
    "function Assert-SafeContainedDirectory",
    "function Assert-SafeContainedOptionalFileTarget",
    "$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'",
    "$packagePath = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
    "$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'",
    "must stay below the repository root",
    "PackageZip must be outside PackageDirectory",
    "[IO.FileAttributes]::ReparsePoint",
    "if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }",
    "Remove-Item -LiteralPath $hashManifest -Force",
    "Remove-Item -LiteralPath $zip -Force",
    "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal",
)
for token in required_tokens:
    if token not in source:
        errors.append(f"missing containment/finalizer contract token: {token}")

for forbidden in (
    "Remove-Item $zip",
    "Remove-Item $hashManifest",
    "Compress-Archive -Path \"$package/*\"",
):
    if forbidden in source:
        errors.append(f"unsafe legacy finalizer token remains: {forbidden}")


def index(token: str) -> int:
    try:
        return source.index(token)
    except ValueError:
        return -1

should_process = index("if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }")
metadata_write = index("$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8")
hash_remove = index("Remove-Item -LiteralPath $hashManifest -Force")
hash_write = index("$hashLines | Set-Content -LiteralPath $hashManifest -Encoding ASCII")
zip_remove = index("Remove-Item -LiteralPath $zip -Force")
compress = index("Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal")

if min(should_process, metadata_write, hash_remove, hash_write, zip_remove, compress) >= 0:
    if not should_process < metadata_write < hash_remove < hash_write < zip_remove < compress:
        errors.append("destructive signed-finalizer operations are not in the guarded expected order")

post_should = source[should_process:] if should_process >= 0 else ""
if post_should:
    package_rechecks = post_should.count(
        "$package = Assert-SafeContainedDirectory -Path $package -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'"
    )
    zip_rechecks = post_should.count(
        "$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'"
    )
    if package_rechecks < 4:
        errors.append(f"expected repeated PackageDirectory containment revalidation after ShouldProcess, found {package_rechecks}")
    if zip_rechecks < 4:
        errors.append(f"expected repeated PackageZip containment revalidation after ShouldProcess, found {zip_rechecks}")

contain_directory = index("function Assert-SafeContainedDirectory")
contain_file = index("function Assert-SafeContainedOptionalFileTarget")
repo_init = index("$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'")
package_init = index("$packagePath = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'")
if min(contain_directory, contain_file, repo_init, package_init) >= 0:
    if not contain_directory < contain_file < repo_init < package_init:
        errors.append("repository containment helpers must be defined before finalizer path initialization")

if errors:
    print("finalize-v25 path containment preflight FAILED:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("finalize-v25 path containment preflight PASS")
