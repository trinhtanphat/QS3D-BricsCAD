#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkRateAssignmentUnmatchedPreviewSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "internal bool TryGetLine(",
    "if (!portfolio.TryGetLine(selectedLineId, out var line))",
    "unmatched.Add(selectedLineId);",
    "continue;",
    "if (!preview.CanCommit)",
]
required_smoke = [
    "MixedKnownUnknownSelectionReturnsReviewablePreview();",
    "AllUnknownSelectionRemainsReviewable();",
    "KnownLineLookupRemainsCaseInsensitive();",
    "UnmatchedPreviewCannotCommitOrPublishAudit();",
    "preview.UnitDistribution.Count == 0",
    "audit.Events.Count == 0",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]

if missing:
    print("FAIL bulk rate assignment unmatched preview source contract")
    for token in missing:
        print(" - missing:", token)
    raise SystemExit(1)

get_line_index = source.index("var selectedLineId = request.LineIds[i];")
try_index = source.index("if (!portfolio.TryGetLine(selectedLineId, out var line))", get_line_index)
unmatched_index = source.index("unmatched.Add(selectedLineId);", try_index)
source_line_index = source.index("sourceLines.Add(line);", try_index)
if not get_line_index < try_index < unmatched_index < source_line_index:
    print("FAIL unknown selected-line admission must precede source-line publication")
    raise SystemExit(1)

print("PASS bulk rate assignment unmatched preview source contract")
