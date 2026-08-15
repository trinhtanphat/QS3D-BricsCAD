#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
layout_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/Qs3dRibbonTabGroupCoordinator.cs"
init_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
bootstrap_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"

errors = []

for path in (layout_path, init_path, bootstrap_path):
    if not path.is_file():
        errors.append(f"missing required source: {path.relative_to(ROOT)}")

if not errors:
    layout = layout_path.read_text(encoding="utf-8")
    init = init_path.read_text(encoding="utf-8")
    bootstrap = bootstrap_path.read_text(encoding="utf-8")

    primary_ids = [
        "QS3D_HOME",
        "QS3D_PROJECT",
        "QS3D_BIM",
        "QS3D_RECOGNIZE",
        "QS3D_DRAW",
        "QS3D_TOOL",
        "QS3D_MODELING",
        "QS3D_VIEW",
        "QS3D_QTY",
        "QS3D_REV",
    ]
    primary_titles = [
        "KHỞI ĐẦU",
        "THIẾT LẬP DỰ ÁN",
        "MÔ HÌNH BIM",
        "NHẬN DẠNG",
        "VẼ",
        "TOOL",
        "MODELING",
        "XEM",
        "ĐỊNH LƯỢNG",
        "BẢN SỬA ĐỔI",
    ]

    last = -1
    for tab_id in primary_ids:
        pos = layout.find(f'"{tab_id}"')
        if pos < 0:
            errors.append(f"primary QS3D tab missing from grouping order: {tab_id}")
        elif pos <= last:
            errors.append(f"primary QS3D tab order drifted at: {tab_id}")
        last = max(last, pos)

    for title in primary_titles:
        if f'"{title}"' not in bootstrap:
            errors.append(f"BLT3D-familiar QS3D top navigation title missing: {title}")

    required_layout_contracts = (
        'private const string OwnedTabPrefix = "QS3D_";',
        'id.StartsWith(OwnedTabPrefix, StringComparison.OrdinalIgnoreCase)',
        'var ownedTabs = snapshot.Where(IsOwnedTab).ToList();',
        'foreach (var tab in ownedTabs)\n                        Remove(tabs, tab);',
        'foreach (var tab in orderedOwnedTabs)\n                        Add(tabs, tab);',
        'return snapshot.Count(IsOwnedTab) == orderedOwnedTabs.Count;',
    )
    for token in required_layout_contracts:
        if token not in layout:
            errors.append(f"QS3D/native Ribbon ownership contract missing: {token}")

    # This coordinator must not mutate tab titles/names/panels. It only repositions QS3D tabs.
    for forbidden in ('SetProperty(', '"Title"', '"Name"', '"Panels"'):
        if forbidden in layout:
            errors.append(f"layout coordinator must not mutate native/QS3D tab content: {forbidden}")

    init_call = "ready = Qs3dRibbonTabGroupCoordinator.TryInitialize() && ready;"
    if init_call not in init:
        errors.append("Ribbon initialization does not finalize the QS3D/native tab grouping")
    else:
        layout_pos = init.find(init_call)
        for augmenter in (
            "ReferenceWallRibbonAugmenter.TryInitialize()",
            "ProjectRibbonAugmenter.TryInitialize()",
            "QuickWorkflowRibbonAugmenter.TryInitialize()",
            "RaftFoundationRibbonAugmenter.TryInitialize()",
            "QuantityReferenceRibbonAugmenter.TryInitialize()",
            "UpdateRibbonAugmenter.TryInitialize()",
        ):
            pos = init.find(augmenter)
            if pos < 0:
                errors.append(f"expected Ribbon augmenter missing: {augmenter}")
            elif pos > layout_pos:
                errors.append(f"QS3D tab grouping must run after Ribbon augmentation: {augmenter}")

    if "Qs3dRibbonTabGroupCoordinator.Reset();" not in init:
        errors.append("Ribbon tab group coordinator is not reset on plugin shutdown")

if errors:
    print("RIBBON TOPBAR GROUP PREFLIGHT: FAIL")
    for error in errors:
        print(f"- {error}")
    raise SystemExit(1)

print("RIBBON TOPBAR GROUP PREFLIGHT: PASS")
print("- Native/third-party tab objects remain untouched and preserve relative order.")
print("- QS3D_* tabs are grouped at the end of the same RibbonControl.Tabs row.")
print("- Primary QS3D navigation follows the requested BLT3D-familiar menu order.")
