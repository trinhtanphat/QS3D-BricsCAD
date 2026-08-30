using System;
using System.Collections.Generic;
using System.Globalization;
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
        internal const string ReleasesEndpoint = "https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=100";
        internal const string UpdateManifestAssetName = "QS3D-BricsCAD-V25.update.json";
        internal const string PackageAssetName = "QS3D-BricsCAD-V25.zip";
        internal const string PackageChecksumAssetName = "QS3D-BricsCAD-V25.zip.sha256";
        private const int MaxResponseBytes = 4 * 1024 * 1024;
        private const int MaxReleasePages = 20;
        private const int MaxRequestAttempts = 3;
        private const int BaseRetryDelayMilliseconds = 500;
        private const int MaxRetryDelayMilliseconds = 3000;
        private static readonly TimeSpan FreshCacheAge = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StaleCacheAge = TimeSpan.FromHours(6);
        private static readonly object CacheSync = new object();
        private static IReadOnlyList<UpdateReleaseInfo>? _cachedPublishedReleases;
        private static DateTime _cachedPublishedReleasesUtc;

        internal async Task<IReadOnlyList<UpdateReleaseInfo>> GetPublishedReleasesAsync()
        {
            if (TryGetFreshCache(out var fresh)) return fresh;

            try
            {
                var result = new List<UpdateReleaseInfo>();
                for (var pageNumber = 1; pageNumber <= MaxReleasePages; pageNumber++)
                {
                    var page = await GetReleasePageAsync(pageNumber).ConfigureAwait(false);
                    result.AddRange(Convert(page.Items));

                    if (!page.HasNext)
                    {
                        var snapshot = (IReadOnlyList<UpdateReleaseInfo>)result.ToArray();
                        SetCache(snapshot);
                        return snapshot;
                    }

                    if (pageNumber == MaxReleasePages)
                    {
                        throw new InvalidOperationException(
                            "GitHub Releases history exceeds the bounded updater scan window. Open the release page manually or increase the reviewed scan bound before relying on automatic latest-version selection.");
                    }
                }
            }
            catch (GitHubRateLimitException error)
            {
                if (TryGetStaleCache(out var stale)) return stale;
                throw new InvalidOperationException(error.Message, error);
            }

            throw new InvalidOperationException("GitHub Releases request loop ended unexpectedly.");
        }

        private static async Task<GitHubReleasePage> GetReleasePageAsync(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > MaxReleasePages)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            var address = pageNumber == 1
                ? ReleasesEndpoint
                : ReleasesEndpoint + "&page=" + pageNumber.ToString(CultureInfo.InvariantCulture);

            for (var attempt = 1; attempt <= MaxRequestAttempts; attempt++)
            {
                try
                {
                    return await GetReleasePageAttemptAsync(address).ConfigureAwait(false);
                }
                catch (WebException error)
                {
                    if (IsRateLimited(error))
                    {
                        var message = DescribeRateLimit(error);
                        error.Response?.Close();
                        throw new GitHubRateLimitException(message, error);
                    }

                    if (!IsTransient(error))
                    {
                        var failure = DescribeFailure(error);
                        error.Response?.Close();
                        throw new InvalidOperationException(
                            "Không lấy được danh sách GitHub Releases (" + failure + "). Hãy bấm \"Kiểm tra lại\" sau ít phút.",
                            error);
                    }

                    var transientFailure = DescribeFailure(error);
                    var retryDelayMilliseconds = GetRetryDelayMilliseconds(error, attempt);
                    error.Response?.Close();

                    if (attempt == MaxRequestAttempts)
                    {
                        throw new InvalidOperationException(
                            "GitHub tạm thời không phản hồi (" + transientFailure + ") sau " + MaxRequestAttempts +
                            " lần thử. Hãy bấm \"Kiểm tra lại\" sau ít phút.",
                            error);
                    }

                    await Task.Delay(retryDelayMilliseconds).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("GitHub Releases request retry loop ended unexpectedly.");
        }

        private static async Task<GitHubReleasePage> GetReleasePageAttemptAsync(string address)
        {
            var request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Accept = "application/vnd.github+json";
            request.UserAgent = "QS3D-BricsCAD-V25-Updater/1.0";
            request.Headers["X-GitHub-Api-Version"] = "2022-11-28";
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

        private static bool IsRateLimited(WebException error)
        {
            var response = error.Response as HttpWebResponse;
            if (response == null) return false;

            if ((int)response.StatusCode == 429) return true;
            if (response.StatusCode != HttpStatusCode.Forbidden) return false;

            var remaining = response.Headers["X-RateLimit-Remaining"];
            var reset = response.Headers["X-RateLimit-Reset"];
            var retryAfter = response.Headers["Retry-After"];
            return string.Equals(remaining, "0", StringComparison.Ordinal)
                   || !string.IsNullOrWhiteSpace(reset)
                   || !string.IsNullOrWhiteSpace(retryAfter)
                   || response.StatusCode == HttpStatusCode.Forbidden;
        }

        private static bool IsTransient(WebException error)
        {
            var response = error.Response as HttpWebResponse;
            if (response != null)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode == 408 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
                    return true;
            }

            switch (error.Status)
            {
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.KeepAliveFailure:
                    return true;
                default:
                    return false;
            }
        }

        private static int GetRetryDelayMilliseconds(WebException error, int attempt)
        {
            var response = error.Response as HttpWebResponse;
            var retryAfter = response?.Headers["Retry-After"];
            if (!string.IsNullOrWhiteSpace(retryAfter) &&
                int.TryParse(retryAfter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retryAfterSeconds) &&
                retryAfterSeconds >= 0)
            {
                var retryAfterMilliseconds = retryAfterSeconds >= MaxRetryDelayMilliseconds / 1000
                    ? MaxRetryDelayMilliseconds
                    : retryAfterSeconds * 1000;
                return Math.Max(BaseRetryDelayMilliseconds, retryAfterMilliseconds);
            }

            return Math.Min(MaxRetryDelayMilliseconds, BaseRetryDelayMilliseconds * attempt);
        }

        private static string DescribeFailure(WebException error)
        {
            var response = error.Response as HttpWebResponse;
            return response != null
                ? "HTTP " + (int)response.StatusCode
                : error.Status.ToString();
        }

        private static string DescribeRateLimit(WebException error)
        {
            var response = error.Response as HttpWebResponse;
            if (response == null)
                return "GitHub đang giới hạn tần suất kiểm tra. Hãy đợi một lúc rồi bấm \"Kiểm tra lại\".";

            var remaining = response.Headers["X-RateLimit-Remaining"];
            var reset = response.Headers["X-RateLimit-Reset"];
            var retryAfter = response.Headers["Retry-After"];
            var hint = DescribeRetryWindow(reset, retryAfter);
            var status = (int)response.StatusCode;
            var remainingText = string.IsNullOrWhiteSpace(remaining) ? string.Empty : " Remaining=" + remaining + ".";
            return "GitHub đang giới hạn tần suất kiểm tra (HTTP " + status + ")." + remainingText + " " + hint +
                   " QS3D sẽ dùng kết quả kiểm tra gần nhất trong phiên nếu còn đủ mới; nếu chưa có cache, hãy thử lại sau.";
        }

        private static string DescribeRetryWindow(string? reset, string? retryAfter)
        {
            if (!string.IsNullOrWhiteSpace(retryAfter) &&
                int.TryParse(retryAfter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
            {
                return "Có thể thử lại sau khoảng " + seconds.ToString(CultureInfo.InvariantCulture) + " giây.";
            }

            if (!string.IsNullOrWhiteSpace(reset) &&
                long.TryParse(reset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochSeconds) && epochSeconds > 0)
            {
                try
                {
                    var retryUtc = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(epochSeconds);
                    return "Có thể thử lại sau " + retryUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture) + ".";
                }
                catch
                {
                }
            }

            return "Hãy đợi một lúc trước khi thử lại.";
        }

        private static bool TryGetFreshCache(out IReadOnlyList<UpdateReleaseInfo> releases)
        {
            lock (CacheSync)
            {
                if (_cachedPublishedReleases != null &&
                    DateTime.UtcNow - _cachedPublishedReleasesUtc <= FreshCacheAge)
                {
                    releases = _cachedPublishedReleases;
                    return true;
                }
            }

            releases = Array.Empty<UpdateReleaseInfo>();
            return false;
        }

        private static bool TryGetStaleCache(out IReadOnlyList<UpdateReleaseInfo> releases)
        {
            lock (CacheSync)
            {
                if (_cachedPublishedReleases != null &&
                    DateTime.UtcNow - _cachedPublishedReleasesUtc <= StaleCacheAge)
                {
                    releases = _cachedPublishedReleases;
                    return true;
                }
            }

            releases = Array.Empty<UpdateReleaseInfo>();
            return false;
        }

        private static void SetCache(IReadOnlyList<UpdateReleaseInfo> releases)
        {
            lock (CacheSync)
            {
                _cachedPublishedReleases = releases ?? Array.Empty<UpdateReleaseInfo>();
                _cachedPublishedReleasesUtc = DateTime.UtcNow;
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
                // coordinator can offer the reviewed SHA-256 preview path without pretending
                // that the preview is a signed commercial auto-update manifest.
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

        private sealed class GitHubRateLimitException : Exception
        {
            internal GitHubRateLimitException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
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
