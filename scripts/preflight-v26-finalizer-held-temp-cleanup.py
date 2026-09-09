#!/usr/bin/env python3
"""Fail closed unless V26 finalizer temp cleanup is bound to the held generation."""

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
        "delete-on-close option": "[IO.FileOptions]::DeleteOnClose",
        "held stream remains the cleanup owner": "$generatedStream.Dispose()",
        "exclusive delete/write sharing remains closed": "[IO.FileShare]::Read",
    }
    for label, token in required.items():
        if token not in source:
            fail(f"missing {label}: {token}")

    forbidden = (
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item $tempScript",
    )
    for token in forbidden:
        if token in source:
            fail(f"pathname cleanup can delete a replacement generation after held-handle release: {token}")

    dispose_at = source.find("$generatedStream.Dispose()")
    invoke_at = source.find("& $tempScript @forward")
    post_assert_at = source.find("Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript", invoke_at + 1)
    if min(dispose_at, invoke_at, post_assert_at) < 0:
        fail("could not prove execute/post-validate/dispose lifecycle ordering")
    if not (invoke_at < post_assert_at < dispose_at):
        fail("held generated-script generation must survive invocation and post-validation until cleanup dispose")

    print("PASS: V26 generated-finalizer cleanup is owned by the held temp generation.")


if __name__ == "__main__":
    main()
