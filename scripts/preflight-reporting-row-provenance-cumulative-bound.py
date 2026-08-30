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

input_bound = text.index("if (index >= MaxSourceHandleEntries)")
current = text.index("var raw = enumerator.Current;", input_bound)
identity = text.index("var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);", current)
duplicate = text.index("if (existingIdentities.Contains(identity) || !stagedIdentities.Add(identity))", identity)
stage = text.index("staged.Add(handle);", duplicate)
known_count_completion = text.index("if (knownCount.HasValue && index != knownCount.Value)", stage)
cumulative = text.index("if (targetSnapshot.Length > MaxSourceHandleEntries - staged.Count)", known_count_completion)
publish = text.index("foreach (var handle in staged) target.Add(handle);", cumulative)

require(
    input_bound < current < identity < duplicate < stage < known_count_completion < cumulative < publish,
    "per-input bound and source validation must finish before cumulative published-bound rejection and publication.",
)
require(
    '"Report provenance SourceHandles cannot exceed " + MaxSourceHandleEntries + " published entries."' in text,
    "deterministic cumulative-bound diagnostic must remain present.",
)
require(
    "target.Add(handle);" not in text[:publish],
    "source handles must not be partially published before full traversal and cumulative validation succeed.",
)

print("PASS reporting row provenance cumulative bound")
