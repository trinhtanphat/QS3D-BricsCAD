#!/usr/bin/env python3
"""Fail closed unless the V25 updater holds its admitted installer across execution."""

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

    for token, label in (
        ("[IO.Path]::GetFullPath", "canonical path/root normalization"),
        ("[StringComparison]::OrdinalIgnoreCase", "Windows path comparison"),
        ("Get-Item -LiteralPath", "ordinary-file/reparse inspection"),
        ("[IO.FileAttributes]::ReparsePoint", "reparse rejection"),
        ("[IO.File]::Open(", "explicit file hold"),
        ("[IO.FileMode]::Open", "existing-file-only hold"),
        ("[IO.FileAccess]::Read", "read-only held access"),
        ("[IO.FileShare]::Read", "write/delete-denying share mode"),
        ("Assert-AuthenticodeSigner", "held Authenticode re-admission"),
        ("ExpectedSigner", "expected signer binding"),
        ("ExtractionRoot", "extraction-root input"),
    ):
        if token not in helper:
            raise SystemExit(f"ERROR: V25 updater installer-hold preflight: helper missing {label}: {token}")

    separator_binding = re.search(
        r"\$rootWithSeparator\s*=\s*\$root\.TrimEnd\([^\n]+\)\s*\+\s*['\"]\\['\"]",
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
            "ERROR: V25 updater installer-hold preflight: canonical containment must use a separator-bounded extraction root"
        )

    reparse_reject = re.search(
        r"if\s*\(\s*\(\s*\$item\.Attributes\s+-band\s+\[IO\.FileAttributes\]::ReparsePoint\s*\)\s+-ne\s+0\s*\)\s*\{\s*throw\b",
        helper,
        re.IGNORECASE | re.DOTALL,
    )
    if reparse_reject is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: ReparsePoint inspection must fail closed with throw"
        )
    if re.search(r"Get-Item\s+-LiteralPath\s+\$full\b", helper, re.IGNORECASE) is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: reparse inspection must target the canonical held-open path"
        )

    held_open = require(helper, "[IO.File]::Open(", "held open")
    open_tail = helper[held_open : held_open + 700]
    if "[IO.FileShare]::ReadWrite" in open_tail or "[IO.FileShare]::Delete" in open_tail or "[IO.FileShare]::None" in open_tail:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: held open must use exact read sharing, not ReadWrite/Delete/None"
        )
    exact_share = re.search(
        r"\[IO\.File\]::Open\(\s*\$full\s*,\s*\[IO\.FileMode\]::Open\s*,\s*\[IO\.FileAccess\]::Read\s*,\s*\[IO\.FileShare\]::Read\s*\)",
        open_tail,
        re.IGNORECASE | re.DOTALL,
    )
    if not exact_share:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: held open must target canonical $full with FileMode.Open/FileAccess.Read/FileShare.Read"
        )
    if re.search(
        r"Assert-AuthenticodeSigner\s+-Path\s+\$full\s+-ExpectedSigner\s+\$ExpectedSigner\b",
        helper,
        re.IGNORECASE,
    ) is None:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: signer re-admission must target the canonical held-open path and expected signer"
        )

    if "catch" not in helper or ".Dispose()" not in helper:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: helper must dispose the hold if re-admission fails"
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
            "ERROR: V25 updater installer-hold preflight: installer must be invoked exactly once while held"
        )
    if held_interval.count("Assert-PackageRoot -Directory $extractRoot") != 1:
        raise SystemExit(
            "ERROR: V25 updater installer-hold preflight: final package admission must occur exactly once while held"
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
$item=Get-Item -LiteralPath $full
if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'reparse' }
$h=$null
try {
  $h=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
  return $h
}
catch { if ($h) { $h.Dispose() }; throw }
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
$installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
Assert-PackageRoot -Directory $extractRoot
& $installer @arguments
""",
        "verify-then-path-reopen without a hold",
    )
    expect_reject(valid.replace("[IO.FileShare]::Read)", "[IO.FileShare]::ReadWrite)", 1), "write-share-permitting hold")
    expect_reject(
        valid.replace(
            "$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot",
            "Assert-PackageRoot -Directory $extractRoot\n$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot",
            1,
        ),
        "package admission before hold acquisition",
    )
    expect_reject(
        valid.replace("$full.StartsWith($rootWithSeparator,", "$full.StartsWith($root,", 1),
        "unsafe prefix containment accepting sibling root names",
    )
    expect_reject(
        valid.replace(
            "if (-not $full.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) { throw 'outside root' }",
            "$unused = $rootWithSeparator",
            1,
        ),
        "extraction-root token without enforced canonical containment",
    )
    expect_reject(
        valid.replace(
            "if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'reparse' }",
            "$mentionsReparse = ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)",
            1,
        ),
        "reparse token without fail-closed rejection",
    )
    expect_reject(
        valid.replace("Get-Item -LiteralPath $full", "Get-Item -LiteralPath $Path", 1),
        "reparse inspection of a different path than canonical held path",
    )
    expect_reject(
        valid.replace("[IO.File]::Open($full,", "[IO.File]::Open($Path,", 1),
        "held open of a different path than canonical admitted path",
    )
    expect_reject(
        valid.replace("Assert-AuthenticodeSigner -Path $full", "Assert-AuthenticodeSigner -Path $Path", 1),
        "signer re-admission of a different path than held path",
    )
    expect_reject(valid.replace("catch { if ($h) { $h.Dispose() }; throw }", "catch { throw }", 1), "held handle leak when signer re-admission throws")
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
    expect_reject(
        valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  # finally {\n  $heldInstaller.Dispose()", 1),
        "comment-only finally token before disposal",
    )
    expect_reject(
        valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  Write-Host 'finally {'\n  $heldInstaller.Dispose()", 1),
        "string-only finally token before disposal",
    )


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print(
        "PASS: V25 updater canonically binds and re-admits the installer, holds it read-shared/write-delete-denied across final package admission and execution, and disposes safely"
    )
