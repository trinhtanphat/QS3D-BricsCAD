#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    target = ROOT / path
    if not target.is_file(): raise AssertionError("missing required file: " + path)
    return target.read_text(encoding="utf-8")

def require(text, token, label):
    if token not in text: raise AssertionError(label + ": missing " + repr(token))

def forbid(text, token, label):
    if token in text: raise AssertionError(label + ": forbidden " + repr(token))

def main():
    try:
        installer = read("scripts/install-v25-autoload.ps1")
        launcher = read("scripts/INSTALL-QS3D.cmd")
        unblock = read("scripts/UNBLOCK-QS3D.cmd")
        helper = read("scripts/unblock-v25-netload.ps1")
        package = read("scripts/package-v25.ps1")
        release_package = read("scripts/package-v25-release.ps1")
        release = read(".github/workflows/release-v25.yml")
        finalize = read("scripts/finalize-v25-signed-package.ps1")
        prefs = read("src/QS3D.BricsCAD.V25/Updates/UpdatePreferences.cs")
        settings = read("src/QS3D.BricsCAD.V25/Updates/UpdateSettingsCommands.cs")
        bootstrap = read("src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs")
        coordinator = read("src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs")
        center = read("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
        secure = read("src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs")
        ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/UpdateRibbonAugmenter.cs")
        ribbon_coord = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
        entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")

        for token in ("ConfirmImpact = 'Medium'", "Assert-PackageIntegrity -Directory $package", "Unblock-File -LiteralPath $destination -ErrorAction Stop"): require(installer, token, "installer")
        for token in ("ExecutionPolicy RemoteSigned", "-NonInteractive", "Get-AuthenticodeSignature", "SignatureStatus]::Valid", "SignatureStatus]::NotSigned", "Unblock-File -LiteralPath $p", "& $p -Confirm:$false"): require(launcher, token, "one-click launcher")
        forbid(launcher, "ExecutionPolicy Bypass", "one-click launcher")
        for token in ("ExecutionPolicy RemoteSigned", "Get-FileHash -LiteralPath $p -Algorithm SHA256", "Get-AuthenticodeSignature -LiteralPath $p", "Unblock-File -LiteralPath $p -ErrorAction Stop", "& $p -PackageDirectory $root"): require(unblock, token, "manual NETLOAD recovery")
        forbid(unblock, "ExecutionPolicy Bypass", "manual NETLOAD recovery")
        for token in ("Assert-Qs3dPackageIntegrity -Root $package", "SHA256SUMS coverage mismatch", "PACKAGE-METADATA target must be BricsCAD V25 x64", "Unblock-File -LiteralPath $packageFile.FullName -ErrorAction Stop"): require(helper, token, "recovery helper")
        for token in ("'INSTALL-QS3D.cmd'", "'UNBLOCK-QS3D.cmd'", "'unblock-v25-netload.ps1'", "gitCommit = $gitCommit", "Get-SourceGitCommit"): require(package, token, "package")
        require(release_package, "does not match the exact clean package source HEAD", "signed release provenance")
        signed_recovery = r"'dist\QS3D-BricsCAD-V25\unblock-v25-netload.ps1'"
        require(release, signed_recovery, "commercial recovery helper signing")
        if release.count(signed_recovery) < 2: raise AssertionError("commercial release must sign and verify recovery helper")
        require(finalize, "signedExecutablePayload", "signed executable metadata")
        require(prefs, 'InstallOnExitValue = "InstallOnExit"', "update preference")
        require(prefs, "ReadBoolean(InstallOnExitValue, false)", "safe update-on-close default")
        require(settings, '[CommandMethod("QS3DUPDATEONCLOSE"', "update-on-close compatibility command")
        require(settings, '[CommandMethod("QS3DUPDATESTATUS"', "update status compatibility command")
        require(coordinator, "_ = CheckAsync(true);", "automatic startup update check")
        require(center, 'MakeButton("Cập nhật ngay", true)', "Update Center")
        for token in ("TryScheduleVerifiedUpdateOnExit();", "UpdatePreferences.InstallOnExit", "result.CanAutoInstall", "SecureUpdateLauncher.TrySchedule(release, out _)"): require(bootstrap, token, "update-on-close lifecycle")
        for token in ("while (Get-Process -Name bricscad", "Get-AuthenticodeSignature -LiteralPath $updater", "Start-Process -FilePath $bricscad"): require(secure, token, "detached updater")

        for token in (
            'PanelTitle = "Hệ thống"',
            '"Cập nhật QS3D", () => new UpdateCommands().ShowUpdateCenter()',
            '"Update khi đóng", ToggleInstallOnExit',
            '"Trạng thái Update", ShowUpdateStatus',
            'UpdatePreferences.TrySetInstallOnExit',
            'UpdatePreferences.InstallOnExit',
            'DirectActionHandler',
            'SetProperty(button, "ShowImage", true)',
            'SetProperty(button, "LargeImage", RibbonIconFactory.Create',
        ):
            require(ribbon, token, "update ribbon")
        forbid(ribbon, "SendStringToExecute", "update ribbon")
        forbid(ribbon, 'new CommandHandler()', "update ribbon")

        require(ribbon_coord, "UpdateRibbonAugmenter.TryInitialize()", "update ribbon retry bootstrap")
        require(entry, "RibbonInitializationCoordinator.Start();", "ribbon bootstrap")
        require(entry, "TryCleanup(UpdateRibbonAugmenter.Reset);", "contained update ribbon teardown")
    except (OSError, UnicodeError, AssertionError, ValueError) as exc:
        print("ERROR:", exc); return 1
    print("PASS: secure update lifecycle remains guarded while Home update controls use direct button actions and rasterized icons instead of host command dispatch.")
    return 0

if __name__ == "__main__": raise SystemExit(main())
