from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsLiveWorkbook2.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    ".Distinct(StringComparer.Ordinal)\n                .OrderBy(x => x, StringComparer.Ordinal)",
    "bindingList.GroupBy(x => x.BindingId, StringComparer.Ordinal)",
    "ToDictionary(x => x.BindingId, StringComparer.Ordinal)",
    "new Dictionary<string, LiveWorkbookRefreshResult>(StringComparer.Ordinal)",
    "binding.DependsOnBindingIds.OrderBy(x => x, StringComparer.Ordinal)",
    "ThenBy(x => x.Binding.BindingId, StringComparer.Ordinal)",
    "bindings.Keys.ToDictionary(x => x, x => 0, StringComparer.Ordinal)",
    "new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.Ordinal)",
]
for marker in required:
    if marker not in text:
        raise SystemExit(f"missing case-sensitive live workbook binding identity marker: {marker}")

for forbidden in [
    "bindingList.GroupBy(x => x.BindingId, StringComparer.OrdinalIgnoreCase)",
    "ToDictionary(x => x.BindingId, StringComparer.OrdinalIgnoreCase)",
    "new Dictionary<string, LiveWorkbookRefreshResult>(StringComparer.OrdinalIgnoreCase)",
    "binding.DependsOnBindingIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
    "ThenBy(x => x.Binding.BindingId, StringComparer.OrdinalIgnoreCase)",
]:
    if forbidden in text:
        raise SystemExit(f"binding/dependency identity must not be case-insensitive: {forbidden}")

# Deliberately retain existing case-insensitive source/cell/revision compatibility semantics.
for compatibility_marker in [
    "GroupBy(x => x.CellKey, StringComparer.OrdinalIgnoreCase)",
    "sourceList.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)",
    "StringComparison.OrdinalIgnoreCase",
]:
    if compatibility_marker not in text:
        raise SystemExit(f"unexpected live workbook compatibility change: {compatibility_marker}")

print("live workbook case-sensitive binding identity guard: PASS")