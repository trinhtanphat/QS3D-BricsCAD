#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GUARD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeMutationTargetGuard.cs"
CALLERS = [
    ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeUseSourceElementImportService.cs",
    ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeUseSourceCatalogImportService.cs",
    ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeUseSourceAllImportService.cs",
]


def main():
    failures = []
    paths = [GUARD, *CALLERS]
    for path in paths:
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    guard = GUARD.read_text(encoding="utf-8")
    active = guard.find("MdiActiveDocument")
    exact = guard.find("ReferenceEquals(currentProject, authorizedProject)")
    backing = guard.find("ProjectContextCoordinator.RequireBackingStoreUnchanged(")
    returned = guard.find("return currentProject;")
    if min(active, exact, backing, returned) < 0:
        failures.append("exact mutation guard is missing active-DWG, exact-project, backing-store, or return boundary")
    elif not (active < exact < backing < returned):
        failures.append("exact mutation guard must verify active DWG -> exact reviewed project -> backing-store revision before returning a mutable target")

    if "ProjectContextCoordinator.GetOrCreate(" in guard:
        failures.append("exact mutation guard must remain non-creating")

    for caller in CALLERS:
        text = caller.read_text(encoding="utf-8")
        if "InterchangeMutationTargetGuard.RequireExact(" not in text:
            failures.append(f"{caller.name} no longer binds through InterchangeMutationTargetGuard.RequireExact")
        if "GeneratedDependentGeometryInvalidator.Prepare(" not in text:
            failures.append(f"{caller.name} no longer exposes the destructive generated-geometry invalidation path this gate protects")

    if failures:
        print("QS3D Interchange exact-target backing-store preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: exact Interchange target binding remains active-DWG, identity-exact, non-creating and backing-store-aware.")
    print("PASS: UseSource Element/Catalog/All destructive import paths enter through the hardened exact-target guard.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
