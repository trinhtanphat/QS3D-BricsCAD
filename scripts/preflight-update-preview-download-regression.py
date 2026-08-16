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

    def mutate_all(self, fixture, relative, old, new):
        path = fixture / relative
        text = path.read_text(encoding="utf-8")
        count = text.count(old)
        self.assertGreater(count, 0, f"mutation anchor missing in {relative}: {old}")
        mutated = text.replace(old, new)
        self.assertNotIn(old, mutated, f"mutation must remove every guard needle ({count} found): {old}")
        path.write_text(mutated, encoding="utf-8")

    def assert_rejected(self, relative, old, new, expected_fragment=None):
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        self.mutate_all(fixture, relative, old, new)
        result = self.run_guard(fixture)
        self.assertNotEqual(0, result.returncode, result.stdout)
        self.assertIn(expected_fragment or old, result.stdout)

    def test_pristine_current_contract_passes(self):
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        result = self.run_guard(fixture)
        self.assertEqual(0, result.returncode, result.stdout)
        self.assertIn("PASS: V25 preview fallback", result.stdout)

    def test_transport_integrity_and_staging_mutations_are_rejected(self):
        cases = (
            (CLIENT, 'internal const string PackageAssetName = "QS3D-BricsCAD-V25.zip";', 'internal const string PackageAssetName = "QS3D.zip";'),
            (CLIENT, 'internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.zip.sha256";', 'internal const string PackageChecksumAssetName = "QS3D.sha256";'),
            (DOWNLOADER, "if (existingLength <= MaxPackageBytes)", "if (true)"),
            (DOWNLOADER, "await DownloadBoundedAsync(release.PackageUri, partialPath, MaxPackageBytes)", "await DownloadBoundedAsync(release.PackageUri, partialPath, long.MaxValue)"),
            (DOWNLOADER, "private const int MaxChecksumBytes = 64 * 1024;", "private const int MaxChecksumBytes = int.MaxValue;"),
            (DOWNLOADER, "private const int MaxRedirects = 8;", "private const int MaxRedirects = int.MaxValue;"),
            (DOWNLOADER, "request.AllowAutoRedirect = false;", "request.AllowAutoRedirect = true;"),
            (DOWNLOADER, "EnsureAllowedUri(nextUri);", "/* mutation removed redirect-hop validation */"),
            (DOWNLOADER, "if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))", "if (false)"),
            (DOWNLOADER, 'string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)', "false"),
            (DOWNLOADER, 'string.Equals(host, "api.github.com", StringComparison.OrdinalIgnoreCase)', "false"),
            (DOWNLOADER, 'host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)', "false"),
            (DOWNLOADER, "if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))", "if (false)"),
            (DOWNLOADER, "if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))", "if (false)"),
            (DOWNLOADER, "TryDelete(partialPath);", "/* mutation removed partial cleanup */"),
            (DOWNLOADER, "File.Move(partialPath, packagePath);", "/* mutation removed final promotion */"),
            (DOWNLOADER, 'Path.Combine(root, "QS3D", "Updates", "Downloads", ToSafePathSegment(tag))', 'Path.Combine(root, "QS3D", "Updates", ToSafePathSegment(tag))'),
            (DOWNLOADER, 'if (IsWindowsReservedPathSegment(result)) result = "_" + result;', 'if (false) result = "_" + result;'),
            (DOWNLOADER, 'return result + "~" + ComputeTagIdentity(exactTag);', "return result;"),
            (DOWNLOADER, "if (result.Length > MaxReleaseTagPrefixChars)", "if (false)"),
            (DOWNLOADER, "var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);", "var bytes = Encoding.UTF8.GetBytes((value ?? string.Empty).ToLowerInvariant());"),
            (WINDOW, "await ScheduleUpdateAsync();", "/* mutation removed signed scheduling */"),
            (START, 'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show())', 'CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => { })'),
            (START, "using QS3D.BricsCAD.V25.Updates;", "// mutation removed updates namespace import"),
        )
        for index, (relative, old, new) in enumerate(cases, 1):
            with self.subTest(case=index, file=str(relative)):
                self.assert_rejected(relative, old, new)

    def test_forbidden_direct_execution_is_rejected(self):
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        path = fixture / WINDOW
        text = path.read_text(encoding="utf-8")
        anchor = "namespace QS3D.BricsCAD.V25.Updates"
        self.assertIn(anchor, text)
        path.write_text(text.replace(anchor, "// Process.Start(verified.Path);\n" + anchor, 1), encoding="utf-8")
        result = self.run_guard(fixture)
        self.assertNotEqual(0, result.returncode, result.stdout)
        self.assertIn("contains forbidden preview-download behavior: Process.Start(verified.Path", result.stdout)

    def test_fixture_mutation_is_hermetic(self):
        before = {relative: (ROOT / relative).read_bytes() for relative in FIXTURE_FILES}
        temporary, fixture = self.make_fixture()
        self.addCleanup(temporary.cleanup)
        self.mutate_all(fixture, DOWNLOADER, "request.AllowAutoRedirect = false;", "request.AllowAutoRedirect = true;")
        for relative, expected in before.items():
            self.assertEqual(expected, (ROOT / relative).read_bytes(), f"fixture mutation leaked to repository source: {relative}")


if __name__ == "__main__":
    unittest.main(verbosity=2)
