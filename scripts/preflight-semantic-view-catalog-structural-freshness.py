#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticViewPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticViewCatalogStructuralFreshnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SemanticViewCatalogStructuralFreshnessSmokeRegistration.cs"
errors = []

for path in (SOURCE, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing semantic View catalog freshness file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("public static IReadOnlyList<SemanticViewPlan> BuildCatalog(")
    end = source.find("private static ProjectStructureSnapshot CaptureProjectStructure", start)
    method = source[start:end] if start >= 0 and end > start else ""
    capture = method.find("var projectSnapshot = CaptureProjectStructure(project);")
    materialize = method.find("var materialized = MaterializeCatalogBounded(definitions);")
    first_check = method.find("EnsureProjectStructureUnchanged(project, projectSnapshot);", materialize)
    result = method.find("var result = plans", first_check)
    second_check = method.find("EnsureProjectStructureUnchanged(project, projectSnapshot);", result)
    returned = method.find("return result;", second_check)
    if min(capture, materialize, first_check, result, second_check, returned) < 0 or not (
        capture < materialize < first_check < result < second_check < returned
    ):
        errors.append("BuildCatalog must snapshot before definition enumeration and recheck after enumeration and before return.")
    if method.count("EnsureProjectStructureUnchanged(project, projectSnapshot);") != 2:
        errors.append("BuildCatalog must perform exactly two project freshness checks.")

    for token in (
        "project.ChangeVersion,",
        "project.Elements.ToArray(),",
        "project.Floors.ToArray(),",
        "project.Zones.ToArray());",
        "if (project.ChangeVersion != snapshot.ChangeVersion)",
        "EnsureSameReferences(project.Elements, snapshot.Elements);",
        "EnsureSameElementPlanningValues(project.Elements, snapshot.ElementPlanningValues);",
        "EnsureSameReferences(project.Floors, snapshot.Floors);",
        "EnsureSameReferences(project.Zones, snapshot.Zones);",
        "if (!ReferenceEquals(current[i], expected[i]))",
        "ElementPlanningValues = elements.Select(x => new ProjectElementPlanningValues(x)).ToArray();",
        "!string.Equals(element.Id, values.Id, StringComparison.Ordinal)",
        "element.Category != values.Category",
        "!string.Equals(element.FloorId, values.FloorId, StringComparison.Ordinal)",
        "!string.Equals(element.ZoneId, values.ZoneId, StringComparison.Ordinal)",
    ):
        if token not in source:
            errors.append("SemanticViewPlanner missing structural freshness token: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SameIdElementReplacementFailsClosed();",
        "SameInstanceCategoryDriftFailsClosed();",
        "SameInstanceRelationDriftFailsClosed();",
        "RevisionDriftFailsClosed();",
        "StableCatalogRemainsDeterministicAndReadOnly();",
        'project.Elements[0] = new ProjectElement("E-01", ElementCategory.Column',
        "project.Elements[0].Category = ElementCategory.Column;",
        'project.Elements[0].FloorId = "F-02";',
        'project.Elements[0].ZoneId = "Z-02";',
        "project.Touch();",
        "Throws<InvalidOperationException>(() => SemanticViewPlanner.BuildCatalog(",
        "Throws<NotSupportedException>(() => mutable[0] = catalog[1]);",
    ):
        if token not in smoke:
            errors.append("Semantic View catalog freshness smoke missing token: " + token)

if REGISTRATION.is_file():
    registration = REGISTRATION.read_text(encoding="utf-8")
    if "[ModuleInitializer]" not in registration or "SemanticViewCatalogStructuralFreshnessSmoke.Run();" not in registration:
        errors.append("Semantic View catalog structural freshness smoke is not module-registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: SemanticViewPlanner.BuildCatalog rejects project revision, ordered reference, or planner-value drift across caller-controlled definition enumeration.")
