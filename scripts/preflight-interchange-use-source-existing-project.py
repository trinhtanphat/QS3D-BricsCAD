#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

SERVICES = {
    "source-element": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceElementImportService.cs",
        '"Interchange source-element import"',
    ),
    "source-catalog": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceCatalogImportService.cs",
        '"Interchange source-catalog import"',
    ),
    "all-scope": (
        ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceAllImportService.cs",
        '"Interchange all-scope source import"',
    ),
}

GUARD = ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeMutationTargetGuard.cs"
FORBIDDEN = ("ProjectContextCoordinator.GetOrCreate(", "ExistingProjectMutationContext.Require(")


def main():
    errors = []
    guard_text = GUARD.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "ReferenceEquals(currentProject, authorizedProject)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    ):
        if token not in guard_text:
            errors.append("exact reviewed-project guard missing existing/active target invariant: " + token)
    if "ProjectContextCoordinator.GetOrCreate(" in guard_text:
        errors.append("exact reviewed-project guard must not create/bootstrap project state")

    for name, (path, operation) in SERVICES.items():
        text = path.read_text(encoding="utf-8")
        for token in (
            "ProjectState authorizedProject",
            "InterchangeMutationTargetGuard.RequireExact(",
            operation,
        ):
            if token not in text:
                errors.append(f"{name}: missing exact reviewed existing-project binding: {token}")
        for forbidden in FORBIDDEN:
            if forbidden in text:
                errors.append(f"{name}: mutation service must not independently create/rebind project state: {forbidden}")

        require_index = text.find("InterchangeMutationTargetGuard.RequireExact(")
        prepare_index = text.find("var prepared = Prepare(project, json);")
        if require_index < 0 or prepare_index < 0 or require_index >= prepare_index:
            errors.append(f"{name}: exact reviewed-project bind must precede mutation planning")

    if errors:
        raise SystemExit("\n".join(errors))

    print("PASS: UseSource mutation services require the exact active existing project instance that was reviewed and cannot bootstrap or independently rebind one.")


if __name__ == "__main__":
    main()
