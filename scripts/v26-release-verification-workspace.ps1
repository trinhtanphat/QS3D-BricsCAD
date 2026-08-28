[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Create', 'Child', 'Cleanup')]
    [string]$Operation,

    [string]$TempRoot,
    [string]$Workspace,
    [string]$ChildName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$WorkspacePrefix = 'qs3d-v26-release-verify-'

function Get-CanonicalAbsolutePath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Label must not be empty."
    }
    $trimmed = $Path.Trim()
    if (-not [IO.Path]::IsPathRooted($trimmed)) {
        throw "$Label must be an absolute path."
    }
    try { return [IO.Path]::GetFullPath($trimmed) }
    catch { throw "$Label is not a valid filesystem path." }
}

function Assert-NoExistingReparseComponent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path -Label $Label
    $root = [IO.Path]::GetPathRoot($canonical)
    if ([string]::IsNullOrWhiteSpace($root)) { throw "$Label must have a filesystem root." }

    $current = $root
    $relative = $canonical.Substring($root.Length)
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { break }
        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point."
        }
    }
}

function Get-TrustedDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path -Label $Label
    Assert-NoExistingReparseComponent -Path $canonical -Label $Label
    if (-not (Test-Path -LiteralPath $canonical -PathType Container)) {
        throw "$Label must exist as a directory."
    }
    $item = Get-Item -LiteralPath $canonical -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse directory."
    }
    return $canonical
}

function Get-OwnedWorkspace {
    param([Parameter(Mandatory = $true)][string]$Path)

    $canonical = Get-TrustedDirectory -Path $Path -Label 'Workspace'
    $name = [IO.Path]::GetFileName($canonical.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
    if (-not $name.StartsWith($WorkspacePrefix, [StringComparison]::Ordinal) -or $name.Length -ne ($WorkspacePrefix.Length + 32)) {
        throw 'Workspace does not have the exact runner-owned nonce shape.'
    }
    $nonce = $name.Substring($WorkspacePrefix.Length)
    if ($nonce -notmatch '^[0-9a-f]{32}$') { throw 'Workspace nonce is invalid.' }
    return $canonical
}

switch ($Operation) {
    'Create' {
        $root = Get-TrustedDirectory -Path $TempRoot -Label 'TempRoot'
        $workspaceName = $WorkspacePrefix + [Guid]::NewGuid().ToString('N')
        $candidate = Join-Path $root $workspaceName
        if (Test-Path -LiteralPath $candidate) { throw 'Verification workspace already exists.' }
        $created = [IO.Directory]::CreateDirectory($candidate)
        $canonical = Get-OwnedWorkspace -Path $created.FullName
        $parent = [IO.Directory]::GetParent($canonical)
        if ($null -eq $parent -or -not [string]::Equals($parent.FullName.TrimEnd('\'), $root.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Verification workspace parent changed during creation.'
        }
        Write-Output $canonical
        break
    }
    'Child' {
        $owned = Get-OwnedWorkspace -Path $Workspace
        if ([string]::IsNullOrWhiteSpace($ChildName) -or $ChildName -notmatch '^asset-[0-9a-f]{32}$') {
            throw 'ChildName must be an owned asset nonce.'
        }
        $child = [IO.Path]::GetFullPath((Join-Path $owned $ChildName))
        $expectedPrefix = $owned.TrimEnd('\') + [IO.Path]::DirectorySeparatorChar
        if (-not $child.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Verification child path escaped the owned workspace.'
        }
        if (Test-Path -LiteralPath $child) { throw 'Verification child already exists.' }
        Write-Output $child
        break
    }
    'Cleanup' {
        $owned = Get-OwnedWorkspace -Path $Workspace
        foreach ($entry in @(Get-ChildItem -LiteralPath $owned -Force)) {
            if ($entry.PSIsContainer -or ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $entry.Name -notmatch '^asset-[0-9a-f]{32}$') {
                throw 'Verification workspace contains an unexpected or unsafe entry; refusing recursive cleanup.'
            }
        }
        Remove-Item -LiteralPath $owned -Recurse -Force
        if (Test-Path -LiteralPath $owned) { throw 'Verification workspace cleanup did not complete.' }
        break
    }
}
