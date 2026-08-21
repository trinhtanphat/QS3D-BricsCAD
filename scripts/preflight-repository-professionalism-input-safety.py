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
        require(text is None and error is not None and "regular file" in error, "directory candidate was not rejected")

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

    print("PASS: repository professionalism input-safety regression")
    print(" - bounded UTF-8 reads accept the exact boundary and reject one-over inputs")
    print(" - invalid UTF-8, non-regular, outside-root, symlink and reparse candidates fail closed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
