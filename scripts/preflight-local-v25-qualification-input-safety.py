#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMPLETE = ROOT / "scripts" / "complete-local-v25-qualification.ps1"
EVIDENCE = ROOT / "scripts" / "test-local-v25-interactive-matrix-evidence.ps1"


def fail(message: str) -> None:
    print(f"::error::{message}")
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: missing required contract: {needle}")


def forbid(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, flags=re.IGNORECASE | re.MULTILINE):
        fail(f"{label}: forbidden unsafe pattern matched: {pattern}")


def main() -> int:
    complete = COMPLETE.read_text(encoding="utf-8")
    evidence = EVIDENCE.read_text(encoding="utf-8")

    for text, label in ((complete, COMPLETE.name), (evidence, EVIDENCE.name)):
        require(text, "[IO.FileAttributes]::ReparsePoint", label)
        require(text, "New-Object IO.FileStream", label)
        require(text, "[IO.FileShare]::Read", label)
        require(text, "New-Object System.Text.UTF8Encoding($false, $true)", label)
        require(text, ".ReadByte() -ne -1", label)
        forbid(text, r"Get-Content\s+[^\r\n]*-Raw", label)

    require(complete, "$MaxQualificationJsonBytes = 1048576", COMPLETE.name)
    require(complete, "Get-SafeInputFile -Path $reportPath", COMPLETE.name)
    require(complete, "Read-StrictUtf8File -File $reportFile", COMPLETE.name)
    require(complete, "[IO.File]::WriteAllBytes($tempPath, $bytes)", COMPLETE.name)
    require(complete, "[IO.File]::Replace($tempPath, $Destination, $null)", COMPLETE.name)
    require(complete, "Completed qualification.json would exceed", COMPLETE.name)
    require(complete, "finally {", COMPLETE.name)
    require(complete, "Remove-Item -LiteralPath $tempPath -Force", COMPLETE.name)

    read_index = complete.find("Read-StrictUtf8File -File $reportFile")
    json_index = complete.find("ConvertFrom-Json")
    if read_index < 0 or json_index < 0 or read_index > json_index:
        fail("qualification.json must pass bounded strict-UTF8 read before JSON parsing")

    replace_index = complete.find("[IO.File]::Replace($tempPath, $Destination, $null)")
    write_index = complete.find("[IO.File]::WriteAllBytes($tempPath, $bytes)")
    if write_index < 0 or replace_index < 0 or write_index > replace_index:
        fail("qualification report completion must write a sibling temp file before atomic replacement")

    require(evidence, "$MaxEvidenceBytes = 1048576", EVIDENCE.name)
    require(evidence, "Read-SafeEvidenceText -Path $EvidencePath", EVIDENCE.name)
    require(evidence, "Interactive matrix evidence must be an ordinary non-reparse file", EVIDENCE.name)
    require(evidence, "Interactive matrix evidence is not strict UTF-8", EVIDENCE.name)

    safe_read_index = evidence.find("Read-SafeEvidenceText -Path $EvidencePath")
    evidence_json_index = evidence.find("ConvertFrom-Json")
    if safe_read_index < 0 or evidence_json_index < 0 or safe_read_index > evidence_json_index:
        fail("interactive evidence must pass bounded strict-UTF8 read before JSON parsing")

    print("Local V25 qualification input-safety preflight PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
