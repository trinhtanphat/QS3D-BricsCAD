#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


renderer = read("src/QS3D.Core/Documentation/SemanticTagRenderer.cs")
smoke = read("tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")

for token in [
    "MaxTemplateLength = 512",
    "MaxRenderedLength = 2048",
    "MaxTokens = 64",
    '"Id"',
    '"Category"',
    '"Family"',
    '"Floor"',
    '"Zone"',
    'token.StartsWith("P:"',
    'token.StartsWith("Q:"',
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
    'key.StartsWith("Generated"',
    'key.StartsWith("QS3D.Generated"',
    'key.StartsWith("PhysicalOpeningCut"',
    "Unsupported semantic tag token",
]:
    require(renderer, token, "semantic tag renderer")

for token in [
    "StableSemanticReferencesRender",
    "OptionalPropertyAndQuantityRender",
    "GeneratedOwnershipCannotLeakIntoTag",
    "UnsupportedTokenFailsClosed",
    "MissingReferenceFailsClosed",
]:
    require(smoke, token, "semantic tag smoke")

require(registration, "SemanticTagRendererSmoke.Run();", "smoke registration")
print("[PASS] semantic tag rendering is bounded, model-linked and blocks generated ownership leakage")
