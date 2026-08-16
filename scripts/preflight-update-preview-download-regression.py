#!/usr/bin/env python3
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PREFLIGHT = Path("scripts/preflight-update-preview-download.py")
FIXTURE_FILES = (
    PREFLIGHT,
    Path("src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs"),
    Path("src/QS3D.BricsCAD.V25/Updates/VerifiedReleaseDownloader.cs"),
    Path("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"),
    Path("src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"),
)
DOWNLOADER = Path("src/QS3D.BricsCAD.V25/Updates/VerifiedReleaseDownloader.cs")
WINDOW = Path("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
START_CENTER = Path("src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs")


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

    def test_checksum_size_bound_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "private const int MaxChecksumBytes = 64 * 1024;",
            "private const int MaxChecksumBytes = int.MaxValue;",
            "missing required preview-download contract: private const int MaxChecksumBytes = 64 * 1024;",
        )

    def test_automatic_redirects_cannot_be_reenabled(self):
        self.assert_rejected(
            DOWNLOADER,
            "request.AllowAutoRedirect = false;",
            "request.AllowAutoRedirect = true;",
            "missing required preview-download contract: request.AllowAutoRedirect = false;",
        )

    def test_redirect_hop_bound_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "private const int MaxRedirects = 8;",
            "private const int MaxRedirects = int.MaxValue;",
            "missing required preview-download contract: private const int MaxRedirects = 8;",
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

    def test_githubusercontent_allowlist_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)',
            "false",
            'missing required preview-download contract: host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)',
        )

    def test_checksum_digest_boundary_must_remain_strict(self):
        self.assert_rejected(
            DOWNLOADER,
            "if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))",
            "if (false)",
            "missing required preview-download contract: if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))",
        )

    def test_partial_file_cleanup_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            "TryDelete(partialPath);",
            "// TryDelete(partialPath);",
            "missing required preview-download contract: TryDelete(partialPath);",
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

    def test_windows_reserved_cache_segment_escape_cannot_be_removed(self):
        self.assert_rejected(
            DOWNLOADER,
            'if (IsWindowsReservedPathSegment(result)) result = "_" + result;',
            "// reserved path escape removed",
            'missing required preview-download contract: if (IsWindowsReservedPathSegment(result)) result = "_" + result;',
        )

    def test_local_appdata_staging_cannot_be_redirected_elsewhere(self):
        self.assert_rejected(
            DOWNLOADER,
            'Path.Combine(root, "QS3D", "Updates", "Downloads", ToSafePathSegment(tag))',
            'Path.Combine(root, "QS3D", "Updates", ToSafePathSegment(tag))',
            'missing required preview-download contract: Path.Combine(root, "QS3D", "Updates", "Downloads", ToSafePathSegment(tag))',
        )

    def test_unsigned_preview_cannot_be_changed_to_direct_execution(self):
        self.assert_rejected(
            WINDOW,
            "namespace QS3D.BricsCAD.V25.Updates",
            "// Process.Start(verified.Path);\nnamespace QS3D.BricsCAD.V25.Updates",
            "contains forbidden preview-download behavior: Process.Start(verified.Path",
        )

    def test_start_center_update_entry_cannot_silently_disappear(self):
        self.assert_rejected(
            START_CENTER,
            'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())',
            'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => { })',
            'missing required preview-download contract: CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())',
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)
