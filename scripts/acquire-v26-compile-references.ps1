[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$MsiPath,
    [string]$ExtractDir = '',
    [string]$ExpectedSha256 = '',
    [Parameter(Mandatory = $true)][string]$PrimaryUrl,
    [switch]$UsePinnedHttpMirror,
    [string]$FallbackUrl = '',
    [switch]$ExtractReferences
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'BricsCAD V26 compile-reference acquisition requires Windows.'
}

$expected = ([string]$ExpectedSha256).Trim().ToUpperInvariant()
if (-not [string]::IsNullOrWhiteSpace($expected) -and $expected -notmatch '^[0-9A-F]{64}$') {
    throw 'ExpectedSha256 must be empty or one exact 64-hex SHA-256 digest.'
}

function Get-CanonicalAbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)
    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root)) { throw "Path has no filesystem root: $Path" }
    if ($full.Length -gt $root.Length) {
        return $full.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $full
}

function Assert-NoExistingReparseComponent {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $canonical = Get-CanonicalAbsolutePath -Path $Path
    $root = [IO.Path]::GetPathRoot($canonical)
    $relative = $canonical.Substring($root.Length)
    $current = $root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { break }
        $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point: $current"
        }
    }
}

function Get-OrdinaryFileOrNull {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]) -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $Path"
    }
    return $item
}

function Assert-SafeHttpsUrl {
    param([Parameter(Mandatory = $true)][string]$Url, [Parameter(Mandatory = $true)][string]$Label)
    $uri = $null
    if (-not [Uri]::TryCreate($Url, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https') {
        throw "$Label must be an absolute HTTPS URL."
    }
    if (-not [string]::IsNullOrEmpty($uri.UserInfo) -or -not [string]::IsNullOrEmpty($uri.Fragment)) {
        throw "$Label must not contain embedded credentials or a fragment."
    }
    return $uri
}

function Assert-PinnedV26HttpMirrorUrl {
    $expectedMirror = 'http://103.9.157.20/BricsCAD-V26.2.07-1-en_US(x64).msi'
    $uri = $null
    if (-not [Uri]::TryCreate($expectedMirror, [UriKind]::Absolute, [ref]$uri) -or
        $uri.Scheme -ne 'http' -or
        $uri.Host -ne '103.9.157.20' -or
        $uri.Port -ne 80 -or
        -not [string]::Equals($uri.AbsolutePath, '/BricsCAD-V26.2.07-1-en_US(x64).msi', [StringComparison]::Ordinal) -or
        -not [string]::IsNullOrEmpty($uri.Query) -or
        -not [string]::IsNullOrEmpty($uri.UserInfo) -or
        -not [string]::IsNullOrEmpty($uri.Fragment)) {
        throw 'The built-in V26 HTTP mirror identity is invalid.'
    }
    return $expectedMirror
}

function Open-AdmittedV26Installer {
    param([Parameter(Mandatory = $true)][string]$Path, [string]$Expected = '')

    [void](Assert-NoExistingReparseComponent -Path $Path -Label 'BricsCAD V26 MSI path')
    $item = Get-OrdinaryFileOrNull -Path $Path -Label 'BricsCAD V26 MSI'
    if ($null -eq $item) { throw 'BricsCAD V26 MSI is missing.' }
    if ([int64]$item.Length -le 100MB) { throw 'BricsCAD V26 MSI is unexpectedly small.' }

    $canonical = Get-CanonicalAbsolutePath -Path $item.FullName
    $length = [int64]$item.Length
    $lastWriteTicks = [int64]$item.LastWriteTimeUtc.Ticks
    $stream = [IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            $hashBytes = $sha.ComputeHash($stream)
            $actualSha = ([BitConverter]::ToString($hashBytes)).Replace('-', '').ToUpperInvariant()
            $stream.Position = 0
        }
        finally { $sha.Dispose() }

        if (-not [string]::IsNullOrWhiteSpace($Expected) -and -not [string]::Equals($actualSha, $Expected, [StringComparison]::Ordinal)) {
            throw "BricsCAD V26 MSI SHA-256 mismatch. Expected $Expected, got $actualSha."
        }

        $afterHash = Get-OrdinaryFileOrNull -Path $canonical -Label 'BricsCAD V26 MSI'
        if ($null -eq $afterHash -or [int64]$afterHash.Length -ne $length -or [int64]$afterHash.LastWriteTimeUtc.Ticks -ne $lastWriteTicks -or $stream.Length -ne $length) {
            throw 'BricsCAD V26 MSI changed during held SHA-256 admission.'
        }

        $signature = Get-AuthenticodeSignature -FilePath $canonical
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $signature.SignerCertificate) {
            throw "BricsCAD V26 MSI Authenticode signature is not valid: $($signature.Status)."
        }
        $signerSubject = [string]$signature.SignerCertificate.Subject
        if ($signerSubject -notmatch '(^|,\s*)(CN|O)=Bricsys(,|$)') {
            throw "BricsCAD V26 MSI signer is not Bricsys: $signerSubject"
        }

        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase($canonical, 0)
        $versionView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')
        [void]$versionView.Execute()
        $versionRecord = $versionView.Fetch()
        $productVersion = if ($versionRecord) { [string]$versionRecord.StringData(1) } else { [string]::Empty }
        $nameView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')
        [void]$nameView.Execute()
        $nameRecord = $nameView.Fetch()
        $productName = if ($nameRecord) { [string]$nameRecord.StringData(1) } else { [string]::Empty }
        if ($productVersion -notmatch '^26\.2\.07(?:\.|$)') {
            throw "Downloaded MSI is not the pinned BricsCAD V26.2.07 product. ProductVersion=$productVersion"
        }
        if ($productName -notmatch 'BricsCAD') {
            throw "Downloaded MSI ProductName is not BricsCAD: $productName"
        }

        $afterMetadata = Get-OrdinaryFileOrNull -Path $canonical -Label 'BricsCAD V26 MSI'
        if ($null -eq $afterMetadata -or [int64]$afterMetadata.Length -ne $length -or [int64]$afterMetadata.LastWriteTimeUtc.Ticks -ne $lastWriteTicks -or $stream.Length -ne $length) {
            throw 'BricsCAD V26 MSI changed during signer/metadata admission.'
        }

        return [pscustomobject]@{
            Path = $canonical
            Sha256 = $actualSha
            Length = $length
            LastWriteUtcTicks = $lastWriteTicks
            ProductName = $productName
            ProductVersion = $productVersion
            SignerSubject = $signerSubject
            Stream = $stream
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Get-SingleV26InstallerAdmission {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Expected = ''
    )

    $outputs = @(Open-AdmittedV26Installer -Path $Path -Expected $Expected)
    if ($outputs.Count -ne 1) {
        foreach ($output in $outputs) {
            if ($null -ne $output -and $null -ne $output.PSObject.Properties['Stream']) {
                $heldStream = $output.PSObject.Properties['Stream'].Value
                if ($heldStream -is [IO.Stream]) { $heldStream.Dispose() }
            }
        }
        $types = @($outputs | ForEach-Object {
            if ($null -eq $_) { '<null>' } else { $_.GetType().FullName }
        })
        throw "Open-AdmittedV26Installer must emit exactly one admission object. Output types: $($types -join ', ')"
    }

    $admission = $outputs[0]
    $requiredProperties = @('Path', 'Sha256', 'Length', 'LastWriteUtcTicks', 'ProductName', 'ProductVersion', 'SignerSubject', 'Stream')
    foreach ($propertyName in $requiredProperties) {
        if ($null -eq $admission -or $null -eq $admission.PSObject.Properties[$propertyName]) {
            if ($null -ne $admission -and $null -ne $admission.PSObject.Properties['Stream']) {
                $heldStream = $admission.PSObject.Properties['Stream'].Value
                if ($heldStream -is [IO.Stream]) { $heldStream.Dispose() }
            }
            throw "Open-AdmittedV26Installer returned one value, but it is missing required admission property $propertyName."
        }
    }
    if ($admission.Stream -isnot [IO.Stream]) {
        throw 'Open-AdmittedV26Installer returned one value, but its Stream property is not a held System.IO.Stream.'
    }
    return $admission
}

function Assert-HeldInstallerStable {
    param([Parameter(Mandatory = $true)]$Held, [Parameter(Mandatory = $true)][string]$Phase)
    $current = Get-OrdinaryFileOrNull -Path $Held.Path -Label 'BricsCAD V26 MSI'
    if ($null -eq $current -or [int64]$current.Length -ne [int64]$Held.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne [int64]$Held.LastWriteUtcTicks -or
        [int64]$Held.Stream.Length -ne [int64]$Held.Length) {
        throw "BricsCAD V26 MSI generation changed $Phase."
    }
}

function Publish-AdmittedV26Installer {
    param(
        [Parameter(Mandatory = $true)]$Candidate,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    Assert-HeldInstallerStable -Held $Candidate -Phase 'before canonical cache publication'
    Assert-NoExistingReparseComponent -Path $Destination -Label 'V26 MSI destination before publication'
    if (Test-Path -LiteralPath $Destination) {
        throw 'V26 MSI destination must be fresh before held-byte publication.'
    }

    $destinationStream = $null
    $published = $null
    try {
        $destinationStream = [IO.File]::Open(
            $Destination,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None
        )
        $Candidate.Stream.Position = 0
        $Candidate.Stream.CopyTo($destinationStream)
        $destinationStream.Flush($true)
        $destinationStream.Dispose()
        $destinationStream = $null

        Assert-HeldInstallerStable -Held $Candidate -Phase 'after canonical cache byte publication'
        $published = Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256
        if (-not [string]::Equals([string]$published.Sha256, [string]$Candidate.Sha256, [StringComparison]::Ordinal)) {
            throw 'published V26 MSI digest does not match admitted staged generation'
        }
        if (-not [string]::Equals([string]$published.ProductVersion, [string]$Candidate.ProductVersion, [StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$published.ProductName, [string]$Candidate.ProductName, [StringComparison]::Ordinal)) {
            throw 'published V26 MSI product identity does not match admitted staged generation'
        }
        if (-not [string]::Equals([string]$published.SignerSubject, [string]$Candidate.SignerSubject, [StringComparison]::Ordinal)) {
            throw 'published V26 MSI signer does not match admitted staged generation'
        }
        return $published
    }
    catch {
        if ($null -ne $published) {
            $published.Stream.Dispose()
            $published = $null
        }
        if ($null -ne $destinationStream) {
            $destinationStream.Dispose()
            $destinationStream = $null
        }
        Write-Warning 'V26 MSI publication failed after canonical destination creation; leaving the destination untouched for fail-closed re-admission.'
        throw
    }
}

function Get-ReferenceDirectories {
    param([Parameter(Mandatory = $true)][string]$Root)
    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction Stop
    if (-not $rootItem.PSIsContainer -or ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "V26 extraction root must be an ordinary non-reparse directory: $Root"
    }
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($rootItem.FullName)
    $directories = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    while ($stack.Count -gt 0) {
        $directory = $stack.Pop()
        foreach ($entry in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)) {
            if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Extracted V26 tree must not contain filesystem reparse points: $($entry.FullName)"
            }
            if ($entry.PSIsContainer) {
                $stack.Push($entry.FullName)
                continue
            }
            if ($entry -isnot [IO.FileInfo]) {
                throw "Extracted V26 tree contains an unsupported filesystem entry: $($entry.FullName)"
            }
            if ([string]::Equals($entry.Name, 'BrxMgd.dll', [StringComparison]::OrdinalIgnoreCase)) {
                [void]$directories.Add($entry.Directory.FullName)
            }
        }
    }
    return @($directories)
}

$msi = Get-CanonicalAbsolutePath -Path $MsiPath
$cacheDir = Get-CanonicalAbsolutePath -Path (Split-Path -Parent $msi)
Assert-NoExistingReparseComponent -Path $cacheDir -Label 'V26 MSI cache directory'
Assert-NoExistingReparseComponent -Path $msi -Label 'V26 MSI path'
New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null

$admission = $null
if (Test-Path -LiteralPath $msi -PathType Leaf) {
    try {
        $admission = Get-SingleV26InstallerAdmission -Path $msi -Expected $expected
        Write-Host 'Using admitted BricsCAD V26.2.07 installer from Actions cache/local cache.'
    }
    catch {
        throw "Cached BricsCAD V26 installer was rejected; rejected cached V26 MSI is left untouched because safe replacement requires a fresh canonical destination. $($_.Exception.Message)"
    }
}

if ($null -eq $admission) {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($PrimaryUrl)) {
        [void](Assert-SafeHttpsUrl -Url $PrimaryUrl -Label 'PrimaryUrl')
        $candidates += [pscustomobject]@{ Name = 'canonical-public'; Url = $PrimaryUrl }
    }
    if ($UsePinnedHttpMirror) {
        $pinnedMirror = Assert-PinnedV26HttpMirrorUrl
        $candidates += [pscustomobject]@{ Name = 'pinned-http-mirror'; Url = $pinnedMirror }
    }
    if (-not [string]::IsNullOrWhiteSpace($FallbackUrl)) {
        [void](Assert-SafeHttpsUrl -Url $FallbackUrl -Label 'FallbackUrl')
        $candidates += [pscustomobject]@{ Name = 'owner-bootstrap-fallback'; Url = $FallbackUrl }
    }
    if ($candidates.Count -eq 0) { throw 'No BricsCAD V26 installer source is configured and the cache is empty.' }

    foreach ($candidate in $candidates) {
        $staging = Join-Path $cacheDir ('.qs3d-v26-msi-' + [Guid]::NewGuid().ToString('N') + '.tmp')
        try {
            Assert-NoExistingReparseComponent -Path $staging -Label 'V26 MSI download staging path'
            Write-Host "Downloading BricsCAD V26.2.07 installer from $($candidate.Name) source."
            Invoke-WebRequest -Uri $candidate.Url -OutFile $staging -MaximumRedirection 10 -TimeoutSec 1800 -UseBasicParsing
            $candidateAdmission = Get-SingleV26InstallerAdmission -Path $staging -Expected $expected
            try {
                $admission = Publish-AdmittedV26Installer -Candidate $candidateAdmission -Destination $msi
                break
            }
            finally {
                if ($null -ne $candidateAdmission) { $candidateAdmission.Stream.Dispose() }
            }
        }
        catch {
            Write-Warning "BricsCAD V26 installer source $($candidate.Name) failed admission: $($_.Exception.Message)"
        }
        finally {
            Remove-Item -LiteralPath $staging -Force -ErrorAction SilentlyContinue
        }
    }
}

if ($null -eq $admission) {
    throw 'Unable to obtain an admitted BricsCAD V26.2.07 x64 installer.'
}

$bricsDir = ''
try {
    Assert-HeldInstallerStable -Held $admission -Phase 'before optional reference extraction'
    if ($ExtractReferences) {
        if ([string]::IsNullOrWhiteSpace($ExtractDir)) { throw 'ExtractDir is required when ExtractReferences is enabled.' }
        $extract = Get-CanonicalAbsolutePath -Path $ExtractDir
        Assert-NoExistingReparseComponent -Path $extract -Label 'V26 extraction directory'
        if (Test-Path -LiteralPath $extract) { throw "ExtractDir must be fresh and non-existent: $extract" }
        if ($msi.StartsWith($extract + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'V26 MSI cache path must remain outside the extraction directory.'
        }
        New-Item -ItemType Directory -Path $extract | Out-Null

        $msiLog = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v26-admin-' + [Guid]::NewGuid().ToString('N') + '.log')
        try {
            $arguments = @('/a', ('"' + $admission.Path + '"'), '/qn', '/norestart', ('TARGETDIR="' + $extract + '"'), 'REBOOT=ReallySuppress', '/L*v', ('"' + $msiLog + '"'))
            Write-Host 'Starting BricsCAD V26 MSI administrative extraction (15-minute timeout).'
            $process = Start-Process -FilePath msiexec.exe -ArgumentList $arguments -PassThru
            if (-not $process.WaitForExit(900000)) {
                & taskkill.exe /PID $process.Id /T /F | Out-Null
                throw 'BricsCAD V26 MSI administrative extraction timed out after 15 minutes; owned process tree termination was requested.'
            }
            if ($process.ExitCode -notin @(0, 3010)) {
                if (Test-Path -LiteralPath $msiLog -PathType Leaf) { Get-Content -LiteralPath $msiLog -Tail 120 }
                throw "BricsCAD V26 MSI administrative extraction failed with exit code $($process.ExitCode)."
            }
            Assert-HeldInstallerStable -Held $admission -Phase 'after administrative extraction'
        }
        finally { Remove-Item -LiteralPath $msiLog -Force -ErrorAction SilentlyContinue }

        $candidateDirs = @(Get-ReferenceDirectories -Root $extract)
        $bricsDir = $candidateDirs | Where-Object {
            (Test-Path -LiteralPath (Join-Path $_ 'BrxMgd.dll') -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $_ 'TD_Mgd.dll') -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $_ 'TD_MgdBrep.dll') -PathType Leaf)
        } | Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace([string]$bricsDir)) {
            throw 'BrxMgd.dll, TD_Mgd.dll, and TD_MgdBrep.dll were not found together in one extracted V26 runtime directory.'
        }
    }

    Write-Host "Verified BricsCAD V26.2.07 MSI SHA256: $($admission.Sha256)"
    Write-Host "Verified installer signer: $($admission.SignerSubject)"
    Write-Host "Verified MSI identity: $($admission.ProductName) $($admission.ProductVersion)"
    [pscustomobject]@{
        MsiPath = $admission.Path
        Sha256 = $admission.Sha256
        ProductVersion = $admission.ProductVersion
        BricsCadDir = [string]$bricsDir
    }
}
finally {
    $admission.Stream.Dispose()
}
