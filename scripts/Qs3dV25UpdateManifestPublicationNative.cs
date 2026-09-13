using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public sealed class Qs3dOwnedGeneration : IDisposable
{
    internal Qs3dOwnedGeneration(FileStream stream, string currentPath)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        CurrentPath = Path.GetFullPath(currentPath ?? throw new ArgumentNullException(nameof(currentPath)));
    }

    internal FileStream Stream { get; }
    public string CurrentPath { get; internal set; }
    public bool DeletePending { get; internal set; }

    public void Dispose()
    {
        Stream.Dispose();
    }
}

public static class Qs3dV25UpdateManifestPublicationNative
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint DeleteAccess = 0x00010000;
    private const uint FileShareRead = 0x00000001;
    private const uint CreateNew = 1;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeReparsePoint = 0x00000400;
    private const uint FileFlagWriteThrough = 0x80000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const int FileRenameInfo = 3;
    private const int FileDispositionInfo = 4;
    private const uint FileNameNormalized = 0x0;
    private const int ErrorAlreadyExists = 183;
    private const int ErrorFileExists = 80;

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public FileTime CreationTime;
        public FileTime LastAccessTime;
        public FileTime LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
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
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file,
        int fileInformationClass,
        IntPtr fileInformation,
        uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out ByHandleFileInformation fileInformation);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle file,
        StringBuilder filePath,
        uint filePathLength,
        uint flags);

    public static Qs3dOwnedGeneration OpenOwnedStaging(string path)
    {
        return OpenOwned(path, CreateNew, GenericRead | GenericWrite | DeleteAccess, FileAccess.ReadWrite, FileFlagWriteThrough);
    }

    public static Qs3dOwnedGeneration OpenOwnedExisting(string path)
    {
        return OpenOwned(path, OpenExisting, GenericRead | DeleteAccess, FileAccess.Read, 0);
    }

    private static Qs3dOwnedGeneration OpenOwned(
        string path,
        uint creationDisposition,
        uint desiredAccess,
        FileAccess fileAccess,
        uint additionalFlags)
    {
        string fullPath = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
        SafeFileHandle handle = CreateFileW(
            fullPath,
            desiredAccess,
            FileShareRead,
            IntPtr.Zero,
            creationDisposition,
            FileAttributeNormal | FileFlagOpenReparsePoint | additionalFlags,
            IntPtr.Zero);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "Could not acquire exclusive generation ownership: " + fullPath);
        }

        try
        {
            ByHandleFileInformation info = GetInformation(handle);
            if ((info.FileAttributes & (FileAttributeDirectory | FileAttributeReparsePoint)) != 0)
            {
                throw new IOException("Owned generation must be an ordinary non-reparse file: " + fullPath);
            }

            var stream = new FileStream(handle, fileAccess, 4096, false);
            return new Qs3dOwnedGeneration(stream, fullPath);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static void WriteOwnedGeneration(Qs3dOwnedGeneration generation, byte[] bytes)
    {
        RequireOwned(generation);
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (!generation.Stream.CanWrite) throw new IOException("Owned staging generation is not writable.");

        generation.Stream.Position = 0;
        generation.Stream.SetLength(0);
        generation.Stream.Write(bytes, 0, bytes.Length);
        generation.Stream.Flush(true);
        generation.Stream.Position = 0;
    }

    public static byte[] ReadOwnedGenerationBytes(Qs3dOwnedGeneration generation, int maximumBytes)
    {
        RequireOwned(generation);
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        long length = generation.Stream.Length;
        if (length > maximumBytes || length > int.MaxValue)
        {
            throw new IOException("Owned generation exceeds bounded read limit.");
        }

        byte[] bytes = new byte[(int)length];
        generation.Stream.Position = 0;
        int offset = 0;
        while (offset < bytes.Length)
        {
            int read = generation.Stream.Read(bytes, offset, bytes.Length - offset);
            if (read <= 0) throw new EndOfStreamException("Owned generation ended before its declared length.");
            offset += read;
        }
        if (generation.Stream.ReadByte() != -1)
        {
            throw new IOException("Owned generation changed while it was being read.");
        }
        return bytes;
    }

    public static string GetOwnedGenerationIdentity(Qs3dOwnedGeneration generation)
    {
        RequireOwned(generation);
        ByHandleFileInformation info = GetInformation(generation.Stream.SafeFileHandle);
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:X8}:{1:X8}{2:X8}",
            info.VolumeSerialNumber,
            info.FileIndexHigh,
            info.FileIndexLow);
    }

    public static string GetOwnedCurrentPath(Qs3dOwnedGeneration generation)
    {
        RequireOwned(generation);
        string actual = GetFinalPath(generation.Stream.SafeFileHandle);
        generation.CurrentPath = actual;
        return actual;
    }

    public static void AssertOwnedPath(Qs3dOwnedGeneration generation, string expectedPath)
    {
        string actual = GetOwnedCurrentPath(generation);
        string expected = Path.GetFullPath(expectedPath ?? throw new ArgumentNullException(nameof(expectedPath)));
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Owned generation path mismatch. Expected " + expected + ", got " + actual + ".");
        }
    }

    public static void PublishOwnedGeneration(
        Qs3dOwnedGeneration staging,
        string destinationPath,
        Qs3dOwnedGeneration prior,
        string backupPath)
    {
        RequireOwned(staging);
        string destination = Path.GetFullPath(destinationPath ?? throw new ArgumentNullException(nameof(destinationPath)));
        string backup = prior == null ? null : Path.GetFullPath(backupPath ?? throw new ArgumentNullException(nameof(backupPath)));

        if (prior != null)
        {
            RequireOwned(prior);
            RenameOwned(prior, backup, false);
        }

        try
        {
            RenameOwned(staging, destination, false);
        }
        catch (Exception publishError)
        {
            if (prior != null && !PathsEqual(prior.CurrentPath, destination))
            {
                try
                {
                    RenameOwned(prior, destination, false);
                }
                catch (Exception restoreError)
                {
                    throw new AggregateException(
                        "Owned staging publication failed and the prior generation could not be restored; prior generation remains held at " + prior.CurrentPath,
                        publishError,
                        restoreError);
                }
            }
            throw;
        }
    }

    public static void RollbackOwnedGeneration(
        Qs3dOwnedGeneration staging,
        Qs3dOwnedGeneration prior,
        string outputPath,
        string originalStagePath)
    {
        RequireOwned(staging);
        string output = Path.GetFullPath(outputPath ?? throw new ArgumentNullException(nameof(outputPath)));

        if (PathsEqual(staging.CurrentPath, output))
        {
            string quarantine = FindRollbackName(originalStagePath);
            RenameOwned(staging, quarantine, false);
        }

        if (prior != null)
        {
            RequireOwned(prior);
            if (!PathsEqual(prior.CurrentPath, output))
            {
                RenameOwned(prior, output, false);
            }
            AssertOwnedPath(prior, output);
        }

        DeleteOwnedGeneration(staging);
    }

    public static void DeleteOwnedGeneration(Qs3dOwnedGeneration generation)
    {
        RequireOwned(generation);
        if (generation.DeletePending) return;

        IntPtr buffer = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(buffer, 1);
            if (!SetFileInformationByHandle(
                    generation.Stream.SafeFileHandle,
                    FileDispositionInfo,
                    buffer,
                    sizeof(int)))
            {
                throw LastWin32("Could not mark owned generation for deletion: " + generation.CurrentPath);
            }
            generation.DeletePending = true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void RenameOwned(Qs3dOwnedGeneration generation, string destinationPath, bool replaceIfExists)
    {
        RequireOwned(generation);
        string destination = Path.GetFullPath(destinationPath ?? throw new ArgumentNullException(nameof(destinationPath)));
        byte[] nameBytes = Encoding.Unicode.GetBytes(destination);
        int rootOffset = IntPtr.Size == 8 ? 8 : 4;
        int fileNameLengthOffset = rootOffset + IntPtr.Size;
        int fileNameOffset = fileNameLengthOffset + sizeof(int);
        int total = checked(fileNameOffset + nameBytes.Length + 2);
        IntPtr buffer = Marshal.AllocHGlobal(total);
        try
        {
            for (int i = 0; i < total; i++) Marshal.WriteByte(buffer, i, 0);
            Marshal.WriteByte(buffer, 0, replaceIfExists ? (byte)1 : (byte)0);
            Marshal.WriteIntPtr(buffer, rootOffset, IntPtr.Zero);
            Marshal.WriteInt32(buffer, fileNameLengthOffset, nameBytes.Length);
            Marshal.Copy(nameBytes, 0, IntPtr.Add(buffer, fileNameOffset), nameBytes.Length);
            if (!SetFileInformationByHandle(
                    generation.Stream.SafeFileHandle,
                    FileRenameInfo,
                    buffer,
                    (uint)total))
            {
                throw LastWin32("Could not rename owned generation to " + destination);
            }
            generation.CurrentPath = destination;
            AssertOwnedPath(generation, destination);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string FindRollbackName(string originalStagePath)
    {
        string basePath = Path.GetFullPath(originalStagePath ?? throw new ArgumentNullException(nameof(originalStagePath)));
        string directory = Path.GetDirectoryName(basePath);
        string fileName = Path.GetFileName(basePath);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            string candidate = Path.Combine(directory, fileName + ".rollback-" + Guid.NewGuid().ToString("N"));
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        throw new IOException("Could not allocate a unique rollback quarantine pathname.");
    }

    private static ByHandleFileInformation GetInformation(SafeFileHandle handle)
    {
        ByHandleFileInformation info;
        if (!GetFileInformationByHandle(handle, out info))
        {
            throw LastWin32("Could not read owned generation identity.");
        }
        return info;
    }

    private static string GetFinalPath(SafeFileHandle handle)
    {
        int capacity = 512;
        while (capacity <= 32768)
        {
            var buffer = new StringBuilder(capacity);
            uint result = GetFinalPathNameByHandleW(handle, buffer, (uint)capacity, FileNameNormalized);
            if (result == 0) throw LastWin32("Could not resolve owned generation final path.");
            if (result < capacity)
            {
                string path = buffer.ToString();
                if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                {
                    path = @"\\" + path.Substring(8);
                }
                else if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(4);
                }
                return Path.GetFullPath(path);
            }
            capacity = checked((int)result + 1);
        }
        throw new IOException("Owned generation final path exceeds safety limit.");
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static void RequireOwned(Qs3dOwnedGeneration generation)
    {
        if (generation == null) throw new ArgumentNullException(nameof(generation));
        if (generation.Stream.SafeFileHandle.IsClosed || generation.Stream.SafeFileHandle.IsInvalid)
        {
            throw new ObjectDisposedException(nameof(Qs3dOwnedGeneration));
        }
    }

    private static Win32Exception LastWin32(string message)
    {
        return new Win32Exception(Marshal.GetLastWin32Error(), message);
    }
}
