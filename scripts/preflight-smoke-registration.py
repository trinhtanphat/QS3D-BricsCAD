#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / "tests" / "QS3D.Core.SmokeTests"
REG = TESTS / "SmokeTestRegistration.cs"
errors = []

if not TESTS.is_dir():
    errors.append("missing Core smoke-test directory")
if not REG.is_file():
    errors.append("missing SmokeTestRegistration.cs")

registration = REG.read_text(encoding="utf-8") if REG.is_file() else ""
class_pattern = re.compile(r"\b(?:internal|public)\s+static\s+class\s+(\w+Smoke)\b")
run_pattern = re.compile(r"\b(?:public|internal)\s+static\s+void\s+Run\s*\(\s*\)")

if TESTS.is_dir():
    for path in sorted(TESTS.glob("*Smoke.cs")):
        text = path.read_text(encoding="utf-8")
        classes = class_pattern.findall(text)
        if not classes or not run_pattern.search(text):
            continue
        for class_name in classes:
            token = class_name + ".Run();"
            if token not in registration:
                errors.append(str(path.relative_to(ROOT)) + " exposes parameterless Run() but is not registered: " + token)

print("QS3D Core smoke registration preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: every discoverable Core *Smoke class with a parameterless static Run() participates in SmokeTestRegistration.RunAll().")
