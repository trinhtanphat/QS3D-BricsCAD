#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "private Document? _boundDocument;",
        "private string _boundProjectId = string.Empty;",
        "private string _boundDrawingFingerprint = string.Empty;",
        "private Dictionary<QuantityInsightItemViewModel, QuantityReportRow> _rowSnapshots",
        "_boundDocument = document;",
        "_boundProjectId = project.ProjectId;",
        "_boundDrawingFingerprint = project.DrawingFingerprint ?? string.Empty;",
        "!ReferenceEquals(document, _boundDocument)",
        "if (!SameProjectIdentity(project))",
        "var currentRow = ResolveCurrentRow(item, project);",
        "SourceHandleResolver.Resolve(project, currentRow.ElementIds)",
        "private QuantityReportRow ResolveCurrentRow(QuantityInsightItemViewModel item, ProjectState project)",
        "var currentRows = BuildPreviewRows(project, out _);",
        "private static IReadOnlyList<QuantityReportRow> BuildPreviewRows(ProjectState project, out int regenerated)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(previewProject)",
        "ProjectQuantityReportBuilder.Group(previewProject)",
        "SameElementIdentity(displayedIds, x)",
        "if (matches.Count != 1)",
        "if (!SameRow(displayedRow, matches[0]))",
        "CanonicalIds(left.SourceHandles).SequenceEqual(CanonicalIds(right.SourceHandles), StringComparer.OrdinalIgnoreCase)",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        print("ERROR: Quantity Insight document/row affinity contract is incomplete:")
        for token in missing:
            print(" - missing:", token)
        return 1

    locate_pos = text.find("private void LocateSelected()")
    document_pos = text.find("!ReferenceEquals(document, _boundDocument)", locate_pos)
    project_pos = text.find("if (!SameProjectIdentity(project))", document_pos)
    resolve_pos = text.find("var currentRow = ResolveCurrentRow(item, project);", project_pos)
    handles_pos = text.find("SourceHandleResolver.Resolve(project, currentRow.ElementIds)", resolve_pos)
    select_pos = text.find("Cad.CadHandleService.Select(document, handles)", handles_pos)
    if min(locate_pos, document_pos, project_pos, resolve_pos, handles_pos, select_pos) < 0 or not (
        locate_pos < document_pos < project_pos < resolve_pos < handles_pos < select_pos
    ):
        print("ERROR: locate must validate DWG -> project -> current preview row before resolving handles/selecting CAD objects.")
        return 1

    preview_method = text.find("private static IReadOnlyList<QuantityReportRow> BuildPreviewRows")
    detached_pos = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)", preview_method)
    regen_pos = text.find("RegenerateDirty(previewProject)", detached_pos)
    grouped_pos = text.find("ProjectQuantityReportBuilder.Group(previewProject)", regen_pos)
    if min(preview_method, detached_pos, regen_pos, grouped_pos) < 0 or not (
        preview_method < detached_pos < regen_pos < grouped_pos
    ):
        print("ERROR: Quantity Insight preview rows must regenerate detached project state before grouping quantities.")
        return 1

    resolve_method = text.find("private QuantityReportRow ResolveCurrentRow")
    preview_pos = text.find("BuildPreviewRows(project, out _)", resolve_method)
    match_pos = text.find("SameElementIdentity(displayedIds, x)", preview_pos)
    same_row_pos = text.find("if (!SameRow(displayedRow, matches[0]))", match_pos)
    if min(resolve_method, preview_pos, match_pos, same_row_pos) < 0 or not (
        resolve_method < preview_pos < match_pos < same_row_pos
    ):
        print("ERROR: current preview-row revalidation ordering is incomplete.")
        return 1

    forbidden = [
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        "SourceHandleResolver.Resolve(project, item.ElementIds)",
        "ProjectQuantityReportBuilder.Group(project)",
    ]
    found = [token for token in forbidden if token in text]
    if found:
        print("ERROR: Quantity Insight must remain detached/read-only and must not resolve stale item IDs/direct live rows:")
        for token in found:
            print(" - forbidden:", token)
        return 1

    print("PASS: Quantity Insight preview-regenerates detached state, fails closed across DWG/project changes, and revalidates the current preview row before native CAD selection/zoom.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
