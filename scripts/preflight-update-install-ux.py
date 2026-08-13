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
        package = read("scripts/package-v25.ps1")
        preferences = read("src/QS3D.BricsCAD.V25/Updates/UpdatePreferences.cs")
        settings = read("src/QS3D.BricsCAD.V25/Updates/UpdateSettingsCommands.cs")
        bootstrapper = read("src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs")
        coordinator = read("src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs")
        center = read("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
        launcher_cs = read("src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs")
        ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/UpdateRibbonAugmenter.cs")
        entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")

        require(installer, "ConfirmImpact = 'Medium'", "one-click ShouldProcess behavior")
        require(installer, "Assert-PackageIntegrity -Directory $package", "installer package verification")
        require(installer, "Unblock-File -LiteralPath $destination -ErrorAction Stop", "installed payload MOTW repair")

        require(launcher, "ExecutionPolicy RemoteSigned", "one-click launcher")
        require(launcher, "Get-AuthenticodeSignature", "one-click launcher")
        require(launcher, "SignatureStatus]::Valid", "signed installer bootstrap")
        require(launcher, "SignatureStatus]::NotSigned", "unsigned preview bootstrap")
        require(launcher, "Unblock-File -LiteralPath $p", "preview MOTW bootstrap")
        forbid(launcher, "ExecutionPolicy Bypass", "one-click launcher")
        require(package, "'INSTALL-QS3D.cmd'", "release package")
        require(package, "do not NETLOAD the DLL directly from Downloads", "safe install guidance")
        require(package, "Unsigned cloud previews are explicitly warned", "preview install guidance")

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
        require(entry, "UpdateRibbonAugmenter.TryInitialize();", "update ribbon bootstrap")
        require(entry, "UpdateRibbonAugmenter.Reset();", "update ribbon teardown")

    except (OSError, UnicodeError, AssertionError) as exc:
        print("ERROR:", exc)
        return 1

    print("PASS: secure install, preview MOTW bootstrap, automatic check, update-on-close, and ribbon update UX contracts are guarded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
