#!/usr/bin/env python3
import subprocess

EXPECTED_PLATFORM_SHA = "728aa5779e3b1b0a30dae641359d95688228f50f"
SUBMODULE_PATH = "external/QS3D-Platform"


def gitlink_sha() -> str:
    result = subprocess.run(
        ["git", "ls-tree", "HEAD", SUBMODULE_PATH],
        check=True,
        capture_output=True,
        text=True,
    )
    line = result.stdout.strip()
    if not line:
        raise SystemExit(f"ERROR: missing gitlink {SUBMODULE_PATH}")
    metadata, _, path = line.partition("\t")
    fields = metadata.split()
    if path != SUBMODULE_PATH or len(fields) != 3 or fields[0] != "160000" or fields[1] != "commit":
        raise SystemExit(f"ERROR: malformed Platform gitlink: {line}")
    return fields[2]


actual = gitlink_sha()
if actual != EXPECTED_PLATFORM_SHA:
    raise SystemExit(
        "ERROR: QS3D-Platform does not contain the qualified Quantity Schedule CSV output-budget generation: "
        f"expected {EXPECTED_PLATFORM_SHA}, got {actual}"
    )

print(f"PASS: QS3D-Platform Quantity Schedule CSV output-budget generation is pinned at {actual}")
