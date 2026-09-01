from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "GeneratedHandleOwnershipPolicy.cs"
text = SOURCE.read_text(encoding="utf-8")

start = text.find("private static IReadOnlyList<string> SplitHandles(string raw)")
if start < 0:
    raise SystemExit("FAIL: GeneratedHandleOwnershipPolicy.SplitHandles was not found")
end = text.find("\n        }\n    }\n}", start)
if end < 0:
    raise SystemExit("FAIL: could not isolate GeneratedHandleOwnershipPolicy.SplitHandles")
method = text[start:end]

required = [
    "for (var index = 0; index <= source.Length; index++)",
    "source[index] != ';'",
    "handles.Count >= MaxDestructiveHandleCount",
    "source.Substring(tokenStart, index - tokenStart)",
    "NormalizeHandleIdentity(token)",
    "!string.Equals(token, normalized, StringComparison.Ordinal)",
    "!seen.Add(normalized)",
]
for snippet in required:
    if snippet not in method:
        raise SystemExit(f"FAIL: persisted generated-owner parser lost required bounded/integrity contract: {snippet}")

for forbidden in (
    ".Split(new[] { ';' }",
    ".Split(';')",
    ".Split(\";\")",
):
    if forbidden in method:
        raise SystemExit("FAIL: persisted generated-owner parser regressed to eager delimiter splitting before the 10,000-entry bound")

if text.count("private const int MaxDestructiveHandleCount = 10000;") != 1:
    raise SystemExit("FAIL: generated-handle destructive safety bound must remain one canonical 10,000-entry contract")

print("PASS generated handle persisted owner-list bound")
