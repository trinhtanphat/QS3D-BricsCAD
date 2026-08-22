from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectQuantityReportSemanticSelectionFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/ProjectQuantityReportSemanticSelectionFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

method_start = source.index("private static HashSet<string>? ResolveSelection")
method_end = source.index("private static void AddHandles", method_start)
method = source[method_start:method_end]

for token in (
    "var selectionVersion = project.ChangeVersion;",
    "var knownCount = SnapshotKnownSelectionCount(elementIds);",
    "Project changed while quantity report element-id Count contracts were being inspected",
    "foreach (var raw in elementIds)",
    "if (project.ChangeVersion != selectionVersion)",
    "Project changed while quantity report element ids were being enumerated",
    'ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection")',
    "foreach (var selectedInstance in selectedInstances)",
    "ReferenceEquals(current, selectedInstance.Value)",
):
    assert token in method, f"missing quantity report semantic-freshness contract: {token}"

capture = method.index("var selectionVersion = project.ChangeVersion;")
count_snapshot = method.index("var knownCount = SnapshotKnownSelectionCount(elementIds);", capture)
count_version_check = method.index("if (project.ChangeVersion != selectionVersion)", count_snapshot)
enumeration = method.index("foreach (var raw in elementIds)", count_version_check)
version_check = method.index("if (project.ChangeVersion != selectionVersion)", enumeration)
structural_guard = method.index('ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection")')
assert capture < count_snapshot < count_version_check < enumeration < version_check < structural_guard, (
    "quantity report selection freshness ordering drifted: expected capture -> Count snapshot/check -> enumerate -> semantic check -> structural guard"
)

for token in (
    "StableLazySelectionStillWorks",
    "TouchThenYieldFailsClosed",
    "TouchThenEmptyFailsClosed",
    "ProjectQuantityReportBuilder.Group(project, TouchThenYield(project))",
    "ProjectQuantityReportBuilder.Detail(project, TouchThenEmpty(project))",
    "project.Touch();",
    "yield break;",
):
    assert token in smoke, f"missing quantity report semantic-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "quantity report semantic-freshness smoke is not registered"
assert "ProjectQuantityReportSemanticSelectionFreshnessSmoke.Run();" in registration, (
    "quantity report semantic-freshness registration drifted"
)

print("PASS: quantity report lazy selection fails closed across Count inspection and ProjectState.ChangeVersion changes")
