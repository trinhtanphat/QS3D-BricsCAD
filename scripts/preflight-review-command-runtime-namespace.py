#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RulePreviewAndDiagnosticCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing review command adapter: " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "using Teigha.Runtime;",
        "[CommandMethod(",
        "CommandFlags.Modal",
    ):
        if token not in text:
            errors.append("review command runtime contract missing: " + token)
    if "using Bricscad.Runtime;" in text:
        errors.append("review command adapter must use Teigha.Runtime for CommandMethodAttribute/CommandFlags, not Bricscad.Runtime")

if errors:
    print("preflight-review-command-runtime-namespace: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-review-command-runtime-namespace: PASS")
print("V25 review command attributes resolve through Teigha.Runtime.")
