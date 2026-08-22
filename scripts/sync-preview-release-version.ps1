param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tag = $ReleaseTag.Trim()
$match = [regex]::Match(
    $tag,
    '^v(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)-preview\.(?<preview>[1-9][0-9]*)$',
    [Text.RegularExpressions.RegexOptions]::CultureInvariant)

if (-not $match.Success) {
    throw "ReleaseTag must use the exact preview shape v<major>.<minor>.<patch>-preview.<n>. Got: $ReleaseTag"
}

$parts = @(
    [int64]$match.Groups['major'].Value,
    [int64]$match.Groups['minor'].Value,
    [int64]$match.Groups['patch'].Value,
    [int64]$match.Groups['preview'].Value
)
foreach ($part in $parts) {
    if ($part -gt 65535) {
        throw "Preview release components must fit FileVersion's 0..65535 range. Got: $tag"
    }
}

$productVersion = '{0}.{1}.{2}-preview.{3}' -f $parts[0], $parts[1], $parts[2], $parts[3]
$fileVersion = '{0}.{1}.{2}.{3}' -f $parts[0], $parts[1], $parts[2], $parts[3]
$projects = @(
    'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj',
    'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj',
    'src/QS3D.Core/QS3D.Core.csproj'
)

function Replace-SingleProjectValue {
    param(
        [string]$Text,
        [string]$Element,
        [string]$Value,
        [string]$ProjectPath
    )

    $pattern = '(<{0}>)[^<]*(</{0}>)' -f [regex]::Escape($Element)
    $matches = [regex]::Matches($Text, $pattern, [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if ($matches.Count -ne 1) {
        throw "$ProjectPath must declare exactly one <$Element> value; found $($matches.Count)."
    }
    return [regex]::Replace(
        $Text,
        $pattern,
        ('${1}' + $Value + '${2}'),
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
}

$utf8NoBom = New-Object Text.UTF8Encoding($false)
$changed = @()
foreach ($relative in $projects) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Project file was not found: $relative"
    }

    $original = [IO.File]::ReadAllText($path)
    $updated = Replace-SingleProjectValue -Text $original -Element 'Version' -Value $productVersion -ProjectPath $relative
    $updated = Replace-SingleProjectValue -Text $updated -Element 'FileVersion' -Value $fileVersion -ProjectPath $relative
    $updated = Replace-SingleProjectValue -Text $updated -Element 'InformationalVersion' -Value $productVersion -ProjectPath $relative

    if (-not [string]::Equals($original, $updated, [StringComparison]::Ordinal)) {
        [IO.File]::WriteAllText($path, $updated, $utf8NoBom)
        $changed += $relative
    }
}

foreach ($relative in $projects) {
    $path = Join-Path $root $relative
    [xml]$project = [IO.File]::ReadAllText($path)
    $versions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $fileVersions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.FileVersion } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $informationalVersions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.InformationalVersion } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($versions.Count -ne 1 -or $versions[0].Trim() -ne $productVersion) {
        throw "$relative Version did not synchronize to $productVersion."
    }
    if ($fileVersions.Count -ne 1 -or $fileVersions[0].Trim() -ne $fileVersion) {
        throw "$relative FileVersion did not synchronize to $fileVersion."
    }
    if ($informationalVersions.Count -ne 1 -or $informationalVersions[0].Trim() -ne $productVersion) {
        throw "$relative InformationalVersion did not synchronize to $productVersion."
    }
}

if ($changed.Count -eq 0) {
    Write-Host "Preview product identity already matches $tag; no source files changed."
}
else {
    Write-Host "Synchronized preview product identity to $tag in:"
    $changed | ForEach-Object { Write-Host " - $_" }
}