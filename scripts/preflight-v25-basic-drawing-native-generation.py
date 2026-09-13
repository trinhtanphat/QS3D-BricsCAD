#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BasicDrawingCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []


def require(pattern: str, message: str) -> None:
    if re.search(pattern, text, flags=re.MULTILINE | re.DOTALL) is None:
        errors.append(message)


# Basic draw commands host-pump through point/distance prompts. Managed Document
# identity alone is insufficient because BricsCAD may keep the wrapper while the
# native Database generation is replaced/reloaded. Capture that native identity
# in the immutable command context and reject a successor generation before any
# CAD mutation begins.
require(
    r"BasicDrawingContext\s*\(.*?IntPtr\s+nativeDatabaseIdentity",
    "BasicDrawingContext must capture the native database generation identity",
)
require(
    r"NativeDatabaseIdentity\s*=\s*nativeDatabaseIdentity",
    "BasicDrawingContext must retain the captured native database identity",
)
require(
    r"CaptureContext\s*\(.*?document\.Database\.UnmanagedObject",
    "CaptureContext must snapshot document.Database.UnmanagedObject",
)
require(
    r"RequireFreshContext\s*\(.*?IsCurrentDocumentGeneration\(\s*document\s*,\s*expected\.NativeDatabaseIdentity\s*\)",
    "RequireFreshContext must reject same-wrapper native database replacement",
)

# Revalidate again inside the mutation owner immediately after acquiring the
# document lock and before starting/opening a transaction. This closes the gap
# between the post-prompt freshness check and native mutation ownership.
require(
    r"AppendEntity\s*\(.*?using\s*\(document\.LockDocument\(\)\).*?IsCurrentDocumentGeneration\(\s*document\s*,\s*context\.NativeDatabaseIdentity\s*\).*?StartTransaction\(\)",
    "AppendEntity must fence native generation after document lock and before transaction start",
)

# Once Commit() succeeds, UI sync must never turn a successful CAD commit into a
# stale-generation failure or apply implied selection/regen/status to a successor
# DB. Finalization therefore needs a generation fence before all UI publication.
require(
    r"FinalizeSuccess\s*\(.*?IsCurrentDocumentGeneration\(\s*document\s*,\s*context\.NativeDatabaseIdentity\s*\).*?SetImpliedSelection.*?Regen",
    "FinalizeSuccess must suppress selection/regen when committed generation is no longer current",
)
require(
    r"TrySetPaletteStatus\s*\(\s*Document\s+document\s*,\s*IntPtr\s+nativeDatabaseIdentity\s*,\s*string\s+message\s*\).*?IsCurrentDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity\s*\)",
    "palette publication must be bound to the exact committed native database generation",
)

if errors:
    print("ERROR: V25 basic drawing native-generation preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("V25 basic drawing native-generation preflight passed.")
