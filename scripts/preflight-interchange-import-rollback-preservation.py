#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTERS = {
    "src/QS3D.Core/Export/ProjectInterchangeAppendOnlyImporter.cs": "Interchange append-only import failed and project rollback also failed.",
    "src/QS3D.Core/Export/ProjectInterchangeKeepTargetImporter.cs": "Interchange KeepTarget import failed and project rollback also failed.",
    "src/QS3D.Core/Export/ProjectInterchangeRemapAppendImporter.cs": "Interchange Import As New failed and project rollback also failed.",
}
errors = []

for relative, rollback_message in IMPORTERS.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing mutating interchange importer: " + relative)
        continue

    text = path.read_text(encoding="utf-8")
    required = (
        "catch (Exception operationError)",
        "catch (Exception restoreError)",
        "new AggregateException(operationError, restoreError)",
        rollback_message,
    )
    for token in required:
        if token not in text:
            errors.append(relative + " must preserve both operation and rollback failures; missing: " + token)

    operation_index = text.find("catch (Exception operationError)")
    restore_index = text.find("catch (Exception restoreError)", operation_index + 1)
    aggregate_index = text.find("new AggregateException(operationError, restoreError)", restore_index + 1)
    if operation_index < 0 or restore_index < operation_index or aggregate_index < restore_index:
        errors.append(relative + " rollback preservation block is missing or out of order.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: all mutating semantic interchange importers preserve the original operation error when project rollback also fails.")
