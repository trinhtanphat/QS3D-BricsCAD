#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
codec = root / "src/QS3D.Core/Domain/ProjectTbqWorkspaceCodec.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/TbqProjectWorkspaceEncodingBoundSmoke.cs"
errors = []

codec_text = codec.read_text(encoding="utf-8") if codec.exists() else ""
smoke_text = smoke.read_text(encoding="utf-8") if smoke.exists() else ""

append_start = codec_text.find("private static void AppendField(StringBuilder builder, string value)")
read_start = codec_text.find("private static string ReadField(", append_start)
append_method = codec_text[append_start:read_start] if append_start >= 0 and read_start > append_start else ""

required_codec = [
    "var lengthToken = value.Length.ToString(CultureInfo.InvariantCulture);",
    "var prospectiveLength = (long)builder.Length + lengthToken.Length + 1L + value.Length;",
    "if (prospectiveLength > MaxPayloadChars)",
    "throw PayloadTooLargeError();",
    "builder.Append(lengthToken);",
    "builder.Append(':');",
    "builder.Append(value);",
]
for token in required_codec:
    if token not in append_method:
        errors.append(f"AppendField missing bounded-encoding contract token: {token}")

if append_method:
    guard = append_method.find("if (prospectiveLength > MaxPayloadChars)")
    append = append_method.find("builder.Append(lengthToken);")
    if guard < 0 or append < 0 or guard > append:
        errors.append("AppendField must reject the prospective payload before mutating the builder")
else:
    errors.append("AppendField method not found")

for token in [
    "ExactBoundaryAcceptsSupplementaryUnicode",
    "PrefixDigitsParticipateInBound",
    "OverflowRejectsBeforeBuilderMutation",
    "LateFieldOverflowPreservesAcceptedPrefix",
    "ModuleInitializer",
]:
    if token not in smoke_text:
        errors.append(f"smoke missing deterministic encoding-bound coverage token: {token}")

if "private const int MaxPayloadChars = 1024 * 1024;" not in codec_text:
    errors.append("TBQ persistence payload ceiling changed or disappeared")
if "PersistedTextXml.Verify(payload, nameof(state), \"TBQ project workspace metadata\");" not in codec_text:
    errors.append("TBQ final XML validation contract disappeared")
if "Decode(payload);" not in codec_text:
    errors.append("TBQ final self-decode validation contract disappeared")

if errors:
    print("TBQ workspace encoding-bound preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS TBQ workspace incremental persistence payload bound")
