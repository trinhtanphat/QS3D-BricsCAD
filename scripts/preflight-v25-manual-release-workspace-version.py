#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
text = HELPER.read_text(encoding="utf-8")

errors = []

required = {
    "workspace version synchronizer": "function Set-WorkspaceProductVersion",
    "requested tag drives product version": "$productVersion = $tag.Substring(1)",
    "bounded V25 project path": "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "bounded V26 project path": "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
    "bounded Core project path": "src/QS3D.Core/QS3D.Core.csproj",
    "Version synchronization": "Set-ProjectVersionValue -Name 'Version' -Value $productVersion",
    "FileVersion synchronization": "Set-ProjectVersionValue -Name 'FileVersion' -Value $fileVersion",
    "InformationalVersion synchronization": "Set-ProjectVersionValue -Name 'InformationalVersion' -Value $productVersion",
    "workspace-only rewrite call": "Set-WorkspaceProductVersion",
    "post-sync identity validation": "Runtime product-version identity preflight failed after workspace synchronization.",
    "source HEAD remains release base": "Release workspace HEAD must remain the protected-main source commit.",
    "no commit/push mutation contract": "No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.",
}
for label, needle in required.items():
    if needle not in text:
        errors.append(f"missing {label}: {needle}")

forbidden = {
    "old committed-version reader": "function Get-CommittedProductVersion",
    "old protected-main version gate": "Merge the version update to protected main before publishing.",
    "old workspace rewrite prohibition": "workspace-only version rewrite",
}
for label, needle in forbidden.items():
    if needle in text:
        errors.append(f"stale {label} remains: {needle}")

# The intentional dirty workspace must be bounded to the V25/V26/Core identity trio.
if "Unexpected release-preparation workspace change" not in text:
    errors.append("missing fail-closed rejection for workspace changes outside the project identity trio")
if "Workspace version synchronization did not produce exactly three bounded project modifications." not in text:
    errors.append("missing exact-three intentional workspace modification assertion")

# Keep source identity and drift admission before any workspace mutation.
try:
    admission = text.index("Assert-ReleaseBaseIsSafe -TargetSha $releaseBase")
    sync = text.index("Set-WorkspaceProductVersion", admission)
    if sync <= admission:
        errors.append("workspace version synchronization must happen only after protected-main drift admission")
except ValueError:
    pass

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("PASS: V25 manual release synchronizes the coherent V25/V26/Core preview identity only in the bounded workspace while preserving protected-main source identity.")
