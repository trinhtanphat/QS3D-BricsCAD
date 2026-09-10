#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

start = source.find("private void OnAssignClick(object sender, RoutedEventArgs e)")
end = source.find("private void RefreshAll(", start if start >= 0 else 0)
assign = source[start:end if end >= 0 else len(source)] if start >= 0 else ""
if start < 0:
    errors.append("missing Family Manager OnAssignClick")
else:
    required = [
        'EnsureActive("gán Family cho selection");',
        "if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var previewProject))",
        "var expectedProjectId = previewProject.ProjectId;",
        'var project = ExistingProjectMutationContext.Require(_document, "Gán Family cho selection");',
        "!ReferenceEquals(project, _boundProject)",
        "!ReferenceEquals(project, previewProject)",
        "!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        "var family = project.FindFamily(previewFamily.Id)",
        "if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))",
        "var changed = ExecuteAtomic(project, () =>",
        "RefreshAfterCommit(",
    ]
    for needle in required:
        if needle not in assign:
            errors.append("Family assignment affinity contract missing: " + needle)

    reacquire = assign.find('var project = ExistingProjectMutationContext.Require(_document, "Gán Family cho selection");')
    exact_bound = assign.find("!ReferenceEquals(project, _boundProject)", reacquire if reacquire >= 0 else 0)
    exact_preview = assign.find("!ReferenceEquals(project, previewProject)", reacquire if reacquire >= 0 else 0)
    project_id = assign.find("!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)", reacquire if reacquire >= 0 else 0)
    resolve_family = assign.find("var family = project.FindFamily(previewFamily.Id)", reacquire if reacquire >= 0 else 0)
    resolve_elements = assign.find("var elements = SemanticSelectionResolver.ResolveImplied(_document, project)", reacquire if reacquire >= 0 else 0)
    mutate = assign.find("var changed = ExecuteAtomic(project, () =>", reacquire if reacquire >= 0 else 0)
    if min(reacquire, exact_bound, exact_preview, project_id, resolve_family, resolve_elements, mutate) < 0 or not (
        reacquire < exact_bound < resolve_family and
        reacquire < exact_preview < resolve_family and
        reacquire < project_id < resolve_family and
        resolve_family < resolve_elements < mutate
    ):
        errors.append("exact bound/preview project fences must run immediately after mutation-context reacquisition and before family/selection resolution or mutation")

print("QS3D V25 Family Manager exact-project assignment affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Family assignment rejects same-id replacement ProjectState instances before resolving or mutating the selection.")