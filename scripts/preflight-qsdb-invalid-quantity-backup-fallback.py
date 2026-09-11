from pathlib import Path

store = Path("src/QS3D.Core/Persistence/QsdbProjectStore.cs").read_text(encoding="utf-8")
schema = Path("src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs").read_text(encoding="utf-8")


def method_block(method_name: str) -> str:
    signature = "private static void " + method_name
    start = schema.find(signature)
    if start < 0:
        raise SystemExit("QSDB canonical attribute guard failed; missing method: " + method_name)
    end = schema.find("\n        private static", start + len(signature))
    return schema[start:] if end < 0 else schema[start:end]


for method_name in (
    "ValidateRequiredCanonicalIdentityAttribute",
    "ValidateOptionalCanonicalAttribute",
):
    block = method_block(method_name)
    if "value.Any(char.IsControl)" not in block:
        raise SystemExit(
            "QSDB canonical attribute guard failed; " + method_name + " must reject internal control characters."
        )
    if "throw new InvalidDataException" not in block:
        raise SystemExit(
            "QSDB canonical attribute guard failed; " + method_name + " must normalize malformed persisted data as InvalidDataException."
        )

plain_required = method_block("ValidateRequiredCanonicalAttribute")
if "value.Any(char.IsControl)" in plain_required:
    raise SystemExit(
        "QSDB generic required-attribute validation must preserve XML-valid control characters for non-identity payloads such as quantity-rule expressions."
    )

if 'ValidateRequiredCanonicalIdentityAttribute(quantity, "name", "quantity name")' not in schema:
    raise SystemExit("QSDB quantity names must use the control-free required identity validator.")

if "IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException" not in store:
    raise SystemExit("QSDB backup fallback must continue to recover normalized InvalidDataException failures.")

if "exception is ArgumentException" in store:
    raise SystemExit(
        "QSDB backup fallback must not globally classify caller ArgumentException as recoverable; "
        "malformed persisted canonical identities belong at the schema boundary."
    )

if "Invalid persisted QSDB element quantity" in store:
    raise SystemExit(
        "QSDB quantity-name recovery must not retain a quantity-specific ArgumentException wrapper after schema identity canonicality is hardened."
    )

print("QSDB persisted canonical-identity backup-fallback guard passed.")
