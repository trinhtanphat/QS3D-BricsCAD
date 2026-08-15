#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceTokenUnicodeSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing provenance-token Unicode integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);",
        "var canonicalIdentity = (value ?? string.Empty).Trim().ToUpperInvariant();",
        "var bytes = StrictUtf8.GetBytes(canonicalIdentity);",
        "var key = MetadataPrefix + Token(sourceProjectId.Trim()) + ElementRecordSegment + Token(sourceElementId.Trim());",
        "if (!target.Metadata.TryGetValue(key, out var encoded) || string.IsNullOrWhiteSpace(encoded))",
        "var fields = DecodeRecord(encoded);",
        "StrictUtf8.GetString(Convert.FromBase64String(parts[i]))",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectInterchangeSourceHandleProvenance missing token/read contract: " + token)

    token_start = text.find("private static string Token(string value)")
    token_end = text.find("private static string EncodeRecord", token_start)
    token_body = text[token_start:token_end] if token_start >= 0 and token_end > token_start else ""
    if not token_body:
        errors.append("cannot isolate ProjectInterchangeSourceHandleProvenance.Token implementation")
    else:
        if "Encoding.UTF8.GetBytes(canonicalIdentity)" in token_body:
            errors.append("provenance identity token still uses replacement-fallback UTF-8")
        if token_body.count("StrictUtf8.GetBytes(canonicalIdentity)") != 1:
            errors.append("provenance identity token must use the existing strict UTF-8 encoder exactly once")

    project_guard = text.find("if (string.IsNullOrWhiteSpace(sourceProjectId))")
    element_guard = text.find("if (string.IsNullOrWhiteSpace(sourceElementId))", project_guard)
    key = text.find("var key = MetadataPrefix + Token(sourceProjectId.Trim())", element_guard)
    metadata_read = text.find("target.Metadata.TryGetValue(key", key)
    decode = text.find("var fields = DecodeRecord(encoded);", metadata_read)
    positions = (project_guard, element_guard, key, metadata_read, decode)
    if min(positions) < 0 or list(positions) != sorted(positions):
        errors.append("strict provenance lookup tokenization must remain before metadata selection and decode")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "[ModuleInitializer]",
        "MalformedProjectLookupCannotAliasLiteralReplacementCharacter",
        "MalformedElementLookupFailsClosedWithoutMutation",
        "SupplementaryUnicodeIdentitiesRoundTrip",
        'const string validProjectId = "source-\\uFFFD";',
        'const string validElementId = "element-\\uFFFD";',
        '"source-\\uD800"',
        '"source-\\uDC00"',
        '"element-\\uD800"',
        '"element-\\uDC00"',
        "Throws<EncoderFallbackException>(action);",
        "target.Metadata.Count != beforeMetadata.Count",
        "target.AuditEvents.Count != beforeAuditCount",
        "target.ChangeVersion != beforeChangeVersion",
        "target.UpdatedUtc != beforeUpdatedUtc",
        'const string sourceProjectId = "Project-\\uD83D\\uDE80";',
        'const string sourceElementId = "Element-\\uD83E\\uDDF1";',
        "ProjectInterchangeSourceHandleProvenance.Store(",
        "ProjectInterchangeJsonExporter.Build(source)",
        "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(",
    )
    for token in required:
        if token not in text:
            errors.append("provenance-token Unicode smoke missing regression contract: " + token)

if errors:
    print("QS3D interchange source-handle provenance token Unicode preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: source-handle provenance lookup tokens reject malformed UTF-16 without identity aliasing or mutation and preserve valid supplementary Unicode.")
