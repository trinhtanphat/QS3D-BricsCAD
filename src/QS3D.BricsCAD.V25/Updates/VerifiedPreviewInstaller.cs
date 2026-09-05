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
        private const string PreviewPayloadManifestFileName = "payload-manifest.tsv";
        private const string ManifestEntryFile = "ENTRY_FILE";
        private const string ManifestEntryDirectory = "ENTRY_DIRECTORY";
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

                foreach (var fileName in RequiredPayload)
                {
                    var destinationPath = GetSafeChildPath(installDirectory, fileName, "Đường dẫn đích updater");
                    if (!string.Equals(Path.GetDirectoryName(destinationPath), installDirectory, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Payload bắt buộc của updater không nằm ở root thư mục DLL.");
                    if (!File.Exists(destinationPath))
                        throw new FileNotFoundException("Thiếu payload QS3D hiện tại nên updater không thể tạo rollback an toàn.", destinationPath);
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

                var stagedEntries = StageVerifiedPayload(
                    packagePath,
                    expectedSha256,
                    validationDirectory,
                    payloadDirectory);

                var manifestEntries = WritePayloadManifest(
                    stagingRoot,
                    installDirectory,
                    payloadDirectory,
                    backupDirectory,
                    stagedEntries);

                var v25Entry = RequireFileManifestEntry(manifestEntries, RequiredPayload[0]);
                var coreEntry = RequireFileManifestEntry(manifestEntries, RequiredPayload[1]);
                if (!v25Entry.CreatedBeforeApply || !coreEntry.CreatedBeforeApply)
                    throw new InvalidOperationException("Hai DLL QS3D bắt buộc phải tồn tại trước khi áp dụng preview.");

                var manifestPath = Path.Combine(stagingRoot, PreviewPayloadManifestFileName);
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
                    manifestPath,
                    installDirectory,
                    payloadDirectory,
                    backupDirectory,
                    v25Entry,
                    coreEntry);

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
                if (!(entry.Key is string key) || key.Length == 0) continue;
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

        private static List<StagedPayloadEntry> StageVerifiedPayload(
            string packagePath,
            string expectedSha256,
            string validationDirectory,
            string payloadDirectory)
        {
            var result = new List<StagedPayloadEntry>();
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
                        var isDirectory = entry.Name.Length == 0 ||
                                          (entry.FullName ?? string.Empty).EndsWith("/", StringComparison.Ordinal) ||
                                          (entry.FullName ?? string.Empty).EndsWith("\\", StringComparison.Ordinal);
                        var normalizedEntryName = NormalizeRelativePath(entry.FullName ?? string.Empty, isDirectory);

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

                        var stagedPath = GetSafeChildPath(payloadDirectory, normalizedEntryName, "Đường dẫn staging preview");
                        if (isDirectory)
                        {
                            if (File.Exists(stagedPath))
                                throw new InvalidDataException("ZIP preview chứa xung đột file/thư mục: " + normalizedEntryName);
                            Directory.CreateDirectory(stagedPath);
                            result.Add(new StagedPayloadEntry(normalizedEntryName, stagedPath, true, string.Empty));
                            continue;
                        }

                        var parentDirectory = Path.GetDirectoryName(stagedPath);
                        if (string.IsNullOrWhiteSpace(parentDirectory))
                            throw new InvalidDataException("Không xác định được thư mục staging cho payload preview.");
                        if (File.Exists(parentDirectory))
                            throw new InvalidDataException("ZIP preview chứa xung đột file/thư mục: " + normalizedEntryName);
                        Directory.CreateDirectory(parentDirectory);
                        ExtractBounded(entry, stagedPath);
                        result.Add(new StagedPayloadEntry(
                            normalizedEntryName,
                            stagedPath,
                            false,
                            ComputeSha256(stagedPath)));
                    }
                }
            }

            foreach (var fileName in RequiredPayload)
            {
                var found = false;
                foreach (var entry in result)
                {
                    if (!entry.IsDirectory &&
                        string.Equals(entry.RelativePath, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new InvalidDataException("ZIP preview thiếu payload bắt buộc ở root: " + fileName);
            }

            return result;
        }

        private static List<PayloadManifestEntry> WritePayloadManifest(
            string stagingRoot,
            string installDirectory,
            string payloadDirectory,
            string backupDirectory,
            IList<StagedPayloadEntry> stagedEntries)
        {
            var files = new Dictionary<string, StagedPayloadEntry>(StringComparer.OrdinalIgnoreCase);
            var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var stagedEntry in stagedEntries)
            {
                AddParentDirectories(stagedEntry.RelativePath, directories);
                if (stagedEntry.IsDirectory)
                {
                    if (files.ContainsKey(stagedEntry.RelativePath))
                        throw new InvalidDataException("ZIP preview chứa xung đột file/thư mục: " + stagedEntry.RelativePath);
                    directories.Add(stagedEntry.RelativePath);
                }
                else
                {
                    if (directories.Contains(stagedEntry.RelativePath) || files.ContainsKey(stagedEntry.RelativePath))
                        throw new InvalidDataException("ZIP preview chứa đường dẫn file trùng/xung đột: " + stagedEntry.RelativePath);
                    files.Add(stagedEntry.RelativePath, stagedEntry);
                }
            }

            foreach (var relativeDirectory in directories)
            {
                if (files.ContainsKey(relativeDirectory))
                    throw new InvalidDataException("ZIP preview chứa xung đột file/thư mục: " + relativeDirectory);
            }

            var directoryList = new List<string>(directories);
            directoryList.Sort((left, right) =>
            {
                var depth = GetRelativeDepth(left).CompareTo(GetRelativeDepth(right));
                return depth != 0 ? depth : StringComparer.OrdinalIgnoreCase.Compare(left, right);
            });

            var fileList = new List<StagedPayloadEntry>(files.Values);
            fileList.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.RelativePath, right.RelativePath));

            var manifestEntries = new List<PayloadManifestEntry>();
            foreach (var relativeDirectory in directoryList)
            {
                var destinationPath = GetSafeChildPath(installDirectory, relativeDirectory, "Đường dẫn thư mục đích preview");
                if (File.Exists(destinationPath))
                    throw new InvalidOperationException("Không thể mirror preview vì file đang chiếm vị trí thư mục: " + relativeDirectory);

                var createdBeforeApply = Directory.Exists(destinationPath);
                manifestEntries.Add(new PayloadManifestEntry(
                    ManifestEntryDirectory,
                    relativeDirectory,
                    string.Empty,
                    string.Empty,
                    destinationPath,
                    createdBeforeApply,
                    string.Empty,
                    string.Empty));
            }

            long totalBackupBytes = 0;
            foreach (var stagedEntry in fileList)
            {
                var destinationPath = GetSafeChildPath(installDirectory, stagedEntry.RelativePath, "Đường dẫn file đích preview");
                if (Directory.Exists(destinationPath))
                    throw new InvalidOperationException("Không thể mirror preview vì thư mục đang chiếm vị trí file: " + stagedEntry.RelativePath);

                var createdBeforeApply = File.Exists(destinationPath);
                var backupPath = string.Empty;
                var backupSha256 = string.Empty;
                if (createdBeforeApply)
                {
                    var fileInfo = new FileInfo(destinationPath);
                    checked { totalBackupBytes += fileInfo.Length; }
                    if (fileInfo.Length > MaxArchiveUncompressedBytes || totalBackupBytes > MaxArchiveUncompressedBytes)
                        throw new InvalidOperationException("Dữ liệu rollback preview vượt giới hạn an toàn.");

                    backupPath = GetSafeChildPath(backupDirectory, stagedEntry.RelativePath, "Đường dẫn backup preview");
                    var backupParent = Path.GetDirectoryName(backupPath);
                    if (string.IsNullOrWhiteSpace(backupParent))
                        throw new InvalidOperationException("Không xác định được thư mục backup preview.");
                    Directory.CreateDirectory(backupParent);

                    var sourceSha256 = ComputeSha256(destinationPath);
                    File.Copy(destinationPath, backupPath, false);
                    backupSha256 = ComputeSha256(backupPath);
                    if (!string.Equals(sourceSha256, backupSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Backup rollback không khớp file hiện tại: " + stagedEntry.RelativePath);
                }

                manifestEntries.Add(new PayloadManifestEntry(
                    ManifestEntryFile,
                    stagedEntry.RelativePath,
                    stagedEntry.StagedPath,
                    stagedEntry.Sha256,
                    destinationPath,
                    createdBeforeApply,
                    backupPath,
                    backupSha256));
            }

            var manifestPath = Path.Combine(stagingRoot, PreviewPayloadManifestFileName);
            using (var writer = new StreamWriter(manifestPath, false, new UTF8Encoding(false)))
            {
                foreach (var manifestEntry in manifestEntries)
                {
                    writer.Write(manifestEntry.Kind);
                    writer.Write('\t');
                    writer.Write(EncodeRelativePath(manifestEntry.RelativePath));
                    writer.Write('\t');
                    writer.Write(manifestEntry.StageSha256);
                    writer.Write('\t');
                    writer.Write(manifestEntry.CreatedBeforeApply ? "1" : "0");
                    writer.Write('\t');
                    writer.Write(manifestEntry.BackupSha256);
                    writer.WriteLine();
                }
            }

            return manifestEntries;
        }

        private static PayloadManifestEntry RequireFileManifestEntry(
            IList<PayloadManifestEntry> manifestEntries,
            string relativePath)
        {
            foreach (var entry in manifestEntries)
            {
                if (string.Equals(entry.Kind, ManifestEntryFile, StringComparison.Ordinal) &&
                    string.Equals(entry.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            throw new InvalidDataException("Manifest preview thiếu payload bắt buộc: " + relativePath);
        }

        private static void AddParentDirectories(string relativePath, ISet<string> directories)
        {
            var current = Path.GetDirectoryName(relativePath);
            while (!string.IsNullOrWhiteSpace(current))
            {
                current = NormalizeRelativePath(current, true);
                directories.Add(current);
                current = Path.GetDirectoryName(current);
            }
        }

        private static int GetRelativeDepth(string relativePath)
        {
            var depth = 0;
            foreach (var character in relativePath)
            {
                if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                    depth++;
            }
            return depth;
        }

        private static string NormalizeRelativePath(string rawPath, bool isDirectory)
        {
            var normalized = (rawPath ?? string.Empty)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (isDirectory)
                normalized = normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized))
                throw new InvalidDataException("ZIP preview chứa đường dẫn không hợp lệ.");

            var segments = normalized.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new InvalidDataException("ZIP preview chứa đường dẫn rỗng.");
            foreach (var segment in segments)
            {
                if (segment == "." || segment == "..")
                    throw new InvalidDataException("ZIP preview chứa path traversal.");
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), segments);
        }

        private static string GetSafeChildPath(string rootDirectory, string relativePath, string label)
        {
            var root = Path.GetFullPath(rootDirectory);
            var prefix = EnsureTrailingSeparator(root);
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(label + " nằm ngoài root cho phép (path traversal).");
            return candidate;
        }

        private static string EncodeRelativePath(string relativePath)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(relativePath));
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
            string manifestPath,
            string installDirectory,
            string payloadDirectory,
            string backupDirectory,
            PayloadManifestEntry v25Entry,
            PayloadManifestEntry coreEntry)
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
            startInfo.EnvironmentVariables["QS3D_PREVIEW_MANIFEST"] = manifestPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_INSTALL_ROOT"] = installDirectory;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_PAYLOAD_ROOT"] = payloadDirectory;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_BACKUP_ROOT"] = backupDirectory;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_STAGE"] = v25Entry.StagedPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_STAGE"] = coreEntry.StagedPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_STAGE_SHA"] = v25Entry.StageSha256;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_STAGE_SHA"] = coreEntry.StageSha256;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_BACKUP"] = v25Entry.BackupPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_BACKUP"] = coreEntry.BackupPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_BACKUP_SHA"] = v25Entry.BackupSha256;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_BACKUP_SHA"] = coreEntry.BackupSha256;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_V25_DEST"] = v25Entry.DestinationPath;
            startInfo.EnvironmentVariables["QS3D_PREVIEW_CORE_DEST"] = coreEntry.DestinationPath;
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
function Decode-RelativePath([string]$Encoded) {
    if ([string]::IsNullOrWhiteSpace($Encoded)) { throw 'Preview manifest relative path is empty.' }
    try {
        $bytes = [Convert]::FromBase64String($Encoded)
        $relative = [Text.Encoding]::UTF8.GetString($bytes)
    } catch {
        throw 'Preview manifest contains invalid base64 relative path.'
    }
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative)) {
        throw 'Preview manifest contains invalid relative path.'
    }
    return $relative
}
function Assert-SafeDestination([string]$Root, [string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Root) -or [string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative)) {
        throw 'Preview manifest path safety check failed.'
    }
    $rootFull = [IO.Path]::GetFullPath($Root)
    $candidate = [IO.Path]::GetFullPath([IO.Path]::Combine($rootFull, $Relative))
    $prefix = $rootFull.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Preview manifest path escapes its approved root.'
    }
    return $candidate
}
function Read-PreviewManifest {
    if (-not [IO.File]::Exists($env:QS3D_PREVIEW_MANIFEST)) { throw 'Preview payload manifest is missing.' }
    $entries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in [IO.File]::ReadAllLines($env:QS3D_PREVIEW_MANIFEST, [Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line.Split([char]9)
        if ($parts.Length -ne 5) { throw 'Preview manifest row has an invalid field count.' }
        $kind = $parts[0]
        if ($kind -ne 'ENTRY_FILE' -and $kind -ne 'ENTRY_DIRECTORY') { throw 'Preview manifest row has an invalid entry kind.' }
        $relative = Decode-RelativePath $parts[1]
        if ($parts[3] -eq '1') { $createdBeforeApply = $true }
        elseif ($parts[3] -eq '0') { $createdBeforeApply = $false }
        else { throw 'Preview manifest row has an invalid createdBeforeApply flag.' }

        $stageSha = $parts[2]
        $backupSha = $parts[4]
        if ($kind -eq 'ENTRY_FILE') {
            if ($stageSha -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Preview manifest file row has an invalid staged SHA-256.' }
            if ($createdBeforeApply -and $backupSha -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Preview manifest file row has an invalid backup SHA-256.' }
            if (-not $createdBeforeApply -and -not [string]::IsNullOrEmpty($backupSha)) { throw 'Preview manifest new-file row unexpectedly has a backup hash.' }
        } else {
            if (-not [string]::IsNullOrEmpty($stageSha) -or -not [string]::IsNullOrEmpty($backupSha)) {
                throw 'Preview manifest directory row unexpectedly contains file hashes.'
            }
        }

        [void]$entries.Add([pscustomobject]@{
            Kind = $kind
            RelativePath = $relative
            StageSha256 = $stageSha
            CreatedBeforeApply = $createdBeforeApply
            BackupSha256 = $backupSha
        })
    }
    if ($entries.Count -eq 0) { throw 'Preview payload manifest is empty.' }
    return $entries
}
function Mirror-Payload {
    $entries = Read-PreviewManifest
    foreach ($entry in $entries) {
        $destination = Assert-SafeDestination $env:QS3D_PREVIEW_INSTALL_ROOT $entry.RelativePath
        if ($entry.Kind -eq 'ENTRY_DIRECTORY') {
            if ([IO.File]::Exists($destination)) { throw ('File blocks preview directory: ' + $entry.RelativePath) }
            if ($entry.CreatedBeforeApply) {
                if (-not [IO.Directory]::Exists($destination)) { throw ('Existing preview directory disappeared: ' + $entry.RelativePath) }
            } else {
                if ([IO.Directory]::Exists($destination)) { throw ('New preview directory appeared before apply: ' + $entry.RelativePath) }
                [IO.Directory]::CreateDirectory($destination) | Out-Null
                [void]$script:appliedEntries.Add($entry)
            }
            continue
        }

        $stage = Assert-SafeDestination $env:QS3D_PREVIEW_PAYLOAD_ROOT $entry.RelativePath
        Assert-Hash $stage $entry.StageSha256 ('staged payload ' + $entry.RelativePath)
        if ([IO.Directory]::Exists($destination)) { throw ('Directory blocks preview file: ' + $entry.RelativePath) }

        if ($entry.CreatedBeforeApply) {
            $backup = Assert-SafeDestination $env:QS3D_PREVIEW_BACKUP_ROOT $entry.RelativePath
            Assert-Hash $backup $entry.BackupSha256 ('rollback backup ' + $entry.RelativePath)
            Assert-Hash $destination $entry.BackupSha256 ('current installed payload ' + $entry.RelativePath)
        } else {
            if ([IO.File]::Exists($destination)) { throw ('New preview file appeared before apply: ' + $entry.RelativePath) }
        }

        $directory = [IO.Path]::GetDirectoryName($destination)
        if ([string]::IsNullOrWhiteSpace($directory) -or -not [IO.Directory]::Exists($directory)) {
            throw ('Preview destination parent directory is missing: ' + $entry.RelativePath)
        }
        $temp = [IO.Path]::Combine($directory, '.qs3d-preview-' + $env:QS3D_PREVIEW_HANDOFF + '-' + [Guid]::NewGuid().ToString('N') + '.new')
        if ([IO.File]::Exists($temp)) { [IO.File]::Delete($temp) }
        [IO.File]::Copy($stage, $temp, $false)
        Assert-Hash $temp $entry.StageSha256 ('replacement temp ' + $entry.RelativePath)
        if ($entry.CreatedBeforeApply) {
            [IO.File]::Replace($temp, $destination, $null, $true)
        } else {
            [IO.File]::Move($temp, $destination)
        }
        [void]$script:appliedEntries.Add($entry)
        Assert-Hash $destination $entry.StageSha256 ('installed payload ' + $entry.RelativePath)
    }
}
function Restore-MirroredPayload {
    for ($i = $script:appliedEntries.Count - 1; $i -ge 0; $i--) {
        $entry = $script:appliedEntries[$i]
        $destination = Assert-SafeDestination $env:QS3D_PREVIEW_INSTALL_ROOT $entry.RelativePath
        if ($entry.Kind -eq 'ENTRY_FILE') {
            if ($entry.CreatedBeforeApply) {
                $backup = Assert-SafeDestination $env:QS3D_PREVIEW_BACKUP_ROOT $entry.RelativePath
                Assert-Hash $backup $entry.BackupSha256 ('rollback backup ' + $entry.RelativePath)
                $directory = [IO.Path]::GetDirectoryName($destination)
                if (-not [IO.Directory]::Exists($directory)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
                $temp = [IO.Path]::Combine($directory, '.qs3d-preview-' + $env:QS3D_PREVIEW_HANDOFF + '-' + [Guid]::NewGuid().ToString('N') + '.rollback')
                if ([IO.File]::Exists($temp)) { [IO.File]::Delete($temp) }
                [IO.File]::Copy($backup, $temp, $false)
                Assert-Hash $temp $entry.BackupSha256 ('rollback temp ' + $entry.RelativePath)
                if ([IO.File]::Exists($destination)) {
                    [IO.File]::Replace($temp, $destination, $null, $true)
                } else {
                    if ([IO.Directory]::Exists($destination)) { throw ('Directory blocks rollback file: ' + $entry.RelativePath) }
                    [IO.File]::Move($temp, $destination)
                }
                Assert-Hash $destination $entry.BackupSha256 ('restored payload ' + $entry.RelativePath)
            } else {
                if ([IO.Directory]::Exists($destination)) { throw ('Directory blocks rollback deletion: ' + $entry.RelativePath) }
                if ([IO.File]::Exists($destination)) { [IO.File]::Delete($destination) }
            }
            continue
        }

        if (-not $entry.CreatedBeforeApply -and [IO.Directory]::Exists($destination)) {
            $children = [IO.Directory]::GetFileSystemEntries($destination)
            if ($children.Length -eq 0) { [IO.Directory]::Delete($destination, $false) }
        }
    }
}
function Rollback {
    Restore-MirroredPayload
}
function Write-Log([string]$Text) {
    try { [IO.File]::AppendAllText($env:QS3D_PREVIEW_LOG, ((Get-Date).ToString('o') + ' ' + $Text + [Environment]::NewLine)) }
    catch { }
}
function Restart-BricsCAD {
    $exe = $env:QS3D_PREVIEW_BRICSCAD
    if ([string]::IsNullOrWhiteSpace($exe) -or -not [IO.File]::Exists($exe)) {
        throw 'Captured bricscad.exe is missing; cannot restart host.'
    }
    if (-not [string]::Equals([IO.Path]::GetFileName($exe), 'bricscad.exe', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Captured host executable is not bricscad.exe.'
    }
    $working = [IO.Path]::GetDirectoryName($exe)
    Start-Process -FilePath $env:QS3D_PREVIEW_BRICSCAD -WorkingDirectory $working
    Write-Log ('RESTART ' + $exe)
}

$replaceStarted = $false
$parentExited = $false
$restarted = $false
$script:appliedEntries = New-Object 'System.Collections.Generic.List[object]'
try {
    $parentPid = [int]$env:QS3D_PREVIEW_PARENT_PID
    try {
        $hostProcess = [Diagnostics.Process]::GetProcessById($parentPid)
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
    Mirror-Payload
    Assert-Hash $env:QS3D_PREVIEW_V25_DEST $env:QS3D_PREVIEW_V25_STAGE_SHA 'installed V25 adapter'
    Assert-Hash $env:QS3D_PREVIEW_CORE_DEST $env:QS3D_PREVIEW_CORE_STAGE_SHA 'installed Core'

    Write-Log 'PASS verified preview full-package mirror apply'
    Restart-BricsCAD
    $restarted = $true
    try { [IO.Directory]::Delete($env:QS3D_PREVIEW_STAGE_ROOT, $true) } catch { }
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

        private sealed class StagedPayloadEntry
        {
            internal StagedPayloadEntry(string relativePath, string stagedPath, bool isDirectory, string sha256)
            {
                RelativePath = relativePath;
                StagedPath = stagedPath;
                IsDirectory = isDirectory;
                Sha256 = sha256;
            }

            internal string RelativePath { get; }
            internal string StagedPath { get; }
            internal bool IsDirectory { get; }
            internal string Sha256 { get; }
        }

        private sealed class PayloadManifestEntry
        {
            internal PayloadManifestEntry(
                string kind,
                string relativePath,
                string stagedPath,
                string stageSha256,
                string destinationPath,
                bool createdBeforeApply,
                string backupPath,
                string backupSha256)
            {
                Kind = kind;
                RelativePath = relativePath;
                StagedPath = stagedPath;
                StageSha256 = stageSha256;
                DestinationPath = destinationPath;
                CreatedBeforeApply = createdBeforeApply;
                BackupPath = backupPath;
                BackupSha256 = backupSha256;
            }

            internal string Kind { get; }
            internal string RelativePath { get; }
            internal string StagedPath { get; }
            internal string StageSha256 { get; }
            internal string DestinationPath { get; }
            internal bool CreatedBeforeApply { get; }
            internal string BackupPath { get; }
            internal string BackupSha256 { get; }
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
