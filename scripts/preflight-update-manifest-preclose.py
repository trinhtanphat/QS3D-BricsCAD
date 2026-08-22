#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATES = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates"
PROBE = UPDATES / "UpdateManifestProbe.cs"
COORDINATOR = UPDATES / "UpdateCoordinator.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing updater source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    probe = read(PROBE)
    coordinator = read(COORDINATOR)

    require(probe, "private const int MaxManifestBytes = 64 * 1024;", "64 KiB manifest response bound")
    require(probe, 'RepositoryReleasePathPrefix = "/trinhtanphat/QS3D-BricsCAD/releases/download/"', "pinned release-download repository path")
    require(probe, 'request.UserAgent = "QS3D-BricsCAD-V25-Updater"', "updater User-Agent")
    require(probe, "CopyBoundedAsync(source, buffer, MaxManifestBytes)", "streaming manifest byte bound")
    require(probe, "manifest.SchemaVersion != 2", "schema 2 requirement")
    require(probe, 'manifest.Product, Product, StringComparison.Ordinal', "QS3D product binding")
    require(probe, 'manifest.Target, Target, StringComparison.Ordinal', "V25 target binding")
    require(probe, 'release.Tag.StartsWith("v", StringComparison.Ordinal)', "release-tag productVersion normalization")
    require(probe, "SemanticReleaseVersion.TryParse(productVersion", "strict manifest product SemVer parse")
    require(probe, "productVersion, expectedProductVersion, StringComparison.Ordinal", "exact productVersion/release-tag binding")
    require(probe, "assemblyVersion.Major != release.Version.Major", "assembly major binding")
    require(probe, "assemblyVersion.Minor != release.Version.Minor", "assembly minor binding")
    require(probe, "assemblyVersion.Build != release.Version.Patch", "assembly patch binding")
    require(probe, "ThumbprintPattern.IsMatch(signer)", "manifest signer format validation")
    require(probe, "signer, expectedSigner, StringComparison.Ordinal", "manifest signer running-publisher binding")
    require(probe, "Sha256Pattern.IsMatch(sha256)", "manifest SHA-256 format validation")
    require(probe, 'IsExpectedReleaseAssetUri(packageUri, release.Tag, "QS3D-BricsCAD-V25.zip")', "exact package repo/tag/asset URL binding")
    require(probe, "!string.IsNullOrEmpty(uri.Query)", "release URL query rejection")
    require(probe, "!string.IsNullOrEmpty(uri.Fragment)", "release URL fragment rejection")
    require(probe, "public string? ProductVersion { get; set; }", "nullable untrusted manifest productVersion")
    require(probe, "public string? SignerThumbprint { get; set; }", "nullable untrusted manifest signer")
    reject(probe, "#nullable disable", "nullable suppression")
    reject(probe, "null!", "nullable suppression")

    require(coordinator, "private readonly UpdateManifestProbe _manifestProbe = new UpdateManifestProbe();", "manifest probe coordinator dependency")
    require(coordinator, "TryGetCurrentSignerThumbprint(out var signerThumbprint", "verified running signer snapshot")
    require(coordinator, "await _manifestProbe.ValidateAsync(latest, signerThumbprint)", "pre-close manifest validation")
    require(coordinator, "if (!manifestProbe.IsEligible)", "manifest rejection branch")
    require(coordinator, "UpdateState.ManualInstallRequired", "manual fallback state")
    require(coordinator, "Signed update manifest đã được xác minh trước khi đóng BricsCAD", "eligible pre-close state detail")

    signer_pos = coordinator.find("TryGetCurrentSignerThumbprint(out var signerThumbprint")
    probe_pos = coordinator.find("await _manifestProbe.ValidateAsync(latest, signerThumbprint)")
    available_detail_pos = coordinator.find("Signed update manifest đã được xác minh trước khi đóng BricsCAD")
    if signer_pos < 0 or probe_pos < 0 or available_detail_pos < 0 or not (signer_pos < probe_pos < available_detail_pos):
        raise AssertionError("running signer verification -> manifest probe -> UpdateAvailable path must be ordered before one-click eligibility")

    # Scheduling must still re-run CheckAsync(false), then use the lifecycle-linearized side effect.
    schedule_start = coordinator.find("internal async Task<UpdateCheckResult> ScheduleLatestAsync()")
    check_pos = coordinator.find("await CheckAsync(false)", schedule_start)
    authorize_pos = coordinator.find("TryScheduleCurrentGeneration(generation, release", schedule_start)
    if schedule_start < 0 or check_pos < 0 or authorize_pos < 0 or check_pos >= authorize_pos:
        raise AssertionError("one-click scheduling must perform a fresh checked/probed result before lifecycle-authorized updater launch")

    print("PASS: one-click eligibility validates the bounded schema-v2 GitHub manifest against release identity and the verified running publisher before BricsCAD can be scheduled to close.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
