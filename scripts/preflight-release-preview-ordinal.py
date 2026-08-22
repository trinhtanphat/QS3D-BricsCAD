#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

SURFACES = {
    ROOT / ".github/workflows/release-v25-cloud.yml": {
        "required": r"-preview\.[1-9][0-9]*$') {",
        "forbidden": r"-preview\.(?:0|[1-9][0-9]*)$') {",
    },
    ROOT / "scripts/prepare-v25-cloud-release.ps1": {
        "required": r"-preview\.[1-9][0-9]*$') {",
        "forbidden": r"-preview\.(?:0|[1-9][0-9]*)$') {",
    },
    ROOT / "scripts/sync-preview-release-version.ps1": {
        "required": r"-preview\.(?<preview>[1-9][0-9]*)$',",
        "forbidden": r"(?<preview>0|[1-9][0-9]*)",
    },
}

TAG_PATTERN = re.compile(
    r"^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)-preview\.[1-9][0-9]*$"
)

VALID_TAGS = (
    "v0.1.0-preview.1",
    "v1.0.0-preview.12",
    "v10.20.30-preview.65535",
)
INVALID_TAGS = (
    "v0.1.0-preview.0",
    "v0.1.0-preview.01",
    "v0.1.0-preview.",
    "v0.1.0-rc.1",
    "v01.1.0-preview.1",
)


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    for path, contract in SURFACES.items():
        if not path.is_file():
            return fail(f"missing release validation surface: {path.relative_to(ROOT)}")
        text = path.read_text(encoding="utf-8")
        if contract["required"] not in text:
            return fail(
                f"{path.relative_to(ROOT)} does not enforce a positive preview ordinal"
            )
        if contract["forbidden"] in text:
            return fail(
                f"{path.relative_to(ROOT)} still accepts preview ordinal zero"
            )

    for tag in VALID_TAGS:
        if TAG_PATTERN.fullmatch(tag) is None:
            return fail(f"positive preview tag should be accepted: {tag}")
    for tag in INVALID_TAGS:
        if TAG_PATTERN.fullmatch(tag) is not None:
            return fail(f"invalid preview tag should be rejected: {tag}")

    print("PASS: V25 preview release validators require prerelease ordinals >= 1.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
