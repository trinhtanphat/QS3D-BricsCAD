[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$ReleaseTag,
    [ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$PackageReleaseTag,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$SourceCommit,
    [string]$HostReferenceStatePath = $env:V26_HOST_REFERENCE_STATE,
    [string]$InstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$maxMetadataBytes = 65536
$requiredHostNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')

function Resolve-OrdinaryFile([string]$Path, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path must not be empty." }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse file: $Path" }
    $cursor = $item.Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
    return $item
}

function Resolve-OrdinaryDirectory([string]$Path, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path must not be empty." }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse directory: $Path" }
    $cursor = $item
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
    return $item
}

function Get-HeldSha256([IO.Stream]$Stream) {
    $Stream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $Stream.Position = 0 }
}

function Get-OrdinaryFileIdentity([string]$Path, [string]$Label) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $hash = Get-HeldSha256 -Stream $stream
        $current = Resolve-OrdinaryFile -Path $item.FullName -Label $Label
        if ($current.Length -ne $stream.Length -or $current.LastWriteTimeUtc.Ticks -ne $item.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its generation was being admitted."
        }
        return [pscustomobject]@{ Path=$current.FullName; Length=[long]$stream.Length; LastWriteUtcTicks=[long]$current.LastWriteTimeUtc.Ticks; Sha256=$hash }
    }
    finally { $stream.Dispose() }
}

function Normalize-JsonIdentityText([string]$JsonText) {
    if ($null -eq $JsonText) { throw 'V26 admitted JSON identity document is null.' }
    if ($JsonText.Length -gt 0 -and [int][char]$JsonText[0] -eq 0xFEFF) {
        if ($JsonText.Length -gt 1 -and [int][char]$JsonText[1] -eq 0xFEFF) { throw 'V26 admitted JSON identity document contains multiple leading BOM markers.' }
        return $JsonText.Substring(1)
    }
    return $JsonText
}

function Get-JsonPropertyOccurrenceCount([string]$JsonText, [string]$PropertyName) {
    $JsonText = Normalize-JsonIdentityText -JsonText $JsonText
    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') {
        throw 'V26 admitted JSON identity document must have a top-level object.'
    }

    $count = 0
    $objectDepth = 0
    $arrayDepth = 0
    $i = 0
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw 'V26 admitted JSON contains an unterminated string token.' }
            $tokenEnd = $i
            $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw 'V26 admitted JSON contains malformed property encoding.' }
                if ([string]::Equals($decodedPropertyName, $PropertyName, [StringComparison]::OrdinalIgnoreCase)) { $count++ }
            }
            $i = $tokenEnd + 1
            continue
        }

        switch ($ch) {
            '{' { $objectDepth++ }
            '}' { $objectDepth--; if ($objectDepth -lt 0) { throw 'V26 admitted JSON has invalid object nesting.' } }
            '[' { $arrayDepth++ }
            ']' { $arrayDepth--; if ($arrayDepth -lt 0) { throw 'V26 admitted JSON has invalid array nesting.' } }
        }
        $i++
    }
    if ($objectDepth -ne 0 -or $arrayDepth -ne 0) { throw 'V26 admitted JSON has unbalanced container nesting.' }
    return $count
}

function Get-JsonTopLevelArrayObjectTexts([string]$JsonText, [string]$ArrayPropertyName, [string]$Label) {
    $JsonText = Normalize-JsonIdentityText -JsonText $JsonText
    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') { throw "$Label must have a top-level object." }

    $objectDepth = 0
    $arrayDepth = 0
    $arrayStart = -1
    $i = 0
    while ($i -lt $JsonText.Length -and $arrayStart -lt 0) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token." }
            $tokenEnd = $i
            $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw "$Label contains malformed property encoding." }
                if ([string]::Equals($decodedPropertyName, $ArrayPropertyName, [StringComparison]::OrdinalIgnoreCase)) {
                    $valueStart = $lookahead + 1
                    while ($valueStart -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$valueStart])) { $valueStart++ }
                    if ($valueStart -ge $JsonText.Length -or $JsonText[$valueStart] -ne '[') { throw "$Label property '$ArrayPropertyName' must be an array." }
                    $arrayStart = $valueStart
                    break
                }
            }
            $i = $tokenEnd + 1
            continue
        }
        switch ($ch) {
            '{' { $objectDepth++ }
            '}' { $objectDepth-- }
            '[' { $arrayDepth++ }
            ']' { $arrayDepth-- }
        }
        $i++
    }
    if ($arrayStart -lt 0) { throw "$Label is missing top-level array property '$ArrayPropertyName'." }

    $items = [Collections.Generic.List[string]]::new()
    $arrayDepth = 0
    $objectDepth = 0
    $objectStart = -1
    $i = $arrayStart
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token in '$ArrayPropertyName'." }
            $i++
            continue
        }
        switch ($ch) {
            '[' { $arrayDepth++ }
            ']' {
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { return @($items) }
                $arrayDepth--
                if ($arrayDepth -lt 0) { throw "$Label has invalid array nesting in '$ArrayPropertyName'." }
            }
            '{' {
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { $objectStart = $i }
                $objectDepth++
            }
            '}' {
                $objectDepth--
                if ($objectDepth -lt 0) { throw "$Label has invalid object nesting in '$ArrayPropertyName'." }
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0 -and $objectStart -ge 0) {
                    $items.Add($JsonText.Substring($objectStart, $i - $objectStart + 1))
                    $objectStart = -1
                }
            }
        }
        $i++
    }
    throw "$Label array property '$ArrayPropertyName' is unterminated."
}

function Assert-JsonPropertyCounts([string]$JsonText, [hashtable]$ExpectedPropertyCounts, [string]$Label) {
    foreach ($propertyName in @($ExpectedPropertyCounts.Keys)) {
        $expectedCount = [int]$ExpectedPropertyCounts[$propertyName]
        $actualCount = Get-JsonPropertyOccurrenceCount -JsonText $JsonText -PropertyName $propertyName
        if ($actualCount -ne $expectedCount) { throw "$Label must contain exactly $expectedCount top-level $propertyName properties; found $actualCount." }
    }
}

function Assert-JsonArrayObjectPropertyCounts([string]$JsonText, [string]$ArrayPropertyName, [int]$ExpectedObjectCount, [hashtable]$ExpectedPropertyCounts, [string]$Label) {
    $objects = @(Get-JsonTopLevelArrayObjectTexts -JsonText $JsonText -ArrayPropertyName $ArrayPropertyName -Label $Label)
    if ($objects.Count -ne $ExpectedObjectCount) { throw "$Label property '$ArrayPropertyName' must contain exactly $ExpectedObjectCount object records; found $($objects.Count)." }
    for ($recordIndex = 0; $recordIndex -lt $objects.Count; $recordIndex++) {
        Assert-JsonPropertyCounts -JsonText $objects[$recordIndex] -ExpectedPropertyCounts $ExpectedPropertyCounts -Label "$Label $ArrayPropertyName[$recordIndex]"
    }
}

function Read-StrictUtf8Json([string]$Path, [string]$Label, [hashtable]$ExpectedPropertyCounts, [string]$ArrayPropertyName, [int]$ExpectedArrayObjectCount, [hashtable]$ExpectedArrayObjectPropertyCounts) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    if ($item.Length -gt $maxMetadataBytes) { throw "$Label exceeds the $maxMetadataBytes-byte safety limit." }
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label ended before its held length was read." }
            $offset += $read
        }
        $current = Resolve-OrdinaryFile -Path $item.FullName -Label $Label
        if ($current.Length -ne $stream.Length -or $current.LastWriteTimeUtc.Ticks -ne $item.LastWriteTimeUtc.Ticks) { throw "$Label changed while its generation was being admitted." }
        try { $text = $strictUtf8.GetString($bytes) }
        catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
        $text = Normalize-JsonIdentityText -JsonText $text
        Assert-JsonPropertyCounts -JsonText $text -ExpectedPropertyCounts $ExpectedPropertyCounts -Label $Label
        if (-not [string]::IsNullOrWhiteSpace($ArrayPropertyName)) {
            Assert-JsonArrayObjectPropertyCounts -JsonText $text -ArrayPropertyName $ArrayPropertyName -ExpectedObjectCount $ExpectedArrayObjectCount -ExpectedPropertyCounts $ExpectedArrayObjectPropertyCounts -Label $Label
        }
        try { return $text | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "$Label JSON is invalid: $($_.Exception.Message)" }
    }
    finally { $stream.Dispose() }
}

if (-not ('Qs3dProvenanceGenerationNative' -as [type])) {
    Add-Type -Language CSharp -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class Qs3dProvenanceGenerationNative
{
    private const uint DeleteAccess = 0x00010000;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileShareRead = 0x00000001;
    private const uint CreateNew = 1;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileAttributeReparsePoint = 0x00000400;
    private const uint FileBegin = 0;

    private enum FileInfoByHandleClass
    {
        FileRenameInfo = 3,
        FileDispositionInfo = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime { public uint Low; public uint High; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public NativeFileTime CreationTime;
        public NativeFileTime LastAccessTime;
        public NativeFileTime LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileInformationByHandle")]
    private static extern bool SetFileInformationByHandleBuffer(
        SafeFileHandle file, FileInfoByHandleClass informationClass,
        IntPtr information, uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle file, byte[] buffer, uint bytesToWrite,
        out uint bytesWritten, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        SafeFileHandle file, byte[] buffer, uint bytesToRead,
        out uint bytesRead, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushFileBuffers(SafeFileHandle file);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFilePointerEx(
        SafeFileHandle file, long distanceToMove, out long newFilePointer, uint moveMethod);

    private static ByHandleFileInformation Information(SafeFileHandle handle)
    {
        if (handle == null || handle.IsInvalid || handle.IsClosed)
            throw new InvalidOperationException("Owned provenance generation handle is not open.");
        ByHandleFileInformation information;
        if (!GetFileInformationByHandle(handle, out information))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFileInformationByHandle failed for owned provenance generation.");
        if ((information.FileAttributes & FileAttributeReparsePoint) != 0)
            throw new InvalidOperationException("Owned provenance generation resolved to a reparse-point file.");
        return information;
    }

    private static string Identity(ByHandleFileInformation information)
    {
        return string.Format("{0:X8}:{1:X8}{2:X8}", information.VolumeSerialNumber, information.FileIndexHigh, information.FileIndexLow);
    }

    public static SafeFileHandle CreateOwnedProvenanceGeneration(string path, byte[] bytes)
    {
        var handle = CreateFileW(
            path,
            GenericRead | GenericWrite | DeleteAccess | FileReadAttributes,
            FileShareRead,
            IntPtr.Zero,
            CreateNew,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to create owned provenance generation: " + path);
        }
        try
        {
            Information(handle);
            uint written;
            if (bytes.Length > 0 && (!WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero) || written != (uint)bytes.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to write owned provenance generation.");
            if (!FlushFileBuffers(handle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to flush owned provenance generation.");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static SafeFileHandle OpenPinnedPublishedProvenanceGeneration(string path, string expectedIdentity)
    {
        var handle = CreateFileW(
            path,
            GenericRead | DeleteAccess | FileReadAttributes,
            FileShareRead,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to pin published provenance generation: " + path);
        }
        try
        {
            var information = Information(handle);
            var actualIdentity = Identity(information);
            if (!string.Equals(actualIdentity, expectedIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("Published provenance generation identity changed before pin: expected " + expectedIdentity + ", got " + actualIdentity + ".");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static byte[] ReadPinnedPublishedProvenanceBytes(SafeFileHandle handle, int expectedLength)
    {
        var information = Information(handle);
        long length = ((long)information.FileSizeHigh << 32) | information.FileSizeLow;
        if (length != expectedLength)
            throw new InvalidOperationException("Published provenance byte length changed while pinned.");
        var bytes = new byte[expectedLength];
        if (expectedLength == 0) return bytes;
        long newPosition;
        if (!SetFilePointerEx(handle, 0, out newPosition, FileBegin) || newPosition != 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to rewind pinned published provenance generation.");
        uint read;
        if (!ReadFile(handle, bytes, (uint)expectedLength, out read, IntPtr.Zero) || read != (uint)expectedLength)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read pinned published provenance generation.");
        return bytes;
    }

    public static string GetOwnedProvenanceGenerationIdentity(SafeFileHandle handle)
    {
        return Identity(Information(handle));
    }

    public static void RenameOwnedProvenanceGeneration(SafeFileHandle handle, string destinationPath, bool replaceExisting)
    {
        Information(handle);
        var nameBytes = Encoding.Unicode.GetBytes(destinationPath);
        var rootOffset = IntPtr.Size == 8 ? 8 : 4;
        var lengthOffset = rootOffset + IntPtr.Size;
        var nameOffset = lengthOffset + sizeof(uint);
        var bufferSize = checked(nameOffset + nameBytes.Length + sizeof(char));
        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            for (var i = 0; i < bufferSize; i++) Marshal.WriteByte(buffer, i, 0);
            Marshal.WriteByte(buffer, 0, replaceExisting ? (byte)1 : (byte)0);
            Marshal.WriteIntPtr(buffer, rootOffset, IntPtr.Zero);
            Marshal.WriteInt32(buffer, lengthOffset, nameBytes.Length);
            Marshal.Copy(nameBytes, 0, IntPtr.Add(buffer, nameOffset), nameBytes.Length);
            if (!SetFileInformationByHandleBuffer(handle, FileInfoByHandleClass.FileRenameInfo, buffer, (uint)bufferSize))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetFileInformationByHandle(FileRenameInfo) failed for owned provenance generation.");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    public static void RemoveOwnedProvenanceGeneration(SafeFileHandle handle)
    {
        Information(handle);
        var buffer = Marshal.AllocHGlobal(1);
        try
        {
            Marshal.WriteByte(buffer, 0, 1);
            if (!SetFileInformationByHandleBuffer(handle, FileInfoByHandleClass.FileDispositionInfo, buffer, 1))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetFileInformationByHandle(FileDispositionInfo) failed for owned provenance generation.");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
}
'@
}

function New-OwnedProvenanceGeneration([string]$Path, [byte[]]$Bytes, [string]$Label) {
    if (Test-Path -LiteralPath $Path) { throw "Refusing to reuse V26 provenance staging path: $Path" }
    $parentPath = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Path))
    $null = Resolve-OrdinaryDirectory -Path $parentPath -Label "$Label parent"
    $handle = [Qs3dProvenanceGenerationNative]::CreateOwnedProvenanceGeneration([IO.Path]::GetFullPath($Path), $Bytes)
    try {
        $identity = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($handle)
        return [pscustomobject]@{ Handle=$handle; Identity=$identity; Path=[IO.Path]::GetFullPath($Path); Label=$Label }
    }
    catch { $handle.Dispose(); throw }
}

function Get-OwnedProvenanceGenerationIdentity($Generation) {
    return [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($Generation.Handle)
}

function Open-PinnedPublishedProvenanceGeneration([string]$Path, [string]$ExpectedIdentity) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $handle = [Qs3dProvenanceGenerationNative]::OpenPinnedPublishedProvenanceGeneration($fullPath, $ExpectedIdentity)
    try {
        $identity = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($handle)
        return [pscustomobject]@{ Handle=$handle; Identity=$identity; Path=$fullPath; Label='V26 published provenance generation' }
    }
    catch { $handle.Dispose(); throw }
}

function Assert-PinnedPublishedProvenanceBytes($Generation, [byte[]]$ExpectedBytes) {
    $currentIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $Generation
    if (-not [string]::Equals($currentIdentity, $Generation.Identity, [StringComparison]::Ordinal)) { throw "$($Generation.Label) identity changed while pinned: expected $($Generation.Identity), got $currentIdentity" }
    $actualBytes = [Qs3dProvenanceGenerationNative]::ReadPinnedPublishedProvenanceBytes($Generation.Handle, $ExpectedBytes.Length)
    if ($actualBytes.Length -ne $ExpectedBytes.Length) { throw "$($Generation.Label) byte length changed while pinned." }
    for ($i = 0; $i -lt $ExpectedBytes.Length; $i++) { if ($actualBytes[$i] -ne $ExpectedBytes[$i]) { throw "$($Generation.Label) bytes differ from the staged provenance generation." } }
}

function Remove-OwnedProvenanceGeneration($Generation) {
    $currentIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $Generation
    if (-not [string]::Equals($currentIdentity, $Generation.Identity, [StringComparison]::Ordinal)) { throw "$($Generation.Label) identity changed while owned: expected $($Generation.Identity), got $currentIdentity" }
    try { [Qs3dProvenanceGenerationNative]::RemoveOwnedProvenanceGeneration($Generation.Handle) }
    finally { $Generation.Handle.Dispose() }
}

function Close-OwnedProvenanceGeneration($Generation) {
    if ($null -ne $Generation) { $Generation.Handle.Dispose() }
}

if ([string]::IsNullOrWhiteSpace($InstallerSha256) -or $InstallerSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'V26 admitted installer SHA-256 must be canonical lowercase 64-hex.' }

$hostExpectedPropertyCounts = @{
    Version = 1
    Files = 1
}
$hostFileExpectedPropertyCounts = @{
    Name = 1
    Path = 1
    Sha256 = 1
    Length = 1
}
$hostState = Read-StrictUtf8Json -Path $HostReferenceStatePath -Label 'V26 host-reference state' -ExpectedPropertyCounts $hostExpectedPropertyCounts -ArrayPropertyName 'Files' -ExpectedArrayObjectCount $requiredHostNames.Count -ExpectedArrayObjectPropertyCounts $hostFileExpectedPropertyCounts
if ([int]$hostState.Version -ne 1) { throw 'V26 host-reference state version must be 1.' }
$hostFiles = @($hostState.Files)
if ($hostFiles.Count -ne $requiredHostNames.Count) { throw 'V26 host-reference state must contain exactly four required files.' }
$hostReferences = foreach ($name in $requiredHostNames) {
    $matches = @($hostFiles | Where-Object { [string]::Equals([string]$_.Name, $name, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "V26 host-reference state must contain exactly one $name record." }
    $record = $matches[0]
    $sha256 = [string]$record.Sha256
    $length = [long]$record.Length
    $path = [string]$record.Path
    if ($sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "V26 host-reference SHA-256 must be canonical lowercase hex for $name." }
    if ($length -le 0) { throw "V26 host-reference length must be positive for $name." }
    $actual = Get-OrdinaryFileIdentity -Path $path -Label "V26 host reference $name"
    if ($actual.Length -ne $length -or -not [string]::Equals($actual.Sha256, $sha256, [StringComparison]::Ordinal)) { throw "V26 host reference $name changed after its admitted generation was captured." }
    [ordered]@{ name = $name; length = $length; sha256 = $sha256 }
}

$zipItem = Resolve-OrdinaryFile -Path $PackageZip -Label 'V26 package ZIP'
$zipStream = [IO.File]::Open($zipItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $zipHash = Get-HeldSha256 -Stream $zipStream
    $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        $entries = @($archive.Entries | Where-Object { [string]::Equals([string]$_.FullName, 'PACKAGE-METADATA.json', [StringComparison]::Ordinal) })
        if ($entries.Count -ne 1) { throw "V26 package ZIP must contain exactly one PACKAGE-METADATA.json entry; found $($entries.Count)." }
        $entry = $entries[0]
        if ($entry.Length -gt $maxMetadataBytes) { throw 'V26 PACKAGE-METADATA.json exceeds the metadata safety limit.' }
        $entryStream = $entry.Open()
        try {
            $reader = [IO.StreamReader]::new($entryStream, $strictUtf8, $false, 4096, $true)
            try { $metadataText = $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally { $entryStream.Dispose() }
    }
    finally { $archive.Dispose() }

    $metadataText = Normalize-JsonIdentityText -JsonText $metadataText

    $metadataExpectedPropertyCounts = @{
        product = 1
        target = 1
        framework = 1
        productVersion = 1
    }
    Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts -Label 'V26 PACKAGE-METADATA.json'
    try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "V26 PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64') { throw 'V26 package product/target identity is invalid.' }
    if ([string]$metadata.framework -ne 'net8.0-windows') { throw 'V26 package framework identity is invalid.' }
    $productVersion = [string]$metadata.productVersion
    $effectivePackageTag = if ([string]::IsNullOrWhiteSpace($PackageReleaseTag)) { $ReleaseTag } else { $PackageReleaseTag }
    if (-not [string]::Equals(('v' + $productVersion), $effectivePackageTag, [StringComparison]::Ordinal)) { throw "V26 package release tag $effectivePackageTag does not match package productVersion $productVersion." }

    $provenance = [ordered]@{
        product = 'QS3D'
        target = 'BricsCAD V26 x64'
        releaseTag = $ReleaseTag
        productVersion = $productVersion
        sourceCommit = $SourceCommit.ToLowerInvariant()
        packageSha256 = $zipHash
        installerSha256 = $InstallerSha256
        hostReferences = @($hostReferences)
    }

    $outputFull = [IO.Path]::GetFullPath($OutputPath)
    $parent = Split-Path -Parent $outputFull
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    $null = Resolve-OrdinaryDirectory -Path $parent -Label 'V26 provenance output directory'
    $replaceExisting = Test-Path -LiteralPath $outputFull
    if ($replaceExisting) { $null = Resolve-OrdinaryFile -Path $outputFull -Label 'V26 provenance output' }

    $tempPath = Join-Path $parent ('.' + [IO.Path]::GetFileName($outputFull) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    $provenanceText = ($provenance | ConvertTo-Json -Depth 5) + [Environment]::NewLine
    $provenanceBytes = $strictUtf8.GetBytes($provenanceText)
    if ($provenanceBytes.Length -gt $maxMetadataBytes) { throw 'V26 candidate provenance exceeds the metadata safety limit.' }

    $tempGeneration = New-OwnedProvenanceGeneration -Path $tempPath -Bytes $provenanceBytes -Label 'V26 provenance staging generation'
    $publicationCommitted = $false
    try {
        $attemptIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration
        [Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration($tempGeneration.Handle, $outputFull, [bool]$replaceExisting)
        $renamedIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration
        if (-not [string]::Equals($renamedIdentity, $attemptIdentity, [StringComparison]::Ordinal)) { throw "V26 provenance generation identity changed during handle-owned publication: expected $attemptIdentity, got $renamedIdentity" }
        Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration -ExpectedBytes $provenanceBytes
        $publicationCommitted = $true
    }
    finally {
        if ($publicationCommitted) { Close-OwnedProvenanceGeneration -Generation $tempGeneration }
        elseif ($null -ne $tempGeneration) { Remove-OwnedProvenanceGeneration -Generation $tempGeneration }
        $tempGeneration = $null
    }

    [pscustomobject]@{ SourceCommit = $provenance.sourceCommit; PackageSha256 = $zipHash; ProductVersion = $productVersion; InstallerSha256 = $InstallerSha256; HostReferences = @($hostReferences) }
}
finally { $zipStream.Dispose() }
