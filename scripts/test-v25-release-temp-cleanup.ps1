$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$helper = Join-Path $PSScriptRoot 'remove-v25-release-temp-tree.ps1'
if (-not (Test-Path -LiteralPath $helper -PathType Leaf)) {
    throw "V25 release temp cleanup helper was not found: $helper"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-release-cleanup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $outside = Join-Path $tempRoot 'outside'
    New-Item -ItemType Directory -Path $outside -Force | Out-Null
    $outsideSentinel = Join-Path $outside 'sentinel.txt'
    Set-Content -LiteralPath $outsideSentinel -Value 'outside-must-survive' -Encoding ASCII

    $cleanup = Join-Path $tempRoot 'cleanup-tree'
    $nested = Join-Path $cleanup 'nested'
    New-Item -ItemType Directory -Path $nested -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $nested 'payload.txt') -Value 'delete-me' -Encoding ASCII
    New-Item -ItemType Junction -Path (Join-Path $cleanup 'outside-link') -Target $outside -ErrorAction Stop | Out-Null

    & $helper -Path $cleanup -ExpectedParent $tempRoot
    if (Test-Path -LiteralPath $cleanup) {
        throw 'Generation-bound release cleanup left the admitted ordinary tree behind.'
    }
    if (-not (Test-Path -LiteralPath $outsideSentinel -PathType Leaf)) {
        throw 'Generation-bound release cleanup followed a child junction and deleted the outside sentinel.'
    }
    if ((Get-Content -LiteralPath $outsideSentinel -Raw).Trim() -ne 'outside-must-survive') {
        throw 'Generation-bound release cleanup modified the outside sentinel through a child junction.'
    }

    # Also prove a substituted top-level cleanup pathname that is itself a junction is deleted as a
    # reparse entry, not traversed into its target generation.
    $substituted = Join-Path $tempRoot 'substituted-root'
    New-Item -ItemType Junction -Path $substituted -Target $outside -ErrorAction Stop | Out-Null
    & $helper -Path $substituted -ExpectedParent $tempRoot
    if (Test-Path -LiteralPath $substituted) {
        throw 'Generation-bound release cleanup left the substituted root junction behind.'
    }
    if (-not (Test-Path -LiteralPath $outsideSentinel -PathType Leaf)) {
        throw 'Generation-bound release cleanup followed a substituted root junction and deleted outside data.'
    }

    Write-Host 'V25 generation-bound release temp cleanup runtime tests passed.'
}
finally {
    # Test-fixture cleanup is outside the production trust-boundary helper. Remove any leftovers best-effort.
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
