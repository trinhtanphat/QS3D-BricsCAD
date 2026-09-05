#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "finalize-v25-signed-package.ps1"
errors: list[str] = []
source = TARGET.read_text(encoding="utf-8") if TARGET.is_file() else ""
if not source:
    errors.append("missing scripts/finalize-v25-signed-package.ps1")


def pos(token: str) -> int:
    return source.find(token)


def require(token: str, label: str = "containment/finalizer contract token") -> None:
    if token not in source:
        errors.append(f"missing {label}: {token}")


atomic_markers = (
    "function Read-BoundedUtf8Text",
    "$metadataStage = New-SiblingTempPath",
    "$manifestStage = New-SiblingTempPath",
    "$tempZip = New-SiblingTempPath",
    "Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package",
)
atomic_mode = all(token in source for token in atomic_markers)

for token in (
    "function Test-PathEqualOrContained",
    "function Assert-SafeContainedDirectory",
    "function Assert-SafeContainedOptionalFileTarget",
    "$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'",
    "$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'",
    "must stay below the repository root",
    "PackageZip must be outside PackageDirectory",
    "[IO.FileAttributes]::ReparsePoint",
    "if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }",
):
    require(token)

package_init_tokens = (
    "$packagePath = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
    "$package = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
)
package_positions = [pos(token) for token in package_init_tokens if pos(token) >= 0]
if not package_positions:
    errors.append("missing contained PackageDirectory initialization")

for forbidden in ("Remove-Item $zip", "Remove-Item $hashManifest", 'Compress-Archive -Path "$package/*"'):
    if forbidden in source:
        errors.append(f"unsafe finalizer token remains: {forbidden}")

contain_dir = pos("function Assert-SafeContainedDirectory")
contain_file = pos("function Assert-SafeContainedOptionalFileTarget")
repo_init = pos("$repositoryRoot = Assert-SafeDirectory -Path (Split-Path -Parent $PSScriptRoot) -Label 'repository root'")
package_init = min(package_positions, default=-1)
if min(contain_dir, contain_file, repo_init, package_init) >= 0 and not contain_dir < contain_file < repo_init < package_init:
    errors.append("repository containment helpers must precede finalizer path initialization")

should_token = "if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP')) { return }"
should = pos(should_token)
post_should = source[should:] if should >= 0 else ""

if atomic_mode:
    atomic_required = (
        "function New-SiblingTempPath",
        "$metadataStage = New-SiblingTempPath -TargetPath $metadataPath",
        "$metadataBackup = New-SiblingTempPath -TargetPath $zip -Suffix '.metadata.backup.json'",
        "$metadataRollbackDiscard = New-SiblingTempPath -TargetPath $zip -Suffix '.metadata.rollback-discard'",
        "$manifestStage = New-SiblingTempPath -TargetPath $hashManifest",
        "$manifestBackup = New-SiblingTempPath -TargetPath $zip -Suffix '.manifest.backup.txt'",
        "$tempZip = New-SiblingTempPath -TargetPath $zip",
        "$zipBackup = New-SiblingTempPath -TargetPath $zip",
        "$zipRollbackDiscard = New-SiblingTempPath -TargetPath $zip -Suffix '.zip.rollback-discard'",
        "foreach ($transactionBackup in @($metadataBackup, $manifestBackup, $metadataRollbackDiscard, $zipBackup, $zipRollbackDiscard))",
        "Signed-package transaction backup must stay outside PackageDirectory",
        "[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)",
        "[IO.File]::Move($hashManifest, $manifestBackup)",
        "[IO.File]::Move($manifestStage, $hashManifest)",
        "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip -CompressionLevel Optimal",
        "$tempZip = Assert-SafeOptionalFileTarget -Path $tempZip -Label 'staged PackageZip'",
        "Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package",
        "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)",
        "[IO.File]::Move($tempZip, $zip)",
        "restore original manifest",
        "restore original metadata",
        "Rollback also failed",
    )
    for token in atomic_required:
        require(token, "atomic containment/finalizer contract token")

    for forbidden_backup in (
        "$metadataBackup = New-SiblingTempPath -TargetPath $metadataPath",
        "$manifestBackup = New-SiblingTempPath -TargetPath $hashManifest",
    ):
        if forbidden_backup in source:
            errors.append(
                "atomic transaction backup must not be staged inside PackageDirectory: "
                + forbidden_backup
            )

    zip_remove = pos("Remove-Item -LiteralPath $zip -Force")
    rollback_marker = pos("$originalError = $_")
    if zip_remove >= 0 and (rollback_marker < 0 or zip_remove < rollback_marker):
        errors.append("atomic finalizer must not delete the published ZIP outside rollback")

    package_rechecks = post_should.count("$package = Assert-SafeContainedDirectory -Path $package -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'")
    zip_rechecks = post_should.count("$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'")
    if package_rechecks < 2:
        errors.append(f"expected at least 2 atomic PackageDirectory revalidations after ShouldProcess, found {package_rechecks}")
    if zip_rechecks < 2:
        errors.append(f"expected at least 2 atomic PackageZip revalidations after ShouldProcess, found {zip_rechecks}")

    ordered = (
        should,
        pos("$metadataStage = New-SiblingTempPath"),
        pos("[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)"),
        pos("[IO.File]::Move($hashManifest, $manifestBackup)"),
        pos("[IO.File]::Move($manifestStage, $hashManifest)"),
        pos("Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip -CompressionLevel Optimal"),
        pos("Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package"),
        pos("[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)"),
        pos("$transactionCommitted = $true"),
    )
    if min(ordered) >= 0 and list(ordered) != sorted(ordered):
        errors.append("atomic finalizer must stage, verify, then publish the existing ZIP before commit")
    new_zip = pos("[IO.File]::Move($tempZip, $zip)")
    verify = pos("Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package")
    committed = pos("$transactionCommitted = $true")
    if min(new_zip, verify, committed) >= 0 and not verify < new_zip < committed:
        errors.append("atomic finalizer must verify the staged ZIP before publishing a new ZIP")
else:
    legacy_required = (
        "$packagePath = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
        "Remove-Item -LiteralPath $hashManifest -Force",
        "Remove-Item -LiteralPath $zip -Force",
        "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal",
    )
    for token in legacy_required:
        require(token, "legacy containment/finalizer contract token")
    ordered = (
        should,
        pos("$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8"),
        pos("Remove-Item -LiteralPath $hashManifest -Force"),
        pos("$hashLines | Set-Content -LiteralPath $hashManifest -Encoding ASCII"),
        pos("Remove-Item -LiteralPath $zip -Force"),
        pos("Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal"),
    )
    if min(ordered) >= 0 and list(ordered) != sorted(ordered):
        errors.append("destructive signed-finalizer operations are not in the guarded expected order")
    package_rechecks = post_should.count("$package = Assert-SafeContainedDirectory -Path $package -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'")
    zip_rechecks = post_should.count("$zip = Assert-SafeContainedOptionalFileTarget -Path $zip -RepositoryRoot $repositoryRoot -Label 'PackageZip'")
    if package_rechecks < 4:
        errors.append(f"expected repeated PackageDirectory containment revalidation after ShouldProcess, found {package_rechecks}")
    if zip_rechecks < 4:
        errors.append(f"expected repeated PackageZip containment revalidation after ShouldProcess, found {zip_rechecks}")

if errors:
    print("finalize-v25 path containment preflight FAILED:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

mode = "failure-atomic" if atomic_mode else "legacy"
print(f"finalize-v25 path containment preflight PASS ({mode} publication contract)")
