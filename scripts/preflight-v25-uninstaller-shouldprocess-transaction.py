#!/usr/bin/env python3
"""Fail closed unless the V25 uninstaller admits its selected mutations as one transaction."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
UNINSTALLER = ROOT / "scripts" / "uninstall-v25-autoload.ps1"


class ContractError(RuntimeError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractError(message)


def find(source: str, pattern: str, label: str):
    match = re.search(pattern, source, flags=re.IGNORECASE | re.MULTILINE | re.DOTALL)
    if not match:
        raise ContractError(f"missing {label}")
    return match


def validate(source: str) -> None:
    # Helper definitions are above the host guard; this marker begins executable behavior.
    entry = find(source, r"^\s*if\s*\(\s*Get-Process\s+-Name\s+bricscad\b", "BricsCAD executable-body guard")
    body = source[entry.start():]

    approvals = list(re.finditer(r"\$PSCmdlet\s*\.\s*ShouldProcess\s*\(", body, flags=re.IGNORECASE))
    require(len(approvals) == 1, f"expected exactly one uninstall transaction ShouldProcess call, found {len(approvals)}")
    approval = approvals[0].start()

    decline = re.search(
        r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}",
        body,
        flags=re.IGNORECASE | re.MULTILINE | re.DOTALL,
    )
    require(decline is not None, "uninstall admission must use an explicit negative ShouldProcess branch")
    require(
        re.fullmatch(r"\s*return\s*", decline.group("body"), flags=re.IGNORECASE) is not None,
        "declined uninstall transaction must directly return",
    )

    preapproval = body[:approval]
    require(
        re.search(
            r"(?im)^\s*(?:New-Item(?:Property)?|Set-ItemProperty|Remove-Item(?:Property)?|Move-Item|Copy-Item|Unblock-File)\b",
            preapproval,
        ) is None,
        "filesystem/registry mutation appears before uninstall transaction approval",
    )

    for pattern, label in (
        (r"Move-Item\s+-LiteralPath\s+\$installFull\s+-Destination\s+\$quarantine", "payload quarantine move"),
        (r"Remove-Item\s+-LiteralPath\s+\$entry\.Target\.AppKey", "DemandLoad removal"),
        (r"Remove-Item\s+-LiteralPath\s+\$quarantine", "quarantine cleanup"),
        (r"Write-Host\s+[\"']QS3D DemandLoad registration removed", "success report"),
    ):
        require(approval < find(body, pattern, label).start(), f"uninstall approval must precede {label}")

    # -KeepFiles must remain a planning input to the one transaction rather than creating a
    # second confirmation surface. The mutation branch itself still skips payload staging.
    require("$KeepFiles" in body, "uninstaller lost -KeepFiles semantics")
    require(
        re.search(r"if\s*\(\s*-not\s+\$KeepFiles\b", body, flags=re.IGNORECASE) is not None,
        "uninstaller must preserve the no-file-removal -KeepFiles branch",
    )

    for token, label in (
        ("Assert-InstallDirectorySafeToRemove", "install-directory identity validation"),
        ("Get-RegistryTreeSnapshot", "registry snapshot"),
        ("Restore-RegistryTreeSnapshot", "registry rollback"),
        ("Enter-Qs3dUpdateMutex", "update mutex"),
        ("Get-Process -Name bricscad", "running-host guard"),
        ("$quarantine", "payload quarantine rollback"),
        ("$rollbackFailures", "rollback error reporting"),
    ):
        require(token in source, f"missing existing safety control: {label}")


def expect_rejected(label: str, mutant: str) -> None:
    try:
        validate(mutant)
    except ContractError:
        return
    raise ContractError(f"preflight self-test failed to reject {label}")


source = UNINSTALLER.read_text(encoding="utf-8")
try:
    validate(source)
except ContractError as exc:
    raise SystemExit(f"ERROR: V25 uninstaller ShouldProcess transaction preflight failed: {exc}")

entry = find(source, r"^\s*if\s*\(\s*Get-Process\s+-Name\s+bricscad\b", "self-test executable marker")
body = source[entry.start():]
approval_match = find(body, r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}", "self-test transaction approval")
approval_start = entry.start() + approval_match.start()
approval_end = entry.start() + approval_match.end()
approval_block = source[approval_start:approval_end]
decline_body = approval_match.group("body")

expect_rejected(
    "independent per-target confirmation",
    source[:approval_end] + "\n$PSCmdlet.ShouldProcess('registry-target', 'unsafe split') | Out-Null" + source[approval_end:],
)
expect_rejected(
    "decline fall-through",
    source[:approval_start] + approval_block.replace(decline_body, " Write-Verbose 'unsafe' ", 1) + source[approval_end:],
)
expect_rejected(
    "pre-admission registry mutation",
    source[:approval_start] + "New-ItemProperty -Path 'HKCU:\\unsafe' -Name Loader -Value bad -Force\n" + source[approval_start:],
)

print("OK: V25 uninstaller admits selected file + DemandLoad removals as one ShouldProcess transaction")
