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

summary_select = "Cad.CadHandleService.Select(_document, liveHandles)"
for needle in (
    summary_select,
    "if (selectedCount <= 0)",
    "if (_locate != null)",
    '_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in summary_locate:
        errors.append("QuantitySummary locate missing contract: " + needle)

if summary_locate:
    if summary_locate.count(summary_select) < 2:
        errors.append("QuantitySummary must replace selection both for live handles and for the zero-candidate fallback path")
    first_select_pos = summary_locate.find(summary_select)
    zero_live_guard_pos = summary_locate.find("if (selectedCount <= 0)")
    zoom_pos = summary_locate.find('_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);')
    zero_candidate_select_pos = summary_locate.find(summary_select, first_select_pos + len(summary_select))
    fallback_pos = summary_locate.find("if (_locate != null)")
    if not (0 <= first_select_pos < zero_live_guard_pos < zoom_pos < zero_candidate_select_pos < fallback_pos):
        errors.append("QuantitySummary must clear zero-candidate selection after the live-handle branch and before fallback callback")

insight_select = "Cad.CadHandleService.Select(document, handles)"
for needle in (
    "if (handles.Count == 0)",
    insight_select,
    "if (count > 0)",
    'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in insight_locate:
        errors.append("QuantityInsight locate missing contract: " + needle)

if insight_locate:
    if insight_locate.count(insight_select) < 2:
        errors.append("QuantityInsight must replace selection in both zero-candidate and normal locate paths")
    zero_candidate_guard_pos = insight_locate.find("if (handles.Count == 0)")
    zero_candidate_select_pos = insight_locate.find(insight_select, zero_candidate_guard_pos)
    zero_candidate_status_pos = insight_locate.find(
        '_viewModel.Status = "Dòng này chưa có semantic handle hiện hành để định vị trong CAD.";',
        zero_candidate_guard_pos,
    )
    normal_select_pos = insight_locate.find(insight_select, zero_candidate_select_pos + len(insight_select))
    positive_guard_pos = insight_locate.find("if (count > 0)", normal_select_pos)
    zoom_pos = insight_locate.find(
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
        normal_select_pos,
    )
    if not (
        0 <= zero_candidate_guard_pos < zero_candidate_select_pos < zero_candidate_status_pos < normal_select_pos < positive_guard_pos <= zoom_pos
    ):
        errors.append("QuantityInsight must clear zero-candidate selection before status/return and zoom only after normal positive selection")

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
    "PASS: explicit CAD Select replaces implied selection for zero-live and zero-candidate quantity targets, "
    "SelectIfAny preserves its no-op-on-empty contract, and both quantity locate paths keep multi-object/stale-handle "
    "resilience with zoom gated on a positive live selection."
)
