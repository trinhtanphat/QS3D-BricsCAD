using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25.Updates
{
    internal static class SecureUpdateLauncher
    {
        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        private const uint WtdUiNone = 2;
        private const uint WtdRevokeNone = 0;
        private const uint WtdChoiceFile = 1;
        private const uint WtdStateActionVerify = 1;
        private const uint WtdStateActionClose = 2;
        private static int _scheduled;

        internal static bool IsScheduled => Volatile.Read(ref _scheduled) != 0;

        internal static bool TryGetCurrentSignerThumbprint(out string thumbprint, out string reason)
        {
            thumbprint = string.Empty;
            reason = string.Empty;
            try
            {
                var pluginPath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrWhiteSpace(pluginPath) || !File.Exists(pluginPath))
                {
                    reason = "Không xác định được DLL QS3D đang chạy.";
                    return false;
                }

                if (!TryVerifyAuthenticode(pluginPath, out reason)) return false;

                var certificate = X509Certificate.CreateFromSignedFile(pluginPath);
                using (var signer = new X509Certificate2(certificate))
                {
                    var normalized = NormalizeThumbprint(signer.Thumbprint);
                    if (normalized.Length != 40)
                    {
                        reason = "Bản QS3D hiện tại không có publisher Authenticode hợp lệ cho auto-update.";
                        return false;
                    }
                    thumbprint = normalized;
                    return true;
                }
            }
            catch
            {
                reason = "Bản QS3D hiện tại chưa có Authenticode hợp lệ; cần cài thủ công một bản signed hợp lệ trước khi bật one-click update.";
                return false;
            }
        }

        internal static bool TrySchedule(UpdateReleaseInfo release, out string error)
        {
            error = string.Empty;
            if (release == null || release.ManifestUri == null)
            {
                error = "Release này không có signed update manifest.";
                return false;
            }
            if (!string.Equals(release.ManifestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(release.ManifestUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                error = "Update manifest không thuộc host GitHub HTTPS được phép.";
                return false;
            }
            if (!TryGetCurrentSignerThumbprint(out var signerThumbprint, out error)) return false;
            if (Interlocked.CompareExchange(ref _scheduled, 1, 0) != 0)
            {
                error = "Bản cập nhật đã được lên lịch trong phiên này.";
                return false;
            }

            try
            {
                var pluginPath = Assembly.GetExecutingAssembly().Location;
                var installDirectory = Path.GetDirectoryName(pluginPath);
                if (string.IsNullOrWhiteSpace(installDirectory))
                    throw new InvalidOperationException("Không xác định được thư mục cài QS3D.");

                var updaterPath = Path.Combine(installDirectory, "update-v25.ps1");
                if (!File.Exists(updaterPath))
                    throw new FileNotFoundException("Không tìm thấy update-v25.ps1 cạnh plugin QS3D.", updaterPath);

                string bricscadPath;
                using (var process = Process.GetCurrentProcess())
                {
                    bricscadPath = process.MainModule?.FileName;
                }
                if (string.IsNullOrWhiteSpace(bricscadPath) || !File.Exists(bricscadPath))
                    throw new InvalidOperationException("Không xác định được bricscad.exe đang chạy.");

                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QS3D",
                    "UpdateLogs");
                Directory.CreateDirectory(logDirectory);

                var worker = BuildWorkerScript(
                    updaterPath,
                    release.ManifestUri.AbsoluteUri,
                    signerThumbprint,
                    installDirectory,
                    bricscadPath,
                    logDirectory);
                var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(worker));

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -EncodedCommand " + encoded,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                };

                var updater = Process.Start(startInfo);
                if (updater == null) throw new InvalidOperationException("Không thể khởi động tiến trình updater tách rời.");
                updater.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _scheduled, 0);
                error = ex.Message;
                return false;
            }
        }

        internal static bool TryRequestGracefulHostClose(out string error)
        {
            error = string.Empty;
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    if (process.CloseMainWindow()) return true;
                }
                error = "BricsCAD chưa chấp nhận yêu cầu đóng cửa sổ chính. Hãy lưu bản vẽ và đóng BricsCAD bình thường; updater sẽ tiếp tục chờ.";
                return false;
            }
            catch (Exception ex)
            {
                error = "Không gửi được yêu cầu đóng BricsCAD: " + ex.Message + " Hãy đóng BricsCAD bình thường; updater sẽ tiếp tục chờ.";
                return false;
            }
        }

        private static bool TryVerifyAuthenticode(string filePath, out string reason)
        {
            reason = string.Empty;
            var fileInfoPointer = IntPtr.Zero;
            try
            {
                var fileInfo = new WinTrustFileInfo
                {
                    cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                    pcwszFilePath = filePath,
                    hFile = IntPtr.Zero,
                    pgKnownSubject = IntPtr.Zero
                };
                fileInfoPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

                var trustData = new WinTrustData
                {
                    cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustData)),
                    pPolicyCallbackData = IntPtr.Zero,
                    pSIPClientData = IntPtr.Zero,
                    dwUIChoice = WtdUiNone,
                    fdwRevocationChecks = WtdRevokeNone,
                    dwUnionChoice = WtdChoiceFile,
                    pFile = fileInfoPointer,
                    dwStateAction = WtdStateActionVerify,
                    hWVTStateData = IntPtr.Zero,
                    pwszURLReference = IntPtr.Zero,
                    dwProvFlags = 0,
                    dwUIContext = 0
                };

                var status = WinVerifyTrust(IntPtr.Zero, WinTrustActionGenericVerifyV2, ref trustData);
                trustData.dwStateAction = WtdStateActionClose;
                WinVerifyTrust(IntPtr.Zero, WinTrustActionGenericVerifyV2, ref trustData);

                if (status == 0) return true;
                reason = "Authenticode của DLL QS3D hiện tại không hợp lệ (WinVerifyTrust 0x" + unchecked((uint)status).ToString("X8") + ").";
                return false;
            }
            catch (Exception ex)
            {
                reason = "Không xác minh được Authenticode của DLL QS3D hiện tại: " + ex.Message;
                return false;
            }
            finally
            {
                if (fileInfoPointer != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(fileInfoPointer, typeof(WinTrustFileInfo));
                    Marshal.FreeCoTaskMem(fileInfoPointer);
                }
            }
        }

        private static string BuildWorkerScript(string updaterPath, string manifestUri, string signerThumbprint, string installDirectory, string bricscadPath, string logDirectory)
        {
            var script = new StringBuilder();
            script.AppendLine("$ErrorActionPreference = 'Stop'");
            script.AppendLine("$updater = " + PsLiteral(updaterPath));
            script.AppendLine("$manifest = " + PsLiteral(manifestUri));
            script.AppendLine("$expectedSigner = " + PsLiteral(signerThumbprint));
            script.AppendLine("$install = " + PsLiteral(installDirectory));
            script.AppendLine("$bricscad = " + PsLiteral(bricscadPath));
            script.AppendLine("$logDirectory = " + PsLiteral(logDirectory));
            script.AppendLine("New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null");
            script.AppendLine("$log = Join-Path $logDirectory ('update-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '.log')");
            script.AppendLine("Start-Transcript -Path $log -Force | Out-Null");
            script.AppendLine("try {");
            script.AppendLine("  while (Get-Process -Name bricscad -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 2 }");
            script.AppendLine("  if (-not (Test-Path -LiteralPath $updater -PathType Leaf)) { throw 'Installed QS3D updater script is missing.' }");
            script.AppendLine("  $signature = Get-AuthenticodeSignature -LiteralPath $updater");
            script.AppendLine("  if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $signature.SignerCertificate) { throw ('Installed updater signature is not valid: ' + $signature.Status) }");
            script.AppendLine("  $actualSigner = $signature.SignerCertificate.Thumbprint.Replace(' ', '').ToUpperInvariant()");
            script.AppendLine("  if ($actualSigner -ne $expectedSigner) { throw ('Installed updater signer mismatch. Expected ' + $expectedSigner + ', got ' + $actualSigner) }");
            script.AppendLine("  & $updater -ManifestUri $manifest -ExpectedSignerThumbprint $expectedSigner -InstallDirectory $install -AllowedPackageHost @('github.com') -AllowSameVersion -Confirm:$false");
            script.AppendLine("  if (-not $?) { throw 'QS3D update script reported failure.' }");
            script.AppendLine("  Stop-Transcript | Out-Null");
            script.AppendLine("  Start-Process -FilePath $bricscad | Out-Null");
            script.AppendLine("  exit 0");
            script.AppendLine("}");
            script.AppendLine("catch {");
            script.AppendLine("  Write-Error $_");
            script.AppendLine("  try { Stop-Transcript | Out-Null } catch { }");
            script.AppendLine("  exit 1");
            script.AppendLine("}");
            return script.ToString();
        }

        private static string NormalizeThumbprint(string value)
        {
            return (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        }

        private static string PsLiteral(string value)
        {
            return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
            ref WinTrustData trustData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustFileInfo
        {
            internal uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] internal string pcwszFilePath;
            internal IntPtr hFile;
            internal IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustData
        {
            internal uint cbStruct;
            internal IntPtr pPolicyCallbackData;
            internal IntPtr pSIPClientData;
            internal uint dwUIChoice;
            internal uint fdwRevocationChecks;
            internal uint dwUnionChoice;
            internal IntPtr pFile;
            internal uint dwStateAction;
            internal IntPtr hWVTStateData;
            internal IntPtr pwszURLReference;
            internal uint dwProvFlags;
            internal uint dwUIContext;
        }
    }
}
