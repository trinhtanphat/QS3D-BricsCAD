#!/usr/bin/env python3
"""Fail closed if the V25 installer can approve payload and registry mutations separately."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"ERROR: V25 installer ShouldProcess transaction preflight failed: {message}")


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def first_index(source: str, pattern: str, label: str) -> int:
    match = re.search(pattern, source, flags=re.IGNORECASE | re.MULTILINE | re.DOTALL)
    if not match:
        fail(f"missing {label}")
    return match.start()


source = INSTALLER.read_text(encoding="utf-8")

# One user decision must admit or decline the whole mutating transaction. Independent
# per-target confirmations can otherwise register a Loader whose payload was declined.
should_process = list(re.finditer(r"\$PSCmdlet\s*\.\s*ShouldProcess\s*\(", source, flags=re.IGNORECASE))
require(
    len(should_process) == 1,
    f"expected exactly one transaction-level ShouldProcess call, found {len(should_process)}",
)
approval_index = should_process[0].start()

# A declined transaction must leave before the first payload/registry mutation and before
# the success messages. The negative branch is intentionally explicit so -WhatIf/-Confirm
# cannot fall through to DemandLoad registration.
decline = re.search(
    r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}",
    source,
    flags=re.IGNORECASE | re.MULTILINE | re.DOTALL,
)
require(decline is not None, "transaction approval must use an explicit negative/decline branch")
require(re.search(r"\breturn\b", decline.group("body"), flags=re.IGNORECASE) is not None,
        "declined transaction must return before any installer mutation")

payload_mutation_index = first_index(
    source,
    r"New-Item\s+-ItemType\s+Directory\s+-Path\s+\$stage\b",
    "staging-directory creation",
)
registry_mutation_index = first_index(
    source,
    r"New-Item(?:Property)?\s+-Path\s+\$target\.AppKey\b",
    "DemandLoad registry mutation",
)
success_index = first_index(source, r"Write-Host\s+[\"']QS3D installed:", "installer success report")
require(approval_index < payload_mutation_index, "transaction approval must precede payload mutation")
require(approval_index < registry_mutation_index, "transaction approval must precede registry mutation")
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

# Guard the guard against the original regression shape.
mutant = source[:approval_index] + "$PSCmdlet.ShouldProcess('extra', 'unsafe')\n" + source[approval_index:]
mutant_calls = list(re.finditer(r"\$PSCmdlet\s*\.\s*ShouldProcess\s*\(", mutant, flags=re.IGNORECASE))
require(len(mutant_calls) == 2, "preflight self-test could not construct independent-approval regression")

print("OK: V25 installer admits payload + DemandLoad writes as one ShouldProcess transaction")
