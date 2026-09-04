#!/usr/bin/env python3
"""Regression guard: Platform gitlink changes must require managed build validation."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "ci-validation-scope.py"


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_ci_validation_scope", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"could not load {TARGET}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    module = load_target()

    platform_source, platform_build = module.classify_path("external/QS3D-Platform")
    require(platform_source, "Platform gitlink change must require source validation")
    require(platform_build, "Platform gitlink change must require managed build validation")

    aggregate_source, aggregate_build = module.classify_paths(
        ["docs/non-build-note.md", "external/QS3D-Platform"]
    )
    require(aggregate_source, "Platform gitlink must keep aggregate source validation enabled")
    require(aggregate_build, "Platform gitlink must keep aggregate build validation enabled")

    for lookalike in (
        "external/README.md",
        "external/QS3D-Platform-copy",
        "external/QS3D-Platform/README.md",
    ):
        unrelated_source, unrelated_build = module.classify_path(lookalike)
        require(
            (unrelated_source, unrelated_build) == (False, False),
            f"Do not broaden non-gitlink external path into validation: {lookalike}",
        )

    print("PASS ci validation scope Platform gitlink build admission")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
