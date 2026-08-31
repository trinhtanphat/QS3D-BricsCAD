#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeMapBoundSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "public const int MaxInterchangeMapItems = 4096;",
    'AppendStringMap(json, family.Properties, IsInterchangeProperty, 2, "family properties");',
    'AppendStringMap(json, element.Properties, ProjectInterchangeElementPropertyPolicy.IsPortable, 3, "element properties");',
    'AppendNumberMap(json, element.Quantities, "element quantities");',
    "if (items.Count >= MaxInterchangeMapItems)",
    "items.Add(item);",
    "items.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key));",
    "if (source.Count > MaxInterchangeMapItems)",
    "var items = source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing interchange map-bound source contract: {marker}")

string_helper = source.index("private static void AppendStringMap")
string_bound = source.index("if (items.Count >= MaxInterchangeMapItems)", string_helper)
string_retain = source.index("items.Add(item);", string_bound)
string_sort = source.index("items.Sort((left, right) =>", string_retain)
if not string_helper < string_bound < string_retain < string_sort:
    raise SystemExit("portable string-map ceiling must fail before retention/sort")

number_helper = source.index("private static void AppendNumberMap")
number_bound = source.index("if (source.Count > MaxInterchangeMapItems)", number_helper)
number_materialize = source.index("source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();", number_bound)
if not number_helper < number_bound < number_materialize:
    raise SystemExit("quantity-map ceiling must fail before sorting/materialization")

required_smoke = [
    "ExactLimitPortablePropertiesRemainExportable();",
    "FirstPortablePropertyBeyondLimitFailsClosed();",
    "StablePropertyAndQuantityMapsRemainExportable();",
    "ProjectInterchangeJsonExporter.MaxInterchangeMapItems",
    '"4096-member map limit"',
    'element.Quantities["Area"] = 12.5;',
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing interchange map-bound smoke contract: {marker}")

print("project interchange map-bound preflight: PASS")
