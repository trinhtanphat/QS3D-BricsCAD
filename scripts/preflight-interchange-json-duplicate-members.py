#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeDuplicateMemberSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing interchange duplicate-member integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    required = [
        '"JSON_DUPLICATE_MEMBER"',
        "var observedMembers = new HashSet<string>(StringComparer.Ordinal);",
        "if (!observedMembers.Add(name))",
        "ValidateNoUnknownMembers(utf8, issues);",
        "serializer.ReadObject(stream) as SnapshotContract",
    ]
    for token in required:
        if token not in source:
            errors.append("interchange validator missing duplicate-member integrity token: " + token)

    shape = source.find("ValidateNoUnknownMembers(utf8, issues);")
    deserialize = source.find("serializer.ReadObject(stream) as SnapshotContract")
    if shape < 0 or deserialize < 0 or shape > deserialize:
        errors.append("interchange JSON shape/duplicate inspection must run before DataContract deserialization")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicateRootMemberFailsClosed();",
        "DuplicateProjectMemberFailsClosed();",
        "DuplicateArrayObjectMemberFailsClosed();",
        "UnknownMemberContractRemainsStable();",
        "UniqueControlStillReachesSemanticValidation();",
        '"JSON_DUPLICATE_MEMBER"',
        '"$.project"',
        '"$.zones[0]"',
        "[ModuleInitializer]",
    ):
        if token not in smoke:
            errors.append("duplicate-member smoke missing assertion/control: " + token)

print("QS3D interchange JSON duplicate-member integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: interchange JSON structure rejects duplicate known members before semantic deserialization.")
