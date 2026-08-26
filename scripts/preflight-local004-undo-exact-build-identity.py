#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-undo-lifecycle.ps1"
PROPS = ROOT / "Directory.Build.props"

runner = RUNNER.read_text(encoding="utf-8")
props = PROPS.read_text(encoding="utf-8")

errors: list[str] = []


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        errors.append(label)


require(props, "<Deterministic>true</Deterministic>", "Directory.Build.props must keep deterministic builds enabled")
require(
    props,
    "<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>",
    "ProductVersion must stay decoupled from Git SHA",
)

for forbidden in ("$expectedRevision", ".VersionInfo.ProductVersion", "EndsWith($expectedRevision"):
    if forbidden in runner:
        errors.append(f"retired ProductVersion exact-SHA assertion returned: {forbidden}")

required_runner_contract = {
    "function Assert-Qs3dExactBuildIdentity": "exact-build verifier function is missing",
    '"build", $project, "-c", "Release"': "verifier must perform a Release rebuild from the exact checkout",
    '"-p:Platform=x64"': "verifier must reproduce the x64 adapter build",
    '("-p:BRICSCAD_V25_DIR=" + $BricsCadDir)': "verifier must use the supplied licensed V25 reference directory",
    '"-p:AppendTargetFrameworkToOutputPath=false"': "verifier must pin the comparison output layout",
    'Get-FileHash -LiteralPath $pair[0] -Algorithm SHA256': "actual binary SHA-256 comparison is missing",
    'Get-FileHash -LiteralPath $pair[1] -Algorithm SHA256': "rebuilt binary SHA-256 comparison is missing",
    'throw "LOCAL-004 Undo lifecycle assembly does not match the exact deterministic build."': "stale/wrong binary rejection is missing",
    'Assert-Qs3dExactBuildIdentity -RepoRoot $repoRoot -BricsCadDir $BricsCadDir -PluginDll $PluginDll -CoreDll $coreDll': "runner does not invoke exact-build verification",
}
for needle, label in required_runner_contract.items():
    require(runner, needle, label)

call = runner.find("Assert-Qs3dExactBuildIdentity -RepoRoot $repoRoot")
launch = runner.find("Start-Process -FilePath $bricscadExe")
clean = runner.find("status --porcelain=v1 --untracked-files=all")
if call < 0 or launch < 0 or call >= launch:
    errors.append("exact-build verification must fail closed before BricsCAD launch")
if clean < 0 or call < 0 or clean >= call:
    errors.append("clean exact-SHA worktree verification must precede exact-build verification")

for assembly in ("QS3D.BricsCAD.V25.dll", "QS3D.Core.dll"):
    require(runner, assembly, f"exact-build verification must cover {assembly}")

if errors:
    print("ERROR: LOCAL-004 Undo exact-build identity preflight failed:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS LOCAL-004 Undo exact deterministic build identity guard")
