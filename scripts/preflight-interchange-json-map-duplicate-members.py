from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeMapDuplicateMemberSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "ValidateMapDuplicateMembers",
    '"$.families["',
    '".properties"',
    '"$.elements["',
    '".quantities"',
    '"JSON_DUPLICATE_MEMBER"',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing interchange map duplicate-member guard token: {token}")

shape = source.index("ValidateNoUnknownMembers(utf8, issues);")
deserialize = source.index("serializer.ReadObject(stream)")
if shape >= deserialize:
    raise SystemExit("map duplicate-member shape inspection must precede typed deserialization")

for token in [
    "DuplicateFamilyPropertyFailsClosed",
    "DuplicateElementPropertyFailsClosed",
    "DuplicateElementQuantityFailsClosed",
    "UniqueMapControlRemainsValid",
]:
    if token not in smoke:
        raise SystemExit(f"missing deterministic interchange map smoke: {token}")

print("PASS interchange JSON map duplicate-member source guard")
