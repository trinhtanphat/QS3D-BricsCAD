#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

before = "RequireStablePieceCount(inputPieces, pieceCount);\n                var sourcePiece = inputPieces[index];"
after = "var sourcePiece = inputPieces[index];\n                RequireStablePieceCount(inputPieces, pieceCount);\n                pieces.Add(SnapshotAndValidate(sourcePiece));"
if before not in source:
    raise SystemExit("ERROR: curtain fingerprint must revalidate Pieces Count immediately before indexed access")
if after not in source:
    raise SystemExit("ERROR: curtain fingerprint must revalidate Pieces Count immediately after indexed access and before snapshot acceptance")
if source.count("var sourcePiece = inputPieces[index];") != 1:
    raise SystemExit("ERROR: curtain fingerprint must read each admitted source index exactly once")

required_source = [
    "private static void RequireStablePieceCount",
    '"Curtain panel fingerprint Pieces Count must not be negative."',
    '"Curtain panel fingerprint exceeds " + MaxPieces + " pieces."',
    '"Curtain panel fingerprint Pieces Count changed while being validated."',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"ERROR: curtain fingerprint Count integrity source missing token: {token}")

required_smoke = [
    "[ModuleInitializer]",
    "TransientCountDriftFailsBeforeSecondIndexRead();",
    "StableInputPreservesFingerprintAndSingleIndexerReads();",
    "_count = 3;",
    "_count = 2;",
    'Equal(1, pieces.IndexReads',
    'Equal(3, pieces.CountReads',
    'Equal(2, stable.IndexReads',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"ERROR: curtain fingerprint Count smoke missing token: {token}")

print("PASS curtain panel fingerprint indexed Count integrity")
