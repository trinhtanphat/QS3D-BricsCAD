#!/usr/bin/env python3
"""Fail closed unless V25 commercial temp-tree cleanup is generation-bound and non-following."""

from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "remove-v25-release-temp-tree.ps1"


def contract_errors(workflow: str | None, helper: str | None) -> list[str]:
    errors: list[str] = []
    if workflow is None:
        return ["missing .github/workflows/release-v25.yml"]
    if helper is None:
        errors.append("missing scripts/remove-v25-release-temp-tree.ps1")
        helper = ""

    forbidden_workflow = (
        "Remove-Item -LiteralPath $verificationRoot -Recurse",
        "Remove-Item -LiteralPath $heldRoot -Recurse",
        "Remove-Item -LiteralPath $extract -Recurse",
    )
    for token in forbidden_workflow:
        if token in workflow:
            errors.append(f"commercial release cleanup reopens an unbound recursive pathname: {token}")

    invocation = "scripts\\remove-v25-release-temp-tree.ps1"
    if workflow.count(invocation) < 4:
        errors.append("both commercial verification phases must generation-bind cleanup of extraction and held roots")

    required_helper = (
        "FILE_FLAG_OPEN_REPARSE_POINT",
        "FILE_FLAG_BACKUP_SEMANTICS",
        "FILE_SHARE_READ | FILE_SHARE_WRITE",
        "DELETE_ACCESS",
        "SetFileInformationByHandle",
        "FileDispositionInfoEx",
        "FILE_DISPOSITION_FLAG_DELETE",
        "GetFileInformationByHandle",
        "GetFinalPathNameByHandleW",
        "OpenPathNoFollow",
        "Remove-HeldTree",
        "[IO.FileAttributes]::ReparsePoint",
        "GetFinalDosPath",
    )
    for token in required_helper:
        if token not in helper:
            errors.append(f"generation-bound cleanup helper missing invariant: {token}")

    forbidden_helper = (
        "Remove-Item -Recurse",
        "Directory.Delete($Path, $true)",
        "Directory.Delete($canonical, $true)",
        "FILE_SHARE_DELETE",
        "cmd /c rmdir",
    )
    for token in forbidden_helper:
        if token in helper:
            errors.append(f"generation-bound cleanup helper contains unsafe fallback: {token}")

    hold = helper.find("OpenPathNoFollow")
    enumerate_children = helper.find("Get-ChildItem", hold)
    recurse = helper.find("Remove-HeldTree", enumerate_children)
    dispose = helper.find("Set-DispositionDelete", recurse)
    if min(hold, enumerate_children, recurse, dispose) < 0:
        errors.append("cleanup helper must hold the current generation while enumerating/receding and delete by handle")

    return errors


def self_test() -> list[str]:
    workflow = r'''& .\scripts\remove-v25-release-temp-tree.ps1 -Path $verificationRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $heldRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $extract
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $heldRoot
'''
    helper = r'''FILE_FLAG_OPEN_REPARSE_POINT FILE_FLAG_BACKUP_SEMANTICS
FILE_SHARE_READ | FILE_SHARE_WRITE DELETE_ACCESS
SetFileInformationByHandle FileDispositionInfoEx FILE_DISPOSITION_FLAG_DELETE
GetFileInformationByHandle GetFinalPathNameByHandleW OpenPathNoFollow
function Remove-HeldTree {
  $h = OpenPathNoFollow
  GetFinalDosPath
  [IO.FileAttributes]::ReparsePoint
  $children = Get-ChildItem
  Remove-HeldTree
  Set-DispositionDelete
}
'''
    errors: list[str] = []
    if contract_errors(workflow, helper):
        errors.append("guard rejected intended generation-bound cleanup contract")
    mutants = {
        "workflow recursive delete": (workflow + "\nRemove-Item -LiteralPath $extract -Recurse -Force", helper),
        "delete sharing": (workflow, helper + "\nFILE_SHARE_DELETE"),
        "recursive Directory.Delete": (workflow, helper + "\nDirectory.Delete($Path, $true)"),
        "missing handle deletion": (workflow, helper.replace("SetFileInformationByHandle", "SetFileInformationByName", 1)),
        "missing no-follow": (workflow, helper.replace("FILE_FLAG_OPEN_REPARSE_POINT", "0", 1)),
    }
    for label, (wf, hp) in mutants.items():
        if not contract_errors(wf, hp):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def main() -> int:
    errors = self_test()
    try:
        workflow = WORKFLOW.read_text(encoding="utf-8")
    except OSError:
        workflow = None
    try:
        helper = HELPER.read_text(encoding="utf-8")
    except OSError:
        helper = None
    errors.extend(contract_errors(workflow, helper))
    if errors:
        for error in errors:
            print("FAIL:", error)
        return 1
    print("PASS: V25 commercial release temp-tree cleanup is bound to no-follow generations and deletes by handle without recursive pathname traversal.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
