#!/usr/bin/env python3
"""Fail-closed source guard for V26 MSI administrative-extraction exit 1603."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
text = SOURCE.read_text(encoding="utf-8")

required_literals = [
    "function Get-CompleteV26ReferenceDirectory",
    "$process.ExitCode -eq 1603",
    "Get-CompleteV26ReferenceDirectory -Root $extract",
    "BrxMgd.dll",
    "TD_Mgd.dll",
    "TD_MgdBrep.dll",
    "returned exit code 1603 after materializing a complete managed-reference payload",
    "BricsCAD V26 MSI administrative extraction failed with exit code $($process.ExitCode).",
]

missing = [literal for literal in required_literals if literal not in text]
if missing:
    print("ERROR: V26 MSI 1603 recovery guard missing required source contract:")
    for literal in missing:
        print(f" - {literal}")
    sys.exit(1)

# The 1603 exception may be tolerated only when a complete reference directory was
# found in the fresh extraction tree. Other non-success MSI exit codes must still throw.
exit_block = re.search(
    r"if \(\$process\.ExitCode -notin @\(0, 3010\)\) \{(?P<body>.*?)\n\s*\}\n\s*Assert-HeldInstallerStable",
    text,
    flags=re.DOTALL,
)
if not exit_block:
    print("ERROR: could not locate bounded non-success MSI exit handling block.")
    sys.exit(1)

body = exit_block.group("body")
contract_patterns = [
    r"\$process\.ExitCode -eq 1603",
    r"Get-CompleteV26ReferenceDirectory -Root \$extract",
    r"IsNullOrWhiteSpace\(\[string\]\$completeReferenceDirAfter1603\)",
    r"else\s*\{.*?throw \"BricsCAD V26 MSI administrative extraction failed with exit code \$\(\$process\.ExitCode\)\.\"",
]
for pattern in contract_patterns:
    if not re.search(pattern, body, flags=re.DOTALL):
        print(f"ERROR: V26 MSI 1603 recovery block does not satisfy pattern: {pattern}")
        sys.exit(1)

print("PASS: V26 MSI exit 1603 is recoverable only after complete reference-payload validation; all other failures remain fail-closed.")
