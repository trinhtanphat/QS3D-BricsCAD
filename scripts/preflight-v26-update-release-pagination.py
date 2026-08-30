#!/usr/bin/env python3
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "src" / "QS3D.BricsCAD.V26" / "Updates" / "GitHubReleaseClient.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing V26 updater source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def parse_int_constant(text: str, name: str) -> int:
    match = re.search(rf"private const int {re.escape(name)} = (\d+);", text)
    if not match:
        raise AssertionError(f"cannot parse integer constant: {name}")
    return int(match.group(1))


def parse_per_page(text: str) -> int:
    match = re.search(r"ReleasesEndpoint = \"[^\"]+[?&]per_page=(\d+)\"", text)
    if not match:
        raise AssertionError("cannot parse GitHub Releases per_page value")
    return int(match.group(1))


def simulate_scan(total_releases: int, per_page: int, max_pages: int) -> int:
    if total_releases < 0:
        raise AssertionError("release count must be non-negative")
    pages_needed = max(1, math.ceil(total_releases / per_page))
    for page_number in range(1, max_pages + 1):
        has_next = page_number < pages_needed
        if not has_next:
            return page_number
        if page_number == max_pages:
            raise RuntimeError("bounded release scan exhausted")
    raise AssertionError("simulated bounded scan ended unexpectedly")


def main() -> int:
    client = read(CLIENT)

    require(
        client,
        'ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=100"',
        "high-density GitHub Releases endpoint",
    )
    require(client, "private const int MaxResponseBytes = 4 * 1024 * 1024;", "bounded 4 MiB response ceiling")
    require(client, "private const int MaxReleasePages = 20;", "20-page hard scan bound")
    require(client, "for (var pageNumber = 1; pageNumber <= MaxReleasePages; pageNumber++)", "sequential bounded scan")
    require(client, 'ReleasesEndpoint + "&page=" + pageNumber.ToString', "explicit page addressing")
    require(client, 'response.Headers.TryGetValues("Link"', "GitHub Link pagination inspection")
    require(client, 'rel=\\\"next\\\"', "rel=next detection")
    require(client, "if (!page.HasNext) return result;", "early completion when release history ends")
    require(client, "if (pageNumber == MaxReleasePages)", "scan-ceiling branch")
    require(client, "GitHub Releases history exceeds the bounded V26 updater scan window", "fail-closed overflow diagnostic")

    require(client, "if (contentLength.HasValue && contentLength.Value > MaxResponseBytes)", "declared-size bound")
    require(client, "CopyBoundedAsync(source, buffer, MaxResponseBytes, timeout.Token)", "streaming-size bound")
    require(client, "if (total > maxBytes)", "streaming byte ceiling")
    require(client, 'request.Headers.Accept.ParseAdd("application/vnd.github+json")', "GitHub JSON accept header")
    require(client, 'request.Headers.UserAgent.ParseAdd("QS3D-BricsCAD-V26-Updater")', "V26 updater user agent")
    require(client, 'request.Headers.Add("X-GitHub-Api-Version", "2022-11-28")', "reviewed GitHub API version")

    require(client, 'UpdateManifestAssetName = "QS3D-BricsCAD-V26.update.json"', "V26 signed manifest channel")
    require(client, "if (manifestUri == null) continue;", "manifest-channel isolation")
    require(client, 'candidate.Host, "github.com"', "GitHub host allowlist")
    require(client, "candidate.Scheme, Uri.UriSchemeHttps", "HTTPS-only asset/page URLs")
    reject(client, "QS3D-BricsCAD-V25.update.json", "V25 manifest cross-channel token")
    reject(client, "QS3D-BricsCAD-V25-Updater", "V25 updater user-agent token")

    scan_start = client.find("internal async Task<IReadOnlyList<UpdateReleaseInfo>> GetPublishedReleasesAsync()")
    scan_end = client.find("private static HttpClient CreateHttpClient()", scan_start)
    if scan_start < 0 or scan_end <= scan_start:
        raise AssertionError("cannot isolate V26 bounded release discovery method")
    scan = client[scan_start:scan_end]
    reject(scan, "while (true)", "unbounded release-page loop")
    reject(scan, "Task.WhenAll", "parallel pagination burst")

    per_page = parse_per_page(client)
    max_pages = parse_int_constant(client, "MaxReleasePages")
    capacity = per_page * max_pages
    if capacity != 2000:
        raise AssertionError(f"reviewed V26 scan capacity changed unexpectedly: {capacity}")

    if simulate_scan(201, per_page, max_pages) != 3:
        raise AssertionError("201-release regression case must complete on page 3")
    if simulate_scan(capacity, per_page, max_pages) != max_pages:
        raise AssertionError("exact bounded capacity must complete on the final reviewed page")
    try:
        simulate_scan(capacity + 1, per_page, max_pages)
    except RuntimeError:
        pass
    else:
        raise AssertionError("history beyond the reviewed bound must fail closed")

    print(
        "PASS: V26 GitHub release discovery keeps sequential bounded pagination, "
        "handles 201 and 2,000 releases, and fails closed beyond the 2,000-release review window."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
