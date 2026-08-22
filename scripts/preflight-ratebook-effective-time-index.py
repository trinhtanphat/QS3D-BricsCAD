#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/RateBook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateBookSmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing RateBook timestamp-index file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)

constructor_start = source.find("public RateBook(string rateBookId, IEnumerable<RateItem> items)")
resolve_start = source.find("public RateBookResolution Resolve(", constructor_start)
constructor = (
    source[constructor_start:resolve_start]
    if constructor_start >= 0 and resolve_start > constructor_start
    else ""
)

for token in (
    "var effectiveTimesByScope = new Dictionary<string, HashSet<DateTime>>(StringComparer.OrdinalIgnoreCase);",
    "effectiveTimesByScope.Add(scopeKey, new HashSet<DateTime>());",
    "if (!effectiveTimesByScope[scopeKey].Add(item.EffectiveFromUtc))",
    "Ambiguous rate items share the same cost code, unit, currency and effective timestamp:",
):
    if token not in constructor:
        errors.append("RateBook constructor missing indexed effective-time contract: " + token)

if "for (var i = 0; i < scopedItems.Count; i++)" in constructor:
    errors.append("RateBook constructor restored the quadratic per-scope timestamp scan")

reserve = constructor.find("if (!effectiveTimesByScope[scopeKey].Add(item.EffectiveFromUtc))")
append = constructor.find("scopedItems.Add(item);")
if min(reserve, append) < 0 or not reserve < append:
    errors.append("RateBook must reserve each effective timestamp before appending the scoped item")

for token in (
    "LargeSingleScopeUsesIndexedTimestampUniqueness",
    "const int count = 4096;",
    'Equal("RATE-LARGE-0000", book.Items[0].RateItemId',
    'Equal("RATE-LARGE-4095", book.Items[count - 1].RateItemId',
    'Equal("RATE-LARGE-4095", resolved.Item!.RateItemId',
    'Throws<ArgumentException>(() => new RateBook("BOOK-LARGE-DUP", items));',
):
    if token not in smoke:
        errors.append("RateBook smoke missing timestamp-index regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: RateBook reserves per-scope effective timestamps through an index before append; duplicate semantics, deterministic ordering, and large-scope lookup behavior remain covered.")
