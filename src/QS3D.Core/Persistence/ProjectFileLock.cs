using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectFileLock : IDisposable
    {
        private FileStream? _stream;
        private readonly string _lockPath;

        private ProjectFileLock(string lockPath, FileStream stream)
        {
            _lockPath = lockPath;
            _stream = stream;
        }

        public static ProjectFileLock Acquire(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath)) throw new ArgumentException("Project path is required.", nameof(projectPath));
            var lockPath = Path.GetFullPath(projectPath) + ".lock";
            try
            {
                var stream = new FileStream(lockPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                var payload = Encoding.UTF8.GetBytes("pid=" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "\nutc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
                return new ProjectFileLock(lockPath, stream);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("QS3D project is already opened by another writer: " + lockPath, ex);
            }
        }

        public void Dispose()
        {
            var stream = _stream;
            if (stream == null) return;
            _stream = null;
            stream.Dispose();
            try { File.Delete(_lockPath); } catch (IOException) { }
        }
    }
}
