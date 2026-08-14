#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        raise AssertionError(f"missing required file: {path}")
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"{label}: missing {token!r}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise AssertionError(f"{label}: forbidden {token!r}")


def main() -> int:
    try:
        installer = read("scripts/install-v25-autoload.ps1")
        launcher = read("scripts/INSTALL-QS3D.cmd")
        unblock_launcher = read("scripts/UNBLOCK-QS3D.cmd")
        unblock_helper = read("scripts/unblock-v25-netload.ps1")
        package = read("scripts/package-v25.ps1")
        release_package = read("scripts/package-v25-release.ps1")
        preferences = read("src/QS3D.BricsCAD.V25/Updates/UpdatePreferences.cs")
        settings = read("src/QS3D.BricsCAD.V25/Updates/UpdateSettingsCommands.cs")
        bootstrapper = read("src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs")
        coordinator = read("src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs")
        center = read("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
        launcher_cs = read("src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs")
        ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/UpdateRibbonAugmenter.cs")
        ribbon_coordinator = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
        entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")

        require(installer, "ConfirmImpact = 'Medium'", "one-click ShouldProcess behavior")
        require(installer, "Assert-PackageIntegrity -Directory $package", "installer package verification")
        require(installer, "Unblock-File -LiteralPath $destination -ErrorAction Stop", "installed payload MOTW repair")

        require(launcher, "ExecutionPolicy RemoteSigned", "one-click launcher")
        require(launcher, "-NonInteractive", "one-click launcher")
        require(launcher, "Get-AuthenticodeSignature", "one-click launcher")
        require(launcher, "SignatureStatus]::Valid", "signed installer bootstrap")
        require(launcher, "SignatureStatus]::NotSigned", "unsigned preview bootstrap")
        require(launcher, "Unblock-File -LiteralPath $p", "preview MOTW bootstrap")
        require(launcher, "& $p -Confirm:$false", "noninteractive installer invocation")
        if launcher.lower().count("powershell.exe") != 1:
            raise AssertionError("one-click launcher: bootstrap verification and installer invocation must stay in one PowerShell process")
        forbid(launcher, "ExecutionPolicy Bypass", "one-click launcher")

        require(unblock_launcher, "ExecutionPolicy RemoteSigned", "manual NETLOAD recovery launcher")
        require(unblock_launcher, "-NonInteractive", "manual NETLOAD recovery launcher")
        require(unblock_launcher, "Get-FileHash -LiteralPath $p -Algorithm SHA256", "manual NETLOAD recovery bootstrap hash")
        require(unblock_launcher, "Get-AuthenticodeSignature -LiteralPath $p", "manual NETLOAD recovery bootstrap signature")
        require(unblock_launcher, "Unblock-File -LiteralPath $p -ErrorAction Stop", "manual NETLOAD recovery helper bootstrap")
        require(unblock_launcher, "& $p -PackageDirectory $root", "manual NETLOAD recovery helper invocation")
        if unblock_launcher.lower().count("powershell.exe") != 1:
            raise AssertionError("manual NETLOAD recovery launcher: helper verification and invocation must stay in one PowerShell process")
        if unblock_launcher.index("Get-FileHash -LiteralPath $p -Algorithm SHA256") > unblock_launcher.index("Unblock-File -LiteralPath $p -ErrorAction Stop"):
            raise AssertionError("manual NETLOAD recovery launcher: helper hash must be verified before the helper is unblocked")
        forbid(unblock_launcher, "ExecutionPolicy Bypass", "manual NETLOAD recovery launcher")

        require(unblock_helper, "Assert-Qs3dPackageIntegrity -Root $package", "manual NETLOAD package verification")
        require(unblock_helper, "Get-FileHash -LiteralPath $path -Algorithm SHA256", "manual NETLOAD package hash verification")
        require(unblock_helper, "SHA256SUMS coverage mismatch", "manual NETLOAD complete manifest coverage")
        require(unblock_helper, "PACKAGE-METADATA target must be BricsCAD V25 x64", "manual NETLOAD V25 identity")
        require(unblock_helper, "'QS3D.BricsCAD.V25.dll'", "manual NETLOAD required plugin payload")
        require(unblock_helper, "'QS3D.Core.dll'", "manual NETLOAD required Core payload")
        require(unblock_helper, "'UNBLOCK-QS3D.cmd'", "manual NETLOAD recovery launcher manifest coverage")
        require(unblock_helper, "Unblock-File -LiteralPath $packageFile.FullName -ErrorAction Stop", "manual NETLOAD complete package MOTW repair")
        if unblock_helper.index("Assert-Qs3dPackageIntegrity -Root $package") > unblock_helper.index("Unblock-File -LiteralPath $packageFile.FullName -ErrorAction Stop"):
            raise AssertionError("manual NETLOAD recovery helper: complete package integrity must be verified before package files are unblocked")

        require(package, "'INSTALL-QS3D.cmd'", "release package")
        require(package, "'UNBLOCK-QS3D.cmd'", "manual NETLOAD recovery package")
        require(package, "'unblock-v25-netload.ps1'", "manual NETLOAD recovery helper package")
        require(package, "gitCommit = $gitCommit", "package source provenance")
        require(package, "Get-SourceGitCommit", "package source provenance")
        require(package, "do not NETLOAD the DLL directly from Downloads", "safe install guidance")
        require(package, "Unsigned cloud previews are explicitly warned", "preview install guidance")
        require(package, '"Operation is not supported"', "manual NETLOAD MOTW guidance")
        require(package, "Do not unblock only one DLL", "manual NETLOAD dependency guidance")
        require(release_package, "$metadata.gitCommit", "signed release provenance validation")
        require(release_package, "does not match the exact clean package source HEAD", "signed release provenance validation")

        require(preferences, 'InstallOnExitValue = "InstallOnExit"', "update preference")
        require(preferences, "ReadBoolean(InstallOnExitValue, false)", "safe update-on-close default")
        require(settings, '[CommandMethod("QS3DUPDATEONCLOSE"', "update-on-close command")
        require(settings, '[CommandMethod("QS3DUPDATESTATUS"', "update status command")

        require(coordinator, "_ = CheckAsync(true);", "automatic startup update check")
        require(center, 'MakeButton("Cập nhật ngay", true)', "Update Center manual button")
        require(bootstrapper, "TryScheduleVerifiedUpdateOnExit();", "update-on-close lifecycle")
        require(bootstrapper, "UpdatePreferences.InstallOnExit", "update-on-close lifecycle")
        require(bootstrapper, "result.CanAutoInstall", "verified release requirement")
        require(bootstrapper, "SecureUpdateLauncher.TrySchedule(release, out _)", "secure detached updater handoff")

        require(launcher_cs, "while (Get-Process -Name bricscad", "detached updater host wait")
        require(launcher_cs, "Get-AuthenticodeSignature -LiteralPath $updater", "detached updater signer verification")
        require(launcher_cs, "Start-Process -FilePath $bricscad", "post-update BricsCAD restart")

        require(ribbon, 'PanelTitle = "Hệ thống"', "update ribbon panel")
        require(ribbon, '"Cập nhật QS3D", "QS3DUPDATE"', "update ribbon button")
        require(ribbon, '"Update khi đóng", "QS3DUPDATEONCLOSE"', "update-on-close ribbon button")
        require(ribbon, '"Trạng thái Update", "QS3DUPDATESTATUS"', "update status ribbon button")
        require(ribbon_coordinator, "UpdateRibbonAugmenter.TryInitialize()", "update ribbon retry bootstrap")
        require(entry, "RibbonInitializationCoordinator.Start();", "ribbon initialization coordinator bootstrap")
        require(entry, "UpdateRibbonAugmenter.Reset();", "update ribbon teardown")

    except (OSError, UnicodeError, AssertionError, ValueError) as exc:
        print("ERROR:", exc)
        return 1

    print("PASS: secure install, integrity-first manual NETLOAD MOTW recovery, signed release provenance, automatic check, update-on-close, and retry-coordinated ribbon update UX contracts are guarded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
