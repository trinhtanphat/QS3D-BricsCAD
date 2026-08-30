param(
    [Parameter(Mandatory = $true)][string]$MsiPath,
    [Parameter(Mandatory = $true)][string]$ExtractDir,
    [Parameter(Mandatory = $true)][string]$ExpectedSha256,
    [Parameter(Mandatory = $true)][string]$MirrorUrl,
    [Parameter(Mandatory = $true)][string]$PublicUrl,
    [string]$FallbackUrl = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'BricsCAD V25 compile-reference acquisition requires Windows.'
}

$expected = $ExpectedSha256.Trim().ToUpperInvariant()
if ($expected -notmatch '^[0-9A-F]{64}$') {
    throw 'ExpectedSha256 must be one 64-hex SHA-256 digest.'
}

function Get-CanonicalAbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root)) {
        throw "Path has no filesystem root: $Path"
    }

    if ($full.Length -gt $root.Length) {
        return $full.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $full
}

function Test-CanonicalPathEqual {
    param(
        [Parameter(Mandatory = $true)][string]$Left,
        [Parameter(Mandatory = $true)][string]$Right
    )

    return [string]::Equals($Left, $Right, [StringComparison]::OrdinalIgnoreCase)
}

function Test-CanonicalPathWithin {
    param(
        [Parameter(Mandatory = $true)][string]$Candidate,
        [Parameter(Mandatory = $true)][string]$Parent
    )

    if (Test-CanonicalPathEqual -Left $Candidate -Right $Parent) { return $true }
    $prefix = $Parent.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    return $Candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoExistingReparseComponent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path
    $root = [IO.Path]::GetPathRoot($canonical)
    $relative = $canonical.Substring($root.Length)
    $current = $root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { break }
        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point: $current"
        }
    }
}

function Get-OrdinaryFileOrNull {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file."
    }
    return $item
}

function Open-PinnedMsiReadLock {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedSha256
    )

    $item = Get-OrdinaryFileOrNull -Path $Path -Label 'BricsCAD V25 MSI'
    if ($null -eq $item) {
        throw 'Pinned BricsCAD V25 MSI disappeared before stable-generation admission.'
    }
    if ($item.Length -le 1048576) {
        throw 'Pinned BricsCAD V25 MSI is unexpectedly small.'
    }

    $canonicalBefore = Get-CanonicalAbsolutePath -Path $item.FullName
    $lengthBefore = [int64]$item.Length
    $lastWriteTicksBefore = [int64]$item.LastWriteTimeUtc.Ticks
    $stream = $null
    $sha = $null
    try {
        # FileShare.Read deliberately denies write/delete/replace while trust
        # metadata and msiexec consume the admitted generation by path.
        $stream = [IO.File]::Open(
            $canonicalBefore,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        $sha = [Security.Cryptography.SHA256]::Create()
        $hashBytes = $sha.ComputeHash($stream)
        $actual = ([BitConverter]::ToString($hashBytes)).Replace('-', '').ToUpperInvariant()
        $stream.Position = 0

        $itemAfter = Get-OrdinaryFileOrNull -Path $Path -Label 'BricsCAD V25 MSI'
        if ($null -eq $itemAfter) {
            throw 'Pinned BricsCAD V25 MSI disappeared during stable-generation admission.'
        }
        $canonicalAfter = Get-CanonicalAbsolutePath -Path $itemAfter.FullName
        if (-not (Test-CanonicalPathEqual -Left $canonicalBefore -Right $canonicalAfter)) {
            throw 'Pinned BricsCAD V25 MSI resolved to a different path during stable-generation admission.'
        }
        if ([int64]$itemAfter.Length -ne $lengthBefore -or [int64]$itemAfter.LastWriteTimeUtc.Ticks -ne $lastWriteTicksBefore) {
            throw 'Pinned BricsCAD V25 MSI changed during stable-generation admission.'
        }
        if ($stream.Length -ne $lengthBefore) {
            throw 'Pinned BricsCAD V25 MSI stream length changed during stable-generation admission.'
        }
        if (-not [string]::Equals($actual, $ExpectedSha256, [StringComparison]::Ordinal)) {
            throw "Pinned BricsCAD V25 MSI SHA256 changed before trust consumption: $actual"
        }

        return [pscustomobject]@{
            Path = $canonicalBefore
            Sha256 = $actual
            Length = $lengthBefore
            LastWriteUtcTicks = $lastWriteTicksBefore
            Stream = $stream
        }
    }
    catch {
        if ($null -ne $stream) { $stream.Dispose() }
        throw
    }
    finally {
        if ($null -ne $sha) { $sha.Dispose() }
    }
}

function Test-PinnedMsiGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $state = $null
    try {
        $state = Open-PinnedMsiReadLock -Path $Path -ExpectedSha256 $expected
        return $true
    }
    catch {
        Write-Warning "$Label failed exact held-generation admission: $($_.Exception.Message)"
        return $false
    }
    finally {
        if ($null -ne $state) { $state.Stream.Dispose() }
    }
}

function Assert-PinnedMsiStable {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $item = Get-OrdinaryFileOrNull -Path $State.Path -Label 'BricsCAD V25 MSI'
    if ($null -eq $item) {
        throw "Pinned BricsCAD V25 MSI disappeared $Label."
    }
    $canonical = Get-CanonicalAbsolutePath -Path $item.FullName
    if (-not (Test-CanonicalPathEqual -Left $canonical -Right $State.Path)) {
        throw "Pinned BricsCAD V25 MSI resolved to a different path $Label."
    }
    if ([int64]$item.Length -ne [int64]$State.Length -or
        [int64]$item.LastWriteTimeUtc.Ticks -ne [int64]$State.LastWriteUtcTicks -or
        [int64]$State.Stream.Length -ne [int64]$State.Length) {
        throw "Pinned BricsCAD V25 MSI generation changed $Label."
    }
}

function Get-OrdinaryFilesByNameUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $rootItem = Get-Item -LiteralPath $Root -Force
    if (-not $rootItem.PSIsContainer) {
        throw "Extraction root is not a directory: $Root"
    }
    if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Extraction root must not be a filesystem reparse point: $Root"
    }

    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $matches = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $stack.Push($rootItem.FullName)
    while ($stack.Count -gt 0) {
        $directory = $stack.Pop()
        foreach ($entry in @(Get-ChildItem -LiteralPath $directory -Force)) {
            if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Extracted V25 tree must not contain filesystem reparse points: $($entry.FullName)"
            }
            if ($entry.PSIsContainer) {
                $stack.Push($entry.FullName)
                continue
            }
            if ($entry -isnot [IO.FileInfo]) {
                throw "Extracted V25 tree contains an unsupported filesystem entry: $($entry.FullName)"
            }
            if ([string]::Equals($entry.Name, $Name, [StringComparison]::OrdinalIgnoreCase)) {
                $matches.Add($entry)
            }
        }
    }
    return @($matches)
}

function Stop-OwnedProcessTree {
    param(
        [Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process,
        [ValidateRange(1, 60000)][int]$CleanupTimeoutMs = 10000
    )

    # Always attempt PID-scoped tree cleanup after the extraction timeout. The
    # root may race to exit between WaitForExit(false) and this helper; treating
    # that race as a clean return would make surviving descendants unverifiable.
    $taskkill = $null
    try {
        $taskkill = Start-Process -FilePath 'taskkill.exe' -ArgumentList @('/PID', [string]$Process.Id, '/T', '/F') -PassThru -NoNewWindow
        if (-not $taskkill.WaitForExit($CleanupTimeoutMs)) {
            try { $taskkill.Kill() } catch { }
            try { [void]$taskkill.WaitForExit(1000) } catch { }
            throw "owned process-tree cleanup command timed out after $CleanupTimeoutMs ms"
        }
        if ($taskkill.ExitCode -ne 0) {
            throw "owned process-tree cleanup command failed with exit code $($taskkill.ExitCode)"
        }
        if (-not $Process.WaitForExit($CleanupTimeoutMs)) {
            throw "owned MSI root process did not exit within $CleanupTimeoutMs ms after tree cleanup"
        }
    }
    finally {
        if ($null -ne $taskkill) {
            $taskkill.Dispose()
        }
    }
}

$msi = Get-CanonicalAbsolutePath -Path $MsiPath
$extract = Get-CanonicalAbsolutePath -Path $ExtractDir
$cacheDir = Get-CanonicalAbsolutePath -Path (Split-Path -Parent $msi)
$extractRoot = Get-CanonicalAbsolutePath -Path ([IO.Path]::GetPathRoot($extract))

if (Test-CanonicalPathEqual -Left $extract -Right $extractRoot) {
    throw "ExtractDir must not be a filesystem root: $extract"
}
if (Test-CanonicalPathWithin -Candidate $msi -Parent $extract) {
    throw 'ExtractDir must not equal or contain MsiPath because extraction cleanup is recursive.'
}
if (Test-CanonicalPathWithin -Candidate $cacheDir -Parent $extract) {
    throw 'ExtractDir must not equal or contain the MSI cache directory because extraction cleanup is recursive.'
}

# Existing filesystem aliases must be rejected before any recursive cleanup or
# cache mutation. This keeps lexical overlap checks from being bypassed by a
# junction/symlink that redirects an apparently safe path elsewhere.
Assert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'
Assert-NoExistingReparseComponent -Path $msi -Label 'MsiPath'
Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'

# No destructive filesystem mutation may occur before the path-overlap and
# reparse-component guards above.
New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $extract -Force | Out-Null

$sourceName = $null
if (Test-PinnedMsiGeneration -Path $msi -Label 'Cached BricsCAD V25 MSI') {
    $sourceName = 'actions-cache/local-cache'
}
else {
    $candidates = @(
        [pscustomobject]@{ Name = 'pinned-user-mirror'; Url = $MirrorUrl },
        [pscustomobject]@{ Name = 'pinned-public'; Url = $PublicUrl }
    )
    if (-not [string]::IsNullOrWhiteSpace($FallbackUrl)) {
        $candidates += [pscustomobject]@{ Name = 'signed-secret-fallback'; Url = $FallbackUrl }
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace([string]$candidate.Url)) { continue }
        $staging = Join-Path $cacheDir ('.qs3d-v25-msi-' + [Guid]::NewGuid().ToString('N') + '.tmp')
        try {
            Assert-NoExistingReparseComponent -Path $staging -Label 'MSI download staging path'
            Write-Host "Downloading BricsCAD V25 installer from $($candidate.Name) to isolated staging..."
            Invoke-WebRequest -Uri $candidate.Url -OutFile $staging -MaximumRedirection 10 -TimeoutSec 1200 -UseBasicParsing
            if (-not (Test-PinnedMsiGeneration -Path $staging -Label "Staged BricsCAD V25 MSI from $($candidate.Name)")) {
                continue
            }

            Assert-NoExistingReparseComponent -Path $msi -Label 'MsiPath before atomic publication'
            if (Test-Path -LiteralPath $msi) {
                $existing = Get-Item -LiteralPath $msi -Force
                if ($existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw 'Canonical MSI destination became a non-ordinary or reparse-backed entry before publication.'
                }
                Remove-Item -LiteralPath $msi -Force
            }
            [IO.File]::Move($staging, $msi)
            if (-not (Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI')) {
                throw 'Canonical MSI generation failed held verification immediately after publication.'
            }
            $sourceName = $candidate.Name
            break
        }
        catch {
            Write-Warning "BricsCAD V25 installer source failed: $($candidate.Name) • $($_.Exception.Message)"
        }
        finally {
            Remove-Item -LiteralPath $staging -Force -ErrorAction SilentlyContinue
        }
    }
}

if ([string]::IsNullOrWhiteSpace($sourceName) -or -not (Test-PinnedMsiGeneration -Path $msi -Label 'Final BricsCAD V25 MSI')) {
    throw 'Unable to obtain the exact pinned BricsCAD V25.2.10 x64 installer.'
}

$msiState = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected
try {
    $actualHash = [string]$msiState.Sha256
    Assert-PinnedMsiStable -State $msiState -Label 'before Authenticode verification'

    $signature = Get-AuthenticodeSignature -FilePath $msiState.Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $signature.SignerCertificate) {
        throw "BricsCAD V25 MSI Authenticode signature is not valid: $($signature.Status)."
    }
    $signerSubject = [string]$signature.SignerCertificate.Subject
    if ($signerSubject -notmatch '(^|,\s*)(CN|O)=Bricsys(,|$)') {
        throw "BricsCAD V25 MSI signer is not Bricsys: $signerSubject"
    }
    Assert-PinnedMsiStable -State $msiState -Label 'after Authenticode verification'

    $installer = New-Object -ComObject WindowsInstaller.Installer
    $database = $installer.OpenDatabase($msiState.Path, 0)
    $versionView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')
    $versionView.Execute()
    $versionRecord = $versionView.Fetch()
    $productVersion = if ($versionRecord) { [string]$versionRecord.StringData(1) } else { [string]::Empty }
    $nameView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')
    $nameView.Execute()
    $nameRecord = $nameView.Fetch()
    $productName = if ($nameRecord) { [string]$nameRecord.StringData(1) } else { [string]::Empty }
    if ($productVersion -notmatch '^25\.2\.10(?:\.|$)') {
        throw "Downloaded MSI is not the pinned BricsCAD V25.2.10 product. ProductVersion=$productVersion"
    }
    if ($productName -notmatch 'BricsCAD') {
        throw "Downloaded MSI ProductName is not BricsCAD: $productName"
    }
    Assert-PinnedMsiStable -State $msiState -Label 'after Windows Installer metadata verification'

    $msiLog = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-admin-' + [Guid]::NewGuid().ToString('N') + '.log')
    try {
        Assert-PinnedMsiStable -State $msiState -Label 'immediately before administrative extraction'
        $arguments = @('/a', ('"' + $msiState.Path + '"'), '/qn', '/norestart', ('TARGETDIR="' + $extract + '"'), 'REBOOT=ReallySuppress', '/L*v', ('"' + $msiLog + '"'))
        Write-Host 'Starting BricsCAD V25 MSI administrative extraction (15-minute process timeout)...'
        $process = Start-Process -FilePath msiexec.exe -ArgumentList $arguments -PassThru
        $exited = $process.WaitForExit(900000)
        if (-not $exited) {
            $cleanupFailure = $null
            try {
                Stop-OwnedProcessTree -Process $process -CleanupTimeoutMs 10000
            }
            catch {
                $cleanupFailure = $_.Exception.Message
            }
            if (Test-Path -LiteralPath $msiLog -PathType Leaf) { Get-Content -LiteralPath $msiLog -Tail 120 }
            if (-not [string]::IsNullOrWhiteSpace([string]$cleanupFailure)) {
                throw "BricsCAD V25 MSI administrative extraction timed out after 15 minutes; owned process-tree cleanup failed: $cleanupFailure"
            }
            throw 'BricsCAD V25 MSI administrative extraction timed out after 15 minutes; owned process tree terminated.'
        }
        if ($process.ExitCode -notin @(0, 3010)) {
            if (Test-Path -LiteralPath $msiLog -PathType Leaf) { Get-Content -LiteralPath $msiLog -Tail 120 }
            throw "BricsCAD V25 MSI administrative extraction failed with exit code $($process.ExitCode)."
        }
        Assert-PinnedMsiStable -State $msiState -Label 'after administrative extraction'
    }
    finally {
        Remove-Item -LiteralPath $msiLog -Force -ErrorAction SilentlyContinue
    }
}
finally {
    $msiState.Stream.Dispose()
}

$brx = @(Get-OrdinaryFilesByNameUnderRoot -Root $extract -Name 'BrxMgd.dll')
$candidateDirs = @($brx | ForEach-Object { $_.Directory.FullName } | Sort-Object -Unique)
$bricsDir = $candidateDirs | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_ 'BrxMgd.dll') -PathType Leaf) -and
    (Test-Path -LiteralPath (Join-Path $_ 'TD_Mgd.dll') -PathType Leaf) -and
    (Test-Path -LiteralPath (Join-Path $_ 'TD_MgdBrep.dll') -PathType Leaf)
} | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($bricsDir)) {
    throw 'BrxMgd.dll, TD_Mgd.dll, and TD_MgdBrep.dll were not found together in one extracted V25 runtime directory.'
}

Write-Host "Verified BricsCAD V25.2.10 MSI SHA256: $actualHash"
Write-Host "Verified installer signer: $signerSubject"
Write-Host "Verified MSI identity: $productName $productVersion"
Write-Host "V25 compile references resolved from $sourceName."
Write-Output $bricsDir
