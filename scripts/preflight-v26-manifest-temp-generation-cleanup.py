#!/usr/bin/env python3
"""Fail closed unless every generated V26 manifest dependency is generation-owned through cleanup."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def fail(message: str) -> None:
    print(f"ERROR: V26 manifest temp-generation cleanup preflight failed closed: {message}", file=sys.stderr)
    raise SystemExit(1)


def validate(source: str) -> list[str]:
    errors: list[str] = []
    required = {
        "three-dependency generation plan": "$generationPlan = @(",
        "validation-core dependency": "Source = 'new-v25-update-manifest-validation-core.ps1'",
        "native-helper dependency": "Source = 'Qs3dV25UpdateManifestPublicationNative.cs'",
        "wrapper dependency": "Source = 'new-v25-update-manifest.ps1'",
        "held graph": "$heldGenerations = [Collections.Generic.List[object]]::new()",
        "held admission": "$heldGenerations.Add([pscustomobject]@{ Admission = $admission; Path = $entry.Output; Label = $entry.Label })",
        "graph revalidation": "Assert-HeldGeneratedTemplate -Admission $held.Admission -ExpectedPath $held.Path -Label $held.Label",
        "per-generation identity": "$identity = [string]$held.Admission.Identity",
        "per-generation stream dispose": "$held.Admission.Stream.Dispose()",
        "exact generation cleanup": "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
        "held workspace cleanup": "Remove-HeldManifestWorkspace -Handle $workspaceHandle",
    }
    for label, token in required.items():
        if token not in source:
            errors.append(f"missing {label}: {token}")
    for token in (
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $tempRoot",
        "Remove-Item -LiteralPath $held.Path",
        "FILE_SHARE_DELETE",
    ):
        if token in source:
            errors.append(f"unsafe pathname/delete-share cleanup remains: {token}")

    plan = source.find("$generationPlan = @(")
    add = source.find("$heldGenerations.Add(", plan)
    invoke = source.find("& $tempScript @forward", add)
    cleanup_loop = source.find("for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)", invoke)
    dispose = source.find("$held.Admission.Stream.Dispose()", cleanup_loop)
    cleanup = source.find("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", dispose)
    workspace = source.find("Remove-HeldManifestWorkspace -Handle $workspaceHandle", cleanup)
    if min(plan, add, invoke, cleanup_loop, dispose, cleanup, workspace) < 0 or not (
        plan < add < invoke < cleanup_loop < dispose < cleanup < workspace
    ):
        errors.append("graph lifecycle must be plan -> hold all dependencies -> invoke -> reverse dispose/exact cleanup -> held workspace cleanup")
    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        fail("; ".join(errors))
    probes = {
        "held graph admission": source.replace("$heldGenerations.Add(", "# removed(", 1),
        "exact generation cleanup": source.replace("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", "# removed", 1),
        "workspace cleanup": source.replace("Remove-HeldManifestWorkspace -Handle $workspaceHandle", "# removed", 1),
    }
    for label, mutated in probes.items():
        if not validate(mutated):
            fail(f"mutation probe was not rejected: {label}")
    print("PASS: V26 wrapper/core/native temp generations remain held, revalidated, and exact-generation cleaned.")


if __name__ == "__main__":
    main()
