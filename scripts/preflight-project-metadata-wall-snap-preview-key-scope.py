#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "WallJunctionSnapPreviewRevisionSmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "project-metadata-wall-snap-preview-key-scope.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit(
            "Project metadata Wall Snap preview key-scope preflight missing file: "
            + str(path.relative_to(ROOT))
        )

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    '"WallJunctionSnapPreviewPlanHash"',
    '"WallJunctionSnapPreviewSourceFingerprint"',
    '"WallJunctionSnapPreviewCount"',
    '"WallJunctionSnapPreviewUtc"',
    '"WallJunctionSnapPreviewProjectId"',
    '"WallJunctionSnapPreviewChangeVersion"',
    "IsWallJunctionSnapPreviewWorkflowKey",
    "return !IsWallJunctionSnapPreviewWorkflowKey(key);",
)
missing_source = [token for token in required_source if token not in source]
if missing_source:
    raise SystemExit(
        "Project metadata Wall Snap exact-key contract missing source token(s): "
        + repr(missing_source)
    )

for forbidden in (
    "WallJunctionSnapPreviewMetadataPrefix",
    "StartsWith(WallJunctionSnapPreview",
):
    if forbidden in source:
        raise SystemExit(
            "Project metadata Wall Snap dirty-state exemption must not use a broad preview prefix: "
            + forbidden
        )

required_smoke = (
    "PreviewPublicationUsesTwoBoundedRevisionsAndKeepsApprovalFresh();",
    "PreviewCleanupUsesOneBoundedRevisionForEmptyAndAppliedPlans();",
    "PreviewPrefixLookalikeStillMarksSemanticStateDirty();",
    'const string key = "WallJunctionSnapPreviewCustomerData";',
    "public preview-prefix lookalike set must advance ChangeVersion.",
    "public preview-prefix lookalike update must advance ChangeVersion.",
    "public preview-prefix lookalike remove must advance ChangeVersion.",
    "public preview-prefix lookalike Add must advance ChangeVersion.",
    "clearing public preview-prefix lookalike metadata must advance ChangeVersion.",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit(
        "Project metadata Wall Snap preview-key smoke contract missing token(s): "
        + repr(missing_smoke)
    )

if "Lane-Key: `issue-4546`" not in runbook:
    raise SystemExit("Project metadata Wall Snap preview key-scope runbook must pin Lane-Key issue-4546.")

print("PASS Project metadata Wall Snap preview exact-key dirty-state scope")
