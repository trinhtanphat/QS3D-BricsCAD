#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/Geometry/RoomBoundaryEngine.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/RoomBoundarySourceProvenanceSmoke.cs"
runbook = root / "docs/FEATURE-RUNBOOKS/room-boundary-source-provenance.md"
errors = []

for path in (source, smoke, runbook):
    if not path.is_file():
        errors.append("missing room-boundary provenance contract file: " + str(path.relative_to(root)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    required = (
        "SourceId = NormalizeSourceId(sourceId);",
        "private static string NormalizeSourceId(string? sourceId)",
        "char.IsControl(character)",
        "char.IsHighSurrogate(character)",
        "char.IsLowSurrogate(character)",
        "Room boundary source provenance must not contain control characters.",
        "Room boundary source provenance must contain well-formed UTF-16.",
    )
    for token in required:
        if token not in text:
            errors.append("RoomBoundaryEngine.cs missing source-provenance token: " + token)
    if "SourceId = sourceId?.Trim() ?? string.Empty;" in text:
        errors.append("BoundarySegment must not regress to trim-only source provenance admission")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "MalformedUtf16FailsAtSegmentAdmission();",
        "ControlCharactersFailAtSegmentAdmission();",
        "CanonicalOptionalProvenanceRemainsSupported();",
        "DirectDiscoveryRetainsCanonicalProvenance();",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("RoomBoundarySourceProvenanceSmoke.cs missing regression token: " + token)

if runbook.is_file():
    text = runbook.read_text(encoding="utf-8")
    for token in ("issue-5162", "NOT_APPLICABLE", "BoundarySegment", "well-formed UTF-16", "control"):
        if token not in text:
            errors.append("room-boundary provenance runbook missing token: " + token)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS room boundary source provenance contract")
