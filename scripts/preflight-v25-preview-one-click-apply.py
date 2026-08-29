#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    path = ROOT / rel
    if not path.exists():
        raise SystemExit(f"FAIL: missing required file for V25 preview one-click apply: {rel}")
    return path.read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required V25 preview one-click apply contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden V25 preview one-click apply behavior: {needle}")


def main():
    installer_rel = "src/QS3D.BricsCAD.V25/Updates/VerifiedPreviewInstaller.cs"
    window_rel = "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"

    installer = read(installer_rel)
    window = read(window_rel)

    for needle in (
        "internal static class VerifiedPreviewInstaller",
        "internal static bool TrySchedule(string packagePath, string expectedSha256, out string error)",
        "Assembly.GetExecutingAssembly().Location",
        'new[] { "QS3D.BricsCAD.V25.dll", "QS3D.Core.dll" }',
        "ComputeSha256(packagePath)",
        "string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase)",
        "ZipArchive",
        "GetFullPath",
        "StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase)",
        "using (var currentProcess = Process.GetCurrentProcess())",
        "parentProcessId = currentProcess.Id;",
        "WaitForExit",
        "File.Copy(sourcePath, backupPath, true)",
        "File.Replace",
        "Rollback",
    ):
        require(installer, needle, installer_rel)

    for needle in (
        "await new VerifiedReleaseDownloader().DownloadAsync(release)",
        "VerifiedPreviewInstaller.TrySchedule(verified.Path, verified.Sha256, out var installError)",
        '"Tải & cài đặt"',
        "SecureUpdateLauncher.TryRequestGracefulHostClose(out var closeError)",
    ):
        require(window, needle, window_rel)

    for needle in (
        "Process.Start(verified.Path",
        "Process.Start(_downloadedPackagePath",
        "File.Copy(verified.Path, Assembly.GetExecutingAssembly().Location",
        "File.Copy(_downloadedPackagePath, Assembly.GetExecutingAssembly().Location",
        "RevealDownloadedFile(",
        'Process.Start(new ProcessStartInfo("explorer.exe"',
    ):
        forbid(window, needle, window_rel)

    print(
        "PASS: verified V25 preview package is staged and re-hashed, required adapter/Core payload is resolved safely, "
        "replacement is deferred until the current BricsCAD process exits, current files are backed up with rollback, "
        "and Update Center schedules install instead of merely revealing the downloaded ZIP."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
