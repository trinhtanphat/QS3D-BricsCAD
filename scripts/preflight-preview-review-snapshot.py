#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Review" / "PreviewReviewSnapshot.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "PreviewReviewSnapshotSmoke.cs"


def require(text, token, label):
    if token not in text:
        print(f"ERROR: missing {label}: {token}")
        return False
    return True


def main():
    if not SOURCE.exists() or not SMOKE.exists():
        print("ERROR: preview review snapshot source/smoke file is missing.")
        return 1
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    ok = True
    for token, label in [
        ("QS3D.PreviewReviewSnapshot", "versioned format"),
        ("PreviewReviewKind.QuantityRule", "quantity preview support"),
        ("PreviewReviewKind.Regeneration", "regeneration preview support"),
        ("SHA256.Create()", "content fingerprint"),
        ("AtomicFileCommit.ReplaceWithBackup", "atomic publication"),
        ("DtdProcessing = DtdProcessing.Prohibit", "hardened XML load"),
        ("IsHandleField", "CAD handle redaction"),
        ("fingerprint or invariants are invalid", "tamper guard"),
        ("SourceChangeVersion", "preview version binding"),
    ]:
        ok = require(source, token, label) and ok
    for token, label in [
        ("QuantityReviewIsImmutableAndRoundTrips", "quantity round-trip smoke"),
        ("RegenerationReviewKeepsSubsetScope", "regeneration scope smoke"),
        ("TamperedReviewFailsClosed", "fingerprint tamper smoke"),
        ("HandleFieldInjectionFailsClosed", "handle injection smoke"),
    ]:
        ok = require(smoke, token, label) and ok
    lowered = source.lower()
    if "bricscad" in lowered or "teigha" in lowered:
        print("ERROR: preview review snapshot contract must remain Core-only and CAD-runtime independent.")
        ok = False
    if not ok:
        return 1
    print("PASS: preview review snapshots are versioned, fingerprinted, handle-safe, atomic, and Core-only.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
