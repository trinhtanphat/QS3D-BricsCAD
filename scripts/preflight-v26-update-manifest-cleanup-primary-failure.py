#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
path = root / "scripts" / "new-v26-update-manifest.ps1"
text = path.read_text(encoding="utf-8")

exact_cleanup = "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity"
workspace_cleanup = "Remove-HeldManifestWorkspace -Handle $workspaceHandle"
required = [
    "$primaryFailure = $null",
    "catch {\n    $primaryFailure = $_\n    throw\n}",
    "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
    "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
    exact_cleanup,
    workspace_cleanup,
    "Secondary V26 manifest held-stream cleanup failed while preserving the primary failure",
    "Secondary V26 manifest exact-script cleanup failed while preserving the primary failure",
    "Secondary V26 manifest held-workspace cleanup failed while preserving the primary failure",
    "[IO.FileShare]::Read",
    "Read-HeldStrictUtf8",
]
missing = [token for token in required if token not in text]
if missing:
    raise SystemExit("V26 update-manifest cleanup primary-failure guard missing: " + ", ".join(missing))

# Each secondary cleanup stage is strict when no primary failure exists, but its
# own cleanup exception is suppressed when the transformer/manifest operation is
# already failing so primary evidence is never replaced.
strict_rethrow = "if ($null -eq $primaryFailure) { throw }"
if text.count(strict_rethrow) < 3:
    raise SystemExit("V26 update-manifest cleanup must rethrow secondary cleanup failures on success for stream, script, and workspace stages.")

finally_index = text.index("finally {")
stream_dispose = text.index("$generatedStream.Dispose()", finally_index)
script_cleanup = text.index(exact_cleanup, stream_dispose)
workspace_cleanup_index = text.index(workspace_cleanup, script_cleanup)
workspace_dispose = text.index("$workspaceHandle.Dispose()", workspace_cleanup_index)
if not finally_index < stream_dispose < script_cleanup < workspace_cleanup_index < workspace_dispose:
    raise SystemExit("Cleanup order must be held-stream dispose -> exact script generation cleanup -> held workspace delete -> workspace handle dispose.")

for forbidden in (
    "Remove-V26ManifestTemporaryWorkspaceStrict",
    "Remove-V26ManifestTemporaryWorkspaceBestEffort",
    "Remove-Item -LiteralPath $tempScript",
    "Remove-Item -LiteralPath $tempRoot",
    "Remove-Item -LiteralPath $RootPath",
    "Remove-Item -LiteralPath $ScriptPath",
):
    if forbidden in text:
        raise SystemExit("V26 update-manifest primary-failure cleanup must not fall back to pathname cleanup: " + forbidden)

print("PASS V26 update-manifest preserves primary failure across held-generation cleanup and remains strict on success")
