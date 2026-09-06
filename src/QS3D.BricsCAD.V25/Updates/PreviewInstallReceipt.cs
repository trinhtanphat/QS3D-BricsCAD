using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class PreviewInstallReceiptInfo
    {
        internal PreviewInstallReceiptInfo(
            int originProcessId,
            long originProcessStartUtcTicks,
            string expectedVersion,
            string expectedAdapterPath)
        {
            OriginProcessId = originProcessId;
            OriginProcessStartUtcTicks = originProcessStartUtcTicks;
            ExpectedVersion = expectedVersion ?? string.Empty;
            ExpectedAdapterPath = expectedAdapterPath ?? string.Empty;
        }

        internal int OriginProcessId { get; }
        internal long OriginProcessStartUtcTicks { get; }
        internal string ExpectedVersion { get; }
        internal string ExpectedAdapterPath { get; }
    }

    internal static class PreviewInstallReceipt
    {
        internal const int MaxReceiptBytes = 32 * 1024;
        private const string ReceiptFileName = "preview-install.receipt";
        private const string FormatMarker = "QS3D_PREVIEW_RECEIPT_V1";

        private static string ReceiptPath
        {
            get
            {
                var localRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localRoot, "QS3D", "UpdateState", ReceiptFileName);
            }
        }

        internal static bool TryWrite(string expectedVersion, string expectedAdapterPath, out string error)
        {
            error = string.Empty;
            string? tempPath = null;
            try
            {
                var normalizedVersion = NormalizeVersion(expectedVersion);
                if (normalizedVersion.Length == 0)
                    throw new InvalidOperationException("Phiên bản dự kiến của updater đang trống.");
                if (string.IsNullOrWhiteSpace(expectedAdapterPath))
                    throw new InvalidOperationException("Đường dẫn DLL dự kiến của updater đang trống.");

                var fullAdapterPath = Path.GetFullPath(expectedAdapterPath);
                using (var process = Process.GetCurrentProcess())
                {
                    var processStartTicks = process.StartTime.ToUniversalTime().Ticks;
                    var payload = new StringBuilder()
                        .AppendLine(FormatMarker)
                        .Append("pid=").AppendLine(process.Id.ToString(CultureInfo.InvariantCulture))
                        .Append("startTicks=").AppendLine(processStartTicks.ToString(CultureInfo.InvariantCulture))
                        .Append("version64=").AppendLine(Encode(normalizedVersion))
                        .Append("path64=").AppendLine(Encode(fullAdapterPath))
                        .ToString();

                    var bytes = Encoding.UTF8.GetBytes(payload);
                    if (bytes.Length <= 0 || bytes.Length > MaxReceiptBytes)
                        throw new InvalidOperationException("Receipt updater vượt giới hạn kích thước an toàn.");

                    var path = ReceiptPath;
                    var directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory))
                        throw new InvalidOperationException("Không xác định được thư mục trạng thái updater.");
                    Directory.CreateDirectory(directory);

                    tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
                    File.WriteAllBytes(tempPath, bytes);
                    if (File.Exists(path))
                        File.Replace(tempPath, path, null, true);
                    else
                        File.Move(tempPath, path);
                    tempPath = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        internal static bool TryRead(out PreviewInstallReceiptInfo? info, out string error)
        {
            info = null;
            error = string.Empty;
            try
            {
                var path = ReceiptPath;
                if (!File.Exists(path)) return true;

                var fileInfo = new FileInfo(path);
                if (fileInfo.Length <= 0 || fileInfo.Length > MaxReceiptBytes)
                    throw new InvalidOperationException("Receipt updater không hợp lệ hoặc vượt giới hạn an toàn.");

                var lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length != 5 || !string.Equals(lines[0], FormatMarker, StringComparison.Ordinal))
                    throw new InvalidOperationException("Receipt updater sai định dạng.");

                var pid = ParseInt(lines[1], "pid=");
                var startTicks = ParseLong(lines[2], "startTicks=");
                var version = NormalizeVersion(DecodeValue(lines[3], "version64="));
                var adapterPath = Path.GetFullPath(DecodeValue(lines[4], "path64="));
                if (pid <= 0 || startTicks <= 0 || version.Length == 0 || adapterPath.Length == 0)
                    throw new InvalidOperationException("Receipt updater thiếu dữ liệu bắt buộc.");

                info = new PreviewInstallReceiptInfo(pid, startTicks, version, adapterPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static bool IsFromCurrentProcess(PreviewInstallReceiptInfo info)
        {
            if (info == null) return false;
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.Id == info.OriginProcessId &&
                           process.StartTime.ToUniversalTime().Ticks == info.OriginProcessStartUtcTicks;
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool MatchesLoadedAssembly(
            PreviewInstallReceiptInfo info,
            string actualVersion,
            string actualAdapterPath)
        {
            if (info == null || string.IsNullOrWhiteSpace(actualAdapterPath)) return false;
            string actualPath;
            try { actualPath = Path.GetFullPath(actualAdapterPath); }
            catch { return false; }

            return string.Equals(
                       NormalizeVersion(info.ExpectedVersion),
                       NormalizeVersion(actualVersion),
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       info.ExpectedAdapterPath,
                       actualPath,
                       StringComparison.OrdinalIgnoreCase);
        }

        internal static string DescribeMismatch(
            PreviewInstallReceiptInfo info,
            string actualVersion,
            string actualAdapterPath)
        {
            var actual = NormalizeVersion(actualVersion);
            if (actual.Length == 0) actual = "<không xác định>";
            var loadedPath = string.IsNullOrWhiteSpace(actualAdapterPath) ? "<không xác định>" : actualAdapterPath;
            return "Đã yêu cầu " + info.ExpectedVersion +
                   " nhưng BricsCAD đang load " + actual +
                   ". DLL đang load: " + loadedPath +
                   ". Đích updater: " + info.ExpectedAdapterPath +
                   ". Hãy kiểm tra bản cài trùng hoặc đường dẫn autoload cũ.";
        }

        internal static string NormalizeVersion(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);
            var buildMetadata = normalized.IndexOf('+');
            if (buildMetadata >= 0)
                normalized = normalized.Substring(0, buildMetadata);
            return normalized.Trim();
        }

        internal static bool TryDelete()
        {
            try
            {
                var path = ReceiptPath;
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string DecodeValue(string line, string prefix)
        {
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Receipt updater thiếu trường " + prefix.TrimEnd('='));
            var encoded = line.Substring(prefix.Length);
            var bytes = Convert.FromBase64String(encoded);
            if (bytes.Length > MaxReceiptBytes)
                throw new InvalidOperationException("Trường receipt updater vượt giới hạn an toàn.");
            return Encoding.UTF8.GetString(bytes);
        }

        private static int ParseInt(string line, string prefix)
        {
            var value = ParseRaw(line, prefix);
            int parsed;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidOperationException("Receipt updater có PID không hợp lệ.");
            return parsed;
        }

        private static long ParseLong(string line, string prefix)
        {
            var value = ParseRaw(line, prefix);
            long parsed;
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidOperationException("Receipt updater có process start không hợp lệ.");
            return parsed;
        }

        private static string ParseRaw(string line, string prefix)
        {
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Receipt updater thiếu trường " + prefix.TrimEnd('='));
            return line.Substring(prefix.Length).Trim();
        }
    }
}
