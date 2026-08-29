#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityReportRevisionReview.cs"
BUILDER = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
REVISION = ROOT / "src/QS3D.Core/Revisions/RevisionService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportRevisionReviewSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/QUANTITY-REPORT-REVISION-REVIEW.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing quantity report revision review file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
builder = read(BUILDER)
revision = read(REVISION)
smoke = read(SMOKE)
registration = read(REGISTRATION)
doc = read(DOC)

for token in (
    "public sealed class QuantityReportRevisionService",
    "ProjectQuantityReportBuilder.Detail(project)",
    "new RevisionService().Capture(project, identity)",
    "new RevisionService().Compare(before.SemanticRevision, after.SemanticRevision)",
    "QuantityReportRevisionChangeKind.Added",
    "QuantityReportRevisionChangeKind.Removed",
    "QuantityReportRevisionChangeKind.Changed",
    'CanonicalIdentity(row.ElementIds[0], "quantity report stable element key")',
    "RevisionMath.Subtract(after, before",
    "Quantity report revision snapshots belong to different projects.",
    "Quantity report revision snapshots must have distinct snapshot ids.",
    "project.ChangeVersion != sourceChangeVersion",
):
    if token not in source:
        errors.append("quantity report revision source missing contract token: " + token)

for forbidden in (
    "Bricscad.",
    "Teigha.",
    "Autodesk.",
    "project.Touch()",
    "SetQuantity(",
    "RegenerateDirty(",
    "element.Quantities",
):
    if forbidden in source:
        errors.append("quantity report revision review must remain CAD-independent/read-only and must not become a second quantity engine: " + forbidden)

# Revision review must continue to reuse the authoritative detail builder. Pin the
# builder's compensated aggregation contract rather than an implementation-specific
# pairwise addition primitive so commercial precision fixes cannot be reverted by
# this cross-feature guard.
for token in (
    "public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project)",
    "var accumulators = new Dictionary<string, QuantityReportAggregateState>",
    "private sealed class QuantityReportAggregateState",
    "private sealed class CompensatedValue",
    'row.GrossConcreteM3 = aggregate.GrossConcreteM3.Value("GrossConcreteM3")',
    "lost a non-zero compensation at floating-point precision",
):
    if token not in builder:
        errors.append("authoritative ProjectQuantityReportBuilder compensated contract is missing: " + token)

for forbidden in (
    "row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3",
    "row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3",
    "row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2",
    "row.LengthM = QuantityReportMath.Add(row.LengthM",
):
    if forbidden in builder:
        errors.append("authoritative ProjectQuantityReportBuilder regressed to pairwise grouped accumulation: " + forbidden)

for token in (
    "public RevisionSnapshot Capture(ProjectState project, string revisionId)",
    "public IReadOnlyList<RevisionDelta> Compare(RevisionSnapshot before, RevisionSnapshot after)",
):
    if token not in revision:
        errors.append("RevisionService authority is missing: " + token)

for token in (
    "AddedRemovedChangedRowsUseStableElementKeys",
    "CaptureAndCompareDoNotMutateLiveProjects",
    "ProjectAndSnapshotIdentityFailClosed",
    "NonFiniteAndInvalidMagnitudeFailClosed",
    "double.MaxValue",
    "double.NaN",
):
    if token not in smoke:
        errors.append("quantity report revision smoke missing regression token: " + token)

if "QuantityReportRevisionReviewSmoke.Run();" not in registration:
    errors.append("quantity report revision smoke is not registered")

for token in (
    "ProjectQuantityReportBuilder.Detail",
    "RevisionService",
    "Added / Removed / Changed",
    "stable semantic Element ID",
    "finite/overflow",
    "does not mutate",
    "in-memory",
):
    if token not in doc:
        errors.append("quantity report revision documentation missing boundary token: " + token)

print("QS3D quantity report revision review preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: quantity report revision review reuses the authoritative compensated BQ detail builder and RevisionService, uses stable semantic keys, rejects identity/finite/overflow hazards, and remains CAD-independent/read-only.")
