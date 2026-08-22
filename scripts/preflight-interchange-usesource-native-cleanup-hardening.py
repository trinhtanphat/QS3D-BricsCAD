#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE_ROOT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services"
SERVICES = {
    "Element": SERVICE_ROOT / "InterchangeUseSourceElementImportService.cs",
    "Catalog": SERVICE_ROOT / "InterchangeUseSourceCatalogImportService.cs",
    "All": SERVICE_ROOT / "InterchangeUseSourceAllImportService.cs",
}


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def check_order(text, label, mutation_marker, failures):
    early_guard = text.find("GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);")
    document_lock = text.find("using (document.LockDocument())")
    locked_bind = text.find("var lockedProject = InterchangeMutationTargetGuard.RequireExact(")
    locked_targets = text.find("var lockedInvalidationTargets = ExpandInvalidationTargets(")
    transaction = text.find("StartTransaction()")
    snapshot = text.find("ProjectStateSnapshot.Capture(lockedProject)")
    locked_guard = text.find("GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);")
    pre_native = text.find("/ pre-native cleanup\"")
    invalidation = text.find("GeneratedDependentGeometryInvalidator.Prepare(")
    pre_semantic = text.find("/ pre-semantic apply\"")
    mutation = text.find(mutation_marker)
    metadata = text.find("invalidation.CommitMetadata();")
    pre_commit = text.find("/ pre-CAD commit\"")
    commit = text.find("transaction.Commit();")

    ordered = [
        early_guard,
        document_lock,
        locked_bind,
        locked_targets,
        transaction,
        snapshot,
        locked_guard,
        pre_native,
        invalidation,
        pre_semantic,
        mutation,
        metadata,
        pre_commit,
        commit,
    ]
    if min(ordered) < 0:
        failures.append(
            f"{label}: missing early/locked cleanup coverage, canonical locked bind/closure, rollback snapshot, "
            "three authority phases, native invalidation, semantic mutation, metadata cleanup, or CAD commit"
        )
    elif ordered != sorted(ordered):
        failures.append(
            f"{label}: required order is early coverage -> document lock -> exact bind/locked closure -> "
            "transaction/snapshot -> locked coverage -> pre-native authority -> native prepare -> "
            "pre-semantic authority -> semantic mutation -> metadata cleanup -> pre-commit authority -> CAD commit"
        )


def main():
    failures = []
    texts = {}
    for name, path in SERVICES.items():
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
            continue
        texts[name] = path.read_text(encoding="utf-8")

    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    mutation_markers = {
        "Element": "ApplyCatalogAdds(lockedProject,",
        "Catalog": "ApplyCatalogState(lockedProject,",
        "All": "ApplyCatalogState(lockedProject,",
    }

    for name, text in texts.items():
        label = f"UseSource {name} service"
        require(text, "using QS3D.BricsCAD.V25;", label, failures)
        require(text, "GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);", label, failures)
        require(text, "GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);", label, failures)
        require(text, "var lockedProject = InterchangeMutationTargetGuard.RequireExact(", label, failures)
        require(text, "var lockedInvalidationTargets = ExpandInvalidationTargets(", label, failures)
        require(text, "ProjectStateSnapshot.Capture(lockedProject)", label, failures)
        require(text, "if (!cadCommitted && rollback != null)", label, failures)
        require(text, "rollback.Restore(project)", label, failures)
        require(text, "GeneratedElementsInvalidated = generatedElementsInvalidated", label, failures)
        require(text, "lockedProject.Touch();", label, failures)

        if text.count("ProjectContextCoordinator.RequireBackingStoreUnchanged(") != 3:
            failures.append(
                f"{label}: must recheck sidecar/backing-store authority exactly at pre-native, pre-semantic, and pre-CAD-commit phases"
            )
        if "var rollback = ProjectStateSnapshot.Capture(project);" in text:
            failures.append(f"{label}: rollback snapshot must not be captured from the pre-lock project context")
        if "GeneratedElementsInvalidated = invalidationTargets.Count" in text:
            failures.append(f"{label}: result must report the locked/native invalidation plan, not the pre-lock closure")

        check_order(text, label, mutation_markers[name], failures)

    element = texts["Element"]
    require(element, "var lockedReplacementTargets = prepared.Plan.ReplacementElementIds", "UseSource Element service", failures)
    require(element, "lockedProject.FindElement(id)", "UseSource Element locked replacement resolution", failures)
    require(
        element,
        "lockedProject,\n                        lockedReplacementTargets,\n                        prepared.Source,\n                        prepared.Resolution",
        "UseSource Element locked invalidation closure",
        failures,
    )

    for name in ("Catalog", "All"):
        text = texts[name]
        require(
            text,
            "var lockedInvalidationTargets = ExpandInvalidationTargets(\n                        lockedProject,\n                        prepared.Source,\n                        prepared.Resolution,\n                        prepared.Plan);",
            f"UseSource {name} locked invalidation closure",
            failures,
        )

    if failures:
        print("QS3D Interchange UseSource native cleanup hardening preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: UseSource Element/Catalog/All reject unsupported generated owner slots before native mutation.")
    print("PASS: each UseSource path rebinds the exact reviewed project and recomputes its affected closure under the document lock.")
    print("PASS: rollback snapshots are captured from the locked canonical project before destructive preparation.")
    print("PASS: backing-store authority is rechecked before native cleanup, before semantic apply, and before CAD commit.")
    print("PASS: reported invalidation counts come from the locked native invalidation plan.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
