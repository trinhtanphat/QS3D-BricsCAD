#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFamilyWorkspace.cs"
METHOD = "private void RestoreBlt3dWorkspaceColumns()"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find(METHOD)
    if start < 0:
        return fail("BLT3D workspace bootstrap method was not found.")

    next_method = source.find("\n        private ", start + len(METHOD))
    body = source[start:] if next_method < 0 else source[start:next_method]

    if re.search(r"modelColumn\.Width\s*=\s*new\s+GridLength\(\s*220(?:\.0+)?\s*(?:,|\))", body):
        return fail("BLT3D workspace bootstrap must not hard-code the model column to 220 px.")

    required = {
        "persisted UI layout": "Services.UserUiLayoutStore.Get()",
        "persisted model-column width": "layout.ModelColumnWidth",
        "canonical model-column clamp": "Math.Max(modelColumn.MinWidth, Math.Min(modelColumn.MaxWidth, layout.ModelColumnWidth))",
        "flexible Family column": "familyColumn.Width = new GridLength(1, GridUnitType.Star);",
        "retired room/inspection columns": "retired.Width = new GridLength(0);",
    }
    missing = [label for label, token in required.items() if token not in body]
    if missing:
        return fail("BLT3D workspace persistence contract is missing: " + ", ".join(missing) + ".")

    print("PASS: BLT3D workspace bootstrap restores persisted model-column width, clamps it to workspace bounds, keeps Family flexible, retires room/inspection columns, and does not hard-code 220 px.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
