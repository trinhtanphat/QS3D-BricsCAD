#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticScheduleCatalog.cs"
METADATA = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleSaveBoundedEnumerationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleSaveBoundedEnumerationSmokeRegistration.cs"


def metadata_revision_owned(metadata):
    setter = metadata.find('public string this[string key] { get => _items[key]; set => SetPublic(key, value, false); }')
    set_public = metadata.find('Set(canonicalKey, xmlValue, addOnly, true);')
    remove_public = metadata.find('public bool Remove(string key) => Remove(key, true);')
    remove_private = metadata.find('private bool Remove(string key, bool touchMutation)')
    remove_touch = metadata.find('if (touchMutation) TouchProject();', remove_private)
    remove_storage = metadata.find('return _items.Remove(key);', remove_private)
    set_private = metadata.find('private void Set(string key, string value, bool addOnly, bool touchMutation)')
    set_touch = metadata.find('if (touchMutation) TouchProject();', set_private)
    set_storage = metadata.find('if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;', set_private)
    touch_owner = metadata.find('private void TouchProject()')
    project_touch = metadata.find('project.Touch();', touch_owner)
    return (
        min(setter, set_public, remove_public, remove_private, remove_touch, remove_storage, set_private, set_touch, set_storage, touch_owner, project_touch) >= 0
        and remove_private < remove_touch < remove_storage
        and set_private < set_touch < set_storage
        and touch_owner < project_touch
    )


def main():
    source = SOURCE.read_text(encoding="utf-8")
    metadata = METADATA.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    start = source.find("public static void Save(ProjectState project, IEnumerable<SemanticScheduleDefinition> definitions)")
    end = source.find("public static void Upsert(", start)
    if start < 0 or end < 0:
        print("ERROR: SemanticScheduleCatalog.Save method boundary not found.")
        return 1
    save = source[start:end]

    required = [
        "var list = new List<SemanticScheduleDefinition>(MaxSchedules);",
        "foreach (var definition in definitions)",
        "if (list.Count >= MaxSchedules)",
        'throw new InvalidOperationException("Semantic schedule catalog exceeds the supported 128 definitions.");',
        "list.Add(definition);",
        "ValidateCatalog(list);",
        "project.Metadata.Remove(MetadataKey);",
        "project.Metadata[MetadataKey] = payload;",
    ]
    for token in required:
        if token not in save:
            print("ERROR: missing semantic schedule save bound contract: " + token)
            return 1

    if not metadata_revision_owned(metadata):
        print("ERROR: ProjectMetadataDictionary must own exact-once project revision updates for public Remove/indexer persistence mutations.")
        return 1

    legacy = [
        "definitions.ToList()",
        "if (list.Count > MaxSchedules)",
    ]
    for token in legacy:
        if token in save:
            print("ERROR: legacy post-materialization semantic schedule capacity path returned: " + token)
            return 1

    loop = save.find("foreach (var definition in definitions)")
    cap = save.find("if (list.Count >= MaxSchedules)", loop)
    add = save.find("list.Add(definition);", loop)
    validate = save.find("ValidateCatalog(list);")
    remove = save.find("project.Metadata.Remove(MetadataKey);")
    assign = save.find("project.Metadata[MetadataKey] = payload;")
    mutations = [position for position in (remove, assign) if position >= 0]
    first_mutation = min(mutations) if mutations else -1
    if min(loop, cap, add, validate, first_mutation) < 0 or not (loop < cap < add < validate < first_mutation):
        print("ERROR: Semantic schedule save capacity guard must run during enumeration before validation or metadata persistence mutation.")
        return 1

    smoke_tokens = [
        "OversizeLazyCatalogStopsAtFirstItemBeyondCapacity();",
        "SemanticScheduleCatalog.Save(project, source.Values());",
        'Equal("Semantic schedule catalog exceeds the supported 128 definitions.", ex.Message);',
        "Equal(129, source.YieldCount);",
        "if (YieldCount > 129)",
        "Semantic schedule save enumerated beyond the first item over capacity.",
        "Equal(beforeVersion, project.ChangeVersion);",
        "project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey)",
    ]
    for token in smoke_tokens:
        if token not in smoke:
            print("ERROR: missing semantic schedule save bound smoke token: " + token)
            return 1

    if "[ModuleInitializer]" not in registration or "SemanticScheduleSaveBoundedEnumerationSmoke.Run();" not in registration:
        print("ERROR: semantic schedule save bound smoke is not module-registered.")
        return 1

    print("PASS: SemanticScheduleCatalog.Save bounds lazy definition enumeration before metadata persistence, whose dictionary owner performs exact-once project revision mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
