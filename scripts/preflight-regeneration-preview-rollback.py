#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
path = root / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
errors = []

if not path.is_file():
    errors.append("missing RegenerationPreviewService.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "catch (Exception applyError)",
        "snapshot.Restore(project);",
        "catch (Exception rollbackError)",
        'throw new AggregateException("Regeneration preview apply failed and project rollback also failed.", applyError, rollbackError);',
    )
    for token in required:
        if token not in text:
            errors.append("regeneration preview rollback boundary missing: " + token)

    if "catch\n            {\n                snapshot.Restore(project);\n                throw;\n            }" in text:
        errors.append("regeneration preview still allows rollback failure to mask the original apply error")

if errors:
    print("preflight-regeneration-preview-rollback: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-regeneration-preview-rollback: PASS")
print("Regeneration preview apply preserves the original failure and reports rollback failure without masking it.")
