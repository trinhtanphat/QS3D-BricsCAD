#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
SERVICE = CAD / "GeneratedRebarNativeOwnershipService.cs"
SLAB = CAD / "SlabMeshSolidBuilder.cs"
FOUNDATION = CAD / "FoundationMeshSolidBuilder.cs"
MULTI = CAD / "SlabFoundationMultiRegionMeshSolidBuilder.cs"


def read(path):
    return path.read_text(encoding="utf-8")


def require(condition, message, failures):
    if not condition:
        failures.append(message)


def normalized(text):
    return re.sub(r"\s+", " ", text)


def main():
    failures = []
    sources = {path: read(path) for path in (SERVICE, SLAB, FOUNDATION, MULTI)}
    service = sources[SERVICE]

    require("MarkFreshGeneratedHandles" not in service,
            "native ownership service must not reintroduce handle-based fresh-object marking", failures)
    require("IsNewObject" not in service,
            "native ownership service must not depend on host-specific Entity.IsNewObject after handle re-resolution", failures)
    require("RequireMatchingOwnership" in service and "HasMatchingOwnership" in service,
            "native ownership service must retain fail-closed ownership verification for destructive operations", failures)

    append_pattern = re.compile(
        r"modelSpace\.AppendEntity\(bar\);\s*"
        r"transaction\.AddNewlyCreatedDBObject\(bar,\s*true\);\s*"
        r"GeneratedRebarNativeOwnershipService\.MarkGenerated\([^;]*bar[^;]*\);\s*"
        r"update\.Handles\.Add\(bar\.Handle\.ToString\(\)\);",
        re.DOTALL,
    )

    for path in (SLAB, FOUNDATION):
        text = sources[path]
        rel = path.relative_to(ROOT)
        require("MarkFreshGeneratedHandles" not in text,
                str(rel) + " must not re-resolve freshly appended bars by handle", failures)
        require(append_pattern.search(text) is not None,
                str(rel) + " must mark QS3D ownership on the live bar immediately after append/add and before persisting its handle", failures)
        require("GeneratedRebarNativeOwnershipService.RequireMatchingOwnership" in text,
                str(rel) + " must retain fail-closed native ownership verification before erase", failures)

    multi = normalized(sources[MULTI])
    require(
        "AppendEntity(bar); transaction.AddNewlyCreatedDBObject(bar, true); GeneratedRebarNativeOwnershipService.MarkGenerated(" in multi,
        "multi-region slab/foundation builder must retain the same direct live-entity ownership precedent",
        failures,
    )

    if failures:
        print("ERROR: generated rebar native ownership preflight failed:")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: generated rebar ownership is marked on live appended entities without IsNewObject/handle re-resolution drift.")
    print("PASS: destructive erase paths retain native project/element/owner-slot verification.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
