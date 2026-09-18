#!/usr/bin/env python3
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SUBMODULE_PATH = "external/QS3D-Platform"
PLATFORM = ROOT / SUBMODULE_PATH
SMOKE = PLATFORM / "tests" / "QS3D.Platform.SmokeTests" / "QuantityScheduleCsvUnicodeFidelityModuleSmoke.cs"


def gitlink_sha() -> str:
    result = subprocess.run(
        ["git", "ls-tree", "HEAD", SUBMODULE_PATH],
        cwd=ROOT,
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


def ensure_exact_checkout(expected: str) -> None:
    result = subprocess.run(
        ["git", "submodule", "update", "--init", "--depth", "1", "--", SUBMODULE_PATH],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise SystemExit("ERROR: cannot initialize pinned QS3D-Platform for CSV Unicode validation: " + result.stderr.strip())
    head = subprocess.run(
        ["git", "-C", str(PLATFORM), "rev-parse", "HEAD"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    actual = head.stdout.strip().lower() if head.returncode == 0 else ""
    if actual != expected.lower():
        raise SystemExit(f"ERROR: initialized QS3D-Platform HEAD does not match gitlink: expected {expected}, got {actual or '<unresolved>'}")


pinned = gitlink_sha()
ensure_exact_checkout(pinned)
if not SMOKE.is_file():
    raise SystemExit(f"ERROR: pinned QS3D-Platform lacks strict CSV Unicode regression: {SMOKE.relative_to(ROOT)}")
source = SMOKE.read_text(encoding="utf-8")
for marker in ("RejectsMalformedHighSurrogate();", "RejectsMalformedLowSurrogate();", "PreservesSupplementaryUnicode();", "char.ConvertFromUtf32"):
    if marker not in source:
        raise SystemExit(f"ERROR: pinned QS3D-Platform CSV Unicode regression is missing required contract marker: {marker}")

print(f"PASS: pinned QS3D-Platform {pinned} contains the executable strict Quantity Schedule CSV Unicode fidelity regression")
