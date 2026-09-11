from pathlib import Path

source = Path("src/QS3D.Core/Persistence/QsdbProjectStore.cs").read_text(encoding="utf-8")

required = [
    "try",
    "element.SetQuantity(quantityName, quantityValue);",
    "catch (ArgumentException ex)",
    "throw new InvalidDataException(\"Invalid persisted QSDB element quantity",
]

missing = [token for token in required if token not in source]
if missing:
    raise SystemExit(
        "QSDB invalid persisted quantity backup-fallback guard failed; missing: "
        + ", ".join(repr(token) for token in missing)
    )

if "IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException" not in source:
    raise SystemExit("QSDB backup fallback must continue to normalize malformed persisted data through InvalidDataException.")

if "exception is ArgumentException" in source:
    raise SystemExit(
        "QSDB backup fallback must not globally classify caller ArgumentException as recoverable; "
        "normalization belongs at the persisted quantity deserialization boundary."
    )

print("QSDB invalid persisted quantity backup-fallback guard passed.")
