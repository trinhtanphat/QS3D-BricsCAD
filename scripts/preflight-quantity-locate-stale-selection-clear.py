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
    "public static void ClearSelection(Document document)",
)
clear_selection = block(
    service,
    "public static void ClearSelection(Document document)",
    "public static ISet<string> GetLiveHandles",
)
resolve = block(
    service,
    "public static IReadOnlyList<ObjectId> Resolve",
    "public static string? NormalizeHexHandle",
)
summary_locate = block(summary, "private void LocateCurrent()", "private QuantityReportRow ResolveCurrentRow")
insight_locate = block(insight, "private void LocateSelected()", "private QuantityReportRow ResolveCurrentRow")

if "=> SelectIfAny(document, handles);" not in select:
    errors.append("Select must preserve its normal locate no-op-on-empty behavior through SelectIfAny")

for needle in (
    "var ids = Resolve(document, handles);",
    "if (ids.Count == 0) return 0;",
    "document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());",
    "return ids.Count;",
):
    if needle not in select_if_any:
        errors.append("SelectIfAny preserve-on-empty contract changed: " + needle)

if select_if_any:
    zero_pos = select_if_any.find("if (ids.Count == 0) return 0;")
    replace_pos = select_if_any.find("document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());")
    if not (0 <= zero_pos < replace_pos):
        errors.append("SelectIfAny must keep zero-count return before implied-selection replacement")

for needle in (
    "if (document == null) throw new ArgumentNullException(nameof(document));",
    "document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());",
):
    if needle not in clear_selection:
        errors.append("ClearSelection explicit replacement contract changed: " + needle)

for needle in (
    "var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "!seen.Add(normalized)",
    "entity != null && !entity.IsErased",
):
    if needle not in resolve:
        errors.append("multi-object/stale-handle Resolve contract changed: " + needle)

summary_select = "Cad.CadHandleService.Select(_document, liveHandles)"
summary_clear = "Cad.CadHandleService.ClearSelection(_document)"
for needle in (
    summary_select,
    "if (selectedCount <= 0)",
    summary_clear,
    "if (_locate != null)",
    '_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in summary_locate:
        errors.append("QuantitySummary locate missing contract: " + needle)

if summary_locate:
    if summary_locate.count(summary_select) != 1:
        errors.append("QuantitySummary must use normal Select only for the positive-candidate locate attempt")
    if summary_locate.count(summary_clear) < 2:
        errors.append("QuantitySummary must explicitly clear both zero-resolved and zero-candidate stale selection paths")
    select_pos = summary_locate.find(summary_select)
    zero_resolved_guard_pos = summary_locate.find("if (selectedCount <= 0)", select_pos)
    zero_resolved_clear_pos = summary_locate.find(summary_clear, zero_resolved_guard_pos)
    zoom_pos = summary_locate.find('_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);')
    zero_candidate_clear_pos = summary_locate.find(summary_clear, zoom_pos)
    fallback_pos = summary_locate.find("if (_locate != null)")
    if not (0 <= select_pos < zero_resolved_guard_pos < zero_resolved_clear_pos < zoom_pos < zero_candidate_clear_pos < fallback_pos):
        errors.append("QuantitySummary must clear stale selection before zero-resolved return and before zero-candidate fallback")

insight_select = "Cad.CadHandleService.Select(document, handles)"
insight_clear = "Cad.CadHandleService.ClearSelection(document)"
for needle in (
    "if (handles.Count == 0)",
    insight_clear,
    insight_select,
    "if (count > 0)",
    'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in insight_locate:
        errors.append("QuantityInsight locate missing contract: " + needle)

if insight_locate:
    if insight_locate.count(insight_select) != 1:
        errors.append("QuantityInsight must use normal Select only for the positive-candidate locate attempt")
    if insight_locate.count(insight_clear) < 2:
        errors.append("QuantityInsight must explicitly clear both zero-candidate and zero-resolved stale selection paths")
    zero_candidate_guard_pos = insight_locate.find("if (handles.Count == 0)")
    zero_candidate_clear_pos = insight_locate.find(insight_clear, zero_candidate_guard_pos)
    zero_candidate_status_pos = insight_locate.find(
        '_viewModel.Status = "Dòng này chưa có semantic handle hiện hành để định vị trong CAD.";',
        zero_candidate_guard_pos,
    )
    normal_select_pos = insight_locate.find(insight_select, zero_candidate_status_pos)
    positive_guard_pos = insight_locate.find("if (count > 0)", normal_select_pos)
    zoom_pos = insight_locate.find(
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
        positive_guard_pos,
    )
    zero_resolved_clear_pos = insight_locate.find(insight_clear, zoom_pos)
    if not (
        0 <= zero_candidate_guard_pos < zero_candidate_clear_pos < zero_candidate_status_pos < normal_select_pos < positive_guard_pos < zoom_pos < zero_resolved_clear_pos
    ):
        errors.append("QuantityInsight must clear zero-candidate selection before return, zoom only after positive selection, and clear zero-resolved selection afterwards")

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
    "PASS: normal CAD Select preserves implied selection when no handle resolves, explicit ClearSelection removes stale PICKFIRST on failed quantity locate paths, "
    "and both quantity surfaces keep multi-object resolution with zoom gated on a positive live selection."
)
