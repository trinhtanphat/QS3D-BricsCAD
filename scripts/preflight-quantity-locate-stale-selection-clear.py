#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs"
SUMMARY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
INSIGHT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
errors = []

for path in (SERVICE, SUMMARY, INSIGHT):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

service = SERVICE.read_text(encoding="utf-8")
summary = SUMMARY.read_text(encoding="utf-8")
insight = INSIGHT.read_text(encoding="utf-8")


def block(source: str, start_token: str, end_token: str) -> str:
    start = source.find(start_token)
    end = source.find(end_token, start + len(start_token)) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate block: " + start_token)
        return ""
    return source[start:end]


select = block(
    service,
    "public static int Select(Document document, IEnumerable<string> handles)",
    "public static int SelectIfAny(Document document, IEnumerable<string> handles)",
)
select_if_any = block(
    service,
    "public static int SelectIfAny(Document document, IEnumerable<string> handles)",
    "public static ISet<string> GetLiveHandles",
)
resolve = block(
    service,
    "public static IReadOnlyList<ObjectId> Resolve",
    "public static string? NormalizeHexHandle",
)
summary_locate = block(summary, "private void LocateCurrent()", "private QuantityReportRow ResolveCurrentRow")
insight_locate = block(insight, "private void LocateSelected()", "private QuantityReportRow ResolveCurrentRow")

for needle in (
    "var ids = Resolve(document, handles);",
    "document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());",
    "return ids.Count;",
):
    if needle not in select:
        errors.append("explicit Select missing replacement contract: " + needle)

if "if (ids.Count == 0) return 0;" in select:
    errors.append("explicit Select must not return before replacing an empty implied selection")

if select:
    resolve_pos = select.find("var ids = Resolve(document, handles);")
    replace_pos = select.find("document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());")
    return_pos = select.find("return ids.Count;")
    if not (0 <= resolve_pos < replace_pos < return_pos):
        errors.append("explicit Select must resolve, replace implied selection, then return count")

for needle in (
    "var ids = Resolve(document, handles);",
    "if (ids.Count == 0) return 0;",
    "document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());",
):
    if needle not in select_if_any:
        errors.append("SelectIfAny preserve-on-empty contract changed: " + needle)

if select_if_any:
    zero_pos = select_if_any.find("if (ids.Count == 0) return 0;")
    replace_pos = select_if_any.find("document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());")
    if not (0 <= zero_pos < replace_pos):
        errors.append("SelectIfAny must keep its zero-count return before implied-selection replacement")

for needle in (
    "var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "!seen.Add(normalized)",
    "entity != null && !entity.IsErased",
):
    if needle not in resolve:
        errors.append("multi-object/stale-handle Resolve contract changed: " + needle)

for needle in (
    "Cad.CadHandleService.Select(_document, liveHandles)",
    "if (selectedCount <= 0)",
    '_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in summary_locate:
        errors.append("QuantitySummary locate missing contract: " + needle)

if summary_locate:
    select_pos = summary_locate.find("Cad.CadHandleService.Select(_document, liveHandles)")
    zero_pos = summary_locate.find("if (selectedCount <= 0)")
    zoom_pos = summary_locate.find('_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);')
    if not (0 <= select_pos < zero_pos < zoom_pos):
        errors.append("QuantitySummary must replace selection before zero guard and zoom only after positive selection")

for needle in (
    "Cad.CadHandleService.Select(document, handles)",
    "if (count > 0)",
    'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in insight_locate:
        errors.append("QuantityInsight locate missing contract: " + needle)

if insight_locate:
    select_pos = insight_locate.find("Cad.CadHandleService.Select(document, handles)")
    guard_pos = insight_locate.find("if (count > 0)")
    zoom_pos = insight_locate.find('document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);')
    if not (0 <= select_pos < guard_pos <= zoom_pos):
        errors.append("QuantityInsight must replace selection before positive-count zoom guard")

for locate_name, source in (("QuantitySummary", summary_locate), ("QuantityInsight", insight_locate)):
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        ".Touch()",
        "ProjectContextCoordinator.Save",
    ):
        if forbidden in source:
            errors.append(locate_name + " locate must remain non-creating/read-only: " + forbidden)

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

print(
    "PASS: explicit CAD Select now replaces implied selection even when no handle survives, "
    "SelectIfAny preserves its no-op-on-empty contract, and both quantity locate paths keep "
    "multi-object/stale-handle resilience with zoom gated on a positive live selection."
)
