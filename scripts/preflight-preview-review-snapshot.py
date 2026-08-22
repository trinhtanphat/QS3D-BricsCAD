#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Review" / "PreviewReviewSnapshot.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "PreviewReviewSnapshotSmoke.cs"
KIND_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "PreviewReviewKindParsingSmoke.cs"
PORTABILITY = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeElementPropertyPolicy.cs"


def require(text, token, label):
    if token not in text:
        print(f"ERROR: missing {label}: {token}")
        return False
    return True


def main():
    if not SOURCE.exists() or not SMOKE.exists() or not KIND_SMOKE.exists() or not PORTABILITY.exists():
        print("ERROR: preview review snapshot source/smoke/portability file is missing.")
        return 1
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    kind_smoke = KIND_SMOKE.read_text(encoding="utf-8")
    portability = PORTABILITY.read_text(encoding="utf-8")
    ok = True
    for token, label in [
        ("QS3D.PreviewReviewSnapshot", "versioned format"),
        ("PreviewReviewKind.QuantityRule", "quantity preview support"),
        ("PreviewReviewKind.Regeneration", "regeneration preview support"),
        ("Enum.IsDefined(typeof(PreviewReviewKind), snapshot.Kind)", "snapshot enum invariant"),
        ("Enum.IsDefined(typeof(PreviewReviewKind), kind)", "loader enum definition guard"),
        ("string.Equals(kindText, kind.ToString(), StringComparison.Ordinal)", "canonical symbolic enum guard"),
        ("SHA256.Create()", "content fingerprint"),
        ("AtomicFileCommit.ReplaceWithBackup", "atomic publication"),
        ("DtdProcessing = DtdProcessing.Prohibit", "hardened XML load"),
        ("private const string PropertyFieldPrefix = \"Property:\"", "property field prefix"),
        ("IsHandleField", "explicit CAD handle redaction"),
        ("IsPortableReviewField(fieldName)", "regeneration creation portability filter"),
        ("IsPortableReviewField(entry.Field)", "snapshot invariant portability filter"),
        ("PreviewReviewSnapshotService.IsPortableReviewField(field)", "loader portability filter"),
        ("ProjectInterchangeElementPropertyPolicy.IsPortable", "shared element property portability policy"),
        ("fingerprint or invariants are invalid", "tamper guard"),
        ("SourceChangeVersion", "preview version binding"),
    ]:
        ok = require(source, token, label) and ok
    for token, label in [
        ("GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)", "generated owner-slot portability guard"),
        ('normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)', "generated property portability guard"),
        ('normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)', "QS3D generated metadata portability guard"),
        ('normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)', "physical opening portability guard"),
        ('normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0', "generic handle-bearing property guard"),
    ]:
        ok = require(portability, token, label) and ok
    for token, label in [
        ("QuantityReviewIsImmutableAndRoundTrips", "quantity round-trip smoke"),
        ("RegenerationReviewKeepsSubsetScope", "regeneration scope smoke"),
        ("TamperedReviewFailsClosed", "fingerprint tamper smoke"),
        ("HandleFieldInjectionFailsClosed", "handle injection smoke"),
        ("NonPortableGeneratedFieldInjectionFailsClosed", "generated native metadata injection smoke"),
        ("Property:QS3D.GeneratedSolid.StaleSnapshot", "generated stale snapshot fixture"),
        ("ThrowsInvalidDataContaining", "parse-boundary error assertion"),
        ("forbidden drawing-local/native field", "portable review rejection message"),
    ]:
        ok = require(smoke, token, label) and ok
    for token, label in [
        ("NumericKindsFailAtParseBoundary", "numeric kind parse-boundary smoke"),
        ("AssertKindRejectedAtParseBoundary", "strict kind rejection helper"),
        ("Invalid preview review kind.", "strict parse failure contract"),
    ]:
        ok = require(kind_smoke, token, label) and ok
    lowered = source.lower()
    if "bricscad" in lowered or "teigha" in lowered:
        print("ERROR: preview review snapshot contract must remain Core-only and CAD-runtime independent.")
        ok = False
    if not ok:
        return 1
    print("PASS: preview review snapshots are versioned, fingerprinted, canonical-kind guarded, shared-portability/native-reference safe, atomic, and Core-only.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
