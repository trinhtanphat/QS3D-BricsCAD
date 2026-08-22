#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message):
    print("ERROR:", message)
    return 1


def require(path, tokens):
    if not path.is_file():
        raise RuntimeError(f"missing Family Template surface: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise RuntimeError(f"{path.relative_to(ROOT)} missing Family Template import contract token: {token}")
    return text


def main():
    try:
        ui = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.TemplateCatalog.cs",
            (
                'Content = "Nạp file…"',
                "OnLoadFamilyTemplateFileClick",
                "new OpenFileDialog",
                'Filter = "QS3D Template (*.qs3dtpl)|*.qs3dtpl|All files (*.*)|*.*"',
                "store.Load(dialog.FileName)",
                "FamilyTemplateImportService.Apply(project, profile)",
                "profile.QuantityRules.Count + profile.LayerMappings.Count + profile.VisibleBqColumns.Count",
            ),
        )
        service = require(
            ROOT / "src/QS3D.Core/Templates/FamilyTemplateImportService.cs",
            (
                "public static class FamilyTemplateImportService",
                "x.Category == source.Category",
                "string.Equals(x.Name, source.Name, StringComparison.OrdinalIgnoreCase)",
                "NextLocalId(project)",
                "ProjectFamilyService.SetProperty",
                '"family.template.import"',
            ),
        )
    except RuntimeError as exc:
        return fail(str(exc))

    handler = ui.find("private void OnLoadFamilyTemplateFileClick")
    load = ui.find("store.Load(dialog.FileName)", handler)
    import_call = ui.find("FamilyTemplateImportService.Apply(project, profile)", load)
    refresh = ui.find("RefreshAfterCommit(", import_call)
    if min(handler, load, import_call, refresh) < 0 or not (handler < load < import_call < refresh):
        return fail("Family Template file route must validate/load before family-only import and refresh")

    apply_method = service.find("public static FamilyTemplateImportResult Apply")
    family_loop = service.find("foreach (var source in profile.Families", apply_method)
    if apply_method < 0 or family_loop < 0:
        return fail("family-only import must enumerate profile.Families")

    forbidden = (
        "profile.QuantityRules)",
        "profile.LayerMappings)",
        "profile.VisibleBqColumns)",
        "TemplateProfileStore().Apply",
    )
    apply_body = service[apply_method:]
    for token in forbidden:
        if token in apply_body:
            return fail("Family-only import service must not apply non-Family template sections: " + token)

    if "source.Id" in service[family_loop:service.find("private static void ValidateSourceFamilies")]:
        return fail("new target Families must not trust/reuse serialized source Family ids")

    print(
        "PASS: Family Manager can reload .qs3dtpl via bounded TemplateProfileStore.Load, then applies only "
        "Families by Category+Name with fresh local ids and normal Family property mutation semantics."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
