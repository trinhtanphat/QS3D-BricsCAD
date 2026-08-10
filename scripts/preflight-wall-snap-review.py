#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

adapter = ROOT / "src/QS3D.BricsCAD.V25"
if not adapter.is_dir():
    errors.append("missing BricsCAD V25 adapter directory")
else:
    sources = []
    combined = []
    for path in adapter.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        sources.append((path, text))
        combined.append(text)
    all_text = "\n".join(combined)

    for command in ("QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY"):
        if not re.search(r'CommandMethod\("' + re.escape(command) + r'"', all_text):
            errors.append("missing wall snap command: " + command)

    for token in (
        "WallJunctionAdjustmentPlanner",
        "PreviewSignatureKey",
        "BuildSignature",
        "SHA256.Create",
        "Preview không còn khớp selection/geometry hiện tại",
        "GeneratedDependentGeometryInvalidator.Prepare",
    ):
        if token not in all_text:
            errors.append("wall snap review/apply safety token missing: " + token)

    apply_sources = [text for _, text in sources if 'CommandMethod("QS3DWALLSNAPAPPLY"' in text]
    if apply_sources:
        apply_text = "\n".join(apply_sources)
        if "Transaction" not in apply_text:
            errors.append("wall snap apply must use a CAD transaction")

        signature_marker = "string.Equals(preview, plan.Signature"
        invalidation_marker = "GeneratedDependentGeometryInvalidator.Prepare"
        signature_index = apply_text.find(signature_marker)
        invalidation_index = apply_text.find(invalidation_marker)
        if signature_index < 0:
            errors.append("wall snap apply must compare the preview signature with the freshly rebuilt plan")
        if invalidation_index < 0:
            errors.append("wall snap apply must prepare generated-geometry invalidation")
        if signature_index >= 0 and invalidation_index >= 0 and signature_index > invalidation_index:
            errors.append("wall snap source/plan signature validation must happen before generated-geometry invalidation")

        destructive_index = apply_text.find(".Erase(")
        if destructive_index >= 0 and (signature_index < 0 or signature_index > destructive_index):
            errors.append("wall snap apply contains destructive erase before source/plan signature validation")

        build_signature_index = apply_text.find("private static string BuildSignature")
        sha_index = apply_text.find("SHA256.Create", build_signature_index if build_signature_index >= 0 else 0)
        if build_signature_index < 0 or sha_index < build_signature_index:
            errors.append("wall snap preview signature must be a cryptographic hash of the rebuilt plan/source geometry")

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    for tag in ('Tag="QS3DWALLSNAPPREVIEW"', 'Tag="QS3DWALLSNAPAPPLY"'):
        if tag not in text:
            errors.append("Domain Hub missing wall snap workflow tag: " + tag)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.is_file():
    text = registration.read_text(encoding="utf-8")
    if "WallJunctionAdjustmentSmoke.Run();" not in text:
        errors.append("WallJunctionAdjustmentSmoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: wall snap Preview/Apply commands, SHA-256 source/plan signature validation before invalidation, transaction boundary and UI wiring are present.")
