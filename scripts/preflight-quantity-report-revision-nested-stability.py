#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityReportRevisionReview.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportRevisionReviewSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/quantity-report-revision-nested-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL quantity-report revision nested stability: missing {label}: {token}")


def ordered(text: str, tokens: list[str], label: str) -> None:
    cursor = 0
    for token in tokens:
        index = text.find(token, cursor)
        if index < 0:
            raise SystemExit(f"FAIL quantity-report revision nested stability: {label} ordering missing {token}")
        cursor = index + len(token)


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    ordered(
        source,
        [
            "var first = CapturePass(project, identity);",
            "RequireStableProject(project, projectId, sourceChangeVersion);",
            "var second = CapturePass(project, identity);",
            "RequireStableProject(project, projectId, sourceChangeVersion);",
            "new RevisionService().Compare(first.SemanticRevision, second.SemanticRevision)",
            "RowsExactlyEqual(first.Rows, second.Rows)",
            "new QuantityReportRevisionSnapshot(projectId, identity, sourceChangeVersion, second.SemanticRevision, second.Rows)",
        ],
        "capture admission",
    )
    require(source, "private static CapturePassResult CapturePass", "complete-pass helper")
    require(source, "ProjectQuantityReportBuilder.Detail(project)", "authoritative BQ detail materialization")
    require(source, "string.Equals(CanonicalIdentity(project.ProjectId, \"project id\"), projectId, StringComparison.Ordinal)", "project identity rebound")
    require(source, "project.ChangeVersion != sourceChangeVersion", "project revision rebound")
    require(source, "private static bool RowExactlyEqual", "exact row comparator")
    require(source, "left.LengthM.Equals(right.LengthM)", "exact numeric row comparison")
    require(source, "Nullable.Equals(left.DensityKgM3, right.DensityKgM3)", "exact nullable numeric comparison")

    require(smoke, "NestedMutationDuringCaptureFailsClosed();", "registered hostile smoke call")
    require(smoke, "new MutatingDictionary", "caller-controlled nested mutation")
    require(smoke, "() => element.FamilyId = \"family-after\"", "mixed-generation family mutation")
    require(smoke, "Equal(beforeProjectRevision, project.ChangeVersion);", "parent revision premise")
    require(smoke, "Throws<InvalidOperationException>(() => new QuantityReportRevisionService().Capture(project, \"RACE\"));", "fail-closed regression")

    require(runbook, "two complete materialization passes", "two-pass contract documentation")
    require(runbook, "no retry loop", "no-retry contract documentation")
    require(runbook, "RevisionService.Compare", "semantic authority documentation")
    require(runbook, "exact row equality", "exact BQ equality documentation")

    print("PASS quantity report revision nested stability")
    return 0


if __name__ == "__main__":
    sys.exit(main())
