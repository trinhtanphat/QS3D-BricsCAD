#!/usr/bin/env python3
"""Fail closed unless the V25 updater pins the admitted installer across execution."""

from __future__ import annotations

from pathlib import Path
import re

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

    # The security boundary must be established by the opened Windows object, not
    # by a pathname-only Get-Item check that can race with the subsequent open.
    for token, label in (
        ("[IO.Path]::GetFullPath", "canonical path/root normalization"),
        ("[StringComparison]::OrdinalIgnoreCase", "Windows path comparison"),
        ("OpenOrdinaryReadHeld", "atomic held-file opener"),
        ("FinalPath", "handle-resolved final path binding"),
        ("Assert-AuthenticodeSigner", "held Authenticode re-admission"),
        ("ExpectedSigner", "expected signer binding"),
        ("ExtractionRoot", "extraction-root input"),
    ):
        if token not in helper:
            raise SystemExit(f"ERROR: V25 updater installer-hold preflight: helper missing {label}: {token}")

    native_requirements = (
        ("CreateFileW", "Win32 atomic open"),
        ("FILE_FLAG_OPEN_REPARSE_POINT", "no-follow leaf open"),
        ("FILE_SHARE_READ", "write/delete-denying share"),
        ("GENERIC_READ", "read-only desired access"),
        ("OPEN_EXISTING", "existing-file-only disposition"),
        ("GetFileInformationByHandle", "handle-bound file attributes"),
        ("FILE_ATTRIBUTE_REPARSE_POINT", "handle-bound reparse rejection"),
        ("FILE_ATTRIBUTE_DIRECTORY", "handle-bound directory rejection"),
        ("GetFinalPathNameByHandleW", "handle-resolved pathname"),
    )
    for token, label in native_requirements:
        require(source, token, label)

    # A pre-open Get-Item is specifically forbidden as the authoritative ordinary
    # file/reparse admission because it recreates the check->open race.
    if re.search(r"Get-Item\s+-LiteralPath\s+\$full\b", helper, re.IGNORECASE):
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: canonical installer must not be admitted by pathname-only Get-Item before atomic no-follow open"
        )

    separator_binding = re.search(
        r"(?:\$rootWithSeparator\s*=\s*\$root\.TrimEnd\([^\n]+\)\s*\+\s*['\"]\\['\"]|\$rootWithSeparator\s*=\s*\$root\s*\+\s*\[IO\.Path\]::DirectorySeparatorChar\b)",
        helper,
        re.IGNORECASE,
    )
    boundary_check = re.search(
        r"\$full\.StartsWith\(\s*\$rootWithSeparator\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)",
        helper,
        re.IGNORECASE,
    )
    if separator_binding is None or boundary_check is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: lexical containment must use a separator-bounded extraction root"
        )

    atomic_open = require(helper, "OpenOrdinaryReadHeld", "atomic no-follow held open")
    final_path = require(helper, "FinalPath", "handle-resolved final path", atomic_open)
    signer = require(helper, "Assert-AuthenticodeSigner", "held signer re-admission", final_path)
    if not (atomic_open < final_path < signer):
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: require atomic open < handle final-path binding < signer re-admission"
        )

    if re.search(
        r"\[string\]::Equals\(\s*\$heldFinal\s*,\s*\$full\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)",
        helper,
        re.IGNORECASE,
    ) is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: handle-resolved final path must equal canonical installer path"
        )

    if re.search(
        r"Assert-AuthenticodeSigner\s+-Path\s+\$full\s+-ExpectedSigner\s+\$ExpectedSigner\b",
        helper,
        re.IGNORECASE,
    ) is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: signer re-admission must target canonical pinned path and expected signer"
        )

    if "catch" not in helper or ".Dispose()" not in helper:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: helper must dispose the native hold if re-admission fails"
        )

    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition")
    first_package_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package-root admission")
    package_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package-root admission", acquire)
    if first_package_admission < acquire:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: package admission must not occur before installer hold acquisition"
        )
    invoke = require(source, "& $installer @arguments", "installer invocation", package_admission)
    dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal", invoke)

    if not (acquire < package_admission < invoke < dispose):
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: require acquire < final package admission < invoke < dispose"
        )

    after_invoke = source[invoke:dispose]
    if re.search(r"(?mi)^\s*finally\s*\{", after_invoke) is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: held installer disposal must be inside an actual finally block after invocation"
        )

    held_interval = source[acquire:dispose]
    if held_interval.count("& $installer @arguments") != 1:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: installer must be invoked exactly once while pinned"
        )
    if held_interval.count("Assert-PackageRoot -Directory $extractRoot") != 1:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: final package admission must occur exactly once while pinned"
        )
    if re.search(r"(?m)^\s*\$installer\s*=", held_interval):
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: installer pathname must not be reassigned after the hold is acquired"
        )

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


def common_fences() -> str:
    return """
# Native helper semantics required by the contract:
# CreateFileW GENERIC_READ FILE_SHARE_READ OPEN_EXISTING FILE_FLAG_OPEN_REPARSE_POINT
# GetFileInformationByHandle FILE_ATTRIBUTE_REPARSE_POINT FILE_ATTRIBUTE_DIRECTORY
# GetFinalPathNameByHandleW
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


def strong_helper() -> str:
    return """
function Open-HeldVerifiedInstaller {
param($Path,$ExtractionRoot,$ExpectedSigner)
$full=[IO.Path]::GetFullPath($Path)
$root=[IO.Path]::GetFullPath($ExtractionRoot)
$rootWithSeparator=$root.TrimEnd('\\') + '\\'
if (-not $full.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) { throw 'outside root' }
$held=$null
try {
  $held=[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)
  $heldFinal=[IO.Path]::GetFullPath($held.FinalPath)
  if (-not [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)) { throw 'resolved path mismatch' }
  Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
  return $held
}
catch { if ($held) { $held.Dispose() }; throw }
}
"""


def valid_topology() -> str:
    return common_fences() + strong_helper() + """
$installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
try {
  Assert-PackageRoot -Directory $extractRoot
  & $installer @arguments
}
finally {
  $heldInstaller.Dispose()
}
"""


def self_test() -> None:
    valid = valid_topology()
    validate(valid)

    expect_reject(
        common_fences() + """
function Open-HeldVerifiedInstaller {
param($Path,$ExtractionRoot,$ExpectedSigner)
$full=[IO.Path]::GetFullPath($Path)
$root=[IO.Path]::GetFullPath($ExtractionRoot)
$rootWithSeparator=$root + [IO.Path]::DirectorySeparatorChar
if (-not $full.StartsWith($rootWithSeparator,[StringComparison]::OrdinalIgnoreCase)) { throw 'outside root' }
$item=Get-Item -LiteralPath $full
$held=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
return $held
}
$installer=Join-Path $extractRoot 'install-v25-autoload.ps1'
$heldInstaller=Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
try { Assert-PackageRoot -Directory $extractRoot; & $installer @arguments }
finally { $heldInstaller.Dispose() }
""",
        "pathname Get-Item check followed by normal File.Open",
    )
    expect_reject(valid.replace("OpenOrdinaryReadHeld($full)", "OpenReadFollowingReparse($full)", 1), "follow-reparse open")
    expect_reject(valid.replace("FILE_FLAG_OPEN_REPARSE_POINT", "FILE_FLAG_SEQUENTIAL_SCAN", 1), "native opener without OPEN_REPARSE_POINT")
    expect_reject(valid.replace("GetFileInformationByHandle", "GetFileAttributesW", 1), "pathname attributes instead of handle attributes")
    expect_reject(valid.replace("GetFinalPathNameByHandleW", "GetFullPathNameW", 1), "pathname resolution instead of handle final path")
    expect_reject(valid.replace("$held.FinalPath", "$full", 1), "final-path check not bound to opened handle")
    expect_reject(
        valid.replace(
            "if (-not [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)) { throw 'resolved path mismatch' }",
            "$same = [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)",
            1,
        ),
        "resolved-path comparison without fail-closed rejection",
    )
    expect_reject(valid.replace("FILE_SHARE_READ", "FILE_SHARE_READ | FILE_SHARE_WRITE", 1), "write-share-permitting native hold")
    expect_reject(
        valid.replace(
            "$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot",
            "Assert-PackageRoot -Directory $extractRoot\n$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot",
            1,
        ),
        "package admission before hold acquisition",
    )
    expect_reject(valid.replace("$full.StartsWith($rootWithSeparator,", "$full.StartsWith($root,", 1), "unsafe prefix containment")
    expect_reject(valid.replace("Assert-AuthenticodeSigner -Path $full", "Assert-AuthenticodeSigner -Path $Path", 1), "signer check on unbound path")
    expect_reject(valid.replace("catch { if ($held) { $held.Dispose() }; throw }", "catch { throw }", 1), "hold leak on re-admission failure")
    expect_reject(
        valid.replace("  & $installer @arguments\n", "  $installer = Join-Path $extractRoot 'install-v25-autoload.ps1'\n  & $installer @arguments\n", 1),
        "installer pathname reassignment after hold acquisition",
    )
    expect_reject(
        valid.replace(
            "  & $installer @arguments\n}\nfinally {\n  $heldInstaller.Dispose()",
            "  $heldInstaller.Dispose()\n  & $installer @arguments\n}\nfinally {\n  Write-Host done",
            1,
        ),
        "hold disposed before invocation",
    )
    expect_reject(valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  # finally {\n  $heldInstaller.Dispose()", 1), "comment-only finally")
    expect_reject(valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  Write-Host 'finally {'\n  $heldInstaller.Dispose()", 1), "string-only finally")


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print(
        "PASS: V25 updater atomically no-follow opens the installer, binds handle attributes/final path, re-admits signer, pins it across final package admission/execution, and disposes safely"
    )
