using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class VerifiedReleaseDownload
    {
        internal VerifiedReleaseDownload(string path, string sha256)
        {
            Path = path;
            Sha256 = sha256;
        }

        internal string Path { get; }
        internal string Sha256 { get; }
    }

    internal sealed class UpdateDownloadProgress
    {
        internal UpdateDownloadProgress(string stage, long bytesReceived, long totalBytes, int percent)
        {
            Stage = stage ?? string.Empty;
            BytesReceived = Math.Max(0, bytesReceived);
            TotalBytes = Math.Max(0, totalBytes);
            Percent = Math.Max(0, Math.Min(100, percent));
        }

        internal string Stage { get; }
        internal long BytesReceived { get; }
        internal long TotalBytes { get; }
        internal int Percent { get; }
    }

    internal sealed class VerifiedReleaseDownloader
    {
        private const long MaxPackageBytes = 256L * 1024L * 1024L;
        private const int MaxChecksumBytes = 64 * 1024;
        private const int NetworkTimeoutMilliseconds = 30000;
        private const int MaxRedirects = 8;
        private const int MaxNetworkAttempts = 3;
        private const int MaxRetryDelayMilliseconds = 3000;
        private const int MaxReleaseTagPrefixChars = 48;

        internal async Task<VerifiedReleaseDownload> DownloadAsync(
            UpdateReleaseInfo release,
            IProgress<UpdateDownloadProgress>? progress = null)
        {
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (release.PackageUri == null || release.PackageChecksumUri == null)
                throw new InvalidOperationException("Release không có đủ package V25 và checksum SHA-256 để tải an toàn.");

            EnsureAllowedUri(release.PackageUri);
            EnsureAllowedUri(release.PackageChecksumUri);

            progress?.Report(new UpdateDownloadProgress("Đang tải checksum SHA-256…", 0, 0, 2));
            var expectedSha256 = await ReadExpectedSha256Async(release.PackageChecksumUri).ConfigureAwait(false);
            progress?.Report(new UpdateDownloadProgress("Đã nhận checksum • chuẩn bị package…", 0, 0, 6));

            var releaseDirectory = GetReleaseDirectory(release.Tag);
            Directory.CreateDirectory(releaseDirectory);

            var packagePath = System.IO.Path.Combine(releaseDirectory, GitHubReleaseClient.PackageAssetName);
            if (File.Exists(packagePath))
            {
                var existingLength = new FileInfo(packagePath).Length;
                if (existingLength <= MaxPackageBytes)
                {
                    progress?.Report(new UpdateDownloadProgress("Đang kiểm tra package đã tải trước đó…", existingLength, existingLength, 88));
                    var existingSha256 = ComputeSha256(packagePath);
                    if (string.Equals(existingSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        progress?.Report(new UpdateDownloadProgress("Package đã có sẵn và SHA-256 hợp lệ.", existingLength, existingLength, 100));
                        return new VerifiedReleaseDownload(packagePath, existingSha256);
                    }
                }

                File.Delete(packagePath);
            }

            var partialPath = packagePath + ".part";
            TryDelete(partialPath);

            try
            {
                await DownloadBoundedAsync(release.PackageUri, partialPath, MaxPackageBytes, progress).ConfigureAwait(false);
                var downloadedLength = new FileInfo(partialPath).Length;
                progress?.Report(new UpdateDownloadProgress("Đang xác minh SHA-256 package…", downloadedLength, downloadedLength, 90));

                var actualSha256 = ComputeSha256(partialPath);
                if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "SHA-256 của package tải về không khớp checksum công bố trên GitHub Release. File tạm đã bị loại bỏ.");

                progress?.Report(new UpdateDownloadProgress("SHA-256 hợp lệ • đang chốt package…", downloadedLength, downloadedLength, 96));
                if (File.Exists(packagePath)) File.Delete(packagePath);
                File.Move(partialPath, packagePath);
                progress?.Report(new UpdateDownloadProgress("Tải và xác minh package hoàn tất.", downloadedLength, downloadedLength, 100));
                return new VerifiedReleaseDownload(packagePath, actualSha256);
            }
            catch
            {
                TryDelete(partialPath);
                throw;
            }
        }

        private static async Task<string> ReadExpectedSha256Async(Uri checksumUri)
        {
            using (var response = await GetResponseFollowingRedirectsAsync(checksumUri).ConfigureAwait(false))
            {
                EnsureSuccessfulResponse(response, MaxChecksumBytes);
                using (var source = response.GetResponseStream())
                using (var buffer = new MemoryStream())
                {
                    await CopyBoundedAsync(source, buffer, MaxChecksumBytes, null, 0).ConfigureAwait(false);
                    var text = Encoding.UTF8.GetString(buffer.ToArray());
                    return ParseSha256(text);
                }
            }
        }

        private static async Task DownloadBoundedAsync(
            Uri packageUri,
            string targetPath,
            long maxBytes,
            IProgress<UpdateDownloadProgress>? progress)
        {
            using (var response = await GetResponseFollowingRedirectsAsync(packageUri).ConfigureAwait(false))
            {
                EnsureSuccessfulResponse(response, maxBytes);
                var totalBytes = response.ContentLength > 0 ? response.ContentLength : 0;
                using (var source = response.GetResponseStream())
                using (var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
                {
                    await CopyBoundedAsync(source, target, maxBytes, progress, totalBytes).ConfigureAwait(false);
                    await target.FlushAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task<HttpWebResponse> GetResponseFollowingRedirectsAsync(Uri uri)
        {
            EnsureAllowedUri(uri);
            InvalidOperationException? lastFailure = null;

            for (var attempt = 1; attempt <= MaxNetworkAttempts; attempt++)
            {
                try
                {
                    return await GetResponseFollowingRedirectsOnceAsync(uri).ConfigureAwait(false);
                }
                catch (WebException error)
                {
                    var retryable = IsRetryableNetworkFailure(error);
                    lastFailure = CreateFriendlyNetworkException(error, uri);
                    error.Response?.Close();

                    if (!retryable || attempt == MaxNetworkAttempts)
                        throw lastFailure;

                    await Task.Delay(GetRetryDelayMilliseconds(error, attempt)).ConfigureAwait(false);
                }
            }

            throw lastFailure ?? new InvalidOperationException("Không thể tải GitHub Release asset.");
        }

        private static async Task<HttpWebResponse> GetResponseFollowingRedirectsOnceAsync(Uri uri)
        {
            var current = uri;
            for (var redirectCount = 0; ; redirectCount++)
            {
                var request = CreateRequest(current);
                var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
                if (!IsRedirect(response.StatusCode)) return response;

                try
                {
                    if (redirectCount >= MaxRedirects)
                        throw new InvalidOperationException("GitHub Release asset vượt quá giới hạn redirect cho phép.");

                    var location = response.Headers[HttpResponseHeader.Location];
                    if (string.IsNullOrWhiteSpace(location))
                        throw new InvalidOperationException("GitHub Release asset trả redirect nhưng không có Location hợp lệ.");

                    Uri nextUri;
                    if (!Uri.TryCreate(current, location, out nextUri) || nextUri == null)
                        throw new InvalidOperationException("GitHub Release asset trả Location không hợp lệ.");

                    EnsureAllowedUri(nextUri);
                    current = nextUri;
                }
                finally
                {
                    response.Dispose();
                }
            }
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            var code = (int)statusCode;
            return code == 301 || code == 302 || code == 303 || code == 307 || code == 308;
        }

        private static HttpWebRequest CreateRequest(Uri uri)
        {
            EnsureAllowedUri(uri);
            var request = WebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.Accept = "application/octet-stream";
            request.UserAgent = "QS3D-BricsCAD-V25-Updater/1.0";
            request.AllowAutoRedirect = false;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = NetworkTimeoutMilliseconds;
            request.ReadWriteTimeout = NetworkTimeoutMilliseconds;
            return request;
        }

        private static bool IsRetryableNetworkFailure(WebException error)
        {
            var response = error.Response as HttpWebResponse;
            if (response != null)
            {
                var status = response.StatusCode;
                if (status == HttpStatusCode.RequestTimeout || (int)status == 429 ||
                    status == HttpStatusCode.InternalServerError || status == HttpStatusCode.BadGateway ||
                    status == HttpStatusCode.ServiceUnavailable || status == HttpStatusCode.GatewayTimeout)
                    return true;

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var remaining = response.Headers["X-RateLimit-Remaining"];
                    if (string.Equals(remaining, "0", StringComparison.Ordinal)) return false;
                    return true;
                }
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
                int.TryParse(retryAfter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
            {
                return Math.Min(MaxRetryDelayMilliseconds, Math.Max(350, seconds * 1000));
            }

            return Math.Min(MaxRetryDelayMilliseconds, 350 * attempt);
        }

        private static InvalidOperationException CreateFriendlyNetworkException(WebException error, Uri requestedUri)
        {
            var response = error.Response as HttpWebResponse;
            if (response == null)
                return new InvalidOperationException("Không thể kết nối GitHub Release (" + error.Status + "). Hãy kiểm tra mạng rồi bấm Kiểm tra lại.", error);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var remaining = response.Headers["X-RateLimit-Remaining"];
                var reset = response.Headers["X-RateLimit-Reset"];
                var retryAfter = response.Headers["Retry-After"];
                var waitHint = DescribeRetryWindow(reset, retryAfter);
                if (string.Equals(remaining, "0", StringComparison.Ordinal) ||
                    !string.IsNullOrWhiteSpace(reset) || !string.IsNullOrWhiteSpace(retryAfter))
                {
                    return new InvalidOperationException(
                        "GitHub đang giới hạn tần suất tải (HTTP 403). " + waitHint +
                        " QS3D sẽ không bỏ qua SHA-256; hãy thử lại sau hoặc mở trang release nếu cần tải thủ công.",
                        error);
                }

                return new InvalidOperationException(
                    "GitHub từ chối tải release asset (HTTP 403) từ " + requestedUri.Host +
                    ". QS3D đã retry có giới hạn nhưng vẫn bị từ chối. Hãy thử lại sau; kiểm tra VPN/proxy/firewall nếu lỗi lặp lại.",
                    error);
            }

            if ((int)response.StatusCode == 429)
            {
                var reset = response.Headers["X-RateLimit-Reset"];
                var retryAfter = response.Headers["Retry-After"];
                return new InvalidOperationException(
                    "GitHub đang giới hạn tần suất tải (HTTP 429). " + DescribeRetryWindow(reset, retryAfter) +
                    " Hãy thử lại sau.", error);
            }

            return new InvalidOperationException(
                "GitHub Release asset trả HTTP " + (int)response.StatusCode + " " + response.StatusDescription + ".",
                error);
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

        private static void EnsureSuccessfulResponse(HttpWebResponse response, long maxBytes)
        {
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("GitHub Release asset returned HTTP " + (int)response.StatusCode + ".");

            EnsureAllowedUri(response.ResponseUri);
            if (response.ContentLength > maxBytes)
                throw new InvalidOperationException("GitHub Release asset vượt quá giới hạn kích thước cho phép.");
        }

        private static void EnsureAllowedUri(Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
                throw new InvalidOperationException("GitHub Release asset URI không hợp lệ.");
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Updater chỉ cho phép tải release asset qua HTTPS.");
            if (!string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("GitHub Release asset URI có user-info không được phép.");

            var host = uri.Host;
            var allowed = string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(host, "api.github.com", StringComparison.OrdinalIgnoreCase)
                          || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                throw new InvalidOperationException("Updater từ chối redirect release asset tới host ngoài GitHub: " + host);
        }

        private static string ParseSha256(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var normalized = value.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            var end = 0;
            while (end < normalized.Length && IsHex(normalized[end])) end++;
            if (end != 64)
                throw new InvalidOperationException("Checksum asset không chứa SHA-256 hợp lệ ở đầu file.");
            if (end < normalized.Length && !char.IsWhiteSpace(normalized[end]))
                throw new InvalidOperationException("Checksum asset chứa ký tự không hợp lệ ngay sau digest SHA-256.");

            var digest = normalized.Substring(0, 64);
            for (var i = 0; i < digest.Length; i++)
            {
                if (!IsHex(digest[i]))
                    throw new InvalidOperationException("Checksum asset chứa digest SHA-256 không hợp lệ.");
            }

            return digest.ToLowerInvariant();
        }

        private static bool IsHex(char value)
        {
            return (value >= '0' && value <= '9')
                   || (value >= 'a' && value <= 'f')
                   || (value >= 'A' && value <= 'F');
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static async Task CopyBoundedAsync(
            Stream? input,
            Stream output,
            long maxBytes,
            IProgress<UpdateDownloadProgress>? progress,
            long totalBytes)
        {
            if (input == null) throw new InvalidOperationException("GitHub Release asset response body was empty.");
            var buffer = new byte[65536];
            long total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read <= 0) return;
                total += read;
                if (total > maxBytes)
                    throw new InvalidOperationException("GitHub Release asset vượt quá giới hạn kích thước cho phép.");
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);

                if (progress != null)
                {
                    var percent = 10;
                    if (totalBytes > 0)
                    {
                        var fraction = Math.Min(1d, (double)total / totalBytes);
                        percent = 10 + (int)Math.Round(fraction * 74d, MidpointRounding.AwayFromZero);
                    }
                    progress?.Report(new UpdateDownloadProgress("Đang tải package từ GitHub…", total, totalBytes, percent));
                }
            }
        }

        private static string GetReleaseDirectory(string tag)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Không xác định được thư mục LocalApplicationData để lưu bản cập nhật.");

            return System.IO.Path.Combine(root, "QS3D", "Updates", "Downloads", ToSafePathSegment(tag));
        }

        private static string ToSafePathSegment(string value)
        {
            var exactTag = value ?? string.Empty;
            var source = string.IsNullOrWhiteSpace(exactTag) ? "release" : exactTag.Trim();
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(source.Length);
            foreach (var character in source)
            {
                var isInvalid = false;
                for (var i = 0; i < invalid.Length; i++)
                {
                    if (character != invalid[i]) continue;
                    isInvalid = true;
                    break;
                }

                builder.Append(isInvalid ? '_' : character);
            }

            var result = builder.ToString().Trim().TrimEnd('.');
            if (result.Length == 0) result = "release";
            if (IsWindowsReservedPathSegment(result)) result = "_" + result;
            if (result.Length > MaxReleaseTagPrefixChars)
                result = result.Substring(0, MaxReleaseTagPrefixChars).TrimEnd(' ', '.');
            if (result.Length == 0) result = "release";
            return result + "~" + ComputeTagIdentity(exactTag);
        }

        private static string ComputeTagIdentity(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var item in hash) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool IsWindowsReservedPathSegment(string value)
        {
            var dotIndex = value.IndexOf('.');
            var stem = (dotIndex >= 0 ? value.Substring(0, dotIndex) : value).TrimEnd(' ');
            if (string.Equals(stem, "CON", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stem, "PRN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stem, "AUX", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stem, "NUL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stem, "CONIN$", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stem, "CONOUT$", StringComparison.OrdinalIgnoreCase))
                return true;

            if (stem.Length != 4) return false;
            var suffix = stem[3];
            if (suffix < '1' || suffix > '9') return false;
            return stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                   || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
