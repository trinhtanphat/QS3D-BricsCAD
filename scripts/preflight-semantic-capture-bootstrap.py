#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        position = text.find(token, cursor + 1)
        if position < 0:
            raise AssertionError(label + " missing ordered token: " + token)
        if position <= cursor:
            raise AssertionError(label + " has invalid token ordering: " + token)
        cursor = position


def main():
    text = SOURCE.read_text(encoding="utf-8")

    batch = method_slice(
        text,
        "public static int Capture(Document document, ElementCategory category)",
        "public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)")
    require_order(
        batch,
        "batch semantic capture",
        "if (snapshots.Count == 0) return 0;",
        "EnsureCapturePreflight(document, snapshots, category);",
        "ProjectContextCoordinator.GetOrCreate(document);")

    single = method_slice(
        text,
        "public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)",
        "private static void EnsureCapturePreflight(")
    require_order(
        single,
        "single semantic capture",
        "if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));",
        "EnsureCapturePreflight(document, new[] { snapshot }, category);",
        "ProjectContextCoordinator.GetOrCreate(document);")

    preflight = method_slice(
        text,
        "private static void EnsureCapturePreflight(",
        "private static bool CaptureSnapshotCore(")
    require(preflight, "EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category);", "capture preflight eligibility")
    require(preflight, "CadUnitService.TryGetPolicy(document, out _, out _)", "capture preflight unit policy")
    if "ProjectContextCoordinator.GetOrCreate" in preflight:
        raise AssertionError("Capture preflight must remain read-only and cannot bootstrap project state.")

    core = method_slice(
        text,
        "private static bool CaptureSnapshotCore(",
        "private static void RestoreOrThrow(")
    require(core, "EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category);", "capture core eligibility recheck")
    require(core, "CadUnitService.TryGetPolicy(document, out var units, out var unitResolution)", "capture core unit-policy recheck")
    require(core, "DrawingUnitResolutionPolicy.BindQuantityUnit(", "capture core quantity-unit binding")

    print("PASS: semantic capture validates eligibility and units before project bootstrap while retaining core rechecks.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
