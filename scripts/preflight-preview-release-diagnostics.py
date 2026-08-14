#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "preview release preparation": ROOT / "scripts/prepare-v25-cloud-release.ps1",
    "preview version synchronization": ROOT / "scripts/sync-preview-release-version.ps1",
}
SHAPE = "v<major>.<minor>.<patch>-preview.<n>"
CONCRETE_PRODUCT_TAG = re.compile(r"v0\.1\.0-preview\.\d+", re.IGNORECASE)
PREVIEW_REGEX_MARKER = "-preview\\.(?:0|[1-9][0-9]*)$"
errors = []

for label, path in FILES.items():
    try:
        text = path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)}: {exc}")
        continue

    if SHAPE not in text:
        errors.append(f"{label} must describe the accepted preview tag with the version-neutral shape {SHAPE}")
    concrete = sorted(set(CONCRETE_PRODUCT_TAG.findall(text)))
    if concrete:
        errors.append(
            f"{label} contains historical concrete preview tag diagnostic(s): " + ", ".join(concrete)
        )
    if PREVIEW_REGEX_MARKER not in text:
        errors.append(f"{label} no longer contains the bounded preview-number regex contract")
    if "Got: $ReleaseTag" not in text:
        errors.append(f"{label} must retain the rejected caller value in its validation diagnostic")

if errors:
    print("Preview release diagnostic preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print(
    "Preview release diagnostic preflight PASS: executable helpers retain the exact preview regex "
    "while using version-neutral operator diagnostics instead of already-published product tags."
)
