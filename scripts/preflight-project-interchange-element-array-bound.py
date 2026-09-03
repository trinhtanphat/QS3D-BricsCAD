#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeElementArrayBoundSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "public const int MaxElementStringArrayItems = 4096;",
    'AppendStringArray(json, element.SourceHandles, "sourceHandles");',
    'AppendStringArray(json, element.DependsOn, "dependencies");',
    "if (items.Count >= MaxElementStringArrayItems)",
    'MaxElementStringArrayItems.ToString(CultureInfo.InvariantCulture) + "-item per-element limit."',
    "items.Add(raw);",
    "items.Sort(StringComparer.OrdinalIgnoreCase);",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing interchange element-array source contract: {marker}")

helper = source.index("private static void AppendStringArray")
bound = source.index("if (items.Count >= MaxElementStringArrayItems)", helper)
empty = source.index("if (string.IsNullOrWhiteSpace(raw))", bound)
duplicate = source.index("if (!seen.Add(raw))", empty)
retain = source.index("items.Add(raw);", duplicate)
sort = source.index("items.Sort(StringComparer.OrdinalIgnoreCase);", retain)
if not helper < bound < empty < duplicate < retain < sort:
    raise SystemExit("interchange element-array ceiling must fail before validation/retention of the first over-limit item")

required_smoke = [
    "ExactLimitSourceHandlesRemainExportable();",
    "FirstSourceHandleBeyondLimitFailsClosed();",
    "StableDependenciesRemainExportable();",
    "ProjectInterchangeJsonExporter.MaxElementStringArrayItems",
    '"4096-item per-element limit"',
    'dependent.DependsOn.Add(source.Id);',
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing interchange element-array smoke contract: {marker}")

print("project interchange element-array bound preflight: PASS")
