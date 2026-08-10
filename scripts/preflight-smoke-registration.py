#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / "tests" / "QS3D.Core.SmokeTests"
errors = []

if not TESTS.is_dir():
    print("ERROR: missing tests/QS3D.Core.SmokeTests")
    sys.exit(1)

sources = {path: path.read_text(encoding="utf-8") for path in TESTS.glob("*.cs")}
all_text = "\n".join(sources.values())
run_pattern = re.compile(r"\b(?:public|internal|private)?\s*static\s+void\s+Run\s*\(")
class_pattern = re.compile(r"\b(?:public|internal|private)?\s*(?:static\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)")

checked = 0
for path, text in sorted(sources.items(), key=lambda item: item[0].name.lower()):
    if not path.name.endswith("Smoke.cs"):
        continue
    if not run_pattern.search(text):
        continue
    match = class_pattern.search(text)
    if not match:
        errors.append(path.name + ": Run() exists but no smoke class could be identified")
        continue
    class_name = match.group(1)
    checked += 1

    # A smoke can self-register with ModuleInitializer or be called from the central
    # registration file / a dedicated *Registration.cs module initializer.
    if "[ModuleInitializer]" in text:
        continue
    references = 0
    call_pattern = re.compile(r"\b" + re.escape(class_name) + r"\s*\.\s*Run\s*\(")
    for other_path, other_text in sources.items():
        if other_path == path:
            continue
        references += len(call_pattern.findall(other_text))
    if references == 0:
        errors.append(path.name + ": " + class_name + ".Run() is never registered or invoked")

# Lock the known historical regression that motivated this repository-wide guard.
beam_registration = TESTS / "BeamRebarSmokeRegistration.cs"
if not beam_registration.is_file():
    errors.append("missing BeamRebarSmokeRegistration.cs")
else:
    text = beam_registration.read_text(encoding="utf-8")
    for needle in ("[ModuleInitializer]", "BeamRebarRegressionSmoke.Run()"):
        if needle not in text:
            errors.append("Beam rebar smoke registration missing: " + needle)

print("QS3D smoke registration preflight")
print("Checked", checked, "smoke class(es) exposing static Run().")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: every runnable smoke class is self-registered or referenced by another test registration source.")