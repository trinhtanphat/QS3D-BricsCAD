#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = (
    (
        ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs",
        "private void OnAssignClick",
        "ExistingProjectMutationContext.Require(_document, \"Gán Family cho selection\")",
        "ProjectFamilyService.Assign(project, family.Id, elements)",
        "Family",
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
        "private void OnApplyClick",
        "ExistingProjectMutationContext.TryGet(_document, out var project)",
        "element.SetProperty(target, material.Name)",
        "Material",
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs",
        "private void OnAssignClick",
        "ExistingProjectMutationContext.Require(_document, \"Gán Zone cho selection\")",
        "ProjectZoneService.Assign(project, zone.Id, elements)",
        "Zone",
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
        "private void OnAssignClick",
        "ExistingProjectMutationContext.Require(_document, \"Gán Floor/Level cho selection\")",
        "ProjectFloorService.Assign(project, floor.Id, elements)",
        "Floor/Level",
    ),
)
errors = []


def method_body(text, start_token):
    start = text.find(start_token)
    if start < 0:
        return ""
    next_method = text.find("\n        private void ", start + len(start_token))
    if next_method < 0:
        next_method = len(text)
    return text[start:next_method]


for path, method, bind_token, mutation_token, label in CASES:
    if not path.is_file():
        errors.append("missing modeless semantic assignment file: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    body = method_body(text, method)
    if not body:
        errors.append(path.name + " missing " + method)
        continue

    preview = body.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var previewProject)")
    first_resolve = body.find("SemanticSelectionResolver.ResolveImplied(_document, previewProject)")
    empty_guard = body.find("Count == 0")
    bind = body.find(bind_token)
    project_guard = body.find("expectedProjectId")
    canonical_resolve = body.find("SemanticSelectionResolver.ResolveImplied(_document, project)", bind if bind >= 0 else 0)
    freshness = body.find("SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase)")
    mutation = body.find(mutation_token)

    if min(preview, first_resolve, empty_guard, bind) < 0 or not preview < first_resolve < empty_guard < bind:
        errors.append(label + " assignment must resolve a read-only semantic selection and reject empty selection before canonical mutation binding")
    if project_guard < 0 or bind < 0 or project_guard > bind:
        errors.append(label + " assignment must capture preview ProjectId before canonical mutation binding")
    if min(bind, canonical_resolve, freshness, mutation) < 0 or not bind < canonical_resolve < freshness < mutation:
        errors.append(label + " assignment must re-resolve canonical semantic ownership and verify freshness before mutation")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in body:
        errors.append(label + " assignment must not create/cache a replacement project directly")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Family/Material/Zone/Floor modeless semantic assignment resolves selection before project bind and rechecks ProjectId/ownership freshness before mutation")
