using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
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
        private const uint ShareRead = 0x00000001;
        private const uint ShareWrite = 0x00000002;
        private const uint ShareDelete = 0x00000004;

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

            // Desired access is deliberately zero: this obtains identity metadata
            // without asking for read/write/delete access that would conflict with
            // the FileShare.None writer handle already held by ProjectFileLock.
            using (var pathHandle = CreateFileW(
                canonical,
                MetadataAccess,
                ShareRead | ShareWrite | ShareDelete,
                IntPtr.Zero,
                OpenExisting,
                NormalAttributes,
                IntPtr.Zero))
            {
                if (pathHandle.IsInvalid)
                    throw CreateIdentityIOException(role + " pathname");

                ByHandleFileInformation pathInformation;
                if (!GetFileInformationByHandle(pathHandle, out pathInformation))
                    throw CreateIdentityIOException(role + " pathname generation");

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
