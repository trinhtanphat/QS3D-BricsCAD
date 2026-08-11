using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25.Updates
{
    internal static class SecureUpdateLauncher
    {
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
                reason = "Bản QS3D hiện tại chưa được ký Authenticode; cần cài thủ công một bản signed trước khi bật one-click update.";
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

                var process = Process.GetCurrentProcess();
                var bricscadPath = process.MainModule?.FileName;
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
                return true;
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _scheduled, 0);
                error = ex.Message;
                return false;
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
            script.AppendLine("  & $updater -ManifestUri $manifest -ExpectedSignerThumbprint $expectedSigner -InstallDirectory $install -AllowedPackageHost @('github.com') -Confirm:$false");
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
    }
}