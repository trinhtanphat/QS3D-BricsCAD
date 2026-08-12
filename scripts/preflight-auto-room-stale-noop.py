from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/AutoRoomStaleNoOpSmoke.cs").read_text(encoding="utf-8")

method = source.index("public static IReadOnlyList<ProjectElement> MarkStaleForSelection")
helper_filter = source.index(".Where(room => !HasCanonicalTopologyStaleMetadata(room))", method)
touch = source.index("project.Touch();", method)
assert helper_filter < touch, "canonical stale no-op filter must run before project.Touch()"

helper = source.index("private static bool HasCanonicalTopologyStaleMetadata")
assert 'string.Equals(state, BoundaryStateStale, StringComparison.Ordinal)' in source[helper:], "state must be canonical ordinal text"
assert 'string.Equals(reason, "TopologyChanged", StringComparison.Ordinal)' in source[helper:], "reason must be canonical ordinal text"
assert 'DateTime.TryParseExact(' in source[helper:], "stale timestamp must use strict round-trip parsing"
assert 'System.Globalization.DateTimeStyles.RoundtripKind' in source[helper:], "stale timestamp must preserve round-trip kind"
assert 'parsed.Kind != DateTimeKind.Utc' in source[helper:], "stale timestamp must be UTC"
assert 'string.Equals(staleUtc, parsed.ToString("O"), StringComparison.Ordinal)' in source[helper:], "stale timestamp must already be canonical O text"

for marker in (
    "FirstTransitionMutatesOnce();",
    "RepeatedCanonicalStaleIsNoOp();",
    "MalformedStaleMetadataIsRepaired();",
    "Equal(0, repeated.Count, \"repeated stale count\")",
    "Equal(beforeVersion, project.ChangeVersion, \"repeated project revision\")",
    "Equal(beforeStaleUtc, room.Properties[\"BoundaryStaleUtc\"], \"repeated stale timestamp\")",
):
    assert marker in smoke, f"missing smoke contract: {marker}"

print("Auto Room stale-marking no-op preflight OK")
