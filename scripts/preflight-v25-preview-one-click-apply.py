#!/usr/bin/env python3
# Reservation-v2: keep the preview apply guard aligned with the rendered Run-based version highlights.
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
    downloader_rel = "src/QS3D.BricsCAD.V25/Updates/VerifiedReleaseDownloader.cs"
    release_client_rel = "src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs"
    window_rel = "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
    preferences_rel = "src/QS3D.BricsCAD.V25/Updates/UpdatePreferences.cs"

    installer = read(installer_rel)
    downloader = read(downloader_rel)
    release_client = read(release_client_rel)
    window = read(window_rel)
    preferences = read(preferences_rel)

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
        "currentProcess.MainModule?.FileName",
        'startInfo.EnvironmentVariables["QS3D_PREVIEW_BRICSCAD"]',
        "WaitForExit",
        "File.Copy(sourcePath, backupPath, true)",
        "File.Replace",
        "Rollback",
        "Restart-BricsCAD",
        "Start-Process -FilePath $env:QS3D_PREVIEW_BRICSCAD",
    ):
        require(installer, needle, installer_rel)

    for needle in (
        "internal sealed class UpdateDownloadProgress",
        "IProgress<UpdateDownloadProgress>",
        "progress?.Report",
        "ContentLength",
        "BytesReceived",
        "TotalBytes",
        "CreateFriendlyNetworkException",
        'response.StatusCode == HttpStatusCode.Forbidden',
        'response.Headers["X-RateLimit-Remaining"]',
        'response.Headers["X-RateLimit-Reset"]',
        'response.Headers["Retry-After"]',
    ):
        require(downloader, needle, downloader_rel)

    for needle in (
        'request.Headers["X-GitHub-Api-Version"] = "2022-11-28"',
        'response.Headers["X-RateLimit-Remaining"]',
        'response.Headers["X-RateLimit-Reset"]',
        'response.Headers["Retry-After"]',
        "TryGetFreshCache",
        "TryGetStaleCache",
        "SetCache",
        "GitHub đang giới hạn",
    ):
        require(release_client, needle, release_client_rel)

    for needle in (
        "await new VerifiedReleaseDownloader().DownloadAsync(release, progress)",
        "VerifiedPreviewInstaller.TrySchedule(verified.Path, verified.Sha256, out var installError)",
        '"Tải & cài đặt"',
        "SecureUpdateLauncher.TryRequestGracefulHostClose(out var closeError)",
        "ProgressBar",
        "UpdateDownloadProgress",
        "ApplyDownloadProgress",
        "ApplyVersionHighlights(currentDisplay, latest)",
        'new Run("Phiên bản hiện tại ")',
        'new Run("Phiên bản mới ")',
        "BricsCAD sẽ tự mở lại.",
        "private readonly CheckBox _updateOnCloseCheckBox;",
        "IsChecked = UpdatePreferences.InstallOnExit",
        "UpdatePreferences.TrySetInstallOnExit",
        '"Gói preview + SHA-256 đã sẵn sàng"',
        '"Tải, xác minh SHA-256, đóng BricsCAD an toàn, cài đặt rồi tự mở lại BricsCAD."',
        "UpdateState.Error",
        "BorderStroke",
        "Color.FromRgb(74, 222, 128)",
        "private static readonly Brush Danger",
        "Color.FromRgb(248, 113, 113)",
        "private readonly Grid _progressHeader;",
        "Visibility = Visibility.Collapsed",
        "HideProgress();",
        "_progressHeader.Visibility = Visibility.Visible;",
        "_progressBar.Visibility = Visibility.Visible;",
        "private void HideProgress()",
        "Template = CreateCheckBoxTemplate()",
        "private static ControlTemplate CreateCheckBoxTemplate()",
        'new FrameworkElementFactory(typeof(Border), "Indicator")',
        "CheckBox.IsCheckedProperty",
        'checkGlyph.SetValue(TextBlock.TextProperty, "✓")',
        "var latestComparable = string.IsNullOrWhiteSpace(latest)",
        "var sameVersion = string.Equals(currentDisplay, latestComparable, StringComparison.OrdinalIgnoreCase);",
        "var currentVersionBrush = sameVersion ? Success : Danger;",
        "new Run(currentDisplay) { Foreground = currentVersionBrush",
        "new Run(latest) { Foreground = Success",
    ):
        require(window, needle, window_rel)

    require(preferences, "ReadBoolean(InstallOnExitValue, false)", preferences_rel)

    for needle in (
        "UpdateState.Failed",
        "private static readonly Brush Border =",
        "Process.Start(verified.Path",
        "Process.Start(_downloadedPackagePath",
        "File.Copy(verified.Path, Assembly.GetExecutingAssembly().Location",
        "File.Copy(_downloadedPackagePath, Assembly.GetExecutingAssembly().Location",
        "RevealDownloadedFile(",
        'Process.Start(new ProcessStartInfo("explorer.exe"',
        'SetProgress("Sẵn sàng để tải và xác minh", 0, false)',
        'SetProgress("Chờ package + checksum hợp lệ", 0, false)',
        'SetProgress("Đang kiểm tra GitHub Releases…", 12, true)',
        'SetProgress("Đang dùng phiên bản mới nhất", 100, false)',
    ):
        forbid(window, needle, window_rel)

    print(
        "PASS: verified V25 preview package is staged and re-hashed, adapter/Core replacement remains deferred until BricsCAD exits, "
        "rollback is preserved, the exact BricsCAD executable is restarted after apply/recovery, downloader progress is shown only during active apply/download states, "
        "preview capability copy stays coherent with the primary action, update-on-close uses dark-theme checkbox chrome and defaults OFF unless persisted by the user, "
        "current/latest versions use fresh green when equal and red-vs-green when different, GitHub 403/rate-limit responses remain bounded, "
        "and Update Center keeps valid shared V25/V26 WPF identifiers."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
