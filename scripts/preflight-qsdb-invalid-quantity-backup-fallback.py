from pathlib import Path

store = Path("src/QS3D.Core/Persistence/QsdbProjectStore.cs").read_text(encoding="utf-8")
schema = Path("src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs").read_text(encoding="utf-8")

for method_name in (
    "ValidateRequiredCanonicalAttribute",
    "ValidateOptionalCanonicalAttribute",
):
    signature = "private static void " + method_name
    start = schema.find(signature)
    if start < 0:
        raise SystemExit("QSDB canonical attribute guard failed; missing method: " + method_name)
    end = schema.find("\n        private static", start + len(signature))
    block = schema[start:] if end < 0 else schema[start:end]
    if "value.Any(char.IsControl)" not in block:
        raise SystemExit(
            "QSDB canonical attribute guard failed; " + method_name + " must reject internal control characters."
        )
    if "throw new InvalidDataException" not in block:
        raise SystemExit(
            "QSDB canonical attribute guard failed; " + method_name + " must normalize malformed persisted data as InvalidDataException."
        )

if "IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException" not in store:
    raise SystemExit("QSDB backup fallback must continue to recover normalized InvalidDataException failures.")

if "exception is ArgumentException" in store:
    raise SystemExit(
        "QSDB backup fallback must not globally classify caller ArgumentException as recoverable; "
        "malformed persisted canonical attributes belong at the schema boundary."
    )

if "Invalid persisted QSDB element quantity" in store:
    raise SystemExit(
        "QSDB quantity-name recovery must not retain a quantity-specific ArgumentException wrapper after schema canonicality is hardened."
    )

print("QSDB persisted canonical-attribute backup-fallback guard passed.")
