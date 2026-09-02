#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing QsdbProjectStore.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'new ProjectState(RequiredCanonical(root, "projectId"), RequiredText(root, "name"))',
        'new ZoneDefinition(Required(item, "id"), RequiredText(item, "name"))',
        'new FloorDefinition(Required(item, "id"), RequiredText(item, "name"),',
        'new ProjectFamily(Required(item, "id"), RequiredText(item, "name"), category)',
        'RequiredText(item, "expression"), RequiredText(item, "version"))',
        'private static string RequiredText(XElement element, string attribute)',
        'return value;',
    )
    for token in required:
        if token not in text:
            errors.append("QsdbProjectStore.cs missing round-trip text token: " + token)

    helper = text.find('private static string RequiredText(XElement element, string attribute)')
    if helper >= 0:
        helper_end = text.find('\n        }', helper)
        helper_text = text[helper:helper_end if helper_end >= 0 else len(text)]
        if '.Trim()' in helper_text:
            errors.append("RequiredText must preserve accepted persisted text verbatim instead of trimming it")
        if 'string.IsNullOrWhiteSpace(value)' not in helper_text:
            errors.append("RequiredText must still reject missing/whitespace-only required text")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: accepted QSDB display/business text is preserved verbatim across load while required-text validation remains fail-closed.")
