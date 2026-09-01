#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "QsWorkbookTemplateEngine.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsWorkbookTemplatePackageBoundSmoke.cs"
REGISTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"
errors = []

for path in (SOURCE, SMOKE, REGISTRY):
    if not path.is_file():
        errors.append("missing QS workbook template package-bound file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "using System.Xml;",
        "MaxTemplateWorkbookBytes = 128L * 1024L * 1024L",
        "MaxTemplateMetadataXmlCharacters = 4L * 1024L * 1024L",
        "MaxTemplateXmlCharacters = 64L * 1024L * 1024L",
        "new FileInfo(source).Length",
        "ValidateTemplatePackageLength",
        "DtdProcessing = DtdProcessing.Prohibit",
        "XmlResolver = null",
        "MaxCharactersInDocument = maxCharacters",
        "MaxCharactersFromEntities = 0",
        "entry.Length < 0 || entry.Length > maxCharacters",
        "XDocument.Load(reader, LoadOptions.PreserveWhitespace)",
    ):
        if token not in source:
            errors.append("QsWorkbookTemplateEngine.cs missing hardened template package token: " + token)
    forbidden = "using (var stream = entry.Open()) return XDocument.Load(stream, LoadOptions.PreserveWhitespace);"
    if forbidden in source:
        errors.append("QsWorkbookTemplateEngine.cs still performs raw unbounded XDocument.Load(entry.Open()).")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsCompressionAmplifiedWorkbookMetadata();",
        "CanonicalTemplateStillExports();",
        "OversizedMetadataPadding = 4 * 1024 * 1024",
        "compressed-metadata-bomb.xlsx",
        "new FileInfo(template).Length < 256 * 1024",
        "ExpectThrows<InvalidDataException>",
        "BytesEqual(before, File.ReadAllBytes(destination))",
    ):
        if token not in smoke:
            errors.append("QsWorkbookTemplatePackageBoundSmoke.cs missing hostile-template token: " + token)

if REGISTRY.is_file():
    registry = REGISTRY.read_text(encoding="utf-8")
    if "QsWorkbookTemplatePackageBoundSmoke.Run();" not in registry:
        errors.append("SmokeTestRegistration.cs does not register QsWorkbookTemplatePackageBoundSmoke.Run().")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS workbook template XML/package materialization is bounded before DOM hydration and covered by hostile compressed-template smoke.")
