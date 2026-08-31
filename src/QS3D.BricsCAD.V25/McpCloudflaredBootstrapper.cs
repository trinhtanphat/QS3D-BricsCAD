using System;
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
        private static readonly object Sync = new object();
        private static bool _installing;

        public static string ManagedPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QS3D", "MCP", "bin", "cloudflared.exe");

        public static bool IsInstalling { get { lock (Sync) return _installing; } }

        /// <summary>
        /// Starts one managed cloudflared download/install. Returns false when another install is
        /// already active; that busy condition is informational and never invokes the completion
        /// callback as a synthetic failure.
        /// </summary>
        public static bool BeginInstall(Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            lock (Sync)
            {
                if (_installing) return false;
                _installing = true;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool ok;
                string message;
                try { ok = Install(out message); }
                catch (Exception ex) { ok = false; message = "Không cài được Cloudflare Tunnel: " + ex.Message; }
                finally { lock (Sync) _installing = false; }
                try { completed(ok, message); } catch { }
            });
            return true;
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
            message = "Cloudflare Tunnel sẵn sàng; signer=" + signer + ".";
            return true;
        }

        private static bool Install(out string message)
        {
            message = string.Empty;
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
                string downloadError;
                if (!DownloadWithRetry(temporary, out downloadError))
                {
                    string adopted;
                    if (AdoptExistingManagedBinary(out adopted))
                    {
                        message = "Không tải được bản cloudflared mới sau " + MaxDownloadAttempts + " lần thử: " + downloadError
                                  + " Bản đã xác minh hiện có vẫn được giữ nguyên và tiếp tục dùng được. " + adopted;
                        return false;
                    }
                    message = "Không tải được Cloudflare Tunnel sau " + MaxDownloadAttempts + " lần thử: " + downloadError;
                    return false;
                }

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

                string version;
                try { version = FileVersionInfo.GetVersionInfo(destination).FileVersion ?? string.Empty; }
                catch { version = string.Empty; }
                message = "Cloudflare Tunnel đã cài và xác minh thành công"
                          + (string.IsNullOrWhiteSpace(version) ? string.Empty : "; version=" + version)
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
                try
                {
                    try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
#pragma warning disable SYSLIB0014
                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "QS3D-BricsCAD-MCP/2";
                        client.Headers[HttpRequestHeader.Accept] = "application/octet-stream";
                        client.DownloadFile(DownloadUrl, targetPath);
                    }
#pragma warning restore SYSLIB0014
                    return true;
                }
                catch (Exception ex)
                {
                    last = ex;
                    try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                    if (attempt < MaxDownloadAttempts) Thread.Sleep(RetryDelayMilliseconds * attempt);
                }
            }

            error = last == null ? "lỗi mạng không xác định." : last.GetType().Name + ": " + last.Message;
            return false;
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
