using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    internal static class McpCloudflaredBootstrapper
    {
        private const string DownloadUrl =
            "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";
        private const string PathEnvironment = "QS3D_CLOUDFLARED_PATH";
        private const int MaxDownloadAttempts = 3;
        private const int RetryDelayMilliseconds = 750;
        private const int DownloadTimeoutMilliseconds = 120000;
        private const int ReadWriteTimeoutMilliseconds = 30000;
        private const int CancellationDrainMilliseconds = 5000;
        private static readonly object Sync = new object();
        private static bool _installing;
        private static bool _cancelRequested;
        private static bool _lastInstallCancelled;
        private static int _installProgressPercent;
        private static string _installStatus = "Idle";
        private static WebClient? _activeClient;

        private sealed class Candidate
        {
            public Candidate(string path, string source)
            {
                Path = path ?? string.Empty;
                Source = source ?? string.Empty;
            }

            public string Path { get; private set; }
            public string Source { get; private set; }
        }

        private sealed class BoundedWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                request.Timeout = DownloadTimeoutMilliseconds;
                var http = request as HttpWebRequest;
                if (http != null) http.ReadWriteTimeout = ReadWriteTimeoutMilliseconds;
                return request;
            }
        }

        public static string ManagedPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QS3D", "MCP", "bin", "cloudflared.exe");

        public static bool IsInstalling { get { lock (Sync) return _installing; } }
        public static bool WasLastInstallCancelled { get { lock (Sync) return _lastInstallCancelled; } }
        public static int InstallProgressPercent { get { lock (Sync) return _installProgressPercent; } }
        public static string InstallStatus { get { lock (Sync) return _installStatus; } }

        /// <summary>
        /// Starts one bounded cloudflared discovery/download/install. Returns false when another
        /// install is already active; that busy condition is informational and never invokes the
        /// completion callback as a synthetic failure.
        /// </summary>
        public static bool BeginInstall(Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            lock (Sync)
            {
                if (_installing) return false;
                _installing = true;
                _cancelRequested = false;
                _lastInstallCancelled = false;
                _installProgressPercent = 0;
                _installStatus = "Đang kiểm tra cloudflared hiện có...";
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool ok;
                string message;
                try { ok = Install(out message); }
                catch (Exception ex) { ok = false; message = "Không cài được Cloudflare Tunnel: " + ex.Message; }
                finally
                {
                    lock (Sync)
                    {
                        _activeClient = null;
                        _installing = false;
                        if (_cancelRequested) _lastInstallCancelled = true;
                    }
                }
                SetStatus(ok ? 100 : InstallProgressPercent, message);
                try { completed(ok, message); } catch { }
            });
            return true;
        }

        public static bool CancelInstall(out string message)
        {
            WebClient? client;
            lock (Sync)
            {
                if (!_installing)
                {
                    message = "Không có tiến trình cài Cloudflare Tunnel đang chạy.";
                    return false;
                }
                _cancelRequested = true;
                _lastInstallCancelled = true;
                _installStatus = "Đang hủy tải/cài Cloudflare Tunnel...";
                client = _activeClient;
            }
            try { client?.CancelAsync(); } catch { }
            message = "Đã gửi yêu cầu hủy. QS3D sẽ dọn file tạm và giữ nguyên bản cloudflared đang dùng.";
            return true;
        }

        public static bool TryResolveTrustedInstalledBinary(out string path, out string source, out string message)
        {
            return TryResolveTrustedInstalledBinary(true, out path, out source, out message);
        }

        public static bool AdoptExistingManagedBinary(out string message)
        {
            message = string.Empty;
            if (!File.Exists(ManagedPath))
            {
                message = "Chưa có cloudflared do QS3D quản lý.";
                return false;
            }
            string signer;
            string error;
            if (!VerifyCloudflareBinary(ManagedPath, out signer, out error))
            {
                message = "cloudflared hiện có không qua xác minh: " + error;
                return false;
            }
            PersistPath(ManagedPath);
            message = "Cloudflare Tunnel sẵn sàng; source=QS3D managed; signer=" + signer + FormatVersion(ManagedPath) + ".";
            return true;
        }

        public static bool AdoptExistingTrustedBinary(out string message)
        {
            string path;
            string source;
            if (!TryResolveTrustedInstalledBinary(out path, out source, out message)) return false;
            PersistPath(path);
            message = "Cloudflare Tunnel sẵn sàng; source=" + source + "; path=" + path + FormatVersion(path) + ".";
            return true;
        }

        private static bool Install(out string message)
        {
            message = string.Empty;
            if (IsCancellationRequested())
            {
                message = "Đã hủy cài Cloudflare Tunnel trước khi tải.";
                return false;
            }

            // Prefer a trusted WinGet/system installation over downloading a duplicate managed copy.
            string existingPath;
            string existingSource;
            string existingMessage;
            if (TryResolveTrustedInstalledBinary(false, out existingPath, out existingSource, out existingMessage))
            {
                PersistPath(existingPath);
                message = "Đã phát hiện cloudflared đáng tin cậy và dùng lại, không tải bản trùng; source="
                          + existingSource + "; path=" + existingPath + FormatVersion(existingPath) + ".";
                return true;
            }

            var destination = ManagedPath;
            var directory = Path.GetDirectoryName(destination);
            if (string.IsNullOrWhiteSpace(directory))
            {
                message = "Không xác định được thư mục cài Cloudflare Tunnel.";
                return false;
            }
            Directory.CreateDirectory(directory);

            var temporary = destination + ".download-" + Guid.NewGuid().ToString("N");
            var backup = destination + ".previous";
            var backupCreated = false;
            try
            {
                SetStatus(0, "Đang tải cloudflared chính thức...");
                string downloadError;
                if (!DownloadWithRetry(temporary, out downloadError))
                {
                    if (IsCancellationRequested())
                    {
                        message = "Đã hủy tải/cài Cloudflare Tunnel; file tạm sẽ được dọn và bản hiện có không bị thay đổi.";
                        return false;
                    }
                    string adopted;
                    if (AdoptExistingManagedBinary(out adopted))
                    {
                        message = "Không tải được bản cloudflared mới sau " + MaxDownloadAttempts + " lần thử: " + downloadError
                                  + " Bản đã xác minh hiện có vẫn được giữ nguyên và tiếp tục dùng được. " + adopted;
                        return false;
                    }
                    message = "Không tải được Cloudflare Tunnel sau " + MaxDownloadAttempts + " lần thử: " + downloadError
                              + " Có thể cài recovery bằng WinGet: winget install --id Cloudflare.cloudflared";
                    return false;
                }

                if (IsCancellationRequested())
                {
                    message = "Đã hủy cài Cloudflare Tunnel sau khi tải; không thay thế binary hiện có.";
                    return false;
                }

                SetStatus(92, "Đang kiểm tra kích thước và Authenticode của cloudflared...");
                var info = new FileInfo(temporary);
                if (!info.Exists || info.Length < 1024L * 1024L || info.Length > 250L * 1024L * 1024L)
                {
                    message = "File cloudflared tải về có kích thước bất thường; đã hủy cài đặt.";
                    return false;
                }

                string signer;
                string verificationError;
                if (!VerifyCloudflareBinary(temporary, out signer, out verificationError))
                {
                    message = "Cloudflare Tunnel tải về không qua xác minh Authenticode: " + verificationError;
                    return false;
                }

                if (IsCancellationRequested())
                {
                    message = "Đã hủy cài Cloudflare Tunnel trước khi thay binary; bản hiện có vẫn nguyên vẹn.";
                    return false;
                }

                SetStatus(97, "Đang thay binary cloudflared theo cách atomic...");
                try { if (File.Exists(backup)) File.Delete(backup); } catch { }
                if (File.Exists(destination))
                {
                    File.Move(destination, backup);
                    backupCreated = true;
                }

                try
                {
                    File.Move(temporary, destination);
                    temporary = string.Empty;
                }
                catch
                {
                    try
                    {
                        if (!File.Exists(destination) && backupCreated && File.Exists(backup))
                        {
                            File.Move(backup, destination);
                            backupCreated = false;
                        }
                    }
                    catch { }
                    throw;
                }

                PersistPath(destination);
                try
                {
                    if (backupCreated && File.Exists(backup)) File.Delete(backup);
                    backupCreated = false;
                }
                catch { }

                message = "Cloudflare Tunnel đã cài và xác minh thành công"
                          + FormatVersion(destination)
                          + "; signer=" + signer + ".";
                return true;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporary))
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
                }
                if (!File.Exists(destination) && backupCreated && File.Exists(backup))
                {
                    try { File.Move(backup, destination); } catch { }
                }
            }
        }

        private static bool DownloadWithRetry(string targetPath, out string error)
        {
            error = string.Empty;
            Exception? last = null;
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

            for (var attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
            {
                if (IsCancellationRequested())
                {
                    error = "đã hủy bởi người dùng.";
                    return false;
                }

                try
                {
                    try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
#pragma warning disable SYSLIB0014
                    using (var client = new BoundedWebClient())
                    using (var completed = new ManualResetEventSlim(false))
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "QS3D-BricsCAD-MCP/2";
                        client.Headers[HttpRequestHeader.Accept] = "application/octet-stream";
                        AsyncCompletedEventArgs? completedArgs = null;
                        client.DownloadProgressChanged += (_, args) =>
                        {
                            var percent = Math.Max(0, Math.Min(90, (int)Math.Round(args.ProgressPercentage * 0.90)));
                            SetStatus(percent, "Đang tải cloudflared... " + args.ProgressPercentage + "%"
                                + (args.TotalBytesToReceive > 0
                                    ? " · " + FormatBytes(args.BytesReceived) + "/" + FormatBytes(args.TotalBytesToReceive)
                                    : string.Empty));
                        };
                        client.DownloadFileCompleted += (_, args) =>
                        {
                            completedArgs = args;
                            completed.Set();
                        };
                        lock (Sync) _activeClient = client;
                        SetStatus(0, "Đang tải cloudflared · lần " + attempt + "/" + MaxDownloadAttempts + "...");
                        client.DownloadFileAsync(new Uri(DownloadUrl), targetPath);

                        var startedUtc = DateTime.UtcNow;
                        while (!completed.Wait(250))
                        {
                            if (IsCancellationRequested())
                            {
                                try { client.CancelAsync(); } catch { }
                                completed.Wait(CancellationDrainMilliseconds);
                                error = "đã hủy bởi người dùng.";
                                return false;
                            }
                            if ((DateTime.UtcNow - startedUtc).TotalMilliseconds >= DownloadTimeoutMilliseconds)
                            {
                                try { client.CancelAsync(); } catch { }
                                completed.Wait(CancellationDrainMilliseconds);
                                throw new TimeoutException("download cloudflared vượt quá " + (DownloadTimeoutMilliseconds / 1000) + " giây.");
                            }
                        }

                        lock (Sync) if (ReferenceEquals(_activeClient, client)) _activeClient = null;
                        if (completedArgs == null) throw new IOException("download kết thúc nhưng không có completion state.");
                        if (completedArgs.Cancelled)
                        {
                            if (IsCancellationRequested())
                            {
                                error = "đã hủy bởi người dùng.";
                                return false;
                            }
                            throw new OperationCanceledException("download cloudflared bị hủy ngoài dự kiến.");
                        }
                        if (completedArgs.Error != null) throw completedArgs.Error;
                    }
#pragma warning restore SYSLIB0014
                    SetStatus(90, "Đã tải cloudflared; đang xác minh...");
                    return true;
                }
                catch (Exception ex)
                {
                    lock (Sync) _activeClient = null;
                    last = ex;
                    try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                    if (IsCancellationRequested())
                    {
                        error = "đã hủy bởi người dùng.";
                        return false;
                    }
                    if (attempt < MaxDownloadAttempts)
                    {
                        SetStatus(0, "Tải cloudflared lỗi ở lần " + attempt + "; sẽ thử lại...");
                        Thread.Sleep(RetryDelayMilliseconds * attempt);
                    }
                }
            }

            error = last == null ? "lỗi mạng không xác định." : last.GetType().Name + ": " + last.Message;
            return false;
        }

        private static bool TryResolveTrustedInstalledBinary(bool includeManaged, out string path, out string source, out string message)
        {
            path = string.Empty;
            source = string.Empty;
            message = string.Empty;
            var rejected = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in EnumerateCandidates(includeManaged))
            {
                var normalized = NormalizePath(candidate.Path);
                if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized) || !File.Exists(normalized)) continue;
                string signer;
                string error;
                if (!VerifyCloudflareBinary(normalized, out signer, out error))
                {
                    rejected.Add(candidate.Source + ": " + error);
                    continue;
                }
                path = normalized;
                source = candidate.Source;
                message = "cloudflared trusted; source=" + source + "; signer=" + signer + FormatVersion(path) + ".";
                return true;
            }
            message = rejected.Count == 0
                ? "Không tìm thấy cloudflared đã cài."
                : "Có cloudflared nhưng không qua trust verification: " + string.Join(" | ", rejected.ToArray());
            return false;
        }

        private static IEnumerable<Candidate> EnumerateCandidates(bool includeManaged)
        {
            if (includeManaged) yield return new Candidate(ManagedPath, "QS3D managed");

            yield return new Candidate(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Links", "cloudflared.exe"), "WinGet");
            yield return new Candidate(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "cloudflared", "cloudflared.exe"), "Program Files");
            yield return new Candidate(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "cloudflared", "cloudflared.exe"), "Program Files (x86)");

            var explicitPath = (Environment.GetEnvironmentVariable(PathEnvironment) ?? string.Empty).Trim();
            if (!string.Equals(NormalizePath(explicitPath), NormalizePath(ManagedPath), StringComparison.OrdinalIgnoreCase))
                yield return new Candidate(explicitPath, PathEnvironment);

            foreach (var segment in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                var directory = segment.Trim().Trim('"');
                if (directory.Length == 0) continue;
                yield return new Candidate(Path.Combine(directory, "cloudflared.exe"), "PATH");
            }
        }

        private static string NormalizePath(string path)
        {
            try { return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path.Trim().Trim('"')); }
            catch { return string.Empty; }
        }

        private static bool IsCancellationRequested()
        {
            lock (Sync) return _cancelRequested;
        }

        private static void SetStatus(int progressPercent, string status)
        {
            lock (Sync)
            {
                _installProgressPercent = Math.Max(0, Math.Min(100, progressPercent));
                _installStatus = status ?? string.Empty;
            }
        }

        private static string FormatBytes(long value)
        {
            if (value < 1024L) return value + " B";
            if (value < 1024L * 1024L) return (value / 1024d).ToString("0.0") + " KB";
            return (value / (1024d * 1024d)).ToString("0.0") + " MB";
        }

        private static string FormatVersion(string path)
        {
            try
            {
                var version = FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
                return string.IsNullOrWhiteSpace(version) ? string.Empty : "; version=" + version;
            }
            catch { return string.Empty; }
        }

        private static void PersistPath(string path)
        {
            Environment.SetEnvironmentVariable(PathEnvironment, path, EnvironmentVariableTarget.Process);
            try { Environment.SetEnvironmentVariable(PathEnvironment, path, EnvironmentVariableTarget.User); }
            catch { }
        }

        private static bool VerifyCloudflareBinary(string path, out string signer, out string error)
        {
            signer = string.Empty;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "file không tồn tại.";
                return false;
            }

            var trust = VerifyAuthenticode(path);
            if (trust != 0)
            {
                error = "WinVerifyTrust=0x" + trust.ToString("X8") + ".";
                return false;
            }

            try
            {
                using (var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                {
                    signer = certificate.Subject ?? string.Empty;
                    if (signer.IndexOf("Cloudflare", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        error = "signer không phải Cloudflare: " + signer;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = "không đọc được signer certificate: " + ex.Message;
                return false;
            }
            return true;
        }

        private static uint VerifyAuthenticode(string path)
        {
            var fileInfo = new WinTrustFileInfo(path);
            var fileInfoPointer = IntPtr.Zero;
            var dataPointer = IntPtr.Zero;
            try
            {
                fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
                var data = new WinTrustData(fileInfoPointer);
                dataPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
                Marshal.StructureToPtr(data, dataPointer, false);
                var action = WinTrustActionGenericVerifyV2;
                return WinVerifyTrust(IntPtr.Zero, ref action, dataPointer);
            }
            finally
            {
                if (dataPointer != IntPtr.Zero) Marshal.FreeHGlobal(dataPointer);
                if (fileInfoPointer != IntPtr.Zero) Marshal.FreeHGlobal(fileInfoPointer);
            }
        }

        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustFileInfo
        {
            public WinTrustFileInfo(string filePath)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                FilePath = filePath;
                FileHandle = IntPtr.Zero;
                KnownSubject = IntPtr.Zero;
            }
            public uint StructSize;
            [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustData
        {
            public WinTrustData(IntPtr fileInfo)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
                PolicyCallbackData = IntPtr.Zero;
                SipClientData = IntPtr.Zero;
                UiChoice = 2;
                RevocationChecks = 0;
                UnionChoice = 1;
                FileInfoPointer = fileInfo;
                StateAction = 0;
                StateData = IntPtr.Zero;
                UrlReference = IntPtr.Zero;
                ProviderFlags = 0;
                UiContext = 0;
            }
            public uint StructSize;
            public IntPtr PolicyCallbackData;
            public IntPtr SipClientData;
            public uint UiChoice;
            public uint RevocationChecks;
            public uint UnionChoice;
            public IntPtr FileInfoPointer;
            public uint StateAction;
            public IntPtr StateData;
            public IntPtr UrlReference;
            public uint ProviderFlags;
            public uint UiContext;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr trustData);
    }
}
