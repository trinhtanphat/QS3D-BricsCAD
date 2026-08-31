#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnerTokenIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/generated-handle-owner-token-integrity.md"


def fail(message: str) -> None:
    print("ERROR: generated handle owner token integrity preflight failed: " + message)
    raise SystemExit(1)


for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        fail("missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

split_start = source.find("private static IReadOnlyList<string> SplitHandles(string raw)")
if split_start < 0:
    fail("fail-closed SplitHandles implementation missing")
split_region = source[split_start:]

required_source = [
    ".Split(new[] { ';' }, StringSplitOptions.None)",
    "var normalized = NormalizeHandleIdentity(token);",
    "if (normalized.Length == 0)",
    "if (!string.Equals(token, normalized, StringComparison.Ordinal))",
    "var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "if (!seen.Add(normalized))",
    "persisted ownership provenance is malformed",
    "contains non-canonical handle token",
    "contains duplicate handle token",
]
missing = [token for token in required_source if token not in split_region]
if missing:
    fail("source contract token(s) missing: " + repr(missing))

for forbidden in (
    "StringSplitOptions.RemoveEmptyEntries",
    ".Where(x => x.Length > 0)",
    ".Distinct(StringComparer.OrdinalIgnoreCase)",
):
    if forbidden in split_region:
        fail("SplitHandles must not silently repair malformed owner tokens: " + forbidden)

required_smoke = [
    "LeadingDelimiterFailsClosed",
    "TrailingDelimiterFailsClosed",
    "DoubleDelimiterFailsClosed",
    "WhitespaceOnlyTokenFailsClosed",
    "DuplicateTokenFailsClosed",
    "PaddedTokenFailsClosed",
    "NonCanonicalTokenFailsClosed",
    "MalformedOwnershipFailsAcrossPublicSurfacesBeforeCallback",
    "ValidMultiHandleOwnershipPreservesLogicalEquality",
    'Equal(0, callbacks, "malformed persisted ownership native callbacks")',
    'GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(owner)',
    'GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(owner)',
    'GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)',
    'GeneratedHandleOwnershipPolicy.TryFindOwner(project, "A"',
    'GeneratedHandleOwnershipPolicy.ValidateAllBeforeErase(',
]
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    fail("smoke contract token(s) missing: " + repr(missing_smoke))

for token in (
    "REMOTE_SAFE",
    "StringSplitOptions.None",
    "fail closed",
    "duplicate",
    "non-canonical",
    "zero native callback",
    "#4778",
    "NOT_APPLICABLE",
):
    if token not in runbook:
        fail("runbook token missing: " + token)

print("PASS generated handle owner token integrity")
