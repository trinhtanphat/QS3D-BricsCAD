from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsIntegrationApiV1.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    'new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/tenders", QsApiResourceKind.Tender, "qs3d.tender.read", false)',
    'new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/procurement", QsApiResourceKind.Procurement, "qs3d.procurement.read", false)',
    'if (endpoint.Cacheable && string.Equals(request.IfNoneMatch, etag, StringComparison.Ordinal))',
]
for marker in required:
    if marker not in text:
        raise SystemExit(f"missing commercial cache coherency marker: {marker}")

for forbidden in [
    'new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/tenders", QsApiResourceKind.Tender, "qs3d.tender.read", true)',
    'new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/procurement", QsApiResourceKind.Procurement, "qs3d.procurement.read", true)',
]:
    if forbidden in text:
        raise SystemExit(f"mutable commercial resource must not use model-revision conditional caching: {forbidden}")

print("integration API commercial cache coherency guard: PASS")
