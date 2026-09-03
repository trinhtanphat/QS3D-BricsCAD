using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25.Updates
{
    internal static class VerifiedPreviewInstaller
    {
        private const long MaxArchiveUncompressedBytes = 512L * 1024L * 1024L;
        private const long MaxPayloadFileBytes = 256L * 1024L * 1024L;
        private const int WorkerStartupProbeMilliseconds = 5000;
        private const uint CreateBreakawayFromJob = 0x01000000;
        private const uint CreateUnicodeEnvironment = 0x00000400;
        private const uint CreateNoWindow = 0x08000000;
        private static readonly string[] RequiredPayload = new[] { "QS3D.BricsCAD.V25.dll", "QS3D.Core.dll" };
        private static int _scheduled;

        internal static bool TrySchedule(string packagePath, string expectedSha256, out string error)
        {
            error = string.Empty;
            if (Volatile.Read(ref _scheduled) != 0)
            {
                error = "Bản preview đã được lên lịch trong phiên BricsCAD này.";
                return false;
            }

            if (!IsSha256(expectedSha256))
            {
                error = "Checksum SHA-256 của package preview không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                error = "Không tìm thấy package preview đã xác minh.";
                return false;
            }

            var actualSha256 = ComputeSha256(packagePath);
            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Package preview đã thay đổi sau khi tải; SHA-256 không còn khớp.";
                return false;
            }

            if (Interlocked.CompareExchange(ref _scheduled, 1, 0) != 0)
            {
                error = "Bản preview đã được lên lịch trong phiên BricsCAD này.";
                return false;
            }

            string? stagingRoot = null;
            try
            {
                var pluginPath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrWhiteSpace(pluginPath) || !File.Exists(pluginPath))
                    throw new InvalidOperationException("Không xác định được DLL QS3D V25 đang chạy.");

                pluginPath = Path.GetFullPath(pluginPath);
                var installDirectory = Path.GetDirectoryName(pluginPath);
                if (string.IsNullOrWhiteSpace(installDirectory))
                    throw new InvalidOperationException("Không xác định được thư mục cài QS3D V25.");

                installDirectory = Path.GetFullPath(installDirectory);
                var expectedPluginPath = Path.GetFullPath(Path.Combine(installDirectory, RequiredPayload[0]));
                if (!string.Equals(pluginPath, expectedPluginPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("DLL QS3D V25 đang chạy không nằm ở vị trí cài đặt chuẩn.");

                var destinations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var installPrefix = EnsureTrailingSeparator(installDirectory);
                foreach (var fileName in RequiredPayload)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(installDirectory, fileName));
                    if (!destinationPath.StartsWith(installPrefix, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(Path.GetDirectoryName(destinationPath), installDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Đường dẫn đích updater nằm ngoài thư mục DLL đang chạy.");
                    }
                    if (!File.Exists(destinationPath))
                        throw new FileNotFoundException("Thiếu payload QS3D hiện tại nên updater không thể tạo rollback an toàn.", destinationPath);
                    destinations[fileName] = destinationPath;
                }

                var localRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localRoot))
                    throw new InvalidOperationException("Không xác định được LocalApplicationData để stage updater.");

                var handoffId = Guid.NewGuid().ToString("N");
                stagingRoot = Path.Combine(localRoot, "QS3D", "Updates", "PreviewApply", handoffId);
                var payloadDirectory = Path.Combine(stagingRoot, "payload");
                var backupDirectory = Path.Combine(stagingRoot, "backup");
                var validationDirectory = Path.Combine(stagingRoot, "zip");
                Directory.CreateDirectory(payloadDirectory);
                Directory.CreateDirectory(backupDirectory);
                Directory.CreateDirectory(validationDirectory);

                var stagedPaths = StageVerifiedPayload(
                    packagePath,
                    expectedSha256,
                    validationDirectory,
                    payloadDirectory);

                var stagedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var backupPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var backupHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var fileName in RequiredPayload)
                {
                    var stagedPath = stagedPaths[fileName];
                    stagedHashes[fileName] = ComputeSha256(stagedPath);

                    var sourcePath = destinations[fileName];
                    var sourceHash = ComputeSha256(sourcePath);
                    var backupPath = Path.Combine(backupDirectory, fileName + ".bak");
                    File.Copy(sourcePath, backupPath, true);
                    var backupHash = ComputeSha256(backupPath);
                    if (!string.Equals(sourceHash, backupHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Backup rollback không khớp với DLL QS3D hiện tại: " + fileName);

                    backupPaths[fileName] = backupPath;
                    backupHashes[fileName] = backupHash;
                }

                var logDirectory = Path.Combine(localRoot, "QS3D", "UpdateLogs");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "preview-apply-" + handoffId + ".log");

                int parentProcessId;
                string bricsCadExecutable;
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    parentProcessId = currentProcess.Id;
                    try
                    {
                        bricsCadExecutable = currentProcess.MainModule?.FileName ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Không đọc được đường dẫn BricsCAD đang chạy để tự mở lại sau cập nhật.", ex);
                    }
                }

                if (string.IsNullOrWhiteSpace(bricsCadExecutable))
                    throw new InvalidOperationException("Không xác định được bricscad.exe đang chạy để tự mở lại sau cập nhật.");
                bricsCadExecutable = Path.GetFullPath(bricsCadExecutable);
                if (!File.Exists(bricsCadExecutable) ||
                    !string.Equals(Path.GetFileName(bricsCadExecutable), "bricscad.exe", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Updater chỉ tự khởi động lại đúng bricscad.exe đang chạy.");
                }

                var startInfo = BuildWorkerStartInfo(
                    parentProcessId,
                    bricsCadExecutable,
                    handoffId,
                    stagingRoot,
                    logPath,
                    stagedPaths,
                    stagedHashes,
                    backupPaths,
                    backupHashes,
                    destinations);

                Process? worker;
                string workerError;
                if (!TryStartBreakawayWorker(startInfo, out worker, out workerError) || worker == null)
                    throw new InvalidOperationException(workerError);

                using (worker)
                {
                    if (worker.WaitForExit(WorkerStartupProbeMilliseconds))
                    {
                        var exitCode = worker.ExitCode;
                        throw new InvalidOperationException("Updater worker thoát sớm trước khi BricsCAD đóng (exit " + exitCode + ").");
                    }
                }

                stagingRoot = null; // ownership transferred to breakaway worker
                return true;
            }
            catch (Exception ex)
            {
                if (stagingRoot != null) TryDeleteDirectory(stagingRoot);
                Interlocked.Exchange(ref _scheduled, 0);
                error = ex.Message;
                return false;
            }
        }

        private static bool TryStartBreakawayWorker(ProcessStartInfo startInfo, out Process? worker, out string error)
        {
            worker = null;
            error = string.Empty;
            IntPtr environmentBlock = IntPtr.Zero;
            var processInformation = new ProcessInformation();
            try
            {
                if (startInfo == null || string.IsNullOrWhiteSpace(startInfo.FileName))
                {
                    error = "Không thể tạo updater worker độc lập khỏi vòng đời BricsCAD: thiếu executable worker.";
                    return false;
                }

                environmentBlock = BuildEnvironmentBlock(startInfo);
                var commandLine = new StringBuilder();
                commandLine.Append('"').Append(startInfo.FileName).Append('"');
                if (!string.IsNullOrWhiteSpace(startInfo.Arguments))
                    commandLine.Append(' ').Append(startInfo.Arguments);

                var startupInfo = new StartupInfo
                {
                    cb = Marshal.SizeOf(typeof(StartupInfo))
                };
                var creationFlags = CreateBreakawayFromJob | CreateUnicodeEnvironment | CreateNoWindow;
                var workingDirectory = string.IsNullOrWhiteSpace(startInfo.WorkingDirectory)
                    ? null
                    : startInfo.WorkingDirectory;

                if (!CreateProcess(
                        startInfo.FileName,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        creationFlags,
                        environmentBlock,
                        workingDirectory,
                        ref startupInfo,
                        out processInformation))
                {
                    var win32 = Marshal.GetLastWin32Error();
                    error = "Không thể tạo updater worker độc lập khỏi vòng đời BricsCAD (Win32 " +
                            win32.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                            "). Cập nhật đã dừng trước khi đóng BricsCAD.";
                    return false;
                }

                try
                {
                    worker = Process.GetProcessById(processInformation.dwProcessId);
                    return true;
                }
                catch (Exception ex)
                {
                    error = "Updater worker đã khởi động nhưng không thể mở handle giám sát: " + ex.Message;
                    TryTerminateProcess(processInformation.dwProcessId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = "Không thể tạo updater worker độc lập khỏi vòng đời BricsCAD: " + ex.Message;
                return false;
            }
            finally
            {
                if (processInformation.hThread != IntPtr.Zero) CloseHandle(processInformation.hThread);
                if (processInformation.hProcess != IntPtr.Zero) CloseHandle(processInformation.hProcess);
                if (environmentBlock != IntPtr.Zero) Marshal.FreeHGlobal(environmentBlock);
            }
        }

        private static IntPtr BuildEnvironmentBlock(ProcessStartInfo startInfo)
        {
            var entries = new List<KeyValuePair<string, string>>();
            foreach (DictionaryEntry entry in startInfo.EnvironmentVariables)
            {
                var key = entry.Key as string;
                if (string.IsNullOrEmpty(key)) continue;
                var value = entry.Value as string ?? string.Empty;
                entries.Add(new KeyValuePair<string, string>(key, value));
            }

            entries.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key));
            var builder = new StringBuilder();
            foreach (var entry in entries)
                builder.Append(entry.Key).Append('=').Append(entry.Value).Append('\0');
            builder.Append('\0');

            var bytes = Encoding.Unicode.GetBytes(builder.ToString());
            var block = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, block, bytes.Length);
            return block;
        }

        private static void TryTerminateProcess(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (process.HasExited) return;
                    process.Kill();
                    process.WaitForExit(WorkerStartupProbeMilliseconds);
                }
            }
            catch
            {
            }
        }

        private static Dictionary<string, string> StageVerifiedPayload(
            string packagePath,
            string expectedSha256,
            string validationDirectory,
            string payloadDirectory)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var seenCanonicalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stagingPrefix = EnsureTrailingSeparator(Path.GetFullPath(validationDirectory));
            long totalUncompressed = 0;

            using (var packageStream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var lockedSha256 = ComputeSha256(packageStream);
                if (!string.Equals(lockedSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Package preview thay đổi giữa bước xác minh và staging.");
                packageStream.Position = 0;

                using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var normalizedEntryName = (entry.FullName ?? string.Empty)
                            .Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar);
                        if (normalizedEntryName.Length == 0 || Path.IsPathRooted(normalizedEntryName))
                            throw new InvalidDataException("ZIP preview chứa đường dẫn không hợp lệ.");

                        var canonicalPath = Path.GetFullPath(Path.Combine(validationDirectory, normalizedEntryName));
                        if (!canonicalPath.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("ZIP preview chứa path traversal.");
                        if (!seenCanonicalPaths.Add(canonicalPath))
                            throw new InvalidDataException("ZIP preview chứa đường dẫn trùng nhau theo canonical path.");

                        if (entry.Length < 0 || entry.Length > MaxPayloadFileBytes)
                            throw new InvalidDataException("ZIP preview chứa entry vượt giới hạn kích thước.");
                        checked { totalUncompressed += entry.Length; }
                        if (totalUncompressed > MaxArchiveUncompressedBytes)
                            throw new InvalidDataException("ZIP preview vượt giới hạn dữ liệu giải nén.");

                        foreach (var fileName in RequiredPayload)
                        {
                            if (!string.Equals(normalizedEntryName, fileName, StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (entry.Name.Length == 0 || result.ContainsKey(fileName))
                                throw new InvalidDataException("ZIP preview thiếu hoặc trùng payload bắt buộc: " + fileName);

                            var stagedPath = Path.Combine(payloadDirectory, fileName);
                            ExtractBounded(entry, stagedPath);
                            result[fileName] = stagedPath;
                        }
                    }
                }
            }

            foreach (var fileName in RequiredPayload)
            {
                if (!result.ContainsKey(fileName))
                    throw new InvalidDataException("ZIP preview thiếu payload bắt buộc ở root: " + fileName);
            }
            return result;
        }

        private static void ExtractBounded(ZipArchiveEntry entry, string destinationPath)
        {
            long total = 0;
            var buffer = new byte[81920];
            using (var source = entry.Open())
            using (var target = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                while (true)
                {
                    var read = source.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    total += read;
                    if (total > MaxPayloadFileBytes)
                        throw new InvalidDataException("Payload preview giải nén vượt giới hạn.");
                    target.Write(buffer, 0, read);
                }
                target.Flush(true);
            }

            if (total != entry.Length)
                throw new InvalidDataException("Kích thước payload preview sau giải nén không khớp ZIP metadata.");
        }

        private static ProcessStartInfo BuildWorkerStartInfo(
            int parentProcessId,
            string bricsCadExecutable,
            string handoffId,
            string stagingRoot,
            string logPath,
            IDictionary<string, string> stagedPaths,
            IDictionary<string, string> stagedHashes,
            IDictionary<string, string> backupPaths,
            IDictionary<string, string> backupHashes,
            IDictionary<string, string> destinations)
        {
            var script = BuildWorkerScript();
            var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var powershellPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");
            if (!File.Exists(powershellPath))
                throw new FileNotFoundException("Không tìm thấy Windows PowerShell để chạy updater worker.", powershellPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = powershellPath,
                Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -EncodedCommand " + encodedScript,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            startInfo.EnvironmentVariables["QS3D_PREVIEW_PARENT_PID"] = parentProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            startInfo.EnvironmentVariables["QS3D_PREVIEW_BRICSCAD"] = bricsCadExecutable;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_HANDOFF"] = handoffId;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_STAGE_ROOT"] = stagingRoot;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_LOG"] = logPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_STAGE"] = stagedPaths[RequiredPayload[0]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_STAGE"] = stagedPaths[RequiredPayload[1]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_STAGE_SHA"] = stagedHashes[RequiredPayload[0]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_STAGE_SHA"] = stagedHashes[RequiredPayload[1]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_BACKUP"] = backupPaths[RequiredPayload[0]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_BACKUP"] = backupPaths[RequiredPayload[1]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_BACKUP_SHA"] = backupHashes[RequiredPayload[0]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_BACKUP_SHA"] = backupHashes[RequiredPayload[1]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_DEST"] = destinations[RequiredPayload[0]];
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_DEST"] = destinations[RequiredPayload[1]];
            return startInfo;
        }

        private static string BuildWorkerScript()
        {
            return @"
$ErrorActionPreference = 'Stop'
function Get-Sha256([string]$Path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
        finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}
function Assert-Hash([string]$Path, [string]$Expected, [string]$Label) {
    if (-not [System.IO.File]::Exists($Path)) { throw ($Label + ' is missing.') }
    $actual = Get-Sha256 $Path
    if (-not [string]::Equals($actual, $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw ($Label + ' SHA-256 mismatch.')
    }
}
function Replace-Payload([string]$Source, [string]$Destination, [string]$ExpectedHash, [string]$Suffix) {
    $directory = [System.IO.Path]::GetDirectoryName($Destination)
    $temp = [System.IO.Path]::Combine($directory, '.qs3d-preview-' + $env:QS3D_PREVIEW_HANDOFF + '-' + $Suffix + '.new')
    if ([System.IO.File]::Exists($temp)) { [System.IO.File]::Delete($temp) }
    [System.IO.File]::Copy($Source, $temp, $false)
    Assert-Hash $temp $ExpectedHash ('replacement temp ' + $Suffix)
    [System.IO.File]::Replace($temp, $Destination, $null, $true)
}
function Restore-Payload([string]$Backup, [string]$BackupHash, [string]$Destination, [string]$Suffix) {
    Assert-Hash $Backup $BackupHash ('rollback backup ' + $Suffix)
    $directory = [System.IO.Path]::GetDirectoryName($Destination)
    $temp = [System.IO.Path]::Combine($directory, '.qs3d-preview-' + $env:QS3D_PREVIEW_HANDOFF + '-' + $Suffix + '.rollback')
    if ([System.IO.File]::Exists($temp)) { [System.IO.File]::Delete($temp) }
    [System.IO.File]::Copy($Backup, $temp, $false)
    Assert-Hash $temp $BackupHash ('rollback temp ' + $Suffix)
    if ([System.IO.File]::Exists($Destination)) {
        [System.IO.File]::Replace($temp, $Destination, $null, $true)
    } else {
        [System.IO.File]::Move($temp, $Destination)
    }
}
function Rollback {
    Restore-Payload $env:QS3D_PREVIEW_V25_BACKUP $env:QS3D_PREVIEW_V25_BACKUP_SHA $env:QS3D_PREVIEW_V25_DEST 'v25'
    Restore-Payload $env:QS3D_PREVIEW_CORE_BACKUP $env:QS3D_PREVIEW_CORE_BACKUP_SHA $env:QS3D_PREVIEW_CORE_DEST 'core'
}
function Write-Log([string]$Text) {
    try { [System.IO.File]::AppendAllText($env:QS3D_PREVIEW_LOG, ((Get-Date).ToString('o') + ' ' + $Text + [Environment]::NewLine)) }
    catch { }
}
function Restart-BricsCAD {
    $exe = $env:QS3D_PREVIEW_BRICSCAD
    if ([string]::IsNullOrWhiteSpace($exe) -or -not [System.IO.File]::Exists($exe)) {
        throw 'Captured bricscad.exe is missing; cannot restart host.'
    }
    if (-not [string]::Equals([System.IO.Path]::GetFileName($exe), 'bricscad.exe', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Captured host executable is not bricscad.exe.'
    }
    $working = [System.IO.Path]::GetDirectoryName($exe)
    Start-Process -FilePath $env:QS3D_PREVIEW_BRICSCAD -WorkingDirectory $working
    Write-Log ('RESTART ' + $exe)
}

$replaceStarted = $false
$parentExited = $false
$restarted = $false
try {
    $parentPid = [int]$env:QS3D_PREVIEW_PARENT_PID
    try {
        $hostProcess = [System.Diagnostics.Process]::GetProcessById($parentPid)
        $hostProcess.WaitForExit()
        $hostProcess.Dispose()
        $parentExited = $true
    } catch [ArgumentException] {
        $parentExited = $true
    }

    Assert-Hash $env:QS3D_PREVIEW_V25_STAGE $env:QS3D_PREVIEW_V25_STAGE_SHA 'staged V25 adapter'
    Assert-Hash $env:QS3D_PREVIEW_CORE_STAGE $env:QS3D_PREVIEW_CORE_STAGE_SHA 'staged Core'
    Assert-Hash $env:QS3D_PREVIEW_V25_DEST $env:QS3D_PREVIEW_V25_BACKUP_SHA 'current V25 adapter'
    Assert-Hash $env:QS3D_PREVIEW_CORE_DEST $env:QS3D_PREVIEW_CORE_BACKUP_SHA 'current Core'

    $replaceStarted = $true
    Replace-Payload $env:QS3D_PREVIEW_V25_STAGE $env:QS3D_PREVIEW_V25_DEST $env:QS3D_PREVIEW_V25_STAGE_SHA 'v25'
    Replace-Payload $env:QS3D_PREVIEW_CORE_STAGE $env:QS3D_PREVIEW_CORE_DEST $env:QS3D_PREVIEW_CORE_STAGE_SHA 'core'
    Assert-Hash $env:QS3D_PREVIEW_V25_DEST $env:QS3D_PREVIEW_V25_STAGE_SHA 'installed V25 adapter'
    Assert-Hash $env:QS3D_PREVIEW_CORE_DEST $env:QS3D_PREVIEW_CORE_STAGE_SHA 'installed Core'

    Write-Log 'PASS verified preview apply'
    Restart-BricsCAD
    $restarted = $true
    try { [System.IO.Directory]::Delete($env:QS3D_PREVIEW_STAGE_ROOT, $true) } catch { }
    exit 0
}
catch {
    $message = $_.Exception.Message
    if ($replaceStarted) {
        try {
            Rollback
            Write-Log ('ROLLBACK ' + $message)
        } catch {
            Write-Log ('ROLLBACK-FAILED ' + $message + ' / ' + $_.Exception.Message)
        }
    } else {
        Write-Log ('FAIL ' + $message)
    }

    if ($parentExited -and -not $restarted) {
        try {
            Restart-BricsCAD
            $restarted = $true
            Write-Log 'RECOVERY-RESTART PASS'
        } catch {
            Write-Log ('RECOVERY-RESTART-FAILED ' + $_.Exception.Message)
        }
    }
    exit 1
}
";
        }

        // Mirrors the worker's atomic operation and keeps the replacement primitive centralized for source-contract review.
        private static void ReplaceAtomically(string sourcePath, string destinationPath)
        {
            File.Replace(sourcePath, destinationPath, null, true);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return ComputeSha256(stream);
        }

        private static string ComputeSha256(Stream stream)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) builder.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            foreach (var character in value)
            {
                var hex = (character >= '0' && character <= '9') ||
                          (character >= 'a' && character <= 'f') ||
                          (character >= 'A' && character <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;
            return path + Path.DirectorySeparatorChar;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public string? lpReserved;
            public string? lpDesktop;
            public string? lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcess(
            string? lpApplicationName,
            StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
