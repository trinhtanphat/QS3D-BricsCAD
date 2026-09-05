#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
path = root / "scripts" / "new-v26-update-manifest.ps1"
text = path.read_text(encoding="utf-8")

required = [
    "$primaryFailure = $null",
    "catch {\n    $primaryFailure = $_\n    throw\n}",
    "if ($null -eq $primaryFailure)",
    "Remove-V26ManifestTemporaryWorkspaceStrict",
    "Remove-V26ManifestTemporaryWorkspaceBestEffort",
    "still exists after cleanup",
    "Secondary V26 manifest held-stream cleanup failed while preserving the primary failure",
    "Secondary V26 manifest script cleanup failed while preserving the primary failure",
    "Secondary V26 manifest workspace cleanup failed while preserving the primary failure",
    "[IO.FileShare]::Read",
    "Read-HeldStrictUtf8",
    "refusing recursive cleanup",
]
missing = [token for token in required if token not in text]
if missing:
    raise SystemExit("V26 update-manifest cleanup primary-failure guard missing: " + ", ".join(missing))

primary_branch = text.index("if ($null -eq $primaryFailure)")
strict_call = text.index("Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath", primary_branch)
best_effort_call = text.index("Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath", primary_branch)
secondary_stream = text.index("Secondary V26 manifest held-stream cleanup failed while preserving the primary failure", primary_branch)
if not (primary_branch < strict_call < secondary_stream < best_effort_call):
    raise SystemExit("Cleanup branches are not ordered strict-on-success / suppressed-secondary-on-primary-failure.")

if "Remove-Item -LiteralPath $RootPath -Recurse" in text or "Remove-Item -LiteralPath $tempRoot -Recurse" in text:
    raise SystemExit("Recursive cleanup is forbidden for the V26 manifest temporary workspace.")

print("PASS V26 update-manifest preserves primary failure across stream/path cleanup and retains strict success cleanup")
