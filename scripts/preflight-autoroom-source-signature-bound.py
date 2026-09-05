from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AutoRoomSourceSignatureBoundSmoke.cs"
HANDLE_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AutoRoomHandleIdentitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
handle_smoke = HANDLE_SMOKE.read_text(encoding="utf-8")

required_source = [
    "private const int MaxSourceHandleInputCount = 5000;",
    "var canonical = CanonicalizeSourceHandle(raw);",
    "private static string CanonicalizeSourceHandle(string raw)",
    "var canonical = GeneratedHandleIdentity.Normalize(raw);",
    "if (!string.Equals(canonical, trimmed, StringComparison.Ordinal)) return canonical;",
    "canonical.Any(ch => !char.IsLetterOrDigit(ch))",
    "? canonical.ToUpperInvariant()",
    "private static IReadOnlyList<string> ParseSourceHandleText(string? raw)",
    "for (var index = 0; index <= source.Length; index++)",
    "source[index] != ';'",
    "var tokenLength = index - tokenStart;",
    "if (tokenLength == 0)",
    "if (handles.Count >= MaxSourceHandleInputCount)",
    "handles.Add(source.Substring(tokenStart, tokenLength));",
    "private static string NormalizeSourceHandleText(string? raw)",
    "return NormalizeSourceHandleText(signature);",
    "return NormalizeSourceHandleText(raw);",
    "var normalized = NormalizeSourceHandleText(signature);",
    "var handles = ParseSourceHandleText(SourceSignature(room));",
    "var normalizedSourceSignature = NormalizeSourceHandleText(sourceSignature);",
    "room.SetProperty(BoundarySourceSignatureKey, normalizedSourceSignature);",
]
for token in required_source:
    if token not in source:
        raise SystemExit("FAIL: Auto Room bounded source-signature contract missing: " + token)

if ".Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)" in source:
    raise SystemExit("FAIL: Auto Room source-signature lifecycle regressed to eager semicolon Split before the 5,000-input envelope")

mark_start = source.find("public static void MarkActive(ProjectElement room, string sourceSignature)")
mark_end = source.find("public static int SyncFamilyDefaults", mark_start)
if mark_start < 0 or mark_end < 0:
    raise SystemExit("FAIL: could not isolate AutoRoomLifecycle.MarkActive")
mark = source[mark_start:mark_end]
normalize_at = mark.find("var normalizedSourceSignature = NormalizeSourceHandleText(sourceSignature);")
state_at = mark.find("room.SetProperty(BoundaryStateKey, BoundaryStateActive);")
if normalize_at < 0 or state_at < 0 or normalize_at > state_at:
    raise SystemExit("FAIL: MarkActive must validate/bound the source signature before lifecycle mutation")

required_smoke = [
    "ExactBoundaryRemainsAccepted",
    "PersistedSignatureOverBoundaryFailsClosed",
    "PersistedHandleFallbackOverBoundaryFailsClosed",
    "MarkActiveOverBoundaryFailsClosedBeforeMutation",
    "RemoveEmptyEntriesSemanticsRemainStable",
    "WhitespaceOnlyTokensStillConsumeTheInputEnvelope",
    "OpaqueCaseEquivalentPermutationsCanonicalizeDeterministically",
    "MaxHandles = 5000",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("FAIL: Auto Room source-signature regression missing: " + token)

for token in (
    "LegacyMalformedAndZeroTokensRemainStable",
    'Equal("0;0x;xyz", normalized);',
):
    if token not in handle_smoke:
        raise SystemExit("FAIL: Auto Room malformed/zero legacy identity regression missing: " + token)

print("PASS Auto Room bounded source-signature parsing and opaque casing compatibility")
