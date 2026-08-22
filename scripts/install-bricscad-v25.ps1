param(
    [Parameter(Mandatory = $true)][string]$MsiPath,
    [string]$InstallDir = "",
    [string]$ExpectedSha256 = "",
    [switch]$AllowUntrustedPublisher
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "BricsCAD V25 installation requires Windows."
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this installer helper from an elevated PowerShell session."
}

$MsiPath = [IO.Path]::GetFullPath($MsiPath)
if (-not (Test-Path -LiteralPath $MsiPath -PathType Leaf)) {
    throw "MSI not found: $MsiPath"
}
if ([IO.Path]::GetExtension($MsiPath) -ne ".msi") {
    throw "Expected a BricsCAD V25 .msi installer."
}
if ([IO.Path]::GetFileName($MsiPath) -notmatch '^BricsCAD-V25[.-].*\(x64\)\.msi$') {
    Write-Warning "Installer filename does not look like the usual BricsCAD V25 x64 naming convention: $MsiPath"
}

$actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $MsiPath).Hash.ToUpperInvariant()
if (-not [string]::IsNullOrWhiteSpace($ExpectedSha256)) {
    $normalizedExpected = $ExpectedSha256.Replace("-", "").Trim().ToUpperInvariant()
    if ($normalizedExpected -notmatch '^[0-9A-F]{64}$') {
        throw "ExpectedSha256 must contain exactly 64 hexadecimal characters."
    }
    if (-not [string]::Equals($actualSha256, $normalizedExpected, [StringComparison]::Ordinal)) {
        throw "BricsCAD MSI SHA-256 mismatch. Expected $normalizedExpected, got $actualSha256."
    }
}

$signature = Get-AuthenticodeSignature -LiteralPath $MsiPath
$signatureStatus = $signature.Status.ToString()
$publisher = if ($null -ne $signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { "" }
if (-not $AllowUntrustedPublisher) {
    if (-not [string]::Equals($signatureStatus, "Valid", [StringComparison]::OrdinalIgnoreCase)) {
        throw "BricsCAD MSI Authenticode signature is not valid. Status: $signatureStatus. Use -AllowUntrustedPublisher only for an intentionally trusted offline/certificate-chain exception."
    }
    if ([string]::IsNullOrWhiteSpace($publisher) -or $publisher -notmatch '(?i)\bBricsys\b') {
        throw "BricsCAD MSI signer is not recognized as Bricsys. Signer: '$publisher'."
    }
}
elseif (-not [string]::Equals($signatureStatus, "Valid", [StringComparison]::OrdinalIgnoreCase) -or $publisher -notmatch '(?i)\bBricsys\b') {
    Write-Warning "Authenticode publisher validation was explicitly bypassed. Status='$signatureStatus', signer='$publisher'."
}

$windowsInstaller = New-Object -ComObject WindowsInstaller.Installer
$database = $windowsInstaller.OpenDatabase($MsiPath, 0)
$productNameView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')
$productNameView.Execute()
$productNameRecord = $productNameView.Fetch()
$productName = if ($productNameRecord) { [string]$productNameRecord.StringData(1) } else { "" }
$productVersionView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')
$productVersionView.Execute()
$productVersionRecord = $productVersionView.Fetch()
$productVersion = if ($productVersionRecord) { [string]$productVersionRecord.StringData(1) } else { "" }
if ([string]::IsNullOrWhiteSpace($productName) -or $productName -notmatch '(?i)\bBricsCAD\b') {
    throw "MSI ProductName does not identify BricsCAD: '$productName'."
}
if ([string]::IsNullOrWhiteSpace($productVersion) -or $productVersion -notmatch '^25(?:\.|$)') {
    throw "MSI ProductVersion does not identify BricsCAD V25: '$productVersion'."
}

$arguments = New-Object System.Collections.Generic.List[string]
$arguments.Add('/i')
$arguments.Add('"' + $MsiPath + '"')
$arguments.Add('/qn')
$arguments.Add('/norestart')
$arguments.Add('REBOOT=ReallySuppress')
$arguments.Add('ADDDESKTOPSHORTCUT=""')
$arguments.Add('SHOWRELEASENOTES=""')
if (-not [string]::IsNullOrWhiteSpace($InstallDir)) {
    $InstallDir = [IO.Path]::GetFullPath($InstallDir)
    $arguments.Add('APPLICATIONFOLDER="' + $InstallDir + '"')
}

Write-Host "Verified MSI SHA-256: $actualSha256"
if (-not [string]::IsNullOrWhiteSpace($publisher)) { Write-Host "Verified MSI signer: $publisher" }
Write-Host "Verified MSI identity: $productName $productVersion"
Write-Host "Installing BricsCAD V25 silently from: $MsiPath"
$process = Start-Process -FilePath "msiexec.exe" -ArgumentList ([string]::Join(' ', $arguments)) -Wait -PassThru
if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
    throw "BricsCAD installer failed with msiexec exit code $($process.ExitCode)."
}

Write-Host "BricsCAD V25 installation completed. msiexec exit code: $($process.ExitCode)"
Write-Host "Licensing is intentionally not automated by this repository. Activate a valid BricsCAD V25 license/trial for the dedicated runner account, launch BricsCAD once interactively, choose the desired interface/workspace, then configure BRICSCAD_V25_DIR."
