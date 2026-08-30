from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "ReportingRowProvenance.cs"
text = SOURCE.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)


require(
    "private const int MaxSourceHandleEntries = 10000;" in text,
    "ReportingRowProvenance must retain the explicit 10000-entry provenance ceiling.",
)
require(
    "RequireTargetWithinBound(target);" in text,
    "existing target cardinality must be admitted before target snapshot allocation.",
)
require(
    "if (target.Count > MaxSourceHandleEntries)" in text,
    "already-oversize target must fail closed.",
)

identity = text.index("var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);")
duplicate = text.index("if (existingIdentities.Contains(identity) || !stagedIdentities.Add(identity))", identity)
cumulative = text.index("if (targetSnapshot.Length + staged.Count >= MaxSourceHandleEntries)", duplicate)
stage = text.index("staged.Add(handle);", cumulative)
publish = text.index("foreach (var handle in staged) target.Add(handle);", stage)

require(
    identity < duplicate < cumulative < stage < publish,
    "cumulative bound must execute after canonical/duplicate validation but before staging and publication.",
)
require(
    '"Report provenance SourceHandles cannot exceed " + MaxSourceHandleEntries + " published entries."' in text,
    "deterministic cumulative-bound diagnostic must remain present.",
)
require(
    "target.Add(handle);" not in text[:stage],
    "source handles must not be partially published before full traversal succeeds.",
)

print("PASS reporting row provenance cumulative bound")
