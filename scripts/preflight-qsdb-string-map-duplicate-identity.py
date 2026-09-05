from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs").read_text(encoding="utf-8")

start = source.index("private static void ValidateMap(")
end = source.index("private static void ValidateCanonicalMapKey(", start)
method = source[start:end]

required = [
    'var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);',
    'var key = property.Attribute("name")?.Value ?? string.Empty;',
    'if (!seenKeys.Add(key))',
    'throw new InvalidDataException("Duplicate QSDB map key in " + owner + ": " + key + ".");',
]
for token in required:
    if token not in method:
        raise SystemExit("Missing fail-closed QSDB string-map duplicate identity fence: " + token)

canonical = method.index("ValidateCanonicalMapKey(property, owner);")
key_capture = method.index('var key = property.Attribute("name")?.Value ?? string.Empty;')
duplicate = method.index("if (!seenKeys.Add(key))")
if not (canonical < key_capture < duplicate):
    raise SystemExit("QSDB map duplicate detection must run after per-key canonicality and before hydration admission completes.")

validate_current = source[source.index("internal static void ValidateCurrent("):source.index("private static void ValidateMap(")]
for token in [
    'ValidateMap(root.Element("metadata"), "project metadata");',
    'ValidateFamilies(root.Element("families"));',
    'ValidateElements(root.Element("elements"));',
]:
    if token not in validate_current:
        raise SystemExit("Current-schema admission no longer routes a persisted string-map surface through duplicate validation: " + token)

families = source[source.index("private static void ValidateFamilies("):source.index("private static void ValidateRules(")]
if 'ValidateMap(properties, "family properties")' not in families:
    raise SystemExit("Family properties must share the fail-closed QSDB map validator.")

elements = source[source.index("private static void ValidateElements("):source.index("private static void ValidateAudit(")]
if 'ValidateMap(properties, "element properties")' not in elements:
    raise SystemExit("Element properties must share the fail-closed QSDB map validator.")

print("QSDB project/family/element string-map duplicate identity preflight passed.")
