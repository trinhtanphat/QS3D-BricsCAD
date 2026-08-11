#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticScheduleCatalog.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleSaveBoundedEnumerationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleSaveBoundedEnumerationSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
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
        "project.Touch();",
    ]
    for token in required:
        if token not in save:
            print("ERROR: missing semantic schedule save bound contract: " + token)
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
    touch = save.find("project.Touch();")
    if min(loop, cap, add, validate, touch) < 0 or not (loop < cap < add < validate < touch):
        print("ERROR: Semantic schedule save capacity guard must run during enumeration before validation or persistence mutation.")
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

    print("PASS: SemanticScheduleCatalog.Save bounds lazy definition enumeration at the first item beyond the 128-definition capacity before validation or project mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
