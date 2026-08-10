#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "docs/COMMANDS.md": [
        "`QS3DCUTSELECTEDOPENINGS`",
        "### Planar UCS contract",
        "P0, guarded P1 and Door/Opening Direct Draw share the current source-level UCS contract",
        "Family / Type",
    ],
    "docs/IMPLEMENTATION-STATUS.md": [
        "`QS3DCUTSELECTEDOPENINGS` is source-implemented",
        "P1 now shares the P0 planar-UCS contract",
        "Door/Opening uses the same planar-UCS source contract as P0/P1",
        "scripts/preflight-direct-draw-ucs-extended.py",
        "scripts/preflight-opening-cut-readiness.py",
    ],
    "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md": [
        "## 3. Planar UCS source contract",
        "`QS3DCUTSELECTEDOPENINGS`",
        "translated UCS and in-plane rotated UCS",
    ],
    "docs/CONTINUE-ALL-2026-08-10.md": [
        "### Targeted Door/Opening physical cut",
        "### Planar current-UCS support",
        "scripts/preflight-direct-draw-ucs-extended.py",
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing current-status document: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing current source-status marker: " + needle)

stale_tokens = {
    "docs/COMMANDS.md": [
        "then run `QS3DCUTOPENINGS` intentionally when host Solid3d mutation is wanted",
    ],
    "docs/IMPLEMENTATION-STATUS.md": [
        "until a safe explicit-target subset transaction/fingerprint contract exists",
    ],
}
for relative, needles in stale_tokens.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle in text:
            errors.append(relative + " still contains stale source-status text: " + needle)

print("QS3D current source-status sync preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: command/status/handoff/continue-all docs reflect targeted selected opening cuts and full current planar-UCS Direct Draw source support without stale pre-feature wording.")
