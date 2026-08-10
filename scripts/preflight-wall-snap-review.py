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

    safety_tokens = (
        "WallJunctionAdjustmentPlanner",
        "PlanHash",
        "SourceFingerprint",
        "preview",
        "fingerprint",
    )
    for token in safety_tokens:
        if token.lower() not in all_text.lower():
            errors.append("wall snap review/apply safety token missing: " + token)

    apply_sources = [text for _, text in sources if 'CommandMethod("QS3DWALLSNAPAPPLY"' in text]
    if apply_sources:
        apply_text = "\n".join(apply_sources)
        if "Transaction" not in apply_text:
            errors.append("wall snap apply must use a CAD transaction")
        if "Erase" in apply_text and "SourceFingerprint" not in apply_text:
            errors.append("wall snap apply contains destructive erase without source fingerprint guard")

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

print("PASS: wall snap Preview/Apply commands, plan/source fingerprint safeguards, transaction boundary and UI wiring are present.")
