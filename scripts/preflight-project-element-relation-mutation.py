#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ELEMENT = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
RELATIONS = ROOT / "src/QS3D.Core/Domain/ProjectElementRelationList.cs"
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
STORE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementRelationMutationSmoke.cs"
errors = []

for path in (ELEMENT, RELATIONS, SNAPSHOT, STORE, SMOKE):
    if not path.is_file():
        errors.append("missing relation mutation contract file: " + str(path.relative_to(ROOT)))

if not errors:
    element = ELEMENT.read_text(encoding="utf-8")
    relations = RELATIONS.read_text(encoding="utf-8")
    snapshot = SNAPSHOT.read_text(encoding="utf-8")
    store = STORE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for raw in ("SourceHandles = new List<string>();", "DependsOn = new List<string>();"):
        if raw in element:
            errors.append("ProjectElement still exposes an unowned raw relation list: " + raw)

    for token in (
        "ProjectElementRelationList",
        "SourceHandles = new ProjectElementRelationList",
        "DependsOn = new ProjectElementRelationList",
        "ElementDirtyFlags.Relations",
    ):
        if token not in element:
            errors.append("ProjectElement missing relation lifecycle token: " + token)

    for token in (
        "IList<string>",
        "AddPersistenceValue",
        "ClearPersistenceValues",
        "RequireRelationValue",
        "value.Trim()",
        "StringComparison.OrdinalIgnoreCase",
    ):
        if token not in relations:
            errors.append("ProjectElementRelationList missing contract token: " + token)

    if "target.SourceHandles.Clear();" in snapshot or "target.DependsOn.Clear();" in snapshot:
        errors.append("snapshot restore must use explicit persistence relation bypass instead of public mutation")
    if "element.SourceHandles.Add(handle.Value.Trim())" in store or "element.DependsOn.Add(dep.Value.Trim())" in store:
        errors.append("QSDB hydration must use explicit persistence relation bypass instead of public mutation")

    for token in (
        "SourceHandleAddMarksRelationsDirty",
        "DependencyAddMarksRelationsDirty",
        "NoOpRemovalPreservesCleanState",
        "RelationInputsNormalizeAndValidate",
        'source.SourceHandles.Add(" padded ")',
        'Equal("padded", source.SourceHandles[0])',
        "[ModuleInitializer]",
    ):
        if token not in smoke:
            errors.append("relation mutation smoke missing regression token: " + token)

print("QS3D ProjectElement relation mutation lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ProjectElement relation collections preserve IList compatibility while routing semantic mutation through Relations dirty state, normalizing canonical inputs, and reserving explicit persistence hydration paths.")
