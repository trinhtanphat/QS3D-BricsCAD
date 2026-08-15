#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapUnicodeIntegritySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing target-map Unicode integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);",
        "Convert.ToBase64String(StrictUtf8.GetBytes(canonical))",
        "Convert.ToBase64String(StrictUtf8.GetBytes(x ?? string.Empty))",
        "StrictUtf8.GetString(Convert.FromBase64String(parts[i]))",
        "var sourcePrefix = MetadataPrefix + Token(sourceId);",
        "var records = new Dictionary<string, string>",
        "var rollback = ProjectStateSnapshot.Capture(target);",
        "target.Metadata.Remove(key);",
        "target.Metadata[pair.Key] = pair.Value;",
        'AuditTrail.ForProject(target).Record(',
        "target.Touch();",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectInterchangeProvenanceTargetMap missing Unicode/mutation contract: " + token)

    if "Encoding.UTF8.GetBytes" in text:
        errors.append("Target-map writer must not use replacement-fallback UTF-8.")
    if text.count("StrictUtf8.GetBytes(") != 2:
        errors.append("Target-map writer must use strict UTF-8 for exactly token and record encoding.")

    source_prefix = text.find("var sourcePrefix = MetadataPrefix + Token(sourceId);")
    records = text.find("var records = new Dictionary<string, string>", source_prefix)
    element_records = text.find("records[sourcePrefix + ElementRecordSegment + Token(pair.Key)]", records)
    rollback = text.find("var rollback = ProjectStateSnapshot.Capture(target);", element_records)
    metadata_remove = text.find("target.Metadata.Remove(key);", rollback)
    metadata_write = text.find("target.Metadata[pair.Key] = pair.Value;", metadata_remove)
    audit = text.find("AuditTrail.ForProject(target).Record(", metadata_write)
    touch = text.find("target.Touch();", audit)
    positions = (source_prefix, records, element_records, rollback, metadata_remove, metadata_write, audit, touch)
    if min(positions) < 0 or list(positions) != sorted(positions):
        errors.append("Target-map strict record construction must precede rollback capture and all metadata/audit/revision mutation.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "[ModuleInitializer]",
        "MalformedWriterInputsFailBeforeMutation",
        '"source-high-\\uD800"',
        '"drawing-low-\\uDC00"',
        '"source-low-\\uDC00"',
        "Throws<EncoderFallbackException>",
        "target.Metadata.Count != beforeMetadata.Count",
        "target.AuditEvents.Count != beforeAuditCount",
        "target.ChangeVersion != beforeChangeVersion",
        "SupplementaryUnicodeRoundTripsExactly",
        'const string sourceProjectId = "source-rocket-\\uD83D\\uDE80";',
        'const string sourceFingerprint = "drawing-rocket-\\uD83D\\uDE80";',
        'const string sourceElementId = "source-element-rocket-\\uD83D\\uDE80";',
        'const string targetElementId = "target-element-rocket-\\uD83D\\uDE80";',
        "ProjectInterchangeProvenanceTargetMap.Store(",
        "ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(",
        "StrictUtf8.GetString(Convert.FromBase64String(x))",
    )
    for token in required:
        if token not in text:
            errors.append("Target-map Unicode smoke missing regression contract: " + token)

if errors:
    print("QS3D interchange provenance target-map Unicode integrity preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: provenance target-map writer rejects malformed UTF-16 before metadata/audit/revision mutation and preserves valid supplementary Unicode through strict token/record storage and readback.")
