#!/usr/bin/env python3
"""Fail closed unless the V25 uninstaller admits one TOCTOU-safe transaction."""
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

    quarantine_move = find(
        body,
        r"Move-Item\s+-LiteralPath\s+\$installFull\s+-Destination\s+\$quarantine",
        "payload quarantine move",
    )
    demandload_remove = find(
        body,
        r"Remove-Item\s+-LiteralPath\s+\$entry\.Target\.AppKey",
        "DemandLoad removal",
    )
    cleanup = find(body, r"Remove-Item\s+-LiteralPath\s+\$quarantine", "quarantine cleanup")
    success = find(body, r"Write-Host\s+[\"']QS3D DemandLoad registration removed", "success report")
    for mutation, label in (
        (quarantine_move, "payload quarantine move"),
        (demandload_remove, "DemandLoad removal"),
        (cleanup, "quarantine cleanup"),
        (success, "success report"),
    ):
        require(approval < mutation.start(), f"uninstall approval must precede {label}")

    # Approval can be arbitrarily delayed. Re-admit every destructive input after approval
    # and before the first mutation, then promote only the fresh snapshots into rollback.
    first_mutation = min(quarantine_move.start(), demandload_remove.start())
    post_approval = body[decline.end():first_mutation]
    for token, label in (
        ("$freshInstallFull = Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory -ForceDelete:$Force",
         "post-approval install-directory identity validation"),
        ("$freshPayloadSnapshot = Get-InstallPayloadSnapshot -Directory $freshInstallFull",
         "post-approval payload snapshot"),
        ("Get-RegistryRemovalPlan -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys",
         "post-approval registry plan"),
        ("Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual $freshPayloadSnapshot",
         "payload identity revalidation"),
        ("Assert-RegistryPlanEqual -Expected $registryPlan -Actual $freshRegistryPlan",
         "registry plan revalidation"),
        ("$registryPlan = @($freshRegistryPlan)", "fresh rollback snapshot promotion"),
    ):
        require(token in post_approval, f"missing {label} between ShouldProcess and first mutation")

    payload_revalidation = find(
        body,
        re.escape("Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual (Get-InstallPayloadSnapshot -Directory $installFull)"),
        "immediate payload revalidation",
    )
    require(
        approval < payload_revalidation.start() < quarantine_move.start(),
        "payload must be revalidated after approval and before quarantine move",
    )
    between_payload_revalidation_and_move = body[payload_revalidation.end():quarantine_move.start()]
    require(
        re.search(r"(?im)^\s*(?:New-Item(?:Property)?|Set-ItemProperty|Remove-Item(?:Property)?|Move-Item|Copy-Item|Unblock-File)\b", between_payload_revalidation_and_move) is None,
        "no filesystem/registry mutation may occur between payload revalidation and quarantine move",
    )

    registry_revalidation = find(
        body,
        re.escape("Assert-RegistryTreeSnapshotEqual -Expected $entry.Snapshot -Actual (Get-RegistryTreeSnapshot -Path $entry.Target.AppKey)"),
        "immediate DemandLoad registry revalidation",
    )
    require(
        approval < registry_revalidation.start() < demandload_remove.start(),
        "DemandLoad key must be revalidated after approval and before destructive removal",
    )
    between_registry_revalidation_and_remove = body[registry_revalidation.end():demandload_remove.start()]
    require(
        re.search(r"(?im)^\s*(?:New-Item(?:Property)?|Set-ItemProperty|Remove-Item(?:Property)?|Move-Item|Copy-Item|Unblock-File)\b", between_registry_revalidation_and_remove) is None,
        "no filesystem/registry mutation may occur between DemandLoad revalidation and removal",
    )

    # -KeepFiles remains a planning input to the one transaction rather than a second prompt.
    require("$KeepFiles" in body, "uninstaller lost -KeepFiles semantics")
    require(
        re.search(r"if\s*\(\s*-not\s+\$KeepFiles\b", body, flags=re.IGNORECASE) is not None,
        "uninstaller must preserve the no-file-removal -KeepFiles branch",
    )

    for token, label in (
        ("Assert-InstallDirectorySafeToRemove", "install-directory identity validation"),
        ("Get-InstallPayloadSnapshot", "recursive payload identity snapshot"),
        ("Assert-InstallPayloadSnapshotEqual", "payload snapshot comparison"),
        ("Get-RegistryTreeSnapshot", "registry snapshot"),
        ("Assert-RegistryTreeSnapshotEqual", "registry snapshot comparison"),
        ("Get-RegistryRemovalPlan", "registry removal plan"),
        ("Assert-RegistryPlanEqual", "registry plan comparison"),
        ("Restore-RegistryTreeSnapshot", "registry rollback"),
        ("Enter-Qs3dUpdateMutex", "update mutex"),
        ("Get-Process -Name bricscad", "running-host guard"),
        ("$quarantine", "payload quarantine rollback"),
        ("$rollbackFailures", "rollback error reporting"),
        ("ReparsePoint", "payload reparse rejection"),
        ("Get-FileHash", "payload content identity hashing"),
        ("[StringComparer]::Ordinal", "deterministic payload identity ordering"),
        ("[Array]::Sort($relativePaths, [StringComparer]::Ordinal)", "ordinal payload snapshot ordering"),
    ):
        require(token in source, f"missing safety control: {label}")

    restore = find(
        source,
        r"function\s+Restore-RegistryTreeSnapshot\s*\{(?P<body>.*?)^\}",
        "registry restore helper",
    )
    restore_body = restore.group("body")
    require(
        re.search(r"Remove-Item\s+-LiteralPath\s+\$path\b", restore_body, flags=re.IGNORECASE) is None,
        "registry rollback must not delete a path recreated by a foreign writer",
    )
    require(
        "Refusing registry rollback because DemandLoad path was recreated" in restore_body,
        "registry rollback must fail closed when a foreign writer recreates the target path",
    )


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
approval_match = find(
    body,
    r"if\s*\(\s*-not\s*\(\s*\$PSCmdlet\s*\.\s*ShouldProcess\s*\([^)]*\)\s*\)\s*\)\s*\{(?P<body>.*?)\}",
    "self-test transaction approval",
)
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
expect_rejected(
    "missing post-approval registry revalidation",
    source.replace(
        "Assert-RegistryPlanEqual -Expected $registryPlan -Actual $freshRegistryPlan",
        "# removed registry revalidation",
        1,
    ),
)
expect_rejected(
    "missing immediate payload revalidation",
    source.replace(
        "Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual (Get-InstallPayloadSnapshot -Directory $installFull)",
        "# removed payload revalidation",
        1,
    ),
)
expect_rejected(
    "mutation between payload revalidation and quarantine move",
    source.replace(
        "Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual (Get-InstallPayloadSnapshot -Directory $installFull)",
        "Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual (Get-InstallPayloadSnapshot -Directory $installFull)\nRemove-Item -LiteralPath 'unsafe' -Force",
        1,
    ),
)
registry_revalidation_token = "Assert-RegistryTreeSnapshotEqual -Expected $entry.Snapshot -Actual (Get-RegistryTreeSnapshot -Path $entry.Target.AppKey)"
registry_remove_token = "Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop"
expect_rejected(
    "registry revalidation moved after destructive removal",
    source.replace(registry_revalidation_token, "# moved registry revalidation", 1).replace(
        registry_remove_token,
        registry_remove_token + "\n            " + registry_revalidation_token,
        1,
    ),
)
expect_rejected(
    "foreign-writer destructive rollback",
    source.replace(
        "throw \"Refusing registry rollback because DemandLoad path was recreated: $path\"",
        "Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop",
        1,
    ),
)

print("OK: V25 uninstaller has one approval plus post-approval TOCTOU revalidation and non-destructive rollback")
