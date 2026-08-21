#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import stat
import tempfile
from pathlib import Path
from types import SimpleNamespace

SCRIPT = Path(__file__).resolve().with_name("preflight-repository-professionalism.py")
spec = importlib.util.spec_from_file_location("qs3d_professionalism_preflight", SCRIPT)
if spec is None or spec.loader is None:
    raise RuntimeError("Cannot load repository professionalism preflight module.")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="qs3d-professionalism-input-") as temp:
        root = Path(temp) / "repo"
        root.mkdir()

        safe = root / "safe.yml"
        safe.write_text("name: safe\n", encoding="utf-8")
        text, error = module.read_repository_text(safe, root, 64)
        require(error is None and text == "name: safe\n", f"safe UTF-8 file rejected: {error}")

        crlf = root / "crlf.yml"
        crlf.write_bytes(b"permissions:\r\n  contents: read\r\n")
        text, error = module.read_repository_text(crlf, root, 64)
        require(
            error is None and text == "permissions:\n  contents: read\n",
            f"CRLF repository text did not preserve universal-newline semantics: text={text!r}, error={error}",
        )

        exact = root / "exact.txt"
        exact.write_bytes(b"x" * 16)
        text, error = module.read_repository_text(exact, root, 16)
        require(error is None and text == "x" * 16, f"exact-size file rejected: {error}")

        oversized = root / "oversized.txt"
        oversized.write_bytes(b"x" * 17)
        text, error = module.read_repository_text(oversized, root, 16)
        require(text is None and error is not None and "exceeds 16 byte" in error, "oversized source was not rejected")

        invalid_utf8 = root / "invalid.txt"
        invalid_utf8.write_bytes(b"valid-prefix\xff")
        text, error = module.read_repository_text(invalid_utf8, root, 64)
        require(text is None and error is not None and "valid UTF-8" in error, "invalid UTF-8 source was not rejected")

        directory = root / "directory.yml"
        directory.mkdir()
        text, error = module.read_repository_text(directory, root, 64)
        require(text is None and error is not None and "regular file" in error, "directory candidate was not rejected by strict reader")

        outside = Path(temp) / "outside.yml"
        outside.write_text("name: outside\n", encoding="utf-8")
        text, error = module.read_repository_text(outside, root, 64)
        require(text is None and error is not None and "escapes repository root" in error, "outside-root file was not rejected")

        symlink = root / "linked.yml"
        try:
            symlink.symlink_to(outside)
        except (OSError, NotImplementedError):
            symlink = None
        if symlink is not None:
            text, error = module.read_repository_text(symlink, root, 64)
            require(text is None and error is not None and "symlink" in error, "symlink candidate was not rejected")

        fake_reparse = SimpleNamespace(
            st_mode=stat.S_IFREG | 0o644,
            st_file_attributes=module.WINDOWS_REPARSE_POINT_ATTRIBUTE,
        )
        require(
            module._metadata_type_error(fake_reparse) == "must not be a Windows reparse point",
            "Windows reparse metadata was not rejected",
        )

        fake_regular = SimpleNamespace(st_mode=stat.S_IFREG | 0o644, st_file_attributes=0)
        require(module._metadata_type_error(fake_regular) is None, "regular metadata was rejected")

        replacement = root / "replacement.txt"
        replacement.write_text("stable after replacement\n", encoding="utf-8")
        original_same_opened_file = module._same_opened_file
        transient_calls = 0

        def transient_identity(before, opened):
            nonlocal transient_calls
            transient_calls += 1
            if transient_calls == 1:
                return False
            return original_same_opened_file(before, opened)

        try:
            module._same_opened_file = transient_identity
            text, error = module.read_repository_text(replacement, root, 64)
        finally:
            module._same_opened_file = original_same_opened_file
        require(
            error is None and text == "stable after replacement\n" and transient_calls == 2,
            f"single transient identity replacement did not recover with one bounded retry: calls={transient_calls}, error={error}",
        )

        persistent_calls = 0

        def persistent_identity(before, opened):
            nonlocal persistent_calls
            persistent_calls += 1
            return False

        try:
            module._same_opened_file = persistent_identity
            text, error = module.read_repository_text(replacement, root, 64)
        finally:
            module._same_opened_file = original_same_opened_file
        require(
            text is None
            and error is not None
            and "bounded retry" in error
            and persistent_calls == module.MAX_OPEN_IDENTITY_ATTEMPTS,
            f"persistently moving identity did not fail closed after bounded retry: calls={persistent_calls}, error={error}",
        )

        scan_root = root / "scan"
        scan_root.mkdir()
        (scan_root / "ordinary.md").mkdir()
        (scan_root / "safe.txt").write_text("ordinary repository text\n", encoding="utf-8")
        scan_outside = root / "scan-outside.txt"
        scan_outside.write_text("outside target\n", encoding="utf-8")
        scan_link = scan_root / "unsafe.yml"
        try:
            scan_link.symlink_to(scan_outside)
        except (OSError, NotImplementedError):
            scan_link = None

        original_root = module.ROOT
        try:
            module.ROOT = scan_root
            failures: list[str] = []
            module.reject_external_orchestration_artifacts(failures)
        finally:
            module.ROOT = original_root

        require(
            not any("ordinary.md" in failure for failure in failures),
            f"ordinary suffix-bearing directory was misclassified as a file: {failures}",
        )
        if scan_link is not None:
            require(
                any("unsafe.yml" in failure and "symlink" in failure for failure in failures),
                f"symlink scan candidate did not fail closed: {failures}",
            )

    print("PASS: repository professionalism input-safety regression")
    print(" - bounded UTF-8 reads accept the exact boundary and reject one-over inputs")
    print(" - CRLF repository text preserves platform-independent universal-newline semantics")
    print(" - invalid UTF-8, non-regular, outside-root, symlink and reparse candidates fail closed")
    print(" - one transient file replacement is retried once while persistent identity churn remains fail-closed")
    print(" - orchestration scan skips ordinary suffix-bearing directories while retaining unsafe-link refusal")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
