#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
helper = root / "src/QS3D.BricsCAD.V25/Services/InterchangeFamilySemanticApplier.cs"
catalog = root / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceCatalogImportService.cs"
all_scope = root / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceAllImportService.cs"
family_service = root / "src/QS3D.Core/Domain/ProjectFamilyService.cs"

errors = []
for path in (helper, catalog, all_scope, family_service):
    if not path.exists():
        errors.append(f"missing family interchange contract file: {path.relative_to(root)}")

if not errors:
    h = helper.read_text(encoding="utf-8")
    c = catalog.read_text(encoding="utf-8")
    a = all_scope.read_text(encoding="utf-8")
    f = family_service.read_text(encoding="utf-8")

    required_helper = [
        "ProjectFamilyService.Create(project, id, name, category)",
        "ProjectFamilyService.Rename(project, family.Id, name)",
        "new HashSet<string>(incoming.Select(x => x.Key), StringComparer.OrdinalIgnoreCase)",
        "ProjectFamilyService.RemoveProperty(project, family.Id, key)",
        "ProjectFamilyService.SetProperty(project, family.Id, property.Key, property.Value ?? string.Empty)",
        "family.Category != category",
    ]
    for needle in required_helper:
        if needle not in h:
            errors.append("family semantic applier missing inheritance contract: " + needle)

    replace_match = re.search(r"public static ProjectFamily Replace\(.*?\n        private static IEnumerable", h, re.S)
    if not replace_match:
        errors.append("family semantic applier Replace method not found")
    else:
        replace = replace_match.group(0)
        remove_pos = replace.find("ProjectFamilyService.RemoveProperty")
        set_pos = replace.find("ProjectFamilyService.SetProperty")
        if remove_pos < 0 or set_pos < 0 or remove_pos > set_pos:
            errors.append("Family properties removed by source must be processed before new values are propagated")
        if ".Properties.Clear()" in replace:
            errors.append("Family replacement must never direct-clear properties because it bypasses inherited-instance semantics")

    required_domain = [
        "public static FamilyPropertyUpdateResult SetProperty",
        "public static FamilyPropertyUpdateResult RemoveProperty",
        "if (!hasInstance || (hadPrevious && string.Equals(instance, previous, StringComparison.Ordinal)))",
        "if (string.Equals(instance, previous, StringComparison.Ordinal))",
        "result.OverridesPreserved++",
    ]
    for needle in required_domain:
        if needle not in f:
            errors.append("ProjectFamilyService missing inheritance-preserving domain behavior: " + needle)

    for label, text, next_method in [
        ("catalog", c, "ApplyNewElementsOnly"),
        ("all-scope", a, "ApplyElementState"),
    ]:
        section = re.search(r"private static void ApplyCatalogState\(.*?\n        private static void " + next_method, text, re.S)
        if not section:
            errors.append(label + " ApplyCatalogState section not found")
            continue
        body = section.group(0)
        for needle in (
            "InterchangeFamilySemanticApplier.Add(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
            "InterchangeFamilySemanticApplier.Replace(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
        ):
            if needle not in body:
                errors.append(label + " importer must route Family mutation through inheritance-aware applier: " + needle)
        if "target.Properties.Clear();" in body:
            errors.append(label + " catalog mutation direct-clears Family.Properties")

if errors:
    print("preflight-interchange-family-inheritance: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-family-inheritance: PASS")
print("Catalog/ALL source replacement routes Family changes through domain services so inherited values/removals propagate while true element overrides remain intact.")
