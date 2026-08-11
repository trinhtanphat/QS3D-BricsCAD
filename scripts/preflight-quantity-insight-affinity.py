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
        print("ERROR: locate must validate DWG -> project -> live row before resolving handles/selecting CAD objects.")
        return 1

    resolve_method = text.find("private QuantityReportRow ResolveCurrentRow")
    preview_pos = text.find("BuildPreviewRows(project, out _)", resolve_method)
    match_pos = text.find("SameElementIdentity(displayedIds, x)", preview_pos)
    same_row_pos = text.find("if (!SameRow(displayedRow, matches[0]))", match_pos)
    if min(resolve_method, preview_pos, match_pos, same_row_pos) < 0 or not (
        resolve_method < preview_pos < match_pos < same_row_pos
    ):
        print("ERROR: live row revalidation ordering is incomplete.")
        return 1

    forbidden = [
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        "SourceHandleResolver.Resolve(project, item.ElementIds)",
        "var currentRows = ProjectQuantityReportBuilder.Group(project);",
    ]
    found = [token for token in forbidden if token in text]
    if found:
        print("ERROR: Quantity Insight locate must remain read-only and must not resolve stale item IDs/direct live rows:")
        for token in found:
            print(" - forbidden:", token)
        return 1

    print("PASS: Quantity Insight fails closed across DWG/project changes and revalidates the detached-preview live grouped row before native CAD selection/zoom.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
