from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsIntegrationApiV1.cs"
text = SOURCE.read_text(encoding="utf-8")
start = text.index("public sealed class QsApiPrincipal")
end = text.index("public sealed class QsApiEndpointDescriptor")
principal = text[start:end]

required = [
    ".Distinct(StringComparer.Ordinal)",
    ".OrderBy(x => x, StringComparer.Ordinal)",
    "Scopes.Contains(scope, StringComparer.Ordinal)",
    "Scopes.Contains(\"qs3d.admin\", StringComparer.Ordinal)",
]
for marker in required:
    if marker not in principal:
        raise SystemExit(f"missing case-sensitive scope authorization marker: {marker}")

if "OrdinalIgnoreCase" in principal:
    raise SystemExit("QsApiPrincipal authorization scopes must not use OrdinalIgnoreCase")

print("integration API scope case-sensitivity guard: PASS")
