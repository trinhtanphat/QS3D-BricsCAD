#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonExporter.cs"
VALIDATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonValidator.cs"
READER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeValidatedSnapshotReader.cs"
USE_SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeUseSourceSemanticImporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeNamespacedOwnershipBoundarySmoke.cs"

errors = []
for path in (EXPORTER, VALIDATOR, READER, USE_SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing interchange ownership contract file: " + str(path.relative_to(ROOT)))

if not errors:
    exporter = EXPORTER.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")
    reader = READER.read_text(encoding="utf-8")
    use_source = USE_SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    namespace_guard = 'StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)'
    for label, text in (("exporter", exporter), ("validator", validator), ("typed reader", reader), ("UseSource cleanup", use_source)):
        if namespace_guard not in text:
            errors.append(label + " must reject/strip QS3D.PhysicalOpeningCut* generated/native ownership metadata")

    if "GeneratedHandleOwnershipPolicy.IsOwnerSlot" not in exporter:
        errors.append("exporter must keep canonical generated owner-slot filtering")
    if "GeneratedHandleOwnershipPolicy.IsOwnerSlot" not in validator:
        errors.append("validator must keep canonical generated owner-slot rejection")
    if "GeneratedHandleOwnershipPolicy.IsOwnerSlot" not in reader:
        errors.append("typed reader must keep canonical generated owner-slot rejection")

    smoke_required = (
        "ExporterOmitsNamespacedOpeningOwnership",
        "ValidatorAndTypedReaderRejectElementSmuggling",
        "ValidatorAndTypedReaderRejectFamilySmuggling",
        "GENERATED_RUNTIME_PROPERTY",
        "ProjectInterchangeValidatedSnapshotReader.Read",
    )
    for token in smoke_required:
        if token not in smoke:
            errors.append("namespaced ownership smoke missing regression: " + token)

if errors:
    print("QS3D interchange namespaced ownership preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic export, validation, typed reading and UseSource cleanup consistently exclude QS3D.PhysicalOpeningCut* and canonical generated owner slots.")
