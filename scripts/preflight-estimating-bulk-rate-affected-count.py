from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs").read_text(encoding="utf-8")

start = source.index("public BulkRateAssignmentPreview PreviewBulkRateAssignment(")
end = source.index("public EstimatingPortfolio CommitBulkRateAssignment(", start)
method = source[start:end]

for token in [
    "var sourceLines = new List<EstimatingLine>(request.LineIds.Count);",
    "if (!portfolio.TryGetLine(selectedLineId, out var line))",
    "unmatched.Add(selectedLineId);",
    "sourceLines.Add(line);",
    "sourceLines.Count,",
]:
    if token not in method:
        raise SystemExit("Missing bulk preview affected-count contract: " + token)

unknown = method.index("if (!portfolio.TryGetLine(selectedLineId, out var line))")
unmatched = method.index("unmatched.Add(selectedLineId);", unknown)
resolved = method.index("sourceLines.Add(line);", unmatched)
affected = method.index("sourceLines.Count,", resolved)
if not (unknown < unmatched < resolved < affected):
    raise SystemExit("Bulk preview must resolve existing source lines before deriving affected count from the resolved snapshot.")

if "request.LineIds.Count,\n                replacements," in method:
    raise SystemExit("Bulk preview affected count must not use raw requested id cardinality.")

smoke = (root / "tests/QS3D.Core.SmokeTests/EstimatingBulkRateAffectedCountSmoke.cs").read_text(encoding="utf-8")
for token in [
    "MixedExistingAndUnknownSelectionCountsOnlyExistingLines",
    "AllExistingSelectionRetainsAffectedCountAndCommitReadiness",
    "preview.AffectedCount != 1",
    "preview.UnmatchedLineIds.Count != 1",
    "preview.AffectedCount != 2",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic bulk preview affected-count smoke contract: " + token)

print("Estimating bulk rate affected-count preflight passed.")
