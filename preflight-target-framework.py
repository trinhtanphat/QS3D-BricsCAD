#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

core_project = ROOT / "src/QS3D.Core/QS3D.Core.csproj"
plugin_project = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if not core_project.is_file(): errors.append("missing Core project file")
if not plugin_project.is_file(): errors.append("missing BricsCAD V25 project file")
if core_project.is_file() and "<TargetFramework>netstandard2.0</TargetFramework>" not in core_project.read_text(encoding="utf-8"):
    errors.append("QS3D.Core target framework drifted from netstandard2.0")
if plugin_project.is_file() and "<TargetFramework>net48</TargetFramework>" not in plugin_project.read_text(encoding="utf-8"):
    errors.append("QS3D.BricsCAD.V25 target framework drifted from net48")

# High-confidence APIs that are unavailable on netstandard2.0 / .NET Framework 4.8.
patterns = [
    (re.compile(r"\bMath\.Clamp\s*\("), "Math.Clamp"),
    (re.compile(r"\bStringSplitOptions\.TrimEntries\b"), "StringSplitOptions.TrimEntries"),
    (re.compile(r"\bRandom\.Shared\b"), "Random.Shared"),
    (re.compile(r"\bDateOnly\b"), "DateOnly"),
    (re.compile(r"\bTimeOnly\b"), "TimeOnly"),
    (re.compile(r"\.(?:MaxBy|MinBy|DistinctBy|Chunk)\s*\("), "newer LINQ operator"),
    (re.compile(r"\.Contains\s*\([^;\n]*\bStringComparison\."), "string.Contains(StringComparison) overload"),
    (re.compile(r"\.Replace\s*\([^;\n]*\bStringComparison\."), "string.Replace(StringComparison) overload"),
    (re.compile(r"\bConvert\.ToHexString\s*\("), "Convert.ToHexString"),
    (re.compile(r"\bSHA(?:1|256|384|512)\.HashData\s*\("), "static HashData API"),
    (re.compile(r"\bOperatingSystem\.Is(?:Windows|Linux|MacOS)\s*\("), "OperatingSystem.Is* API"),
]

roots = [ROOT / "src/QS3D.Core", ROOT / "src/QS3D.BricsCAD.V25"]
for source_root in roots:
    if not source_root.is_dir():
        continue
    for path in source_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        # Remove single-line comments to reduce false positives from documentation/examples.
        code = "\n".join(line.split("//", 1)[0] for line in text.splitlines())
        for pattern, label in patterns:
            for match in pattern.finditer(code):
                line = code.count("\n", 0, match.start()) + 1
                errors.append(str(path.relative_to(ROOT)) + ":" + str(line) + " uses unsupported target-framework API: " + label)

# File.Move(source,destination,overwrite) is not available on these targets.
move_three_args = re.compile(r"\bFile\.Move\s*\([^;\n]*,[^;\n]*,[^;\n]*\)")
for source_root in roots:
    if not source_root.is_dir():
        continue
    for path in source_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        code = "\n".join(line.split("//", 1)[0] for line in text.splitlines())
        for match in move_three_args.finditer(code):
            line = code.count("\n", 0, match.start()) + 1
            errors.append(str(path.relative_to(ROOT)) + ":" + str(line) + " uses unsupported File.Move overwrite overload")

print("QS3D target-framework compatibility preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: netstandard2.0/net48 targets and high-confidence incompatible API guards are clean.")
