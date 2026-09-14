#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "function Get-JsonTopLevelArrayObjectTexts",
    "function Assert-JsonPropertyCounts",
    "function Assert-JsonArrayObjectPropertyCounts",
    "Version = 1",
    "Files = 1",
    "$hostFileExpectedPropertyCounts = @{",
    "Name = 1",
    "Path = 1",
    "Sha256 = 1",
    "Length = 1",
    "-ArrayPropertyName 'Files'",
    "-ExpectedArrayObjectCount $requiredHostNames.Count",
    "-ExpectedArrayObjectPropertyCounts $hostFileExpectedPropertyCounts",
    "product = 1",
    "target = 1",
    "framework = 1",
    "productVersion = 1",
    "Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts",
    "RenameOwnedProvenanceGeneration",
    "SetFileInformationByHandleBuffer",
    "SetFilePointerEx",
    "Marshal.AllocHGlobal(1)",
    "Marshal.WriteByte(disposition, 1)",
    "FileDispositionInfo, disposition, 1",
)
for token in required_tokens:
    if token not in source:
        raise SystemExit(f"ERROR: V26 provenance-generator input uniqueness contract missing: {token}")

helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
reader_start = source.index("function Read-StrictUtf8Json")
host_parse = source.index("return $text | ConvertFrom-Json -ErrorAction Stop", reader_start)
host_root_assert = source.index("Assert-JsonPropertyCounts -JsonText $text", reader_start)
host_array_assert = source.index("Assert-JsonArrayObjectPropertyCounts -JsonText $text", reader_start)
if not (helper_start < reader_start < host_root_assert < host_array_assert < host_parse):
    raise SystemExit("ERROR: host-reference-state root and Files[] cardinality must be proved before ConvertFrom-Json")

host_expected = source.index("$hostExpectedPropertyCounts = @{")
host_record_expected = source.index("$hostFileExpectedPropertyCounts = @{")
host_call = source.index("$hostState = Read-StrictUtf8Json")
if not (host_expected < host_record_expected < host_call):
    raise SystemExit("ERROR: host-reference-state root/record cardinalities must be bound before the JSON reader is invoked")

metadata_expected = source.index("$metadataExpectedPropertyCounts = @{")
metadata_assert = source.index("Assert-JsonPropertyCounts -JsonText $metadataText")
metadata_parse = source.index("$metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop")
if not (metadata_expected < metadata_assert < metadata_parse):
    raise SystemExit("ERROR: PACKAGE-METADATA identity cardinality must be proved before ConvertFrom-Json")

native_start = source.index("public static class Qs3dProvenanceGenerationNative")
create_start = source.index("public static SafeFileHandle CreateOwnedProvenanceGeneration", native_start)
create_end = source.index("public static SafeFileHandle OpenPinnedPublishedProvenanceGeneration", create_start)
create_block = source[create_start:create_end]
if "FileShareRead," not in create_block or "FileShareWrite" in create_block or "FileShareDelete" in create_block:
    raise SystemExit("ERROR: owned V26 provenance staging handle must deny write/delete sharing")

remove_start = source.index("public static void RemoveOwnedProvenanceGeneration", native_start)
remove_end = source.index("}", remove_start)
remove_block = source[remove_start:remove_end]
for token in ("Marshal.AllocHGlobal(1)", "Marshal.WriteByte(disposition, 1)", "FileDispositionInfo, disposition, 1"):
    if token not in remove_block:
        raise SystemExit("ERROR: V26 provenance FILE_DISPOSITION_INFO must use the native one-byte BOOLEAN layout")
if "MarshalAs(UnmanagedType.Bool)" in remove_block or "Marshal.SizeOf" in remove_block:
    raise SystemExit("ERROR: V26 provenance delete disposition must not marshal FILE_DISPOSITION_INFO BOOLEAN as a 4-byte BOOL")

publication_start = source.index("$tempGeneration = New-OwnedProvenanceGeneration")
publication_end = source.index("[pscustomobject]@{ SourceCommit", publication_start)
publication = source[publication_start:publication_end]
rename_owned = publication.index("[Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration")
identity_after = publication.index("$renamedIdentity = Get-OwnedProvenanceGenerationIdentity")
bytes_after = publication.index("Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration")
commit_after = publication.index("$publicationCommitted = $true")
close_after = publication.index("Close-OwnedProvenanceGeneration -Generation $tempGeneration")
rollback_after = publication.index("Remove-OwnedProvenanceGeneration -Generation $tempGeneration")
if not (rename_owned < identity_after < bytes_after < commit_after < close_after):
    raise SystemExit("ERROR: V26 provenance publication must rename, verify identity/bytes, commit, then close the same owned handle")
if rollback_after < rename_owned:
    raise SystemExit("ERROR: V26 provenance rollback must remain available after handle-owned rename")
if "[IO.File]::Move(" in publication or "[IO.File]::Replace(" in publication or "Open-PinnedPublishedProvenanceGeneration -Path" in publication:
    raise SystemExit("ERROR: V26 provenance publication must not reopen or pathname-move the admitted generation")

native_wrapper_start = source.index("if (-not ('Qs3dProvenanceGenerationNative' -as [type]))")
native_wrapper_end = source.index("function New-OwnedProvenanceGeneration", native_wrapper_start)
native_wrapper = source[native_wrapper_start:native_wrapper_end]
helpers = source[helper_start:reader_start]

probe = native_wrapper + "\n" + helpers + r'''
$rootCases = @(
    @{ Json='{"Name":"bricscad.exe"}'; Name='Name'; Expected=1 },
    @{ Json='{"Name":"bricscad.exe","Name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Na\u006de":"bricscad.exe","name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Sha256":"a","SHA256":"b"}'; Name='Sha256'; Expected=2 },
    @{ Json='{"product":"QS3D","PRODUCT":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","extension":{"product":"diagnostic"}}'; Name='product'; Expected=1 },
    @{ Json='{"Version":1,"Files":[],"extension":{"Version":99}}'; Name='Version'; Expected=1 },
    @{ Json='{"framework":"net8.0-windows","extension":[{"framework":"ignored"}]}'; Name='framework'; Expected=1 }
)
foreach ($case in $rootCases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "provenance-generator root/path probe failed for $($case.Name): expected $($case.Expected), got $actual"
    }
}

$validHost = '{"Version":1,"Files":[{"Name":"a","Path":"p1","Sha256":"s1","Length":1,"extension":{"Name":"nested"}},{"Name":"b","Path":"p2","Sha256":"s2","Length":2}],"extension":{"Files":[]}}'
$recordCounts = @{ Name=1; Path=1; Sha256=1; Length=1 }
$objects = @(Get-JsonTopLevelArrayObjectTexts -JsonText $validHost -ArrayPropertyName 'Files' -Label 'probe')
if ($objects.Count -ne 2) { throw "provenance-generator Files[] probe expected 2 direct objects, got $($objects.Count)" }
Assert-JsonArrayObjectPropertyCounts -JsonText $validHost -ArrayPropertyName 'Files' -ExpectedObjectCount 2 -ExpectedPropertyCounts $recordCounts -Label 'probe'

$duplicateRecord = '{"Version":1,"Files":[{"Name":"a","NAME":"evil","Path":"p","Sha256":"s","Length":1}]}'
$rejected = $false
try {
    Assert-JsonArrayObjectPropertyCounts -JsonText $duplicateRecord -ArrayPropertyName 'Files' -ExpectedObjectCount 1 -ExpectedPropertyCounts $recordCounts -Label 'probe'
}
catch { $rejected = $true }
if (-not $rejected) { throw 'provenance-generator Files[] duplicate record identity was not rejected' }

$escapedDuplicateRecord = '{"Version":1,"Files":[{"Name":"a","Na\u006de":"evil","Path":"p","Sha256":"s","Length":1}]}'
$rejected = $false
try {
    Assert-JsonArrayObjectPropertyCounts -JsonText $escapedDuplicateRecord -ArrayPropertyName 'Files' -ExpectedObjectCount 1 -ExpectedPropertyCounts $recordCounts -Label 'probe'
}
catch { $rejected = $true }
if (-not $rejected) { throw 'provenance-generator Files[] escaped-equivalent duplicate record identity was not rejected' }

function Assert-OwnedPublication([string]$SourcePath, [string]$DestinationPath, [byte[]]$Payload, [bool]$ReplaceExisting) {
    $owned = $null
    try {
        $owned = [Qs3dProvenanceGenerationNative]::CreateOwnedProvenanceGeneration($SourcePath, $Payload)
        $before = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($owned)
        [Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration($owned, $DestinationPath, $ReplaceExisting)
        $after = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($owned)
        if (-not [string]::Equals($before, $after, [StringComparison]::Ordinal)) { throw 'owned provenance identity changed across handle rename' }
        if (Test-Path -LiteralPath $SourcePath) { throw 'owned provenance source pathname still exists after handle rename' }
        if (-not (Test-Path -LiteralPath $DestinationPath -PathType Leaf)) { throw 'owned provenance destination missing after handle rename' }

        $writerBlocked = $false
        try {
            $other = [IO.File]::Open($DestinationPath, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
            $other.Dispose()
        }
        catch [IO.IOException] { $writerBlocked = $true }
        if (-not $writerBlocked) { throw 'owned provenance handle did not block a concurrent writer during publication verification' }

        $actualBytes = [Qs3dProvenanceGenerationNative]::ReadPinnedPublishedProvenanceBytes($owned, $Payload.Length)
        if ($actualBytes.Length -ne $Payload.Length) { throw 'owned provenance payload length changed after handle rename' }
        for ($i = 0; $i -lt $Payload.Length; $i++) {
            if ($actualBytes[$i] -ne $Payload[$i]) { throw 'owned provenance payload changed after handle rename' }
        }

        [Qs3dProvenanceGenerationNative]::RemoveOwnedProvenanceGeneration($owned)
        $owned.Dispose()
        $owned = $null
        if (Test-Path -LiteralPath $DestinationPath) { throw 'owned provenance destination remained after delete-on-close handle disposal' }
    }
    finally {
        if ($null -ne $owned) {
            try { [Qs3dProvenanceGenerationNative]::RemoveOwnedProvenanceGeneration($owned) }
            finally { $owned.Dispose() }
        }
    }
}

$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v26-provenance-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($probeDir) | Out-Null
try {
    $destinationPath = Join-Path $probeDir 'published.json'
    $firstSource = Join-Path $probeDir 'owned-first.tmp'
    $firstPayload = [Text.UTF8Encoding]::new($false, $true).GetBytes('{"probe":"first"}')
    Assert-OwnedPublication -SourcePath $firstSource -DestinationPath $destinationPath -Payload $firstPayload -ReplaceExisting $false
    if (Test-Path -LiteralPath $destinationPath) { throw 'first owned provenance generation remained after handle-owned cleanup' }

    [IO.File]::WriteAllText($destinationPath, '{"old":true}', [Text.UTF8Encoding]::new($false))
    $secondSource = Join-Path $probeDir 'owned-replacement.tmp'
    $secondPayload = [Text.UTF8Encoding]::new($false, $true).GetBytes('{"probe":"replacement"}')
    Assert-OwnedPublication -SourcePath $secondSource -DestinationPath $destinationPath -Payload $secondPayload -ReplaceExisting $true
    if (Test-Path -LiteralPath $destinationPath) { throw 'replacement owned provenance generation remained after handle-owned cleanup' }
}
finally {
    if (Test-Path -LiteralPath $probeDir) { Remove-Item -LiteralPath $probeDir -Recurse -Force -ErrorAction SilentlyContinue }
}
'''

with tempfile.NamedTemporaryFile("w", suffix=".ps1", encoding="utf-8", delete=False) as tmp:
    tmp.write(probe)
    probe_path = Path(tmp.name)
try:
    completed = subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(probe_path)],
        capture_output=True,
        text=True,
        timeout=30,
    )
finally:
    probe_path.unlink(missing_ok=True)
if completed.returncode != 0:
    raise SystemExit(
        "ERROR: behavioral V26 provenance-generator identity/publication probe failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 provenance generator proves path-scoped input identity and handle-owned rollback-safe publication behavior")
