using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateManifestProbeResult
    {
        private UpdateManifestProbeResult(bool isEligible, string detail)
        {
            IsEligible = isEligible;
            Detail = detail ?? string.Empty;
        }

        internal bool IsEligible { get; }
        internal string Detail { get; }

        internal static UpdateManifestProbeResult Eligible()
        {
            return new UpdateManifestProbeResult(true, string.Empty);
        }

        internal static UpdateManifestProbeResult Rejected(string detail)
        {
            return new UpdateManifestProbeResult(false, detail);
        }
    }

    internal sealed class UpdateManifestProbe
    {
        private const int MaxManifestBytes = 64 * 1024;
        private const string Product = "QS3D";
        private const string Target = "BricsCAD V25 x64";
        private const string RepositoryReleasePathPrefix = "/trinhtanphat/QS3D-BricsCAD/releases/download/";
        private const string ManifestProbeFailure = "Không xác minh được update manifest trước khi đóng BricsCAD. Kiểm tra kết nối mạng và thử lại; auto-update vẫn bị chặn.";
        private static readonly Regex Sha256Pattern = new Regex("^[0-9A-Fa-f]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ThumbprintPattern = new Regex("^[0-9A-Fa-f]{40}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal async Task<UpdateManifestProbeResult> ValidateAsync(UpdateReleaseInfo release, string expectedSignerThumbprint)
        {
            if (release == null) return UpdateManifestProbeResult.Rejected("Release cập nhật không hợp lệ.");
            var manifestUri = release.ManifestUri;
            if (manifestUri == null) return UpdateManifestProbeResult.Rejected("Release không có update manifest.");
            if (!IsExpectedReleaseAssetUri(manifestUri, release.Tag, GitHubReleaseClient.UpdateManifestAssetName))
                return UpdateManifestProbeResult.Rejected("Update manifest không thuộc đúng repository/tag/asset GitHub đã chọn.");

            var expectedSigner = NormalizeThumbprint(expectedSignerThumbprint);
            if (!ThumbprintPattern.IsMatch(expectedSigner))
                return UpdateManifestProbeResult.Rejected("Publisher trust anchor của QS3D không hợp lệ.");

            try
            {
                var request = WebRequest.CreateHttp(manifestUri);
                request.Method = "GET";
                request.Accept = "application/json";
                request.UserAgent = "QS3D-BricsCAD-V25-Updater";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.AllowAutoRedirect = true;
                request.MaximumAutomaticRedirections = 5;
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        return UpdateManifestProbeResult.Rejected("GitHub update manifest trả HTTP " + (int)response.StatusCode + ".");
                    if (response.ContentLength > MaxManifestBytes)
                        return UpdateManifestProbeResult.Rejected("Update manifest vượt quá giới hạn 64 KiB.");

                    using (var source = response.GetResponseStream())
                    using (var buffer = new MemoryStream())
                    {
                        await CopyBoundedAsync(source, buffer, MaxManifestBytes).ConfigureAwait(false);
                        buffer.Position = 0;
                        var serializer = new DataContractJsonSerializer(typeof(UpdateManifestDto));
                        var manifest = serializer.ReadObject(buffer) as UpdateManifestDto;
                        if (manifest == null)
                            return UpdateManifestProbeResult.Rejected("Update manifest không đọc được.");
                        return ValidateManifest(release, manifest, expectedSigner);
                    }
                }
            }
            catch (Exception)
            {
                return UpdateManifestProbeResult.Rejected(ManifestProbeFailure);
            }
        }

        private static UpdateManifestProbeResult ValidateManifest(UpdateReleaseInfo release, UpdateManifestDto manifest, string expectedSigner)
        {
            if (manifest.SchemaVersion != 2)
                return UpdateManifestProbeResult.Rejected("Update manifest không dùng schemaVersion 2.");
            if (!string.Equals(manifest.Product, Product, StringComparison.Ordinal))
                return UpdateManifestProbeResult.Rejected("Update manifest không dành cho QS3D.");
            if (!string.Equals(manifest.Target, Target, StringComparison.Ordinal))
                return UpdateManifestProbeResult.Rejected("Update manifest không dành cho BricsCAD V25 x64.");

            var expectedProductVersion = release.Tag.StartsWith("v", StringComparison.Ordinal) ? release.Tag.Substring(1) : release.Tag;
            var productVersion = manifest.ProductVersion?.Trim();
            if (productVersion == null || productVersion.Length == 0 || productVersion.StartsWith("v", StringComparison.Ordinal))
                return UpdateManifestProbeResult.Rejected("Update manifest productVersion không phải QS3D SemVer chuẩn.");
            if (!SemanticReleaseVersion.TryParse(productVersion, out var parsedProductVersion) || parsedProductVersion == null)
                return UpdateManifestProbeResult.Rejected("Update manifest productVersion không phải SemVer hợp lệ.");
            if (!string.Equals(productVersion, expectedProductVersion, StringComparison.Ordinal))
                return UpdateManifestProbeResult.Rejected("Update manifest productVersion không khớp release tag đã chọn.");
            if (parsedProductVersion.CompareTo(release.Version) != 0)
                return UpdateManifestProbeResult.Rejected("Update manifest productVersion không khớp release SemVer đã chọn.");

            var assemblyVersionText = manifest.Version?.Trim();
            if (string.IsNullOrEmpty(assemblyVersionText) || !Version.TryParse(assemblyVersionText, out var assemblyVersion) || assemblyVersion == null || assemblyVersion.Major != release.Version.Major || assemblyVersion.Minor != release.Version.Minor || assemblyVersion.Build != release.Version.Patch)
                return UpdateManifestProbeResult.Rejected("Update manifest assembly version không khớp release core version.");

            var signer = NormalizeThumbprint(manifest.SignerThumbprint);
            if (!ThumbprintPattern.IsMatch(signer) || !string.Equals(signer, expectedSigner, StringComparison.Ordinal))
                return UpdateManifestProbeResult.Rejected("Update manifest publisher không khớp QS3D đang chạy.");

            var sha256 = manifest.Sha256?.Trim();
            if (string.IsNullOrEmpty(sha256) || !Sha256Pattern.IsMatch(sha256))
                return UpdateManifestProbeResult.Rejected("Update manifest SHA-256 không hợp lệ.");

            var packageUriText = manifest.PackageUri?.Trim();
            if (string.IsNullOrEmpty(packageUriText) || !Uri.TryCreate(packageUriText, UriKind.Absolute, out var packageUri) || packageUri == null || !IsExpectedReleaseAssetUri(packageUri, release.Tag, "QS3D-BricsCAD-V25.zip"))
                return UpdateManifestProbeResult.Rejected("Update package URL không thuộc đúng repository/tag/asset GitHub đã chọn.");

            return UpdateManifestProbeResult.Eligible();
        }

        private static bool IsExpectedReleaseAssetUri(Uri uri, string tag, string assetName)
        {
            if (uri == null || !uri.IsAbsoluteUri) return false;
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;

            var path = uri.AbsolutePath;
            if (!path.StartsWith(RepositoryReleasePathPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            var remainder = path.Substring(RepositoryReleasePathPrefix.Length);
            var slash = remainder.IndexOf('/');
            if (slash <= 0 || slash == remainder.Length - 1) return false;
            if (remainder.IndexOf('/', slash + 1) >= 0) return false;

            string decodedTag;
            string decodedAsset;
            try
            {
                decodedTag = Uri.UnescapeDataString(remainder.Substring(0, slash));
                decodedAsset = Uri.UnescapeDataString(remainder.Substring(slash + 1));
            }
            catch (UriFormatException)
            {
                return false;
            }

            return string.Equals(decodedTag, tag, StringComparison.Ordinal) && string.Equals(decodedAsset, assetName, StringComparison.Ordinal);
        }

        private static string NormalizeThumbprint(string? value)
        {
            return (value ?? string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
        }

        private static async Task CopyBoundedAsync(Stream? input, Stream output, int maxBytes)
        {
            if (input == null) throw new InvalidOperationException("Update manifest response body was empty.");
            var buffer = new byte[8192];
            var total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read <= 0) return;
                total += read;
                if (total > maxBytes) throw new InvalidOperationException("Update manifest exceeded the allowed size.");
                await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            }
        }

        [DataContract]
        private sealed class UpdateManifestDto
        {
            [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
            [DataMember(Name = "product")] public string? Product { get; set; }
            [DataMember(Name = "target")] public string? Target { get; set; }
            [DataMember(Name = "productVersion")] public string? ProductVersion { get; set; }
            [DataMember(Name = "version")] public string? Version { get; set; }
            [DataMember(Name = "packageUri")] public string? PackageUri { get; set; }
            [DataMember(Name = "sha256")] public string? Sha256 { get; set; }
            [DataMember(Name = "signerThumbprint")] public string? SignerThumbprint { get; set; }
        }
    }
}
