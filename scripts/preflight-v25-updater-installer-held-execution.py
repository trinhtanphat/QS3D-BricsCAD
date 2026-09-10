#!/usr/bin/env python3
"""Fail closed unless the V25 updater holds the admitted installer across execution."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def require(source: str, token: str, label: str) -> int:
    index = source.find(token)
    if index < 0:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: missing {label}: {token}")
    return index


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: forbidden {label}: {token}")


def validate(source: str) -> None:
    # The final installer admission must happen under a handle that denies write/delete
    # sharing, and that same hold must stay alive until after PowerShell has consumed the
    # script. This closes the Authenticode-verify -> pathname-reopen replacement window.
    helper = require(source, "function Open-HeldVerifiedInstaller", "held-installer helper")
    open_index = require(source, "[IO.File]::Open(", "explicit installer file hold")
    require(source, "[IO.FileMode]::Open", "existing-file-only hold")
    require(source, "[IO.FileAccess]::Read", "read-only hold")
    require(source, "[IO.FileShare]::Read", "write/delete-denying share mode")
    require(source, "Assert-AuthenticodeSigner", "Authenticode admission")
    require(source, "Resolve-OrdinaryNonReparse", "non-reparse installer admission")

    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition")
    invoke = require(source, "& $installer @arguments", "installer invocation")
    dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal")
    finally_index = source.rfind("finally", acquire, dispose + 1)

    if not (helper < acquire < invoke < dispose):
        raise SystemExit("ERROR: V25 updater installer-hold preflight: hold/acquire/invoke/dispose ordering is not fail closed")
    if finally_index < invoke:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: held installer disposal must be protected by finally after invocation")

    # Re-admission must occur after the hold is acquired, not only in the earlier package
    # sweep. Require the helper itself to bind expected signer and extraction root.
    helper_end = source.find("\n}\n", helper)
    if helper_end < 0:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: held-installer helper has no bounded body")
    helper_body = source[helper:helper_end]
    for token, label in (
        ("ExpectedSigner", "expected signer binding"),
        ("ExtractionRoot", "extraction-root binding"),
        ("Assert-AuthenticodeSigner", "held Authenticode verification"),
        ("[IO.FileShare]::Read", "held share mode"),
    ):
        if token not in helper_body:
            raise SystemExit(f"ERROR: V25 updater installer-hold preflight: helper missing {label}: {token}")

    # The historical vulnerable shape must not be reintroduced as an unheld final reopen.
    tail = source[acquire:dispose]
    if "& $installer @arguments" not in tail:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: installer execution escaped held interval")

    # Existing high-value update fences must remain present.
    for token, label in (
        ("Expand-VerifiedHeldArchive", "bounded ZIP/hash admission"),
        ("Assert-PackageRoot", "package signature/integrity admission"),
        ("Compare-StrictSemVer", "product-version anti-downgrade"),
        ("Enter-Qs3dUpdateMutex", "update serialization mutex"),
        ("Get-Process -Name bricscad", "running-BricsCAD guard"),
        ("Read-InstalledProductVersion", "installed-state freshness check"),
        ("RequireSigned = $true", "installer signed-payload enforcement"),
    ):
        require(source, token, label)


def self_test() -> None:
    vulnerable = """
function Open-HeldVerifiedInstaller { }
function Resolve-OrdinaryNonReparse { }
function Assert-AuthenticodeSigner { }
function Expand-VerifiedHeldArchive { }
function Assert-PackageRoot { }
function Compare-StrictSemVer { }
function Enter-Qs3dUpdateMutex { }
function Read-InstalledProductVersion { }
Get-Process -Name bricscad
$installer = 'x'
& $installer @arguments
RequireSigned = $true
"""
    try:
        validate(vulnerable)
    except SystemExit:
        pass
    else:
        raise SystemExit("ERROR: V25 updater installer-hold preflight self-test accepted verify-then-reopen shape")


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print("PASS: V25 updater holds the re-admitted installer across pathname execution")
