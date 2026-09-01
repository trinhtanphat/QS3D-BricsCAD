#!/usr/bin/env python3
# Reservation-v2 scope includes this shared compatibility guard for #4675.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "GitHubReleaseClient.cs"


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
    client = read(CLIENT)

    require(client, 'ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=100"', "pinned high-density first-page endpoint")
    require(client, "private const int MaxResponseBytes = 4 * 1024 * 1024;", "bounded larger per-page response ceiling")
    require(client, "private const int MaxReleasePages = 20;", "hard release-page scan bound")
    require(client, "for (var pageNumber = 1; pageNumber <= MaxReleasePages; pageNumber++)", "sequential bounded page scan")
    require(client, 'ReleasesEndpoint + "&page=" + pageNumber.ToString', "explicit GitHub page addressing")
    require(client, 'response.Headers["Link"]', "GitHub pagination Link inspection")
    require(client, 'link.IndexOf("rel=\\\"next\\\"", StringComparison.OrdinalIgnoreCase)', "rel=next detection")
    require(client, "if (!page.HasNext)", "early stop when history is complete")
    require(client, "if (pageNumber == MaxReleasePages)", "scan ceiling branch")
    require(client, "GitHub Releases history exceeds the bounded updater scan window", "fail-closed incomplete-history behavior")

    require(client, "if (response.ContentLength > MaxResponseBytes)", "per-page declared-size bound")
    require(client, "CopyBoundedAsync(source, buffer, MaxResponseBytes)", "per-page streaming-size bound")
    require(client, 'request.Accept = "application/vnd.github+json"', "GitHub JSON accept header")
    require(client, 'request.UserAgent = "QS3D-BricsCAD-V25-Updater/1.0"', "explicit updater user agent")
    require(client, 'request.Headers["X-GitHub-Api-Version"] = "2022-11-28"', "GitHub API version header")
    require(client, "DataContractJsonSerializer(typeof(GitHubReleaseDto[]))", "bounded DTO JSON parsing")

    require(client, "release.Prerelease != version.IsPrerelease", "GitHub/tag prerelease consistency")
    require(client, 'candidate.Host, "github.com"', "GitHub page/asset host allowlist")
    require(client, 'UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json"', "signed manifest asset contract")

    scan_start = client.find("internal async Task<IReadOnlyList<UpdateReleaseInfo>> GetPublishedReleasesAsync()")
    scan_end = client.find("private static async Task<GitHubReleasePage> GetReleasePageAsync", scan_start)
    if scan_start < 0 or scan_end <= scan_start:
        raise AssertionError("cannot isolate bounded release-page discovery method")
    scan = client[scan_start:scan_end]
    reject(scan, "while (true)", "unbounded release-page loop")
    reject(scan, "Task.WhenAll", "parallel release-page burst")

    require(client, "private static async Task CopyBoundedAsync", "bounded response streaming helper")
    require(client, "if (total > maxBytes)", "streaming byte ceiling")

    print("PASS: GitHub release discovery scans up to 2,000 releases in bounded sequential pages, keeps per-page response limits, and still fails closed on an incomplete history window.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
