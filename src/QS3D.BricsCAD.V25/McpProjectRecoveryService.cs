using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    internal sealed class McpRecoverySnapshot
    {
        public McpRecoverySnapshot(string path, DateTime utc, long bytes)
        {
            Path = path ?? string.Empty;
            Utc = utc;
            Bytes = bytes;
        }

        public string Path { get; private set; }
        public DateTime Utc { get; private set; }
        public long Bytes { get; private set; }
    }

    /// <summary>
    /// Two-layer recovery safety:
    /// 1) preserve/enable BricsCAD autosave + BAK, and
    /// 2) retain bounded coherent copies of the last saved DWG on disk.
    /// It never silently overwrites the active drawing during recovery.
    /// </summary>
    internal static class McpProjectRecoveryService
    {
        internal const int MaxSnapshotsPerProject = 30;
        private static readonly TimeSpan SnapshotInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
        private static readonly object Sync = new object();
        private static DispatcherTimer? _timer;
        private static string _lastSourcePath = string.Empty;
        private static long _lastSourceLength = -1;
        private static long _lastSourceWriteTicks = -1;
        private static DateTime _lastAttemptUtc = DateTime.MinValue;
        private static DateTime _lastBackupUtc = DateTime.MinValue;
        private static string _lastBackupPath = string.Empty;
        private static string _lastError = string.Empty;

        public static string BackupRoot
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QS3D",
                    "Backups");
            }
        }

        public static DateTime LastBackupUtc { get { lock (Sync) return _lastBackupUtc; } }
        public static string LastBackupPath { get { lock (Sync) return _lastBackupPath; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static bool IsRunning { get { lock (Sync) return _timer != null; } }

        public static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                var dispatcher = Dispatcher.CurrentDispatcher;
                _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                {
                    Interval = TickInterval
                };
                _timer.Tick += OnTick;
                _timer.Start();
            }

            ConfigureBricsCadAutosave();
            McpAgentExperience.Info("recovery", "QS3D recovery service đã bật.", string.Empty,
                "BricsCAD autosave + BAK được giữ an toàn; QS3D sẽ tạo versioned copy khi DWG on-disk thay đổi.");
        }

        public static void Stop()
        {
            DispatcherTimer? timer;
            lock (Sync)
            {
                timer = _timer;
                _timer = null;
            }
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= OnTick;
            }
        }

        public static string Describe()
        {
            lock (Sync)
            {
                return "running=" + (_timer != null)
                       + "; lastBackupUtc=" + (_lastBackupUtc == DateTime.MinValue ? "never" : _lastBackupUtc.ToString("o", CultureInfo.InvariantCulture))
                       + "; lastError=" + (_lastError ?? string.Empty);
            }
        }

        public static bool BackupNow(out string message)
        {
            try
            {
                ConfigureBricsCadAutosave();
                var captured = CaptureStableSource(true, out message);
                if (captured)
                    McpAgentExperience.Success("recovery", message, "Tiếp tục làm việc; recovery copy đã được tạo.");
                else if (!string.IsNullOrWhiteSpace(message))
                    McpAgentExperience.Warning("recovery", message, "Đợi BricsCAD idle/saved rồi thử Backup ngay lại.");
                return captured;
            }
            catch (Exception ex)
            {
                message = "Backup thất bại: " + ex.Message;
                SetError(message);
                McpAgentExperience.Error("recovery", message, "Mở Backup & khôi phục để kiểm tra đường dẫn/quyền ghi.");
                return false;
            }
        }

        public static McpRecoverySnapshot[] ListActiveDocumentBackups()
        {
            string source;
            if (!TryGetActiveSource(out source)) return new McpRecoverySnapshot[0];
            return ListForSource(source);
        }

        public static bool RecoverLatestToCopy(out string recoveredPath, out string message)
        {
            recoveredPath = string.Empty;
            message = string.Empty;
            try
            {
                string source;
                if (!TryGetActiveSource(out source))
                {
                    message = "Drawing hiện tại chưa có file DWG đã lưu trên disk.";
                    return false;
                }
                var snapshots = ListForSource(source);
                if (snapshots.Length == 0)
                {
                    message = "Chưa có QS3D recovery copy cho drawing này.";
                    return false;
                }

                var newest = snapshots[snapshots.Length - 1];
                var recoveredDirectory = System.IO.Path.Combine(BackupRoot, "Recovered");
                Directory.CreateDirectory(recoveredDirectory);
                var safeName = SafeFileStem(System.IO.Path.GetFileNameWithoutExtension(source));
                recoveredPath = UniquePath(System.IO.Path.Combine(
                    recoveredDirectory,
                    safeName + "-RECOVERED-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".dwg"));
                CopyStableFile(newest.Path, recoveredPath);
                message = "Đã khôi phục thành file mới: " + recoveredPath;
                McpAgentExperience.Success("recovery", "Đã tạo recovered copy từ snapshot gần nhất.",
                    "Mở file recovered để kiểm tra trước; QS3D không ghi đè drawing gốc.");
                return true;
            }
            catch (Exception ex)
            {
                recoveredPath = string.Empty;
                message = "Khôi phục thất bại: " + ex.Message;
                SetError(message);
                McpAgentExperience.Error("recovery", message, "Giữ nguyên drawing gốc và thử mở thư mục backup thủ công.");
                return false;
            }
        }

        private static void OnTick(object sender, EventArgs e)
        {
            try
            {
                ConfigureBricsCadAutosave();
                lock (Sync)
                {
                    if (DateTime.UtcNow - _lastAttemptUtc < SnapshotInterval) return;
                    _lastAttemptUtc = DateTime.UtcNow;
                }
                string ignored;
                CaptureStableSource(false, out ignored);
            }
            catch (Exception ex)
            {
                SetError("Periodic backup: " + ex.Message);
                McpAgentExperience.Error("recovery", "Periodic backup lỗi: " + ex.Message,
                    "QS3D sẽ thử lại ở chu kỳ sau; BricsCAD autosave/BAK vẫn độc lập.");
            }
        }

        private static void ConfigureBricsCadAutosave()
        {
            try
            {
                var saveTime = Convert.ToInt32(Application.GetSystemVariable("SAVETIME"), CultureInfo.InvariantCulture);
                // Keep an already-shorter user interval. Enable or reduce only when it is off/too long.
                if (saveTime <= 0 || saveTime > 5) Application.SetSystemVariable("SAVETIME", 5);
            }
            catch (Exception ex)
            {
                SetError("Không cấu hình được SAVETIME: " + ex.Message);
            }

            try
            {
                var saveBak = Convert.ToInt32(Application.GetSystemVariable("ISAVEBAK"), CultureInfo.InvariantCulture);
                if (saveBak == 0) Application.SetSystemVariable("ISAVEBAK", 1);
            }
            catch (Exception ex)
            {
                SetError("Không cấu hình được ISAVEBAK: " + ex.Message);
            }
        }

        private static bool CaptureStableSource(bool force, out string message)
        {
            message = string.Empty;
            if (!IsCadIdle())
            {
                message = "BricsCAD đang chạy command; QS3D không copy DWG giữa lúc mutation đang hoạt động.";
                return false;
            }

            string source;
            if (!TryGetActiveSource(out source))
            {
                message = "Drawing hiện tại chưa có đường dẫn DWG đã lưu; hãy Save lần đầu để bật versioned recovery copy.";
                return false;
            }

            var before = new FileInfo(source);
            if (!before.Exists || before.Length <= 0)
            {
                message = "DWG nguồn chưa tồn tại hoặc rỗng trên disk.";
                return false;
            }

            lock (Sync)
            {
                if (!force
                    && string.Equals(source, _lastSourcePath, StringComparison.OrdinalIgnoreCase)
                    && before.Length == _lastSourceLength
                    && before.LastWriteTimeUtc.Ticks == _lastSourceWriteTicks)
                    return false;
            }

            var folder = ProjectFolder(source);
            Directory.CreateDirectory(folder);
            var stem = SafeFileStem(System.IO.Path.GetFileNameWithoutExtension(source));
            var destination = UniquePath(System.IO.Path.Combine(
                folder,
                stem + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".dwg"));

            CopyStableFile(source, destination);
            var after = new FileInfo(source);
            if (!after.Exists || before.Length != after.Length || before.LastWriteTimeUtc.Ticks != after.LastWriteTimeUtc.Ticks)
            {
                try { File.Delete(destination); } catch { }
                message = "DWG thay đổi trong lúc tạo snapshot; bản copy trung gian đã bỏ và QS3D sẽ thử lại sau.";
                return false;
            }

            lock (Sync)
            {
                _lastSourcePath = source;
                _lastSourceLength = after.Length;
                _lastSourceWriteTicks = after.LastWriteTimeUtc.Ticks;
                _lastBackupUtc = DateTime.UtcNow;
                _lastBackupPath = destination;
                _lastError = string.Empty;
            }
            TrimRetention(folder);
            message = "Đã tạo QS3D recovery copy: " + System.IO.Path.GetFileName(destination);
            return true;
        }

        private static void CopyStableFile(string source, string destination)
        {
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                input.CopyTo(output, 1024 * 1024);
                output.Flush(true);
            }
        }

        private static bool IsCadIdle()
        {
            try
            {
                return Convert.ToInt32(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) == 0;
            }
            catch { return false; }
        }

        private static bool TryGetActiveSource(out string source)
        {
            source = string.Empty;
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null || string.IsNullOrWhiteSpace(document.Name)) return false;
                var candidate = System.IO.Path.GetFullPath(document.Name);
                if (!System.IO.Path.IsPathRooted(candidate) || !File.Exists(candidate)) return false;
                if (!string.Equals(System.IO.Path.GetExtension(candidate), ".dwg", StringComparison.OrdinalIgnoreCase)) return false;
                source = candidate;
                return true;
            }
            catch { return false; }
        }

        private static McpRecoverySnapshot[] ListForSource(string source)
        {
            try
            {
                var folder = ProjectFolder(source);
                if (!Directory.Exists(folder)) return new McpRecoverySnapshot[0];
                return Directory.GetFiles(folder, "*.dwg", SearchOption.TopDirectoryOnly)
                    .Select(path => new FileInfo(path))
                    .Where(info => info.Exists && info.Length > 0)
                    .OrderBy(info => info.LastWriteTimeUtc)
                    .Select(info => new McpRecoverySnapshot(info.FullName, info.LastWriteTimeUtc, info.Length))
                    .ToArray();
            }
            catch { return new McpRecoverySnapshot[0]; }
        }

        private static string ProjectFolder(string source)
        {
            var normalized = System.IO.Path.GetFullPath(source).Trim().ToUpperInvariant();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                var key = BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty).ToLowerInvariant();
                return System.IO.Path.Combine(BackupRoot, SafeFileStem(System.IO.Path.GetFileNameWithoutExtension(source)) + "-" + key);
            }
        }

        private static void TrimRetention(string folder)
        {
            try
            {
                var files = Directory.GetFiles(folder, "*.dwg", SearchOption.TopDirectoryOnly)
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(info => info.LastWriteTimeUtc)
                    .ToArray();
                for (var i = MaxSnapshotsPerProject; i < files.Length; i++)
                {
                    try { files[i].Delete(); } catch { }
                }
            }
            catch { }
        }

        private static string SafeFileStem(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "drawing" : name.Trim();
            foreach (var invalid in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            if (name.Length > 80) name = name.Substring(0, 80);
            return name;
        }

        private static string UniquePath(string path)
        {
            if (!File.Exists(path)) return path;
            var directory = System.IO.Path.GetDirectoryName(path) ?? BackupRoot;
            var stem = System.IO.Path.GetFileNameWithoutExtension(path);
            var extension = System.IO.Path.GetExtension(path);
            for (var i = 1; i < 1000; i++)
            {
                var candidate = System.IO.Path.Combine(directory, stem + "-" + i.ToString(CultureInfo.InvariantCulture) + extension);
                if (!File.Exists(candidate)) return candidate;
            }
            throw new IOException("Không tạo được tên recovery copy duy nhất.");
        }

        private static void SetError(string message)
        {
            lock (Sync) _lastError = message ?? string.Empty;
        }
    }
}
