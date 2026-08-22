from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        raise FileNotFoundError(relative)
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str, failures: list[str]) -> None:
    if token not in text:
        failures.append(f"{label} is missing required token: {token}")


def main() -> int:
    failures: list[str] = []
    try:
        shared_runner = read("scripts/test-bricscad-v25-source-reconcile-native-beam-dependent.ps1")
        v26_runner = read("scripts/test-bricscad-v26-native-beam-dependent.ps1")
        probe = read("src/QS3D.BricsCAD.V25/SourceReconcileNativeBeamDependentRuntimeProbeCommands.cs")
        v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
        qualification = read("docs/LOCAL-V26-QUALIFICATION.md")
        inbox = read("docs/LOCAL-AGENT-INBOX.md")
    except (OSError, UnicodeError) as exc:
        print(f"FAIL: cannot read V26 native Beam qualification inputs: {exc}")
        return 1

    for token in (
        "[ValidateSet(25, 26)][int]$HostMajor = 25",
        '"src\\QS3D.BricsCAD.V26\\bin\\x64\\Release\\net8.0-windows\\QS3D.BricsCAD.V26.dll"',
        '[IO.Path]::ChangeExtension($PluginDll, ".runtimeconfig.json")',
        "$bricscadVersion.FileMajorPart -ne $HostMajor",
        "Assert-Qs3dExactCandidateAssembly",
        "Assert-Qs3dV26DotNetRoot",
        '[Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")',
        'Join-Path $root "host\\fxr"',
        'Join-Path $root "shared\\Microsoft.NETCore.App"',
        'Join-Path $_.FullName "hostfxr.dll"',
        'Join-Path $_.FullName "coreclr.dll"',
        '[IO.Path]::ChangeExtension($AssemblyPath, ".pdb")',
        "https://raw.githubusercontent.com/trinhtanphat/QS3D-BricsCAD/",
        "$declaredVersion + \"+\" + $GitHead",
        "bricscad_host_major = $HostMajor",
        "plugin_product_version =",
    ):
        require(shared_runner, token, "shared V25/V26 Beam runner", failures)
    if '$expectedAssemblyRevision = "+" + $gitHead' in shared_runner:
        failures.append("shared Beam runner still relies only on the superseded +gitSHA ProductVersion suffix")

    for token in (
        "test-bricscad-v25-source-reconcile-native-beam-dependent.ps1",
        "-HostMajor 26",
        "-ConfirmDisposableCopies:$ConfirmDisposableCopies",
        "-StartupTimeoutSeconds $StartupTimeoutSeconds",
    ):
        require(v26_runner, token, "V26 Beam runner", failures)

    for token in (
        "QS3DSRBEAMP03PREPARE",
        "QS3DSRBEAMP03MOVECHECK",
        "QS3DSRBEAMP03SYNCCHECK",
        "QS3DSRBEAMP03REOPEN",
        '"production_local004_p03_qualified=true"',
        '"cold_reopen_verified=true"',
    ):
        require(probe, token, "native Beam dependent probe", failures)

    require(
        v26_project,
        '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
        "V26 shared adapter compile surface",
        failures,
    )
    for text, label in ((qualification, "V26 qualification"), (inbox, "local-agent inbox")):
        for token in ("test-bricscad-v26-native-beam-dependent.ps1", "#3573", "PENDING_LOCAL"):
            require(text, token, label, failures)

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    print(
        "PASS: V26 P01 reuses the production Beam/native MOVE/reconcile/dependent rebuild/cold-reopen "
        "probe with strict host-major, stable product-version, exact SourceLink SHA and disposable-fixture guards."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
