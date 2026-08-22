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
    "foreach (var raw in elementIds)",
    "if (project.ChangeVersion != selectionVersion)",
    "Project changed while quantity report element ids were being enumerated",
    'ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection")',
    "foreach (var selectedInstance in selectedInstances)",
    "ReferenceEquals(current, selectedInstance.Value)",
):
    assert token in method, f"missing quantity report semantic-freshness contract: {token}"

capture = method.index("var selectionVersion = project.ChangeVersion;")
enumeration = method.index("foreach (var raw in elementIds)")
version_check = method.index("if (project.ChangeVersion != selectionVersion)")
structural_guard = method.index('ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection")')
assert capture < enumeration < version_check < structural_guard, (
    "quantity report selection freshness ordering drifted: expected capture -> enumerate -> semantic check -> structural guard"
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

print("PASS: quantity report lazy selection fails closed across ProjectState.ChangeVersion changes")
