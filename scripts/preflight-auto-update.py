#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATES = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates"
PLUGIN_ENTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing required updater source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    semantic = read(UPDATES / "SemanticReleaseVersion.cs")
    client = read(UPDATES / "GitHubReleaseClient.cs")
    launcher = read(UPDATES / "SecureUpdateLauncher.cs")
    coordinator = read(UPDATES / "UpdateCoordinator.cs")
    ui = read(UPDATES / "UpdateCenterWindow.cs")
    commands = read(UPDATES / "UpdateCommands.cs")
    bootstrapper = read(UPDATES / "UpdateBootstrapper.cs")
    plugin_entry = read(PLUGIN_ENTRY)
    workflow = read(WORKFLOW)
    package = read(PACKAGE)

    require(semantic, "SemanticReleaseVersion : IComparable<SemanticReleaseVersion>", "strict SemVer type")
    require(semantic, "leftNumeric ? -1 : 1", "SemVer numeric prerelease precedence")
    require(semantic, "_prerelease.Length == 0) return 1", "stable-over-prerelease precedence")

    require(client, 'Repository = "trinhtanphat/QS3D-BricsCAD"', "pinned GitHub repository")
    require(client, 'ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=20"', "HTTPS GitHub Releases endpoint")
    require(client, 'UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json"', "signed update manifest asset contract")
    require(client, "release.Prerelease != version.IsPrerelease", "GitHub/tag prerelease consistency gate")
    require(client, 'candidate.Host, "github.com"', "GitHub release/asset host allowlist")
    require(client, "MaxResponseBytes", "bounded GitHub response")

    require(coordinator, "current.IsPrerelease || !release.IsPrerelease", "stable/prerelease channel policy")
    require(coordinator, "!latest.HasSignedUpdateManifest", "manifest eligibility gate")
    require(coordinator, "TryGetCurrentSignerThumbprint", "running publisher trust anchor gate")
    require(coordinator, "ScheduleLatestAsync", "fresh-check one-click scheduling")

    require(launcher, 'new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE")', "WINTRUST_ACTION_GENERIC_VERIFY_V2 policy")
    require(launcher, "TryVerifyAuthenticode(pluginPath, out reason)", "running plugin Authenticode verification gate")
    require(launcher, "WinVerifyTrust(IntPtr.Zero, WinTrustActionGenericVerifyV2, ref trustData)", "Windows trust-provider verification")
    require(launcher, "WtdStateActionVerify", "WinVerifyTrust state verification")
    require(launcher, "WtdStateActionClose", "WinVerifyTrust state cleanup")
    require(launcher, "if (status == 0) return true;", "verified Authenticode success requirement")
    require(launcher, "X509Certificate.CreateFromSignedFile", "verified running plugin publisher extraction")
    verify_pos = launcher.find("if (!TryVerifyAuthenticode(pluginPath, out reason)) return false;")
    signer_pos = launcher.find("X509Certificate.CreateFromSignedFile(pluginPath)")
    if verify_pos < 0 or signer_pos < 0 or verify_pos >= signer_pos:
        raise AssertionError("running plugin Authenticode trust must be verified before its signer certificate can become the updater publisher anchor")

    require(launcher, "while (Get-Process -Name bricscad", "wait-for-BricsCAD-exit handoff")
    require(launcher, "Get-AuthenticodeSignature -LiteralPath $updater", "installed updater signature validation")
    require(launcher, "Installed updater signer mismatch", "updater signer pinning")
    require(launcher, "-AllowedPackageHost @('github.com')", "package host allowlist handoff")
    require(launcher, "-AllowSameVersion", "newer prerelease same-assembly-version handoff")
    require(launcher, "TryRequestGracefulHostClose", "graceful host-close API")
    require(launcher, "process.CloseMainWindow()", "WM_CLOSE-style BricsCAD close request")
    require(launcher, '"QS3D",\n                    "UpdateLogs"', "log path outside replaceable plugin directory")
    reject(launcher, "Stop-Process", "forced BricsCAD termination")
    reject(launcher, "taskkill", "forced BricsCAD termination")

    # Readiness timeout may terminate only the detached PowerShell updater child before
    # BricsCAD is asked to close. Generic/current-host process termination remains forbidden.
    require(launcher, "private static void TryTerminateUnreadyWorker(Process updater)", "narrow detached-worker cleanup helper")
    require(launcher, "updater.Kill();", "detached-worker readiness-timeout cleanup")
    kill_lines = [line.strip() for line in launcher.splitlines() if ".Kill(" in line]
    if kill_lines != ["updater.Kill();"]:
        raise AssertionError("only detached updater.Kill() is permitted; found: " + repr(kill_lines))
    if "process.Kill(" in launcher or "Process.GetCurrentProcess().Kill(" in launcher:
        raise AssertionError("BricsCAD/current-process force termination is forbidden")

    require(commands, '[CommandMethod("QS3DUPDATE", CommandFlags.Modal)]', "QS3DUPDATE command")
    require(ui, 'MakeButton("Kiểm tra lại"', "manual refresh button")
    require(ui, 'MakeButton("Cập nhật ngay"', "one-click update button")
    require(ui, "TryRequestGracefulHostClose", "one-click graceful close wiring")
    require(ui, "ShowModelessWindow", "modeless Update Center")
    require(bootstrapper, "AutomaticUpdateFound += OnAutomaticUpdateFound", "automatic release notification")
    require(plugin_entry, "UpdateBootstrapper.Start();", "plugin initialize updater start")
    require(plugin_entry, "UpdateBootstrapper.Stop();", "plugin terminate updater stop")

    require(package, "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs'", "recursive command discovery")
    require(package, "[CommandMethod", "DemandLoad command extraction")

    require(workflow, "Create signed auto-update manifest", "signed manifest generation step")
    require(workflow, "if: ${{ inputs.sign_package }}", "signed-only manifest generation")
    require(workflow, "new-v25-update-manifest.ps1", "canonical manifest generator")
    require(workflow, "QS3D-BricsCAD-V25.update.json", "manifest release asset")
    require(workflow, "if ($signPackage) { $releaseAssets += 'dist\\QS3D-BricsCAD-V25.update.json' }", "signed-only manifest upload")
    require(workflow, "if ($signPackage) { $expectedAssets += 'QS3D-BricsCAD-V25.update.json' }", "signed release publication gate")
    require(workflow, "unsigned explicit prerelease preview; manual install only", "unsigned release manual-only policy")

    print("PASS: secure GitHub release auto-update source contract is present.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
