#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotElementIdentitySmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Project snapshot property preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
required_source = (
    "RequireCanonicalFamilyProperties(family);",
    "RequireCanonicalElementProperties(element);",
    'ProjectFamilyService.SnapshotProperties(family, "Snapshot", "snapshot capture")',
    "var snapshotProperties = ProjectFamilyService.SnapshotProperties(",
    '"snapshot materialization",',
    "preserveNullValues: true);",
    "target.RestoreSnapshotState(source.Name, source.Category, snapshotProperties);",
    "var canonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "if (!string.Equals(canonicalName, property.Key, StringComparison.Ordinal))",
    "RequireXmlPropertyText(canonicalName, element.Id, \"name\");",
    "RequireXmlPropertyText(property.Value ?? string.Empty, element.Id, \"value\");",
    "property names that collapse to the same canonical identity",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Project snapshot property source contract missing: " + repr(missing))

required_smoke = (
    "RejectsInvalidMutablePropertyState();",
    "PreservesCanonicalPropertyState();",
    'ExpectRejectedElementProperty("padded key", " WidthM ", "0.2");',
    'ExpectRejectedElementProperty("malformed value", "WidthM", "bad\\uD800value");',
    'ExpectRejectedFamilyProperty("oversized key", new string(\'K\', 121), "0.2");',
    'ExpectRejectedFamilyProperty("oversized value", "Description", new string(\'V\', 1001));',
    'family.Properties["Description"] = "Family-\\U0001F680\\tvalue";',
    'element.SetProperty("Label", "Element-\\U0001F680\\nvalue");',
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Project snapshot property regression coverage missing: " + repr(missing))

print("PASS project snapshot Family/Element property canonicality guard")
