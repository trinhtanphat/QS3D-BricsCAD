#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeSemanticReferencePolicy.cs"
VALIDATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeSemanticReferenceValidator.cs"
JSON_VALIDATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonValidator.cs"
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonExporter.cs"
READER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeValidatedSnapshotReader.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeSemanticReferenceValidationSmoke.cs"

errors = []
for path in (POLICY, VALIDATOR, JSON_VALIDATOR, EXPORTER, READER, SMOKE):
    if not path.is_file():
        errors.append("missing semantic-reference contract file: " + str(path.relative_to(ROOT)))

if not errors:
    policy = POLICY.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")
    json_validator = JSON_VALIDATOR.read_text(encoding="utf-8")
    exporter = EXPORTER.read_text(encoding="utf-8")
    reader = READER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token in ("HostWallIdKey", "BottomLevelIdKey", "TopLevelIdKey"):
        if token not in policy:
            errors.append("semantic reference registry missing canonical reference: " + token)

    for token in (
        "ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences",
        "ValidateLevelConsistency",
        "top level elevation must be above bottom level elevation",
        "InvalidDataException",
    ):
        if token not in validator:
            errors.append("central semantic reference validator missing contract: " + token)

    for token in (
        "ValidateSemanticPropertyReferences",
        "ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences",
        "SEMANTIC_PROPERTY_REF_MISSING",
        "LEVEL_ORDER",
        "TryLevelOffset",
    ):
        if token not in json_validator:
            errors.append("validate-only semantic reference diagnostics missing contract: " + token)

    if "ProjectInterchangeSemanticReferenceValidator.Validate(project);" not in exporter:
        errors.append("semantic exporter must validate registered property references before emitting a snapshot")
    if "ProjectInterchangeSemanticReferenceValidator.Validate(result);" not in reader:
        errors.append("typed snapshot reader must validate registered property references before returning import authority")

    for token in (
        "ExportRejectsMissingRegisteredReference",
        "ValidatorAndTypedReaderRejectMissingRegisteredReference",
        "ValidatorAndTypedReaderRejectInvalidLevelChain",
        "MixedFieldMergeRollsBackInvalidLevelComposition",
        "SEMANTIC_PROPERTY_REF_MISSING",
        "LEVEL_ORDER",
        "ProjectInterchangeFieldMergeImporter.Import",
    ):
        if token not in smoke:
            errors.append("semantic reference smoke missing regression: " + token)

if errors:
    print("QS3D interchange semantic-reference preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical HostWall/BottomLevel/TopLevel property references are aligned across validate-only diagnostics, export, typed import and mixed field-merge rollback.")
