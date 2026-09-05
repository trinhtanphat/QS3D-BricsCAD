using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace QS3D.Core.Persistence
{
    /// <summary>
    /// Fail-closed path checks shared by persistence write primitives.
    /// Persistence authority is expressed by the canonical path, so an existing
    /// symbolic-link/reparse-point component must never redirect a write or lock
    /// to a different filesystem object.
    /// </summary>
    internal static class PersistencePathSafety
    {
        private const uint OpenExisting = 3;
        private const uint MetadataAccess = 0;
        private const uint NormalAttributes = 0x00000080;
        private const uint FileFlagOpenReparsePoint = 0x00200000;
        private const uint ShareRead = 0x00000001;
        private const uint ShareWrite = 0x00000002;
        private const uint ShareDelete = 0x00000004;
        private const uint FileNameNormalized = 0x0;
        private const int InitialFinalPathCapacity = 512;

        public static void RequireNonRedirected(string fullPath, string role)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("A persistence path is required.", nameof(fullPath));

            var canonical = Path.GetFullPath(fullPath);
            var root = Path.GetPathRoot(canonical);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidDataException("QS3D could not resolve the persistence path root.");

            var separators = Path.AltDirectorySeparatorChar == Path.DirectorySeparatorChar
                ? new[] { Path.DirectorySeparatorChar }
                : new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var remainder = canonical.Substring(root.Length);
            var components = remainder.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var index = 0; index < components.Length; index++)
            {
                current = Path.Combine(current, components[index]);
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(current);
                }
                catch (FileNotFoundException)
                {
                    // Once a component is missing, every deeper component is also
                    // non-existent at this observation. Callers recheck immediately
                    // before destructive filesystem operations.
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("QS3D refused a redirected or reparse-point " + role + " path.");
            }
        }

        /// <summary>
        /// Binds a supported-product Windows persistence pathname to the exact file
        /// generation already held by <paramref name="openedStream"/>. A pathname-only
        /// recheck after open is insufficient because it can observe a replacement
        /// generation rather than the stream that actually owns the exclusive lock.
        /// </summary>
        public static void RequireExclusiveOpenStillBound(FileStream openedStream, string fullPath, string role)
        {
            if (openedStream == null) throw new ArgumentNullException(nameof(openedStream));
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("A persistence path is required.", nameof(fullPath));
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // QS3D's supported product boundary is Windows x64. Do not silently
                // downgrade an exact-generation safety assertion on another platform.
                throw new PlatformNotSupportedException(
                    "QS3D exact persistence lock generation validation requires Windows.");
            }

            var canonical = Path.GetFullPath(fullPath);
            RequireNonRedirected(canonical, role);

            ByHandleFileInformation heldInformation;
            if (!GetFileInformationByHandle(openedStream.SafeFileHandle, out heldInformation))
                throw CreateIdentityIOException("held " + role + " stream");
            RequireCanonicalFinalPath(canonical, openedStream.SafeFileHandle, "held " + role + " stream");

            // Desired access is deliberately zero: this obtains identity metadata
            // without asking for read/write/delete access that would conflict with
            // the FileShare.None writer handle already held by ProjectFileLock.
            // OPEN_REPARSE_POINT prevents the final component from being followed by
            // this verification open; any such redirect is rejected from handle
            // metadata before generation identity can be accepted.
            using (var pathHandle = CreateFileW(
                canonical,
                MetadataAccess,
                ShareRead | ShareWrite | ShareDelete,
                IntPtr.Zero,
                OpenExisting,
                NormalAttributes | FileFlagOpenReparsePoint,
                IntPtr.Zero))
            {
                if (pathHandle.IsInvalid)
                    throw CreateIdentityIOException(role + " pathname");

                ByHandleFileInformation pathInformation;
                if (!GetFileInformationByHandle(pathHandle, out pathInformation))
                    throw CreateIdentityIOException(role + " pathname generation");
                if ((((FileAttributes)pathInformation.FileAttributes) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("QS3D refused a redirected or reparse-point " + role + " pathname generation.");

                RequireCanonicalFinalPath(canonical, pathHandle, role + " pathname generation");

                // Re-check redirect attributes after the metadata handle has been
                // opened, then compare immutable filesystem identity of both handles.
                RequireNonRedirected(canonical, role);
                if (heldInformation.VolumeSerialNumber != pathInformation.VolumeSerialNumber ||
                    heldInformation.FileIndexHigh != pathInformation.FileIndexHigh ||
                    heldInformation.FileIndexLow != pathInformation.FileIndexLow)
                {
                    throw new IOException(
                        "QS3D refused a " + role + " path whose filesystem generation changed during acquisition.");
                }
            }
        }

        private static void RequireCanonicalFinalPath(string canonical, SafeFileHandle handle, string subject)
        {
            var actual = ReadFinalPath(handle, subject);
            var expected = NormalizeFinalPath(canonical);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException(
                    "QS3D refused a " + subject + " whose resolved filesystem path differs from the canonical persistence path.");
            }
        }

        private static string ReadFinalPath(SafeFileHandle handle, string subject)
        {
            var capacity = InitialFinalPathCapacity;
            while (true)
            {
                var buffer = new StringBuilder(capacity);
                var length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, FileNameNormalized);
                if (length == 0)
                    throw CreateIdentityIOException(subject + " final pathname");
                if (length < buffer.Capacity)
                    return NormalizeFinalPath(buffer.ToString());
                if (length > int.MaxValue - 1)
                    throw new IOException("QS3D could not safely size the resolved persistence pathname buffer.");
                capacity = checked((int)length + 1);
            }
        }

        private static string NormalizeFinalPath(string path)
        {
            var normalized = path;
            const string uncPrefix = @"\\?\UNC\";
            const string extendedPrefix = @"\\?\";
            if (normalized.StartsWith(uncPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = @"\\" + normalized.Substring(uncPrefix.Length);
            else if (normalized.StartsWith(extendedPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(extendedPrefix.Length);

            normalized = normalized.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.GetFullPath(normalized).TrimEnd(Path.DirectorySeparatorChar);
        }

        private static IOException CreateIdentityIOException(string subject)
        {
            var code = Marshal.GetLastWin32Error();
            return new IOException(
                "QS3D could not verify filesystem identity for the " + subject + ".",
                new Win32Exception(code));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandleW(
            SafeFileHandle file,
            StringBuilder filePath,
            uint filePathLength,
            uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(
            SafeFileHandle file,
            out ByHandleFileInformation fileInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct ByHandleFileInformation
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }
    }
}
