#!/usr/bin/env python3
"""Fail-closed source guard for V26 MSI execution-copy isolation."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
text = SOURCE.read_text(encoding="utf-8")

required_literals = [
    "$executionMsi",
    "$executionStream = [IO.File]::Open(",
    "[IO.FileMode]::CreateNew",
    "[IO.FileAccess]::Write",
    "[IO.FileShare]::None",
    "$admission.Stream.Position = 0",
    "$admission.Stream.CopyTo($executionStream)",
    "$executionStream.Flush($true)",
    "Get-FileHash -LiteralPath $executionMsi -Algorithm SHA256",
    "execution MSI SHA-256 mismatch before administrative extraction",
    "execution MSI SHA-256 mismatch after administrative extraction",
    "Assert-HeldInstallerStable -Held $admission -Phase 'before optional reference extraction'",
    "Assert-HeldInstallerStable -Held $admission -Phase 'after administrative extraction'",
    "Remove-Item -LiteralPath $executionMsi -Force -ErrorAction SilentlyContinue",
]

missing = [literal for literal in required_literals if literal not in text]
if missing:
    print("ERROR: V26 MSI execution-copy guard missing required source contract:")
    for literal in missing:
        print(f" - {literal}")
    sys.exit(1)

extract_block = re.search(
    r"if \(\$ExtractReferences\) \{(?P<body>.*?)\n\s*\}\n\n\s*Write-Host \"Verified BricsCAD V26\.2\.07 MSI SHA256",
    text,
    flags=re.DOTALL,
)
if not extract_block:
    print("ERROR: could not locate V26 ExtractReferences block.")
    sys.exit(1)

body = extract_block.group("body")

argument_match = re.search(r"\$arguments\s*=\s*@\((?P<args>.*?)\)\s*\n", body, flags=re.DOTALL)
if not argument_match:
    print("ERROR: could not locate V26 msiexec administrative-extraction arguments.")
    sys.exit(1)

arguments = argument_match.group("args")
if "$executionMsi" not in arguments:
    print("ERROR: V26 administrative extraction must invoke msiexec against the fresh execution MSI copy.")
    sys.exit(1)
if "$admission.Path" in arguments:
    print("ERROR: V26 administrative extraction must not invoke msiexec against the held canonical MSI path.")
    sys.exit(1)

copy_index = body.find("$admission.Stream.CopyTo($executionStream)")
pre_hash_index = body.find("execution MSI SHA-256 mismatch before administrative extraction")
arguments_index = body.find("$arguments = @")
post_hash_index = body.find("execution MSI SHA-256 mismatch after administrative extraction")
cleanup_index = body.find("Remove-Item -LiteralPath $executionMsi -Force -ErrorAction SilentlyContinue")
if not (0 <= copy_index < pre_hash_index < arguments_index < post_hash_index < cleanup_index):
    print("ERROR: V26 MSI execution-copy lifecycle is not ordered copy -> pre-hash -> msiexec -> post-hash -> cleanup.")
    sys.exit(1)

print("PASS: V26 administrative extraction uses a digest-verified execution copy while the canonical admitted MSI remains held and immutable.")