#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
POLICY = ROOT / "src/QS3D.Core/Domain/ElementGeometryPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementPropertyInvalidationSmoke.cs"
errors = []

for path in (SOURCE, POLICY, SMOKE):
    if not path.is_file():
        errors.append("missing property invalidation contract file: " + str(path.relative_to(ROOT)))

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    policy = POLICY.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    set_property = re.search(
        r"public void SetProperty\(string name, string value\)(?P<body>.*?)\n        internal void AddProperty",
        source,
        re.DOTALL,
    )
    if not set_property:
        errors.append("missing ProjectElement.SetProperty body")
    else:
        body = set_property.group("body")
        if "MarkPropertyChanged(key);" not in body:
            errors.append("SetProperty must route changed property values through MarkPropertyChanged(key)")
        if "MarkDirty(flags)" in body or "MarkDirtyCore(" in body:
            errors.append("SetProperty must delegate property-specific invalidation instead of applying dirty state directly")

    mark_property_changed = re.search(
        r"private void MarkPropertyChanged\(string key\)(?P<body>.*?)\n        private void MarkDirtyCore",
        source,
        re.DOTALL,
    )
    if not mark_property_changed:
        errors.append("missing ProjectElement.MarkPropertyChanged body")
    else:
        body = mark_property_changed.group("body")
        for token in (
            "ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key)",
            "ElementGeometryPolicy.AffectsGeneratedOutput(Category, key)",
            "ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity",
            "if (affectsGeneratedGeometry) flags |= ElementDirtyFlags.Geometry",
            "MarkDirtyCore(flags, affectsGeneratedOutput)",
        ):
            if token not in body:
                errors.append("MarkPropertyChanged missing property-specific invalidation token: " + token)
        if "MarkDirty(flags)" in body:
            errors.append("MarkPropertyChanged must not route non-geometry property edits through broad MarkDirty(flags)")

    for method_name in ("AddProperty", "RemoveProperty"):
        method = re.search(
            rf"internal (?:void|bool) {method_name}\(.*?\)(?P<body>.*?)\n        (?:internal|public|private) ",
            source,
            re.DOTALL,
        )
        if not method or "MarkPropertyChanged(key);" not in method.group("body"):
            errors.append(method_name + " must preserve the shared property-specific invalidation boundary")

    mark_dirty = re.search(
        r"public void MarkDirty\(ElementDirtyFlags flags\)(?P<body>.*?)\n        public void MarkClean",
        source,
        re.DOTALL,
    )
    if not mark_dirty:
        errors.append("missing ProjectElement.MarkDirty body")
    else:
        body = mark_dirty.group("body")
        for token in (
            "MarkDirtyCore(",
            "ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Relations",
        ):
            if token not in body:
                errors.append("public MarkDirty must retain broad generated-stale compatibility: " + token)

    for token in (
        "private void MarkDirtyCore(ElementDirtyFlags flags, bool markGeneratedGeometryStale)",
        "if ((flags & ~ElementDirtyFlags.All) != 0)",
        "if (markGeneratedGeometryStale)",
        'MarkGeneratedGeometryStale("Semantic/source state changed.")',
        "Dirty |= flags",
        "UpdatedUtc = DateTime.UtcNow",
    ):
        if token not in source:
            errors.append("ProjectElement dirty core missing token: " + token)

    for token in (
        '"WidthM"',
        '"Material"',
        '"CurtainFrameMaterial"',
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.BottomLevelOffsetKey",
        "ProjectFloorService.TopLevelIdKey",
        "ProjectFloorService.TopLevelOffsetKey",
        "AffectsGeneratedGeometry",
        "AffectsGeneratedOutput",
        "RequiresGeneratedGeometry(category)",
    ):
        if token not in policy:
            errors.append("ElementGeometryPolicy missing geometry classification token: " + token)

    for token in (
        "NonGeometryPropertyPreservesFreshGeneratedOutput",
        "GeometryPropertyStalesGeneratedOutput",
        "LevelReferencePropertiesStaleGeneratedOutput",
        "BroadPropertyDirtyRetainsCompatibility",
        "NoOpPropertyWriteDoesNotMutateState",
        "NonGeometryPropertyDoesNotClearExistingStaleState",
        'element.SetProperty("Mark", "B-01")',
        'element.SetProperty("WidthM", "0.35")',
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelOffsetKey",
        "False(element.IsGeneratedGeometryStale())",
        "True(element.IsGeneratedGeometryStale())",
        "[ModuleInitializer]",
    ):
        if token not in smoke:
            errors.append("property invalidation smoke missing regression token: " + token)

print("QS3D ProjectElement property-specific invalidation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ProjectElement property mutations share MarkPropertyChanged for key-sensitive generated-output invalidation, while public MarkDirty(Properties/Relations/Geometry) retains broad compatibility and Core smoke coverage locks the behavior.")