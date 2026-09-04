#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-script-from-v25.ps1"


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        raise SystemExit(f"ERROR: V26 template admission guard missing {label}: {token}")


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        raise SystemExit(f"ERROR: V26 template admission guard found forbidden {label}: {token}")


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")

    # The template must be transformed from bytes captured through one admitted
    # read handle, not from a later pathname reopen.
    require(source, "[IO.File]::Open($sourceFull", "handle-bound template open")
    require(source, "[IO.FileShare]::Read", "write/delete sharing refusal")
    require(source, "$sourceStream.CopyTo($memory)", "capture from admitted handle")
    require(source, "$sourceBytes = $memory.ToArray()", "detached admitted bytes")
    forbid(source, "Get-Content -LiteralPath $sourceFull -Raw", "pathname source reopen")
    forbid(source, "Get-FileHash -LiteralPath $sourceFull", "pathname hash reopen")

    # Identity/path/length evidence must be observed through the same handle on
    # both sides of capture so a redirected/replaced template fails closed.
    require(source, "GetFileInformationByHandle", "handle identity inspection")
    require(source, "GetFinalPathNameByHandle", "handle path inspection")
    require(source, "$beforeInfo = Get-AdmittedHandleInformation", "pre-capture identity")
    require(source, "$afterInfo = Get-AdmittedHandleInformation", "post-capture identity")
    require(source, "Test-SameHandleIdentity -Before $beforeInfo -After $afterInfo", "identity stability fence")
    require(source, "$beforePath = Get-AdmittedHandlePath", "pre-capture resolved path")
    require(source, "$afterPath = Get-AdmittedHandlePath", "post-capture resolved path")
    require(source, "Admitted V25 template length changed during capture.", "capture length fence")

    # Invalid UTF-8 must be rejected rather than replacement-decoded, and the
    # evidence hash must be computed from exactly the captured bytes.
    require(source, "Text.UTF8Encoding($false, $true)", "strict UTF-8 decoder")
    require(source, "$utf8.GetString($sourceBytes", "decode admitted bytes")
    require(source, "$sha256.ComputeHash($sourceBytes)", "admitted-byte SHA-256")
    require(source, 'Write-Host "Template SHA256: $templateHash"', "captured hash publication")

    # Preserve existing V26 parity/output safety architecture while hardening
    # template admission.
    for token, label in (
        ("$text.Replace('V25', 'V26').Replace('v25', 'v26')", "narrow host-major transform"),
        ("QS3D.BricsCAD.V26.runtimeconfig.json", "V26 runtimeconfig delta"),
        ("Assert-DirectoryAncestorChain -Path $parent", "output ancestor containment"),
        ("Assert-SafeExistingOutputLeaf -Path $outputFull", "output leaf safety"),
        ("[IO.File]::Replace($stagePath, $outputFull, $null)", "atomic replacement"),
        ("[IO.File]::Move($stagePath, $outputFull)", "atomic first publication"),
    ):
        require(source, token, label)

    open_index = source.index("[IO.File]::Open($sourceFull")
    capture_index = source.index("$sourceStream.CopyTo($memory)")
    decode_index = source.index("$utf8.GetString($sourceBytes")
    transform_index = source.index("$text.Replace('V25', 'V26')")
    publish_index = source.index("[IO.File]::WriteAllText($stagePath")
    if not open_index < capture_index < decode_index < transform_index < publish_index:
        raise SystemExit("ERROR: V26 template admission ordering is not open -> capture -> strict decode -> transform -> publish")

    print("PASS V26 script template admitted-byte identity guard")


if __name__ == "__main__":
    main()
