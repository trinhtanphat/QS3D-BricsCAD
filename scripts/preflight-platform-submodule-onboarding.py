#!/usr/bin/env python3
"""Keep the contributor checkout instructions compatible with Core dependencies."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
README = ROOT / "README.md"


def main() -> int:
    if not README.is_file():
        raise AssertionError("missing contributor onboarding document: README.md")

    text = README.read_text(encoding="utf-8")
    required = (
        "git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git",
        "QS3D.Core` references the pinned `external/QS3D-Platform` submodule",
        "git submodule sync --recursive",
        "git submodule update --init --recursive",
    )
    for token in required:
        if token not in text:
            raise AssertionError("missing Platform submodule onboarding instruction: " + token)

    clone_at = text.index(required[0])
    update_at = text.index(required[3])
    core_build_at = text.index("dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release")
    if not clone_at < update_at < core_build_at:
        raise AssertionError("Platform submodule initialization must precede the Core build instructions")

    print("PASS: contributor onboarding initializes the pinned Platform submodule before Core validation.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
