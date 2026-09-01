#!/usr/bin/env python3
"""Fail closed if V26 self-hosted qualification regains repository write authority."""

from pathlib import Path

WORKFLOW = Path(".github/workflows/release-v26.yml")
text = WORKFLOW.read_text(encoding="utf-8")


def require(token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(f"V26 publish-token isolation preflight failed: {message}")


def reject(token: str, message: str) -> None:
    if token in text:
        raise SystemExit(f"V26 publish-token isolation preflight failed: {message}")


require(
    "permissions:\n  contents: read\n",
    "workflow default must be contents: read so self-hosted qualification cannot inherit repository write authority.",
)
require(
    "jobs:\n  qualify:\n",
    "V26 workflow must isolate self-hosted qualification in a dedicated qualify job.",
)
require(
    "  qualify:\n    if: ${{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' }}\n    runs-on: [self-hosted, windows, x64, bricscad-v26]\n    permissions:\n      contents: read\n",
    "self-hosted V26 qualify job must explicitly remain contents: read.",
)
require(
    "  release:\n    if: ${{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' }}\n    needs: qualify\n    runs-on: windows-latest\n    permissions:\n      contents: write\n",
    "repository write authority must exist only on the hosted release job after qualification.",
)
require(
    "actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c # v8",
    "hosted publisher must consume the frozen qualification artifact instead of rebuilding on the write-capable job.",
)
require(
    "ref: ${{ github.sha }}\n          fetch-depth: 0\n          persist-credentials: false",
    "hosted publisher checkout must remain pinned to the exact workflow SHA with credentials disabled.",
)

qualify_start = text.index("  qualify:\n")
release_start = text.index("  release:\n", qualify_start + 1)
publish_step = text.index("      - name: Publish V26 GitHub Release\n")
upload_step = text.index("      - name: Upload V26 qualification artifacts\n")

if not (qualify_start < upload_step < release_start < publish_step):
    raise SystemExit(
        "V26 publish-token isolation preflight failed: qualification artifacts must be frozen before the hosted publish job starts."
    )

qualify_block = text[qualify_start:release_start]
release_block = text[release_start:]
reject_in_qualify = (
    "contents: write",
    "GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}",
    "Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"",
)
for token in reject_in_qualify:
    if token in qualify_block:
        raise SystemExit(
            f"V26 publish-token isolation preflight failed: self-hosted qualify job contains publish authority token {token!r}."
        )

if release_block.count("contents: write") != 1:
    raise SystemExit(
        "V26 publish-token isolation preflight failed: hosted release job must be the sole contents: write grant."
    )
if "runs-on: [self-hosted" in release_block:
    raise SystemExit(
        "V26 publish-token isolation preflight failed: write-capable release job must not run on a self-hosted runner."
    )

print("PASS V26 publish token isolation")
