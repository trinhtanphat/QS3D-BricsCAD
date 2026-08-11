#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND_ROOT = ROOT / "src" / "QS3D.BricsCAD.V25"
TARGETS = (
    "ProjectInterchangeUseSourceCommands.cs",
    "ProjectInterchangeUseSourceAllCommands.cs",
    "ProjectInterchangeUseSourceCatalogCommands.cs",
)


def main():
    failures = []
    for name in TARGETS:
        path = COMMAND_ROOT / name
        if not path.is_file():
            failures.append(f"missing {path.relative_to(ROOT)}")
            continue

        text = path.read_text(encoding="utf-8")
        rel = path.relative_to(ROOT)
        if "ProjectContextCoordinator.GetOrCreate(" in text:
            failures.append(f"{rel}: UseSource preview must not create/cache a target project")
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
            failures.append(f"{rel}: UseSource preview must resolve the existing target project read-only")
        if "QS3DINTERCHANGEIMPORT" not in text:
            failures.append(f"{rel}: no-project path must direct new/empty targets to QS3DINTERCHANGEIMPORT")
        if "InterchangeConfirmationGuard.RequireFresh(" not in text:
            failures.append(f"{rel}: confirmed import must retain freshness validation")

    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    print("PASS: Interchange UseSource previews are read-only, fail closed without a target project, and keep confirmation freshness guards.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
