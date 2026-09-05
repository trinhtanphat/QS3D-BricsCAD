#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "FloorIdentityAdmissionSmoke.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


service = SERVICE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_service_tokens = (
    "CanonicalFloorIdentityId",
    "NormalizationForm.FormC",
    "ToUpperInvariant()",
    "var canonicalId = CanonicalFloorIdentityId(normalizedId);",
    "CanonicalFloorIdentityId(x.Id)",
    "var seenIds = new HashSet<string>(StringComparer.Ordinal);",
    "var canonicalId = CanonicalFloorIdentityId(floor.Id);",
)
for token in required_service_tokens:
    if token not in service:
        fail(f"ProjectFloorService must retain Unicode-canonical floor admission token: {token}")

create_start = service.index("public static FloorDefinition Create")
create_end = service.index("public static FloorDefinition Update", create_start)
create = service[create_start:create_end]
if create.find("CanonicalFloorIdentityId(normalizedId)") > create.find("project.Floors.Any(x => string.Equals(CanonicalFloorIdentityId(x.Id)"):
    fail("Create must canonicalize the requested floor id before checking admitted floor identities")

validate_start = service.index("private static void ValidateUniqueFloorIds")
validate_end = service.index("private static string CanonicalFloorIdentityId", validate_start)
validate = service[validate_start:validate_end]
if "CanonicalFloorIdentityId(floor.Id)" not in validate or "seenIds.Add(canonicalId)" not in validate:
    fail("ValidateUniqueFloorIds must reject persisted Unicode-canonical floor identity aliases")

helper_start = service.index("private static string CanonicalFloorIdentityId")
helper_end = service.index("private static void EnsureUniqueName", helper_start)
helper = service[helper_start:helper_end]
nfc_first = helper.find("Normalize(NormalizationForm.FormC)")
upper = helper.find("ToUpperInvariant()", nfc_first)
nfc_second = helper.find("Normalize(NormalizationForm.FormC)", upper)
if min(nfc_first, upper, nfc_second) < 0 or not (nfc_first < upper < nfc_second):
    fail("floor admission identity must remain Trim -> NFC -> invariant uppercase -> NFC")

required_smoke_tokens = (
    'const string composedId = "LEVEL-\\u00C9";',
    'const string decomposedId = "LEVEL-E\\u0301";',
    "RejectsUnicodeCanonicalAliasAtCreate",
    "RejectsUnicodeCanonicalAliasInPersistedProjectValidation",
    'Equal(composedId, project.Floors[0].Id, "stored caller spelling")',
)
for token in required_smoke_tokens:
    if token not in smoke:
        fail(f"FloorIdentityAdmissionSmoke must retain regression coverage token: {token}")

print("PASS: floor admission rejects Unicode-canonical identity aliases at create and persisted validation boundaries without rewriting valid caller spelling")
