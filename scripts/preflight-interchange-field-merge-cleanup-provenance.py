#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeFieldMergeImporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeFieldMergeImporterSmoke.cs"
DOC = ROOT / "docs" / "INTERCHANGE-FIELD-PRECEDENCE.md"
errors = []

for path in (IMPORTER, SMOKE, DOC):
    if not path.is_file():
        errors.append("missing field-merge cleanup provenance contract file: " + str(path.relative_to(ROOT)))

if not errors:
    importer = IMPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    required = (
        "public int NativeCleanupHandlesRequired { get; }",
        "NativeCleanupHandlesRequired = plan.TargetGeneratedHandlesToClean;",
        "Use NativeCleanupHandlesRequired. Core authorizes/requires native cleanup but does not erase BricsCAD entities.",
        "public int TargetGeneratedHandlesCleaned => NativeCleanupHandlesRequired;",
        'LastNativeCleanupHandlesRequiredKey = "Interchange.LastImport.NativeCleanupHandlesRequired"',
        "Use LastNativeCleanupHandlesRequiredKey. Core does not prove native CAD cleanup.",
        "target.Metadata[LastNativeCleanupHandlesRequiredKey] = plan.TargetGeneratedHandlesToClean.ToString(CultureInfo.InvariantCulture);",
        'target.Metadata.Remove("Interchange.LastImport.TargetGeneratedHandlesCleaned")',
        "nativeCleanupHandlesRequired=",
    )
    for token in required:
        if token not in importer:
            errors.append("field-merge cleanup provenance missing token: " + token)

    forbidden = (
        "TargetGeneratedHandlesCleaned = plan.TargetGeneratedHandlesToClean;",
        "target.Metadata[LastTargetGeneratedHandlesCleanedKey] =",
        'target.Metadata["Interchange.LastImport.TargetGeneratedHandlesCleaned"] =',
    )
    for token in forbidden:
        if token in importer:
            errors.append("Core field merge still claims native cleanup completion: " + token)

    for token in (
        "CleanupReportingUsesRequiredSemantics",
        "result.NativeCleanupHandlesRequired",
        "LastNativeCleanupHandlesRequiredKey",
        "!element.Properties.ContainsKey(\"GeneratedSolidHandle\")",
    ):
        if token not in smoke:
            errors.append("field-merge cleanup provenance smoke missing token: " + token)

    for token in (
        "Creating an authorization does **not** erase native entities.",
        "This does not claim that Core erased or rebuilt native BricsCAD entities.",
    ):
        if token not in doc:
            errors.append("field-merge documentation must retain native cleanup boundary: " + token)

print("QS3D interchange field-merge cleanup provenance preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Core field merge reports native cleanup as required/authorized work, preserves deprecated compatibility aliases, writes only requirement metadata, and does not claim BricsCAD entities were erased.")
