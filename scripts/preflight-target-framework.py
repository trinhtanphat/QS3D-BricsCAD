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
# Keep call-local regexes bounded by the matching call's first closing parenthesis so a later
# StartsWith(..., StringComparison) on the same line cannot be attributed to Contains().
patterns = [
    (re.compile(r"\bMath\.Clamp\s*\("), "Math.Clamp"),
    (re.compile(r"\bStringSplitOptions\.TrimEntries\b"), "StringSplitOptions.TrimEntries"),
    (re.compile(r"\bRandom\.Shared\b"), "Random.Shared"),
    (re.compile(r"\bDateOnly\b"), "DateOnly"),
    (re.compile(r"\bTimeOnly\b"), "TimeOnly"),
    (re.compile(r"\.(?:MaxBy|MinBy|DistinctBy|Chunk)\s*\("), "newer LINQ operator"),
    (re.compile(r"\.Contains\s*\([^\)\n]*\bStringComparison\."), "string.Contains(StringComparison) overload"),
    (re.compile(r"\.Replace\s*\([^\)\n]*\bStringComparison\."), "string.Replace(StringComparison) overload"),
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


def call_top_level_comma_count(code, open_paren):
    """Return top-level comma count for one call, or None when parentheses are malformed.

    This intentionally understands nested parentheses and C# string/char literals so calls such as
    File.Move(Path.Combine(a, b), target) are not mistaken for the unsupported three-argument overload.
    """
    depth = 0
    commas = 0
    quote = None
    verbatim = False
    escaped = False
    index = open_paren
    while index < len(code):
        ch = code[index]
        if quote is not None:
            if quote == '"' and verbatim:
                if ch == '"':
                    if index + 1 < len(code) and code[index + 1] == '"':
                        index += 2
                        continue
                    quote = None
                    verbatim = False
                index += 1
                continue
            if escaped:
                escaped = False
                index += 1
                continue
            if ch == '\\':
                escaped = True
                index += 1
                continue
            if ch == quote:
                quote = None
            index += 1
            continue

        if ch in ('"', "'"):
            quote = ch
            verbatim = ch == '"' and index > 0 and code[index - 1] == '@'
            index += 1
            continue
        if ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
            if depth == 0:
                return commas
            if depth < 0:
                return None
        elif ch == ',' and depth == 1:
            commas += 1
        index += 1
    return None


# File.Move(source,destination,overwrite) is not available on these targets. Parse the call instead
# of using a comma regex: nested calls such as File.Move(Path.Combine(a, b), target) are valid.
move_call = re.compile(r"\bFile\.Move\s*\(")
for source_root in roots:
    if not source_root.is_dir():
        continue
    for path in source_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        code = "\n".join(line.split("//", 1)[0] for line in text.splitlines())
        for match in move_call.finditer(code):
            open_paren = code.find("(", match.start(), match.end())
            comma_count = call_top_level_comma_count(code, open_paren)
            if comma_count is None:
                continue
            if comma_count >= 2:
                line = code.count("\n", 0, match.start()) + 1
                errors.append(str(path.relative_to(ROOT)) + ":" + str(line) + " uses unsupported File.Move overwrite overload")

print("QS3D target-framework compatibility preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: netstandard2.0/net48 targets and high-confidence incompatible API guards are clean.")
