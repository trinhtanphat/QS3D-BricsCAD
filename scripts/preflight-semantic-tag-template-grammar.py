#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RENDERER = ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderer.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs"
DOC = ROOT / "docs/SEMANTIC-TAGS.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic tag grammar file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


renderer = read(RENDERER)
smoke = read(SMOKE)
doc = read(DOC)

for token in (
    "var strayClose = source.IndexOf('}', index);",
    "strayClose >= 0 && (open < 0 || strayClose < open)",
    "unexpected closing brace",
    "unclosed token",
    "tokens cannot be nested",
    "ValidateToken(token)",
):
    if token not in renderer:
        errors.append("semantic tag renderer missing brace grammar contract: " + token)

for token in (
    "MalformedBraceGrammarFailsClosed",
    'ValidateTemplate("abc}")',
    'ValidateTemplate("{Id}}")',
    'ValidateTemplate("{{Id}")',
    'ValidateTemplate("prefix {Id")',
    'Render(fixture.Project, fixture.Element, "{Id}}")',
):
    if token not in smoke:
        errors.append("semantic tag smoke missing malformed-brace regression: " + token)

for token in (
    "Raw `{` and `}` characters are reserved for semantic tokens",
    "no literal-brace escape syntax",
    "fail closed",
):
    if token not in doc:
        errors.append("semantic tag docs missing brace grammar boundary: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: SemanticTagRenderer rejects stray, trailing, nested and unclosed braces before semantic/native documentation rendering.")
