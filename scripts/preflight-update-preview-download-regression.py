#!/usr/bin/env python3
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PREFLIGHT = Path("scripts/preflight-update-preview-download.py")
CLIENT = Path("src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs")
DOWNLOADER = Path("src/QS3D.BricsCAD.V25/Updates/VerifiedReleaseDownloader.cs")
WINDOW = Path("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
START = Path("src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs")
FIXTURE_FILES = (PREFLIGHT, CLIENT, DOWNLOADER, WINDOW, START)


class PreviewDownloadGuardMutationTests(unittest.TestCase):
    maxDiff = None

    def make_fixture(self):
        temporary = tempfile.TemporaryDirectory(prefix="qs3d-preview-guard-")
        fixture = Path(temporary.name)
        for relative in FIXTURE_FILES:
            source = ROOT / relative
            self.assertTrue(source.is_file(), f"missing fixture source: {relative}")
            target = fixture / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
        return temporary, fixture

    def run_guard(self, fixture):
        return subprocess.run(
            [sys.executable, str(fixture / PREFLIGHT)],
            cwd=str(fixture),
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=30,
        )

    def mutate(self, fixture, relative, old, new):
        path = fixture / relative
        text = path.read_text(encoding="utf-8")
        self.assertIn(old, text, f"mutation anchor missing in {relative}: {old}")
        path.write_text(text.replace(old, new, 1), encoding="utf-8")

    def assert_rejected(self, relative, old, new, expected):
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        self.mutate(fixture, relative, old, new)
        result = self.run_guard(fixture)
        self.assertNotEqual(0, result.returncode, result.stdout)
        self.assertIn(expected, result.stdout)

    def test_pristine_current_contract_passes(self):
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        result = self.run_guard(fixture)
        self.assertEqual(0, result.returncode, result.stdout)
        self.assertIn("PASS: V25 preview fallback", result.stdout)

    def test_package_asset_identity_is_pinned(self):
        self.assert_rejected(
            CLIENT,
            'internal const string PackageAssetName = "QS3D-BricsCAD-V25.zip";',
            'internal const string PackageAssetName = "QS3D-BricsCAD.zip";',
            "missing required preview-download contract: internal const string PackageAssetName",
        )

    def test_checksum_asset_identity_is_pinned(self):
        self.assert_rejected(
            CLIENT,
            'internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.zip.sha256";',
            'internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.sha256";',
            "missing required preview-download contract: internal const string PackageChecksumAssetName",
        )

    def test_cached_package_size_gate_cannot_be_bypassed(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (existingLength <= MaxPackageBytes)",
            "if (true)",
            "missing required preview-download contract: if (existingLength <= MaxPackageBytes)",
        )

    def test_network_package_bound_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "await DownloadBoundedAsync(release.PackageUri, partialPath, MaxPackageBytes)",
            "await DownloadBoundedAsync(release.PackageUri, partialPath, long.MaxValue)",
            "missing required preview-download contract: await DownloadBoundedAsync(release.PackageUri, partialPath, MaxPackageBytes)",
        )

    def test_checksum_size_bound_cannot_be_widened(self):
        self.assert_rejected(
            DOWNLOADER,
            "private const int MaxChecksumBytes = 64 * 1024;",
            "private const int MaxChecksumBytes = int.MaxValue;",
            "missing required preview-download contract: private const int MaxChecksumBytes = 64 * 1024;",
        )

    def test_redirect_count_bound_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "private const int MaxRedirects = 8;",
            "private const int MaxRedirects = int.MaxValue;",
            "missing required preview-download contract: private const int MaxRedirects = 8;",
        )

    def test_automatic_redirects_cannot_be_reenabled(self):
        self.assert_rejected(
            DOWNLOADER,
            "request.AllowAutoRedirect = false;",
            "request.AllowAutoRedirect = true;",
            "missing required preview-download contract: request.AllowAutoRedirect = false;",
        )

    def test_each_redirect_hop_must_remain_host_validated(self):
        self.assert_rejected(
            DOWNLOADER,
            "                    EnsureAllowedUri(nextUri);\n                    current = nextUri;",
            "                    current = nextUri;",
            "missing required preview-download contract: EnsureAllowedUri(nextUri);",
        )

    def test_https_only_requirement_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))",
            "if (false)",
            "missing required preview-download contract: if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))",
        )

    def test_github_com_allowlist_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)',
            "false",
            'missing required preview-download contract: string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)',
        )

    def test_api_github_com_allowlist_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'string.Equals(host, "api.github.com", StringComparison.OrdinalIgnoreCase)',
            "false",
            'missing required preview-download contract: string.Equals(host, "api.github.com", StringComparison.OrdinalIgnoreCase)',
        )

    def test_githubusercontent_allowlist_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)',
            "false",
            'missing required preview-download contract: host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)',
        )

    def test_checksum_comparison_cannot_be_bypassed(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))",
            "if (false)",
            "missing required preview-download contract: if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))",
        )

    def test_checksum_digest_boundary_must_remain_strict(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))",
            "if (false)",
            "missing required preview-download contract: if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))",
        )

    def test_partial_download_cleanup_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "TryDelete(partialPath);",
            "// TryDelete(partialPath);",
            "missing required preview-download contract: TryDelete(partialPath);",
        )

    def test_verified_partial_must_be_promoted_to_final_path(self):
        self.assert_rejected(
            DOWNLOADER,
            "File.Move(partialPath, packagePath);",
            "// File.Move(partialPath, packagePath);",
            "missing required preview-download contract: File.Move(partialPath, packagePath);",
        )

    def test_windows_reserved_cache_segment_escape_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'if (IsWindowsReservedPathSegment(result)) result = "_" + result;',
            "if (false) result = \"_\" + result;",
            'missing required preview-download contract: if (IsWindowsReservedPathSegment(result)) result = "_" + result;',
        )

    def test_release_tag_cache_identity_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'return result + "~" + ComputeTagIdentity(exactTag);',
            "return result;",
            'missing required preview-download contract: return result + "~" + ComputeTagIdentity(exactTag);',
        )

    def test_release_tag_prefix_bound_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (result.Length > MaxReleaseTagPrefixChars)",
            "if (false)",
            "missing required preview-download contract: if (result.Length > MaxReleaseTagPrefixChars)",
        )

    def test_release_tag_identity_must_hash_exact_tag(self):
        self.assert_rejected(
            DOWNLOADER,
            "var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);",
            "var bytes = Encoding.UTF8.GetBytes((value ?? string.Empty).ToLowerInvariant());",
            "missing required preview-download contract: var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);",
        )

    def test_unsigned_preview_cannot_be_changed_to_direct_execution(self):
        self.assert_rejected(
            WINDOW,
            "namespace QS3D.BricsCAD.V25.Updates",
            "// Process.Start(verified.Path);\nnamespace QS3D.BricsCAD.V25.Updates",
            "contains forbidden preview-download behavior: Process.Start(verified.Path",
        )

    def test_signed_manifest_scheduling_path_must_remain_present(self):
        self.assert_rejected(
            WINDOW,
            "await ScheduleUpdateAsync();",
            "// await ScheduleUpdateAsync();",
            "missing required preview-download contract: await ScheduleUpdateAsync();",
        )

    def test_start_center_update_entry_cannot_silently_disappear(self):
        self.assert_rejected(
            START,
            'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())',
            'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => { })',
            'missing required preview-download contract: CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())',
        )

    def test_start_center_must_keep_updates_namespace_import(self):
        self.assert_rejected(
            START,
            "using QS3D.BricsCAD.V25.Updates;",
            "// using QS3D.BricsCAD.V25.Updates;",
            "missing required preview-download contract: using QS3D.BricsCAD.V25.Updates;",
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)
