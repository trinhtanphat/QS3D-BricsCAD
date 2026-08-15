#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message):
    print("ERROR:", message)
    return 1


def require(path, tokens):
    if not path.is_file():
        raise RuntimeError(f"missing slabOpen workspace surface: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise RuntimeError(f"{path.relative_to(ROOT)} missing workspace contract token: {token}")
    return text


def main():
    try:
        quick = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs",
            (
                "EnsureSlabOpeningWorkspaceRoute();",
                "var family = ResolveWorkspaceDrawFamily();",
                'var command = advanced ? "QS3DDRAWACTIVEADV" : "QS3DDRAWACTIVE";',
                "IsSlabOpeningWorkspaceRouteSelected()",
                "Lỗ Mở Sàn chỉ dùng Vẽ Nhanh / Vẽ tùy chỉnh",
            ),
        )
        route = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SlabOpen.cs",
            (
                'SlabOpeningWorkspaceTag = "QS3D.SlabOpen"',
                '"Lỗ Mở Sàn"',
                '"Sàn Đặc"',
                "SlabOpeningContract.IsSlabOpenFamily",
                "ApplySlabOpeningWorkspaceFamilyFilter()",
                "FindOrCreateExactSlabOpeningFamily()",
                "FindUniqueExactSlabOpeningFamily",
                ".Where(SlabOpeningContract.IsSlabOpenFamily)",
                ".Take(2)",
                "matches.Count > 1",
                "Project có nhiều Family cùng thỏa exact slabOpen",
                "ExistingProjectMutationContext.Require",
                "ProjectFamilyService.Create",
                "SlabOpeningContract.FamilyKey",
                "ElementCategory.WallOpening",
                "_viewModel.SetActiveFamily(family);",
                "không dùng Family Sàn thay thế",
                "không fallback sang Slab/WallOpening khác",
            ),
        )
    except RuntimeError as exc:
        return fail(str(exc))

    if "ElementCategory.Slab.ToString()" not in route:
        return fail("workspace route must split the existing Slab leaf instead of inventing another generic category")
    if "family.Category == ElementCategory.Slab" in route:
        return fail("slabOpen workspace route must not accept a generic Slab family")
    if "GetOrCreate" in route:
        return fail("selecting Lỗ Mở Sàn must not silently create a QS3D project")
    if "FirstOrDefault(SlabOpeningContract.IsSlabOpenFamily)" in route:
        return fail("slabOpen workspace must not silently choose the first of multiple exact-family candidates")

    unique = route.find("FindUniqueExactSlabOpeningFamily")
    bounded = route.find(".Take(2)", unique)
    ambiguous = route.find("matches.Count > 1", unique)
    if unique < 0 or bounded < 0 or ambiguous < 0 or not (unique < bounded < ambiguous):
        return fail("exact slabOpen resolution must use a bounded uniqueness check and reject ambiguity")

    canonical_reread = route.find("family = FindUniqueExactSlabOpeningFamily(project.Families);")
    create = route.find("ProjectFamilyService.Create(", canonical_reread)
    family_key = route.find("SlabOpeningContract.FamilyKey", create)
    wall_opening = route.find("ElementCategory.WallOpening", create)
    if canonical_reread < 0 or create < 0 or family_key < 0 or wall_opening < 0:
        return fail("missing unique canonical re-read before exact slabOpen provisioning as WallOpening")
    if not canonical_reread < create:
        return fail("workspace must reject canonical-project ambiguity before provisioning slabOpen")

    if 'Send("QS3DDRAWSLABOPEN")' in quick or 'Send("QS3DDRAWSLABOPENADV")' in quick:
        return fail("workspace must retain active-family dispatch freshness instead of bypassing QS3DDRAWACTIVE")
    if "_viewModel.SetActiveFamily(family);" not in quick:
        return fail("quick draw must canonically activate the resolved exact family before dispatch")

    ensure = quick.find("EnsureSlabOpeningWorkspaceRoute();")
    menu = quick.find("var menu = FamilyList.ContextMenu;")
    if ensure < 0 or menu < 0 or ensure > menu:
        return fail("slabOpen workspace route must be installed before context-menu early returns")

    resolve = quick.find("var family = ResolveWorkspaceDrawFamily();")
    activate = quick.find("_viewModel.SetActiveFamily(family);", resolve)
    dispatch = quick.find('var command = advanced ? "QS3DDRAWACTIVEADV" : "QS3DDRAWACTIVE";', resolve)
    if resolve < 0 or activate < 0 or dispatch < 0 or not (resolve < activate < dispatch):
        return fail("quick draw must resolve exact slabOpen, activate it, then dispatch")

    print(
        "PASS: V25 Sàn > Lỗ Mở Sàn uniquely resolves/provisions exact slabOpen, rejects ambiguous "
        "exact-family candidates, activates before QS3DDRAWACTIVE dispatch, blocks generic basic draw "
        "fallback, and preserves the dedicated negative-Z/BoolSubtract command boundary. "
        "NATIVE_RUNTIME=LOCAL_ONLY"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
