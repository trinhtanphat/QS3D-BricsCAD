using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateReleaseInfo
    {
        internal UpdateReleaseInfo(SemanticReleaseVersion version, string tag, string name, bool prerelease, DateTime publishedUtc, Uri pageUri, Uri manifestUri, string notes)
        {
            Version = version;
            Tag = tag;
            Name = name;
            IsPrerelease = prerelease;
            PublishedUtc = publishedUtc;
            PageUri = pageUri;
            ManifestUri = manifestUri;
            Notes = notes;
        }

        internal SemanticReleaseVersion Version { get; }
        internal string Tag { get; }
        internal string Name { get; }
        internal bool IsPrerelease { get; }
        internal DateTime PublishedUtc { get; }
        internal Uri PageUri { get; }
        internal Uri ManifestUri { get; }
        internal string Notes { get; }
        internal bool HasSignedUpdateManifest => ManifestUri != null;
    }

    internal sealed class GitHubReleaseClient
    {
        internal const string Repository = "trinhtanphat/QS3D-BricsCAD";
        internal const string ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=20";
        internal const string UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json";
        private const int MaxResponseBytes = 2 * 1024 * 1024;

        internal async Task<IReadOnlyList<UpdateReleaseInfo>> GetPublishedReleasesAsync()
        {
            var request = WebRequest.CreateHttp(ReleasesEndpoint);
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
                    var payload = serializer.ReadObject(buffer) as GitHubReleaseDto[] ?? Array.Empty<GitHubReleaseDto>();
                    return Convert(payload);
                }
            }
        }

        private static IReadOnlyList<UpdateReleaseInfo> Convert(IEnumerable<GitHubReleaseDto> releases)
        {
            var result = new List<UpdateReleaseInfo>();
            foreach (var release in releases ?? Enumerable.Empty<GitHubReleaseDto>())
            {
                if (release == null || release.Draft) continue;
                if (!SemanticReleaseVersion.TryParse(release.TagName, out var version)) continue;
                if (!TryGitHubUri(release.HtmlUrl, out var pageUri)) continue;

                Uri manifestUri = null;
                var manifest = (release.Assets ?? Array.Empty<GitHubAssetDto>())
                    .FirstOrDefault(asset => asset != null && string.Equals(asset.Name, UpdateManifestAssetName, StringComparison.Ordinal));
                if (manifest != null && TryGitHubUri(manifest.BrowserDownloadUrl, out var candidate))
                    manifestUri = candidate;

                var publishedUtc = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(release.PublishedAt))
                    DateTime.TryParse(release.PublishedAt, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out publishedUtc);

                var notes = NormalizeNotes(release.Body);
                result.Add(new UpdateReleaseInfo(
                    version,
                    release.TagName ?? version.Original,
                    string.IsNullOrWhiteSpace(release.Name) ? (release.TagName ?? version.Original) : release.Name.Trim(),
                    release.Prerelease,
                    publishedUtc,
                    pageUri,
                    manifestUri,
                    notes));
            }
            return result;
        }

        private static bool TryGitHubUri(string value, out Uri uri)
        {
            uri = null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate)) return false;
            if (!string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(candidate.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(candidate.UserInfo)) return false;
            uri = candidate;
            return true;
        }

        private static string NormalizeNotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Không có ghi chú phát hành.";
            var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            return normalized.Length <= 1800 ? normalized : normalized.Substring(0, 1800).TrimEnd() + "…";
        }

        private static async Task CopyBoundedAsync(Stream input, Stream output, int maxBytes)
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

        [DataContract]
        private sealed class GitHubReleaseDto
        {
            [DataMember(Name = "tag_name")] public string TagName { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "draft")] public bool Draft { get; set; }
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
            [DataMember(Name = "published_at")] public string PublishedAt { get; set; }
            [DataMember(Name = "html_url")] public string HtmlUrl { get; set; }
            [DataMember(Name = "body")] public string Body { get; set; }
            [DataMember(Name = "assets")] public GitHubAssetDto[] Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAssetDto
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "browser_download_url")] public string BrowserDownloadUrl { get; set; }
        }
    }
}