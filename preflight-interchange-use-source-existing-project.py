#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

SERVICES = {
    "source-element": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceElementImportService.cs",
        'ExistingProjectMutationContext.Require(document, "Interchange source-element import")',
        'EnsureActive(document, "Interchange source-element import")',
    ),
    "source-catalog": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceCatalogImportService.cs",
        'ExistingProjectMutationContext.Require(document, "Interchange source-catalog import")',
        'EnsureActive(document, "Interchange source-catalog import")',
    ),
    "all-scope": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceAllImportService.cs",
        'ExistingProjectMutationContext.Require(document, "Interchange all-scope source import")',
        'EnsureActive(document, "Interchange all-scope source import")',
    ),
}

FORBIDDEN = "ProjectContextCoordinator.GetOrCreate("


def main():
    errors = []
    for name, (path, required, active_guard) in SERVICES.items():
        text = path.read_text(encoding="utf-8")
        if required not in text:
            errors.append(f"{name}: missing existing-project mutation binding")
        if FORBIDDEN in text:
            errors.append(f"{name}: mutation service must not create/bootstrap project state")

        active_index = text.find(active_guard)
        require_index = text.find(required)
        prepare_index = text.find("var prepared = Prepare(project, json);")
        if active_index < 0 or require_index < 0 or prepare_index < 0 or not (active_index < require_index < prepare_index):
            errors.append(f"{name}: active-document guard, existing-project bind, and planning order regressed")

    if errors:
        raise SystemExit("\n".join(errors))

    print("PASS: UseSource mutation services require an existing project and cannot bootstrap one.")


if __name__ == "__main__":
    main()
