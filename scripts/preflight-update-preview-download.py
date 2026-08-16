#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required preview-download contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden preview-download behavior: {needle}")


def main():
    client_rel = "src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs"
    downloader_rel = "src/QS3D.BricsCAD.V25/Updates/VerifiedReleaseDownloader.cs"
    window_rel = "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
    start_rel = "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"

    client = read(client_rel)
    downloader = read(downloader_rel)
    window = read(window_rel)
    start = read(start_rel)

    for needle in (
        'internal const string PackageAssetName = "QS3D-BricsCAD-V25.zip";',
        'internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.zip.sha256";',
        "internal bool HasSignedUpdateManifest => ManifestUri != null;",
        "internal bool HasVerifiedPreviewPackage => PackageUri != null && PackageChecksumUri != null;",
        "TryGitHubUri(package.BrowserDownloadUrl",
        "TryGitHubUri(packageChecksum.BrowserDownloadUrl",
    ):
        require(client, needle, client_rel)

    for needle in (
        "private const long MaxPackageBytes = 256L * 1024L * 1024L;",
        "private const int MaxChecksumBytes = 64 * 1024;",
        "private const int NetworkTimeoutMilliseconds = 30000;",
        "private const int MaxRedirects = 8;",
        "private const int MaxReleaseTagPrefixChars = 48;",
        "EnsureAllowedUri(release.PackageUri);",
        "EnsureAllowedUri(release.PackageChecksumUri);",
        "var existingLength = new FileInfo(packagePath).Length;",
        "if (existingLength <= MaxPackageBytes)",
        "await CopyBoundedAsync(source, buffer, MaxChecksumBytes)",
        "private static async Task<HttpWebResponse> GetResponseFollowingRedirectsAsync(Uri uri)",
        "request.AllowAutoRedirect = false;",
        "request.Timeout = NetworkTimeoutMilliseconds;",
        "request.ReadWriteTimeout = NetworkTimeoutMilliseconds;",
        "var location = response.Headers[HttpResponseHeader.Location];",
        "if (!Uri.TryCreate(current, location, out nextUri) || nextUri == null)",
        "EnsureAllowedUri(nextUri);",
        "EnsureAllowedUri(response.ResponseUri);",
        "if (response.ContentLength > maxBytes)",
        "if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))",
        "if (!string.IsNullOrEmpty(uri.UserInfo))",
        'string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)',
        'string.Equals(host, "api.github.com", StringComparison.OrdinalIgnoreCase)',
        'host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)',
        "await DownloadBoundedAsync(release.PackageUri, partialPath, MaxPackageBytes)",
        "await CopyBoundedAsync(source, target, maxBytes)",
        "if (total > maxBytes)",
        "var actualSha256 = ComputeSha256(partialPath);",
        "if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))",
        "if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))",
        "File.Move(partialPath, packagePath);",
        "TryDelete(partialPath);",
        "var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);",
        "if (string.IsNullOrWhiteSpace(root))",
        'Path.Combine(root, "QS3D", "Updates", "Downloads", ToSafePathSegment(tag))',
        "var exactTag = value ?? string.Empty;",
        'if (IsWindowsReservedPathSegment(result)) result = "_" + result;',
        "if (result.Length > MaxReleaseTagPrefixChars)",
        "result = result.Substring(0, MaxReleaseTagPrefixChars).TrimEnd(' ', '.');",
        'return result + "~" + ComputeTagIdentity(exactTag);',
        "private static string ComputeTagIdentity(string value)",
        "var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);",
        "var hash = sha256.ComputeHash(bytes);",
        "private static bool IsWindowsReservedPathSegment(string value)",
        "var dotIndex = value.IndexOf('.');",
        "var stem = (dotIndex >= 0 ? value.Substring(0, dotIndex) : value).TrimEnd(' ');",
        'string.Equals(stem, "CON", StringComparison.OrdinalIgnoreCase)',
        'string.Equals(stem, "PRN", StringComparison.OrdinalIgnoreCase)',
        'string.Equals(stem, "AUX", StringComparison.OrdinalIgnoreCase)',
        'string.Equals(stem, "NUL", StringComparison.OrdinalIgnoreCase)',
        'stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)',
        'stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)',
    ):
        require(downloader, needle, downloader_rel)

    cache_size_gate = downloader.index("if (existingLength <= MaxPackageBytes)")
    cache_hash = downloader.index("var existingSha256 = ComputeSha256(packagePath);")
    if cache_size_gate > cache_hash:
        raise SystemExit("FAIL: cached preview package size must be checked before hashing the existing file")

    safe_segment_start = downloader.index("private static string ToSafePathSegment(string value)")
    reserved_gate = downloader.index('if (IsWindowsReservedPathSegment(result)) result = "_" + result;', safe_segment_start)
    prefix_bound = downloader.index("if (result.Length > MaxReleaseTagPrefixChars)", safe_segment_start)
    identity_return = downloader.index('return result + "~" + ComputeTagIdentity(exactTag);', safe_segment_start)
    if not (reserved_gate < prefix_bound < identity_return):
        raise SystemExit(
            "FAIL: release-tag cache segment must escape reserved names, bound its readable prefix, then append exact-tag identity"
        )

    for stale in (
        "request.AllowAutoRedirect = true;",
        "request.MaximumAutomaticRedirections",
        'if (result.Length == 0) return "release";',
    ):
        forbid(downloader, stale, downloader_rel)

    for needle in (
        "if (current.Release.HasVerifiedPreviewPackage)",
        "await DownloadPreviewAsync(current.Release);",
        "var verified = await new VerifiedReleaseDownloader().DownloadAsync(release);",
        'Process.Start(new ProcessStartInfo("explorer.exe", "/select,\\\"" + path + "\\\"") { UseShellExecute = true });',
        "await ScheduleUpdateAsync();",
    ):
        require(window, needle, window_rel)

    for needle in (
        "Process.Start(verified.Path",
        "Process.Start(_downloadedPackagePath",
        "File.Open(_downloadedPackagePath",
        "File.Open(verified.Path",
        "SecureUpdateLauncher.Launch(_downloadedPackagePath",
        "SecureUpdateLauncher.Launch(verified.Path",
    ):
        forbid(window, needle, window_rel)

    for needle in (
        "using QS3D.BricsCAD.V25.Updates;",
        'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())',
    ):
        require(start, needle, start_rel)
    for stale in (
        'SendStringToExecute("QS3DUPDATE',
        'SendStringToExecute("QSUPDATE',
    ):
        forbid(start, stale, start_rel)

    print(
        "PASS: V25 preview fallback discovers the exact package/checksum pair, bounds cached, declared and streamed network bytes before hashing, "
        "uses bounded request/read-write timeouts, validates every bounded HTTPS GitHub redirect hop and final response URI, rejects URI user-info, "
        "verifies SHA-256 before retaining the ZIP, escapes Windows reserved release-tag cache segments, bounds the readable cache prefix, appends a "
        "SHA-256 identity of the exact release tag to prevent case/sanitization cache collisions, stages under LocalApplicationData, exposes the Update "
        "Center directly from Start Center without command dispatch, and only reveals unsigned preview packages while the existing signed-manifest "
        "scheduling path remains separate."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
