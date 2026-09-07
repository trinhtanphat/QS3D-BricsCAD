#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
GENERATOR = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"
VALIDATOR = ROOT / "scripts" / "assert-v26-candidate-identity.ps1"
REQUIRED_HOSTS = ("bricscad.exe", "BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(path: Path, tokens: list[str]) -> None:
    text = read(path)
    missing = [token for token in tokens if token not in text]
    if missing:
        raise SystemExit(
            f"{path.relative_to(ROOT)} missing V26 cloud host-reference provenance contract token(s): {missing}"
        )


def require_once(text: str, token: str, label: str) -> None:
    count = text.count(token)
    if count != 1:
        raise SystemExit(f"{label} must occur exactly once; found {count}: {token}")


workflow = read(WORKFLOW)
generator = read(GENERATOR)
validator = read(VALIDATOR)

require(
    WORKFLOW,
    [
        "V26_HOST_REFERENCE_STATE=$statePath",
        "Capture V26 host-reference generation",
        "Build BricsCAD V26 plugin against held reference generations",
        "Create V26 cloud candidate provenance",
        "new-v26-candidate-provenance.ps1",
        "dist\\QS3D-BricsCAD-V26.provenance.json",
        "assert-v26-candidate-identity.ps1",
        "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'",
    ],
)
require(
    GENERATOR,
    [
        "[string]$HostReferenceStatePath = $env:V26_HOST_REFERENCE_STATE",
        "$hostState = Read-StrictUtf8Json -Path $HostReferenceStatePath",
        "V26 host-reference state must contain exactly four required files",
        "changed after its admitted generation was captured",
        "hostReferences = @($hostReferences)",
        "^[0-9a-f]{64}$",
    ] + list(REQUIRED_HOSTS),
)
require(
    VALIDATOR,
    [
        "$hostReferences = @($provenance.hostReferences)",
        "must contain exactly four held host-reference identities",
        "must contain exactly one $name host-reference identity",
        "host-reference SHA-256 is noncanonical",
        "host-reference length must be positive",
        "HostReferences=$hostReferences",
    ] + list(REQUIRED_HOSTS),
)

# The workflow's state capture must precede the held-reference build and provenance emission.
capture = workflow.index("Capture V26 host-reference generation")
build = workflow.index("Build BricsCAD V26 plugin against held reference generations")
provenance = workflow.index("Create V26 cloud candidate provenance")
if not capture < build < provenance:
    raise SystemExit("V26 host-reference capture/build/provenance ordering is not fail-closed")

# The generator obtains the exact state path from the workflow environment at invocation time,
# reopens every recorded host path, and verifies its current length/hash before emitting provenance.
require_once(generator, "[string]$HostReferenceStatePath = $env:V26_HOST_REFERENCE_STATE", "generator environment binding")
for host in REQUIRED_HOSTS:
    require_once(generator, f"'{host}'", f"generator required host {host}")
    require_once(validator, f"'{host}'", f"validator required host {host}")

# Mutation controls prove the guard is sensitive to each critical cross-job contract.
mutations = {
    "workflow state binding": (workflow, "V26_HOST_REFERENCE_STATE=$statePath"),
    "workflow candidate generator": (workflow, "new-v26-candidate-provenance.ps1"),
    "generator environment binding": (generator, "[string]$HostReferenceStatePath = $env:V26_HOST_REFERENCE_STATE"),
    "generator generation re-verification": (generator, "changed after its admitted generation was captured"),
    "generator provenance field": (generator, "hostReferences = @($hostReferences)"),
    "validator provenance field": (validator, "$hostReferences = @($provenance.hostReferences)"),
    "validator duplicate/missing defense": (validator, "must contain exactly one $name host-reference identity"),
}
for label, (text, token) in mutations.items():
    if token not in text:
        raise SystemExit(f"mutation control unavailable: {label}")
    mutated = text.replace(token, "", 1)
    if token in mutated:
        raise SystemExit(f"mutation control did not remove guarded token: {label}")

print("PASS V26 cloud held host-reference provenance source guard")
