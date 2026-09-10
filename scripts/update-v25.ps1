[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$ManifestUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),

    [ValidateSet('OnCommand', 'OnStartup')]
    [string]$LoadMode = 'OnCommand',

    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [string[]]$AllowedPackageHost,

    [ValidateRange(1, 512)]
    [int]$MaxPackageSizeMB = 256,

    [ValidateRange(1, 2048)]
    [int]$MaxExpandedPackageSizeMB = 512,

    [ValidateRange(1, 20000)]
    [int]$MaxArchiveEntries = 4096,

    [switch]$AllowSameVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not ('Qs3dNativeFile' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public sealed class Qs3dHeldFile : IDisposable
{
    public SafeFileHandle Handle { get; private set; }
    public string FinalPath { get; private set; }

    internal Qs3dHeldFile(SafeFileHandle handle, string finalPath)
    {
        Handle = handle;
        FinalPath = finalPath;
    }

    public void Dispose()
    {
        if (Handle != null)
        {
            Handle.Dispose();
            Handle = null;
        }
    }
}

public static class Qs3dNativeFile
{
    private const uint GENERIC_READ = 0x80000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
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
        out BY_HANDLE_FILE_INFORMATION information);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle file,
        StringBuilder path,
        uint pathLength,
        uint flags);

    public static Qs3dHeldFile OpenOrdinaryReadHeld(string path)
    {
        SafeFileHandle handle = CreateFileW(
            path,
            GENERIC_READ,
            FILE_SHARE_READ,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Could not atomically open the updater installer without following a reparse point.");
        }

        try
        {
            BY_HANDLE_FILE_INFORMATION information;
            if (!GetFileInformationByHandle(handle, out information))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not inspect the held updater installer.");
            }
            if ((information.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
            {
                throw new IOException("Updater installer is a directory, not an ordinary file.");
            }
            if ((information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
            {
                throw new IOException("Updater installer is a reparse point.");
            }

            StringBuilder resolved = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandleW(handle, resolved, (uint)resolved.Capacity, 0);
            if (length == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve the held updater installer path.");
            }
            if (length >= (uint)resolved.Capacity)
            {
                throw new PathTooLongException("Held updater installer resolved path exceeded the supported Win32 path buffer.");
            }

            Qs3dHeldFile held = new Qs3dHeldFile(handle, resolved.ToString());
            handle = null;
            return held;
        }
        finally
        {
            if (handle != null) handle.Dispose();
        }
    }
}
'@
}

$SignedPayloadNames = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1'
)
$UpdateMutexPrefix = 'Global\QS3D-BricsCAD-V25-Update-'

function Enter-Qs3dUpdateMutex {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try { $sid = [string]$identity.User.Value }
    finally { $identity.Dispose() }
    if ([string]::IsNullOrWhiteSpace($sid)) { throw 'Could not resolve the current Windows user SID for QS3D update serialization.' }

    $mutexName = $UpdateMutexPrefix + $sid
    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
    $ownsMutex = $false
    try {
        try { $ownsMutex = $mutex.WaitOne(0) }
        catch [System.Threading.AbandonedMutexException] { $ownsMutex = $true }
        if (-not $ownsMutex) {
            throw 'Another QS3D install/update is already active for this Windows user. Finish that operation before starting another update.'
        }
        return $mutex
    }
    catch {
        $mutex.Dispose()
        throw
    }
}

function Exit-Qs3dUpdateMutex {
    param([System.Threading.Mutex]$Mutex)
    if ($null -eq $Mutex) { return }
    try { $Mutex.ReleaseMutex() }
    finally { $Mutex.Dispose() }
}

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    return ($Thumbprint.Replace(' ', '').ToUpperInvariant())
}

function Convert-ToSafeHttpsUri {
    param([string]$Value, [string]$Label)
    $uri = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri)) { throw "$Label is not an absolute URI." }
    if ($uri.Scheme -ne [Uri]::UriSchemeHttps) { throw "$Label must use HTTPS." }
    if ([string]::IsNullOrWhiteSpace($uri.Host)) { throw "$Label must include a host." }
    if ($uri.UserInfo) { throw "$Label must not contain embedded credentials." }
    return $uri
}

function Invoke-BoundedHttpsDownload {
    param(
        [Parameter(Mandatory = $true)]
        [Uri]$Address,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,
        [Parameter(Mandatory = $true)]
        [int64]$MaxBytes,
        [Parameter(Mandatory = $true)]
        [ValidateRange(1000, 600000)]
        [int]$TimeoutMilliseconds,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if ($null -eq $Address -or $Address.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($Address.Host)) {
        throw "$Label download URI must be an absolute HTTPS URI."
    }
    if ($Address.UserInfo) { throw "$Label download URI must not contain embedded credentials." }
    if ($MaxBytes -le 0) { throw "$Label download byte limit must be positive." }

    $request = [System.Net.WebRequest]::CreateHttp($Address.AbsoluteUri)
    $request.Method = 'GET'
    $request.UserAgent = 'QS3D-BricsCAD-V25-Updater'
    $request.Accept = '*/*'
    $request.AllowAutoRedirect = $true
    $request.MaximumAutomaticRedirections = 5
    $request.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
    $request.Timeout = $TimeoutMilliseconds
    $request.ReadWriteTimeout = $TimeoutMilliseconds

    $response = $null
    $input = $null
    $output = $null
    $completed = $false
    try {
        $response = [System.Net.HttpWebResponse]$request.GetResponse()
        if ($response.StatusCode -ne [System.Net.HttpStatusCode]::OK) {
            throw "$Label download returned HTTP $([int]$response.StatusCode)."
        }
        $finalUri = $response.ResponseUri
        if ($null -eq $finalUri -or $finalUri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($finalUri.Host) -or $finalUri.UserInfo) {
            throw "$Label download redirected to a non-HTTPS or credential-bearing URI."
        }
        if ($response.ContentLength -gt $MaxBytes) {
            throw "$Label download Content-Length $($response.ContentLength) exceeds the allowed maximum $MaxBytes bytes."
        }

        $input = $response.GetResponseStream()
        if ($null -eq $input) { throw "$Label download response body was empty." }
        $output = [System.IO.File]::Open($DestinationPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        $buffer = New-Object byte[] 65536
        [int64]$total = 0
        while ($true) {
            $read = $input.Read($buffer, 0, $buffer.Length)
            if ($read -le 0) { break }
            if ($total -gt ($MaxBytes - [int64]$read)) {
                throw "$Label download exceeded the allowed maximum $MaxBytes bytes while streaming."
            }
            $output.Write($buffer, 0, $read)
            $total += [int64]$read
        }
        if ($total -le 0) { throw "$Label download returned an empty response body." }
        $output.Flush()
        $completed = $true
        return $total
    }
    finally {
        if ($output) { $output.Dispose() }
        if ($input) { $input.Dispose() }
        if ($response) { $response.Dispose() }
        if (-not $completed -and (Test-Path -LiteralPath $DestinationPath)) {
            Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Require-ManifestProperty {
    param($Manifest, [string]$Name)
    $property = $Manifest.PSObject.Properties[$Name]
    if (-not $property) { throw "Update manifest is missing '$Name'." }
    return $property.Value
}

function Convert-ToStrictSemVer {
    param([string]$Value, [string]$Label)

    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is missing." }
    $text = $Value.Trim()
    $match = [regex]::Match(
        $text,
        '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw "$Label is not strict SemVer: $text" }

    $components = @()
    foreach ($index in 1..3) {
        $parsed = 0
        if (-not [int]::TryParse($match.Groups[$index].Value, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
            throw "$Label numeric component is outside the supported range: $text"
        }
        $components += $parsed
    }

    [string[]]$prerelease = @()
    if ($match.Groups[4].Success) {
        $prerelease = @($match.Groups[4].Value.Split('.'))
        foreach ($identifier in $prerelease) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') {
                throw "$Label has a numeric prerelease identifier with a leading zero: $text"
            }
        }
    }

    return [pscustomobject]@{
        Text = $text
        Major = [int]$components[0]
        Minor = [int]$components[1]
        Patch = [int]$components[2]
        Prerelease = [string[]]$prerelease
    }
}

function Compare-StrictSemVer {
    param($Left, $Right)

    foreach ($property in @('Major', 'Minor', 'Patch')) {
        $comparison = ([int]$Left.$property).CompareTo([int]$Right.$property)
        if ($comparison -ne 0) { return $comparison }
    }

    $leftPre = @($Left.Prerelease)
    $rightPre = @($Right.Prerelease)
    if ($leftPre.Count -eq 0 -and $rightPre.Count -eq 0) { return 0 }
    if ($leftPre.Count -eq 0) { return 1 }
    if ($rightPre.Count -eq 0) { return -1 }

    $count = [Math]::Min($leftPre.Count, $rightPre.Count)
    for ($index = 0; $index -lt $count; $index++) {
        $leftIdentifier = [string]$leftPre[$index]
        $rightIdentifier = [string]$rightPre[$index]
        $leftNumeric = $leftIdentifier -match '^[0-9]+$'
        $rightNumeric = $rightIdentifier -match '^[0-9]+$'

        if ($leftNumeric -and $rightNumeric) {
            $lengthComparison = $leftIdentifier.Length.CompareTo($rightIdentifier.Length)
            if ($lengthComparison -ne 0) { return $lengthComparison }
            $numericComparison = [string]::CompareOrdinal($leftIdentifier, $rightIdentifier)
            if ($numericComparison -ne 0) { return $numericComparison }
            continue
        }
        if ($leftNumeric -ne $rightNumeric) { return $(if ($leftNumeric) { -1 } else { 1 }) }

        $lexicalComparison = [string]::CompareOrdinal($leftIdentifier, $rightIdentifier)
        if ($lexicalComparison -ne 0) { return $lexicalComparison }
    }

    return $leftPre.Count.CompareTo($rightPre.Count)
}

function Get-OfficialGitHubReleaseSnapshot {
    param([Uri]$ManifestAddress)

    if (-not [string]::Equals($ManifestAddress.Host, 'github.com', [StringComparison]::OrdinalIgnoreCase)) { return $null }
    $prefix = '/trinhtanphat/QS3D-BricsCAD/releases/download/'
    $path = $ManifestAddress.AbsolutePath
    if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $null }
    if ($ManifestAddress.UserInfo -or $ManifestAddress.Query -or $ManifestAddress.Fragment) {
        throw 'Official QS3D GitHub manifest URI must not contain credentials, query, or fragment.'
    }

    $remainder = $path.Substring($prefix.Length)
    $slash = $remainder.IndexOf('/')
    if ($slash -le 0 -or $slash -eq ($remainder.Length - 1) -or $remainder.IndexOf('/', $slash + 1) -ge 0) {
        throw 'Official QS3D GitHub manifest URI has an invalid release-download path.'
    }

    try {
        $tag = [Uri]::UnescapeDataString($remainder.Substring(0, $slash))
        $asset = [Uri]::UnescapeDataString($remainder.Substring($slash + 1))
    }
    catch [UriFormatException] {
        throw 'Official QS3D GitHub manifest URI contains invalid escaping.'
    }
    if (-not [string]::Equals($asset, 'QS3D-BricsCAD-V25.update.json', [StringComparison]::Ordinal)) {
        throw "Official QS3D GitHub manifest asset must be QS3D-BricsCAD-V25.update.json, got '$asset'."
    }
    if ($tag.Length -le 1 -or -not [string]::Equals($tag.Substring(0, 1), 'v', [StringComparison]::Ordinal)) {
        throw "Official QS3D GitHub release tag must start with lowercase v: $tag"
    }
    $productVersion = Convert-ToStrictSemVer -Value $tag.Substring(1) -Label 'Official GitHub release tag'
    return [pscustomobject]@{
        Tag = $tag
        ProductVersion = $productVersion.Text
    }
}

function Assert-OfficialGitHubPackageSnapshot {
    param([Uri]$PackageAddress, $Snapshot)

    if ($PackageAddress.Scheme -ne [Uri]::UriSchemeHttps -or
        -not [string]::Equals($PackageAddress.Host, 'github.com', [StringComparison]::OrdinalIgnoreCase) -or
        $PackageAddress.UserInfo -or $PackageAddress.Query -or $PackageAddress.Fragment) {
        throw 'Official QS3D package URI must be a credential-free HTTPS github.com URL without query or fragment.'
    }

    $prefix = '/trinhtanphat/QS3D-BricsCAD/releases/download/'
    $path = $PackageAddress.AbsolutePath
    if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Official QS3D package URI does not belong to trinhtanphat/QS3D-BricsCAD release downloads.'
    }
    $remainder = $path.Substring($prefix.Length)
    $slash = $remainder.IndexOf('/')
    if ($slash -le 0 -or $slash -eq ($remainder.Length - 1) -or $remainder.IndexOf('/', $slash + 1) -ge 0) {
        throw 'Official QS3D package URI has an invalid release-download path.'
    }

    try {
        $tag = [Uri]::UnescapeDataString($remainder.Substring(0, $slash))
        $asset = [Uri]::UnescapeDataString($remainder.Substring($slash + 1))
    }
    catch [UriFormatException] {
        throw 'Official QS3D package URI contains invalid escaping.'
    }
    if (-not [string]::Equals($tag, [string]$Snapshot.Tag, [StringComparison]::Ordinal)) {
        throw "Official QS3D package release tag '$tag' does not match scheduled release tag '$($Snapshot.Tag)'."
    }
    if (-not [string]::Equals($asset, 'QS3D-BricsCAD-V25.zip', [StringComparison]::Ordinal)) {
        throw "Official QS3D package asset must be QS3D-BricsCAD-V25.zip, got '$asset'."
    }
}

function Read-InstalledVersion {
    param([string]$Directory)

    $metadataPath = Join-Path $Directory 'PACKAGE-METADATA.json'
    $pluginPath = Join-Path $Directory 'QS3D.BricsCAD.V25.dll'
    $hasMetadata = Test-Path -LiteralPath $metadataPath -PathType Leaf
    $hasPlugin = Test-Path -LiteralPath $pluginPath -PathType Leaf
    if (-not $hasMetadata -and -not $hasPlugin) { return [Version]'0.0.0.0' }

    $metadataVersion = $null
    if ($hasMetadata) {
        try {
            $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if (-not $metadata.PSObject.Properties['version']) {
                throw 'version property is missing'
            }
            $metadataVersion = [Version]::Parse([string]$metadata.version)
        }
        catch {
            throw "Installed PACKAGE-METADATA.json has an invalid version: $($_.Exception.Message)"
        }
    }

    $pluginVersion = $null
    if ($hasPlugin) {
        try {
            $pluginVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginPath).Version
            if (-not $pluginVersion) { throw 'assembly version is missing' }
        }
        catch {
            throw "Installed QS3D plugin assembly version is unreadable: $($_.Exception.Message)"
        }
    }

    if ($metadataVersion -and $pluginVersion -and $metadataVersion -ne $pluginVersion) {
        throw "Installed package metadata version $metadataVersion does not match installed plugin assembly version $pluginVersion. Refusing update until installed state is repaired."
    }
    if ($pluginVersion) { return $pluginVersion }
    if ($metadataVersion) { return $metadataVersion }
    throw 'Installed QS3D state does not expose a readable version.'
}

function Read-PluginProductVersion {
    param([string]$Path, [string]$Label)
    try {
        $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path).ProductVersion
        return Convert-ToStrictSemVer -Value ([string]$productVersion) -Label $Label
    }
    catch {
        throw "$Label is unreadable: $($_.Exception.Message)"
    }
}

function Read-InstalledProductVersion {
    param([string]$Directory)

    $metadataPath = Join-Path $Directory 'PACKAGE-METADATA.json'
    $pluginPath = Join-Path $Directory 'QS3D.BricsCAD.V25.dll'
    $hasMetadata = Test-Path -LiteralPath $metadataPath -PathType Leaf
    $hasPlugin = Test-Path -LiteralPath $pluginPath -PathType Leaf
    if (-not $hasMetadata -and -not $hasPlugin) {
        return Convert-ToStrictSemVer -Value '0.0.0' -Label 'Installed QS3D productVersion'
    }
    if (-not $hasMetadata -or -not $hasPlugin) {
        throw 'Installed QS3D state is incomplete; PACKAGE-METADATA.json and QS3D.BricsCAD.V25.dll must both exist before secure update.'
    }

    try { $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json }
    catch { throw "Installed PACKAGE-METADATA.json is unreadable: $($_.Exception.Message)" }
    if (-not $metadata.PSObject.Properties['productVersion']) {
        throw 'Installed PACKAGE-METADATA.json is missing productVersion; install a product-version-aware signed QS3D build manually before using secure auto-update.'
    }

    $metadataProductVersion = Convert-ToStrictSemVer -Value ([string]$metadata.productVersion) -Label 'Installed PACKAGE-METADATA productVersion'
    $pluginProductVersion = Read-PluginProductVersion -Path $pluginPath -Label 'Installed signed QS3D plugin product version'
    if (-not [string]::Equals($metadataProductVersion.Text, $pluginProductVersion.Text, [StringComparison]::Ordinal)) {
        throw "Installed PACKAGE-METADATA productVersion $($metadataProductVersion.Text) does not match installed QS3D plugin product version $($pluginProductVersion.Text)."
    }
    return $pluginProductVersion
}

function Read-SignedPluginVersion {
    param([string]$Path)
    try {
        $version = [Reflection.AssemblyName]::GetAssemblyName($Path).Version
        if (-not $version) { throw 'assembly version is missing' }
        return $version
    }
    catch {
        throw "Signed QS3D plugin assembly version is unreadable: $($_.Exception.Message)"
    }
}

function Assert-AuthenticodeSigner {
    param([string]$Path, [string]$ExpectedSigner, [string]$Label)
    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "$Label signature is not valid: $($signature.Status)"
    }
    if (-not $signature.SignerCertificate) { throw "$Label signature has no signer certificate." }
    $actualSigner = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
    if ($actualSigner -ne $ExpectedSigner) {
        throw "$Label signer mismatch. Expected $ExpectedSigner, got $actualSigner."
    }
}

function Convert-FromExtendedWin32Path {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ($Path.StartsWith('\\?\UNC\', [StringComparison]::OrdinalIgnoreCase)) {
        return '\\' + $Path.Substring(8)
    }
    if ($Path.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring(4)
    }
    return $Path
}

function Open-HeldVerifiedInstaller {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExtractionRoot,
        [Parameter(Mandatory = $true)][string]$ExpectedSigner
    )

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetFullPath($ExtractionRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $rootWithSeparator = $root + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Updater installer path escaped the extraction root: $full"
    }

    $held = $null
    try {
        $held = [Qs3dNativeFile]::OpenOrdinaryReadHeld($full)
        $heldFinal = [IO.Path]::GetFullPath((Convert-FromExtendedWin32Path -Path $held.FinalPath))
        if (-not [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Updater installer resolved path '$heldFinal' does not match admitted path '$full'."
        }
        Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label 'Downloaded QS3D installer'
        return $held
    }
    catch {
        if ($held) { $held.Dispose() }
        throw
    }
}

function Expand-VerifiedHeldArchive {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ExpectedSha256,
        [Parameter(Mandatory = $true)][string]$DestinationRoot,
        [Parameter(Mandatory = $true)][int64]$MaxPackageBytes,
        [Parameter(Mandatory = $true)][int64]$MaxExpandedBytes,
        [Parameter(Mandatory = $true)][int]$MaxEntries
    )

    if ($MaxPackageBytes -le 0 -or $MaxExpandedBytes -le 0 -or $MaxEntries -le 0) {
        throw 'Archive safety limits must all be positive.'
    }

    function Assert-ExistingExtractionPathChain {
        param(
            [Parameter(Mandatory = $true)][string]$Path,
            [Parameter(Mandatory = $true)][string]$BoundaryRoot,
            [switch]$AllowOutsideBoundary
        )

        $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $boundaryFull = [IO.Path]::GetFullPath($BoundaryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $boundaryPrefix = $boundaryFull + [IO.Path]::DirectorySeparatorChar
        if (-not $AllowOutsideBoundary -and
            -not [string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase) -and
            -not $pathFull.StartsWith($boundaryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Package extraction validation escaped the extraction root: $pathFull"
        }

        $probe = $pathFull
        while (-not (Test-Path -LiteralPath $probe)) {
            $parent = [IO.Path]::GetDirectoryName($probe)
            if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $probe, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Package extraction path has no existing ancestor: $pathFull"
            }
            $probe = [IO.Path]::GetFullPath($parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        }

        $cursor = Get-Item -LiteralPath $probe -Force -ErrorAction Stop
        $reachedBoundary = $false
        while ($null -ne $cursor) {
            $cursorFull = [IO.Path]::GetFullPath([string]$cursor.FullName).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            $isBoundary = [string]::Equals($cursorFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase)
            if (-not $AllowOutsideBoundary -and -not $isBoundary -and -not $cursorFull.StartsWith($boundaryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Package extraction validation escaped the extraction root: $cursorFull"
            }
            if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                if ($isBoundary) { throw "Package extraction root is a reparse point: $cursorFull" }
                throw "Package extraction target traverses a reparse-point directory: $cursorFull"
            }
            if ($isBoundary) {
                $reachedBoundary = $true
                break
            }
            $cursor = $cursor.Parent
        }
        if (-not $AllowOutsideBoundary -and -not $reachedBoundary) {
            throw "Package extraction path does not resolve through the extraction root: $pathFull"
        }
    }

    function Ensure-SafeExtractionDirectory {
        param(
            [Parameter(Mandatory = $true)][string]$Path,
            [Parameter(Mandatory = $true)][string]$BoundaryRoot
        )

        $boundaryFull = [IO.Path]::GetFullPath($BoundaryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $boundaryPrefix = $boundaryFull + [IO.Path]::DirectorySeparatorChar
        $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        if (-not [string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase) -and
            -not $pathFull.StartsWith($boundaryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Package extraction directory escaped the extraction root: $pathFull"
        }

        Assert-ExistingExtractionPathChain -Path $boundaryFull -BoundaryRoot $boundaryFull
        if ([string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase)) { return }

        $relative = $pathFull.Substring($boundaryPrefix.Length)
        $current = $boundaryFull
        foreach ($segment in $relative.Split([IO.Path]::DirectorySeparatorChar)) {
            if ([string]::IsNullOrWhiteSpace($segment)) { throw "Package extraction directory has an empty path segment: $pathFull" }
            Assert-ExistingExtractionPathChain -Path $current -BoundaryRoot $boundaryFull
            $next = [IO.Path]::GetFullPath((Join-Path $current $segment))
            if (Test-Path -LiteralPath $next) {
                if (-not (Test-Path -LiteralPath $next -PathType Container)) {
                    throw "Package extraction directory target already exists as a non-directory: $next"
                }
            }
            else {
                [IO.Directory]::CreateDirectory($next) | Out-Null
            }
            Assert-ExistingExtractionPathChain -Path $next -BoundaryRoot $boundaryFull
            $current = $next
        }
    }

    $zipStream = [IO.File]::Open($ZipPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) {
            throw "Downloaded package size $($zipStream.Length) bytes is outside the allowed range (max $MaxPackageBytes)."
        }

        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            $zipStream.Position = 0
            $actualSha256 = ([BitConverter]::ToString($sha.ComputeHash($zipStream))).Replace('-', '').ToUpperInvariant()
        }
        finally { $sha.Dispose() }
        if (-not [string]::Equals($actualSha256, $ExpectedSha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Downloaded package SHA-256 does not match the update manifest.'
        }

        $zipStream.Position = 0
        $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            $destinationFull = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            $rootPrefix = $destinationFull + [IO.Path]::DirectorySeparatorChar
            $seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            $fileRelatives = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            $records = [Collections.Generic.List[object]]::new()
            $invalidFileNameChars = [IO.Path]::GetInvalidFileNameChars()
            [int64]$expandedBytes = 0
            $entryCount = 0

            foreach ($entry in $archive.Entries) {
                $entryCount++
                if ($entryCount -gt $MaxEntries) { throw "Package archive exceeds the allowed entry count ($MaxEntries)." }

                $name = [string]$entry.FullName
                if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
                    throw "Unsafe package archive entry: $name"
                }
                $isDirectory = $name.EndsWith('/', [StringComparison]::Ordinal)
                $relative = $name.TrimEnd('/')
                if ([string]::IsNullOrWhiteSpace($relative)) { throw "Unsafe package archive entry: $name" }
                $segments = @($relative.Split('/'))
                if ($segments.Count -eq 0) { throw "Unsafe package archive entry: $name" }
                foreach ($segment in $segments) {
                    if ([string]::IsNullOrWhiteSpace($segment) -or $segment -eq '.' -or $segment -eq '..' -or
                        $segment.IndexOfAny($invalidFileNameChars) -ge 0 -or
                        $segment.EndsWith('.', [StringComparison]::Ordinal) -or $segment.EndsWith(' ', [StringComparison]::Ordinal) -or
                        $segment -match '^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)') {
                        throw "Unsafe package archive entry segment '$segment' in '$name'."
                    }
                }

                $normalizedRelative = $segments -join '/'
                $target = [IO.Path]::GetFullPath((Join-Path $destinationFull ($normalizedRelative.Replace('/', [IO.Path]::DirectorySeparatorChar))))
                if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe package archive entry: $name" }
                if (-not $seenTargets.Add($target)) { throw "Duplicate or case-ambiguous package archive target: $name" }

                $entryLength = [int64]$entry.Length
                if ($entryLength -lt 0 -or $expandedBytes -gt ($MaxExpandedBytes - $entryLength)) {
                    throw "Package expanded size exceeds the allowed maximum ($MaxExpandedBytes bytes)."
                }
                if ($isDirectory -and $entryLength -ne 0) { throw "Package directory entry must have zero length: $name" }
                $expandedBytes += $entryLength
                if (-not $isDirectory) { $null = $fileRelatives.Add($normalizedRelative) }
                $records.Add([pscustomobject]@{
                    Entry = $entry
                    Relative = $normalizedRelative
                    Target = $target
                    IsDirectory = $isDirectory
                    Length = $entryLength
                })
            }
            if ($entryCount -eq 0) { throw 'Downloaded package archive contains no entries.' }

            foreach ($record in $records) {
                $parts = @(([string]$record.Relative).Split('/'))
                for ($index = 1; $index -lt $parts.Count; $index++) {
                    $ancestor = ($parts[0..($index - 1)] -join '/')
                    if ($fileRelatives.Contains($ancestor)) {
                        throw "Package archive file/directory target conflict: $($record.Relative) has file ancestor $ancestor."
                    }
                }
            }

            Assert-ExistingExtractionPathChain -Path $destinationFull -BoundaryRoot $destinationFull -AllowOutsideBoundary
            [IO.Directory]::CreateDirectory($destinationFull) | Out-Null
            $rootItem = Get-Item -LiteralPath $destinationFull -Force -ErrorAction Stop
            if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Package extraction root is a reparse point: $($rootItem.FullName)"
            }
            Assert-ExistingExtractionPathChain -Path $destinationFull -BoundaryRoot $destinationFull

            foreach ($record in $records) {
                if ($record.IsDirectory) {
                    Ensure-SafeExtractionDirectory -Path ([string]$record.Target) -BoundaryRoot $destinationFull
                    continue
                }

                $parent = [IO.Path]::GetDirectoryName([string]$record.Target)
                Ensure-SafeExtractionDirectory -Path $parent -BoundaryRoot $destinationFull
                Assert-ExistingExtractionPathChain -Path $parent -BoundaryRoot $destinationFull

                $entryStream = $record.Entry.Open()
                $output = $null
                $completed = $false
                try {
                    Assert-ExistingExtractionPathChain -Path $parent -BoundaryRoot $destinationFull
                    $output = [IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                    $buffer = New-Object byte[] 65536
                    [int64]$written = 0
                    while ($true) {
                        $read = $entryStream.Read($buffer, 0, $buffer.Length)
                        if ($read -le 0) { break }
                        if ($written -gt ([int64]$record.Length - [int64]$read)) {
                            throw "Package archive entry expanded beyond its admitted length: $($record.Relative)"
                        }
                        $output.Write($buffer, 0, $read)
                        $written += [int64]$read
                    }
                    if ($written -ne [int64]$record.Length) {
                        throw "Package archive entry length changed during held extraction: $($record.Relative)"
                    }
                    $output.Flush($true)
                    $completed = $true
                }
                finally {
                    if ($output) { $output.Dispose() }
                    $entryStream.Dispose()
                    if (-not $completed -and (Test-Path -LiteralPath ([string]$record.Target))) {
                        Remove-Item -LiteralPath ([string]$record.Target) -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $zipStream.Dispose() }
}

function Assert-PackageRoot {
    param([string]$Directory, [string]$ExpectedSigner)

    foreach ($name in @('COMMANDS.txt', 'PACKAGE-METADATA.json', 'SHA256SUMS.txt') + $SignedPayloadNames) {
        if (-not (Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf)) {
            throw "Downloaded package is missing required payload: $name"
        }
    }

    foreach ($name in $SignedPayloadNames) {
        Assert-AuthenticodeSigner -Path (Join-Path $Directory $name) -ExpectedSigner $ExpectedSigner -Label ("Downloaded QS3D executable payload " + $name)
    }

    $hashManifest = Join-Path $Directory 'SHA256SUMS.txt'
    $verified = 0
    foreach ($line in Get-Content -LiteralPath $hashManifest) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s{2}(.+)$') { throw "Invalid SHA256SUMS entry: $line" }
        $expected = $Matches[1].ToUpperInvariant()
        $name = $Matches[2].Trim()
        if ($name -eq 'SHA256SUMS.txt' -or [IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $segments = @($name.Split('/'))
        if ($segments.Count -eq 0 -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $packageRoot = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $path = [IO.Path]::GetFullPath((Join-Path $Directory ($name.Replace('/', [IO.Path]::DirectorySeparatorChar))))
        if (-not $path.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe SHA256SUMS entry: $name" }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing hashed payload: $name" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $expected) { throw "SHA-256 mismatch for downloaded payload: $name"
        }
        $verified++
    }
    if ($verified -eq 0) { throw 'Downloaded SHA256SUMS.txt contains no payload entries.' }
}

if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) {
    throw 'Close all BricsCAD processes before updating QS3D.'
}

$updateMutex = Enter-Qs3dUpdateMutex
try {
    $manifestAddress = Convert-ToSafeHttpsUri -Value $ManifestUri -Label 'ManifestUri'
    $officialReleaseSnapshot = Get-OfficialGitHubReleaseSnapshot -ManifestAddress $manifestAddress
    $expectedSigner = Normalize-Thumbprint $ExpectedSignerThumbprint
    $allowedHosts = @($manifestAddress.Host)
    if ($AllowedPackageHost) { $allowedHosts += $AllowedPackageHost }
    $allowedHosts = @($allowedHosts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Sort-Object -Unique)

    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-update-' + [Guid]::NewGuid().ToString('N'))
    $manifestPath = Join-Path $tempRoot 'manifest.json'
    $zipPath = Join-Path $tempRoot 'package.zip'
    $extractRoot = Join-Path $tempRoot 'package'
    $heldInstaller = $null
    $installerStream = $null
    $installerReader = $null

    try {
        New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
        Invoke-BoundedHttpsDownload -Address $manifestAddress -DestinationPath $manifestPath -MaxBytes 65536 -TimeoutMilliseconds 30000 -Label 'Update manifest' | Out-Null
        $manifestFile = Get-Item -LiteralPath $manifestPath
        if ($manifestFile.Length -le 0 -or $manifestFile.Length -gt 65536) { throw 'Update manifest must be between 1 byte and 64 KiB.' }

        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $schemaVersion = [int](Require-ManifestProperty -Manifest $manifest -Name 'schemaVersion')
        if ($schemaVersion -ne 2) { throw "Unsupported update manifest schemaVersion: $schemaVersion. Secure auto-update requires schemaVersion 2 with productVersion binding." }
        if ([string](Require-ManifestProperty -Manifest $manifest -Name 'product') -ne 'QS3D') { throw 'Update manifest product must be QS3D.' }
        if ([string](Require-ManifestProperty -Manifest $manifest -Name 'target') -ne 'BricsCAD V25 x64') { throw 'Update manifest target must be BricsCAD V25 x64.' }

        $targetProductVersion = Convert-ToStrictSemVer -Value ([string](Require-ManifestProperty -Manifest $manifest -Name 'productVersion')) -Label 'Update manifest productVersion'
        if ($officialReleaseSnapshot -and -not [string]::Equals($targetProductVersion.Text, [string]$officialReleaseSnapshot.ProductVersion, [StringComparison]::Ordinal)) {
            throw "Update manifest productVersion $($targetProductVersion.Text) does not match scheduled GitHub release $($officialReleaseSnapshot.Tag)."
        }
        $versionText = [string](Require-ManifestProperty -Manifest $manifest -Name 'version')
        try { $targetVersion = [Version]::Parse($versionText) }
        catch { throw "Update manifest version is invalid: $versionText" }

        $packageAddress = Convert-ToSafeHttpsUri -Value ([string](Require-ManifestProperty -Manifest $manifest -Name 'packageUri')) -Label 'packageUri'
        if ($officialReleaseSnapshot) {
            Assert-OfficialGitHubPackageSnapshot -PackageAddress $packageAddress -Snapshot $officialReleaseSnapshot
        }
        if ($allowedHosts -notcontains $packageAddress.Host.ToLowerInvariant()) {
            throw "Package host '$($packageAddress.Host)' is not approved. Allowed hosts: $($allowedHosts -join ', ')"
        }

        $expectedZipHash = ([string](Require-ManifestProperty -Manifest $manifest -Name 'sha256')).Trim().ToUpperInvariant()
        if ($expectedZipHash -notmatch '^[0-9A-F]{64}$') { throw 'Update manifest sha256 must be 64 hexadecimal characters.' }
        $manifestSigner = Normalize-Thumbprint ([string](Require-ManifestProperty -Manifest $manifest -Name 'signerThumbprint'))
        if ($manifestSigner -ne $expectedSigner) { throw 'Update manifest signerThumbprint does not match ExpectedSignerThumbprint.' }

        $installedVersion = Read-InstalledVersion -Directory $InstallDirectory
        if ($targetVersion -lt $installedVersion) { throw "Refusing assembly-version downgrade from $installedVersion to $targetVersion." }
        if ($targetVersion -eq $installedVersion -and -not $AllowSameVersion) { throw "QS3D assembly version $targetVersion is already installed. Use -AllowSameVersion only when the product SemVer is independently newer." }

        $installedProductVersion = Read-InstalledProductVersion -Directory $InstallDirectory
        $productComparison = Compare-StrictSemVer -Left $targetProductVersion -Right $installedProductVersion
        if ($productComparison -lt 0) {
            throw "Refusing product-version downgrade from $($installedProductVersion.Text) to $($targetProductVersion.Text)."
        }
        if ($productComparison -eq 0) {
            throw "QS3D product version $($targetProductVersion.Text) is already installed. -AllowSameVersion never authorizes product-version replay or repair."
        }

        if (-not $PSCmdlet.ShouldProcess($InstallDirectory, "Update QS3D product $($installedProductVersion.Text) -> $($targetProductVersion.Text); assembly $installedVersion -> $targetVersion")) { return }

        $maxBytes = [int64]$MaxPackageSizeMB * 1MB
        Invoke-BoundedHttpsDownload -Address $packageAddress -DestinationPath $zipPath -MaxBytes $maxBytes -TimeoutMilliseconds 120000 -Label 'Update package' | Out-Null
        $zipFile = Get-Item -LiteralPath $zipPath
        if ($zipFile.Length -le 0 -or $zipFile.Length -gt $maxBytes) {
            throw "Downloaded package size $($zipFile.Length) bytes is outside the allowed range (max $maxBytes)."
        }

        $maxExpandedBytes = [int64]$MaxExpandedPackageSizeMB * 1MB
        Expand-VerifiedHeldArchive -ZipPath $zipPath `
            -ExpectedSha256 $expectedZipHash `
            -DestinationRoot $extractRoot `
            -MaxPackageBytes $maxBytes `
            -MaxExpandedBytes $maxExpandedBytes `
            -MaxEntries $MaxArchiveEntries

        $installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
        $heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
        $installerStream = [IO.FileStream]::new($heldInstaller.Handle, [IO.FileAccess]::Read)
        $strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
        $installerReader = [IO.StreamReader]::new($installerStream, $strictUtf8, $false, 4096, $true)
        $installerText = $installerReader.ReadToEnd()
        if ([string]::IsNullOrWhiteSpace($installerText)) { throw 'Downloaded QS3D installer is empty.' }
        $installerScript = [ScriptBlock]::Create($installerText)
        Assert-PackageRoot -Directory $extractRoot -ExpectedSigner $expectedSigner

        $downloadedPluginPath = Join-Path $extractRoot 'QS3D.BricsCAD.V25.dll'
        $signedPluginVersion = Read-SignedPluginVersion -Path $downloadedPluginPath
        if ($signedPluginVersion -ne $targetVersion) {
            throw "Signed QS3D plugin assembly version $signedPluginVersion does not match manifest version $targetVersion. Refusing replay/downgrade metadata substitution."
        }

        $downloadedMetadata = Get-Content -LiteralPath (Join-Path $extractRoot 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
        if (-not $downloadedMetadata.PSObject.Properties['version']) { throw 'Downloaded PACKAGE-METADATA.json is missing version.' }
        if (-not $downloadedMetadata.PSObject.Properties['productVersion']) { throw 'Downloaded PACKAGE-METADATA.json is missing productVersion.' }
        $packageVersion = [Version]::Parse([string]$downloadedMetadata.version)
        if ($packageVersion -ne $signedPluginVersion) {
            throw "Downloaded package metadata version $packageVersion does not match signed plugin assembly version $signedPluginVersion."
        }
        if ($packageVersion -ne $targetVersion) { throw "Downloaded package version $packageVersion does not match manifest version $targetVersion." }

        $packageProductVersion = Convert-ToStrictSemVer -Value ([string]$downloadedMetadata.productVersion) -Label 'Downloaded PACKAGE-METADATA productVersion'
        $signedPluginProductVersion = Read-PluginProductVersion -Path $downloadedPluginPath -Label 'Downloaded signed QS3D plugin product version'
        if (-not [string]::Equals($packageProductVersion.Text, $signedPluginProductVersion.Text, [StringComparison]::Ordinal)) {
            throw "Downloaded PACKAGE-METADATA productVersion $($packageProductVersion.Text) does not match signed plugin product version $($signedPluginProductVersion.Text)."
        }
        if (-not [string]::Equals($packageProductVersion.Text, $targetProductVersion.Text, [StringComparison]::Ordinal)) {
            throw "Downloaded package productVersion $($packageProductVersion.Text) does not match manifest productVersion $($targetProductVersion.Text)."
        }
        if ((Compare-StrictSemVer -Left $packageProductVersion -Right $installedProductVersion) -le 0) {
            throw "Downloaded package productVersion $($packageProductVersion.Text) is not newer than installed productVersion $($installedProductVersion.Text)."
        }

        $currentInstalledVersion = Read-InstalledVersion -Directory $InstallDirectory
        if ($currentInstalledVersion -ne $installedVersion) {
            throw "Installed QS3D assembly version changed during update preparation ($installedVersion -> $currentInstalledVersion). Refusing concurrent/stale install."
        }
        $currentInstalledProductVersion = Read-InstalledProductVersion -Directory $InstallDirectory
        if (-not [string]::Equals($currentInstalledProductVersion.Text, $installedProductVersion.Text, [StringComparison]::Ordinal)) {
            throw "Installed QS3D productVersion changed during update preparation ($($installedProductVersion.Text) -> $($currentInstalledProductVersion.Text)). Refusing concurrent/stale install."
        }

        $arguments = @{
            PackageDirectory = $extractRoot
            InstallDirectory = $InstallDirectory
            LoadMode = $LoadMode
            Force = $true
            RequireSigned = $true
            ExpectedSignerThumbprint = $expectedSigner
        }
        if ($VersionKeys) { $arguments.VersionKeys = $VersionKeys }
        if ($LanguageKeys) { $arguments.LanguageKeys = $LanguageKeys }
        & $installerScript @arguments

        Write-Host "QS3D updated securely to product $($targetProductVersion.Text) (assembly $targetVersion)."
    }
    finally {
        if ($installerReader) { $installerReader.Dispose() }
        if ($installerStream) { $installerStream.Dispose() }
        if ($heldInstaller) { $heldInstaller.Dispose() }
        if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
    }
}
finally {
    Exit-Qs3dUpdateMutex -Mutex $updateMutex
}
