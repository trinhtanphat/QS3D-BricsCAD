#!/usr/bin/env python3
"""Fail closed unless V25 commercial temp-tree cleanup is generation-bound and non-following."""

from __future__ import annotations
from pathlib import Path
import os
import subprocess

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "remove-v25-release-temp-tree.ps1"
RUNTIME_TEST = ROOT / "scripts" / "test-v25-release-temp-cleanup.ps1"


def contract_errors(workflow: str | None, helper: str | None, runtime: str | None = None) -> list[str]:
    errors: list[str] = []
    if workflow is None:
        return ["missing .github/workflows/release-v25.yml"]
    if helper is None:
        errors.append("missing scripts/remove-v25-release-temp-tree.ps1")
        helper = ""
    if runtime is None:
        errors.append("missing scripts/test-v25-release-temp-cleanup.ps1")
        runtime = ""

    forbidden_workflow = (
        "Remove-Item -LiteralPath $verificationRoot -Recurse",
        "Remove-Item -LiteralPath $heldRoot -Recurse",
        "Remove-Item -LiteralPath $extract -Recurse",
        "Remove-Item -LiteralPath $downloadRoot -Recurse",
    )
    for token in forbidden_workflow:
        if token in workflow:
            errors.append(f"commercial release cleanup reopens an unbound recursive pathname: {token}")

    invocation = "scripts\\remove-v25-release-temp-tree.ps1"
    if workflow.count(invocation) != 6:
        errors.append("all six commercial verification/held/download temp-root cleanup sites must use the generation-bound helper exactly once")

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
        "ExpectedParent",
        "immediate child of the admitted temp parent",
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

    function_start = helper.find("function Remove-HeldTree")
    hold = helper.find("OpenPathNoFollow($canonical)", function_start)
    enumerate_children = helper.find("Get-ChildItem -LiteralPath $canonical", hold)
    recurse = helper.find("Remove-HeldTree -LiteralPath $child.FullName", enumerate_children)
    delete_by_handle = helper.find("SetDispositionDelete($handle)", recurse)
    dispose = helper.find("$handle.Dispose()", delete_by_handle)
    if not (0 <= function_start < hold < enumerate_children < recurse < delete_by_handle < dispose):
        errors.append("cleanup helper must hold the exact current generation while enumerating children, recurse through held entries, delete by handle, then dispose")

    required_runtime = (
        "outside-must-survive",
        "New-Item -ItemType Junction",
        "outside-link",
        "substituted-root",
        "followed a child junction and deleted the outside sentinel",
        "followed a substituted root junction and deleted outside data",
        "& $helper -Path $cleanup -ExpectedParent $tempRoot",
        "& $helper -Path $substituted -ExpectedParent $tempRoot",
    )
    for token in required_runtime:
        if token not in runtime:
            errors.append(f"release cleanup runtime harness missing adversarial contract: {token}")

    return errors


def self_test() -> list[str]:
    workflow = r'''& .\scripts\remove-v25-release-temp-tree.ps1 -Path $verificationRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $heldRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $extract
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $heldRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $heldRoot
& .\scripts\remove-v25-release-temp-tree.ps1 -Path $downloadRoot
'''
    helper = r'''FILE_FLAG_OPEN_REPARSE_POINT FILE_FLAG_BACKUP_SEMANTICS
FILE_SHARE_READ | FILE_SHARE_WRITE DELETE_ACCESS
SetFileInformationByHandle FileDispositionInfoEx FILE_DISPOSITION_FLAG_DELETE
GetFileInformationByHandle GetFinalPathNameByHandleW OpenPathNoFollow GetFinalDosPath
ExpectedParent immediate child of the admitted temp parent
function Remove-HeldTree {
  $handle = OpenPathNoFollow($canonical)
  [IO.FileAttributes]::ReparsePoint
  $children = Get-ChildItem -LiteralPath $canonical
  Remove-HeldTree -LiteralPath $child.FullName
  SetDispositionDelete($handle)
  $handle.Dispose()
}
'''
    runtime = r'''outside-must-survive
New-Item -ItemType Junction -Path outside-link
New-Item -ItemType Junction -Path substituted-root
& $helper -Path $cleanup -ExpectedParent $tempRoot
& $helper -Path $substituted -ExpectedParent $tempRoot
throw 'followed a child junction and deleted the outside sentinel'
throw 'followed a substituted root junction and deleted outside data'
'''
    errors: list[str] = []
    if contract_errors(workflow, helper, runtime):
        errors.append("guard rejected intended generation-bound cleanup contract")
    mutants = {
        "workflow recursive delete": (workflow + "\nRemove-Item -LiteralPath $extract -Recurse -Force", helper, runtime),
        "missing cleanup site": (workflow.replace("& .\\scripts\\remove-v25-release-temp-tree.ps1 -Path $downloadRoot\n", "", 1), helper, runtime),
        "delete sharing": (workflow, helper + "\nFILE_SHARE_DELETE", runtime),
        "recursive Directory.Delete": (workflow, helper + "\nDirectory.Delete($Path, $true)", runtime),
        "missing handle deletion": (workflow, helper.replace("SetFileInformationByHandle", "SetFileInformationByName", 1), runtime),
        "missing no-follow": (workflow, helper.replace("FILE_FLAG_OPEN_REPARSE_POINT", "0", 1), runtime),
        "drops outside sentinel": (workflow, helper, runtime.replace("outside-must-survive", "sentinel omitted", 1)),
        "drops substituted-root test": (workflow, helper, runtime.replace("substituted-root", "ordinary-root", 1)),
    }
    for label, (wf, hp, rt) in mutants.items():
        if not contract_errors(wf, hp, rt):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def read(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except OSError:
        return None


def run_windows_runtime() -> list[str]:
    if os.name != "nt":
        return []
    try:
        completed = subprocess.run(
            ["powershell", "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(RUNTIME_TEST)],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=120,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as exc:
        return [f"unable to execute Windows release cleanup adversarial runtime harness: {exc}"]
    if completed.returncode != 0:
        tail = completed.stdout[-4000:] if completed.stdout else "<no output>"
        return [f"Windows release cleanup adversarial runtime harness failed (exit={completed.returncode}):\n{tail}"]
    print(completed.stdout, end="")
    return []


def main() -> int:
    errors = self_test()
    errors.extend(contract_errors(read(WORKFLOW), read(HELPER), read(RUNTIME_TEST)))
    if not errors:
        errors.extend(run_windows_runtime())
    if errors:
        for error in errors:
            print("FAIL:", error)
        return 1
    print("PASS: V25 commercial release temp-tree cleanup is bound to no-follow generations, deletes by handle, and preserves outside junction targets.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
