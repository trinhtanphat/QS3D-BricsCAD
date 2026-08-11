#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "using QS3D.Core.Persistence;",
        "private static IReadOnlyList<QuantityReportRow> BuildPreviewRows(ProjectState project, out int regenerated)",
        "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);",
        "regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(previewProject);",
        "return ProjectQuantityReportBuilder.Group(previewProject);",
        "var rows = BuildPreviewRows(project, out var regenerated);",
        "var currentRows = BuildPreviewRows(project, out _);",
        "preview-regenerate ",
        "snapshot tách rời",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        print("ERROR: Quantity Insight detached preview-regeneration contract is incomplete:")
        for token in missing:
            print(" - missing:", token)
        return 1

    helper_pos = text.find("private static IReadOnlyList<QuantityReportRow> BuildPreviewRows")
    copy_pos = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)", helper_pos)
    regen_pos = text.find("RegenerateDirty(previewProject)", copy_pos)
    group_pos = text.find("ProjectQuantityReportBuilder.Group(previewProject)", regen_pos)
    if min(helper_pos, copy_pos, regen_pos, group_pos) < 0 or not (
        helper_pos < copy_pos < regen_pos < group_pos
    ):
        print("ERROR: preview rows must be built in detached-copy -> regenerate -> grouped-report order.")
        return 1

    refresh_pos = text.find("public void RefreshQuantityInsights()")
    refresh_preview_pos = text.find("BuildPreviewRows(project, out var regenerated)", refresh_pos)
    totals_pos = text.find("QuantityReportTotals.FromRows(rows)", refresh_preview_pos)
    if min(refresh_pos, refresh_preview_pos, totals_pos) < 0 or not (
        refresh_pos < refresh_preview_pos < totals_pos
    ):
        print("ERROR: panel refresh must build detached regenerated rows before totals/tree materialization.")
        return 1

    resolve_pos = text.find("private QuantityReportRow ResolveCurrentRow")
    current_preview_pos = text.find("BuildPreviewRows(project, out _)", resolve_pos)
    match_pos = text.find("SameElementIdentity(displayedIds, x)", current_preview_pos)
    same_row_pos = text.find("if (!SameRow(displayedRow, matches[0]))", match_pos)
    if min(resolve_pos, current_preview_pos, match_pos, same_row_pos) < 0 or not (
        resolve_pos < current_preview_pos < match_pos < same_row_pos
    ):
        print("ERROR: locate revalidation must compare against the same detached regenerated read model.")
        return 1

    forbidden = [
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        "RegenerateDirty(project)",
        "ProjectQuantityReportBuilder.Group(project)",
    ]
    found = [token for token in forbidden if token in text]
    if found:
        print("ERROR: Quantity Insight preview must not create/mutate or regenerate/group the live canonical project directly:")
        for token in found:
            print(" - forbidden:", token)
        return 1

    print("PASS: Quantity Insight totals/tree and stale-row revalidation use detached regenerated project snapshots without mutating the live project.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
