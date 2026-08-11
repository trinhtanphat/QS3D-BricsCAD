#!/usr/bin/env python3
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

    # Preserve the existing pinned endpoint/security marker while extending it with page=N.
    require(client, 'ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=20"', "pinned first-page endpoint")
    require(client, "private const int MaxReleasePages = 10;", "hard release-page scan bound")
    require(client, "for (var pageNumber = 1; pageNumber <= MaxReleasePages; pageNumber++)", "sequential bounded page scan")
    require(client, 'ReleasesEndpoint + "&page=" + pageNumber.ToString', "explicit GitHub page addressing")
    require(client, 'response.Headers["Link"]', "GitHub pagination Link inspection")
    require(client, 'link.IndexOf("rel=\\\"next\\\"", StringComparison.OrdinalIgnoreCase)', "rel=next detection")
    require(client, "if (!page.HasNext) return result;", "early stop when history is complete")
    require(client, "if (pageNumber == MaxReleasePages)", "scan ceiling branch")
    require(client, "GitHub Releases history exceeds the bounded updater scan window", "fail-closed incomplete-history behavior")

    # Every page must retain the original response-size/network/JSON guards.
    require(client, "if (response.ContentLength > MaxResponseBytes)", "per-page declared-size bound")
    require(client, "CopyBoundedAsync(source, buffer, MaxResponseBytes)", "per-page streaming-size bound")
    require(client, 'request.Accept = "application/vnd.github+json"', "GitHub JSON accept header")
    require(client, 'request.UserAgent = "QS3D-BricsCAD-V25-Updater"', "explicit updater user agent")
    require(client, "DataContractJsonSerializer(typeof(GitHubReleaseDto[]))", "bounded DTO JSON parsing")

    # Keep release trust/filtering contracts while scanning a wider history window.
    require(client, "release.Prerelease != version.IsPrerelease", "GitHub/tag prerelease consistency")
    require(client, 'candidate.Host, "github.com"', "GitHub page/asset host allowlist")
    require(client, 'UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json"', "signed manifest asset contract")

    # Avoid unbounded/burst fan-out implementations.
    reject(client, "while (true)", "unbounded page loop")
    reject(client, "Task.WhenAll", "parallel release-page burst")

    print("PASS: GitHub release discovery scans bounded sequential pages and fails closed on an incomplete history window.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
