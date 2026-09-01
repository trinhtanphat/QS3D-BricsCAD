#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UPDATES = ROOT / "src/QS3D.BricsCAD.V25/Updates"
FILES = {
    "commands": UPDATES / "UpdateCommands.cs",
    "preferences": UPDATES / "UpdatePreferences.cs",
    "coordinator": UPDATES / "UpdateCoordinator.cs",
    "manifest": UPDATES / "UpdateManifestProbe.cs",
}
errors = []
texts = {}

for key, path in FILES.items():
    if not path.is_file():
        errors.append("missing updater source: " + str(path.relative_to(ROOT)))
        texts[key] = ""
    else:
        texts[key] = path.read_text(encoding="utf-8")

for key, text in texts.items():
    for forbidden in (
        "ex.Message",
        "+ ex.Message",
        "catch (Exception ex)",
        "GetBaseException()",
        "StackTrace",
    ):
        if forbidden in text:
            errors.append(key + " updater failure surface retains raw exception detail: " + forbidden)

commands = texts["commands"]
for token in (
    'private const string UpdateCenterFailure = "Không thể mở QS3D Update Center. QS3D vẫn tiếp tục hoạt động bình thường.";',
    'private const string VersionFailure = "Không thể đọc thông tin phiên bản QS3D đang chạy.";',
    'ShowUpdateCenterCore("QS3DUPDATE")',
    'ShowUpdateCenterCore("QSUPDATE")',
    'WriteVersionCore("QS3DVER")',
    'WriteVersionCore("QSVER")',
    "UpdateCenterWindowHost.Show();",
    "TryWriteFailure(commandName, UpdateCenterFailure);",
    "TryWriteFailure(commandName, VersionFailure);",
    'document?.Editor.WriteMessage("\\n" + commandName + ": " + message);',
    "catch (Exception)",
):
    if token not in commands:
        errors.append("UpdateCommands redaction/alias/fail-isolation contract missing: " + token)
if commands.count("catch (Exception)") < 3:
    errors.append("UpdateCommands must fail-isolate update-center, version and failure-reporting host boundaries")

preferences = texts["preferences"]
for token in (
    'private const string SavePreferenceFailure = "Không lưu được tùy chọn cập nhật trong Windows Registry. Tùy chọn hiện tại vẫn được giữ nguyên.";',
    "Registry.CurrentUser.CreateSubKey(RegistryPath, true)",
    "key.SetValue(InstallOnExitValue, enabled ? 1 : 0, RegistryValueKind.DWord);",
    "error = SavePreferenceFailure;",
    "return false;",
    "Registry.CurrentUser.OpenSubKey(RegistryPath, false)",
):
    if token not in preferences:
        errors.append("UpdatePreferences persistence/redaction contract missing: " + token)
if "catch (Exception)" not in preferences:
    errors.append("UpdatePreferences write failure must use a non-binding Exception catch")

coordinator = texts["coordinator"]
for token in (
    'private const string CheckFailureDetail = "Không đọc được dữ liệu release từ GitHub. Kiểm tra kết nối mạng rồi thử lại; QS3D không thay đổi bản đang chạy.";',
    "_generation++;",
    "_inFlightGeneration = generation;",
    "var releases = await _client.GetPublishedReleasesAsync().ConfigureAwait(false);",
    ".Where(release => current.IsPrerelease || !release.IsPrerelease)",
    "SecureUpdateLauncher.TryGetCurrentSignerThumbprint",
    "_manifestProbe.ValidateAsync(latest, signerThumbprint)",
    "TryPublishCurrent(generation, result, automatic && result.HasUpdate);",
    'new UpdateCheckResult(UpdateState.Error, current, null, "Không kiểm tra được cập nhật. QS3D vẫn tiếp tục hoạt động bình thường.", CheckFailureDetail)',
    "if (!_started || generation != _generation) return false;",
):
    if token not in coordinator:
        errors.append("UpdateCoordinator lifecycle/security/redaction contract missing: " + token)
if "catch (Exception)" not in coordinator:
    errors.append("UpdateCoordinator release-check failure must use a non-binding Exception catch")

manifest = texts["manifest"]
for token in (
    'private const string ManifestProbeFailure = "Không xác minh được update manifest trước khi đóng BricsCAD. Kiểm tra kết nối mạng và thử lại; auto-update vẫn bị chặn.";',
    'private const string RepositoryReleasePathPrefix = "/trinhtanphat/QS3D-BricsCAD/releases/download/";',
    'new Regex("^[0-9A-Fa-f]{64}$"',
    'new Regex("^[0-9A-Fa-f]{40}$"',
    "if (!IsExpectedReleaseAssetUri(manifestUri, release.Tag, GitHubReleaseClient.UpdateManifestAssetName))",
    "request.AllowAutoRedirect = true;",
    "request.MaximumAutomaticRedirections = 5;",
    "CopyBoundedAsync(source, buffer, MaxManifestBytes)",
    "manifest.SchemaVersion != 2",
    "SemanticReleaseVersion.TryParse(productVersion",
    "ThumbprintPattern.IsMatch(signer)",
    "Sha256Pattern.IsMatch(sha256)",
    'IsExpectedReleaseAssetUri(packageUri, release.Tag, "QS3D-BricsCAD-V25.zip")',
    "return UpdateManifestProbeResult.Rejected(ManifestProbeFailure);",
):
    if token not in manifest:
        errors.append("UpdateManifestProbe authenticity/bounds/redaction contract missing: " + token)
if "catch (Exception)" not in manifest:
    errors.append("UpdateManifestProbe network/parser failure must use a non-binding Exception catch")

print("QS3D Update Center failure-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Update Center commands, preferences, release checks and manifest probes preserve updater lifecycle/authenticity gates while redacting caught host/network/parser exception detail.")
