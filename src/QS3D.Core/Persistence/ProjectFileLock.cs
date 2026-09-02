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

        private ProjectFileLock(FileStream stream)
        {
            _stream = stream;
        }

        public static ProjectFileLock Acquire(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath)) throw new ArgumentException("Project path is required.", nameof(projectPath));
            var lockPath = Path.GetFullPath(projectPath) + ".lock";
            var directory = Path.GetDirectoryName(lockPath);

            // The lock is the write-serialization authority for a QSDB path. Never
            // let an existing symbolic-link/reparse-point file or directory component
            // redirect that authority to a different filesystem object.
            PersistencePathSafety.RequireNonRedirected(lockPath, "project-lock");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                PersistencePathSafety.RequireNonRedirected(lockPath, "project-lock");
            }

            FileStream? stream = null;
            try
            {
                // Keep one stable rendezvous path for every owner. The supported
                // Windows product must bind the accepted pathname to the exact file
                // generation held by this exclusive stream before any truncation.
                PersistencePathSafety.RequireNonRedirected(lockPath, "project-lock");
                stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, "project-lock");
                stream.SetLength(0);
                stream.Position = 0;
                var payload = Encoding.UTF8.GetBytes("pid=" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "\nutc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
                return new ProjectFileLock(stream);
            }
            catch (IOException ex)
            {
                stream?.Dispose();
                throw new InvalidOperationException("Unable to acquire exclusive QS3D project write lock.", ex);
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            var stream = _stream;
            if (stream == null) return;
            _stream = null;
            stream.Dispose();
        }
    }
}
