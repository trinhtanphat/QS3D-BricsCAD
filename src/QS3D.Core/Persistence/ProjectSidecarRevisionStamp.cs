using System;
using System.IO;
using System.Security.Cryptography;

namespace QS3D.Core.Persistence
{
    /// <summary>
    /// Immutable, content-based revision for one QSDB primary/backup pair.
    /// Paths and digests remain private so callers can compare authority without
    /// exposing machine-local evidence.
    /// </summary>
    public sealed class ProjectSidecarRevisionStamp : IEquatable<ProjectSidecarRevisionStamp>
    {
        private const long MaxSidecarBytes = 64L * 1024L * 1024L;
        private static readonly StringComparer PathComparer =
            Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        private readonly string _primaryPath;
        private readonly FileRevision _primary;
        private readonly FileRevision _backup;

        private ProjectSidecarRevisionStamp(string primaryPath, FileRevision primary, FileRevision backup)
        {
            _primaryPath = primaryPath;
            _primary = primary;
            _backup = backup;
        }

        public bool HasAnyFile => _primary.Exists || _backup.Exists;

        public static ProjectSidecarRevisionStamp Capture(string primaryPath)
        {
            RequireCanonicalPath(primaryPath, nameof(primaryPath));

            var fullPath = Path.GetFullPath(primaryPath);
            var backupPath = fullPath + ".bak";
            PersistencePathSafety.RequireNonRedirected(fullPath, "sidecar revision primary read");
            PersistencePathSafety.RequireNonRedirected(backupPath, "sidecar revision backup read");
            using (var primary = FileCapture.Open(fullPath))
            using (var backup = FileCapture.Open(backupPath))
            {
                // Keep every existing member open without write/delete sharing while both
                // digests are produced. Missing members are rechecked before returning.
                // This makes the primary/backup observation one stable pair rather than
                // two unrelated point-in-time reads.
                var primaryRevision = primary.CaptureStableRevision();
                var backupRevision = backup.CaptureStableRevision();
                primary.EnsurePresenceUnchanged();
                backup.EnsurePresenceUnchanged();
                return new ProjectSidecarRevisionStamp(fullPath, primaryRevision, backupRevision);
            }
        }

        public bool IsForPath(string primaryPath)
        {
            if (!IsCanonicalPath(primaryPath)) return false;
            try
            {
                return PathComparer.Equals(_primaryPath, Path.GetFullPath(primaryPath));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return false;
            }
        }

        public bool MatchesCurrent()
        {
            return Equals(Capture(_primaryPath));
        }

        public bool Equals(ProjectSidecarRevisionStamp? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;
            return PathComparer.Equals(_primaryPath, other._primaryPath) &&
                   _primary.Equals(other._primary) &&
                   _backup.Equals(other._backup);
        }

        public override bool Equals(object? obj) => Equals(obj as ProjectSidecarRevisionStamp);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = PathComparer.GetHashCode(_primaryPath);
                hash = (hash * 397) ^ _primary.GetHashCode();
                return (hash * 397) ^ _backup.GetHashCode();
            }
        }

        private static void RequireCanonicalPath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A QSDB primary path is required.", parameterName);
            if (!string.Equals(path, path.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("QSDB primary path must not contain leading or trailing whitespace.", parameterName);
        }

        private static bool IsCanonicalPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && string.Equals(path, path.Trim(), StringComparison.Ordinal);
        }

        private sealed class FileCapture : IDisposable
        {
            private readonly string _path;
            private readonly FileStream? _stream;

            private FileCapture(string path, FileStream? stream)
            {
                _path = path;
                _stream = stream;
            }

            public static FileCapture Open(string path)
            {
                FileAttributes attributes;
                try { attributes = File.GetAttributes(path); }
                catch (FileNotFoundException) { return new FileCapture(path, null); }
                catch (DirectoryNotFoundException) { return new FileCapture(path, null); }
                RequireRegularSidecar(attributes);

                try
                {
                    var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        // Recheck path identity after opening so a redirected member cannot
                        // gain digest authority through a pre-open attribute race.
                        RequireRegularSidecar(File.GetAttributes(path));
                        if (stream.Length > MaxSidecarBytes)
                            throw new InvalidDataException("QS3D sidecar exceeds the bounded revision-check size.");
                        return new FileCapture(path, stream);
                    }
                    catch
                    {
                        stream.Dispose();
                        throw;
                    }
                }
                catch (FileNotFoundException) { return new FileCapture(path, null); }
                catch (DirectoryNotFoundException) { return new FileCapture(path, null); }
            }

            public FileRevision CaptureStableRevision()
            {
                if (_stream == null) return FileRevision.Missing;
                var length = _stream.Length;
                var first = ComputeDigest(_stream);
                var second = ComputeDigest(_stream);
                if (_stream.Length != length || !SameDigest(first, second))
                    throw new IOException("QS3D sidecar changed while its revision was being captured.");
                return new FileRevision(length, first);
            }

            public void EnsurePresenceUnchanged()
            {
                if (_stream != null)
                {
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(_path); }
                    catch (FileNotFoundException)
                    {
                        throw new IOException("QS3D sidecar changed while its pair revision was being captured.");
                    }
                    catch (DirectoryNotFoundException)
                    {
                        throw new IOException("QS3D sidecar changed while its pair revision was being captured.");
                    }

                    RequireRegularSidecar(attributes);
                    if (_stream.Length > MaxSidecarBytes)
                        throw new IOException("QS3D sidecar changed while its pair revision was being captured.");
                    return;
                }

                if (File.Exists(_path) || Directory.Exists(_path))
                    throw new IOException("QS3D sidecar pair changed while its revision was being captured.");
            }

            public void Dispose() => _stream?.Dispose();

            private static void RequireRegularSidecar(FileAttributes attributes)
            {
                if ((attributes & FileAttributes.Directory) != 0)
                    throw new InvalidDataException("QS3D sidecar path resolves to a directory.");
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("QS3D sidecar path must not be a redirected or reparse-point file.");
            }

            private static byte[] ComputeDigest(FileStream stream)
            {
                stream.Position = 0L;
                using (var sha = SHA256.Create()) return sha.ComputeHash(stream);
            }

            private static bool SameDigest(byte[] left, byte[] right)
            {
                if (left.Length != right.Length) return false;
                var difference = 0;
                for (var index = 0; index < left.Length; index++) difference |= left[index] ^ right[index];
                return difference == 0;
            }
        }

        private sealed class FileRevision : IEquatable<FileRevision>
        {
            public static readonly FileRevision Missing = new FileRevision();
            private readonly long _length;
            private readonly byte[] _digest;

            private FileRevision()
            {
                Exists = false;
                _digest = Array.Empty<byte>();
            }

            public FileRevision(long length, byte[] digest)
            {
                if (length < 0L) throw new ArgumentOutOfRangeException(nameof(length));
                if (digest == null || digest.Length == 0) throw new ArgumentException("Digest is required.", nameof(digest));
                Exists = true;
                _length = length;
                _digest = (byte[])digest.Clone();
            }

            public bool Exists { get; }

            public bool Equals(FileRevision? other)
            {
                if (ReferenceEquals(this, other)) return true;
                if (other == null || Exists != other.Exists || _length != other._length || _digest.Length != other._digest.Length)
                    return false;
                var difference = 0;
                for (var index = 0; index < _digest.Length; index++) difference |= _digest[index] ^ other._digest[index];
                return difference == 0;
            }

            public override bool Equals(object? obj) => Equals(obj as FileRevision);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Exists ? 1 : 0;
                    hash = (hash * 397) ^ _length.GetHashCode();
                    for (var index = 0; index < Math.Min(4, _digest.Length); index++) hash = (hash * 397) ^ _digest[index];
                    return hash;
                }
            }
        }
    }
}
