#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGETS = (
    (
        ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs",
        (
            ("PromptPositiveMeters", "defaultValue"),
            ("PromptFiniteMeters", "defaultValue"),
        ),
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs",
        (
            ("PromptPositiveMeters", "defaultValue"),
            ("PromptNonNegativeMeters", "defaultValue"),
        ),
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/DirectDrawWindowCommands.cs",
        (
            ("PromptPositiveMeters", "defaultValue"),
            ("PromptNonNegativeMeters", "defaultValue"),
        ),
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs",
        (
            ("PromptPositiveMeters", "defaultValue"),
            ("PromptFiniteMeters", "defaultValue"),
        ),
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/DirectDrawReferenceWallCommands.cs",
        (
            ("PromptPositiveMeters", "safeDefault"),
            ("PromptFiniteMeters", "safeDefault"),
        ),
    ),
)
errors = []

for source, helpers in TARGETS:
    if not source.is_file():
        errors.append("missing Direct Draw prompt source: " + str(source.relative_to(ROOT)))
        continue

    text = source.read_text(encoding="utf-8")
    for helper, expected_default in helpers:
        match = re.search(
            r"private static double\? " + re.escape(helper) + r"\(.*?\n        \}",
            text,
            re.DOTALL,
        )
        label = source.name + "/" + helper
        if not match:
            errors.append("missing Direct Draw numeric prompt helper: " + label)
            continue
        body = match.group(0)
        if 'new PromptDoubleOptions("\\n" + label + ": ")' not in body:
            errors.append(label + " must let BricsCAD render the configured default exactly once")
        if "UseDefaultValue = true" not in body or ("DefaultValue = " + expected_default) not in body:
            errors.append(label + " must keep native PromptDoubleOptions default handling via " + expected_default)
        if (
            'defaultValue.ToString("0.###"' in body
            or 'safeDefault.ToString("0.###"' in body
            or '" <" + defaultValue' in body
            or '" <" + safeDefault' in body
        ):
            errors.append(label + " must not manually embed the default in the prompt when UseDefaultValue is enabled")

print("QS3D Direct Draw numeric prompt-default preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Direct Draw wall/structural/door-opening/window/reference numeric prompts delegate default rendering to BricsCAD and do not duplicate <default> text.")
