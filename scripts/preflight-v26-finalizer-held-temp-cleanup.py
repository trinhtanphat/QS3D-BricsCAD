#!/usr/bin/env python3
"""Fail closed unless V26 finalizer temp cleanup is exact-generation owned."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "finalize-v26-signed-package.ps1"


def fail(message: str) -> None:
    print(f"ERROR: V26 finalizer held-temp cleanup preflight failed closed: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    try:
        source = TARGET.read_text(encoding="utf-8")
    except Exception as exc:
        fail(f"cannot read {TARGET.relative_to(ROOT)}: {exc}")

    required = {
        "held generation identity capture": "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
        "native delete-handle open": "CreateFileW(",
        "delete access": "GENERIC_READ | DELETE",
        "write/delete sharing closed": "FILE_SHARE_READ",
        "same-handle identity proof": "GetFileInformationByHandle(handle",
        "same-handle delete disposition": "SetFileInformationByHandle(handle",
        "exact-generation cleanup call": "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity",
        "held stream remains read-locked through execution": "[IO.FileShare]::Read",
    }
    for label, token in required.items():
        if token not in source:
            fail(f"missing {label}: {token}")

    for token in ("Remove-Item -LiteralPath $tempScript", "Remove-Item $tempScript"):
        if token in source:
            fail(f"pathname cleanup can delete a replacement generation after held-handle release: {token}")

    capture_at = source.find("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream")
    invoke_at = source.find("& $tempScript @forward")
    post_assert_at = source.find(
        "Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript",
        invoke_at + 1,
    )
    dispose_at = source.find("$generatedStream.Dispose()", post_assert_at + 1)
    cleanup_at = source.find(
        "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity",
        dispose_at + 1,
    )
    if min(capture_at, invoke_at, post_assert_at, dispose_at, cleanup_at) < 0:
        fail("could not prove identity-capture/execute/post-validate/dispose/exact-cleanup lifecycle")
    if not (capture_at < invoke_at < post_assert_at < dispose_at < cleanup_at):
        fail("generated-script identity must be captured while held and exact-generation cleanup must occur only after post-validation/close")

    print("PASS: V26 generated-finalizer cleanup proves exact generation before same-handle deletion.")


if __name__ == "__main__":
    main()
