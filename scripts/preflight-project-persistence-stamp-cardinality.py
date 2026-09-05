#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STAMP = ROOT / "src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs"
CARDINALITY = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStructuralCardinality.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectPersistenceStampCardinalitySmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing persistence stamp cardinality file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


stamp = read(STAMP)
cardinality = read(CARDINALITY)
smoke = read(SMOKE)

for token in (
    "internal const int MaxTopLevelEntries = 100000;",
    "internal const int MaxNestedEntries = 10000;",
):
    if token not in cardinality:
        errors.append("canonical QSDB cardinality contract missing: " + token)

for token in (
    "ValidTopLevelCardinalityAboveLegacyStampLimitIsAccepted",
    "NestedCardinalityAboveQsdbLimitRemainsRejected",
    "AboveLegacyTopLevelLimit = 10001",
    "AboveNestedLimit = 10001",
    "new ProjectPersistenceStamp(project)",
):
    if token not in smoke:
        errors.append("persistence stamp cardinality regression missing: " + token)

for token in (
    "MaximumTopLevelSnapshotEntries = QsdbProjectStructuralCardinality.MaxTopLevelEntries",
    "MaximumNestedSnapshotEntries = QsdbProjectStructuralCardinality.MaxNestedEntries",
    "SnapshotTopLevelBounded(project.Zones, project.Zones.Count, \"project zones\")",
    "SnapshotTopLevelBounded(project.Floors, project.Floors.Count, \"project floors\")",
    "SnapshotTopLevelBounded(project.Families, project.Families.Count, \"project families\")",
    "SnapshotTopLevelBounded(project.QuantityRules, project.QuantityRules.Count, \"project quantity rules\")",
    "SnapshotTopLevelBounded(project.Elements, project.Elements.Count, \"project elements\")",
    "SnapshotTopLevelBounded(project.AuditEvents, project.AuditEvents.Count, \"project audit events\")",
    "SnapshotNestedBounded(metadata, metadata.Count, \"project metadata\")",
    "SnapshotNestedBounded(values, values.Count, collectionLabel)",
):
    if token not in stamp:
        errors.append("persistence stamp does not consume canonical cardinality contract: " + token)

for legacy in (
    "private const int MaximumSnapshotEntries = 10_000;",
    "SnapshotBounded(project.Zones, project.Zones.Count, \"project zones\")",
    "SnapshotBounded(project.Floors, project.Floors.Count, \"project floors\")",
    "SnapshotBounded(project.Families, project.Families.Count, \"project families\")",
    "SnapshotBounded(project.QuantityRules, project.QuantityRules.Count, \"project quantity rules\")",
    "SnapshotBounded(project.Elements, project.Elements.Count, \"project elements\")",
    "SnapshotBounded(project.AuditEvents, project.AuditEvents.Count, \"project audit events\")",
):
    if legacy in stamp:
        errors.append("persistence stamp retains legacy ambiguous cardinality path: " + legacy)

for invariant in (
    "RequireStableCountEvidence",
    "ThrowKnownCountMismatch",
    "known count does not match enumerated entry count",
    "count changed or conflicted",
    "exposes conflicting count evidence",
):
    if invariant not in stamp:
        errors.append("hostile Count/enumerator fail-closed invariant weakened: " + invariant)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectPersistenceStamp uses canonical QSDB top-level and nested cardinality limits without weakening hostile Count/enumerator validation.")
