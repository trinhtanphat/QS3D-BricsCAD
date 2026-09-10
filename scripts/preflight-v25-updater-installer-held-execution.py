#!/usr/bin/env python3
"""Fail closed unless the V25 updater holds its admitted installer across execution."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def require(source: str, token: str, label: str, start: int = 0) -> int:
    index = source.find(token, start)
    if index < 0:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: missing {label}: {token}")
    return index


def function_body(source: str, name: str) -> str:
    marker = f"function {name}"
    start = require(source, marker, f"{name} helper")
    brace = source.find("{", start)
    if brace < 0:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: {name} has no body")
    depth = 0
    quote = None
    escaped = False
    for index in range(brace, len(source)):
        char = source[index]
        if escaped:
            escaped = False
            continue
        if char == "`":
            escaped = True
            continue
        if quote:
            if char == quote:
                quote = None
            continue
        if char in ("'", '"'):
            quote = char
            continue
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise SystemExit(f"ERROR: V25 updater installer-hold preflight: {name} body is unbalanced")


def validate(source: str) -> None:
    helper = function_body(source, "Open-HeldVerifiedInstaller")
    for token, label in (
        ("[IO.Path]::GetFullPath", "canonical path/root comparison"),
        ("[StringComparison]::OrdinalIgnoreCase", "Windows path comparison"),
        ("Get-Item -LiteralPath", "ordinary-file/reparse inspection"),
        ("[IO.FileAttributes]::ReparsePoint", "reparse rejection"),
        ("[IO.File]::Open(", "explicit file hold"),
        ("[IO.FileMode]::Open", "existing-file-only hold"),
        ("[IO.FileAccess]::Read", "read-only held access"),
        ("[IO.FileShare]::Read", "write/delete-denying share mode"),
        ("Assert-AuthenticodeSigner", "held Authenticode admission"),
        ("ExpectedSigner", "expected signer binding"),
        ("ExtractionRoot", "extraction-root binding"),
    ):
        if token not in helper:
            raise SystemExit(f"ERROR: V25 updater installer-hold preflight: helper missing {label}: {token}")

    # FileShare.Read must be the exact share mode at the held open. ReadWrite/Delete would
    # reopen the replacement race that this guard exists to close.
    held_open = require(helper, "[IO.File]::Open(", "held open")
    open_tail = helper[held_open : held_open + 500]
    if "[IO.FileShare]::ReadWrite" in open_tail or "[IO.FileShare]::Delete" in open_tail:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: held open permits write/delete sharing")

    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition")
    package_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package-root admission")
    invoke = require(source, "& $installer @arguments", "installer invocation")
    dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal", acquire)
    finally_index = source.rfind("finally", acquire, dispose + 1)

    # Acquire before the final package-root signature/hash sweep so even a same-signer
    # replacement cannot swap between package admission and execution.
    if not (acquire < package_admission < invoke < dispose):
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: require acquire < package admission < invoke < dispose"
        )
    if finally_index < invoke:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: held installer disposal must be in finally after invocation"
        )

    held_interval = source[acquire:dispose]
    if "& $installer @arguments" not in held_interval:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: installer execution escaped held interval")
    if "Assert-PackageRoot -Directory $extractRoot" not in held_interval:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: package admission escaped held interval")

    # Existing high-value updater fences must remain present.
    for token, label in (
        ("Expand-VerifiedHeldArchive", "bounded ZIP/hash admission"),
        ("Assert-PackageRoot", "package signature/integrity admission"),
        ("Compare-StrictSemVer", "product-version anti-downgrade"),
        ("Enter-Qs3dUpdateMutex", "update serialization mutex"),
        ("Get-Process -Name bricscad", "running-BricsCAD guard"),
        ("Read-InstalledProductVersion", "installed-state freshness check"),
        ("RequireSigned = $true", "installer signed-payload enforcement"),
        ("Remove-Item -LiteralPath $tempRoot -Recurse -Force", "temporary-root cleanup"),
    ):
        require(source, token, label)


def expect_reject(source: str, label: str) -> None:
    try:
        validate(source)
    except SystemExit:
        return
    raise SystemExit(f"ERROR: V25 updater installer-hold preflight self-test accepted {label}")


def self_test() -> None:
    common = """
function Assert-AuthenticodeSigner { }
function Expand-VerifiedHeldArchive { }
function Assert-PackageRoot { }
function Compare-StrictSemVer { }
function Enter-Qs3dUpdateMutex { }
function Read-InstalledProductVersion { }
Get-Process -Name bricscad
RequireSigned = $true
Remove-Item -LiteralPath $tempRoot -Recurse -Force
"""
    expect_reject(
        common
        + """
$installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
Assert-PackageRoot -Directory $extractRoot
& $installer @arguments
""",
        "verify-then-path-reopen without a hold",
    )

    weak_helper = """
function Open-HeldVerifiedInstaller {
param($Path,$ExtractionRoot,$ExpectedSigner)
$full=[IO.Path]::GetFullPath($Path); [StringComparison]::OrdinalIgnoreCase
$item=Get-Item -LiteralPath $full; [IO.FileAttributes]::ReparsePoint
$h=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
return $h
}
"""
    expect_reject(
        common
        + weak_helper
        + """
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
Assert-PackageRoot -Directory $extractRoot
try { & $installer @arguments } finally { $heldInstaller.Dispose() }
""",
        "write-share-permitting hold",
    )

    strong_helper = """
function Open-HeldVerifiedInstaller {
param($Path,$ExtractionRoot,$ExpectedSigner)
$full=[IO.Path]::GetFullPath($Path); $root=[IO.Path]::GetFullPath($ExtractionRoot); [StringComparison]::OrdinalIgnoreCase
$item=Get-Item -LiteralPath $full; if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'reparse' }
$h=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
return $h
}
"""
    expect_reject(
        common
        + strong_helper
        + """
Assert-PackageRoot -Directory $extractRoot
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
try { & $installer @arguments } finally { $heldInstaller.Dispose() }
""",
        "hold acquired after package admission",
    )
    expect_reject(
        common
        + strong_helper
        + """
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
Assert-PackageRoot -Directory $extractRoot
$heldInstaller.Dispose()
try { & $installer @arguments } finally { Write-Host done }
""",
        "hold disposed before invocation",
    )


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print("PASS: V25 updater keeps the re-admitted installer write/delete-held across package admission and execution")
