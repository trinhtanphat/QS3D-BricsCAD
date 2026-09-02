#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeExportMaterializationBoundSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/project-interchange-export-materialization-bound.md"

for path in (SOURCE, SMOKE, REGISTRATION, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Project-interchange materialization-bound preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "private sealed class BoundedUtf8StringBuilder",
    "new UTF8Encoding(false, true)",
    "StrictUtf8.GetByteCount(text)",
    "if (_utf8Bytes > _maxBytes - additionalBytes)",
    "ProjectInterchangeJsonValidator.MaxFileBytes",
    "var snapshot = json.ToString();",
    "RequireCanonicalSnapshot(snapshot);",
):
    if token not in source:
        raise SystemExit("Project-interchange exporter missing bounded serialization contract: " + token)

builder_creation = source.index("new BoundedUtf8StringBuilder(32768, ProjectInterchangeJsonValidator.MaxFileBytes)")
final_materialization = source.index("var snapshot = json.ToString();")
canonical_validation = source.index("RequireCanonicalSnapshot(snapshot);")
if not builder_creation < final_materialization < canonical_validation:
    raise SystemExit("Project-interchange exporter must enforce the byte bound while building, before final materialization and canonical validation.")

for token in (
    "OversizedAggregateFailsDuringBoundedSerialization();",
    "OrdinarySnapshotRemainsDeterministicAndValid();",
    "new string('x', 32768)",
    "for (var i = 0; i < 600; i++)",
    "semantic snapshot limit",
    "ProjectInterchangeJsonValidator.Validate(first)",
):
    if token not in smoke:
        raise SystemExit("Project-interchange materialization-bound smoke missing contract: " + token)

if "ProjectInterchangeExportMaterializationBoundSmoke.Run();" not in registration:
    raise SystemExit("Project-interchange materialization-bound smoke is not registered in the deterministic suite.")

for phrase in (
    "Lane-Key: `issue-5271`",
    "16 MiB",
    "before final `ToString()` materialization",
    "UTF-8 byte",
    "deterministic Core smoke",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Project-interchange materialization-bound runbook missing boundary: " + phrase)

print("PASS project interchange export materialization bound contract")
