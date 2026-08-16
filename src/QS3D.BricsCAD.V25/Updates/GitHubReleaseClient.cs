using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateReleaseInfo
    {
        internal UpdateReleaseInfo(
            SemanticReleaseVersion version,
            string tag,
            string name,
            bool prerelease,
            DateTime publishedUtc,
            Uri pageUri,
            Uri? manifestUri,
            Uri? packageUri,
            Uri? packageChecksumUri,
            string notes)
        {
            Version = version;
            Tag = tag;
            Name = name;
            IsPrerelease = prerelease;
            PublishedUtc = publishedUtc;
            PageUri = pageUri;
            ManifestUri = manifestUri;
            PackageUri = packageUri;
            PackageChecksumUri = packageChecksumUri;
            Notes = notes;
        }

        internal SemanticReleaseVersion Version { get; }
        internal string Tag { get; }
        internal string Name { get; }
        internal bool IsPrerelease { get; }
        internal DateTime PublishedUtc { get; }
        internal Uri PageUri { get; }
        internal Uri? ManifestUri { get; }
        internal Uri? PackageUri { get; }
        internal Uri? PackageChecksumUri { get; }
        internal string Notes { get; }
        internal bool HasSignedUpdateManifest => ManifestUri != null;
        internal bool HasVerifiedPreviewPackage => PackageUri != null && PackageChecksumUri != null;
    }

    internal sealed class GitHubReleaseClient
    {
        internal const string Repository = "trinhtanphat/QS3D-BricsCAD";
        internal const string ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=20";
        internal const string UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json";
        internal const string PackageAssetName = "QS3D-BricsCAD-V25.zip";
        internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.zip.sha256";
        private const int MaxResponseBytes = 2 * 1024 * 1024;
        private const int MaxReleasePages = 10;

        internal async Task<IReadOnlyList<UpdateReleaseInfo>> GetPublishedReleasesAsync()
        {
            var result = new List<UpdateReleaseInfo>();
            for (var pageNumber = 1; pageNumber <= MaxReleasePages; pageNumber++)
            {
                var page = await GetReleasePageAsync(pageNumber).ConfigureAwait(false);
                result.AddRange(Convert(page.Items));

                if (!page.HasNext) return result;
                if (pageNumber == MaxReleasePages)
                {
                    throw new InvalidOperationException(
                        "GitHub Releases history exceeds the bounded updater scan window. Open the release page manually or increase the reviewed scan bound before relying on automatic latest-version selection.");
                }
            }

            return result;
        }

        private static async Task<GitHubReleasePage> GetReleasePageAsync(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > MaxReleasePages)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            var address = pageNumber == 1
                ? ReleasesEndpoint
                : ReleasesEndpoint + "&page=" + pageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Accept = "application/vnd.github+json";
            request.UserAgent = "QS3D-BricsCAD-V25-Updater";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;

            using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidOperationException("GitHub Releases returned HTTP " + (int)response.StatusCode + ".");

                if (response.ContentLength > MaxResponseBytes)
                    throw new InvalidOperationException("GitHub Releases response exceeded the allowed size.");

                using (var source = response.GetResponseStream())
                using (var buffer = new MemoryStream())
                {
                    await CopyBoundedAsync(source, buffer, MaxResponseBytes).ConfigureAwait(false);
                    buffer.Position = 0;
                    var serializer = new DataContractJsonSerializer(typeof(GitHubReleaseDto[]));
                    var payload = serializer.ReadObject(buffer) as GitHubReleaseDto?[] ?? Array.Empty<GitHubReleaseDto?>();
                    var link = response.Headers["Link"];
                    var hasNext = link != null && link.IndexOf("rel=\"next\"", StringComparison.OrdinalIgnoreCase) >= 0;
                    return new GitHubReleasePage(payload, hasNext);
                }
            }
        }

        private static IReadOnlyList<UpdateReleaseInfo> Convert(IEnumerable<GitHubReleaseDto?>? releases)
        {
            var result = new List<UpdateReleaseInfo>();
            foreach (var release in releases ?? Enumerable.Empty<GitHubReleaseDto?>())
            {
                if (release == null || release.Draft) continue;
                if (!SemanticReleaseVersion.TryParse(release.TagName, out var version) || version == null) continue;
                if (release.Prerelease != version.IsPrerelease) continue;
                if (!TryGitHubUri(release.HtmlUrl, out var pageUri) || pageUri == null) continue;

                var assets = release.Assets ?? Array.Empty<GitHubAssetDto?>();

                Uri? manifestUri = null;
                var manifest = assets.FirstOrDefault(asset =>
                    asset != null && string.Equals(asset.Name, UpdateManifestAssetName, StringComparison.Ordinal));
                if (manifest != null && TryGitHubUri(manifest.BrowserDownloadUrl, out var manifestCandidate) && manifestCandidate != null)
                    manifestUri = manifestCandidate;

                Uri? packageUri = null;
                var package = assets.FirstOrDefault(asset =>
                    asset != null && string.Equals(asset.Name, PackageAssetName, StringComparison.Ordinal));
                if (package != null && TryGitHubUri(package.BrowserDownloadUrl, out var packageCandidate) && packageCandidate != null)
                    packageUri = packageCandidate;

                Uri? packageChecksumUri = null;
                var packageChecksum = assets.FirstOrDefault(asset =>
                    asset != null && string.Equals(asset.Name, PackageChecksumAssetName, StringComparison.Ordinal));
                if (packageChecksum != null && TryGitHubUri(packageChecksum.BrowserDownloadUrl, out var checksumCandidate) && checksumCandidate != null)
                    packageChecksumUri = checksumCandidate;

                // Repository releases are shared by multiple BricsCAD host majors. Keep a
                // release in the V25 channel only when it carries the exact V25 manifest or
                // V25 package asset. Manifest-less V25 previews remain visible so the
                // coordinator can surface ManualInstallRequired instead of silently hiding
                // a newer preview; one-click install still requires a verified signed manifest.
                if (packageUri == null)
                {
                    if (manifestUri == null) continue;
                }

                var publishedUtc = DateTime.MinValue;
                var publishedAt = release.PublishedAt;
                if (publishedAt != null && publishedAt.Trim().Length != 0)
                    DateTime.TryParse(publishedAt, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out publishedUtc);

                var notes = NormalizeNotes(release.Body);
                var tag = release.TagName ?? version.Original;
                var name = tag;
                if (release.Name != null)
                {
                    var candidateName = release.Name.Trim();
                    if (candidateName.Length != 0) name = candidateName;
                }
                result.Add(new UpdateReleaseInfo(
                    version,
                    tag,
                    name,
                    release.Prerelease,
                    publishedUtc,
                    pageUri,
                    manifestUri,
                    packageUri,
                    packageChecksumUri,
                    notes));
            }
            return result;
        }

        private static bool TryGitHubUri(string? value, out Uri? uri)
        {
            uri = null;
            if (value == null) return false;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate)) return false;
            if (candidate == null) return false;
            if (!string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(candidate.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(candidate.UserInfo)) return false;
            uri = candidate;
            return true;
        }

        private static string NormalizeNotes(string? value)
        {
            if (value == null || value.Trim().Length == 0) return "Không có ghi chú phát hành.";
            var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            return normalized.Length <= 1800 ? normalized : normalized.Substring(0, 1800).TrimEnd() + "…";
        }

        private static async Task CopyBoundedAsync(Stream? input, Stream output, int maxBytes)
        {
            if (input == null) throw new InvalidOperationException("GitHub Releases response body was empty.");
            var buffer = new byte[16384];
            var total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read <= 0) return;
                total += read;
                if (total > maxBytes) throw new InvalidOperationException("GitHub Releases response exceeded the allowed size.");
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            }
        }

        private sealed class GitHubReleasePage
        {
            internal GitHubReleasePage(GitHubReleaseDto?[] items, bool hasNext)
            {
                Items = items ?? Array.Empty<GitHubReleaseDto?>();
                HasNext = hasNext;
            }

            internal GitHubReleaseDto?[] Items { get; }
            internal bool HasNext { get; }
        }

        [DataContract]
        private sealed class GitHubReleaseDto
        {
            [DataMember(Name = "tag_name")] public string? TagName { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "draft")] public bool Draft { get; set; }
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
            [DataMember(Name = "published_at")] public string? PublishedAt { get; set; }
            [DataMember(Name = "html_url")] public string? HtmlUrl { get; set; }
            [DataMember(Name = "body")] public string? Body { get; set; }
            [DataMember(Name = "assets")] public GitHubAssetDto?[]? Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAssetDto
        {
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "browser_download_url")] public string? BrowserDownloadUrl { get; set; }
        }
    }
}
