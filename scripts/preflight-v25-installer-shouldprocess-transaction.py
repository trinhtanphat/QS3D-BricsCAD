#!/usr/bin/env python3
"""Fail closed if the V25 installer can approve payload and registry mutations separately."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"


class ContractError(RuntimeError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractError(message)


def first_index(source: str, pattern: str, label: str) -> int:
    match = re.search(pattern, source, flags=re.IGNORECASE | re.MULTILINE | re.DOTALL)
    if not match:
        raise ContractError(f"missing {label}")
    return match.start()


def validate(source: str) -> None:
    # Restrict confirmation semantics to the executable transaction body. Helper functions
    # legitimately contain registry/file mutations for rollback and admission checks.
    transaction_start = first_index(
        source,
        r"^\s*\$registrySnapshots\s*=",
        "installer transaction start",
    )
    transaction = source[transaction_start:]

    # One user decision must admit or decline the whole mutating transaction. Independent
    # per-target confirmations can otherwise register a Loader whose payload was declined.
    should_process = list(
        re.finditer(r"\$PSCmdlet\s*\.\s*ShouldProcess\s*\(", transaction, flags=re.IGNORECASE)
    )
    require(
        len(should_process) == 1,
        f"expected exactly one transaction-level ShouldProcess call, found {len(should_process)}",
    )
    approval_index = should_process[0].start()

    # The negative branch must be a direct terminal return; accepting a nested/unreachable
    # return would let -WhatIf/-Confirm fall through to mutations.
    decline = re.search(
        r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}",
        transaction,
        flags=re.IGNORECASE | re.MULTILINE | re.DOTALL,
    )
    require(decline is not None, "transaction approval must use an explicit negative/decline branch")
    require(
        re.fullmatch(r"\s*return\s*", decline.group("body"), flags=re.IGNORECASE) is not None,
        "declined transaction must directly return before any installer mutation",
    )

    # Fail closed on any filesystem/registry mutation before the single admission decision,
    # not just on the currently expected first mutation. This guards future refactors too.
    preapproval = transaction[:approval_index]
    premature_mutation = re.search(
        r"(?im)^\s*(?:New-Item(?:Property)?|Set-ItemProperty|Remove-Item(?:Property)?|Move-Item|Copy-Item|Unblock-File)\b",
        preapproval,
    )
    require(premature_mutation is None, "filesystem/registry mutation appears before transaction approval")

    mutation_patterns = (
        (r"New-Item\s+-ItemType\s+Directory\s+-Path\s+\$parent\b", "install-parent creation"),
        (r"New-Item\s+-ItemType\s+Directory\s+-Path\s+\$stage\b", "staging-directory creation"),
        (r"\bCopy-Item\s+-LiteralPath\s+\$source\b", "payload copy"),
        (r"\bMove-Item\s+-LiteralPath\s+\$stage\b", "payload commit"),
        (r"New-Item(?:Property)?\s+-Path\s+\$target\.AppKey\b", "DemandLoad registry mutation"),
    )
    for pattern, label in mutation_patterns:
        require(
            approval_index < first_index(transaction, pattern, label),
            f"transaction approval must precede {label}",
        )

    success_index = first_index(
        transaction,
        r"Write-Host\s+[\"']QS3D installed:",
        "installer success report",
    )
    require(approval_index < success_index, "transaction approval must precede success reporting")

    # Preserve the existing atomicity machinery rather than solving confirmation semantics by
    # bypassing rollback, package admission, or serialization.
    for token, label in (
        ("Assert-PackageIntegrity", "package-integrity admission"),
        ("Assert-StagedPayloadAdmission", "staged-payload admission"),
        ("Assert-UnblockedAdmittedPayload", "MOTW admission"),
        ("Assert-ExistingInstallDirectorySafeToReplace", "existing-install validation"),
        ("Get-DemandLoadSnapshot", "registry snapshots"),
        ("Restore-DemandLoadSnapshot", "registry rollback"),
        ("Enter-Qs3dUpdateMutex", "update mutex"),
        ("Get-RunningBricsCADProcessDetails", "running-host guard"),
    ):
        require(token in source, f"missing existing safety control: {label}")


def expect_rejected(label: str, mutant: str) -> None:
    try:
        validate(mutant)
    except ContractError:
        return
    raise ContractError(f"preflight self-test failed to reject {label}")


source = INSTALLER.read_text(encoding="utf-8")
try:
    validate(source)
except ContractError as exc:
    raise SystemExit(f"ERROR: V25 installer ShouldProcess transaction preflight failed: {exc}")

# Adversarial self-tests make sure the guard detects the regression classes it claims to own.
transaction_match = re.search(r"^\s*\$registrySnapshots\s*=.*$", source, flags=re.IGNORECASE | re.MULTILINE)
require(transaction_match is not None, "preflight self-test could not locate transaction marker")
transaction_insert = transaction_match.end()
expect_rejected(
    "independent registry approval",
    source[:transaction_insert] + "\n$PSCmdlet.ShouldProcess('extra', 'unsafe')" + source[transaction_insert:],
)

approval_match = re.search(
    r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}",
    source[transaction_match.start():],
    flags=re.IGNORECASE | re.MULTILINE | re.DOTALL,
)
require(approval_match is not None, "preflight self-test could not locate transaction approval block")
approval_abs_start = transaction_match.start() + approval_match.start()
approval_abs_end = transaction_match.start() + approval_match.end()
approval_block = source[approval_abs_start:approval_abs_end]
decline_body = approval_match.group("body")
require(re.search(r"\breturn\b", decline_body, flags=re.IGNORECASE) is not None,
        "preflight self-test could not locate decline return")
unsafe_decline_body = re.sub(
    r"\breturn\b",
    "Write-Verbose 'unsafe fall-through'",
    decline_body,
    count=1,
    flags=re.IGNORECASE,
)
unsafe_decline_block = approval_block.replace(decline_body, unsafe_decline_body, 1)
expect_rejected(
    "decline fall-through",
    source[:approval_abs_start] + unsafe_decline_block + source[approval_abs_end:],
)

expect_rejected(
    "mutation before transaction approval",
    source[:approval_abs_start] + "New-Item -Path 'unsafe' -Force\n" + source[approval_abs_start:],
)

without_approval = source[:approval_abs_start] + source[approval_abs_end:]
new_transaction_match = re.search(r"^\s*\$registrySnapshots\s*=.*$", without_approval,
                                  flags=re.IGNORECASE | re.MULTILINE)
require(new_transaction_match is not None, "preflight self-test lost transaction marker")
late_mutation = re.search(
    r"New-Item\s+-ItemType\s+Directory\s+-Path\s+\$parent\b[^\n]*\n",
    without_approval[new_transaction_match.start():],
    flags=re.IGNORECASE,
)
require(late_mutation is not None, "preflight self-test could not locate first main-path mutation")
late_abs_end = new_transaction_match.start() + late_mutation.end()
expect_rejected(
    "approval after first mutation",
    without_approval[:late_abs_end] + approval_block + "\n" + without_approval[late_abs_end:],
)

print("OK: V25 installer admits payload + DemandLoad writes as one ShouldProcess transaction")
